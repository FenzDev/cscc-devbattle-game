
using System;
using Microsoft.Xna.Framework;

/// <summary>
/// CSS mutates DOM blocks; the JS gun fires twice and activates two
/// interactive DOM objects that pursue the player.
/// The first follower moves unpredictably; the second keeps its
/// original direct pursuit and vertical wobble.
/// </summary>
public sealed class CssJsGunWave : DepartmentWaveBase
{
    private DepartmentVisualEntity _gun = null!;

    private readonly DepartmentVisualEntity[] _targets =
        new DepartmentVisualEntity[4];

    private readonly Vector2[] _targetOrigins = new Vector2[4];

    private float _shotTimer;
    private int _shotCount;
    private bool _switchedToJs;
    private int _jsShotsFired;
    private float _interactiveAge;

    // Unpredictable follower movement.
    private float _unpredictableTimer;
    private Vector2 _unpredictableDirection;

    private const int FollowerCount = 2;
    private const float FollowerStartSpeed = 125f;
    private const float FollowerMaxSpeed = 200f;
    private const float FollowerAcceleration = 10f;

    public CssJsGunWave() => Duration = 12f;

    public override Vector2 ArenaSize => new(280f, 240f);

    protected override void OnStart(BattleContext battle)
    {
        Vector2 center = battle.Arena.Center;
        Vector2 min = battle.Arena.InnerMinimum;

        _gun = Sprite(
            battle,
            new Vector2(min.X - 36f, center.Y),
            "Battle/Projectiles/Web/CSSGun",
            2.8f,
            Color.White);

        string[] targetAssets =
        {
            "Battle/Projectiles/ProjectileUnroundedBall",
            "Battle/Projectiles/ProjectileUnroundedBall",
            "Battle/Projectiles/ProjectileUnroundedBall",
            "Battle/Projectiles/ProjectileUnroundedBall"
        };

        Vector2[] offsets =
        {
            new(6f, -48f),
            new(54f, -12f),
            new(10f, 42f),
            new(50f, 42f),
            new(76f, 48f)
        };

        for (int i = 0; i < _targets.Length; i++)
        {
            _targetOrigins[i] = center + offsets[i];

            _targets[i] = Sprite(
                battle,
                _targetOrigins[i],
                targetAssets[i],
                1.55f,
                new Color(150, 205, 255));

            _targets[i].CollisionSize *= 0.8f;
        }

        _shotTimer = 0.5f;
        _shotCount = 0;
        _switchedToJs = false;
        _jsShotsFired = 0;
        _interactiveAge = 0f;

        _unpredictableTimer = 0f;
        _unpredictableDirection = Vector2.UnitX;

        DebugState =
            "CSS gun entering from the left; blocks are about to be restyled";
    }

