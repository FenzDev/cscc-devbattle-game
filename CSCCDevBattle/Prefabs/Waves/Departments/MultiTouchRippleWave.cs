using Microsoft.Xna.Framework;

/// <summary>Mobile: touch points warn briefly, then burst in radial patterns.</summary>
public sealed class MultiTouchRippleWave : DepartmentWaveBase
{
    public MultiTouchRippleWave() { Duration = 9f; }
    public override Vector2 ArenaSize => new(250f, 230f);

    private readonly Vector2[] _points = new Vector2[4];
    private float _timer;
    private int _burst;

    protected override void OnStart(BattleContext battle)
    {
        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;
        _points[0] = new Vector2(min.X + 35f, min.Y + 35f);
        _points[1] = new Vector2(max.X - 35f, min.Y + 35f);
        _points[2] = new Vector2(min.X + 35f, max.Y - 35f);
        _points[3] = new Vector2(max.X - 35f, max.Y - 35f);
        _timer = 0.8f;
        _burst = 0;
        foreach (Vector2 point in _points)
            Visual(battle, point, new Vector2(12f), new Color(60, 220, 255, 170));
        DebugState = "Touch indicators visible; waiting for ripple";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        _timer -= deltaTime;
        if (_timer > 0f) return;
        _timer += 1.55f;
        Vector2 origin = _points[_burst % _points.Length];
        RadialBurst(battle, origin, 8, 105f + _burst * 8f, _burst * 0.23f,
            new Color(60, 220, 255), 1.7f);
        _burst++;
        DebugState = $"Ripple {_burst}; origin=({origin.X:0},{origin.Y:0})";
    }
}
