using Jerry.Connection.Gatekeeper;
using Jerry.Connection;

namespace Jerry;

public readonly struct ConnectedClient
{
    public readonly CommunicationLayer Layer;
    public readonly Ticket ID;
    public readonly ClientValidInfo Info;

    public ConnectedClient(CommunicationLayer layer, Ticket id, ClientValidInfo info)
    {
        Layer = layer;
        ID = id;
        Info = info;
    }
}