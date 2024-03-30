using Common;
using Slave;

namespace Serialization;

public static class SlaveFactory
{
    public static SlaveMessage CreateClientInitInfo(int w, int h, string name, string guid)
    {
        var Guid = new ClientInfo.Types.UUID
        {
            Value = guid
        };
        var init = new ClientInfo
        {
            Width = w,
            Height = h,
            Name = name,
            Guid = Guid
        };

        return new SlaveMessage()
        {
            InitInfo = init,
        };
    }

    public static SlaveMessage CreateClipboardData(string content, Clipboard.Types.Format format)
    {
        var m = new SlaveMessage
        {
            ClipboardSession = new Clipboard()
            {
                Format = format,
                Message = content
            }
        };
        return m;
    }
}