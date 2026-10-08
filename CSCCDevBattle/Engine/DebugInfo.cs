using System;

public sealed class DebugInfo : DebugLine
{
    public Func<string?> GetState { get; }

    public DebugInfo(
        string description,
        Func<string?> getState)
        : base(description)
    {
        GetState = getState;
    }
}