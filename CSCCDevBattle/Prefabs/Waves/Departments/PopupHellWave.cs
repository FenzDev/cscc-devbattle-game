using System;
using Microsoft.Xna.Framework;

/// <summary>Web: popup windows accumulate; their close buttons emit aimed shots.</summary>
public sealed class PopupHellWave : DepartmentWaveBase
{
    public PopupHellWave() { Duration = 9f; }
    public override Vector2 ArenaSize => new(250f, 230f);

    private float _timer;
    private int _popup;
    private readonly Vector2[] _anchors = new Vector2[4];

    protected override void OnStart(BattleContext battle)
    {
        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;
        _anchors[0] = new Vector2(min.X + 42f, min.Y + 38f);
        _anchors[1] = new Vector2(max.X - 42f, min.Y + 38f);
        _anchors[2] = new Vector2(min.X + 42f, max.Y - 38f);
        _anchors[3] = new Vector2(max.X - 42f, max.Y - 38f);
        _timer = 0.4f;
        _popup = 0;
        DebugState = "Popup blockers spawning";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        _timer -= deltaTime;
        if (_timer > 0f) return;
        _timer += 1.0f;

        Vector2 anchor = _anchors[_popup % _anchors.Length];
        Visual(battle, anchor, new Vector2(48f, 29f), new Color(28, 42, 68));
        Visual(battle, anchor + new Vector2(0f, -12f), new Vector2(48f, 5f), new Color(255, 75, 95));
        Vector2 close = anchor + new Vector2(19f, -12f);
        Visual(battle, close, new Vector2(5f), new Color(255, 235, 235));
        Fire(battle, close, Direction(close, battle.Player.Position) * 140f,
            new Color(255, 80, 110), 1.8f);
        if (_popup > 0)
            Fire(battle, close, Rotate(Direction(close, battle.Player.Position), 0.28f) * 125f,
                new Color(255, 125, 145), 1.6f);
        _popup++;
        DebugState = $"Open popups={_popup}; close-button shots={_popup + Math.Max(0, _popup - 1)}";
    }
}
