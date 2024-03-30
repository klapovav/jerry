using Serilog;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Jerry.Hook.WinApi;
using System.Threading;

namespace Jerry.Hook;

public enum HookType
{
    KeyboardHook = WinApi.Constants.WH_KEYBOARD_LL,
    MouseHook = WinApi.Constants.WH_MOUSE_LL
}

public enum FilterResult
{
    Keep = 0,
    Discard = 1,
}


public interface IHook
{
    public void Install();
    public void Uninstall();
}


public abstract class BaseHook : IDisposable, IHook
{
    private IntPtr _hookHandle = IntPtr.Zero;
    private IntPtr _user32LibraryHandle;
    private readonly HookType _hookType;
    private readonly ILogger _logger;
    private NativeMethods.HookProc _hookDelegate;
    protected DataCollector dataCollector { get; set; }
    protected long ID;
    public bool IsInstalled => _hookHandle != IntPtr.Zero;

    public BaseHook(HookType hookType)
    {
        ID = DateTime.Now.Ticks;
        dataCollector = new DataCollector(hookType);
        _hookType = hookType;
        _hookDelegate = new NativeMethods.HookProc(OnHookCall);
        _logger = Log.ForContext<BaseHook>();
        _user32LibraryHandle = (Environment.Version.Major >= 4) ?
                NativeMethods.LoadLibrary("user32.dll") :
                NativeMethods.GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName);
        if (_user32LibraryHandle == IntPtr.Zero)
        {
            int errorCode = Marshal.GetLastWin32Error();
            if (Environment.Version.Major >= 4)
            {
                Log.Error($"Failed to load library 'User32.dll'. Error {errorCode}: {new Win32Exception(errorCode).Message}.");
            }
            else
            {
                Log.Error($"Failed to get current process module. Error {errorCode}: {new Win32Exception(errorCode).Message}.");
            }
        }
    }

    protected abstract IntPtr OnHookCall(int nCode, IntPtr wParam, IntPtr lParam);

    protected IntPtr CallNextHook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        return NativeMethods.CallNextHookEx(_hookHandle, nCode, (int)wParam, lParam);
    }

    public void Install()
    {
        var expected = DispatcherProvider.HookCallbackDispatcher.Thread;
        if (Environment.CurrentManagedThreadId != expected.ManagedThreadId)
        {
            // This should never happen under normal circumstances.
            Log.Warning("{@BaseHook} | Method {methodName} called from different thread: {id} ", this , nameof(this.Install), Thread.CurrentThread.ManagedThreadId);
        }
        DispatcherProvider.HookCallbackDispatcher.Invoke(() =>
        {
            if (IsInstalled)
                return;

            _hookHandle = NativeMethods.SetWindowsHookEx((int)_hookType, _hookDelegate, _user32LibraryHandle, 0);
            if (!IsInstalled)
                Log.Error("Failed to setup hook. Error: " + Marshal.GetLastWin32Error());
            else
                dataCollector = new DataCollector(_hookType);
            _logger.Debug("\t {@BaseHook}[{id}]  ", this, ID);
            _logger.Debug("{h}\tis installed: {i}", _hookType, IsInstalled);
        });


    }

    public void Uninstall()
    {
        var expected = DispatcherProvider.HookCallbackDispatcher.Thread;
        if (Environment.CurrentManagedThreadId != expected.ManagedThreadId)
        {
            // This should never happen under normal circumstances.
            Log.Warning("{@BaseHook} | Method {methodName} called from different thread: {id} ", this, nameof(this.Uninstall), Thread.CurrentThread.ManagedThreadId);
        }
        DispatcherProvider.HookCallbackDispatcher.Invoke(() =>
        {
            if (!IsInstalled)
                return;
            if (!NativeMethods.UnhookWindowsHookEx(_hookHandle))
            {
                int errorCode = Marshal.GetLastWin32Error();
                Log.Error($"Failed to remove hook for '{Process.GetCurrentProcess().ProcessName}'. Errorcode {errorCode}: {new Win32Exception(errorCode).Message}.");
            }
            _hookHandle = IntPtr.Zero;

            _logger.Debug("{h}\tis installed: {i}", _hookType, IsInstalled);
            _logger.Debug("\t {@BaseHook}[{id}]", this, ID);
            dataCollector.LogStats();
        });
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_hookHandle != IntPtr.Zero)
            {
                Uninstall();
                _hookDelegate -= OnHookCall;
                _hookDelegate = null;
            }
            if (_user32LibraryHandle != IntPtr.Zero)
            {

                if (!NativeMethods.FreeLibrary(_user32LibraryHandle))
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    Log.Error($"Failed to unload library 'User32.dll'. Error {errorCode}: {new Win32Exception(errorCode).Message}.");
                }
                _user32LibraryHandle = IntPtr.Zero;
            }
        }
    }

    ~BaseHook()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

