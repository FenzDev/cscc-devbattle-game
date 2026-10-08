/// <summary>
/// Reusable per-entity logic.
///
/// Behaviour instances may be shared by many entities.
/// Therefore implementations should NOT store entity-specific
/// timers, positions, counters, etc. in instance fields.
/// </summary>
public abstract class Behaviour
{
    public abstract void Update(
        Entity entity,
        BattleContext battle,
        float deltaTime);
}