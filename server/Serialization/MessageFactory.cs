using Common;
using Master;
using System;
using System.Text;

namespace Serialization;

public class MessageFactory
{
    private readonly Random numGenerator;

    public MessageFactory()
    {
        numGenerator = new Random();
    }

    private string GetRandomString(int length)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < length; i++)
        {
            sb.Append(((char)(numGenerator.Next(0, 26) + 65)));
        }
        return sb.ToString();
    }

    public MasterMessage Heartbeat()
    {
        var m = new MasterMessage
        {
            Heartbeat = new Heartbeat()
            {
                OneWay = true
            },
            RndB = GetRandomString(numGenerator.Next(1, 5)),
            RndE = GetRandomString(numGenerator.Next(1, 5))
        };
        return m;
    }

    public MasterMessage Request(Request dataRequest)
    {
        return new MasterMessage()
        {
            Request = dataRequest,
            RndB = GetRandomString(numGenerator.Next(1, 3)),
            RndE = GetRandomString(numGenerator.Next(1, 3)),
        };
    }

    public MasterMessage ConnectionResult(HandshakeResult result, string message) => new()
    {
        Handshake = new Echo()
        {
            Result = result,
            Message = message
        },
        RndB = String.Empty,
        RndE = String.Empty
    };

    public MasterMessage MouseMove(int x, int y) => new()
    {
        MousePosition = new MouseMove
        {
            X = x,
            Y = y
        },
        RndB = String.Empty,
        RndE = String.Empty
    };

    public MasterMessage MouseWheel(Direction direction, int amount) => new()
    {
        MouseWheel = new()
        {
            ScrollDirection = direction,
            Amount = amount
        },
        RndB = String.Empty,
        RndE = String.Empty
    };

    public static MasterMessage MouseClick(Button button, State state) => new()
    {
        MouseClick = new MouseClick()
        {
            Button = button,
            EventType = state,
        },
        RndB = String.Empty,
        RndE = String.Empty
    };

    public static MasterMessage KeyboardEvent(uint key, bool pressed)
    {
        var m = new MasterMessage();

        var keyState = (pressed) ? State.Pressed : State.Released;
        m.Keyboard = new Keyboard()
        {
            Key = key,
            EventType = keyState,
        };
        m.RndB = String.Empty;
        m.RndE = String.Empty;
        return m;
    }

    public MasterMessage ClipboardContent(string content, Clipboard.Types.Format format)
    {
        var m = new MasterMessage
        {
            Clipboard = new Clipboard()
            {
                Format = format,
                Message = content
            },
            RndB = GetRandomString(numGenerator.Next(0, 3)),
            RndE = GetRandomString(numGenerator.Next(0, 3))
        };
        return m;
    }

    public MasterMessage SessionEnd()
    {
        var m = new MasterMessage
        {
            EndSession = new SessionEnd()
            {
            },
            RndB = GetRandomString(numGenerator.Next(0, 3)),
            RndE = GetRandomString(numGenerator.Next(0, 3))
        };
        return m;
    }

    public MasterMessage SessionBegin(bool relativeMove)
    {
        MasterMessage m = new()
        {
            StartSession = new SessionBegin()
            {
                MouseMoveRelative = relativeMove,
            },
            RndB = GetRandomString(numGenerator.Next(0, 3)),
            RndE = GetRandomString(numGenerator.Next(0, 3))
        };
        return m;
    }
}