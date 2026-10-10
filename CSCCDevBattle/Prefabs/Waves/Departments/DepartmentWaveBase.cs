using System;
using Microsoft.Xna.Framework;

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
            var velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
            Fire(battle, origin, velocity, tint, scale);
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
}
