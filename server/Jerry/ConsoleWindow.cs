using Serilog;
using Serilog.Core;
using System;
using System.Runtime.InteropServices;


namespace Jerry;

public class ConsoleWindow
{
    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    /// <summary>
    /// Allocates a new console for the calling process. A process can be associated with only one console,
    /// so the AllocConsole function fails if the calling process already has a console. A process can use
    /// the FreeConsole function to detach itself from its current console, then it can call AllocConsole
    /// to create a new console or AttachConsole to attach to another console.
    /// </summary>
    /// <returns>If the function succeeds, the return value is true. </returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    public static extern void FreeConsole();

    const int SW_HIDE = 0;
    const int SW_SHOW = 5;

    private bool visible;
    private nint wndHandle;

    public bool IsVisible
    {
        get { return visible; }
        private set { 
            visible = value;
            if (visible)
                Show();
            else
                Hide();
        }
    }

    
    public ConsoleWindow(bool visible = true)
    {
        if (!AllocConsole())
            Log.Error("Console window allocation failed");

        wndHandle = GetConsoleWindow();
        IsVisible = visible;
    }
    public void ChangeVisibility()
    {
        IsVisible = IsVisible ? false : true;
    }


    private void Hide() => ShowWindow(wndHandle, SW_HIDE);
    private void Show() => ShowWindow(wndHandle, SW_SHOW);
}
