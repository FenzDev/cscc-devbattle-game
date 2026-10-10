using System;
using System.Linq;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;

/// <summary>
/// Owns collision-world setup, actor registration, synchronization,
/// solid-overlap correction, and player-versus-obstacle checks.
/// </summary>
public sealed class BattleCollisionController
{
    private const float SpatialHashCellSize = 64f;

    private readonly BattleContext _battle;
    private readonly BattleDamageSystem _damage;
    private readonly CollisionWorld2D _world;
    private readonly BattleCollisionSystem _collisionSystem;

    public BattleCollisionController(
        BattleContext battle,
        BattleDamageSystem damage)
    {
        _battle = battle ?? throw new ArgumentNullException(nameof(battle));
        _damage = damage ?? throw new ArgumentNullException(nameof(damage));

        Layer defaultLayer = CreateSpatialLayer();
        Layer playerLayer = CreateSpatialLayer();
        Layer obstaclesLayer = CreateSpatialLayer();
        Layer solidLayer = CreateSpatialLayer();

        _world = new CollisionWorld2D(defaultLayer);
        _world.AddLayer("player", playerLayer);
        _world.AddLayer("obstacles", obstaclesLayer);
        _world.AddLayer("solid", solidLayer);

        _world.EnableCollisionBetweenLayers("player", "obstacles");
        _world.EnableCollisionBetweenLayers("player", "solid");

        _collisionSystem = new BattleCollisionSystem(_world);
    }

    public int RegisteredEntityCount => _battle.Entities.Count(
        entity => _world.Contains(entity));

    public void RegisterInitialEntities()
    {
        foreach (Entity entity in _battle.Entities)
            RegisterEntity(entity);
    }

    public void RefreshRegistrations()
    {
        foreach (Entity entity in _battle.Entities)
        {
            bool shouldBeRegistered =
                entity.Enabled &&
                !entity.DestroyRequested &&
                entity.CollisionMode != CollisionMode.None;

            bool isRegistered = _world.Contains(entity);

            if (!shouldBeRegistered)
            {
                if (isRegistered)
                    _collisionSystem.Unregister(entity);

                continue;
            }

            if (!isRegistered)
                RegisterEntity(entity);
        }
    }

    private void RegisterEntity(Entity entity)
    {
        if (!entity.Enabled ||
            entity.DestroyRequested ||
            entity.CollisionMode == CollisionMode.None)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(entity.CollisionLayer))
        {
            throw new InvalidOperationException(
                $"Entity {entity.Id} has collision enabled but no CollisionLayer.");
        }

        _collisionSystem.Register(entity, entity.CollisionLayer);
    }

    public void Synchronize()
    {
        _collisionSystem.Synchronize();
        _world.RebuildDynamicLayers();
    }

    public void ResolvePlayerSolidCollisions()
    {
        const int maxIterations = 8;
        PlayerEntity player = _battle.Player;

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            bool corrected = false;

            foreach (BattleCollisionHit hit in _collisionSystem.Query(player, "solid"))
            {
                if (!hit.GeometricResult.HasValue)
                    continue;

                Vector2 mtv = hit.GeometricResult.Value.MinimumTranslationVector;
                if (mtv.LengthSquared() < 0.000001f)
                    continue;

                // Player is expected to be a root entity here; Position is local/world.
                player.Position += mtv;

                TransformSystem.ResolveAll(_battle.Entities);
                Synchronize();

                corrected = true;
                break; // Resolve one overlap, then query the updated world again.
            }

            if (!corrected)
                break;
        }
    }

    public void CheckPlayerObstacleCollisions()
    {
        foreach (BattleCollisionHit hit in
                 _collisionSystem.Query(_battle.Player, "obstacles"))
        {
            HandlePlayerCollision(hit);
        }
    }

    private void HandlePlayerCollision(BattleCollisionHit hit)
    {
        Entity other = hit.B;

        if (other.ContactDamage > 0f)
            _damage.TryDamagePlayer(other.ContactDamage);

        if (other.DestroyOnPlayerContact)
            other.DestroyRequested = true;
    }

    public void RemoveDestroyedEntities()
    {
        foreach (Entity entity in _battle.Entities.ToArray())
        {
            if (!entity.DestroyRequested)
                continue;

            if (_world.Contains(entity))
                _world.Remove(entity);

            _battle.Remove(entity);
        }
    }

    private static Layer CreateSpatialLayer()
    {
        return new Layer(
            new SpatialHash(
                new SizeF(SpatialHashCellSize, SpatialHashCellSize)));
    }
}
