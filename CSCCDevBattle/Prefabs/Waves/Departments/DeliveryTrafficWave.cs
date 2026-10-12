
using System;
using Microsoft.Xna.Framework;

public sealed class DeliveryTrafficWave : DepartmentWaveBase
{
    private const int LaneCount = 6;

    private const float PlayerSpeedMultiplier = 0.45f;
    private const float TruckScale = 4f;
    private const float SpawnIntervalMin = 0.27f;
    private const float SpawnIntervalRandom = 0.13f;

    private readonly float[] _laneY = new float[LaneCount];

    private float _spawnTimer;
    private int _truckIndex;
    private bool _reachedExit;

    public DeliveryTrafficWave()
    {
        Duration = float.PositiveInfinity;
    }

    public override Vector2 ArenaSize => new(290f, 265f);

    protected override void OnStart(BattleContext battle)
    {
        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;

        float spacing = (max.Y - min.Y) / (LaneCount + 1f);

        for (int lane = 0; lane < LaneCount; lane++)
        {
            _laneY[lane] = min.Y + spacing * (lane + 1);

            Visual(
                battle,
                new Vector2(battle.Arena.Center.X, _laneY[lane]),
                new Vector2(max.X - min.X - 5f, spacing * 0.86f),
                new Color(58, 58, 58));

            float dashY = _laneY[lane] - spacing * 0.42f;

            for (float x = min.X + 14f; x < max.X - 14f; x += 31f)
            {
                Visual(
                    battle,
                    new Vector2(x, dashY),
                    new Vector2(13f, 1.5f),
                    new Color(150, 150, 150));
            }
        }

        battle.PlayerMovementSpeedMultiplier = PlayerSpeedMultiplier;
        battle.Player.Position = new Vector2(
            battle.Arena.Center.X,
            max.Y - 22f);

        _spawnTimer = 0.2f;
        _truckIndex = 0;
        _reachedExit = false;

        DebugState =
            "Three roads active; alternating traffic directions; player speed reduced";
    }

    protected override void OnUpdate(
        BattleContext battle,
        float deltaTime)
    {
        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;

        _spawnTimer -= deltaTime;

        if (_spawnTimer <= 0f)
        {
            _spawnTimer +=
                SpawnIntervalMin +
                Random.Shared.NextSingle() * SpawnIntervalRandom;

            int lane = Random.Shared.Next(LaneCount);

            // Adjacent lanes flow in opposite directions.
            // With three lanes, the outside lane shares a direction.
            bool movesRight = lane % 2 == 0;

            float speed =
                245f + Random.Shared.NextSingle() * 55f;

            Vector2 velocity = new(
                movesRight ? speed : -speed,
                0f);

            Vector2 position = new(
                movesRight ? min.X - 38f : max.X + 38f,
                _laneY[lane]);

            DepartmentVisualEntity truck = SpriteProjectile(
                battle,
                position,
                "Battle/Projectiles/Mobile/MiniDeliveryTruck",
                velocity,
                scale: TruckScale,
                lifetime: 3.2f,
                damaging: true,
                pixelPerfect: true,
                destroyOnContact: false);

            // Never use a negative X scale for mirroring.
            truck.Scale = new Vector2(TruckScale);
            truck.FlipHorizontally = !movesRight;

            // The collision controller uses the normal obstacle
            // collision query to trigger the player's reset.
            truck.ResetPlayerToBottomOnContact = true;

            _truckIndex++;
        }

        if (!_reachedExit &&
            battle.Player.Position.Y <= min.Y + 18f &&
            Elapsed >= 10f)
        {
            _reachedExit = true;
            Finish();
        }

        DebugState =
            $"Roads={LaneCount}; trucks={_truckIndex}; " +
            $"speed={PlayerSpeedMultiplier:0.00}x; " +
            $"reached top exit={_reachedExit}";
    }

    protected override void OnEnd(BattleContext battle)
    {
        // Do not leave the player slowed when the wave ends.
        battle.PlayerMovementSpeedMultiplier = 1f;
    }
}
