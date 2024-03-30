using System;
using System.Runtime.InteropServices;

namespace Jerry.SystemQueueModifier.Emulation.WinApi;

/// <summary>
/// References all of the Native Windows API methods for the WindowsInput functionality.
/// </summary>
internal static class NativeMethods
{
    /// <summary>
    /// The GetAsyncKeyState function determines whether a key is up or down at the time the function is called, and whether the key was pressed after a previous call to GetAsyncKeyState. (See: http://msdn.microsoft.com/en-us/library/ms646293(VS.85).aspx)
    /// </summary>
    /// <param name="virtualKeyCode">Specifies one of 256 possible virtual-key codes. For more information, see Virtual Key Codes. Windows NT/2000/XP: You can use left- and right-distinguishing constants to specify certain keys. See the Remarks section for further information.</param>
    /// <returns>
    /// If the function succeeds, the return value specifies whether the key was pressed since the last call to GetAsyncKeyState, and whether the key is currently up or down. If the most significant bit is set, the key is down, and if the least significant bit is set, the key was pressed after the previous call to GetAsyncKeyState. However, you should not rely on this last behavior; for more information, see the Remarks.
    ///
    /// Windows NT/2000/XP: The return value is zero for the following cases:
    /// - The current desktop is not the active desktop
    /// - The foreground thread belongs to another process and the desktop does not allow the hook or the journal record.
    ///
    /// Windows 95/98/Me: The return value is the global asynchronous key state for each virtual key. The system does not check which thread has the keyboard focus.
    ///
    /// Windows 95/98/Me: Windows 95 does not support the left- and right-distinguishing constants. If you call GetAsyncKeyState with these constants, the return value is zero.
    /// </returns>
    /// <remarks>
    /// The GetAsyncKeyState function works with mouse buttons. However, it checks on the state of the physical mouse buttons, not on the logical mouse buttons that the physical buttons are mapped to. For example, the call GetAsyncKeyState(VK_LBUTTON) always returns the state of the left physical mouse button, regardless of whether it is mapped to the left or right logical mouse button. You can determine the system's current mapping of physical mouse buttons to logical mouse buttons by calling
    /// Copy CodeGetSystemMetrics(SM_SWAPBUTTON) which returns TRUE if the mouse buttons have been swapped.
    ///
    /// Although the least significant bit of the return value indicates whether the key has been pressed since the last query, due to the pre-emptive multitasking nature of Windows, another application can call GetAsyncKeyState and receive the "recently pressed" bit instead of your application. The behavior of the least significant bit of the return value is retained strictly for compatibility with 16-bit Windows applications (which are non-preemptive) and should not be relied upon.
    ///
    /// You can use the virtual-key code constants VK_SHIFT, VK_CONTROL, and VK_MENU as values for the vKey parameter. This gives the state of the SHIFT, CTRL, or ALT keys without distinguishing between left and right.
    ///
    /// Windows NT/2000/XP: You can use the following virtual-key code constants as values for vKey to distinguish between the left and right instances of those keys.
    ///
    /// Code Meaning
    /// VK_LSHIFT Left-shift key.
    /// VK_RSHIFT Right-shift key.
    /// VK_LCONTROL Left-control key.
    /// VK_RCONTROL Right-control key.
    /// VK_LMENU Left-menu key.
    /// VK_RMENU Right-menu key.
    ///
    /// These left- and right-distinguishing constants are only available when you call the GetKeyboardState, SetKeyboardState, GetAsyncKeyState, GetKeyState, and MapVirtualKey functions.
    /// </remarks>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern Int16 GetAsyncKeyState(UInt16 virtualKeyCode);

    /// <summary>
    /// The SendInput function synthesizes keystrokes, mouse motions, and button clicks.
    /// </summary>
    /// <param name="numberOfInputs">Number of structures in the Inputs array.</param>
    /// <param name="inputs">Pointer to an array of INPUT structures. Each structure represents an event to be inserted into the keyboard or mouse input stream.</param>
    /// <param name="sizeOfInputStructure">Specifies the size, in bytes, of an INPUT structure. If cbSize is not the size of an INPUT structure, the function fails.</param>
    /// <returns>The function returns the number of events that it successfully inserted into the keyboard or mouse input stream. If the function returns zero, the input was already blocked by another thread. To get extended error information, call GetLastError.Microsoft Windows Vista. This function fails when it is blocked by User Interface Privilege Isolation (UIPI). Note that neither GetLastError nor the return value will indicate the failure was caused by UIPI blocking.</returns>
    /// <remarks>
    /// Microsoft Windows Vista. This function is subject to UIPI. Applications are permitted to inject input only into applications that are at an equal or lesser integrity level.
    /// The SendInput function inserts the events in the INPUT structures serially into the keyboard or mouse input stream. These events are not interspersed with other keyboard or mouse input events inserted either by the user (with the keyboard or mouse) or by calls to keybd_event, mouse_event, or other calls to SendInput.
    /// This function does not reset the keyboard's current state. Any keys that are already pressed when the function is called might interfere with the events that this function generates. To avoid this problem, check the keyboard's state with the GetAsyncKeyState function and correct as necessary.
    /// </remarks>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern UInt32 SendInput(UInt32 numberOfInputs, INPUT[] inputs, Int32 sizeOfInputStructure);

    /// <summary>
    /// The GetMessageExtraInfo function retrieves the extra message information for the current thread. Extra message information is an application- or driver-defined value associated with the current thread's message queue.
    /// </summary>
    /// <returns></returns>
    /// <remarks>To set a thread's extra message information, use the SetMessageExtraInfo function. </remarks>
    [DllImport("user32.dll")]
    public static extern IntPtr GetMessageExtraInfo();

    /// <summary>
    /// Used to find the keyboard input scan code for single key input. Some applications do not receive the input when scan is not set.
    /// </summary>
    /// <param name="uCode"></param>
    /// <param name="uMapType"></param>
    /// <returns></returns>
    [DllImport("user32.dll")]
    public static extern UInt32 MapVirtualKey(UInt32 uCode, UInt32 uMapType);
}