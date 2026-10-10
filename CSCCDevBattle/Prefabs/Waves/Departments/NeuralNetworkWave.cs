using System;
using Microsoft.Xna.Framework;

/// <summary>AI: a small network lights up while active nodes fire in sequence.</summary>
public sealed class NeuralNetworkWave : DepartmentWaveBase
{
    public NeuralNetworkWave() { Duration = 10f; }
    public override Vector2 ArenaSize => new(250f, 230f);

    private readonly Vector2[] _nodes = new Vector2[6];
    private float _timer;
    private int _activeNode;

    protected override void OnStart(BattleContext battle)
    {
        Vector2 center = battle.Arena.Center;
        _nodes[0] = center + new Vector2(-78f, -52f);
        _nodes[1] = center + new Vector2(0f, -58f);
        _nodes[2] = center + new Vector2(78f, -52f);
        _nodes[3] = center + new Vector2(-78f, 28f);
        _nodes[4] = center + new Vector2(0f, 54f);
        _nodes[5] = center + new Vector2(78f, 28f);

        int[,] edges = { { 0, 1 }, { 1, 2 }, { 0, 3 }, { 1, 4 }, { 2, 5 }, { 3, 4 }, { 4, 5 } };
        for (int i = 0; i < edges.GetLength(0); i++)
        {
            Vector2 a = _nodes[edges[i, 0]];
            Vector2 b = _nodes[edges[i, 1]];
            Vector2 delta = b - a;
            Visual(battle, (a + b) * 0.5f, new Vector2(delta.Length(), 2f),
                new Color(120, 45, 210, 180), MathF.Atan2(delta.Y, delta.X));
        }
        foreach (Vector2 node in _nodes)
            Visual(battle, node, new Vector2(9f), new Color(215, 70, 255));

        _timer = 0.6f;
        _activeNode = 0;
        DebugState = "Network graph initialized; signal moving node-to-node";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        _timer -= deltaTime;
        if (_timer > 0f) return;
        _timer += 0.72f;
        Vector2 origin = _nodes[_activeNode % _nodes.Length];
        Vector2 direction = Direction(origin, battle.Player.Position);
        Fire(battle, origin, direction * 150f, new Color(235, 80, 255), 1.8f);
        _activeNode++;
        DebugState = $"Signal active node {_activeNode % _nodes.Length}; emitted {_activeNode} packets";
    }
}
