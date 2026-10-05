using Microsoft.Xna.Framework.Input;

public class Input
{
    public KeyboardState Current { get; private set; }
    public KeyboardState Previous { get; private set; }

    public void Update()
    {
      Previous = Current;
      Current = Keyboard.GetState();
    }

    public bool Pressed(Keys key)
    {
        return Current.IsKeyDown(key)
            && Previous.IsKeyUp(key);
    }

    public bool Down(Keys key)
    {
        return Current.IsKeyDown(key);
    }
}