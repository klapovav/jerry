using Jerry.ConfigurationManager;
using Jerry.Controllable;
using Serilog;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Jerry.Connection;

public delegate void OnSocketAccept(Socket s);

public class TcpServer : IDisposable
{
    public event NewClientEventHandler? OnIncomingConnection;
    public delegate void NewClientEventHandler(Gatekeeper.HandshakeResult result);
    private readonly TcpListener tcpListener;
    private readonly ClientHealthChecker clientHealthChecker;

    private IPEndPoint IPEndPoint { get; }

    private readonly Gatekeeper.Gatekeeper Gatekeeper;
    private CancellationTokenSource? acceptClientConnections;
    public bool IsRunning { get; private set; }

    public TcpServer(IClientManager virtualDesk, Settings settings)
    {
        var serverID = Guid.NewGuid();
        IPEndPoint = new IPEndPoint(IPAddress.Any, settings.Port);
        tcpListener = new TcpListener(IPEndPoint.Address, IPEndPoint.Port);
        Gatekeeper = new Gatekeeper.Gatekeeper(settings.Password, serverID, virtualDesk);
        clientHealthChecker = new(virtualDesk);


        Log.Debug("Password : {0}", settings.Password);
        Log.Debug("Heartbeat interval : {0}ms", clientHealthChecker.CHECK_INTERVAL);
        Log.Debug("Server ID : '{0}'", serverID);
    }

    public void StartListening()
    {
        acceptClientConnections = new CancellationTokenSource();
        var _acceptClientsTask = Task.Factory.StartNew(
            () => ListenLoop(OnSocketAccept, acceptClientConnections.Token),
            acceptClientConnections.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        IsRunning = true;
    }

    public void StopListening()
    {
        acceptClientConnections?.Cancel();
        tcpListener.Stop();
        IsRunning = false;
    }

    public void DisconnectAll()
    {
        Gatekeeper.DisconnectAll();
    }

    private void OnSocketAccept(Socket socket)
    {
        try
        {
            Log.Information("New incoming connection.{EndPoint}", socket.RemoteEndPoint);
            // Ensure that the HealthChecker does not halt (due to potentially
            // outdated data) shortly after a new client is connected, as the number
            // of clients may change in the near future.
            clientHealthChecker.KeepRunning(TimeSpan.FromSeconds(3));
            var result = Gatekeeper.HandleIncomingConnection(socket);
            if (result.Succeeded) 
            {
                clientHealthChecker.Start();
            }
            OnIncomingConnection?.Invoke(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "TCP Server OnSocketAccept Exception");
        }
    }

    private void ListenLoop(OnSocketAccept onSocketAcceptDelegate, CancellationToken token)
    {
        try
        {
            tcpListener.Start();
            var endpoint = IPAddressProvider.GetEndPoint();
            Log.Information("Waiting for incoming connection on {Address}", endpoint.ToString());

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var socket = tcpListener.AcceptSocket();
                    onSocketAcceptDelegate(socket);
                }
                catch (InvalidOperationException) when (token.IsCancellationRequested)
                {
                    Log.Error("Listen loop cancellation requested");
                    break;
                }

            }
        }
        catch (SocketException e)
        {
            if (e.ErrorCode == 10048)
                Log.Error("Listen loop exception: Address already in use");
            else
                Log.Error("Listen loop socket exception: {0}", e.ErrorCode);
        }
        catch (Exception e)
        {
            Log.Error("Server listen loop exception: {0}", e);
        }
        finally
        {
            Log.Information("Acceptance of new connections is disabled");
            tcpListener?.Stop();
        }
    }

    public void Dispose()
    {
        clientHealthChecker.Dispose();
        StopListening();
        Gatekeeper.DisconnectAll();
        Gatekeeper.Dispose();
        Log.Debug("TcpServer disposed");
    }
}