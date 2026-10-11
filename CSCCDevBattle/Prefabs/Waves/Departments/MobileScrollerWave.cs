using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>App feed scrolls under a moving fingertip; touching the finger hurts.</summary>
public sealed class MobileScrollerWave : DepartmentWaveBase
{
    private sealed class FeedRow
    {
        public DepartmentVisualEntity Button = null!;
        public DepartmentVisualEntity Text = null!;
    }

    private readonly List<FeedRow> _rows = [];
    private DepartmentVisualEntity _phone = null!;
    private DepartmentVisualEntity _finger = null!;
    private float _retargetTimer;
    private float _scrollVelocity;
    private Vector2 _fingerTarget;
    private int _swipeCount;

    public MobileScrollerWave() => Duration = 10.5f;
    public override Vector2 ArenaSize => new(270f, 245f);

    protected override void OnStart(BattleContext battle)
    {
        _rows.Clear();
        _phone = Sprite(battle, battle.Arena.Center,
            "Battle/Projectiles/Mobile/PhonePortrait", 4.5f,
            new Color(230, 245, 255, 230));

        Vector2 center = battle.Arena.Center;
        for (int i = 0; i < 6; i++)
        {
            float y = center.Y - 67f + i * 27f;
            var button = Sprite(battle, new Vector2(center.X - 34f, y),
                "Battle/Projectiles/Mobile/ScrollerButton", 1.35f,
                i % 2 == 0 ? new Color(120, 225, 255) : new Color(155, 245, 165));
            var text = Sprite(battle, new Vector2(center.X + 24f, y),
                "Battle/Projectiles/Mobile/ScrollerText", 1.25f,
                new Color(225, 240, 255));
            _rows.Add(new FeedRow { Button = button, Text = text });
        }

        _finger = Sprite(battle, center + new Vector2(-55f, -70f),
            "Battle/Projectiles/Mobile/ScrollerFinger", 2f,
            Color.White, damaging: true);
        _finger.OriginOverride = new Vector2(_finger.Texture!.Width * 0.5f, 0f);
        // The sprite position is its index fingertip; move the hurtbox down into the hand.
        _finger.CollisionSize = new Vector2(7f, 14f);
        _finger.CollisionOffset = new Vector2(0f, _finger.Texture.Height * 0.48f);
        _finger.CollisionMode = CollisionMode.RotatedBox;
        _finger.CollisionLayer = "obstacles";
        _finger.DestroyOnPlayerContact = false;

        _fingerTarget = center;
        _retargetTimer = 0.25f;
        _scrollVelocity = 85f;
        _swipeCount = 0;
        DebugState = "App feed live; fingertip scrolls and can strike the player";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        Vector2 center = battle.Arena.Center;
        float viewTop = center.Y - 75f;
        float viewBottom = center.Y + 75f;

        _retargetTimer -= deltaTime;
        if (_retargetTimer <= 0f)
        {
            _retargetTimer += 0.62f + Random.Shared.NextSingle() * 0.34f;
            bool nearPlayer = Random.Shared.NextSingle() < 0.28f;
            _fingerTarget = nearPlayer
                ? ClampToArena(battle, battle.Player.Position + new Vector2(
                    Random.Shared.NextSingle() * 54f - 27f,
                    Random.Shared.NextSingle() * 42f - 21f))
                : new Vector2(
                    center.X + Random.Shared.NextSingle() * 108f - 54f,
                    viewTop + 8f + Random.Shared.NextSingle() * (viewBottom - viewTop - 16f));

            _scrollVelocity = _fingerTarget.Y < _finger.Position.Y ? 118f : -118f;
            _swipeCount++;
        }

        Vector2 delta = _fingerTarget - _finger.Position;
        float distance = delta.Length();
        if (distance > 1f)
        {
            Vector2 direction = delta / distance;
            _finger.Position += direction * MathF.Min(distance, 142f * deltaTime);
            // The supplied finger texture points up; rotate that direction onto its target.
            _finger.Rotation = MathF.Atan2(direction.Y, direction.X) + MathHelper.PiOver2;
        }

        foreach (FeedRow row in _rows)
        {
            row.Button.Position += new Vector2(0f, _scrollVelocity * deltaTime);
            row.Text.Position += new Vector2(0f, _scrollVelocity * deltaTime);

            if (_scrollVelocity > 0f && row.Button.Position.Y > viewBottom)
            {
                float wrappedY = viewTop - 25f;
                float shift = wrappedY - row.Button.Position.Y;
                row.Button.Position += new Vector2(0f, shift);
                row.Text.Position += new Vector2(0f, shift);
            }
            else if (_scrollVelocity < 0f && row.Button.Position.Y < viewTop)
            {
                float wrappedY = viewBottom + 25f;
                float shift = wrappedY - row.Button.Position.Y;
                row.Button.Position += new Vector2(0f, shift);
                row.Text.Position += new Vector2(0f, shift);
            }
        }

        DebugState = $"Scroller swipes={_swipeCount}; scroll={_scrollVelocity:0}px/s; fingertip target=({_fingerTarget.X:0},{_fingerTarget.Y:0})";
    }
}
