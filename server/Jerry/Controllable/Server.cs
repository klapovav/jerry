using Jerry.Coordinates;
using Jerry.Extensions;
using Serilog;
using System;
using System.Windows.Input;
using TextCopy;
using WK.Libraries.SharpClipboardNS;
using ClipboardData = Common.Clipboard;
using Format = Common.Clipboard.Types.Format;

namespace Jerry.Controllable;

public class Server : IControllableComputer
{
    private SharpClipboard Clipboard { get; }
    public string Name => Environment.MachineName;
    public Guid ID { get; private set; }
    public string OS => "Windows";
    public LocalCoordinate CursorPosition { get; private set; }
    public Ticket Ticket { get; }
    private ClipboardData SessionClipboard { get; set; }
    private ClipboardData GlobalClipboard { get; set; }

    public Server(Ticket sessionID)
    {
        ID = Guid.NewGuid();
        Ticket = sessionID;
        Clipboard = new SharpClipboard();
        Clipboard.ClipboardChanged += OnClipboardChange;
        Clipboard.ObserveLastEntry = false;
        CursorPosition = new(100, 100);
    }

    public bool OnDeactivate(out ClipboardData clipboard)
    {
        if (SessionClipboard?.Format == Format.Text)
            SessionClipboard.Message = Clipboard.ClipboardText;
        clipboard = SessionClipboard;
        return clipboard != null && clipboard != GlobalClipboard;
    }

    public void OnActivate(ClipboardData clipboard)
    {
        GlobalClipboard = clipboard;
        SessionClipboard = null;

        if (clipboard is null)
            return;

        if (clipboard.Format == Format.Text)
        {
            var a = ClipboardService.SetTextAsync(clipboard.Message);
            Log.Debug("Local clipboard content length: {msg}; ", clipboard.Message.Length);
            Log.Debug("Local clipboard[0..50]:  {msg}", clipboard.Message.Truncate(50));
            a.Wait();
        }
        else
        {
            Log.Error("Clipboard filelist format [not implemented]: {msg} ", clipboard.Message);
        }

    }

    private void OnClipboardChange(object sender, SharpClipboard.ClipboardChangedEventArgs e)
    {
        SessionClipboard ??= new ClipboardData() { Format = Format.File };
        switch (e.ContentType)
        {
            case SharpClipboard.ContentTypes.Files:
                #region UNDONE

                var files = string.Join("\n", Clipboard.ClipboardFiles);
                Log.Debug("Clipboard content changed - [Files] [{number}]: {new}", Clipboard.ClipboardFiles.Count, files);
                SessionClipboard = new ClipboardData()
                {
                    Format = Format.File,
                    Message = string.Join("\n", Clipboard.ClipboardFiles)
                };

                #endregion UNDONE
                break;
            case SharpClipboard.ContentTypes.Text:
                //REVIEW e.Content.ToString() ~ Clipboard.ClipboardText
                SessionClipboard.Format = Format.Text;
                break;

            default:
                return;
        }
        //Log.Information("Clipboard content: {new}", LocalClipData.Message);
    }

    public void OnKeyEvent(Events.KeyboardHookEvent keyEvent)
    { }

    public void OnMouseClick(Events.MouseButton ev)
    { }

    public void OnMouseWheel(Events.MouseWheel ev)
    { }

    public void OnMouseMove(int x, int y)
    {
        CursorPosition = new LocalCoordinate(x, y);
    }

    public bool TrySendHeartbeat()
    {
        return true;
    }

    public void ReleaseModifiers(ModifierKeys modifiers)
    { }

    public void ToogleMouseMode()
    { }
}