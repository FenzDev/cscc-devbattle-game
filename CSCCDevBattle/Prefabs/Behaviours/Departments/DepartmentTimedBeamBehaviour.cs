using System;
using Microsoft.Xna.Framework;

/// <summary>Telegraphs a beam, enables its collision briefly, then removes it.</summary>
public sealed class DepartmentTimedBeamBehaviour : Behaviour
{
    private readonly float _warningDuration;
    private readonly float _activeDuration;
    private readonly Color _warningColor;
    private readonly Color _activeColor;
    private float _elapsed;

    public DepartmentTimedBeamBehaviour(
        float warningDuration,
        float activeDuration,
        Color warningColor,
        Color activeColor)
    {
        _warningDuration = warningDuration;
        _activeDuration = activeDuration;
        _warningColor = warningColor;
        _activeColor = activeColor;
    }

    public override void Update(Entity entity, BattleContext battle, float deltaTime)
    {
        _elapsed += deltaTime;
        if (_elapsed < _warningDuration)
        {
            entity.CollisionMode = CollisionMode.None;
            entity.Tint = _warningColor * (0.45f + 0.25f * MathF.Sin(_elapsed * 18f));
            return;
        }

        if (_elapsed < _warningDuration + _activeDuration)
        {
            entity.CollisionMode = CollisionMode.RotatedBox;
            entity.CollisionLayer = "obstacles";
            entity.ContactDamage = 1f;
            entity.Tint = _activeColor;
            return;
        }

        entity.CollisionMode = CollisionMode.None;
        entity.DestroyRequested = true;
    }
}
