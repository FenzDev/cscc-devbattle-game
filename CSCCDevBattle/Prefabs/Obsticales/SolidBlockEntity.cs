using Microsoft.Xna.Framework;

public sealed class SolidBlockEntity : Entity
{
    public SolidBlockEntity(
        Vector2 position,
        Vector2 size)
    {
        Position = position;

        Texture = Game1.Singleton.Pixel;
        Visible = true;
        Scale = size;
        Tint = Color.Gray;

        CollisionMode = CollisionMode.RotatedBox;
        CollisionSize = Vector2.One;
        CollisionLayer = "solid";
    }
}