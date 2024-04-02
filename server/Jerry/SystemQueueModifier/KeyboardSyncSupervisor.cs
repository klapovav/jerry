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
    private readonly List<byte> virtualKeys;
    private Reliability state;
    private string lastLoggedValue;
    private IEnumerable<byte> previouslyPressed;

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
        virtualKeys = mouse ? mouseButtons : allVirtualKeys.Except(mouseButtons).ToList();
        creationTime = DateTime.Now;
        state = Reliability.Unreliable;
        previouslyPressed = new List<byte>();
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
        var stillPressed = previouslyPressed
            .Where(vk => KeyIsVirtuallyDown(vk));

        previouslyPressed = stillPressed.Any()
            ? stillPressed
            : virtualKeys
                .Where(vk => KeyIsVirtuallyDown(vk));

        Log();
        return !previouslyPressed.Any();
    }
    private void Log()
    {
        var pressedKeys = string.Join(" + ", previouslyPressed.Select(k => (Keys)k));
        if (lastLoggedValue == pressedKeys)
            return;
        lastLoggedValue = pressedKeys;
        if (previouslyPressed.Any())
            Serilog.Log.Debug("Pressed virtual keys: {i}", pressedKeys);
        else
            Serilog.Log.Debug("Virtual keys {a}-{b} are released", virtualKeys.First(), virtualKeys.Last());

    }

}