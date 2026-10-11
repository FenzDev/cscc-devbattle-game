using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>Shared helpers for the department encounter's procedural attack waves.</summary>
public abstract class DepartmentWaveBase : BattleWave
{
    public string DebugState { get; protected set; } = "Waiting";

    protected ProjectileEntity Fire(
        BattleContext battle,
        Vector2 position,
        Vector2 velocity,
        Color tint,
        float scale = 2f,
        float lifetime = 4.5f,
        float redirectAt = -1f,
        float redirectRadians = 0f,
        bool damaging = true)
    {
        var projectile = new ProjectileEntity
        {
            Position = position,
            Tint = tint,
            Scale = new Vector2(scale),
            ContactDamage = damaging ? 1f : 0f,
            DestroyOnPlayerContact = damaging,
            CollisionMode = damaging ? CollisionMode.PixelPerfect : CollisionMode.None
        };

        return Spawn(battle, projectile,
            new DepartmentProjectileBehaviour(velocity, lifetime, redirectAt, redirectRadians));
    }

    /// <summary>Spawns a 1x1 Pixel stretched into procedural UI or geometry.</summary>
    protected DepartmentVisualEntity Visual(
        BattleContext battle,
        Vector2 position,
        Vector2 size,
        Color tint,
        float rotation = 0f,
        bool damaging = false,
        float damage = 1f)
    {
        var entity = new DepartmentVisualEntity
        {
            Position = position,
            Scale = size,
            Rotation = rotation,
            Tint = tint,
            CollisionMode = damaging ? CollisionMode.RotatedBox : CollisionMode.None,
            CollisionLayer = damaging ? "obstacles" : null,
            CollisionSize = Vector2.One,
            ContactDamage = damaging ? damage : 0f,
            DestroyOnPlayerContact = false
        };
        return Spawn(battle, entity);
    }

    /// <summary>
    /// Spawns a sprite from the existing ContentBuilder texture tree.
    /// Scale is the sprite multiplier, not a requested world size.
    /// </summary>
    protected DepartmentVisualEntity Sprite(
        BattleContext battle,
        Vector2 position,
        string asset,
        float scale = 2f,
        Color? tint = null,
        bool damaging = false,
        float damage = 1f,
        bool pixelPerfect = false,
        bool destroyOnContact = false)
    {
        Texture2D texture = Assets.GetTexture(asset);
        var entity = new DepartmentVisualEntity
        {
            Texture = texture,
            Position = position,
            Scale = new Vector2(scale),
            Tint = tint ?? Color.White,
            CollisionMode = damaging
                ? (pixelPerfect ? CollisionMode.PixelPerfect : CollisionMode.RotatedBox)
                : CollisionMode.None,
            CollisionLayer = damaging ? "obstacles" : null,
            CollisionSize = new Vector2(texture.Width, texture.Height),
            ContactDamage = damaging ? damage : 0f,
            DestroyOnPlayerContact = destroyOnContact
        };
        return Spawn(battle, entity);
    }

    protected DepartmentVisualEntity SpriteProjectile(
        BattleContext battle,
        Vector2 position,
        string asset,
        Vector2 velocity,
        float scale = 2f,
        Color? tint = null,
        float lifetime = 4.5f,
        bool damaging = true,
        float redirectAt = -1f,
        float redirectRadians = 0f,
        bool pixelPerfect = false,
        bool destroyOnContact = true)
    {
        DepartmentVisualEntity entity = Sprite(
            battle, position, asset, scale, tint, damaging,
            pixelPerfect: pixelPerfect, destroyOnContact: destroyOnContact);

        // Most pixel-art projectile sprites have oversized transparent margins.
        // Slightly inset the box where texture alpha testing is not requested.
        if (damaging && !pixelPerfect)
        {
            entity.CollisionSize *= 0.72f;
        }

        entity.Behaviours.Add(new DepartmentProjectileBehaviour(
            velocity, lifetime, redirectAt, redirectRadians));
        return entity;
    }

    protected DepartmentVisualEntity TelegraphBeam(
        BattleContext battle,
        Vector2 position,
        Vector2 size,
        float rotation,
        Color warningColor,
        Color activeColor,
        float warningSeconds = 0.7f,
        float activeSeconds = 0.42f)
    {
        var beam = Visual(battle, position, size, warningColor, rotation);
        beam.CollisionSize = Vector2.One;
        beam.Behaviours.Add(new DepartmentTimedBeamBehaviour(
            warningSeconds, activeSeconds, warningColor, activeColor));
        return beam;
    }

    protected void RadialBurst(
        BattleContext battle,
        Vector2 origin,
        int count,
        float speed,
        float startAngle,
        Color tint,
        float scale = 2f)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + MathHelper.TwoPi * i / count;
            Vector2 velocity = new(MathF.Cos(angle), MathF.Sin(angle));
            Fire(battle, origin, velocity * speed, tint, scale);
        }
    }

    protected static Vector2 Direction(Vector2 from, Vector2 to)
    {
        Vector2 direction = to - from;
        if (direction.LengthSquared() < 0.001f)
            return Vector2.UnitY;
        direction.Normalize();
        return direction;
    }

    protected static Vector2 ClampToArena(BattleContext battle, Vector2 point)
    {
        return new Vector2(
            MathHelper.Clamp(point.X, battle.Arena.InnerMinimum.X + 10f, battle.Arena.InnerMaximum.X - 10f),
            MathHelper.Clamp(point.Y, battle.Arena.InnerMinimum.Y + 10f, battle.Arena.InnerMaximum.Y - 10f));
    }

    protected static Vector2 Rotate(Vector2 value, float radians)
    {
        float c = MathF.Cos(radians);
        float s = MathF.Sin(radians);
        return new Vector2(value.X * c - value.Y * s, value.X * s + value.Y * c);
    }

    protected static int CellIndex(float position, float minimum, float cellSize, int count)
    {
        return Math.Clamp((int)((position - minimum) / cellSize), 0, count - 1);
    }
}
