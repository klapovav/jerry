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
    private readonly List<byte> mouseButtons = new() { 1, 2, 4, 5, 6 };
    private readonly IEnumerable<byte> allVirtualKeys = Enumerable.Range(1, 255).Select(i => (byte)i).ToArray();
    private Reliability state;
    private string prevLogContent;
    private IEnumerable<byte> previouslyPressed;
    private List<byte> virtualKeys;

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
        creationTime = DateTime.Now;
        state = Reliability.Unreliable;
        previouslyPressed = new List<byte>();
        virtualKeys = mouse ? mouseButtons : allVirtualKeys.Except(mouseButtons).ToList();
    }

    public static bool KeyIsVirtuallyDown(uint vk_code)
    {
        return Keyboard.GetKeyState((Keys)vk_code).Down;
    }

    public void ExpectMsgInSystemQueue()
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
        if (!previouslyPressed.Any())
        {
            foreach (var key in virtualKeys)
            {
                if (KeyIsVirtuallyDown(key))
                {
                    previouslyPressed = previouslyPressed.Append(key);
                }
            }
            if (!previouslyPressed.Any())
            {
                Log.Debug("Virtual keys {a}-{b} are released", virtualKeys.First(), virtualKeys.Last());
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