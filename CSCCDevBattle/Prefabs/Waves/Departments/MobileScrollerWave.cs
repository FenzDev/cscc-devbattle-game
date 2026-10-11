using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

/// <summary>
/// A full-arena scrolling feed made from consistent grayscale button-like
/// rectangles. Each row has a changing escape gap. Feed pieces cause contact
/// damage instead of physically blocking the player.
/// </summary>
public sealed class MobileScrollerWave : DepartmentWaveBase
{
    private sealed class FeedRow
    {
        public readonly List<DepartmentVisualEntity> Items = [];
        public float Y;
        public float Speed;
    }

    private readonly List<FeedRow> _rows = [];
    private DepartmentVisualEntity _finger = null!;
    private float _retargetTimer;
    private float _scrollVelocity;
    private Vector2 _fingerTarget;
    private int _swipeCount;

    public MobileScrollerWave() => Duration = 14.5f;
    public override Vector2 ArenaSize => new(280f, 245f);

    protected override void OnStart(BattleContext battle)
    {
        _rows.Clear();

        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;
        float width = max.X - min.X;
        float height = max.Y - min.Y;
        const int rowCount = 7;
        float spacing = height / rowCount;

        // No phone, icon, or text sprites: all feed elements use one consistent
        // grayscale rectangle style, with variation in width, height, and layout.
        for (int i = 0; i < rowCount; i++)
        {
            float y = min.Y + spacing * (i + 0.5f);
            float margin = 7f;
            // Gaps weave from side to side but stay away from the arena edges.
            float gapCenter = min.X + width * (0.27f + Random.Shared.NextSingle() * 0.46f);
            float gapWidth = 40f + Random.Shared.NextSingle() * 12f;
            float gapLeft = gapCenter - gapWidth * 0.5f;
            float gapRight = gapCenter + gapWidth * 0.5f;
            float rowHeight = 9f + Random.Shared.NextSingle() * 5f;

            var row = new FeedRow
            {
                Y = y,
                Speed = 72f + Random.Shared.NextSingle() * 20f
            };

            // Build each side from short, button-like hit targets instead of a
            // single wall. This keeps a clear route through every row and lets
            // the player weave between varied shapes.
            AddButtonRun(battle, row, min.X + margin, gapLeft - 4f, y, rowHeight, i);
            AddButtonRun(battle, row, gapRight + 4f, max.X - margin, y, rowHeight, i + 3);

            // A few rows get an extra offset chip on one side. It remains outside
            // the main escape gap and follows the same feed motion as the row.
            if (i % 2 == 1)
            {
                bool placeLeft = i % 4 == 1;
                float chipWidth = 13f + Random.Shared.NextSingle() * 9f;
                float chipX = placeLeft
                    ? min.X + 13f + Random.Shared.NextSingle() * MathF.Max(2f, gapLeft - min.X - chipWidth - 20f)
                    : gapRight + 13f + Random.Shared.NextSingle() * MathF.Max(2f, max.X - gapRight - chipWidth - 20f);
                float chipY = y + (Random.Shared.NextSingle() < 0.5f ? -5f : 5f);
                AddButton(battle, row, chipX + chipWidth * 0.5f, chipY, chipWidth,
                    4f + Random.Shared.NextSingle() * 3f, new Color(145, 145, 145));
            }

            _rows.Add(row);
        }

        Vector2 center = battle.Arena.Center;
        _finger = Sprite(battle, center + new Vector2(-46f, -58f),
            "Battle/Projectiles/Mobile/ScrollerFinger", 1.35f, Color.White, damaging: true);
        // The 24x24 finger art's index tip is top-centre. Keep the position on
        // that fingertip and put a small rotated hurtbox into the hand.
        _finger.OriginOverride = new Vector2(_finger.Texture!.Width * 0.5f, 0f);
        _finger.CollisionSize = new Vector2(5f, 8f);
        _finger.CollisionOffset = new Vector2(0f, _finger.Texture.Height * 0.34f);
        _finger.CollisionMode = CollisionMode.RotatedBox;
        _finger.CollisionLayer = "obstacles";
        _finger.ContactDamage = 1f;
        _finger.DestroyOnPlayerContact = false;

        _fingerTarget = center;
        _retargetTimer = 0.35f;
        _scrollVelocity = 88f;
        _swipeCount = 0;
        DebugState = "Grayscale button feed; seven rows scroll with staggered escape gaps; panels damage on contact";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;
        float spacing = (max.Y - min.Y) / _rows.Count;
        float viewTop = min.Y + 5f;
        float viewBottom = max.Y - 5f;

        _retargetTimer -= deltaTime;
        if (_retargetTimer <= 0f)
        {
            _retargetTimer += 0.62f + Random.Shared.NextSingle() * 0.34f;
            bool nearPlayer = Random.Shared.NextSingle() < 0.27f;
            _fingerTarget = nearPlayer
                ? ClampToArena(battle, battle.Player.Position + new Vector2(
                    Random.Shared.NextSingle() * 44f - 22f,
                    Random.Shared.NextSingle() * 36f - 18f))
                : new Vector2(
                    min.X + 12f + Random.Shared.NextSingle() * (max.X - min.X - 24f),
                    viewTop + 8f + Random.Shared.NextSingle() * (viewBottom - viewTop - 16f));

            _scrollVelocity = _fingerTarget.Y < _finger.Position.Y ? 108f : -108f;
            _swipeCount++;
        }

        Vector2 fingerDelta = _fingerTarget - _finger.Position;
        float distance = fingerDelta.Length();
        if (distance > 1f)
        {
            Vector2 direction = fingerDelta / distance;
            _finger.Position += direction * MathF.Min(distance, 138f * deltaTime);
            // The finger artwork points up; rotate its top-centre tip toward the target.
            _finger.Rotation = MathF.Atan2(direction.Y, direction.X) + MathHelper.PiOver2;
        }

        foreach (FeedRow row in _rows)
        {
            float movement = MathF.Sign(_scrollVelocity) * row.Speed * deltaTime;
            row.Y += movement;
            foreach (DepartmentVisualEntity item in row.Items)
                item.Position += new Vector2(0f, movement);

            if (movement > 0f && row.Y > viewBottom)
            {
                float shift = viewTop - spacing - row.Y;
                row.Y += shift;
                foreach (DepartmentVisualEntity item in row.Items)
                    item.Position += new Vector2(0f, shift);
            }
            else if (movement < 0f && row.Y < viewTop)
            {
                float shift = viewBottom + spacing - row.Y;
                row.Y += shift;
                foreach (DepartmentVisualEntity item in row.Items)
                    item.Position += new Vector2(0f, shift);
            }
        }

        DebugState = $"Button feed rows={_rows.Count}; swipes={_swipeCount}; scroll={_scrollVelocity:0}px/s; feed pieces are damaging, not solid";
    }

