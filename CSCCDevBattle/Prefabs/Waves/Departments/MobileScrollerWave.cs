
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

/// <summary>
/// A synchronized scrolling feed of grayscale button dashes.
/// All rows share one continuous scroll offset and velocity.
/// Direction changes ease smoothly without independently wrapping rows.
/// A larger, slower finger pursues the player.
/// </summary>
public sealed class MobileScrollerWave : DepartmentWaveBase
{
    private sealed class FeedRow
    {
        public readonly List<DepartmentVisualEntity> Items = [];
        public float BaseY;
    }

    private readonly List<FeedRow> _rows = [];

    private DepartmentVisualEntity _finger = null!;
    private float _retargetTimer;
    private float _scrollOffset;
    private float _scrollVelocity;
    private float _targetScrollVelocity;
    private Vector2 _fingerTarget;
    private int _swipeCount;

    private float _previousPlayerSpeedMultiplier;

    private const int RowCount = 10;

    private const float FingerScale = 2f;
    private const float FingerSpeed = 76f;
    private const float PlayerSpeedMultiplier = 0.72f;

    private const float ScrollSpeed = 48f;
    private const float ScrollAcceleration = 90f;

    private const float RowSpacing = 96f;

    private const float DashWidth = 24f;
    private const float DashHeight = 14f;
    private const float DashGap = 7f;
    private const float ClusterGap = 28f;
    private const float RowStagger = 30f;

    private const float PatternHeight = RowCount * RowSpacing;

    public MobileScrollerWave() => Duration = 14.5f;

    public override Vector2 ArenaSize => new(280f, 245f);

    protected override void OnStart(BattleContext battle)
    {
        _rows.Clear();

        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;

        _previousPlayerSpeedMultiplier =
            battle.PlayerMovementSpeedMultiplier;

        battle.PlayerMovementSpeedMultiplier =
            _previousPlayerSpeedMultiplier * PlayerSpeedMultiplier;

        _scrollOffset = 0f;
        _scrollVelocity = ScrollSpeed;
        _targetScrollVelocity = ScrollSpeed;
        _swipeCount = 0;

        float centerY = (min.Y + max.Y) * 0.5f;

        for (int i = 0; i < RowCount; i++)
        {
            float baseY =
                centerY + (i - (RowCount - 1) * 0.5f) * RowSpacing;

            var row = new FeedRow
            {
                BaseY = baseY
            };

            float stagger = i % 2 == 0 ? 0f : RowStagger;

            AddDashRow(
                battle,
                row,
                min.X,
                max.X,
                stagger);

            _rows.Add(row);
        }

        Vector2 center = battle.Arena.Center;

        _finger = Sprite(
            battle,
            center + new Vector2(-46f, -58f),
            "Battle/Projectiles/Mobile/ScrollerFinger",
            FingerScale,
            Color.White,
            damaging: true);

        _finger.OriginOverride = new Vector2(
            _finger.Texture!.Width * 0.5f,
            0f);

        _finger.CollisionSize = new Vector2(8f, 12f);
        _finger.CollisionOffset = new Vector2(
            0f,
            _finger.Texture.Height * FingerScale * 0.34f);

        _finger.CollisionMode = CollisionMode.RotatedBox;
        _finger.CollisionLayer = "obstacles";
        _finger.ContactDamage = 1f;
        _finger.DestroyOnPlayerContact = false;

        _fingerTarget = center;
        _retargetTimer = 0.9f;

        DebugState =
            $"Smooth synchronized feed; rows={RowCount}; " +
            $"spacing={RowSpacing:0}; scroll={ScrollSpeed:0}px/s";
    }

