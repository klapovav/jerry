using Jerry.Connection;
using Jerry.Connection.Gatekeeper;
using Jerry.Coordinates;
using Jerry.Extensions;
using Master;
using Serialization;
using Serilog;
using Slave;
using System;
using System.Diagnostics;
using System.Security.Policy;
using System.Windows.Forms;
using System.Windows.Input;

namespace Jerry.Controllable;

public class Client : IControllableComputer
{
    private readonly CommunicationLayer comLayer;
    private readonly ClientValidInfo clientInfo;
    private readonly ScreenSimple primaryMonitor;
    private readonly Stopwatch lastMoveSend = new();
    private bool relativeMove = true;
    private Vector groupedMove = new(0, 0);
    //FEAT 
    // 1. configuration
    // 2. debug hotkey
    // 3. dynamic adaptation (LAN)
    // 4. dynamic adatpation (client processing capability)
    private const ushort MAX_POLLING_RATE = 125; 
    private const double groupingInterval = 1000.0 / MAX_POLLING_RATE;

    public Client(CommunicationLayer layer, Ticket sessionID, ClientValidInfo info)
    {
        comLayer = layer;
        clientInfo = info;
        Ticket = sessionID;
        primaryMonitor = new ScreenSimple(info.Resolution, info.Name);
        CursorPosition = new LocalCoordinate(info.Cursor.X, info.Cursor.Y);
        lastMoveSend.Start();
    }

    public string Name => clientInfo.Name;
    public Guid ID => clientInfo.Guid;
    public string OS => clientInfo.OS.ToString();

    public LocalCoordinate CursorPosition { get; private set; }

    public Ticket Ticket { get; }

    public void OnMouseMove(int dx, int dy)
    {
        int x;
        int y;
        if (relativeMove)
        {
            groupedMove += new Vector(dx, dy);
            SendConditionally(groupedMove.DX, groupedMove.DY);
        }
        else
        {
            x = Math.Max(0, CursorPosition.X + dx);
            x = Math.Min(x, primaryMonitor.Position.Width);// - 1);
            y = Math.Max(0, CursorPosition.Y + dy);
            y = Math.Min(y, primaryMonitor.Position.Height);// - 1);
            CursorPosition = new LocalCoordinate(x, y);
            SendConditionally(x, y);
        }
    }

    private void SendConditionally(int x, int y)
    {
        if (lastMoveSend.ElapsedMilliseconds > groupingInterval)
        {
            comLayer.TrySendMessage(comLayer.Factory.MouseMove(x, y));
            groupedMove = new Vector(0, 0); //REVIEW
            lastMoveSend.Restart();
        }
    }

    public void OnMouseClick(Events.MouseButton ev)
    {
        var state = ev.ButtonPressed == Events.State.Pressed ? State.Pressed : State.Released;
        Master.Button btn = ev.Button switch
        {
            Events.Button.Left => Master.Button.Left,
            Events.Button.Right => Master.Button.Right,
            Events.Button.Middle => Master.Button.Middle,
            Events.Button.X1 => Master.Button.Xbutton1,
            Events.Button.X2 => Master.Button.Xbutton2,
            _ => Master.Button.Left,
        };

        comLayer.TrySendMessage(MessageFactory.MouseClick(btn, state));
    }

    public void OnMouseWheel(Events.MouseWheel wh)
    {
        var (dir, amount) = wh.ScrollDirection switch
        {
            Events.Direction.Up => (Direction.ScrollUp, wh.Amount),
            Events.Direction.Down => (Direction.ScrollDown, wh.Amount),
            Events.Direction.Right => (Direction.ScrollRight, wh.Amount),
            Events.Direction.Left => (Direction.ScrollLeft, wh.Amount),
            _ => (Direction.ScrollLeft, 0),
        };
        if (amount != 0)
            comLayer.TrySendMessage(comLayer.Factory.MouseWheel(dir, amount));
    }

    public void OnKeyEvent(Events.KeyboardHookEvent keyEvent)
    {
        if ((Keys)keyEvent.KeyCode == Keys.LControlKey && keyEvent.ScanCode != 0x1D) //0x1D... LCtrl scancode,
        {
            Log.Debug("Key: {ctrl} | AltGr", (Keys)keyEvent.KeyCode);
            return;
        }
        var keyPosition = GetLayoutIndependentKey(keyEvent);
        var message = MessageFactory.KeyboardEvent(keyPosition, keyEvent.Pressed);
        comLayer.TrySendMessage(message);
    }

    public bool OnDeactivate(out Common.Clipboard clipboard)
    {
        var clipReceived = comLayer.TryGetRequest(Request.Clipboard, out SlaveMessage message);
        clipboard = message?.ClipboardSession;

        if (!clipReceived)
        {
            comLayer.TrySendMessage(comLayer.Factory.SessionEnd());
            return false;
        }

        if (message?.NoResponse is not null)
        {
            Log.Debug("Client failed to access clipboard {}", message?.NoResponse?.Reason);
            comLayer.TrySendMessage(comLayer.Factory.SessionEnd());
            return false;
        }

        var msg = message?.ClipboardSession?.Message;

        bool updateJerryClipboard = !string.IsNullOrEmpty(msg);
        if (updateJerryClipboard)
        {
            Log.Debug("Global clipboard content changed:{} | content[0..50]: {}", updateJerryClipboard, clipboard.Message.Truncate(50));
        }
        comLayer.TrySendMessage(comLayer.Factory.SessionEnd());
        return updateJerryClipboard;
    }

