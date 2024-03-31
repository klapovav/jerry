using Jerry.Hook.SysGlobalState;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Jerry.SystemQueueModifier;

public enum Reliability
{
    Unreliable = 0,
    Trustworthy = 1,
}

internal class KeyboardSyncSupervisor
{
    private readonly DateTime creationTime;
    private readonly TimeSpan delay = TimeSpan.FromMilliseconds(100);
    private readonly uint keyFirst;
    private readonly uint keyLast;
    private Reliability state;

    private string prevLogContent;

    private List<uint> previouslyPressed;

    public bool Trustworthy
    {
        get
        {
            Update();
            return state == Reliability.Trustworthy;
        }
    }

    private void Update()
    {
        if (state == Reliability.Unreliable
               && (DateTime.Now - creationTime) >= delay)
        {
            state = Reliability.Trustworthy;
        }
    }

    public KeyboardSyncSupervisor(bool mouse = false)
    {
        keyFirst = (uint)(mouse ? 1 : 7);
        keyLast = (uint)(mouse ? 6 : 254);

        creationTime = DateTime.Now;
        state = Reliability.Unreliable;
        previouslyPressed = new List<uint>();
    }

    public static bool KeyIsVirtuallyDown(uint vk_code)
    {
        return Keyboard.GetKeyState((Keys)vk_code).Down;
    }

    public void ExpectMsgInSystemQueue()
    {
        throw new NotImplementedException();
    }

    public void On()
    {
        throw new NotImplementedException();
    }

    public bool AllKeysAreReleased()
    {
        //Querying the state of the keyboard is still not reliable
        if (!Trustworthy)
        {
            return false;
        }

        previouslyPressed = previouslyPressed.Where(key => KeyIsVirtuallyDown(key)).ToList();
        if (previouslyPressed.Count == 0)
        {
            for (uint i = keyFirst; i <= keyLast; i++)
            {
                if (KeyIsVirtuallyDown(i))
                {
                    previouslyPressed.Add(i);
                }
            }
            if (previouslyPressed.Count == 0)
            {
                Log.Debug("Virtual keys {a}-{b} are released", keyFirst, keyLast);
                return true;
            }
        }
        var pressedKeys = string.Join(" + ", previouslyPressed.Select(k => (Keys)k));
        if (pressedKeys != prevLogContent)
        {
            Log.Debug("Pressed virtual keys: {i}", pressedKeys);
            prevLogContent = pressedKeys;
        }
        return false;
    }
}