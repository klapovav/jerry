using Jerry.Extensions;
using Jerry.Hook;
using System.Collections.Generic;

namespace Jerry.SystemQueueModifier
{
    internal abstract class SystemQueueModifierBase<T> where T : struct, System.Enum //: IModifier, ISubscriber
    {
        protected abstract IHook hook { get; }
        private readonly T _none;
        protected T _blockedInput;
        protected T _subscribedInput;

        public T BlockedInputTypes
        {
            get { return _blockedInput; }
            private set
            {
                _blockedInput = value;
            }
        }

        public T SubscribedInput
        {
            get { return _subscribedInput; }
            private set
            {
                _subscribedInput = value;
            }
        }

        public SystemQueueModifierBase(T noneEvent)
        {
            _none = noneEvent;
            _blockedInput = noneEvent;
            _subscribedInput = noneEvent;
        }

        public void UnblockInput(T inputTypes)
        {
            if (inputTypes.Equals(_none))
                return;
            _blockedInput = _blockedInput.Remove(inputTypes);
            if (HookNotNeeded)
                UninstallHook();
        }

        public void BlockInput(T inputTypes)
        {
            if (inputTypes.Equals(_none))
                return;
            _blockedInput = _blockedInput.Add(inputTypes);
            InstallHook();
        }

        public void Subscribe(T inputTypes)
        {
            if (inputTypes.Equals(_none))
                return;
            _subscribedInput = _subscribedInput.Add(inputTypes);
            InstallHook();
        }

        public void Unsubscribe(T inputTypes)
        {
            if (inputTypes.Equals(_none))
                return;
            _subscribedInput = _subscribedInput.Remove(inputTypes);
            if (HookNotNeeded)
                UninstallHook();
        }

        public abstract bool AllKeysAreReleased { get; }

        protected abstract void InstallHook();

        protected virtual void UninstallHook()
        {
            hook.Uninstall();
        }

        private bool HookNotNeeded =>
            _blockedInput.Equals(_none) &&
            _subscribedInput.Equals(_none);

    }
}