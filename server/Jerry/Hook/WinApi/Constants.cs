namespace Jerry.Hook.WinApi
{
    //source https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
    //       https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-mousehwheel?redirectedfrom=MSD
    public enum MouseMessage
    {
        MOUSEMOVE = 0x200,
        MOUSEWHEEL = 0x20a,
        MOUSEHWHEEL = 0x020E,
        LBUTTONDOWN = 0x201,
        LBUTTONUP = 0x202,
        RBUTTONDOWN = 0x204,
        RBUTTONUP = 0x205,
        MBUTTONDOWN = 0x207,
        MBUTTONUP = 0x208,
        XBUTTONDOWN = 0x20B,
        XBUTTONUP = 0x20C,
    }

    internal static class WM
    {
        #region Mouse messages

        public const int WM_MOUSEMOVE = 0x200;

        public const int WM_MOUSEWHEEL = 0x20a;
        public const int WM_MOUSEHWHEEL = 0x020E;  //https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-mousehwheel?redirectedfrom=MSDN

        public const int WM_LBUTTONDOWN = 0x201;
        public const int WM_LBUTTONUP = 0x202;

        public const int WM_RBUTTONDOWN = 0x204;
        public const int WM_RBUTTONUP = 0x205;

        public const int WM_MBUTTONDOWN = 0x207;
        public const int WM_MBUTTONUP = 0x208;

        public const int WM_XBUTTONDOWN = 0x20B;
        public const int WM_XBUTTONUP = 0x20C;

        #endregion Mouse messages

        public const int WM_KEYDOWN = 0x100;
        public const int WM_KEYUP = 0x101;
        public const int WM_SYSKEYDOWN = 0x104;
        public const int WM_SYSKEYUP = 0x105;
        public const int WM_KEYLAST = 0x108;
    }

    internal static class Constants
    {
        public const int WH_KEYBOARD_LL = 13;
        public const int WH_MOUSE_LL = 14;
        public const int CONSUME_INPUT = 1;
        public const int HC_ACTION = 0;
        public const int WM_HOTKEY = 0x0312;
        public const uint WHEEL_UP = 0x00780000;
        public const uint WHEEL_DOWN = 0xff880000;
    }
}