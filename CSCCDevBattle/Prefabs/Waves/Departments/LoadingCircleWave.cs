
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// Oversized loading rings spawn at the arena center.
/// They stay in place, rotate, shrink, and fade out.
/// </summary>
public sealed class LoadingCircleWave : DepartmentWaveBase
{
    private sealed class RingState
    {
        public DepartmentVisualEntity Entity = null!;
        public Vector2 Center;
        public float Age;
        public float Lifetime;
        public float Spin;
        public float StartScale;
        public float EndScale;
    }

    private readonly List<RingState> _rings = [];

    private float _spawnTimer;
    private int _ringCount;
    private Texture2D _circleTexture = null!;
    private float _maxArenaScale;

    public LoadingCircleWave() => Duration = 14f;

    public override Vector2 ArenaSize => new(280f, 245f);

    protected override void OnStart(BattleContext battle)
    {
        _rings.Clear();
        _spawnTimer = 0.2f;
        _ringCount = 0;

        _circleTexture = Assets.GetTexture(
            "Battle/Projectiles/Web/LoadingCircleCut");

        _maxArenaScale = MaxArenaScale(
            battle,
            _circleTexture.Width,
            _circleTexture.Height);

        DebugState = "Rings rotate and shrink at arena center";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        _spawnTimer -= deltaTime;

        if (_spawnTimer <= 0f)
        {
            _spawnTimer += 1f + Random.Shared.NextSingle() * 0.2f;
            SpawnRing(battle);
        }

        foreach (RingState ring in _rings.ToArray())
        {
            ring.Age += deltaTime;

            float progress = MathHelper.Clamp(
                ring.Age / ring.Lifetime,
                0f,
                2.5f);

            // Fixed position: never move the ring away from its spawn point.
            ring.Entity.Position = ring.Center;

            // Faster rotation.
            ring.Entity.Rotation += ring.Spin * deltaTime;

            // Shrink smoothly while remaining large enough to threaten the player.
            float scale = MathHelper.Lerp(
                ring.StartScale,
                ring.EndScale,
                progress);

            ring.Entity.Scale = new Vector2(scale);

            // Fade only near the end.
            float fadeProgress = MathHelper.Clamp(
                (progress - 0.82f) / 0.18f,
                0f,
                1f);

            // Brief warning period, then collision remains active
            // until the ring is nearly invisible.
            ring.Entity.CollisionMode =
                ring.Age < 0.3f || fadeProgress >= 0.98f
                    ? CollisionMode.None
                    : CollisionMode.PixelPerfect;

            ring.Entity.CollisionLayer = "obstacles";

            byte alpha = (byte)Math.Clamp(
                (int)MathF.Round(255f * (1f - fadeProgress)),
                0,
                255);

            ring.Entity.Tint = new Color(
                (byte)255,
                (byte)255,
                (byte)255,
                alpha);

            if (progress >= 1f)
            {
                ring.Entity.DestroyRequested = true;
                _rings.Remove(ring);
            }
        }

        DebugState =
            $"Rings spawned={_ringCount}; active={_rings.Count}";
    }

    private void SpawnRing(BattleContext battle)
    {
        // All rings remain centered here for their entire lifetime.
        Vector2 center = battle.Arena.Center;

        float spin = (Random.Shared.NextSingle() < 0.5f ? -1f : 1f)
            * (0.9f + Random.Shared.NextSingle() * 0.65f);

        // Begin larger than the arena.
        float startScale = _maxArenaScale
            * (1.45f + Random.Shared.NextSingle() * 0.25f);

        // Shrink, but don't collapse into a tiny harmless ring.
        float endScale = _maxArenaScale
            * (0.15f + Random.Shared.NextSingle() * 0.15f);

        DepartmentVisualEntity entity = Sprite(
            battle,
            center,
            "Battle/Projectiles/Web/LoadingCircleCut",
            startScale,
            Color.White,
            damaging: true,
            pixelPerfect: true);

        entity.CollisionSize = new Vector2(
            _circleTexture.Width,
            _circleTexture.Height);

        entity.CollisionLayer = "obstacles";
        entity.CollisionMode = CollisionMode.None;

        _rings.Add(new RingState
        {
            Entity = entity,
            Center = center,
            Age = 0f,
            Lifetime = 3.0f + Random.Shared.NextSingle() * 0.3f,
            Spin = spin,
            StartScale = startScale,
            EndScale = endScale
        });

        entity.Scale = new Vector2(startScale);
        _ringCount++;
    }

    private static float MaxArenaScale(
        BattleContext battle,
        int textureWidth,
        int textureHeight)
    {
        float scaleX = battle.Arena.Size.X / textureWidth;
        float scaleY = battle.Arena.Size.Y / textureHeight;

        return MathF.Max(scaleX, scaleY) * 0.88f;
    }
}