    private void AddButtonRun(
        BattleContext battle,
        FeedRow row,
        float startX,
        float endX,
        float y,
        float baseHeight,
        int styleSeed)
    {
        float cursor = startX;
        int piece = 0;
        while (cursor < endX - 8f && piece < 10)
        {
            float remaining = endX - cursor;
            float desiredWidth = 17f + Random.Shared.NextSingle() * 28f;
            float buttonWidth = MathF.Min(remaining, desiredWidth);

            // Avoid an awkward tiny final segment: merge it into the current button.
            float after = remaining - buttonWidth;
            if (after > 0f && after < 10f)
                buttonWidth = remaining;
            if (buttonWidth < 8f)
                break;

            float buttonHeight = baseHeight * (0.72f + Random.Shared.NextSingle() * 0.42f);
            float yOffset = Random.Shared.NextSingle() * 4f - 2f;
            Color tint = ButtonTone(styleSeed + piece);
            AddButton(battle, row, cursor + buttonWidth * 0.5f, y + yOffset,
                buttonWidth, buttonHeight, tint);

            cursor += buttonWidth + 3f + Random.Shared.NextSingle() * 4f;
            piece++;
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

        // Do not use the solid layer: the player should be able to cross feed
        // pieces and take damage, rather than getting physically blocked.
        button.CollisionMode = CollisionMode.RotatedBox;
        button.CollisionLayer = "obstacles";
        button.CollisionSize = Vector2.One;
        button.ContactDamage = 1f;
        button.DestroyOnPlayerContact = false;
        row.Items.Add(button);
    }

    private static Color ButtonTone(int seed)
    {
        // A restrained monochrome palette keeps all controls visually consistent.
        int[] tones = [72, 86, 98, 112, 126, 138];
        int tone = tones[Math.Abs(seed + Random.Shared.Next(tones.Length)) % tones.Length];
        return new Color((byte)tone, (byte)tone, (byte)tone);
    }
}
