using System;
using Microsoft.Xna.Framework;

/// <summary>Five-by-five grid cells arm behind the player and at random; drifting balls cut across the board.</summary>
public sealed class ReactiveGridWave : DepartmentWaveBase
{
    private sealed class GridCell
    {
        public DepartmentVisualEntity Entity = null!;
        public int State; // 0 safe, 1 warning, 2 damaging
        public float Remaining;
    }

    private const int GridWidth = 5;
    private const int GridHeight = 5;
    private readonly GridCell[,] _cells = new GridCell[GridHeight, GridWidth];
    private float _cellWidth;
    private float _cellHeight;
    private Vector2 _topLeft;
    private (int Row, int Col) _previousPlayerCell;
    private float _randomCellTimer;
    private float _ballTimer;
    private int _ballsLaunched;
    private int _armedCells;

    public ReactiveGridWave() => Duration = 11.5f;
    public override Vector2 ArenaSize => new(280f, 250f);

    protected override void OnStart(BattleContext battle)
    {
        _topLeft = battle.Arena.InnerMinimum;
        _cellWidth = (battle.Arena.InnerMaximum.X - _topLeft.X) / GridWidth;
        _cellHeight = (battle.Arena.InnerMaximum.Y - _topLeft.Y) / GridHeight;
        _armedCells = 0;
        _ballsLaunched = 0;

        for (int row = 0; row < GridHeight; row++)
        {
            for (int col = 0; col < GridWidth; col++)
            {
                Vector2 center = CellCenter(row, col);
                DepartmentVisualEntity cellVisual = Visual(battle, center,
                    new Vector2(_cellWidth - 2f, _cellHeight - 2f),
                    new Color(75, 100, 145, 65));
                cellVisual.CollisionMode = CollisionMode.None;
                cellVisual.CollisionLayer = "obstacles";
                cellVisual.ContactDamage = 1f;
                _cells[row, col] = new GridCell { Entity = cellVisual, State = 0, Remaining = 0f };
            }
        }

        // Cell dividers are drawn over the translucent cell fill.
        for (int col = 0; col <= GridWidth; col++)
        {
            float x = _topLeft.X + col * _cellWidth;
            Visual(battle, new Vector2(x, _topLeft.Y + battle.Arena.Size.Y / 2f),
                new Vector2(1.5f, battle.Arena.Size.Y - 10f), new Color(115, 145, 190, 170));
        }
        for (int row = 0; row <= GridHeight; row++)
        {
            float y = _topLeft.Y + row * _cellHeight;
            Visual(battle, new Vector2(_topLeft.X + battle.Arena.Size.X / 2f, y),
                new Vector2(battle.Arena.Size.X - 10f, 1.5f), new Color(115, 145, 190, 170));
        }

        _previousPlayerCell = PlayerCell(battle.Player.Position);
        _randomCellTimer = 0.78f;
        _ballTimer = 0.48f;
        DebugState = "Reactive 5x5 board initialized; departed cells and random cells telegraph before damage";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        (int Row, int Col) playerCell = PlayerCell(battle.Player.Position);
        if (playerCell != _previousPlayerCell)
        {
            ArmCell(_previousPlayerCell.Row, _previousPlayerCell.Col);
            _previousPlayerCell = playerCell;
        }

        _randomCellTimer -= deltaTime;
        if (_randomCellTimer <= 0f)
        {
            _randomCellTimer += 1.18f + Random.Shared.NextSingle() * 0.35f;
            for (int attempt = 0; attempt < 30; attempt++)
            {
                int row = Random.Shared.Next(GridHeight);
                int col = Random.Shared.Next(GridWidth);
                if (row == playerCell.Row && col == playerCell.Col) continue;
                if (_cells[row, col].State != 0) continue;
                ArmCell(row, col);
                break;
            }
        }

        for (int row = 0; row < GridHeight; row++)
        {
            for (int col = 0; col < GridWidth; col++)
            {
                GridCell cell = _cells[row, col];
                if (cell.State == 0) continue;
                cell.Remaining -= deltaTime;

                if (cell.State == 1 && cell.Remaining <= 0f)
                {
                    cell.State = 2;
                    cell.Remaining = 0.72f;
                    cell.Entity.CollisionMode = CollisionMode.RotatedBox;
                    cell.Entity.CollisionLayer = "obstacles";
                    cell.Entity.Tint = Color.White;
                }
                else if (cell.State == 2)
                {
                    float flash = 0.5f + 0.5f * MathF.Sin(Elapsed * 22f + row + col);
                    cell.Entity.Tint = Color.Lerp(new Color(255, 55, 90), Color.White, flash);
                    if (cell.Remaining <= 0f)
                    {
                        cell.State = 0;
                        cell.Entity.CollisionMode = CollisionMode.None;
                        cell.Entity.Tint = new Color(75, 100, 145, 65);
                    }
                }
            }
        }

        _ballTimer -= deltaTime;
        if (_ballTimer <= 0f)
        {
            _ballTimer += 0.88f;
            LaunchSlidingBall(battle);
        }

        DebugState = $"Grid hazards={_armedCells}; cell=({playerCell.Col},{playerCell.Row}); sliding balls={_ballsLaunched}";
    }

