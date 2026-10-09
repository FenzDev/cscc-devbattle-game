using Microsoft.Xna.Framework;

public sealed class ProjectileEntity : Entity
{
    public ProjectileEntity()
    {
        Texture =
            Assets.GetTexture(
                "Battle/Projectile");

        CollisionMode =
            CollisionMode.PixelPerfect;

        CollisionLayer =
            "obstacles";
    }
}