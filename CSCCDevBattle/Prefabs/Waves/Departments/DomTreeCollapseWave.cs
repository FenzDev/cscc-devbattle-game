using System;
using Microsoft.Xna.Framework;

/// <summary>Web: a DOM-like tree is drawn, then leaves fall before larger branch nodes.</summary>
public sealed class DomTreeCollapseWave : DepartmentWaveBase
{
    public DomTreeCollapseWave() { Duration = 9f; }
    public override Vector2 ArenaSize => new(250f, 230f);

    private float _timer;
    private int _drop;

    protected override void OnStart(BattleContext battle)
    {
        Vector2 top = new(battle.Arena.Center.X, battle.Arena.InnerMinimum.Y + 16f);
        Vector2[] nodes =
        {
            top, top + new Vector2(-48f, 20f), top + new Vector2(48f, 20f),
            top + new Vector2(-72f, 44f), top + new Vector2(-25f, 44f),
            top + new Vector2(25f, 44f), top + new Vector2(72f, 44f)
        };
        for (int i = 1; i < nodes.Length; i++)
        {
            Vector2 a = nodes[i];
            Vector2 b = nodes[i == 1 || i == 2 ? 0 : i <= 4 ? 1 : 2];
            Vector2 delta = b - a;
            float angle = MathF.Atan2(delta.Y, delta.X);
            Visual(battle, (a + b) * 0.5f, new Vector2(delta.Length(), 2f),
                new Color(70, 190, 255), angle);
        }
        foreach (Vector2 node in nodes)
            Visual(battle, node, new Vector2(8f), new Color(110, 230, 255));

        _timer = 0.45f;
        _drop = 0;
        DebugState = "DOM tree rendered; leaf nodes will detach first";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        _timer -= deltaTime;
        if (_timer > 0f) return;
        _timer += Elapsed < 3.2f ? 0.32f : 0.72f;

        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;
        bool leaves = Elapsed < 3.2f;
        float[] leafXs = { -72f, -25f, 25f, 72f };
        float x = battle.Arena.Center.X + leafXs[_drop % leafXs.Length];
        if (!leaves) x = battle.Arena.Center.X + ((_drop % 3) - 1) * 48f;
        Fire(battle, new Vector2(x, min.Y + 8f), new Vector2(0f, 105f + (_drop % 3) * 12f),
            leaves ? new Color(100, 235, 255) : new Color(40, 160, 230), leaves ? 2f : 3f, 4f);
        _drop++;
        DebugState = $"{(leaves ? "Leaf cascade" : "Branch cascade")}; detached node {_drop}";
    }
}
