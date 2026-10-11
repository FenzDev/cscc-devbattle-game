using System;
using Microsoft.Xna.Framework;

/// <summary>Cross four traffic lanes from the bottom exit to the top exit.</summary>
public sealed class DeliveryTrafficWave : DepartmentWaveBase
{
    private float _spawnTimer;
    private int _truckIndex;
    private bool _reachedExit;

    public DeliveryTrafficWave() => Duration = float.PositiveInfinity;
    public override Vector2 ArenaSize => new(280f, 255f);

    protected override void OnStart(BattleContext battle)
    {
        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;
        battle.Player.Position = new Vector2(battle.Arena.Center.X, max.Y - 22f);
        _spawnTimer = 0.35f;
        _truckIndex = 0;
        _reachedExit = false;
        DebugState = "Traffic crossing: start at the bottom and reach the top exit";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;
        _spawnTimer -= deltaTime;

        if (_spawnTimer <= 0f)
        {
            _spawnTimer += 0.52f + Random.Shared.NextSingle() * 0.28f;
            int lane = _truckIndex % 4;
            bool fromLeft = ((_truckIndex / 2) + lane) % 2 == 0;
            float y = min.Y + 34f + lane * ((max.Y - min.Y - 68f) / 3f);
            float speed = 128f + Random.Shared.NextSingle() * 52f;
            if (!fromLeft) speed = -speed;
            float x = fromLeft ? min.X - 24f : max.X + 24f;
            SpriteProjectile(battle, new Vector2(x, y),
                "Battle/Projectiles/Mobile/MiniDeliveryTruck",
                new Vector2(speed, 0f), 2f,
                lane % 2 == 0 ? new Color(255, 220, 105) : new Color(120, 225, 255),
                lifetime: 3.8f, damaging: true, destroyOnContact: true);
            _truckIndex++;
        }

        // This is an objective wave: it completes only after the player crosses the field.
        if (!_reachedExit && battle.Player.Position.Y <= min.Y + 18f && Elapsed >= 8f)
        {
            _reachedExit = true;
            Finish();
        }

        DebugState = $"Traffic trucks={_truckIndex}; crossed top exit={_reachedExit}; survive and advance upward";
    }
}
