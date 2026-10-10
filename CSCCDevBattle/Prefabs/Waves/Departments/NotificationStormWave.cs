using Microsoft.Xna.Framework;

/// <summary>Mobile: notification cards sweep in from alternating sides.</summary>
public sealed class NotificationStormWave : DepartmentWaveBase
{
    public NotificationStormWave() { Duration = 9f; }
    public override Vector2 ArenaSize => new(250f, 230f);

    private float _timer;
    private int _row;

    protected override void OnStart(BattleContext battle)
    {
        _timer = 0.35f;
        _row = 0;
        DebugState = "First notification in 0.35s";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        _timer -= deltaTime;
        if (_timer > 0f) return;
        _timer += 0.78f;

        float y = battle.Arena.InnerMinimum.Y + 28f + (_row % 4) * 48f;
        bool fromLeft = (_row++ % 2) == 0;
        float x = fromLeft ? battle.Arena.Minimum.X - 20f : battle.Arena.Maximum.X + 20f;
        float speed = fromLeft ? 155f : -155f;

        var card = Visual(battle, new Vector2(x, y), new Vector2(34f, 15f),
            _row % 2 == 0 ? new Color(40, 205, 255) : new Color(150, 255, 80),
            damaging: true);
        card.CollisionLayer = "obstacles";
        card.DestroyOnPlayerContact = true;
        card.Behaviours.Add(new DepartmentProjectileBehaviour(new Vector2(speed, 0f), 3f));
        DebugState = $"Card row {_row}; side={(fromLeft ? "left" : "right")}; y={y:0}";
    }
}
