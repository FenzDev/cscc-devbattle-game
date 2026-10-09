using System;
using Microsoft.Xna.Framework.Input;

public static class Input
{
  public static KeyboardState Current { get; private set; }
  public static KeyboardState Previous { get; private set; }

  public static void Update()
  {
    Previous = Current;
    Current = Keyboard.GetState();
  }

  public static bool Pressed(Keys key)
  {
    return Current.IsKeyDown(key)
        && Previous.IsKeyUp(key);
  }
  public static bool Released(Keys key)
  {
    return Current.IsKeyUp(key)
        && Previous.IsKeyDown(key);
  }

  public static bool Down(Keys key)
  {
    return Current.IsKeyDown(key);
  }

  public static bool AnyKeyPressed()
  {
    return Current.GetPressedKeyCount() > 0 && Previous.GetPressedKeyCount() == 0;
  }
}