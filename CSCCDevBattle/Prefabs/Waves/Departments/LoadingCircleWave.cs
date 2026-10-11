using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>Large loading rings drift linearly inward while shrinking and fading, giving the player a clear escape window.</summary>
public sealed class LoadingCircleWave : DepartmentWaveBase
{
    private sealed class RingState
    {
        public DepartmentVisualEntity Entity = null!;
        public Vector2 Start;
        public float Age;
        public float Lifetime;
        public float Spin;
        public float MaxScale;
    }

    private readonly List<RingState> _rings = [];
    private float _spawnTimer;
    private int _ringCount;
    private Texture2D _circleTexture = null!;
    private float _maxArenaScale;

    public LoadingCircleWave() => Duration = 13.5f;
    public override Vector2 ArenaSize => new(280f, 245f);

    protected override void OnStart(BattleContext battle)
    {
        _rings.Clear();
        _spawnTimer = 0.65f;
        _ringCount = 0;
        _circleTexture = Assets.GetTexture("Battle/Projectiles/Web/LoadingCircleCut");
        _maxArenaScale = MaxArenaScale(battle, _circleTexture.Width, _circleTexture.Height);
        DebugState = "Large rings spawn with a warning, then spin, shrink and drift to center";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        _spawnTimer -= deltaTime;
        if (_spawnTimer <= 0f)
        {
            _spawnTimer += 1.8f + Random.Shared.NextSingle() * 0.35f;
            SpawnRing(battle);
        }

        foreach (RingState ring in _rings.ToArray())
        {
            ring.Age += deltaTime;
            float progress = MathHelper.Clamp(ring.Age / ring.Lifetime, 0f, 1f);

            // Position follows a straight, constant-rate path from the spawn point to arena center.
            ring.Entity.Position = Vector2.Lerp(ring.Start, battle.Arena.Center, progress);
            ring.Entity.Rotation += ring.Spin * deltaTime;
            float scale = MathHelper.Lerp(ring.MaxScale, 0.45f, progress);
            ring.Entity.Scale = new Vector2(scale);

            float fadeProgress = MathHelper.Clamp((progress - 0.52f) / 0.48f, 0f, 1f);
            // Give the player a short, readable reaction window before the large ring can hurt;
            // stop damage once the final fade has made it effectively invisible.
            ring.Entity.CollisionMode = ring.Age < 0.48f || fadeProgress >= 0.98f
                ? CollisionMode.None
                : CollisionMode.PixelPerfect;
            ring.Entity.CollisionLayer = "obstacles";

            byte alpha = (byte)Math.Clamp((int)MathF.Round(255f * (1f - fadeProgress)), 0, 255);
            ring.Entity.Tint = new Color((byte)255, (byte)255, (byte)255, alpha);

            if (progress >= 1f)
            {
                ring.Entity.DestroyRequested = true;
                _rings.Remove(ring);
            }
        }

        DebugState = $"Loading rings={_ringCount}; active={_rings.Count}; inward travel={_rings.FirstOrDefault()?.Lifetime ?? 0f:0.0}s";
    }

    private void SpawnRing(BattleContext battle)
    {
        Vector2 center = battle.Arena.Center;
        float angle = Random.Shared.NextSingle() * MathHelper.TwoPi;
        float radius = 20f + Random.Shared.NextSingle() * 28f;
        Vector2 start = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
        float spin = (Random.Shared.NextSingle() < 0.5f ? -1f : 1f)
            * (0.42f + Random.Shared.NextSingle() * 0.55f);

        DepartmentVisualEntity entity = Sprite(battle, start,
            "Battle/Projectiles/Web/LoadingCircleCut", _maxArenaScale, Color.White,
            damaging: true, pixelPerfect: true);
        entity.CollisionSize = new Vector2(_circleTexture.Width, _circleTexture.Height);
        _rings.Add(new RingState
        {
            Entity = entity,
            Start = start,
            Age = 0f,
            Lifetime = 2.7f + Random.Shared.NextSingle() * 0.35f,
            Spin = spin,
            MaxScale = _maxArenaScale * (0.93f + Random.Shared.NextSingle() * 0.07f)
        });
        entity.Scale = new Vector2(_rings[^1].MaxScale);
        _ringCount++;
    }

    private static float MaxArenaScale(BattleContext battle, int textureWidth, int textureHeight)
    {
        float scaleX = battle.Arena.Size.X / textureWidth;
        float scaleY = battle.Arena.Size.Y / textureHeight;
        return MathF.Max(scaleX, scaleY) * 0.88f;
    }
}
