using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public sealed class BattleContext
{
    private readonly List<Entity> _entities = [];

    public IReadOnlyList<Entity> Entities =>
        _entities;

    public PlayerEntity Player { get; }

    public float PlayerHealth { get; set; }

    public BattleArena Arena { get; }

    public BattleWave? CurrentWave { get; set; }

    public TimersManager Timers { get; }

    public BattleContext(TimersManager timers)
    {
        Timers = timers;

        Player = new PlayerEntity();

        Arena = new BattleArena(
            this,
            new Vector2(400f, 400f),
            new Vector2(320f, 240f),
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