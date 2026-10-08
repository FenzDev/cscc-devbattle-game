using Microsoft.Xna.Framework.Input;
using System;

public enum KeyTrigger
{
  Pressed,
  Down,
  Released
}

public sealed class DebugCommand : DebugLine
{
    public Keys Key { get; }

    public KeyTrigger Trigger { get; }

    public Action Action { get; }

    public Func<string?> GetState { get; }

    public DebugCommand(
        Keys key,
        string description,
        Action action,
        Func<string?> getState,
        KeyTrigger trigger)
        : base(description)
    {
        Key = key;
        Action = action
            ?? throw new ArgumentNullException(nameof(action));

        GetState = getState
            ?? throw new ArgumentNullException(nameof(getState));

        Trigger = trigger;
    }
}