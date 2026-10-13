
using System;
using Microsoft.Xna.Framework;


/// <summary>
/// CSS animates all four DOM blocks throughout the wave.
/// In JS mode, two followers chase predictably and two pursue
/// unpredictably with feints and sideways dashes.
/// The JS gun fires exactly two shots.
/// </summary>
public sealed class CssJsGunWave : DepartmentWaveBase
{
    private const int TargetCount = 4;
    private const int PredictableFollowerCount = 2;
    private const int UnpredictableFollowerCount = 2;

    private DepartmentVisualEntity _gun = null!;

    private readonly DepartmentVisualEntity[] _targets =
        new DepartmentVisualEntity[TargetCount];

    private readonly Vector2[] _targetOrigins =
        new Vector2[TargetCount];

    // Independent movement state for each unpredictable follower.
    private readonly float[] _unpredictableTimers =
        new float[UnpredictableFollowerCount];

    private readonly Vector2[] _unpredictableDirections =
        new Vector2[UnpredictableFollowerCount];

    private float _shotTimer;
    private int _shotCount;
    private bool _switchedToJs;
    private int _jsShotsFired;
    private float _interactiveAge;

    private const float FollowerStartSpeed = 100f;
    private const float FollowerMaxSpeed = 180f;
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
            new(50f, 42f)
        };

        for (int i = 0; i < TargetCount; i++)
        {
            _targetOrigins[i] = center + offsets[i];

            _targets[i] = Sprite(
                battle,
                _targetOrigins[i],
                targetAssets[i],
                1.55f,
                new Color(150, 205, 255));

            _targets[i].CollisionSize *= 0.8f;
            _targets[i].CollisionMode = CollisionMode.None;
        }

        _shotTimer = 0.5f;
        _shotCount = 0;
        _switchedToJs = false;
        _jsShotsFired = 0;
        _interactiveAge = 0f;

        for (int i = 0; i < UnpredictableFollowerCount; i++)
        {
            _unpredictableTimers[i] = 0f;
            _unpredictableDirections[i] = Vector2.UnitX;
        }

        DebugState =
            "CSS animates four DOM blocks; JS activates four followers";
    }

    protected override void OnUpdate(
        BattleContext battle,
        float deltaTime)
    {
        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 center = battle.Arena.Center;

        // Gun slides into position.
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

            for (int i = 0; i < TargetCount; i++)
            {
                _targets[i].CollisionMode =
                    CollisionMode.RotatedBox;

                _targets[i].CollisionLayer = "obstacles";
                _targets[i].ContactDamage = 1f;
            }

            _targets[0].Tint = new Color(255, 140, 200);
            _targets[1].Tint = new Color(255, 180, 110);
            _targets[2].Tint = new Color(180, 150, 255);
            _targets[3].Tint = new Color(110, 235, 180);

            _interactiveAge = 0f;

            for (int i = 0; i < UnpredictableFollowerCount; i++)
            {
                _unpredictableTimers[i] = 0f;
                _unpredictableDirections[i] = Vector2.UnitX;
            }
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
        else if (_jsShotsFired < 2)
        {
            FireScriptToken(battle);
            _jsShotsFired++;
        }

        if (!_switchedToJs)
        {
            // CSS mode: all four blocks animate at their original positions.
            for (int i = 0; i < TargetCount; i++)
            {
                UpdateCssAppearance(i);

                float pulse = GetCssPulse(i);

                _targets[i].Position =
                    _targetOrigins[i] +
                    new Vector2(
                        MathF.Sin(Elapsed * 1.8f + i) * 9f,
                        MathF.Sin(Elapsed * 2.7f + i * 1.2f) * 10f);

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
            // JS mode: CSS visuals remain active while every block moves.
            _interactiveAge += deltaTime;

            float speed = MathF.Min(
                FollowerMaxSpeed,
                FollowerStartSpeed +
                _interactiveAge * FollowerAcceleration);

            // First two followers: predictable direct pursuit.
            for (int i = 0; i < PredictableFollowerCount; i++)
            {
                DepartmentVisualEntity follower = _targets[i];

                Vector2 direction = Direction(
                    follower.Position,
                    battle.Player.Position);

                follower.Position += direction * speed * deltaTime;

                // Preserve the original vertical wobble.
                float wobble =
                    MathF.Sin(Elapsed * 3.4f + i * MathHelper.Pi) *
                    10f * deltaTime;

                follower.Position += new Vector2(0f, wobble);
            }

            // Last two followers: unpredictable pursuit.
            for (int i = 0; i < UnpredictableFollowerCount; i++)
            {
                int targetIndex = PredictableFollowerCount + i;

                DepartmentVisualEntity follower = _targets[targetIndex];

                _unpredictableTimers[i] -= deltaTime;

                if (_unpredictableTimers[i] <= 0f)
                {
                    _unpredictableTimers[i] =
                        0.12f + Random.Shared.NextSingle() * 0.28f;

                    Vector2 toPlayer =
                        battle.Player.Position - follower.Position;

                    if (toPlayer.LengthSquared() > 0.001f)
                        toPlayer.Normalize();

                    // Random pursuit angle.
                    float angle =
                        (Random.Shared.NextSingle() * 2f - 1f) * 1.25f;

                    float cos = MathF.Cos(angle);
                    float sin = MathF.Sin(angle);

                    Vector2 direction = new(
                        toPlayer.X * cos - toPlayer.Y * sin,
                        toPlayer.X * sin + toPlayer.Y * cos);

                    // Sometimes dash sideways, blending back toward the player.
                    if (Random.Shared.NextSingle() < 0.3f)
                    {
                        direction = new Vector2(
                            -toPlayer.Y,
                            toPlayer.X);

                        if (Random.Shared.NextSingle() < 0.5f)
                            direction *= -1f;

                        direction = Vector2.Lerp(
                            direction,
                            toPlayer,
                            0.45f);
                    }

                    if (direction.LengthSquared() > 0.001f)
                        direction.Normalize();

                    _unpredictableDirections[i] = direction;
                }

                follower.Position +=
                    _unpredictableDirections[i] * speed * deltaTime;
            }

            // Keep the CSS pulse, rotation, and color effects running
            // AFTER movement, so they never reset follower positions.
            for (int i = 0; i < TargetCount; i++)
            {
                UpdateCssAppearance(i);

                _targets[i].CollisionMode = CollisionMode.RotatedBox;
                _targets[i].CollisionLayer = "obstacles";
                _targets[i].ContactDamage = 1f;
            }
        }

        DebugState = _switchedToJs
            ? $"JS mode: predictable=2; unpredictable=2; " +
              $"shots={_jsShotsFired}/2; " +
              $"speed={MathF.Min(FollowerMaxSpeed, FollowerStartSpeed + _interactiveAge * FollowerAcceleration):0}px/s"
            : $"CSS mode: four animated DOM blocks; CSS tokens={_shotCount}";
    }

    /// <summary>
    /// Applies the original CSS pulse, rotation, and color animation.
    /// Does not change the entity's position.
    /// </summary>
    private void UpdateCssAppearance(int i)
    {
        DepartmentVisualEntity target = _targets[i];

        target.Scale = new Vector2(GetCssPulse(i));

        target.Rotation =
            MathF.Sin(Elapsed * 2.0f + i) * 0.45f;

        target.Tint = new Color(
            (byte)(110 + 120 *
                (0.5f + 0.5f *
                MathF.Sin(Elapsed * 2.4f + i))),

            (byte)(115 + 125 *
                (0.5f + 0.5f *
                MathF.Sin(Elapsed * 2.8f + i * 1.7f))),

            (byte)(130 + 120 *
                (0.5f + 0.5f *
                MathF.Sin(Elapsed * 3.1f + i * 0.8f))));
    }

    private float GetCssPulse(int i)
    {
        return 3.15f +
            0.65f *
            (0.5f + 2f *
            MathF.Sin(Elapsed * 3.2f + i * 1.3f));
    }

    private void FireScriptToken(BattleContext battle)
    {
        Vector2 origin = _gun.Position + new Vector2(
            10f,
            Random.Shared.NextSingle() * 18f - 9f);

        if (!_switchedToJs)
        {
            int targetIndex = _shotCount % TargetCount;

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
            // Exactly two JS shots, aimed at the first two followers.
            int targetIndex = _jsShotsFired % PredictableFollowerCount;

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
