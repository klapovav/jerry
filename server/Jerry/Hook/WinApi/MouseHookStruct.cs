using System;
using System.Runtime.InteropServices;

namespace Jerry.Hook.WinApi
{
    //https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-msllhookstruct?redirectedfrom=MSDN
    [StructLayout(LayoutKind.Sequential)]
    public struct MouseHookStruct
    {
        /// <summary>
        /// The x- and y-coordinates of the cursor, in per-monitor-aware screen coordinates.
        /// </summary>
        public NativePoint pt;

        public uint mouseData;

        [Descriptor(typeof(MouseFlags))]
        public uint flags;

        public uint time;
        public IntPtr dwExtraInfo;

        public override string ToString()
        {
            return String.Format("mouseData 0x{0:X4}, extra 0x{1:X4}, flags: 0x{2:X4}, time: {3}, pt {4}x{5},",
                mouseData, dwExtraInfo, flags,//binary Convert.ToString((int)flags, 2).PadLeft(32, '0'),
                time, pt.x, pt.y);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NativePoint
    {
        public int x;
        public int y;
    }

    /// <summary>
    /// The event-injected flags. An application can use the following values to test the flags. Testing LLMHF_INJECTED (bit 0) will tell you whether the event was injected. If it was, then testing LLMHF_LOWER_IL_INJECTED (bit 1) will tell you whether or not the event was injected from a process running at lower integrity level.
    /// </summary>
    [Flags]
    public enum MouseFlags : int
    {
        NOT_INJECTED = 0,

        /// <summary>
        /// Test the event-injected (from any process) flag.
        /// </summary>
        INJECTED = 1 << 0,

        /// <summary>
        /// Test the event-injected (from a process running at lower integrity level) flag.
        /// </summary>
        LOWER_IL_INJECTED = 1 << 1,
    }
}