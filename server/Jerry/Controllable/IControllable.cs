using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Jerry.Controllable;
public interface IControllable
{
    /// <summary>
    /// Method for changing the mouse movement mode between absolute and relative.
    /// </summary>
    void ToogleMouseMode();
    void OnMouseMove(int dx, int dy);

    void OnMouseClick(Events.MouseButton ev);

    void OnMouseWheel(Events.MouseWheel ev);

    void OnKeyEvent(Events.KeyboardHookEvent keyEvent);
    void ReleaseModifiers(ModifierKeys modifiers);

    bool OnDeactivate([MaybeNullWhen(false)] out Common.Clipboard clipboard);

    void OnActivate(Common.Clipboard? clipboard);

    /// <summary>
    /// Send a heartbeat message to check the connection status.
    /// </summary>
    /// <returns>
    /// True if the computer is able to receive messages; otherwise, false.
    /// </returns>
    bool TrySendHeartbeat();
}
