using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

/// <summary>Carves a connected grid maze; two map markers use the openings to chase the player.</summary>
public sealed class CityMazeChaseWave : DepartmentWaveBase
{
    private const int Columns = 7;
    private const int Rows = 6;
    private const float WallThickness = 4f;

    private readonly bool[,] _openRight = new bool[Rows, Columns];
    private readonly bool[,] _openDown = new bool[Rows, Columns];
    private readonly bool[,] _visited = new bool[Rows, Columns];
    private readonly DepartmentVisualEntity[] _chasers = new DepartmentVisualEntity[2];
    private readonly (int Row, int Col)[] _chaserCells = new (int, int)[2];
    private readonly (int Row, int Col)[] _waypointCells = new (int, int)[2];
    private readonly bool[] _hasWaypoint = new bool[2];
    private float _cellWidth;
    private float _cellHeight;
    private Vector2 _topLeft;

    public CityMazeChaseWave() => Duration = 11f;
    public override Vector2 ArenaSize => new(285f, 260f);

    protected override void OnStart(BattleContext battle)
    {
        Array.Clear(_openRight, 0, _openRight.Length);
        Array.Clear(_openDown, 0, _openDown.Length);
        Array.Clear(_visited, 0, _visited.Length);
        _topLeft = battle.Arena.InnerMinimum;
        _cellWidth = (battle.Arena.InnerMaximum.X - _topLeft.X) / Columns;
        _cellHeight = (battle.Arena.InnerMaximum.Y - _topLeft.Y) / Rows;

        CarveMaze();
        CreateMazeWalls(battle);

        int playerRow = Rows / 2;
        int playerCol = Columns / 2;
        battle.Player.Position = CellCenter(playerRow, playerCol);

        _chaserCells[0] = (0, 0);
        _chaserCells[1] = (Rows - 1, Columns - 1);
        _hasWaypoint[0] = false;
        _hasWaypoint[1] = false;
        for (int i = 0; i < _chasers.Length; i++)
        {
            _chasers[i] = Sprite(battle, CellCenter(_chaserCells[i].Row, _chaserCells[i].Col),
                "Battle/Projectiles/Mobile/ProjectileMapsArrow", 1.65f,
                i == 0 ? new Color(255, 85, 100) : new Color(255, 205, 75),
                damaging: true);
            _chasers[i].CollisionSize *= 0.8f;
        }

        DebugState = "Connected city maze generated; two map markers are pursuing through corridors";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        int targetRow = CellIndex(battle.Player.Position.Y, _topLeft.Y, _cellHeight, Rows);
        int targetCol = CellIndex(battle.Player.Position.X, _topLeft.X, _cellWidth, Columns);

        for (int i = 0; i < _chasers.Length; i++)
        {
            (int Row, int Col) current = _chaserCells[i];
            float speed = i == 0 ? 48f : 62f;
            Vector2 currentWaypoint = CellCenter(_waypointCells[i].Row, _waypointCells[i].Col);
            Vector2 toWaypoint = currentWaypoint - _chasers[i].Position;
            float distanceToWaypoint = toWaypoint.Length();

            // Finish the current corridor before choosing another route so a target change
            // cannot make the marker cut diagonally through a maze wall.
            if (!_hasWaypoint[i] || distanceToWaypoint <= speed * deltaTime + 0.5f)
            {
                if (_hasWaypoint[i])
                {
                    _chasers[i].Position = currentWaypoint;
                    _chaserCells[i] = _waypointCells[i];
                }

                current = _chaserCells[i];
                int nextIndex = FindNextCell(current.Row, current.Col, targetRow, targetCol);
                _waypointCells[i] = (nextIndex / Columns, nextIndex % Columns);
                _hasWaypoint[i] = true;
                currentWaypoint = CellCenter(_waypointCells[i].Row, _waypointCells[i].Col);
                toWaypoint = currentWaypoint - _chasers[i].Position;
                distanceToWaypoint = toWaypoint.Length();
            }

            if (distanceToWaypoint > 0.01f)
            {
                Vector2 direction = toWaypoint / distanceToWaypoint;
                _chasers[i].Position += direction * MathF.Min(distanceToWaypoint, speed * deltaTime);
                _chasers[i].Rotation = MathF.Atan2(direction.Y, direction.X) + MathHelper.PiOver2;
            }
        }

        DebugState = $"Maze {Columns}x{Rows}; player cell=({targetCol},{targetRow}); two route-following markers";
    }

