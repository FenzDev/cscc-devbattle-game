using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

/// <summary>Low-opacity numeric glyphs briefly flash white before their hitboxes activate.</summary>
public sealed class NumericWarningWave : DepartmentWaveBase
{
    private sealed class WarningNumber
    {
        public int Value;
        public float Age;
        public float WarnFor;
        public float ActiveFor;
        public DepartmentVisualEntity Hitbox = null!;
        public List<DepartmentVisualEntity> Pixels = [];
        public bool Activated;
    }

    private static readonly string[][] Digits =
    {
        new[] { "111", "101", "101", "101", "111" },
        new[] { "010", "110", "010", "010", "111" },
        new[] { "111", "001", "111", "100", "111" },
        new[] { "111", "001", "111", "001", "111" },
        new[] { "101", "101", "111", "001", "001" },
        new[] { "111", "100", "111", "001", "111" },
        new[] { "111", "100", "111", "101", "111" },
        new[] { "111", "001", "010", "010", "010" },
        new[] { "111", "101", "111", "101", "111" },
        new[] { "111", "101", "111", "001", "111" }
    };

    private readonly List<WarningNumber> _numbers = [];
    private float _spawnTimer;
    private int _spawned;

    public NumericWarningWave() => Duration = 10.5f;
    public override Vector2 ArenaSize => new(270f, 240f);

    protected override void OnStart(BattleContext battle)
    {
        _numbers.Clear();
        _spawnTimer = 0.28f;
        _spawned = 0;
        DebugState = "Numeric predictions appear dimly; white flash marks the damage instant";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        _spawnTimer -= deltaTime;
        if (_spawnTimer <= 0f)
        {
            _spawnTimer += 1.42f;
            SpawnNumber(battle);
        }

        foreach (WarningNumber number in _numbers.ToArray())
        {
            number.Age += deltaTime;
            if (!number.Activated && number.Age >= number.WarnFor)
            {
                number.Activated = true;
                number.Hitbox.CollisionMode = CollisionMode.RotatedBox;
                number.Hitbox.CollisionLayer = "obstacles";
                number.Hitbox.ContactDamage = 1f;
                number.Hitbox.Tint = Color.Transparent;
                foreach (DepartmentVisualEntity pixel in number.Pixels)
                    pixel.Tint = Color.White;
            }

            if (number.Activated && number.Age >= number.WarnFor + number.ActiveFor)
            {
                number.Hitbox.DestroyRequested = true;
                foreach (DepartmentVisualEntity pixel in number.Pixels)
                    pixel.DestroyRequested = true;
                _numbers.Remove(number);
            }
        }

        DebugState = $"Digits spawned={_spawned}; simultaneous warnings={_numbers.Count}";
    }

    private void SpawnNumber(BattleContext battle)
    {
        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;
        Vector2 center = new(
            min.X + 22f + Random.Shared.NextSingle() * (max.X - min.X - 44f),
            min.Y + 26f + Random.Shared.NextSingle() * (max.Y - min.Y - 52f));
        float rotation = Random.Shared.NextSingle() * 1.1f - 0.55f;
        int value = Random.Shared.Next(10);
        Color dim = new(155, 165, 190, 70);
        var number = new WarningNumber
        {
            Value = value,
            Age = 0f,
            WarnFor = 0.82f + Random.Shared.NextSingle() * 0.24f,
            ActiveFor = 0.22f,
            Hitbox = Visual(battle, center, new Vector2(28f, 36f), Color.Transparent, rotation),
            Pixels = [],
            Activated = false
        };

        number.Hitbox.CollisionMode = CollisionMode.None;
        number.Hitbox.ContactDamage = 1f;
        number.Hitbox.CollisionLayer = "obstacles";

        string[] glyph = Digits[value];
        const float pitch = 5f;
        for (int row = 0; row < glyph.Length; row++)
        {
            for (int col = 0; col < glyph[row].Length; col++)
            {
                if (glyph[row][col] != '1') continue;
                Vector2 local = new((col - 1) * pitch, (row - 2) * pitch);
                Vector2 position = center + Rotate(local, rotation);
                number.Pixels.Add(Visual(battle, position, new Vector2(4f), dim, rotation));
            }
        }

        _numbers.Add(number);
        _spawned++;
    }
}
