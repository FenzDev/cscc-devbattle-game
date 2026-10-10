using Microsoft.Xna.Framework;

/// <summary>Web: 404 warning shots target the player's position, then some redirect mid-flight.</summary>
public sealed class Http404RedirectWave : DepartmentWaveBase
{
    public Http404RedirectWave() { Duration = 9f; }
    public override Vector2 ArenaSize => new(250f, 230f);

    private float _timer;
    private int _request;

    protected override void OnStart(BattleContext battle)
    {
        _timer = 0.3f;
        _request = 0;
        DebugState = "404 routes initializing";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        _timer -= deltaTime;
        if (_timer > 0f) return;
        _timer += 0.66f;

        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;
        Vector2 origin = (_request % 2 == 0)
            ? new Vector2(min.X + 4f, min.Y + 12f + (_request % 4) * 45f)
            : new Vector2(max.X - 4f, max.Y - 12f - (_request % 4) * 45f);
        Vector2 target = battle.Player.Position;
        Vector2 velocity = Direction(origin, target) * 160f;
        bool redirect = (_request % 2) == 1;
        Fire(battle, origin, velocity, new Color(255, 65, 75), 2f, 4f,
            redirect ? 0.52f : -1f, redirect ? ((_request % 4 == 1) ? 0.95f : -0.95f) : 0f);
        _request++;
        DebugState = $"HTTP 404 request {_request}; redirect={(redirect ? "armed" : "none")}";
    }
}
