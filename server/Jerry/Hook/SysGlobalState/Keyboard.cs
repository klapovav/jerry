using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;

namespace Jerry.Hook.SysGlobalState
{
    public class Keyboard
    {
        [DllImport("User32.dll")]
        private static extern short GetAsyncKeyState(Keys key);

        public static KeyState GetKeyState(Keys key)
        {
            var res = GetAsyncKeyState(key);
            return new KeyState(res);
        }

        public static ModifierKeys GetPressedModifiers()
        {
            ModifierKeys response = ModifierKeys.None;
            if (GetKeyState(Keys.ShiftKey).Down) response |= ModifierKeys.Shift;
            if (GetKeyState(Keys.ControlKey).Down) response |= ModifierKeys.Control;
            if (GetKeyState(Keys.Menu).Down) response |= ModifierKeys.Alt;
            if (GetKeyState(Keys.LWin).Down) response |= ModifierKeys.Windows;
            if (GetKeyState(Keys.RWin).Down) response |= ModifierKeys.Windows;
            return response;
        }
    }

    public readonly struct KeyState
    {
        private short StateFlags { get; init; }
        public bool Down => IsBitSet(StateFlags, 15);
        public bool RecentlyPressed => IsBitSet(StateFlags, 0);
 
        public KeyState(short state)
        {
            StateFlags = state;
        }

        private static bool IsBitSet(short n, int pos)
        {
            return (n & 1 << pos) != 0;
        }
    }
}