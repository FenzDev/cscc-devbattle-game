using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

/// <summary>A single map pin appears at a random point, aims at the player, and fires one fast, telegraphed laser.</summary>
public sealed class LocationLaserWave : DepartmentWaveBase
{
    private sealed class ArrowState
    {
        public DepartmentVisualEntity Entity = null!;
        public float Age;
    }

    private readonly List<ArrowState> _arrows = [];
    private float _timer;
    private int _shots;

    public LocationLaserWave() => Duration = 13f;
    public override Vector2 ArenaSize => new(280f, 245f);

    protected override void OnStart(BattleContext battle)
    {
        _arrows.Clear();
        _timer = 0.35f;
        _shots = 0;
        DebugState = "Location pin appears at a random point and telegraphs a laser directly toward the player";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        _timer -= deltaTime;
        if (_timer <= 0f)
        {
            _timer += 0.88f + Random.Shared.NextSingle() * 0.22f;
            FireOneLaser(battle);
        }

        foreach (ArrowState arrow in _arrows.ToArray())
        {
            arrow.Age += deltaTime;
            if (arrow.Age >= 0.82f)
            {
                arrow.Entity.DestroyRequested = true;
                _arrows.Remove(arrow);
            }
        }
        DebugState = $"Location lasers fired={_shots}; active pin telegraphs={_arrows.Count}";
    }

    private void FireOneLaser(BattleContext battle)
    {
        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;
        Vector2 start = new(
            min.X + 20f + Random.Shared.NextSingle() * (max.X - min.X - 40f),
            min.Y + 20f + Random.Shared.NextSingle() * (max.Y - min.Y - 40f));
        Vector2 direction = Direction(start, battle.Player.Position);
        float angle = MathF.Atan2(direction.Y, direction.X);
        float length = MathHelper.Clamp(Vector2.Distance(start, battle.Player.Position) + 35f,
            42f, MathF.Sqrt(battle.Arena.Size.LengthSquared()) + 24f);
        Vector2 beamCenter = start + direction  * (length * 0.55f);

        DepartmentVisualEntity arrow = Sprite(battle, start,
            "Battle/Projectiles/Mobile/ProjectileMapsArrow", 3.4f, Color.White);
        // Source map-arrow points down. Rotate its down-vector to the firing direction.
        arrow.Rotation = angle - MathHelper.PiOver2;
        _arrows.Add(new ArrowState { Entity = arrow, Age = 0f });

        TelegraphBeam(battle, beamCenter, new Vector2(length, 5f), angle,
            new Color(165, 165, 165, 205), Color.White,
            warningSeconds: 0.32f, activeSeconds: 0.30f);
        _shots++;
    }
}
