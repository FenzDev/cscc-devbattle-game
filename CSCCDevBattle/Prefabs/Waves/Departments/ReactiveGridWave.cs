using System;
using Microsoft.Xna.Framework;

/// <summary>Five-by-five monochrome grid: cells behind the player and occasional random cells arm before damaging the player.</summary>
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

    public ReactiveGridWave() => Duration = 14f;
    public override Vector2 ArenaSize => new(280f, 250f);

    protected override void OnStart(BattleContext battle)
    {
        _topLeft = battle.Arena.InnerMinimum;
        _cellWidth = (battle.Arena.InnerMaximum.X - _topLeft.X) / GridWidth;
        _cellHeight = (battle.Arena.InnerMaximum.Y - _topLeft.Y) / GridHeight;
        _armedCells = 0;
        _ballsLaunched = 0;

        for (int row = 0; row < GridHeight; row++)
        for (int col = 0; col < GridWidth; col++)
        {
            DepartmentVisualEntity visual = Visual(battle, CellCenter(row, col),
                new Vector2(_cellWidth - 2f, _cellHeight - 2f), new Color(82, 82, 82, 62));
            visual.CollisionMode = CollisionMode.None;
            visual.CollisionLayer = "obstacles";
            visual.ContactDamage = 1f;
            _cells[row, col] = new GridCell { Entity = visual, State = 0, Remaining = 0f };
        }

        for (int col = 0; col <= GridWidth; col++)
        {
            float x = _topLeft.X + col * _cellWidth;
            Visual(battle, new Vector2(x, _topLeft.Y + battle.Arena.Size.Y / 2f),
                new Vector2(1.2f, battle.Arena.Size.Y - 10f), new Color(155, 155, 155, 145));
        }
        for (int row = 0; row <= GridHeight; row++)
        {
            float y = _topLeft.Y + row * _cellHeight;
            Visual(battle, new Vector2(_topLeft.X + battle.Arena.Size.X / 2f, y),
                new Vector2(battle.Arena.Size.X - 10f, 1.2f), new Color(155, 155, 155, 145));
        }

        _previousPlayerCell = PlayerCell(battle.Player.Position);
        _randomCellTimer = 0.78f;
        _ballTimer = 0.5f;
        DebugState = "Monochrome 5x5 board; departed and random cells warn in gray, then briefly flash red/white when dangerous";
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
            _randomCellTimer += 1.25f + Random.Shared.NextSingle() * 0.45f;
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
        for (int col = 0; col < GridWidth; col++)
        {
            GridCell cell = _cells[row, col];
            if (cell.State == 0) continue;
            cell.Remaining -= deltaTime;
            if (cell.State == 1 && cell.Remaining <= 0f)
            {
                cell.State = 2;
                cell.Remaining = 0.68f;
                cell.Entity.CollisionMode = CollisionMode.RotatedBox;
                cell.Entity.CollisionLayer = "obstacles";
                cell.Entity.Tint = Color.White;
            }
            else if (cell.State == 2)
            {
                // Mostly gray/white. Red appears only for a short portion of the dangerous flash.
                bool redFlash = ((int)(Elapsed * 12f + row * 2 + col) % 5) == 0;
                cell.Entity.Tint = redFlash ? new Color(220, 70, 70) : Color.White;
                if (cell.Remaining <= 0f)
                {
                    cell.State = 0;
                    cell.Entity.CollisionMode = CollisionMode.None;
                    cell.Entity.Tint = new Color(82, 82, 82, 62);
                }
            }
        }

        _ballTimer -= deltaTime;
        if (_ballTimer <= 0f)
        {
            _ballTimer += 0.95f;
            LaunchSlidingBall(battle);
        }
        DebugState = $"Grid hazards={_armedCells}; player cell=({playerCell.Col},{playerCell.Row}); sliding balls={_ballsLaunched}";
    }

    private void ArmCell(int row, int col)
    {
        if (row < 0 || row >= GridHeight || col < 0 || col >= GridWidth) return;
        GridCell cell = _cells[row, col];
        if (cell.State == 2) return;
        cell.State = 1;
        cell.Remaining = 0.46f;
        cell.Entity.CollisionMode = CollisionMode.None;
        cell.Entity.Tint = new Color(188, 188, 188, 190);
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
        float speed = 142f + Random.Shared.NextSingle() * 38f;
        SpriteProjectile(battle, origin, asset, direction * speed, 1.7f,
            Color.White, lifetime: 3.3f, damaging: true,
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
