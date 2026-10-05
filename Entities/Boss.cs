using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BattleGame.Entities;

public sealed class Boss
{
    public Vector2 Position;
    public float Radius = 10f;
    public int MaxHealth = 100;
    public int Health = 100;

    public void Reset(Vector2 position)
    {
        Position = position;
        Health = MaxHealth;
    }

    public bool Alive => Health > 0;

    public void Draw(SpriteBatch sb, Texture2D pixel)
    {
        var rect = new Rectangle(
            (int)(Position.X - Radius),
            (int)(Position.Y - Radius),
            (int)(Radius * 2),
            (int)(Radius * 2));

        sb.Draw(pixel, rect, Color.Red);
    }
}
