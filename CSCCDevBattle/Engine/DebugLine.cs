using System;

public abstract class DebugLine
{
    public string Description { get; }

    protected DebugLine(string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Description = description;
    }
}