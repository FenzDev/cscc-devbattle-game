using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

/// <summary>Open city-block maze with wide buildings, passable streets, and two route-following map markers.</summary>
public sealed class CityMazeChaseWave : DepartmentWaveBase
{
    private const int Columns = 6;
    private const int Rows = 5;
    private readonly bool[,] _buildings = new bool[Rows, Columns];
    private readonly DepartmentVisualEntity[] _chasers = new DepartmentVisualEntity[2];
    private readonly (int Row, int Col)[] _chaserCells = new (int, int)[2];
    private readonly (int Row, int Col)[] _waypoints = new (int, int)[2];
    private readonly bool[] _hasWaypoint = new bool[2];
    private float _cellWidth;
    private float _cellHeight;
    private Vector2 _topLeft;

    public CityMazeChaseWave() => Duration = 13.5f;
    public override Vector2 ArenaSize => new(285f, 260f);

    protected override void OnStart(BattleContext battle)
    {
        Array.Clear(_buildings, 0, _buildings.Length);
        // Denser central building plots keep the perimeter and connecting streets open.
        // The walkable-cell graph remains connected, so both chasers can always route toward the player.
        (int Row, int Col)[] plots =
        [
            (1, 1), (1, 2), (1, 4),
            (2, 1), (2, 2),
            (3, 1), (3, 2), (3, 3), (3, 4)
        ];
        foreach ((int row, int col) in plots)
            _buildings[row, col] = true;

        _topLeft = battle.Arena.InnerMinimum;
        _cellWidth = (battle.Arena.InnerMaximum.X - _topLeft.X) / Columns;
        _cellHeight = (battle.Arena.InnerMaximum.Y - _topLeft.Y) / Rows;
        CreateBuildings(battle);

        int playerRow = Rows / 2;
        int playerCol = Columns / 2;
        battle.Player.Position = CellCenter(playerRow, playerCol);

        _chaserCells[0] = (0, 0);
        _chaserCells[1] = (Rows - 1, Columns - 1);
        Array.Fill(_hasWaypoint, false);
        for (int i = 0; i < _chasers.Length; i++)
        {
            _chaserCells[i] = FindNearestOpenCell(CellCenter(_chaserCells[i].Row, _chaserCells[i].Col));
            _chasers[i] = Sprite(battle, CellCenter(_chaserCells[i].Row, _chaserCells[i].Col),
                "Battle/Projectiles/Mobile/ProjectileMapsArrow", 1.9f,
                Color.White, damaging: true);
            _chasers[i].CollisionSize *= 0.72f;
        }
        DebugState = "Open city blocks generated; two map markers pursue through the street grid";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        (int Row, int Col) target = FindNearestOpenCell(battle.Player.Position);
        for (int i = 0; i < _chasers.Length; i++)
        {
            float speed = i == 0 ? 58f : 70f;
            Vector2 waypoint = _hasWaypoint[i]
                ? CellCenter(_waypoints[i].Row, _waypoints[i].Col)
                : CellCenter(_chaserCells[i].Row, _chaserCells[i].Col);
            Vector2 offset = waypoint - _chasers[i].Position;
            float distance = offset.Length();

            if (!_hasWaypoint[i] || distance <= speed * deltaTime + 0.5f)
            {
                if (_hasWaypoint[i])
                {
                    _chasers[i].Position = waypoint;
                    _chaserCells[i] = _waypoints[i];
                }
                int next = FindNextCell(_chaserCells[i].Row, _chaserCells[i].Col, target.Row, target.Col);
                _waypoints[i] = (next / Columns, next % Columns);
                _hasWaypoint[i] = next != _chaserCells[i].Row * Columns + _chaserCells[i].Col;
                waypoint = CellCenter(_waypoints[i].Row, _waypoints[i].Col);
                offset = waypoint - _chasers[i].Position;
                distance = offset.Length();
            }

            if (distance > 0.01f)
            {
                Vector2 direction = offset / distance;
                _chasers[i].Position += direction * MathF.Min(distance, speed * deltaTime);
                // Map marker texture points down at rotation zero.
                _chasers[i].Rotation = MathF.Atan2(direction.Y, direction.X) - MathHelper.PiOver2;
            }
        }
        DebugState = $"City grid {Columns}x{Rows}; buildings={CountBuildings()}; markers={_chasers.Length}; player street target=({target.Col},{target.Row})";
    }

