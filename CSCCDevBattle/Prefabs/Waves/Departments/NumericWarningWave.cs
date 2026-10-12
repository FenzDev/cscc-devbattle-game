using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>Computer-vision sample textures briefly cover almost the entire arena; opaque pixels become dangerous only on the white flash.</summary>
public sealed class NumericWarningWave : DepartmentWaveBase
{
    private sealed class CvWarning
    {
        public DepartmentVisualEntity Entity = null!;
        public float Age;
        public float WarnFor;
        public float ActiveFor;
        public bool Activated;
    }

    private static readonly string[] CvAssets =
    [
        "Battle/Projectiles/AI/ProjectileCVOne11", "Battle/Projectiles/AI/ProjectileCVOne12",
        "Battle/Projectiles/AI/ProjectileCVOne21", "Battle/Projectiles/AI/ProjectileCVOne22",
        "Battle/Projectiles/AI/ProjectileCVOne31", "Battle/Projectiles/AI/ProjectileCVOne32",
        "Battle/Projectiles/AI/ProjectileCVOne41", "Battle/Projectiles/AI/ProjectileCVOne42",
        "Battle/Projectiles/AI/ProjectileCVOne51", "Battle/Projectiles/AI/ProjectileCVOne52",
        "Battle/Projectiles/AI/ProjectileCVOne61", "Battle/Projectiles/AI/ProjectileCVOne62",
        "Battle/Projectiles/AI/ProjectileCVOne71", "Battle/Projectiles/AI/ProjectileCVOne72",
        "Battle/Projectiles/AI/ProjectileCVOne81", "Battle/Projectiles/AI/ProjectileCVOne82",
        "Battle/Projectiles/AI/ProjectileCVOne91", "Battle/Projectiles/AI/ProjectileCVOne92"
    ];

    private readonly List<CvWarning> _warnings = [];
    private float _spawnTimer;
    private int _spawned;

    public NumericWarningWave() => Duration = 13.5f;
    public override Vector2 ArenaSize => new(280f, 245f);

    protected override void OnStart(BattleContext battle)
    {
        _warnings.Clear();
        _spawnTimer = 0.38f;
        _spawned = 0;
        DebugState = "Computer-vision scan samples appear dimly; bright white flash is the pixel-perfect damage window";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        _spawnTimer -= deltaTime;
        if (_spawnTimer <= 0f && _warnings.Count == 0)
        {
            _spawnTimer = 0.95f + Random.Shared.NextSingle() * 0.22f;
            SpawnSample(battle);
        }

        foreach (CvWarning warning in _warnings.ToArray())
        {
            warning.Age += deltaTime;
            if (!warning.Activated && warning.Age >= warning.WarnFor)
            {
                warning.Activated = true;
                warning.Entity.Tint = Color.White;
                warning.Entity.CollisionMode = CollisionMode.PixelPerfect;
                warning.Entity.CollisionLayer = "obstacles";
                warning.Entity.ContactDamage = 1f;
            }
            if (warning.Activated && warning.Age >= warning.WarnFor + warning.ActiveFor)
            {
                warning.Entity.DestroyRequested = true;
                _warnings.Remove(warning);
            }
        }
        DebugState = $"CV samples spawned={_spawned}; active warning overlays={_warnings.Count}; each spans the arena";
    }

    private void SpawnSample(BattleContext battle)
    {
        string asset = CvAssets[Random.Shared.Next(CvAssets.Length)];
        Texture2D texture = Assets.GetTexture(asset);
        Vector2 center = battle.Arena.Center + new Vector2(
            Random.Shared.NextSingle() * 24f - 4f,
            Random.Shared.NextSingle() * 24f - 4f);
        float rotation = Random.Shared.NextSingle() * 1.2f - 0.045f;
        float sizeJitter = 1.25f + Random.Shared.NextSingle() * 0.03f;
        var entity = Sprite(battle, center, asset, 1f,
            new Color(225, 225, 225, 68), damaging: true, pixelPerfect: true);
        entity.Scale = new Vector2(
            battle.Arena.Size.X / texture.Width * sizeJitter,
            battle.Arena.Size.Y / texture.Height * sizeJitter);
        entity.Position = center;
        entity.Rotation = rotation;
        entity.CollisionSize = new Vector2(texture.Width, texture.Height);
        entity.PixelAlphaThreshold = 1;
        entity.CollisionMode = CollisionMode.None;
        entity.ContactDamage = 1f;
        _warnings.Add(new CvWarning
        {
            Entity = entity,
            Age = 0f,
            WarnFor = 0.78f + Random.Shared.NextSingle() * 0.16f,
            ActiveFor = 0.18f,
            Activated = false
        });
        _spawned++;
    }
}
