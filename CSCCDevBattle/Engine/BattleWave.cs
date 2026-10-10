using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public abstract class BattleWave
{
    private readonly HashSet<Entity> _spawnedEntities = [];
    private bool _ended;

    public bool Finished { get; private set; }

    public float Elapsed { get; private set; }

    // Automatic completion time, in seconds.
    // Use float.PositiveInfinity for a manually completed wave.
    public float Duration { get; set; } = 5f;

    // Default combat arena: a 240 x 240 square.
    // Override these in waves that need a different layout.
    public virtual Vector2 ArenaSize => new(240f, 240f);

    // Null means use the screen center.
    public virtual Vector2? ArenaCenter => null;

    public BossPose BossPose { get; set; }
        = BossPose.Pockets;

    public List<string> BossLines { get; } = [];


    public void Start(BattleContext battle)
    {
        ArgumentNullException.ThrowIfNull(battle);

        if (float.IsNaN(Duration) || Duration < 0f)
        {
            throw new InvalidOperationException(
                $"{GetType().Name} has an invalid Duration.");
        }

        Finished = false;
        Elapsed = 0f;
        _ended = false;
        _spawnedEntities.Clear();

        OnStart(battle);

        if (!Finished && Elapsed >= Duration)
            Finish();
    }

    public void Update(BattleContext battle, float deltaTime)
    {
        if (Finished || _ended)
            return;

        deltaTime = MathF.Max(0f, deltaTime);
        Elapsed += deltaTime;

        OnUpdate(battle, deltaTime);

        if (!Finished && Elapsed >= Duration)
            Finish();
    }

    public void End(BattleContext battle)
    {
        if (_ended)
            return;

        _ended = true;

        OnEnd(battle);

        // Any entity spawned through Spawn() belongs to this wave.
        // Flag it for removal; the scene handles collection changes.
        foreach (Entity entity in _spawnedEntities)
            entity.DestroyRequested = true;

        _spawnedEntities.Clear();
    }

    public void Finish()
    {
        Finished = true;
    }

    protected virtual void OnStart(BattleContext battle)
    {
    }

    protected abstract void OnUpdate(
        BattleContext battle,
        float deltaTime);

    protected virtual void OnEnd(BattleContext battle)
    {
    }

    protected T Spawn<T>(BattleContext battle, T entity, params Behaviour[] behaviours)
        where T : Entity
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(entity);

        battle.Spawn(entity);
        _spawnedEntities.Add(entity);

        foreach (var behaviour in behaviours)
        {
            entity.Behaviours.Add(behaviour);
        }
        
        return entity;
    }
}