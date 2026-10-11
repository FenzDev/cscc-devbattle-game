using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

/// <summary>A green/ red computer-vision frame reacts to movement while momentum-driven missiles and monochrome obstacles crowd the arena.</summary>
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
    private float _obstacleTimer;
    private int _launched;
    private int _obstaclesLaunched;

    public VisionPursuitWave() => Duration = 14f;
    public override Vector2 ArenaSize => new(280f, 245f);

    protected override void OnStart(BattleContext battle)
    {
        _rockets.Clear();
        _lastPlayerPosition = battle.Player.Position;
        _playerMoving = false;
        Vector2 player = battle.Player.Position;
        // A one-pixel-inset vision box compared with the original frame.
        const float width = 56f;
        const float height = 42f;
        _visionFrame[0] = Visual(battle, player + new Vector2(0f, -height / 2f), new Vector2(width, 2f), new Color(100, 190, 100));
        _visionFrame[1] = Visual(battle, player + new Vector2(0f, height / 2f), new Vector2(width, 2f), new Color(100, 190, 100));
        _visionFrame[2] = Visual(battle, player + new Vector2(-width / 2f, 0f), new Vector2(2f, height), new Color(100, 190, 100));
        _visionFrame[3] = Visual(battle, player + new Vector2(width / 2f, 0f), new Vector2(2f, height), new Color(100, 190, 100));
        _spawnTimer = 0.55f;
        _obstacleTimer = 1.25f;
        _launched = 0;
        _obstaclesLaunched = 0;
        DebugState = "Vision frame green at rest, red on movement; left-facing rockets accelerate and coast down, plus random obstacles";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        Vector2 player = battle.Player.Position;
        Vector2 playerDelta = player - _lastPlayerPosition;
        _playerMoving = deltaTime > 0f && playerDelta.LengthSquared() > 1.5f;
        _lastPlayerPosition = player;

        Color frameColor = _playerMoving ? new Color(235, 70, 70) : new Color(105, 205, 105);
        const float width = 56f;
        const float height = 42f;
        _visionFrame[0].Position = player + new Vector2(0f, -height / 2f);
        _visionFrame[1].Position = player + new Vector2(0f, height / 2f);
        _visionFrame[2].Position = player + new Vector2(-width / 2f, 0f);
        _visionFrame[3].Position = player + new Vector2(width / 2f, 0f);
        foreach (DepartmentVisualEntity line in _visionFrame)
            line.Tint = frameColor;

        _spawnTimer -= deltaTime;
        if (_spawnTimer <= 0f)
        {
            _spawnTimer += 1.28f;
            SpawnRocket(battle);
        }
        _obstacleTimer -= deltaTime;
        if (_obstacleTimer <= 0f)
        {
            _obstacleTimer += 1.55f + Random.Shared.NextSingle() * 0.4f;
            SpawnObstacle(battle);
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
                rocket.Velocity += desired * 76f * deltaTime;
                Vector2 preferred = desired * 162f;
                rocket.Velocity = Vector2.Lerp(rocket.Velocity, preferred,
                    MathHelper.Clamp(deltaTime * 1.05f, 0f, 1f));
                float speed = rocket.Velocity.Length();
                if (speed > 178f)
                    rocket.Velocity = rocket.Velocity / speed * 178f;
            }
            else
            {
                // Momentum decays rather than stopping instantly, leaving a readable coast-down window.
                rocket.Velocity *= MathF.Exp(-0.78f * deltaTime);
            }

            rocket.Entity.Position += rocket.Velocity * deltaTime;
            // Current missile art orientation is corrected by using its forward axis directly.
            if (rocket.Velocity.LengthSquared() > 4f)
                rocket.Entity.Rotation = MathF.Atan2(rocket.Velocity.Y, rocket.Velocity.X);

            rocket.ParticleTimer -= deltaTime;
            if (rocket.ParticleTimer <= 0f)
            {
                rocket.ParticleTimer += 0.105f;
                SpriteProjectile(battle, rocket.Entity.Position - rocket.Velocity * 0.045f,
                    "Battle/Projectiles/Particle", -rocket.Velocity * 0.12f,
                    1.05f, Color.White, lifetime: 0.34f,
                    damaging: false, destroyOnContact: false);
            }
        }

        DebugState = $"Vision={(_playerMoving ? "RED / moving" : "GREEN / still")}; rockets={_rockets.Count}; obstacles={_obstaclesLaunched}";
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
        DepartmentVisualEntity missile = Sprite(battle, start,
            "Battle/Projectiles/AI/Missile", 2.2f, Color.White, damaging: true);
        missile.CollisionSize *= 0.68f;
        missile.Rotation = MathF.Atan2(direction.Y, direction.X);
        _rockets.Add(new RocketState
        {
            Entity = missile,
            Velocity = direction * 52f,
            Age = 0f,
            ParticleTimer = 0.02f
        });
        _launched++;
    }

    private void SpawnObstacle(BattleContext battle)
    {
        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;
        int edge = Random.Shared.Next(4);
        Vector2 start = edge switch
        {
            0 => new Vector2(min.X - 10f, min.Y + Random.Shared.NextSingle() * (max.Y - min.Y)),
            1 => new Vector2(max.X + 10f, min.Y + Random.Shared.NextSingle() * (max.Y - min.Y)),
            2 => new Vector2(min.X + Random.Shared.NextSingle() * (max.X - min.X), min.Y - 10f),
            _ => new Vector2(min.X + Random.Shared.NextSingle() * (max.X - min.X), max.Y + 10f)
        };
        Vector2 target = battle.Player.Position + new Vector2(
            Random.Shared.NextSingle() * 30f - 15f,
            Random.Shared.NextSingle() * 30f - 15f);
        Vector2 direction = Rotate(Direction(start, target), Random.Shared.NextSingle() * 0.28f - 0.14f);
        string asset = _obstaclesLaunched % 2 == 0
            ? "Battle/Projectiles/ProjectileBall"
            : "Battle/Projectiles/ProjectileUnroundedBall";
        SpriteProjectile(battle, start, asset,
            direction * (115f + Random.Shared.NextSingle() * 22f),
            1.7f, Color.White, lifetime: 3f,
            damaging: true, pixelPerfect: true);
        _obstaclesLaunched++;
    }

    private static bool OutsideArena(BattleContext battle, Vector2 position, float margin)
    {
        Vector2 min = battle.Arena.Minimum - new Vector2(margin);
        Vector2 max = battle.Arena.Maximum + new Vector2(margin);
        return position.X < min.X || position.X > max.X || position.Y < min.Y || position.Y > max.Y;
    }
}
