using System;
using Microsoft.Xna.Framework;

/// <summary>Map markers telegraph sweeping map-route lasers before firing them.</summary>
public sealed class LocationLaserWave : DepartmentWaveBase
{
    private DepartmentVisualEntity[] _markers = [];
    private float _timer;
    private int _pattern;

    public LocationLaserWave() => Duration = 10f;
    public override Vector2 ArenaSize => new(270f, 240f);

    protected override void OnStart(BattleContext battle)
    {
        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;
        Vector2 c = battle.Arena.Center;
        _markers = new DepartmentVisualEntity[4];
        Vector2[] positions =
        {
            new(min.X + 14f, c.Y), new(max.X - 14f, c.Y),
            new(c.X, min.Y + 14f), new(c.X, max.Y - 14f)
        };
        for (int i = 0; i < _markers.Length; i++)
            _markers[i] = Sprite(battle, positions[i],
                "Battle/Projectiles/Mobile/ProjectileMapsArrow", 1.7f,
                new Color(100, 225, 255));

        _timer = 0.45f;
        _pattern = 0;
        DebugState = "Map markers triangulating; laser lanes are telegraphed";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        _timer -= deltaTime;
        if (_timer > 0f)
            return;

        _timer += _pattern % 3 == 2 ? 1.35f : 1.05f;
        Vector2 c = battle.Arena.Center;
        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;
        Color warning = new(80, 210, 255, 220);
        Color active = new(245, 255, 255);

        switch (_pattern % 4)
        {
            case 0:
            {
                float lane = min.Y + 32f + Random.Shared.NextSingle() * (max.Y - min.Y - 64f);
                TelegraphBeam(battle, new Vector2(c.X, lane),
                    new Vector2(battle.Arena.Size.X - 28f, 4f), 0f, warning, active, 0.72f, 0.43f);
                _markers[0].Position = new Vector2(min.X + 12f, lane);
                _markers[1].Position = new Vector2(max.X - 12f, lane);
                _markers[0].Rotation = 0f;
                _markers[1].Rotation = MathHelper.Pi;
                break;
            }
            case 1:
            {
                float lane = min.X + 32f + Random.Shared.NextSingle() * (max.X - min.X - 64f);
                TelegraphBeam(battle, new Vector2(lane, c.Y),
                    new Vector2(4f, battle.Arena.Size.Y - 28f), 0f,
                    warning, active, 0.78f, 0.40f);
                _markers[2].Position = new Vector2(lane, min.Y + 12f);
                _markers[3].Position = new Vector2(lane, max.Y - 12f);
                _markers[2].Rotation = MathHelper.PiOver2;
                _markers[3].Rotation = -MathHelper.PiOver2;
                break;
            }
            case 2:
            {
                float angle = Random.Shared.NextSingle() < 0.5f ? -0.48f : 0.48f;
                TelegraphBeam(battle, c, new Vector2(battle.Arena.Size.X * 1.15f, 4f),
                    angle, new Color(255, 190, 80), new Color(255, 255, 245), 0.86f, 0.36f);
                _markers[0].Position = new Vector2(min.X + 15f, min.Y + 22f);
                _markers[1].Position = new Vector2(max.X - 15f, max.Y - 22f);
                _markers[0].Rotation = angle;
                _markers[1].Rotation = angle + MathHelper.Pi;
                break;
            }
            default:
            {
                // Two crossing lanes leave a moving gap because their warnings are staggered.
                float offset = Random.Shared.NextSingle() * 24f - 12f;
                TelegraphBeam(battle, c + new Vector2(0f, offset),
                    new Vector2(battle.Arena.Size.X - 35f, 3f), 0f,
                    warning, active, 0.88f, 0.34f);
                TelegraphBeam(battle, c + new Vector2(offset, 0f),
                    new Vector2(3f, battle.Arena.Size.Y - 35f), 0f,
                    new Color(100, 255, 190), Color.White, 1.18f, 0.28f);
                break;
            }
        }

        _pattern++;
        DebugState = $"Laser pattern {_pattern}; map-marker warnings are active";
    }
}