    protected override void OnUpdate(
        BattleContext battle,
        float deltaTime)
    {
        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 center = battle.Arena.Center;

        float gunTargetX = min.X + 25f;

        _gun.Position = new Vector2(
            MathHelper.Lerp(
                _gun.Position.X,
                gunTargetX,
                MathHelper.Clamp(deltaTime * 3.2f, 0f, 1f)),
            center.Y + MathF.Sin(Elapsed * 2.2f) * 8f);

        // Switch from CSS mode to JS mode.
        if (!_switchedToJs && Elapsed >= 5.35f)
        {
            _switchedToJs = true;

            _gun.Texture = Assets.GetTexture(
                "Battle/Projectiles/Web/JSGun");

            _gun.Tint = new Color(255, 245, 175);

            // Only the first two DOM blocks become interactive.
            for (int i = 0; i < _targets.Length; i++)
            {
                if (i < FollowerCount)
                {
                    _targets[i].CollisionMode =
                        CollisionMode.RotatedBox;

                    _targets[i].CollisionLayer = "obstacles";
                    _targets[i].ContactDamage = 1f;
                }
                else
                {
                    _targets[i].CollisionMode = CollisionMode.None;
                }
            }

            _targets[0].Tint = new Color(255, 140, 200);
            _targets[1].Tint = new Color(255, 180, 110);

            _interactiveAge = 0f;
            _unpredictableTimer = 0f;
            _unpredictableDirection = Vector2.UnitX;
        }

        // CSS mode fires repeatedly.
        // JS mode fires exactly two shots, then stops.
        if (!_switchedToJs)
        {
            _shotTimer -= deltaTime;

            if (_shotTimer <= 0f)
            {
                _shotTimer += 0.82f;
                FireScriptToken(battle);
            }
        }
        else if (_jsShotsFired < FollowerCount)
        {
            FireScriptToken(battle);
            _jsShotsFired++;
        }

        if (!_switchedToJs)
        {
            // CSS mode: animate and restyle all DOM blocks.
            for (int i = 0; i < _targets.Length; i++)
            {
                float pulse =
                    3.15f +
                    0.65f *
                    (0.5f + 2f *
                    MathF.Sin(Elapsed * 3.2f + i * 1.3f));

                _targets[i].Scale = new Vector2(pulse);

                _targets[i].Position =
                    _targetOrigins[i] +
                    new Vector2(
                        MathF.Sin(Elapsed * 1.8f + i) * 9f,
                        MathF.Sin(Elapsed * 2.7f + i * 1.2f) * 10f);

                _targets[i].Rotation =
                    MathF.Sin(Elapsed * 2.0f + i) * 0.45f;

                _targets[i].Tint = new Color(
                    (byte)(110 + 120 *
                        (0.5f + 0.5f *
                        MathF.Sin(Elapsed * 2.4f + i))),

                    (byte)(115 + 125 *
                        (0.5f + 0.5f *
                        MathF.Sin(Elapsed * 2.8f + i * 1.7f))),

                    (byte)(130 + 120 *
                        (0.5f + 0.5f *
                        MathF.Sin(Elapsed * 3.1f + i * 0.8f))));

                _targets[i].CollisionMode =
                    pulse > 1.55f
                        ? CollisionMode.RotatedBox
                        : CollisionMode.None;

                _targets[i].CollisionLayer = "obstacles";
                _targets[i].ContactDamage = 1f;
            }
        }
        else
        {
            // JS mode: both followers pursue the player.
            _interactiveAge += deltaTime;

            float speed = MathF.Min(
                FollowerMaxSpeed,
                FollowerStartSpeed +
                _interactiveAge * FollowerAcceleration);

            // -----------------------------------------------------
            // FOLLOWER 0: unpredictable pursuit with feints/dashes.
            // -----------------------------------------------------
            DepartmentVisualEntity unpredictable = _targets[0];

            _unpredictableTimer -= deltaTime;

            if (_unpredictableTimer <= 0f)
            {
                _unpredictableTimer =
                    0.12f + Random.Shared.NextSingle() * 0.28f;

                Vector2 toPlayer =
                    battle.Player.Position - unpredictable.Position;

                if (toPlayer.LengthSquared() > 0.001f)
                    toPlayer.Normalize();

                // Randomly vary the pursuit angle.
                float angle =
                    (Random.Shared.NextSingle() * 2f - 1f) * 1.25f;

                float cos = MathF.Cos(angle);
                float sin = MathF.Sin(angle);

                _unpredictableDirection = new Vector2(
                    toPlayer.X * cos - toPlayer.Y * sin,
                    toPlayer.X * sin + toPlayer.Y * cos);

                // Sometimes make a sharp sideways dash.
                if (Random.Shared.NextSingle() < 0.3f)
                {
                    _unpredictableDirection = new Vector2(
                        -toPlayer.Y,
                        toPlayer.X);

                    if (Random.Shared.NextSingle() < 0.5f)
                        _unpredictableDirection *= -1f;

                    // Keep the dash generally aimed toward the player.
                    _unpredictableDirection = Vector2.Lerp(
                        _unpredictableDirection,
                        toPlayer,
                        0.45f);

                    if (_unpredictableDirection.LengthSquared() > 0.001f)
                        _unpredictableDirection.Normalize();
                }
            }

            unpredictable.Position +=
                _unpredictableDirection * speed * deltaTime;

            unpredictable.Rotation = MathF.Atan2(
                _unpredictableDirection.Y,
                _unpredictableDirection.X) * 0.25f;

            unpredictable.Scale = new Vector2(
                1.65f + 0.5f * MathF.Sin(Elapsed * 4f));

            // -----------------------------------------------------
            // FOLLOWER 1: original direct chase and vertical wobble.
            // -----------------------------------------------------
            DepartmentVisualEntity follower = _targets[1];

            Vector2 direction = Direction(
                follower.Position,
                battle.Player.Position);

            follower.Position += direction * speed * deltaTime;

            float wobble =
                MathF.Sin(Elapsed * 3.4f + MathHelper.Pi) *
                10f * deltaTime;

            follower.Position += new Vector2(0f, wobble);

            follower.Rotation = MathF.Atan2(
                direction.Y,
                direction.X) * 0.25f;

            follower.Scale = new Vector2(
                2.65f + 0.15f *
                MathF.Sin(Elapsed * 4f + 0.8f));
        }

        DebugState = _switchedToJs
            ? $"JS mode: followers=2; JS shots={_jsShotsFired}/2; " +
              $"speed={MathF.Min(FollowerMaxSpeed, FollowerStartSpeed + _interactiveAge * FollowerAcceleration):0}px/s"
            : $"CSS mode: restyling animated DOM blocks; CSS tokens={_shotCount}";
    }

    private void FireScriptToken(BattleContext battle)
    {
        Vector2 origin = _gun.Position + new Vector2(
            10f,
            Random.Shared.NextSingle() * 18f - 9f);

        if (!_switchedToJs)
        {
            int targetIndex = _shotCount % _targets.Length;

            Vector2 direction = Direction(
                origin,
                _targets[targetIndex].Position);

            SpriteProjectile(
                battle,
                origin,
                "Battle/Projectiles/Web/ProjectileFileCss",
                direction * 190f,
                0.5f,
                new Color(100, 225, 255),
                lifetime: 1.2f,
                damaging: false,
                destroyOnContact: false);
        }
        else
        {
            // Aim each JS shot at a different follower.
            int targetIndex = _jsShotsFired % FollowerCount;

            Vector2 direction = Direction(
                origin,
                _targets[targetIndex].Position);

            SpriteProjectile(
                battle,
                origin,
                "Battle/Projectiles/Web/ProjectileFileJs",
                direction * 220f,
                0.5f,
                new Color(255, 225, 125),
                lifetime: 1.4f,
                damaging: false,
                destroyOnContact: false);
        }

        _shotCount++;
    }
}
