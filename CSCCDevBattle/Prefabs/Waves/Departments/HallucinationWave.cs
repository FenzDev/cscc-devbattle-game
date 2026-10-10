using Microsoft.Xna.Framework;

/// <summary>AI: duplicate-looking projectiles mix real hits with harmless decoys.</summary>
public sealed class HallucinationWave : DepartmentWaveBase
{
    public HallucinationWave() { Duration = 9f; }
    public override Vector2 ArenaSize => new(250f, 230f);

    private float _timer;
    private int _sample;

    protected override void OnStart(BattleContext battle)
    {
        _timer = 0.4f;
        _sample = 0;
        DebugState = "Inference pass: real and hallucinated samples mixed";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        _timer -= deltaTime;
        if (_timer > 0f) return;
        _timer += 1.05f;
        Vector2 min = battle.Arena.InnerMinimum;
        float spacing = battle.Arena.Size.X / 6f;
        for (int i = 0; i < 5; i++)
        {
            Vector2 origin = new(min.X + 14f + i * spacing, min.Y + 8f);
            bool real = ((i + _sample) % 3) != 0;
            Vector2 target = battle.Player.Position + new Vector2((i - 2) * 9f, 0f);
            Fire(battle, origin, Direction(origin, target) * (real ? 130f : 118f),
                real ? new Color(245, 80, 255) : new Color(125, 100, 145, 120),
                real ? 2f : 1.7f, 4f, damaging: real);
        }
        _sample++;
        DebugState = $"Inference batch {_sample}; muted particles are decoys";
    }
}
