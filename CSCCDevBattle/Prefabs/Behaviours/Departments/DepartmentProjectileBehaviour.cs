using System;
using Microsoft.Xna.Framework;

/// <summary>Per-projectile movement, optional one-time redirect, and cleanup.</summary>
public sealed class DepartmentProjectileBehaviour : Behaviour
{
    private Vector2 _velocity;
    private readonly float _lifetime;
    private readonly float _redirectAt;
    private readonly float _redirectRadians;
    private float _elapsed;
    private bool _redirected;

    public DepartmentProjectileBehaviour(
        Vector2 velocity,
        float lifetime = 4.5f,
        float redirectAt = -1f,
        float redirectRadians = 0f)
    {
        _velocity = velocity;
        _lifetime = lifetime;
        _redirectAt = redirectAt;
        _redirectRadians = redirectRadians;
    }

    public override void Update(Entity entity, BattleContext battle, float deltaTime)
    {
        _elapsed += deltaTime;

        if (!_redirected && _redirectAt >= 0f && _elapsed >= _redirectAt)
        {
            float c = MathF.Cos(_redirectRadians);
            float s = MathF.Sin(_redirectRadians);
            _velocity = new Vector2(
                _velocity.X * c - _velocity.Y * s,
                _velocity.X * s + _velocity.Y * c);
            _redirected = true;
        }

        entity.Position += _velocity * deltaTime;

        Vector2 min = battle.Arena.Minimum - new Vector2(72f);
        Vector2 max = battle.Arena.Maximum + new Vector2(72f);
        if (_elapsed >= _lifetime ||
            entity.Position.X < min.X || entity.Position.X > max.X ||
            entity.Position.Y < min.Y || entity.Position.Y > max.Y)
        {
            entity.DestroyRequested = true;
        }
    }
}
