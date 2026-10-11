using System;
using Microsoft.Xna.Framework;

/// <summary>Nine network nodes illuminate in sequence and emit fast CV projectiles.</summary>
public sealed class NeuralNetworkWave : DepartmentWaveBase
{
    private readonly Vector2[] _positions = new Vector2[9];
    private readonly DepartmentVisualEntity[] _nodes = new DepartmentVisualEntity[9];
    private float _timer;
    private float _flashRemaining;
    private int _activeNode = -1;
    private int _shotCount;

    public NeuralNetworkWave() => Duration = 10.5f;
    public override Vector2 ArenaSize => new(270f, 240f);

    protected override void OnStart(BattleContext battle)
    {
        Vector2 center = battle.Arena.Center;
        float[] rows = { -62f, 0f, 62f };
        float[] columns = { -78f, 0f, 78f };
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                int index = row * 3 + col;
                _positions[index] = center + new Vector2(columns[col], rows[row]);
            }
        }

        // Dense feed-forward links sit behind the supplied node/projectile sprites.
        int[,] edges =
        {
            { 0, 3 }, { 0, 4 }, { 1, 3 }, { 1, 4 }, { 1, 5 }, { 2, 4 }, { 2, 5 },
            { 3, 6 }, { 3, 7 }, { 4, 6 }, { 4, 7 }, { 4, 8 }, { 5, 7 }, { 5, 8 }
        };
        for (int i = 0; i < edges.GetLength(0); i++)
        {
            Vector2 a = _positions[edges[i, 0]];
            Vector2 b = _positions[edges[i, 1]];
            Vector2 delta = b - a;
            Visual(battle, (a + b) * 0.5f,
                new Vector2(delta.Length(), 2f), new Color(90, 45, 150, 180),
                MathF.Atan2(delta.Y, delta.X));
        }

        for (int i = 0; i < _nodes.Length; i++)
        {
            _nodes[i] = Sprite(battle, _positions[i],
                $"Battle/Projectiles/AI/ProjectileCVOne{i + 1}1", 1.35f,
                new Color(135, 75, 210));
        }

        _timer = 0.55f;
        _flashRemaining = 0f;
        _activeNode = -1;
        _shotCount = 0;
        DebugState = "CV network linked; nodes pulse one by one before firing";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        _timer -= deltaTime;
        _flashRemaining = MathF.Max(0f, _flashRemaining - deltaTime);
        if (_activeNode >= 0 && _flashRemaining <= 0f)
        {
            _nodes[_activeNode].Tint = new Color(135, 75, 210);
            _nodes[_activeNode].Scale = new Vector2(1.35f);
        }

        if (_timer <= 0f)
        {
            _timer += 0.66f;
            _activeNode = (_activeNode + 1) % _nodes.Length;
            _flashRemaining = 0.24f;
            _nodes[_activeNode].Tint = Color.White;
            _nodes[_activeNode].Scale = new Vector2(1.8f);

            Vector2 origin = _positions[_activeNode];
            Vector2 direction = Direction(origin, battle.Player.Position);
            SpriteProjectile(battle, origin,
                $"Battle/Projectiles/AI/ProjectileCVOne{_activeNode + 1}2",
                direction * (176f + (_activeNode % 3) * 14f), 1.35f,
                new Color(235, 155, 255), lifetime: 3.0f,
                damaging: true, pixelPerfect: true);
            _shotCount++;
        }

        DebugState = $"Neuron {_activeNode + 1}/9 active; fast projectiles fired={_shotCount}";
    }
}
