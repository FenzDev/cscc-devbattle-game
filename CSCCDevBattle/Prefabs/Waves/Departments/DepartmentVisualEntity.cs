using Microsoft.Xna.Framework;

/// <summary>Procedural rectangle/dot rendered from the game's existing 1x1 Pixel texture.</summary>
public sealed class DepartmentVisualEntity : Entity
{
    public DepartmentVisualEntity()
    {
        Texture = Game1.Singleton.Pixel;
        CollisionMode = CollisionMode.None;
        Scale = Vector2.One;
    }
}
