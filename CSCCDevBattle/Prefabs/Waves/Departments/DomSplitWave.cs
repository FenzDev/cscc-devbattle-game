using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

/// <summary>DOM trees split along meaningful parent-child relationships while each node keeps travelling along its incoming route.</summary>
public sealed class DomSplitWave : DepartmentWaveBase
{
    private enum NodeType { Html, Head, Body, Div, H1, Paragraph }

    private sealed class BlobState
    {
        public DepartmentVisualEntity Entity = null!;
        public NodeType Type;
        public Vector2 Velocity;
        public float Age;
        public float SplitAt;
        public bool CanSplit;
    }

    private readonly List<BlobState> _blobs = [];
    private float _spawnTimer;
    private int _blobNumber;

    public DomSplitWave() => Duration = 13.5f;
    public override Vector2 ArenaSize => new(270f, 240f);

    protected override void OnStart(BattleContext battle)
    {
        _blobs.Clear();
        _spawnTimer = 0.45f;
        _blobNumber = 0;
        DebugState = "HTML nodes enter, split into HEAD/BODY, then BODY branches into child tags";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        _spawnTimer -= deltaTime;
        if (_spawnTimer <= 0f)
        {
            _spawnTimer += 1.65f;
            SpawnHtml(battle);
        }

        foreach (BlobState blob in _blobs.ToArray())
        {
            if (blob.Entity.DestroyRequested)
            {
                _blobs.Remove(blob);
                continue;
            }

            blob.Age += deltaTime;
            // Every surviving node gently homes toward the player's latest position instead of
            // remaining committed to the direction it had when its parent originally spawned.
            Vector2 desiredDirection = Direction(blob.Entity.Position, battle.Player.Position);
            float nodeSpeed = blob.Velocity.Length();
            Vector2 desiredVelocity = desiredDirection * nodeSpeed;
            blob.Velocity = Vector2.Lerp(blob.Velocity, desiredVelocity,
                MathHelper.Clamp(deltaTime * 1.45f, 0f, 1f));
            blob.Entity.Position += blob.Velocity * deltaTime;
            if (blob.Velocity.LengthSquared() > 1f)
                blob.Entity.Rotation = MathF.Atan2(blob.Velocity.Y, blob.Velocity.X) * 0.12f;

            if (blob.CanSplit && blob.Age >= blob.SplitAt)
            {
                SplitBlob(battle, blob);
                blob.Entity.DestroyRequested = true;
                _blobs.Remove(blob);
                continue;
            }

            Vector2 min = battle.Arena.Minimum - new Vector2(80f);
            Vector2 max = battle.Arena.Maximum + new Vector2(80f);
            Vector2 p = blob.Entity.Position;
            if (blob.Age >= 4.2f || p.X < min.X || p.X > max.X || p.Y < min.Y || p.Y > max.Y)
            {
                blob.Entity.DestroyRequested = true;
                _blobs.Remove(blob);
            }
        }

        DebugState = $"HTML trees spawned={_blobNumber}; active tags={_blobs.Count}; splits follow DOM hierarchy";
    }

    private void SpawnHtml(BattleContext battle)
    {
        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;
        bool fromLeft = _blobNumber % 2 == 0;
        Vector2 start = new(fromLeft ? min.X - 35f : max.X + 35f,
            min.Y + 26f + Random.Shared.NextSingle() * (max.Y - min.Y - 52f));
        Vector2 target = battle.Player.Position + new Vector2(
            Random.Shared.NextSingle() * 30f - 15f,
            Random.Shared.NextSingle() * 30f - 15f);
        Vector2 velocity = Direction(start, target) * (98f + Random.Shared.NextSingle() * 20f);
        DepartmentVisualEntity entity = Sprite(battle, start,
            "Battle/Projectiles/Web/ProjectileDOMHtml", 4.5f,
            Color.White, damaging: true, pixelPerfect: true);
        _blobs.Add(new BlobState
        {
            Entity = entity,
            Type = NodeType.Html,
            Velocity = velocity,
            Age = 0f,
            SplitAt = 1.05f + Random.Shared.NextSingle() * 0.25f,
            CanSplit = true
        });
        _blobNumber++;
    }

    private void SplitBlob(BattleContext battle, BlobState blob)
    {
        NodeType[] children = blob.Type switch
        {
            NodeType.Html => [NodeType.Head, NodeType.Body],
            NodeType.Body => [NodeType.Div, NodeType.Paragraph],
            NodeType.Div => [NodeType.Paragraph, NodeType.H1],
            // HEAD is a terminal visual node here; its browser metadata is not another damaging branch.
            _ => []
        };

        if (children.Length == 0)
            return;

        // Children pick up the parent's current position and aim around the player's current location.
        Vector2 baseDirection = Direction(blob.Entity.Position, battle.Player.Position);

        for (int i = 0; i < children.Length; i++)
        {
            float spread = (i == 0 ? -0.23f : 0.23f) + (Random.Shared.NextSingle() * 0.12f - 0.06f);
            Vector2 direction = Rotate(baseDirection, spread);
            NodeType childType = children[i];
            string asset = AssetFor(childType);
            float speed = 125f + Random.Shared.NextSingle() * 22f;
            float scale = 2.9f; // double the previous child-node scale
            DepartmentVisualEntity child = Sprite(battle, blob.Entity.Position,
                asset, scale, Color.White, damaging: true, pixelPerfect: true);

            bool canSplit = childType is NodeType.Body or NodeType.Div;
            _blobs.Add(new BlobState
            {
                Entity = child,
                Type = childType,
                Velocity = direction * speed,
                Age = 0f,
                SplitAt = 0.9f + Random.Shared.NextSingle() * 0.2f,
                CanSplit = canSplit
            });
        }
    }

    private static string AssetFor(NodeType type) => type switch
    {
        NodeType.Html => "Battle/Projectiles/Web/ProjectileDOMHtml",
        NodeType.Head => "Battle/Projectiles/Web/ProjectileDOMHead",
        NodeType.Body => "Battle/Projectiles/Web/ProjectileDOMBody",
        NodeType.Div => "Battle/Projectiles/Web/ProjectileDOMDiv",
        NodeType.H1 => "Battle/Projectiles/Web/ProjectileDOMH1",
        NodeType.Paragraph => "Battle/Projectiles/Web/ProjectileDOMP",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
