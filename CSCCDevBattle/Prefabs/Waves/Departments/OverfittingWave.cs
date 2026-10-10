using Microsoft.Xna.Framework;

/// <summary>AI: repeats a learnable lane pattern, then changes the gap every third volley.</summary>
public sealed class OverfittingWave : DepartmentWaveBase
{
    public OverfittingWave() { Duration = 9f; }
    public override Vector2 ArenaSize => new(250f, 230f);

    private float _timer;
    private int _volley;
    private readonly int[] _learnedGaps = { 1, 3, 1, 3, 1, 3 };

    protected override void OnStart(BattleContext battle)
    {
        _timer = 0.45f;
        _volley = 0;
        DebugState = "Training pattern: observe the repeated gaps";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        _timer -= deltaTime;
        if (_timer > 0f) return;
        _timer += 1.05f;

        bool adversarial = _volley > 0 && (_volley + 1) % 3 == 0;
        int gap = adversarial ? (_learnedGaps[_volley % _learnedGaps.Length] + 1) % 6
                              : _learnedGaps[_volley % _learnedGaps.Length];
        Vector2 min = battle.Arena.InnerMinimum;
        float spacing = battle.Arena.Size.X / 6f;
        for (int lane = 0; lane < 6; lane++)
        {
            if (lane == gap) continue;
            float x = min.X + spacing * (lane + 0.5f);
            Fire(battle, new Vector2(x, min.Y + 6f), new Vector2(0f, 145f),
                adversarial ? new Color(255, 80, 120) : new Color(185, 75, 255), 1.8f, 4f);
        }
        _volley++;
        DebugState = $"Volley {_volley}; {(adversarial ? "ADVERSARIAL VARIATION" : "memorized pattern")}; safe lane={gap}";
    }
}
