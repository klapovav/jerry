using Serilog;
using System;
using System.Net;
using System.Net.Sockets;

namespace Jerry.Connection;

public class IPAddressProvider
{
    public static IPEndPoint GetEndPoint()
    {
        if (!TryGetAddress(out IPAddress address))
        {
            Log.Error("Failed to get local IP address");
        }
        var set = new ConfigurationManager.AppSettings().GetSettings();
        return new IPEndPoint(address, set.Port);
    }

    private static bool TryGetAddress(out IPAddress address)
    {
        try
        {
            using Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            if (socket.LocalEndPoint is not IPEndPoint endPoint)
            {
                address = IPAddress.None;
                return false;
            }
            address = endPoint.Address;
            return true;
        }
        catch (Exception e)
        {
            address = IPAddress.None;
            Log.Error($"Error: {e.Message}");
            return false;
        }
    }
}