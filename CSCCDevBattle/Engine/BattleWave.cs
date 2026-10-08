using System;
using System.Collections.Generic;

public abstract class BattleWave
{
    private readonly HashSet<Entity> _spawnedEntities =
        new(ReferenceEqualityComparer.Instance);

    private TimersManager.Timer? _durationTimer;

    public bool Started { get; private set; }

    public bool Finished { get; private set; }

    public virtual float Duration =>
        float.PositiveInfinity;


    // ============================================================
    // Lifecycle
    // ============================================================

    public void Start(BattleContext battle)
    {
        ArgumentNullException.ThrowIfNull(battle);

        if (Started)
            return;

        Started = true;
        Finished = false;

        _spawnedEntities.Clear();

        // --------------------------------------------------------
        // Duration
        // --------------------------------------------------------

        if (!float.IsPositiveInfinity(Duration))
        {
            if (Duration <= 0f)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name}.Duration must be " +
                    "greater than zero.");
            }

            _durationTimer =
                battle.Timers.StartAfter(
                    this,
                    Duration);
        }
        else
        {
            _durationTimer = null;
        }

        OnStart(battle);
    }


    public void Update(
        BattleContext battle,
        float deltaTime)
    {
        ArgumentNullException.ThrowIfNull(battle);

        if (!Started || Finished)
            return;

        // --------------------------------------------------------
        // Duration reached
        // --------------------------------------------------------

        if (_durationTimer != null &&
            _durationTimer.HasElapsed())
        {
            Finish();

            return;
        }

        OnUpdate(
            battle,
            deltaTime);
    }


    public void End(BattleContext battle)
    {
        ArgumentNullException.ThrowIfNull(battle);

        if (!Started)
            return;

        try
        {
            OnEnd(battle);

            // Mark all entities created by this wave for deletion.
            foreach (Entity entity in _spawnedEntities)
            {
                if (!entity.DestroyRequested)
                    entity.DestroyRequested = true;
            }
        }
        finally
        {
            _durationTimer?.Stop();
            _durationTimer = null;

            _spawnedEntities.Clear();

            Started = false;
        }
    }


    // ============================================================
    // Derived wave
    // ============================================================

    protected virtual void OnStart(
        BattleContext battle)
    {
    }

    protected virtual void OnUpdate(
        BattleContext battle,
        float deltaTime)
    {
    }

    protected virtual void OnEnd(
        BattleContext battle)
    {
    }


    // ============================================================
    // Wave control
    // ============================================================

    protected void Finish()
    {
        Finished = true;
    }


    // ============================================================
    // Spawn
    // ============================================================

    protected T Spawn<T>(
        BattleContext battle,
        T entity,
        params Behaviour[] behaviours)
        where T : Entity
    {
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentNullException.ThrowIfNull(entity);

        foreach (Behaviour behaviour in behaviours)
        {
            ArgumentNullException.ThrowIfNull(behaviour);

            entity.Behaviours.Add(behaviour);
        }

        battle.Spawn(entity);

        _spawnedEntities.Add(entity);

        return entity;
    }
}