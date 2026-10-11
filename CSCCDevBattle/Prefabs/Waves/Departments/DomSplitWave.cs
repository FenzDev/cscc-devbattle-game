using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

/// <summary>Large DOM nodes travel in, then split into smaller divergent children.</summary>
public sealed class DomSplitWave : DepartmentWaveBase
{
    private sealed class BlobState
    {
        public DepartmentVisualEntity Entity = null!;
        public Vector2 Velocity;
        public float Age;
        public float SplitAt;
        public int Generation;
    }

    private readonly List<BlobState> _blobs = [];
    private float _spawnTimer;
    private int _blobNumber;

    public DomSplitWave() => Duration = 10.5f;
    public override Vector2 ArenaSize => new(270f, 240f);

    protected override void OnStart(BattleContext battle)
    {
        _blobs.Clear();
        _spawnTimer = 0.4f;
        _blobNumber = 0;
        DebugState = "DOM body nodes incoming; large nodes split into child elements";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        _spawnTimer -= deltaTime;
        if (_spawnTimer <= 0f)
        {
            _spawnTimer += 1.55f;
            SpawnParent(battle);
        }

        foreach (BlobState blob in _blobs.ToArray())
        {
            if (blob.Entity.DestroyRequested)
            {
                _blobs.Remove(blob);
                continue;
            }

            blob.Age += deltaTime;
            blob.Entity.Position += blob.Velocity * deltaTime;
            blob.Entity.Rotation = MathF.Atan2(blob.Velocity.Y, blob.Velocity.X) * 0.12f;

            if (blob.Age >= blob.SplitAt)
            {
                SplitBlob(battle, blob);
                blob.Entity.DestroyRequested = true;
                _blobs.Remove(blob);
            }
        }

        DebugState = $"DOM tree collapse: parents spawned={_blobNumber}; active splitting nodes={_blobs.Count}";
    }

    private void SpawnParent(BattleContext battle)
    {
        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;
        bool fromLeft = _blobNumber % 2 == 0;
        Vector2 start = new(fromLeft ? min.X - 15f : max.X + 15f,
            min.Y + 25f + Random.Shared.NextSingle() * (max.Y - min.Y - 50f));
        Vector2 target = battle.Player.Position + new Vector2(
            Random.Shared.NextSingle() * 24f - 12f,
            Random.Shared.NextSingle() * 24f - 12f);
        Vector2 velocity = Direction(start, target) * (88f + Random.Shared.NextSingle() * 22f);
        string[] assets =
        {
            "Battle/Projectiles/Web/ProjectileDOMBody",
            "Battle/Projectiles/Web/ProjectileDOMHtml",
            "Battle/Projectiles/Web/ProjectileDOMDiv"
        };
        var entity = Sprite(battle, start, assets[_blobNumber % assets.Length], 2.25f,
            new Color(85, 220, 255), damaging: true);
        entity.CollisionSize *= 0.8f;
        _blobs.Add(new BlobState
        {
            Entity = entity,
            Velocity = velocity,
            Age = 0f,
            SplitAt = 1.15f + Random.Shared.NextSingle() * 0.45f,
            Generation = 0
        });
        _blobNumber++;
    }

    private void SplitBlob(BattleContext battle, BlobState blob)
    {
        Vector2 baseDirection = blob.Velocity;
        if (baseDirection.LengthSquared() < 0.01f)
            baseDirection = Vector2.UnitX;
        baseDirection.Normalize();

        string[] childAssets =
        {
            "Battle/Projectiles/Web/ProjectileDOMDiv",
            "Battle/Projectiles/Web/ProjectileDOMH1",
            "Battle/Projectiles/Web/ProjectileDOMP",
            "Battle/Projectiles/Web/ProjectileDOMHtml"
        };

        for (int i = 0; i < 3; i++)
        {
            float spread = (i - 1) * 0.58f;
            Vector2 direction = Rotate(baseDirection, spread);
            SpriteProjectile(battle, blob.Entity.Position,
                childAssets[(blob.Generation + i + _blobNumber) % childAssets.Length],
                direction * (112f + i * 12f), 1.45f,
                i == 1 ? new Color(255, 150, 235) : new Color(100, 220, 255),
                lifetime: 3.1f, damaging: true, pixelPerfect: true);
        }
    }
}
