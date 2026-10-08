using System.Numerics;

public sealed class ArenaWallEntity : Entity
{
    public ArenaWallEntity()
    {
        // No texture.
        // Therefore the wall is invisible normally.

        Texture = null;
        Visible = false;

        Rotation = 0f;
        Scale = Vector2.One;

        CollisionMode =
            CollisionMode.RotatedBox;

        CollisionLayer =
            "obstacles";
    }
}