using System;
using System.Linq;

/// <summary>
/// Runs entity behaviours and resolves entity transforms for a battle frame.
/// </summary>
public sealed class BattleEntitySystem
{
    private readonly BattleContext _battle;

    public BattleEntitySystem(BattleContext battle)
    {
        _battle = battle ?? throw new ArgumentNullException(nameof(battle));
    }

    public void UpdateBehaviours(float deltaTime)
    {
        // Snapshot both collections because behaviours may add behaviours,
        // spawn entities, or request destruction during this frame.
        foreach (Entity entity in _battle.Entities.ToArray())
        {
            if (!entity.Enabled || entity.DestroyRequested)
                continue;

            Behaviour[] behaviours = entity.Behaviours.ToArray();

            foreach (Behaviour behaviour in behaviours)
            {
                behaviour.Update(entity, _battle, deltaTime);
            }
        }
    }

    public void ResolveTransforms()
    {
        TransformSystem.ResolveAll(_battle.Entities);
    }
}
