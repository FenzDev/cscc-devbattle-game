using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

/// <summary>A moving-player vision frame turns red while accelerating rockets pursue.</summary>
public sealed class VisionPursuitWave : DepartmentWaveBase
{
    private sealed class RocketState
    {
        public DepartmentVisualEntity Entity = null!;
        public Vector2 Velocity;
        public float Age;
        public float ParticleTimer;
    }

    private readonly List<RocketState> _rockets = [];
    private readonly DepartmentVisualEntity[] _visionFrame = new DepartmentVisualEntity[4];
    private Vector2 _lastPlayerPosition;
    private bool _playerMoving;
    private float _spawnTimer;
    private int _launched;

    public VisionPursuitWave() => Duration = 11f;
    public override Vector2 ArenaSize => new(275f, 240f);

    protected override void OnStart(BattleContext battle)
    {
        _rockets.Clear();
        _lastPlayerPosition = battle.Player.Position;
        _playerMoving = false;
        Vector2 player = battle.Player.Position;
        const float width = 58f;
        const float height = 44f;
        _visionFrame[0] = Visual(battle, player + new Vector2(0f, -height / 2f), new Vector2(width, 2f), new Color(70, 255, 120));
        _visionFrame[1] = Visual(battle, player + new Vector2(0f, height / 2f), new Vector2(width, 2f), new Color(70, 255, 120));
        _visionFrame[2] = Visual(battle, player + new Vector2(-width / 2f, 0f), new Vector2(2f, height), new Color(70, 255, 120));
        _visionFrame[3] = Visual(battle, player + new Vector2(width / 2f, 0f), new Vector2(2f, height), new Color(70, 255, 120));
        _spawnTimer = 0.65f;
        _launched = 0;
        DebugState = "Vision box green at rest; movement turns it red and provokes predictive rockets";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        Vector2 player = battle.Player.Position;
        Vector2 playerDelta = player - _lastPlayerPosition;
        _playerMoving = deltaTime > 0f && playerDelta.LengthSquared() > 1.5f;
        _lastPlayerPosition = player;

        Color frameColor = _playerMoving ? new Color(255, 65, 75) : new Color(70, 255, 120);
        const float width = 58f;
        const float height = 44f;
        _visionFrame[0].Position = player + new Vector2(0f, -height / 2f);
        _visionFrame[1].Position = player + new Vector2(0f, height / 2f);
        _visionFrame[2].Position = player + new Vector2(-width / 2f, 0f);
        _visionFrame[3].Position = player + new Vector2(width / 2f, 0f);
        foreach (DepartmentVisualEntity line in _visionFrame)
            line.Tint = frameColor;

        _spawnTimer -= deltaTime;
        if (_spawnTimer <= 0f)
        {
            _spawnTimer += 1.38f;
            SpawnRocket(battle);
        }

        foreach (RocketState rocket in _rockets.ToArray())
        {
            rocket.Age += deltaTime;
            if (rocket.Entity.DestroyRequested || rocket.Age > 6f || OutsideArena(battle, rocket.Entity.Position, 65f))
            {
                rocket.Entity.DestroyRequested = true;
                _rockets.Remove(rocket);
                continue;
            }

            Vector2 toPlayer = battle.Player.Position - rocket.Entity.Position;
            if (_playerMoving && toPlayer.LengthSquared() > 0.01f)
            {
                Vector2 desired = Vector2.Normalize(toPlayer);
                rocket.Velocity += desired * 68f * deltaTime;
                Vector2 preferred = desired * 154f;
                rocket.Velocity = Vector2.Lerp(rocket.Velocity, preferred,
                    MathHelper.Clamp(deltaTime * 0.95f, 0f, 1f));
                float speed = rocket.Velocity.Length();
                if (speed > 170f)
                    rocket.Velocity = rocket.Velocity / speed * 170f;
            }
            else
            {
                // Momentum decays gradually, so a stopped player can still escape the coast-down.
                rocket.Velocity *= MathF.Exp(-0.82f * deltaTime);
            }

            rocket.Entity.Position += rocket.Velocity * deltaTime;
            if (rocket.Velocity.LengthSquared() > 4f)
                rocket.Entity.Rotation = MathF.Atan2(rocket.Velocity.Y, rocket.Velocity.X) + MathHelper.PiOver2;

            rocket.ParticleTimer -= deltaTime;
            if (rocket.ParticleTimer <= 0f)
            {
                rocket.ParticleTimer += 0.095f;
                Vector2 trailVelocity = -rocket.Velocity * 0.16f;
                SpriteProjectile(battle, rocket.Entity.Position - rocket.Velocity * 0.045f,
                    "Battle/Projectiles/Particle", trailVelocity, 1.05f,
                    new Color(255, 130, 100, 190), lifetime: 0.36f,
                    damaging: false, destroyOnContact: false);
            }
        }

        DebugState = $"Vision={(_playerMoving ? "RED / moving" : "GREEN / still")}; rockets={_rockets.Count}; launched={_launched}";
    }

    private void SpawnRocket(BattleContext battle)
    {
        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;
        int edge = Random.Shared.Next(4);
        Vector2 start = edge switch
        {
            0 => new Vector2(min.X + 8f, min.Y + Random.Shared.NextSingle() * (max.Y - min.Y)),
            1 => new Vector2(max.X - 8f, min.Y + Random.Shared.NextSingle() * (max.Y - min.Y)),
            2 => new Vector2(min.X + Random.Shared.NextSingle() * (max.X - min.X), min.Y + 8f),
            _ => new Vector2(min.X + Random.Shared.NextSingle() * (max.X - min.X), max.Y - 8f)
        };
        Vector2 direction = Direction(start, battle.Player.Position);
        var missile = Sprite(battle, start, "Battle/Projectiles/AI/Missile", 1.55f,
            new Color(255, 105, 120), damaging: true);
        missile.CollisionSize *= 0.68f;
        _rockets.Add(new RocketState
        {
            Entity = missile,
            Velocity = direction * 48f,
            Age = 0f,
            ParticleTimer = 0.02f
        });
        _launched++;
    }

    private static bool OutsideArena(BattleContext battle, Vector2 position, float margin)
    {
        Vector2 min = battle.Arena.Minimum - new Vector2(margin);
        Vector2 max = battle.Arena.Maximum + new Vector2(margin);
        return position.X < min.X || position.X > max.X || position.Y < min.Y || position.Y > max.Y;
    }
}
