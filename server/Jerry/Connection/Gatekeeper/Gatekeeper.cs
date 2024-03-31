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
    private readonly ClientHealthChecker clientHealthChecker;
    private readonly KeyExchange keyExchange;
    private readonly ConcurrentDictionary<Guid, Ticket> clients;
    private int handshakeCount = 0;
    private Task<IEnumerable<Guid>> updateClientsTask;

    public Gatekeeper(string password, Guid localId, IClientManager virtualDesk)
    {
        correctPassword = password;
        serverID = localId;
        virtualDesktopManager = virtualDesk;
        keyExchange = new();
        clientHealthChecker = new(virtualDesk);
        clients = new ConcurrentDictionary<Guid, Ticket>();

        Log.Debug("Server's password : {0}", correctPassword);
        Log.Debug("Server's heartbeat interval : {0}ms", clientHealthChecker.CHECK_INTERVAL);
        Log.Debug("Server's guid : '{0}'", serverID);
    }

    public void TryAccept(Socket socket)
    {
        var stopwatch = Stopwatch.StartNew();
        // Ensure that the HealthChecker does not halt (due to potentially
        // outdated data) shortly after a new client is connected, as the number
        // of clients may change in the near future.
        clientHealthChecker.KeepRunning(TimeSpan.FromSeconds(3));
        InitiateDataUpdate();
        var stream = new NetworkStream(socket, true);

        //Handshake - stream cipher
        Agreement output_key, input_key;
        try
        {
            output_key = keyExchange.GeAgreement(stream);
            input_key = keyExchange.GeAgreement(stream);
        }
        catch (KeyExchangeException)
        {
            Log.Error("Key exchange failed; Client and server couldn't agree on algorithm for key exchange and encryption");
            stream.Dispose();
            return;
        }

        var encryptor = new Encryptor(output_key);
        var decryptor = new Encryptor(input_key);
        var layer = new CommunicationLayer(stream, encryptor, decryptor, false);  //DEBUG  encryption on/off
        var result = Handshake(layer, out var acceptedClient);
        layer.NotifyConnectionResult(result);

        stopwatch.Stop();
        if (!result.Succeeded)
        {
            Log.Warning("Handshake failed {Result}", result.RejectionType.ToString());
            return;
        }

        var client = result.RepairedInfo;
        Log.Verbose("{@ClientInfoValue}", client);
        //Log.Information("Client {Name} display {Width}x{Height} connected, cursor position {X}x{Y}", client.Name,
        //client.Resolution.Width, client.Resolution.Height, client.Cursor.X, client.Cursor.Y);
        _ = clients.TryGetValue(client.Guid, out _);

        virtualDesktopManager.RegisterClient(acceptedClient);

        Log.Debug("Handshake completed successfully Exec time: {Elapsed:000} ms;  Warnings: {war}", stopwatch.Elapsed.Milliseconds, result.Warnings);
    }

    private void InitiateDataUpdate() => updateClientsTask = virtualDesktopManager.GetConnectedClients();

    private void CompleteDataUpdate()
    {
        var keysToInclude = updateClientsTask?.Result;
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

            var new_init_info = validationResult.RepairedInfo;
            client = new ConnectedClient(layer, new Ticket(handshakeCount), new_init_info);
            if (!clients.TryAdd(client.Info.Guid, client.ID))
            {
                return new HandshakeResult(Rejection.Unknown);
            }
            else
            {
                if (clients.Count == 1)
                {
                    clientHealthChecker.Start();
                }
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
        var ValidInfo = new ClientValidInfo(received, clients, serverID);

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
        clientHealthChecker.Dispose();
        DisconnectAll();
        clients.Clear();
    }
}