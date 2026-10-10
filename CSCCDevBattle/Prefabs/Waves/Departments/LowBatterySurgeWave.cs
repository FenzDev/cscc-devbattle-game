using Microsoft.Xna.Framework;

/// <summary>Mobile: charging cables warn first, then become damaging beams.</summary>
public sealed class LowBatterySurgeWave : DepartmentWaveBase
{
    public LowBatterySurgeWave() { Duration = 9f; }
    public override Vector2 ArenaSize => new(250f, 230f);

    private float _timer;
    private int _lane;

    protected override void OnStart(BattleContext battle)
    {
        _timer = 0.45f;
        _lane = 0;
        DebugState = "Battery draining; first cable warning";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        _timer -= deltaTime;
        if (_timer > 0f) return;
        _timer += 1.35f;
        float[] lanes = { -0.30f, 0f, 0.30f };
        float y = battle.Arena.Center.Y + lanes[_lane % lanes.Length] * battle.Arena.Size.Y;
        TelegraphBeam(battle, new Vector2(battle.Arena.Center.X, y),
            new Vector2(battle.Arena.Size.X - 20f, 5f), 0f,
            new Color(110, 220, 255), new Color(190, 255, 70), 0.75f, 0.48f);
        _lane++;
        DebugState = $"Charging cable lane {_lane % 3}; next in {_timer:0.00}s";
    }
}