    public void OnActivate(Common.Clipboard clipboard)
    {
        comLayer.TrySendMessage(comLayer.Factory.SessionBegin(relativeMove));
        if (clipboard is not null)
            comLayer.TrySendMessage(comLayer.Factory.ClipboardContent(clipboard.Message, clipboard.Format));
    }

    public void ToogleMouseMode()
    {
        comLayer.TrySendMessage(comLayer.Factory.SessionEnd());
        relativeMove = !relativeMove;
        comLayer.TrySendMessage(comLayer.Factory.SessionBegin(relativeMove));
    }


    public bool TrySendHeartbeat()
    {
        return comLayer.TrySendMessage(comLayer.Factory.Heartbeat());
    }

    public void ReleaseModifiers(ModifierKeys modifiers)
    {
        Log.Debug("Release modifiers: {mod}", modifiers); //scan: (0x){scan:X4}
        if (modifiers.HasFlag(ModifierKeys.Windows))
        {
            comLayer.TrySendMessage(MessageFactory.KeyboardEvent((uint)Keys.LWin, false));
            comLayer.TrySendMessage(MessageFactory.KeyboardEvent((uint)Keys.RWin, false));
        }
        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            comLayer.TrySendMessage(MessageFactory.KeyboardEvent((uint)Keys.LControlKey, false));
            comLayer.TrySendMessage(MessageFactory.KeyboardEvent((uint)Keys.RControlKey, false));
        }
        if (modifiers.HasFlag(ModifierKeys.Alt))
        {
            comLayer.TrySendMessage(MessageFactory.KeyboardEvent((uint)Keys.LMenu, false));
            comLayer.TrySendMessage(MessageFactory.KeyboardEvent((uint)Keys.RMenu, false));
        }
        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            comLayer.TrySendMessage(MessageFactory.KeyboardEvent((uint)Keys.LShiftKey, false));
            comLayer.TrySendMessage(MessageFactory.KeyboardEvent((uint)Keys.RShiftKey, false));
        }
    }

    private static uint GetLayoutIndependentKey(Events.KeyboardHookEvent keyEvent)
    {
        //scancode => virtual (US Layout)
        var vk = keyEvent.KeyCode;

        return (keyEvent.ScanCode, keyEvent.Flags.HasFlag(Hook.WinApi.KeyFlags.EXTENDEDKEY)) switch
        {
            //a-z
            (0x1E, _) => 0x41,
            (0x30, _) => 0x42,
            (0x2E, _) => 0x43,
            (0x20, _) => 0x44,
            (0x12, _) => 0x45,
            (0x21, _) => 0x46,
            (0x22, _) => 0x47,
            (0x23, _) => 0x48,
            (0x17, _) => 0x49,
            (0x24, _) => 0x4A,
            (0x25, _) => 0x4B,
            (0x26, _) => 0x4C,
            (0x32, _) => 0x4D,
            (0x31, _) => 0x4E,
            (0x18, _) => 0x4F,
            (0x19, _) => 0x50,
            (0x10, _) => 0x51,
            (0x13, _) => 0x52,
            (0x1F, _) => 0x53,
            (0x14, _) => 0x54,
            (0x16, _) => 0x55,
            (0x2F, _) => 0x56,
            (0x11, _) => 0x57,
            (0x2D, _) => 0x58,
            (0x15, _) => 0x59,
            (0x2C, _) => 0x5A,
            //num 0-9
            (0x0B, _) => 0x30,
            (0x02, _) => 0x31,
            (0x03, _) => 0x32,
            (0x04, _) => 0x33,
            (0x05, _) => 0x34,
            (0x06, _) => 0x35,
            (0x07, _) => 0x36,
            (0x08, _) => 0x37,
            (0x09, _) => 0x38,
            (0x0A, _) => 0x39,
            //oem
            (0x27, _) => 0xBA,
            (0x0D, _) => 0xBB,
            (0x33, _) => 0xBC,
            (0x0C, _) => 0xBD,
            (0x34, _) => 0xBE,
            (0x35, false) => 0xBF,
            (0x29, _) => 0xC0,
            (0x1A, _) => 0xDB,
            (0x2B, _) => 0xDC,
            (0x1B, _) => 0xDD,
            (0x28, _) => 0xDE,
            //oem 102
            (0x56, _) => 0xE2,

            //left system
            (0x01, _) => 0x1B, // escape
            (0x0F, _) => 0x09, // tab
            (0x3A, _) => 0x14, // capslock
            (0x2A, _) => 0xA0, // left shift

            // right system
            (0x0E, false) => 0x08, // backspace
            (0x1C, false) => 0x0D, // return
            (0x36, _) => 0xA1,     // right shift

            //numeric keypad
            (0x1C, true) => 0x0A, //Numeric Enter .... 0x0A-0B reserved
            (0x35, true) => vk,

            // F1 - F12
            (0x3B, _) => 0x70, //  VK_F1
            (0x3C, _) => 0x71, //  VK_F2
            (0x3D, _) => 0x72, //  VK_F3
            (0x3E, _) => 0x73, //  VK_F4
            (0x3F, _) => 0x74, //  VK_F5
            (0x40, _) => 0x75, //  VK_F6
            (0x41, _) => 0x76, //  VK_F7
            (0x42, _) => 0x77, //  VK_F8
            (0x43, _) => 0x78, //  VK_F9
            (0x44, _) => 0x79, //  VK_F10
            (0x57, _) => 0x7A, //  VK_F11
            (0x58, _) => 0x7B, //  VK_F12

            _ => vk, // layout independent
        };
    }
}