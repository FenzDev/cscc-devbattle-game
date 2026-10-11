using System;
using Microsoft.Xna.Framework;

/// <summary>Three-layer neural net: each activation selects one random node from the next layer and fires a projectile.</summary>
public sealed class NeuralNetworkWave : DepartmentWaveBase
{
    private const int NodesPerLayer = 3;
    private const int LayerCount = 3;
    private readonly Vector2[] _positions = new Vector2[NodesPerLayer * LayerCount];
    private readonly DepartmentVisualEntity[] _nodes = new DepartmentVisualEntity[NodesPerLayer * LayerCount];
    private float _timer;
    private float _flashRemaining;
    private int _activeNode = -1;
    private int _activeLayer = -1;
    private int _shotCount;

    public NeuralNetworkWave() => Duration = 13.5f;
    public override Vector2 ArenaSize => new(280f, 245f);

    protected override void OnStart(BattleContext battle)
    {
        Vector2 c = battle.Arena.Center;
        float[] layerX = { -76f, 0f, 76f };
        float[] nodeY = { -48f, 0f, 48f };
        for (int layer = 0; layer < LayerCount; layer++)
        for (int node = 0; node < NodesPerLayer; node++)
            _positions[layer * NodesPerLayer + node] = c + new Vector2(layerX[layer], nodeY[node]);

        // Only forward connections are drawn: layer 1 -> layer 2 -> layer 3.
        // No neuron is connected horizontally to another neuron in its own layer.
        for (int layer = 0; layer < LayerCount - 1; layer++)
        for (int source = 0; source < NodesPerLayer; source++)
        for (int destination = 0; destination < NodesPerLayer; destination++)
        {
            Vector2 a = _positions[layer * NodesPerLayer + source];
            Vector2 b = _positions[(layer + 1) * NodesPerLayer + destination];
            Vector2 delta = b - a;
            Visual(battle, (a + b) * 0.5f,
                new Vector2(delta.Length(), 1.4f), new Color(165, 165, 165, 155),
                MathF.Atan2(delta.Y, delta.X));
        }

        for (int i = 0; i < _nodes.Length; i++)
        {
            // Basic white ball sprites represent neurons; CV textures are reserved for computer-vision hazards.
            _nodes[i] = Sprite(battle, _positions[i],
                "Battle/Projectiles/ProjectileBall", 2.7f,
                new Color(205, 205, 205), damaging: false);
        }

        _timer = 0.55f;
        _flashRemaining = 0f;
        _activeNode = -1;
        _activeLayer = -1;
        _shotCount = 0;
        DebugState = "Three-layer feed-forward network linked; random node activates layer-by-layer";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        _timer -= deltaTime;
        _flashRemaining = MathF.Max(0f, _flashRemaining - deltaTime);
        if (_activeNode >= 0 && _flashRemaining <= 0f)
        {
            _nodes[_activeNode].Tint = new Color(205, 205, 205);
            _nodes[_activeNode].Scale = new Vector2(2.7f);
        }

        if (_timer <= 0f)
        {
            _timer += 0.62f;
            _activeLayer = (_activeLayer + 1) % LayerCount;
            int column = Random.Shared.Next(NodesPerLayer);
            _activeNode = _activeLayer * NodesPerLayer + column;
            _flashRemaining = 0.22f;
            _nodes[_activeNode].Tint = Color.White;
            _nodes[_activeNode].Scale = new Vector2(3.4f);

            Vector2 origin = _positions[_activeNode];
            Vector2 direction = Direction(origin, battle.Player.Position);
            SpriteProjectile(battle, origin, "Battle/Projectiles/ProjectileBall",
                direction * (192f + Random.Shared.NextSingle() * 32f), 2.7f,
                Color.White, lifetime: 3.0f, damaging: true, pixelPerfect: true);
            _shotCount++;
        }

        DebugState = $"Layer={_activeLayer + 1}/{LayerCount}; node={_activeNode % NodesPerLayer + 1}/{NodesPerLayer}; projectiles={_shotCount}";
    }
}
