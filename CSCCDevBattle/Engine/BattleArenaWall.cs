using Microsoft.Xna.Framework;

public sealed class ArenaWallEntity : Entity
{
    public ArenaWallEntity()
    {
        // No texture: the arena wall is invisible but still collidable.
        Texture = null;
        Visible = false;
        Rotation = 0f;
        Scale = Vector2.One;
        CollisionMode = CollisionMode.RotatedBox;
        CollisionLayer = "obstacles";
    }
}
