using System;
using Microsoft.Xna.Framework.Input;

namespace CSCCDevBattle;

public enum KeyTrigger
{
  Pressed,
  Down,
  Released
}

public sealed class DebugCommand
{
  public Keys Key { get; }
  public string Description { get; }
  public KeyTrigger Trigger { get; }

  public Action Action { get; }
  public Func<string?> GetState { get; }

  public DebugCommand(
      Keys key,
      string description,
      Action action,
      Func<string?> getState,
      KeyTrigger trigger)
  {
    Key = key;
    Description = description;
    Action = action;
    GetState = getState;
    Trigger = trigger;
  }
}