    private void ArmCell(int row, int col)
    {
        if (row < 0 || row >= GridHeight || col < 0 || col >= GridWidth)
            return;
        GridCell cell = _cells[row, col];
        if (cell.State == 2)
            return;
        cell.State = 1;
        cell.Remaining = 0.43f;
        cell.Entity.CollisionMode = CollisionMode.None;
        cell.Entity.Tint = new Color(255, 185, 65, 210);
        _armedCells++;
    }

    private void LaunchSlidingBall(BattleContext battle)
    {
        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;
        int edge = Random.Shared.Next(4);
        Vector2 origin = edge switch
        {
            0 => new Vector2(min.X - 8f, min.Y + Random.Shared.NextSingle() * (max.Y - min.Y)),
            1 => new Vector2(max.X + 8f, min.Y + Random.Shared.NextSingle() * (max.Y - min.Y)),
            2 => new Vector2(min.X + Random.Shared.NextSingle() * (max.X - min.X), min.Y - 8f),
            _ => new Vector2(min.X + Random.Shared.NextSingle() * (max.X - min.X), max.Y + 8f)
        };
        Vector2 target = battle.Player.Position + new Vector2(
            Random.Shared.NextSingle() * 60f - 30f,
            Random.Shared.NextSingle() * 60f - 30f);
        Vector2 direction = Rotate(Direction(origin, target), Random.Shared.NextSingle() * 0.5f - 0.25f);
        string asset = _ballsLaunched % 2 == 0
            ? "Battle/Projectiles/ProjectileBall"
            : "Battle/Projectiles/ProjectileUnroundedBall";
        float speed = 126f + Random.Shared.NextSingle() * 44f;
        SpriteProjectile(battle, origin, asset, direction * speed, 1.7f,
            _ballsLaunched % 2 == 0 ? new Color(255, 215, 100) : new Color(160, 105, 255),
            lifetime: 3.3f, damaging: true,
            redirectAt: 0.42f, redirectRadians: Random.Shared.NextSingle() * 1.1f - 0.55f,
            pixelPerfect: true);
        _ballsLaunched++;
    }

    private (int Row, int Col) PlayerCell(Vector2 position) =>
        (CellIndex(position.Y, _topLeft.Y, _cellHeight, GridHeight),
         CellIndex(position.X, _topLeft.X, _cellWidth, GridWidth));

    private Vector2 CellCenter(int row, int col) => new(
        _topLeft.X + (col + 0.5f) * _cellWidth,
        _topLeft.Y + (row + 0.5f) * _cellHeight);
}
