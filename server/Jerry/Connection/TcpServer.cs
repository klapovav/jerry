using Jerry.ConfigurationManager;
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
    private readonly TcpListener tcpListener;
    private IPEndPoint IPEndPoint { get; }

    private readonly Gatekeeper.Gatekeeper Gatekeeper;
    private CancellationTokenSource acceptClientConnections; 
    public bool IsRunning { get; private set; }

    public TcpServer(IClientManager virtualDesk, Settings settings)
    {
        IPEndPoint = new IPEndPoint(IPAddress.Any, settings.Port);
        tcpListener = new TcpListener(IPEndPoint.Address, IPEndPoint.Port);
        Gatekeeper = new Gatekeeper.Gatekeeper(settings.Password, Guid.NewGuid(), virtualDesk);
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
        acceptClientConnections.Cancel();
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
            Gatekeeper.TryAccept(socket);
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
        catch (SocketException)
        {
            Log.Information("Accept socket interrupted");
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
        StopListening();
        Gatekeeper.DisconnectAll();
        Gatekeeper.Dispose();
        Log.Debug("TcpServer disposed");
    }
}