    private void CreateBuildings(BattleContext battle)
    {
        for (int row = 0; row < Rows; row++)
        for (int col = 0; col < Columns; col++)
        {
            if (!_buildings[row, col]) continue;
            Vector2 center = CellCenter(row, col);
            float width = _cellWidth * (0.57f + Random.Shared.NextSingle() * 0.12f);
            float height = _cellHeight * (0.48f + Random.Shared.NextSingle() * 0.13f);
            Vector2 size = new(width, height);

            Visual(battle, center + new Vector2(2f, 3f), size + new Vector2(3f), new Color(48, 48, 48));
            Spawn(battle, new SolidBlockEntity(center, size)
            { Tint = new Color(112, 112, 112) });
            Visual(battle, new Vector2(center.X, center.Y - height * 0.27f),
                new Vector2(width * 0.78f, 2f), new Color(155, 155, 155));
            // Tiny monochrome windows give the rectangles a recognisable building silhouette.
            float windowY = center.Y + height * 0.02f;
            Visual(battle, new Vector2(center.X - width * 0.22f, windowY), new Vector2(3f, 4f), new Color(185, 185, 185));
            Visual(battle, new Vector2(center.X + width * 0.22f, windowY), new Vector2(3f, 4f), new Color(185, 185, 185));
        }
    }

    private (int Row, int Col) FindNearestOpenCell(Vector2 position)
    {
        (int Row, int Col) best = (Rows / 2, Columns / 2);
        float bestDistance = float.MaxValue;
        for (int row = 0; row < Rows; row++)
        for (int col = 0; col < Columns; col++)
        {
            if (_buildings[row, col]) continue;
            float d = Vector2.DistanceSquared(position, CellCenter(row, col));
            if (d < bestDistance)
            {
                bestDistance = d;
                best = (row, col);
            }
        }
        return best;
    }

    private int FindNextCell(int startRow, int startCol, int targetRow, int targetCol)
    {
        int start = startRow * Columns + startCol;
        int target = targetRow * Columns + targetCol;
        if (start == target) return start;

        int[] previous = new int[Rows * Columns];
        Array.Fill(previous, -1);
        bool[] seen = new bool[Rows * Columns];
        Queue<int> queue = new();
        queue.Enqueue(start);
        seen[start] = true;

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            if (current == target) break;
            int row = current / Columns;
            int col = current % Columns;
            Visit(row - 1, col, current);
            Visit(row + 1, col, current);
            Visit(row, col - 1, current);
            Visit(row, col + 1, current);
        }

        if (previous[target] < 0) return start;
        int step = target;
        while (previous[step] != start)
        {
            step = previous[step];
            if (step < 0) return start;
        }
        return step;

        void Visit(int row, int col, int from)
        {
            if (row < 0 || row >= Rows || col < 0 || col >= Columns || _buildings[row, col]) return;
            int next = row * Columns + col;
            if (seen[next]) return;
            seen[next] = true;
            previous[next] = from;
            queue.Enqueue(next);
        }
    }

    private int CountBuildings()
    {
        int count = 0;
        foreach (bool isBuilding in _buildings)
            if (isBuilding) count++;
        return count;
    }

    private Vector2 CellCenter(int row, int col) => new(
        _topLeft.X + (col + 0.5f) * _cellWidth,
        _topLeft.Y + (row + 0.5f) * _cellHeight);
}
