using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Shared.WinApi;

public static class User32
{
    [DllImport("user32.dll", SetLastError = false)]
    public static extern IntPtr GetMessageExtraInfo();

    [DllImport("user32.dll", SetLastError = false)]
    public static extern IntPtr SetMessageExtraInfo(IntPtr lParam);

    public static class WndProc
    {
        [DllImport(@"user32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern bool GetMessage(ref MSG message, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport(@"user32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern bool TranslateMessage(ref MSG message);

        [DllImport(@"user32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern long DispatchMessage(ref MSG message);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            private long x;
            private long y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            private IntPtr hwnd;
            public uint message;
            private UIntPtr wParam;
            private IntPtr lParam;
            private uint time;
            private POINT pt;
        }
    }

    public static class Cursor
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetPhysicalCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }
    }

    // Requires administrator rights!!
    //   ->  manifest file : <requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool BlockInput([MarshalAs(UnmanagedType.Bool)] bool fBlockIt);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(Keys key);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetKeyboardState(byte[] lpKeyState);

    //[DllImport("user32.dll")]
    //static extern short GetKeyState(VirtualKeyStates nVirtKey);
}