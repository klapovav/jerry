using Google.Protobuf;
using Jerry.Connection.Gatekeeper;
using Jerry.Connection.Security;
using Master;
using Serialization;
using Serilog;
using Slave;
using System;
using System.IO;
using System.Net.Sockets;

namespace Jerry.Connection;

public class CommunicationLayer : IDisposable
{
    private readonly NetworkStream networkStream;
    private readonly IEncDecryptor encryptor;
    private readonly IEncDecryptor decryptor;
    private readonly bool encryption_on;
    public MessageFactory Factory { get; }
    public uint FailureCount { get; private set; }

    public CommunicationLayer(NetworkStream stream, IEncDecryptor encryptor, IEncDecryptor decryptor, bool encrypted = true)
    {
        networkStream = stream;
        networkStream.ReadTimeout = 100;
        networkStream.Socket.NoDelay = true;
        this.encryptor = encryptor;
        this.decryptor = decryptor;
        encryption_on = encrypted;
        Factory = new MessageFactory();
    }

    public void SendConnectionResult(Connection.Gatekeeper.HandshakeResult result)
    {
        if (result.Succeeded)
        {
            if (result.Warnings == ErrorLeadingToDataCorrection.None)
                TrySendMessage(Factory.ConnectionResult(Master.HandshakeResult.Success, ""));
            else
                TrySendMessage(Factory.ConnectionResult(Master.HandshakeResult.SuccessWarning, result.Warnings.ToString()));
            return;
        }
        var echo = result.RejectionType switch
        {
            Rejection.None or
            Rejection.Unknown or
            Rejection.UnexpectedResolution => Factory.ConnectionResult(Master.HandshakeResult.Rejection, result.Warnings.ToString()),
            Rejection.InitialInfoMissing => Factory.ConnectionResult(Master.HandshakeResult.Rejection, "ClientInfo"),
            Rejection.KeyExchangeFailed => Factory.ConnectionResult(Master.HandshakeResult.Rejection, "Key exchange failed"),
            Rejection.WrongPassword => Factory.ConnectionResult(Master.HandshakeResult.Rejection, "Password rejected"),
            _ => throw new NotImplementedException(),
        };
        TrySendMessage(echo);
        Disconnect();
        Dispose();
    }

    public bool TryGetRequest(Request requestType, out SlaveMessage response)
    {
        response = null;

        if (!TrySendMessage(Factory.Request(requestType)))
            return false;

        if (!TryReadResponse(out response, requestType is Request.InitInfo))
        {
            Log.Warning("Server did not receive response for a request {p}", requestType);
            return false;
        }
        if (response is null) return false;
        return requestType switch
        {
            Request.InitInfo => response.InitInfo is not null,
            Request.MousePosition => response.Cursor is not null,
            Request.Clipboard => (response.ClipboardSession is not null || response.NoResponse is not null),
            _ => false
        };
    }

    public bool TrySendMessage(MasterMessage message)
    {
        try
        {
            if (FailureCount > 3)
                return false;
            var encrypted = EncryptMessage(message);
            networkStream.Write(encrypted);
            FailureCount = 0;
            return true;
        }
        catch (Exception e)
        {
            FailureCount++;
            if (FailureCount == 1)
                Log.Warning($"Failed to send data {e.Message}");
            return false;
        }
    }

    public bool TryReadResponse(out SlaveMessage msg, bool extendedReadTimeout)
    {
        try
        {
            if (extendedReadTimeout) networkStream.ReadTimeout = 200;
            var bytes = new System.Collections.Generic.List<byte>();
            byte[] readBuffer = new byte[1024];
            try
            {
                while (true)
                {
                    int numberOfBytesRead = networkStream.Read(readBuffer);
                    bytes.AddRange(readBuffer[..numberOfBytesRead]);
                }
            }
            catch (IOException ex)
            {
                if (ex.InnerException is SocketException sx && sx.ErrorCode == 10060)
                {
                    // end of receiving data
                }
                else
                {
                    throw;
                }
            }
            Log.Debug($"Response: {bytes.Count} bytes");
            byte[] decoded = encryption_on ? decryptor.EncryptOrDecrypt(bytes.ToArray()) : bytes.ToArray();
            msg = SlaveMessage.Parser.ParseDelimitedFrom(new MemoryStream(decoded));
            return true;
        }
        catch (Exception e)
        {
            FailureCount++;
            if (FailureCount == 1)
                Log.Warning($"Read response failed. {e.Message}");
            msg = null;
            return false;
        }
        finally
        {
            networkStream.ReadTimeout = 100;
        }
    }

    private byte[] EncryptMessage(MasterMessage message)
    {
        //proto serialization
        var ms = new MemoryStream();
        message.WriteDelimitedTo(ms);
        ms.Flush();
        //encryption
        return encryption_on ? encryptor.EncryptOrDecrypt(ms.ToArray()) : ms.ToArray();
    }

    public bool IsConnected()
    {
        try
        {
            var socket = networkStream?.Socket;
            if (socket is null)
                return false;
            return !socket.Poll(1, SelectMode.SelectRead)
                   || socket.Available != 0;
        }
        catch (SocketException e)
        {
            if (e.NativeErrorCode.Equals(10035)) //"Still Connected, but the Send would block"
            {
                return true;
            }
            return false;
        }
        catch (ObjectDisposedException) { return false; }
        catch (NotSupportedException) { return false; }
    }

    public void Disconnect()
    {
        networkStream?.Close(100);
    }

    public void Dispose()
    {
        Log.Information("Communication layer disposed");
        networkStream?.Close();
        networkStream?.Dispose();
    }
}