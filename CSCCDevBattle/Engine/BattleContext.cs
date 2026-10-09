using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public sealed class BattleContext
{
    private readonly List<Entity> _entities = [];

    public IReadOnlyList<Entity> Entities =>
        _entities;

    public PlayerEntity Player { get; }

    public string PlayerName { get; set; } = "PLAYER";

    public float PlayerMaxHealth { get; set; } = 20f;

    public float PlayerHealth { get; set; } = 20f;

    public bool PlayerMovementEnabled { get; set; }

    public BattleArena Arena { get; }

    public BattleWave? CurrentWave { get; set; }

    public TimersManager Timers { get; }

    public BattleContext(TimersManager timers)
    {
        Timers = timers;

        Player = new PlayerEntity();

        Arena = new BattleArena(
            this,
            new Vector2(Game1.BASE_SCREEN_WIDTH / 2f, Game1.BASE_SCREEN_WIDTH / 2f),
            new Vector2(240f, 240f),
            4f);

        Player.Position =
            Arena.Center;

        Spawn(Player);
    }

    public void Spawn(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_entities.Contains(entity))
            throw new InvalidOperationException(
                $"Entity {entity.Id} is already in the battle.");

        _entities.Add(entity);
    }

    public void Remove(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _entities.Remove(entity);
    }
}