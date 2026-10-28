using System.Collections.Generic;

namespace ProjectName.Windows.Input
{
    public interface IWindowsInputInjector
    {
        IReadOnlyCollection<string> HeldKeys { get; }
        IReadOnlyCollection<string> HeldButtons { get; }

        void InjectPointerMove(double xNorm, double yNorm);
        void InjectPointerButton(string button, string action);
        void InjectKey(string keyCode, string action, IEnumerable<string>? modifiers = null);
        void ReleaseAllHeldInputs();
    }
}
