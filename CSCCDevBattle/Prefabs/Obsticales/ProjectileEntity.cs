using Microsoft.Xna.Framework;

public sealed class ProjectileEntity : Entity
{
    public ProjectileEntity()
    {
        // This asset exists in ContentBuilder/Assets/Textures/Battle/Projectiles.
        Texture = Assets.GetTexture("Battle/Projectiles/ProjectileBall");
        CollisionMode = CollisionMode.PixelPerfect;
        CollisionLayer = "obstacles";
    }
}
