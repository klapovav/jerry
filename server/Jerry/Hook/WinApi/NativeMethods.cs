using System;
using System.Runtime.InteropServices;

namespace Jerry.Hook.WinApi;

internal static class NativeMethods
{
    internal static ushort HIWORD(IntPtr dwValue)
    {
        return (ushort)((((long)dwValue) >> 0x10) & 0xffff);
    }

    internal static ushort HIWORD(uint dwValue)
    {
        return (ushort)(dwValue >> 0x10);
    }

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetWindowsHookEx(int idHook, HookProc callback, IntPtr hInstance, uint dwThreadId);

    [DllImport("user32.dll")]
    internal static extern bool UnhookWindowsHookEx(IntPtr hInstance);

    [DllImport("user32.dll")]
    internal static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, int wParam, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern bool FreeLibrary(IntPtr hModule);

    public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
}