    private void CarveMaze()
    {
        List<(int Row, int Col)> stack = [(Rows / 2, Columns / 2)];
        _visited[Rows / 2, Columns / 2] = true;

        while (stack.Count > 0)
        {
            (int Row, int Col) current = stack[^1];
            List<(int Row, int Col)> options = [];
            if (current.Col > 0 && !_visited[current.Row, current.Col - 1]) options.Add((current.Row, current.Col - 1));
            if (current.Col < Columns - 1 && !_visited[current.Row, current.Col + 1]) options.Add((current.Row, current.Col + 1));
            if (current.Row > 0 && !_visited[current.Row - 1, current.Col]) options.Add((current.Row - 1, current.Col));
            if (current.Row < Rows - 1 && !_visited[current.Row + 1, current.Col]) options.Add((current.Row + 1, current.Col));

            if (options.Count == 0)
            {
                stack.RemoveAt(stack.Count - 1);
                continue;
            }

            (int Row, int Col) next = options[Random.Shared.Next(options.Count)];
            if (next.Row == current.Row)
                _openRight[current.Row, Math.Min(next.Col, current.Col)] = true;
            else
                _openDown[Math.Min(next.Row, current.Row), current.Col] = true;

            _visited[next.Row, next.Col] = true;
            stack.Add(next);
        }
    }

    private void CreateMazeWalls(BattleContext battle)
    {
        // Maze walls are solid, so normal engine collision resolution pushes the player away.
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Columns - 1; col++)
            {
                if (_openRight[row, col]) continue;
                Vector2 position = new(
                    _topLeft.X + (col + 1) * _cellWidth,
                    _topLeft.Y + (row + 0.5f) * _cellHeight);
                Spawn(battle, new SolidBlockEntity(position,
                    new Vector2(WallThickness, _cellHeight + WallThickness * 0.25f))
                    { Tint = new Color(85, 145, 180) });
            }
        }

        for (int row = 0; row < Rows - 1; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                if (_openDown[row, col]) continue;
                Vector2 position = new(
                    _topLeft.X + (col + 0.5f) * _cellWidth,
                    _topLeft.Y + (row + 1) * _cellHeight);
                Spawn(battle, new SolidBlockEntity(position,
                    new Vector2(_cellWidth + WallThickness * 0.25f, WallThickness))
                    { Tint = new Color(85, 145, 180) });
            }
        }
    }

    private int FindNextCell(int startRow, int startCol, int targetRow, int targetCol)
    {
        int startIndex = startRow * Columns + startCol;
        int targetIndex = targetRow * Columns + targetCol;
        if (startIndex == targetIndex)
            return startIndex;

        int[,] previous = new int[Rows, Columns];
        bool[,] seen = new bool[Rows, Columns];
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Columns; c++)
                previous[r, c] = -1;

        Queue<(int Row, int Col)> queue = new();
        queue.Enqueue((startRow, startCol));
        seen[startRow, startCol] = true;

        while (queue.Count > 0)
        {
            (int Row, int Col) cell = queue.Dequeue();
            int parentIndex = cell.Row * Columns + cell.Col;
            if (parentIndex == targetIndex)
                break;

            Visit(cell.Row, cell.Col - 1, CanMove(cell.Row, cell.Col, cell.Row, cell.Col - 1), parentIndex);
            Visit(cell.Row, cell.Col + 1, CanMove(cell.Row, cell.Col, cell.Row, cell.Col + 1), parentIndex);
            Visit(cell.Row - 1, cell.Col, CanMove(cell.Row, cell.Col, cell.Row - 1, cell.Col), parentIndex);
            Visit(cell.Row + 1, cell.Col, CanMove(cell.Row, cell.Col, cell.Row + 1, cell.Col), parentIndex);
        }

        if (previous[targetRow, targetCol] < 0)
            return startIndex;

        int step = targetIndex;
        while (previous[step / Columns, step % Columns] != startIndex)
        {
            int parent = previous[step / Columns, step % Columns];
            if (parent < 0)
                return startIndex;
            step = parent;
        }
        return step;

        void Visit(int row, int col, bool connected, int parentIndex)
        {
            if (!connected || row < 0 || row >= Rows || col < 0 || col >= Columns || seen[row, col])
                return;
            seen[row, col] = true;
            previous[row, col] = parentIndex;
            queue.Enqueue((row, col));
        }
    }

    private bool CanMove(int row, int col, int nextRow, int nextCol)
    {
        if (nextRow < 0 || nextRow >= Rows || nextCol < 0 || nextCol >= Columns)
            return false;
        if (nextRow == row)
            return nextCol < col ? _openRight[row, nextCol] : _openRight[row, col];
        return nextRow < row ? _openDown[nextRow, col] : _openDown[row, col];
    }

    private Vector2 CellCenter(int row, int col) => new(
        _topLeft.X + (col + 0.5f) * _cellWidth,
        _topLeft.Y + (row + 0.5f) * _cellHeight);
}
