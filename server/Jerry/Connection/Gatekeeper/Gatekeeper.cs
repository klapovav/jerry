using Jerry.Connection.Security;
using Master;
using Serilog;
using Slave;
using System;
using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<Guid, Ticket> clients;
    private int handshakeCount = 0;
    private Task<IEnumerable<Guid>>? updateClientsTask;

    public Gatekeeper(string password, Guid localId, IClientManager virtualDesk)
    {
        correctPassword = password;
        serverID = localId;
        virtualDesktopManager = virtualDesk;
        keyExchange = new();
        clients = new ConcurrentDictionary<Guid, Ticket>();
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

    private void InitiateDataUpdate() => updateClientsTask = virtualDesktopManager.GetConnectedClientsAsync();


    private void CompleteDataUpdate()
    {
        var keysToInclude = updateClientsTask?.Result ?? Enumerable.Empty<Guid>();
        var keysToRemove = clients.Keys.Except(keysToInclude).ToList();
        foreach (var key in keysToRemove)
            clients.Remove(key, out _);
    }

    public HandshakeResult Handshake(CommunicationLayer layer, out ConnectedClient client)
    {
        client = default;
        //init info exchange
        if (!layer.TryGetRequest(Request.InitInfo, out var received))
        {
            return new HandshakeResult(Rejection.InitialInfoMissing);
        }

        //init info validation
        Log.Information("Incoming message: {@ClientInfo}", received.InitInfo);
        var validationResult = ValidateInitInfo(received.InitInfo);

        if (validationResult.Succeeded)
        {
            handshakeCount++;
            var newInitInfo = validationResult.RepairedInfo!;
            client = new ConnectedClient(layer, new Ticket(handshakeCount), newInitInfo);
            if (!clients.TryAdd(client.Info.Guid, client.ID))
            {
                return new HandshakeResult(Rejection.Unknown);
            }
            
        }

        return validationResult;
    }

    private HandshakeResult ValidateInitInfo(ClientInfo received)
    {
        if (received is null)
        {
            Log.Warning("Client failed to send initial handshake data");
            return new HandshakeResult(Rejection.InitialInfoMissing);
        }
        if (received.Password != correctPassword)
        {
            Log.Debug("Wrong password; expected '{Exp}' - received '{Received}' ", correctPassword, received.Password);
            return new HandshakeResult(Rejection.WrongPassword);
        }

        if (received.Width < 1 || received.Height < 1)
        {
            return new HandshakeResult(Rejection.UnexpectedResolution);
        }
        CompleteDataUpdate();
        var ValidInfo = new ClientValidInfo(received!, clients, serverID);

        return new HandshakeResult(ValidInfo, ValidInfo.Warning);
    }

    public void DisconnectAll()
    {
        InitiateDataUpdate();
        CompleteDataUpdate();
        foreach (var client in clients)
        {
            virtualDesktopManager.DisconnectClient(client.Value);
        }
    }

    public void Dispose()
    {
        DisconnectAll();
        clients.Clear();
    }
}