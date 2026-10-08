using Microsoft.Xna.Framework;

public sealed class Wall : Entity
{
    public Wall()
    {
        CollisionMode = CollisionMode.RotatedBox;

        CollisionSize = new Vector2(
            120f,
            20f);
    }
}