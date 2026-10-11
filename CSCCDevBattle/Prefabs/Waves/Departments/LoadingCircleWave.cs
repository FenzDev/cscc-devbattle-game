using System;
using Microsoft.Xna.Framework;

/// <summary>Rotating loading wheels shrink over the wave and drift on different paths.</summary>
public sealed class LoadingCircleWave : DepartmentWaveBase
{
    private readonly DepartmentVisualEntity[] _circles = new DepartmentVisualEntity[3];
    private readonly Vector2[] _centers = new Vector2[3];
    private readonly float[] _spin = { 2.6f, -3.1f, 4.0f };

    public LoadingCircleWave() => Duration = 10f;
    public override Vector2 ArenaSize => new(270f, 240f);

    protected override void OnStart(BattleContext battle)
    {
        Vector2 c = battle.Arena.Center;
        _centers[0] = c + new Vector2(-52f, -28f);
        _centers[1] = c + new Vector2(52f, 14f);
        _centers[2] = c + new Vector2(-18f, 58f);

        for (int i = 0; i < _circles.Length; i++)
        {
            _circles[i] = Sprite(battle, _centers[i],
                "Battle/Projectiles/Web/LoadingCircleCut", 2.65f,
                i == 1 ? new Color(140, 225, 255) : new Color(225, 245, 255),
                damaging: true, pixelPerfect: true);
            _circles[i].CollisionSize *= 0.74f;
        }
        DebugState = "Loading wheels spinning at different speeds and directions";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        float progress = MathHelper.Clamp(Elapsed / Duration, 0f, 1f);
        for (int i = 0; i < _circles.Length; i++)
        {
            float pulse = MathF.Sin(Elapsed * (1.55f + i * 0.26f) + i * 1.7f) * 3.5f;
            _circles[i].Position = _centers[i] + new Vector2(
                MathF.Sin(Elapsed * (0.72f + i * 0.12f) + i) * 16f,
                MathF.Cos(Elapsed * (0.9f + i * 0.14f) + i * 1.3f) * 13f);
            float scale = MathHelper.Lerp(2.65f, 1.15f, progress) + pulse * 0.035f;
            _circles[i].Scale = new Vector2(MathF.Max(0.95f, scale));
            _circles[i].Rotation += _spin[i] * deltaTime;
        }
        DebugState = $"Loading circles rotating; scale {2.65f * (1f - progress) + 1.15f * progress:0.00}x";
    }
}
