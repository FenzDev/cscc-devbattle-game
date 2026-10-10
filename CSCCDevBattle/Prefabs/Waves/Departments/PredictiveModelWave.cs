using Microsoft.Xna.Framework;

/// <summary>AI: estimates player velocity and aims slightly ahead of the predicted position.</summary>
public sealed class PredictiveModelWave : DepartmentWaveBase
{
    public PredictiveModelWave() { Duration = 10f; }
    public override Vector2 ArenaSize => new(250f, 230f);

    private Vector2 _previousPlayer;
    private Vector2 _estimatedVelocity;
    private float _timer;
    private int _prediction;

    protected override void OnStart(BattleContext battle)
    {
        _previousPlayer = battle.Player.Position;
        _estimatedVelocity = Vector2.Zero;
        _timer = 0.55f;
        _prediction = 0;
        DebugState = "Model warming up; collecting player movement samples";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        if (deltaTime > 0.0001f)
        {
            Vector2 measured = (battle.Player.Position - _previousPlayer) / deltaTime;
            _estimatedVelocity = Vector2.Lerp(_estimatedVelocity, measured, 0.18f);
            _previousPlayer = battle.Player.Position;
        }

        _timer -= deltaTime;
        if (_timer > 0f) return;
        _timer += 1.12f;

        Vector2 predicted = ClampToArena(battle,
            battle.Player.Position + _estimatedVelocity * 0.32f);
        Vector2 origin = new(
            (_prediction % 2 == 0) ? battle.Arena.InnerMinimum.X + 4f : battle.Arena.InnerMaximum.X - 4f,
            battle.Arena.Center.Y + ((_prediction % 3) - 1) * 35f);
        Vector2 direction = Direction(origin, predicted);
        Fire(battle, origin, Rotate(direction, -0.14f) * 148f, new Color(255, 75, 230), 1.8f);
        Fire(battle, origin, direction * 160f, new Color(255, 105, 245), 1.9f);
        Fire(battle, origin, Rotate(direction, 0.14f) * 148f, new Color(255, 75, 230), 1.8f);
        _prediction++;
        DebugState = $"Prediction {_prediction}; estimated speed={_estimatedVelocity.Length():0} px/s; lead=0.32s";
    }
}
