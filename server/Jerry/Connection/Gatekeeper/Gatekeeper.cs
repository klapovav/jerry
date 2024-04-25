using Jerry.Connection.Security;
using Jerry.Coordinates;
using Jerry.Extensions;
using Master;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Jerry.Connection.Gatekeeper;

public class Gatekeeper : IDisposable
{
    private readonly string correctPassword;
    private readonly Guid serverID;
    private readonly IClientManager virtualDesktopManager;
    private readonly KeyExchange keyExchange;
    private int handshakeCount = 0;
    private Task<IEnumerable<Guid>>? clients;

    public Gatekeeper(string password, Guid localId, IClientManager virtualDesk)
    {
        correctPassword = password;
        serverID = localId;
        virtualDesktopManager = virtualDesk;
        keyExchange = new();
    }

    public HandshakeResult HandleIncomingConnection(Socket socket)
    {
        var stopwatch = Stopwatch.StartNew();
        
        InitiateDataUpdate();
        var stream = new NetworkStream(socket, true);

        //Handshake - stream cipher
        Agreement outputKey, inputKey;
        try
        {
            outputKey = keyExchange.GeAgreement(stream);
            inputKey = keyExchange.GeAgreement(stream);
        }
        catch (KeyExchangeException)
        {
            Log.Error("Key exchange failed; Client and server couldn't agree on algorithm for key exchange and encryption");
            stream.Dispose();
            return new HandshakeResult(Rejection.KeyExchangeFailed);
        }

        var encryptor = new Encryptor(outputKey);
        var decryptor = new Encryptor(inputKey);
        var layer = new CommunicationLayer(stream, encryptor, decryptor, true);  //DEBUG  encryption on/off
        var result = Handshake(layer, out var acceptedClient);
        layer.SendConnectionResult(result);

        stopwatch.Stop();
        if (!result.Succeeded)
        {
            Log.Warning("Handshake failed {Result}", result.RejectionType.ToString());
            return result;
        }

        var client = result.RepairedInfo!; 
        Log.Verbose("{@ClientInfoValue}", client);
        //Log.Information("Client {Name} display {Width}x{Height} connected, cursor position {X}x{Y}", client.Name,
        //client.Resolution.Width, client.Resolution.Height, client.Cursor.X, client.Cursor.Y);

        virtualDesktopManager.RegisterClient(acceptedClient);

        Log.Debug("Handshake completed successfully Exec time: {Elapsed:000} ms;  Warnings: {war}", stopwatch.Elapsed.Milliseconds, result.Warnings);
        return result;

    }
   
    private void InitiateDataUpdate() => clients = virtualDesktopManager.GetConnectedClientsAsync();


    private IEnumerable<Guid> CompleteDataUpdate() => clients?.Result ?? Enumerable.Empty<Guid>();

    private HandshakeResult Handshake(CommunicationLayer layer, out ConnectedClient client)
    {
        client = default;
        //init info exchange
        var responded = layer.TryGetRequest(Request.InitInfo, out var rec);
        if (!responded || rec is null)
        {
            return new HandshakeResult(Rejection.InitialInfoMissing);
        }
        var received = rec.InitInfo;
        //init info validation
        Log.Information("Incoming message: {@ClientInfo}", received);
        if (received.Password != correctPassword)
        {
            Log.Debug("Wrong password; expected '{Exp}' - received '{Received}' ", correctPassword, received.Password);
            return new HandshakeResult(Rejection.WrongPassword);
        }
        if (received.Width < 1 || received.Height < 1)
        {
            return new HandshakeResult(Rejection.UnexpectedResolution);
        }

        handshakeCount++;
        var clients = CompleteDataUpdate();
        var (issues,validInfo) = DataCorrection(received!, clients.Append(serverID));
        var validationResult = new HandshakeResult(validInfo, issues);
        client = new ConnectedClient(layer, new Ticket(handshakeCount), validInfo);
        return validationResult;
    }

    private (FixableIssue, ClientInfo) DataCorrection(Slave.ClientInfo original, IEnumerable<Guid> connected)
    {

        ArgumentNullException.ThrowIfNull(original, nameof(original));
        var warning = FixableIssue.None;
        

        if (!Guid.TryParse(original.Guid.Value, out Guid candidate))
        {
            warning = warning.Add(FixableIssue.GuidInvalid);
            candidate = GenerateUniqueGuid(connected);
        }
        else if (connected.Contains(candidate))
        {
            warning = warning.Add(FixableIssue.GuidAlreadyUsed);
            candidate = GenerateUniqueGuid(connected);
        }

        var x = original.Cursor?.X ?? original.Width / 2;
        var y = original.Cursor?.Y ?? original.Height / 2;
        y = Math.Min(Math.Max(0, y), original.Height - 1);
        x = Math.Min(Math.Max(0, x), original.Width - 1);

        if (y != original.Cursor?.Y || x != original.Cursor?.X)
        {
            warning = warning.Add(FixableIssue.MousePositionOutOfBounds);
        }

        return (warning, new ClientInfo(original, candidate, new LocalCoordinate(x, y)));
       
    }
    private static Guid GenerateUniqueGuid(IEnumerable<Guid> used)
    {
        var guid = Guid.NewGuid();
        while (used.Contains(guid))
        {
            guid = Guid.NewGuid();
        }
        return guid;
    }

    public void DisconnectAll()
    {
        InitiateDataUpdate();
        var clients = CompleteDataUpdate();
        foreach (var client in clients)
        {
            virtualDesktopManager.DisconnectClient(client);
        }
    }

    public void Dispose()
    {
        DisconnectAll();
    }
}