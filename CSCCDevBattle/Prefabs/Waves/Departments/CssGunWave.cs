using System;
using Microsoft.Xna.Framework;

/// <summary>Web: UI turrets track the player and switch from aimed shots to a small spiral fan.</summary>
public sealed class CssGunWave : DepartmentWaveBase
{
    public CssGunWave() { Duration = 10f; }
    public override Vector2 ArenaSize => new(250f, 230f);

    private readonly DepartmentVisualEntity[] _bodies = new DepartmentVisualEntity[4];
    private readonly DepartmentVisualEntity[] _barrels = new DepartmentVisualEntity[4];
    private readonly float[] _timers = { 0.20f, 0.42f, 0.64f, 0.86f };
    private int _shot;

    protected override void OnStart(BattleContext battle)
    {
        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;
        Vector2[] positions =
        {
            new(min.X + 15f, min.Y + 15f), new(max.X - 15f, min.Y + 15f),
            new(min.X + 15f, max.Y - 15f), new(max.X - 15f, max.Y - 15f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            _bodies[i] = Visual(battle, positions[i], new Vector2(15f, 12f),
                new Color(45, 210, 255));
            _barrels[i] = Visual(battle, positions[i] + new Vector2(10f, 0f),
                new Vector2(13f, 4f), new Color(90, 255, 150));
        }
        _shot = 0;
        DebugState = "CSS state=hover; guns tracking; first staggered shot";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        bool spiralState = ((int)(Elapsed / 2.4f) % 2) == 1;
        for (int i = 0; i < _bodies.Length; i++)
        {
            Vector2 origin = _bodies[i].Position;
            Vector2 direction = Direction(origin, battle.Player.Position);
            float angle = MathF.Atan2(direction.Y, direction.X);
            _bodies[i].Rotation = angle;
            _barrels[i].Position = origin + direction * 12f;
            _barrels[i].Rotation = angle;

            _timers[i] -= deltaTime;
            if (_timers[i] > 0f) continue;
            _timers[i] += 0.88f;

            if (!spiralState)
            {
                Fire(battle, origin + direction * 12f, direction * 145f,
                    new Color(65, 220, 255), 1.8f);
            }
            else
            {
                float offset = (i % 2 == 0 ? -0.38f : 0.38f) + 0.12f * (_shot % 3);
                Fire(battle, origin + direction * 12f, Rotate(direction, offset) * 132f,
                    new Color(90, 255, 150), 1.8f);
                Fire(battle, origin + direction * 12f, Rotate(direction, -offset) * 132f,
                    new Color(90, 255, 150), 1.8f);
            }
            _shot++;
        }
        DebugState = $"CSS state={(spiralState ? "@keyframes spiral" : ":hover/aimed")}; shots={_shot}";
    }
}
