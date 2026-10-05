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

    public static bool Down(Keys key)
    {
        return Current.IsKeyDown(key);
    }
}