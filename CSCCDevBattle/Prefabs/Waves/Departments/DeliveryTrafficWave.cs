using System;
using Microsoft.Xna.Framework;

/// <summary>Cross three marked city roads from the bottom exit to the top while delivery trucks make repeated passes.</summary>
public sealed class DeliveryTrafficWave : DepartmentWaveBase
{
    private const int LaneCount = 3;
    private readonly float[] _laneY = new float[LaneCount];
    private float _spawnTimer;
    private int _truckIndex;
    private bool _reachedExit;

    public DeliveryTrafficWave() => Duration = float.PositiveInfinity;
    public override Vector2 ArenaSize => new(290f, 265f);

    protected override void OnStart(BattleContext battle)
    {
        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;
        float spacing = (max.Y - min.Y) / (LaneCount + 1f);
        for (int lane = 0; lane < LaneCount; lane++)
        {
            _laneY[lane] = min.Y + spacing * (lane + 1);
            Visual(battle, new Vector2(battle.Arena.Center.X, _laneY[lane]),
                new Vector2(max.X - min.X - 5f, spacing * 0.86f), new Color(58, 58, 58));

            // Short lane dashes keep the road readable without becoming hazards.
            float dashY = _laneY[lane] - spacing * 0.42f;
            for (float x = min.X + 14f; x < max.X - 14f; x += 31f)
                Visual(battle, new Vector2(x, dashY), new Vector2(13f, 1.5f), new Color(150, 150, 150));
        }

        battle.Player.Position = new Vector2(battle.Arena.Center.X, max.Y - 22f);
        _spawnTimer = 0.2f;
        _truckIndex = 0;
        _reachedExit = false;
        DebugState = "Three gray roads active; cross upward between repeated fast delivery traffic";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;
        _spawnTimer -= deltaTime;
        if (_spawnTimer <= 0f)
        {
            _spawnTimer += 0.27f + Random.Shared.NextSingle() * 0.13f;
            // Random lanes allow several trucks to queue on the same road in quick succession.
            int lane = Random.Shared.Next(LaneCount);
            bool fromLeft = ((_truckIndex + lane) % 2) == 0;
            float speed = 205f + Random.Shared.NextSingle() * 55f;
            Vector2 velocity = new(fromLeft ? speed : -speed, 0f);
            Vector2 position = new(fromLeft ? min.X - 38f : max.X + 38f, _laneY[lane]);
            DepartmentVisualEntity truck = SpriteProjectile(battle, position,
                "Battle/Projectiles/Mobile/MiniDeliveryTruck", velocity,
                4f, Color.White, lifetime: 3.2f,
                damaging: true, pixelPerfect: true);
            // Original truck art faces right. Flip only vehicles that move left.
            if (!fromLeft)
                truck.Scale = new Vector2(-4f, 4f);
            _truckIndex++;
        }

        if (!_reachedExit && battle.Player.Position.Y <= min.Y + 18f && Elapsed >= 10f)
        {
            _reachedExit = true;
            Finish();
        }
        DebugState = $"Roads={LaneCount}; trucks={_truckIndex}; reached top exit={_reachedExit}";
    }
}