    protected override void OnUpdate(
        BattleContext battle,
        float deltaTime)
    {
        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;

        // Smoothly accelerate toward the requested scroll velocity.
        // Every row uses this same velocity.
        float velocityDifference =
            _targetScrollVelocity - _scrollVelocity;

        float maxVelocityChange = ScrollAcceleration * deltaTime;

        _scrollVelocity += MathHelper.Clamp(
            velocityDifference,
            -maxVelocityChange,
            maxVelocityChange);

        // Advance one shared offset for the entire pattern.
        _scrollOffset += _scrollVelocity * deltaTime;

        // Keep the offset bounded while preserving continuous movement.
        _scrollOffset %= PatternHeight;

        if (_scrollOffset < 0f)
            _scrollOffset += PatternHeight;

        // Move all objects by the exact same frame displacement.
        // No row-specific wrapping, repositioning, or accumulated drift.
        
        // Move every dash by the same amount.
        // Keep each row at its original Y position.
        float movementY = _scrollVelocity * deltaTime;

        foreach (FeedRow row in _rows)
        {
            foreach (DepartmentVisualEntity item in row.Items)
            {
                item.Position += new Vector2(0f, movementY);
            }
        }


        // Wrap the complete pattern as a single unit.
        // Because the repeating pattern height is RowCount * RowSpacing,
        // translating by one pattern height returns it to the same layout.
        if (_scrollOffset >= PatternHeight - RowSpacing)
        {
            float wrapDistance = PatternHeight;

            foreach (FeedRow row in _rows)
            {
                foreach (DepartmentVisualEntity item in row.Items)
                {
                    item.Position -= new Vector2(0f, wrapDistance);
                }

                row.BaseY -= wrapDistance;
            }

            _scrollOffset -= wrapDistance;
        }
        else if (_scrollOffset < 0f)
        {
            float wrapDistance = PatternHeight;

            foreach (FeedRow row in _rows)
            {
                foreach (DepartmentVisualEntity item in row.Items)
                {
                    item.Position += new Vector2(0f, wrapDistance);
                }

                row.BaseY += wrapDistance;
            }

            _scrollOffset += wrapDistance;
        }

        // Choose the finger's next target.
        _retargetTimer -= deltaTime;

        if (_retargetTimer <= 0f)
        {
            _retargetTimer =
                0.95f + Random.Shared.NextSingle() * 0.4f;

            bool nearPlayer = Random.Shared.NextSingle() < 0.15f;

            _fingerTarget = nearPlayer
                ? ClampToArena(
                    battle,
                    battle.Player.Position + new Vector2(
                        Random.Shared.NextSingle() * 60f - 30f,
                        Random.Shared.NextSingle() * 46f - 23f))
                : new Vector2(
                    min.X + 16f +
                    Random.Shared.NextSingle() *
                    (max.X - min.X - 32f),
                    min.Y + 16f +
                    Random.Shared.NextSingle() *
                    (max.Y - min.Y - 32f));

            // Change direction gradually instead of instantly reversing.
            _targetScrollVelocity =
                _fingerTarget.Y < _finger.Position.Y
                    ? ScrollSpeed
                    : -ScrollSpeed;

            _swipeCount++;
        }

        // Smooth finger movement toward its target.
        Vector2 fingerDelta = _fingerTarget - _finger.Position;
        float distance = fingerDelta.Length();

        if (distance > 1f)
        {
            Vector2 direction = fingerDelta / distance;

            _finger.Position += direction *
                MathF.Min(distance, FingerSpeed * deltaTime);

            _finger.Rotation =
                MathF.Atan2(direction.Y, direction.X)
                + MathHelper.PiOver2;
        }

        DebugState =
            $"Smooth feed rows={_rows.Count}; swipes={_swipeCount}; " +
            $"offset={_scrollOffset:0.0}; " +
            $"velocity={_scrollVelocity:0.0}px/s; " +
            $"pieces={CountPieces()}";
    }

    protected override void OnEnd(BattleContext battle)
    {
        battle.PlayerMovementSpeedMultiplier =
            _previousPlayerSpeedMultiplier;
    }

    private void AddDashRow(
        BattleContext battle,
        FeedRow row,
        float minX,
        float maxX,
        float stagger)
    {
        float startX = minX - DashWidth - stagger;
        float endX = maxX + DashWidth;

        float clusterLength =
            DashWidth * 3f + DashGap * 2f + ClusterGap;

        int clusterCount = (int)MathF.Ceiling(
            (endX - startX) / clusterLength) + 1;

        for (int cluster = 0; cluster < clusterCount; cluster++)
        {
            float clusterStart = startX + cluster * clusterLength;

            for (int dash = 0; dash < 3; dash++)
            {
                float x = clusterStart +
                    dash * (DashWidth + DashGap) +
                    DashWidth * 0.5f;

                if (x < minX - DashWidth || x > maxX + DashWidth)
                    continue;

                AddButton(
                    battle,
                    row,
                    x,
                    row.BaseY,
                    DashWidth,
                    DashHeight,
                    ButtonTone(cluster, dash));
            }
        }
    }

    private void AddButton(
        BattleContext battle,
        FeedRow row,
        float centerX,
        float centerY,
        float width,
        float height,
        Color tint)
    {
        DepartmentVisualEntity button = Visual(
            battle,
            new Vector2(centerX, centerY),
            new Vector2(width, height),
            tint,
            damaging: true);

        button.CollisionMode = CollisionMode.RotatedBox;
        button.CollisionLayer = "obstacles";
        button.CollisionSize = Vector2.One;
        button.ContactDamage = 1f;
        button.DestroyOnPlayerContact = false;

        row.Items.Add(button);
    }

    private int CountPieces()
    {
        int count = 0;

        foreach (FeedRow row in _rows)
            count += row.Items.Count;

        return count;
    }

    private static Color ButtonTone(int cluster, int dash)
    {
        int[] tones = [88, 108, 128];
        int tone = tones[(cluster + dash) % tones.Length];

        return new Color(
            (byte)tone,
            (byte)tone,
            (byte)tone);
    }
}
