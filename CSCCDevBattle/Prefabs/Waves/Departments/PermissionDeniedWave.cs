using Microsoft.Xna.Framework;

/// <summary>Mobile: alternating movement lanes are blocked, leaving a readable gap.</summary>
public sealed class PermissionDeniedWave : DepartmentWaveBase
{
    public PermissionDeniedWave() { Duration = 9f; }
    public override Vector2 ArenaSize => new(250f, 230f);

    private float _timer;
    private int _volley;

    protected override void OnStart(BattleContext battle)
    {
        _timer = 0.4f;
        _volley = 0;
        DebugState = "Controls available; lane lock incoming";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        _timer -= deltaTime;
        if (_timer > 0f) return;
        _timer += 1.0f;
        int gap = (_volley * 2 + 1) % 6;
        bool fromLeft = (_volley % 2) == 0;
        for (int lane = 0; lane < 6; lane++)
        {
            if (lane == gap) continue;
            float y = battle.Arena.InnerMinimum.Y + 18f + lane * 35f;
            float x = fromLeft ? battle.Arena.InnerMinimum.X : battle.Arena.InnerMaximum.X;
            float vx = fromLeft ? 135f : -135f;
            Fire(battle, new Vector2(x, y), new Vector2(vx, 0f),
                new Color(70, 190, 255), 2f, 3.5f);
        }
        _volley++;
        DebugState = $"Volley {_volley}; safe lane {gap}; horizontal input {(fromLeft ? "warned" : "released")}";
    }
}
