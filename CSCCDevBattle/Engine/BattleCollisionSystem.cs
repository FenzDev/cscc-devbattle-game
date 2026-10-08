using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;

/// <summary>
/// Collision system for the battle.
///
/// MonoGame.Extended is used for:
///     - layer filtering
///     - broadphase spatial queries
///     - ordinary geometric collision
///
/// Custom code is used for:
///     - pixel-perfect vs pixel-perfect
///     - pixel-perfect vs rotated box
/// </summary>
public sealed class BattleCollisionSystem
{
    private readonly CollisionWorld2D _world;

    private readonly PixelMaskCache _pixelMasks;

    private readonly Dictionary<
        Entity,
        string> _registeredLayers =
        new(ReferenceEqualityComparer.Instance);

    public BattleCollisionSystem(
        CollisionWorld2D world,
        PixelMaskCache? pixelMasks = null)
    {
        ArgumentNullException.ThrowIfNull(world);

        _world = world;
        _pixelMasks =
            pixelMasks ?? new PixelMaskCache();
    }


    // ============================================================
    // Registration
    // ============================================================

    public void Register(
        Entity entity,
        string layerName)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(layerName);

        if (entity.CollisionMode == CollisionMode.None)
        {
            throw new InvalidOperationException(
                $"Entity {entity.Id} has collision disabled.");
        }

        ValidateCollisionConfiguration(entity);

        if (_world.Contains(entity))
        {
            throw new InvalidOperationException(
                $"Entity {entity.Id} is already registered.");
        }

        SynchronizeShape(entity);

        _world.Insert(
            entity,
            layerName);

        _registeredLayers.Add(
            entity,
            layerName);
    }

    public void Unregister(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_world.Contains(entity))
        {
            _world.Remove(entity);
        }

        _registeredLayers.Remove(entity);
    }

    public void MoveToLayer(
        Entity entity,
        string layerName)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(layerName);

        _world.MoveToLayer(
            entity,
            layerName);

        _registeredLayers[entity] = layerName;
    }


    // ============================================================
    // Per-frame synchronization
    // ============================================================

    /// <summary>
    /// Rebuild collision shapes for every registered entity.
    ///
    /// Call AFTER TransformSystem.ResolveAll().
    /// </summary>
    public void Synchronize()
    {
        foreach (Entity entity in _registeredLayers.Keys)
        {
            if (!entity.Enabled)
                continue;

            ValidateCollisionConfiguration(entity);
            SynchronizeShape(entity);
        }
    }

    /// <summary>
    /// Call after Synchronize() and before collision queries.
    ///
    /// MonoGame.Extended 6.0 explicitly uses this as the
    /// broadphase synchronization point for dynamic layers.
    /// </summary>
    public void Rebuild()
    {
        _world.RebuildDynamicLayers();
    }


    // ============================================================
    // Shape synchronization
    // ============================================================

    private static void SynchronizeShape(
        Entity entity)
    {
        switch (entity.CollisionMode)
        {
            case CollisionMode.None:
            {
                entity.Shape = default;
                break;
            }

            case CollisionMode.RotatedBox:
            {
                entity.Shape =
                    new CollisionShape2D(
                        CreateWorldBox(entity));

                break;
            }

            case CollisionMode.PixelPerfect:
            {
                entity.Shape =
                    new CollisionShape2D(
                        CreateTextureAabb(entity));

                break;
            }

            default:
                throw new ArgumentOutOfRangeException();
        }
    }


    // ============================================================
    // Public query
    // ============================================================

    /// <summary>
    /// Queries one target layer for entities that actually collide
    /// with the given entity.
    ///
    /// IMPORTANT:
    /// QueryCandidates() is used instead of QueryCollisions()
    /// because pixel-perfect collision is not a MonoGame.Extended
    /// TryGetCollision() shape.
    /// </summary>
    public IEnumerable<BattleCollisionHit> Query(
        Entity source,
        string targetLayer)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetLayer);

        if (!source.Enabled)
            yield break;

        if (source.CollisionMode == CollisionMode.None)
            yield break;

        if (!_world.Contains(source))
            throw new InvalidOperationException(
                $"Entity {source.Id} is not registered.");

        var seen =
            new HashSet<Entity>(
                ReferenceEqualityComparer.Instance);

        foreach (ICollisionActor candidateActor
            in _world.QueryCandidates(
                source,
                targetLayer))
        {
            if (candidateActor is not Entity candidate)
                continue;

            if (ReferenceEquals(source, candidate))
                continue;

            if (!candidate.Enabled)
                continue;

            if (!seen.Add(candidate))
                continue;

            if (!Intersects(
                source,
                candidate,
                out bool pixelPerfect,
                out CollisionResult2D? geometricResult))
            {
                continue;
            }

            yield return new BattleCollisionHit(
                source,
                candidate,
                pixelPerfect,
                geometricResult);
        }
    }


    // ============================================================
    // Exact narrow phase
    // ============================================================

    private bool Intersects(
        Entity a,
        Entity b,
        out bool pixelPerfect,
        out CollisionResult2D? geometricResult)
    {
        pixelPerfect =
            a.CollisionMode == CollisionMode.PixelPerfect
            || b.CollisionMode == CollisionMode.PixelPerfect;

        geometricResult = null;

        if (a.CollisionMode == CollisionMode.None ||
            b.CollisionMode == CollisionMode.None)
        {
            return false;
        }

        // --------------------------------------------------------
        // Box <-> Box
        // --------------------------------------------------------

        if (a.CollisionMode == CollisionMode.RotatedBox &&
            b.CollisionMode == CollisionMode.RotatedBox)
        {
            if (!a.Shape.Intersects(b.Shape))
                return false;

            if (a.Shape.TryGetCollision(
                    b.Shape,
                    out CollisionResult2D result))
            {
                geometricResult = result;
            }

            return true;
        }

        // --------------------------------------------------------
        // Pixel <-> Pixel
        // --------------------------------------------------------

        if (a.CollisionMode == CollisionMode.PixelPerfect &&
            b.CollisionMode == CollisionMode.PixelPerfect)
        {
            return PixelVsPixel(a, b);
        }

        // --------------------------------------------------------
        // Pixel <-> Box
        // --------------------------------------------------------

        if (a.CollisionMode == CollisionMode.PixelPerfect &&
            b.CollisionMode == CollisionMode.RotatedBox)
        {
            return PixelVsBox(a, b);
        }

        // --------------------------------------------------------
        // Box <-> Pixel
        // --------------------------------------------------------

        if (a.CollisionMode == CollisionMode.RotatedBox &&
            b.CollisionMode == CollisionMode.PixelPerfect)
        {
            return PixelVsBox(b, a);
        }

        return false;
    }


    // ============================================================
    // Pixel <-> Pixel
    // ============================================================

    private bool PixelVsPixel(
        Entity a,
        Entity b)
    {
        if (a.Texture == null ||
            b.Texture == null)
        {
            return false;
        }

        PixelMask maskA =
            _pixelMasks.Get(a.Texture);

        PixelMask maskB =
            _pixelMasks.Get(b.Texture);

        Rectangle overlap =
            GetPixelOverlapBounds(a, b);

        if (overlap.IsEmpty)
            return false;

        for (int y = overlap.Top;
             y < overlap.Bottom;
             y++)
        {
            for (int x = overlap.Left;
                 x < overlap.Right;
                 x++)
            {
                Vector2 worldPoint =
                    new(
                        x + 0.5f,
                        y + 0.5f);

                Vector2 localA =
                    TransformSystem.InverseTransformPoint(
                        a,
                        worldPoint);

                Vector2 localB =
                    TransformSystem.InverseTransformPoint(
                        b,
                        worldPoint);

                int ax =
                    (int)MathF.Floor(localA.X);

                int ay =
                    (int)MathF.Floor(localA.Y);

                int bx =
                    (int)MathF.Floor(localB.X);

                int by =
                    (int)MathF.Floor(localB.Y);

                if (!IsInside(
                        maskA,
                        ax,
                        ay) ||
                    !IsInside(
                        maskB,
                        bx,
                        by))
                {
                    continue;
                }

                if (!maskA.IsSolid(
                        ax,
                        ay,
                        a.PixelAlphaThreshold))
                {
                    continue;
                }

                if (!maskB.IsSolid(
                        bx,
                        by,
                        b.PixelAlphaThreshold))
                {
                    continue;
                }

                return true;
            }
        }

        return false;
    }


    // ============================================================
    // Pixel <-> Rotated Box
    // ============================================================

    private bool PixelVsBox(
        Entity pixelEntity,
        Entity boxEntity)
    {
        if (pixelEntity.Texture == null)
            return false;

        PixelMask mask =
            _pixelMasks.Get(
                pixelEntity.Texture);

        Rectangle pixelBounds =
            GetTextureBounds(
                pixelEntity);

        if (pixelBounds.IsEmpty)
            return false;

        GetWorldBox(
            boxEntity,
            out Vector2 boxCenter,
            out Vector2 halfExtents,
            out float boxRotation);

        Rectangle overlap =
            Rectangle.Intersect(
                pixelBounds,
                GetBoxBounds(
                    boxCenter,
                    halfExtents,
                    boxRotation));

        if (overlap.IsEmpty)
            return false;

        for (int y = overlap.Top;
             y < overlap.Bottom;
             y++)
        {
            for (int x = overlap.Left;
                 x < overlap.Right;
                 x++)
            {
                Vector2 worldPoint =
                    new(
                        x + 0.5f,
                        y + 0.5f);

                Vector2 localPixel =
                    TransformSystem.InverseTransformPoint(
                        pixelEntity,
                        worldPoint);

                int px =
                    (int)MathF.Floor(
                        localPixel.X);

                int py =
                    (int)MathF.Floor(
                        localPixel.Y);

                if (!IsInside(
                        mask,
                        px,
                        py))
                {
                    continue;
                }

                if (!mask.IsSolid(
                        px,
                        py,
                        pixelEntity.PixelAlphaThreshold))
                {
                    continue;
                }

                if (!PointInsideOrientedBox(
                        worldPoint,
                        boxCenter,
                        halfExtents,
                        boxRotation))
                {
                    continue;
                }

                return true;
            }
        }

        return false;
    }


    // ============================================================
    // Geometry creation
    // ============================================================

    private static OrientedBoundingBox2D
        CreateWorldBox(Entity entity)
    {
        GetWorldBox(
            entity,
            out Vector2 center,
            out Vector2 halfExtents,
            out float rotation);

        return
            OrientedBoundingBox2D.CreateFromRotation(
                center,
                rotation,
                halfExtents);
    }

    private static BoundingBox2D
        CreateTextureAabb(Entity entity)
    {
        Rectangle bounds =
            GetTextureBounds(entity);

        Vector2 min =
            new(
                bounds.Left,
                bounds.Top);

        Vector2 max =
            new(
                bounds.Right,
                bounds.Bottom);

        return new BoundingBox2D(
            min,
            max);
    }

    private static void GetWorldBox(
        Entity entity,
        out Vector2 center,
        out Vector2 halfExtents,
        out float rotation)
    {
        center =
            TransformSystem.TransformOffset(
                entity,
                entity.CollisionOffset);

        Vector2 size =
            TransformSystem.Multiply(
                entity.CollisionSize,
                TransformSystem.Abs(
                    entity.WorldScale));

        halfExtents =
            size * 0.5f;

        rotation =
            entity.WorldRotation
            + entity.CollisionRotation;
    }


    // ============================================================
    // Texture/world bounds
    // ============================================================

    private static Rectangle
        GetTextureBounds(Entity entity)
    {
        if (entity.Texture == null)
            return Rectangle.Empty;

        Vector2 p0 =
            TransformSystem.TransformPoint(
                entity,
                Vector2.Zero);

        Vector2 p1 =
            TransformSystem.TransformPoint(
                entity,
                new Vector2(
                    entity.Texture.Width,
                    0f));

        Vector2 p2 =
            TransformSystem.TransformPoint(
                entity,
                new Vector2(
                    0f,
                    entity.Texture.Height));

        Vector2 p3 =
            TransformSystem.TransformPoint(
                entity,
                new Vector2(
                    entity.Texture.Width,
                    entity.Texture.Height));

        float minX =
            MathF.Min(
                MathF.Min(p0.X, p1.X),
                MathF.Min(p2.X, p3.X));

        float maxX =
            MathF.Max(
                MathF.Max(p0.X, p1.X),
                MathF.Max(p2.X, p3.X));

        float minY =
            MathF.Min(
                MathF.Min(p0.Y, p1.Y),
                MathF.Min(p2.Y, p3.Y));

        float maxY =
            MathF.Max(
                MathF.Max(p0.Y, p1.Y),
                MathF.Max(p2.Y, p3.Y));

        return new Rectangle(
            (int)MathF.Floor(minX),
            (int)MathF.Floor(minY),
            (int)MathF.Ceiling(maxX - minX),
            (int)MathF.Ceiling(maxY - minY));
    }


    private static Rectangle
        GetPixelOverlapBounds(
            Entity a,
            Entity b)
    {
        return Rectangle.Intersect(
            GetTextureBounds(a),
            GetTextureBounds(b));
    }

    private static Rectangle GetBoxBounds(
        Vector2 center,
        Vector2 halfExtents,
        float rotation)
    {
        Vector2 right =
            TransformSystem.Rotate(
                new Vector2(
                    halfExtents.X,
                    0f),
                rotation);

        Vector2 up =
            TransformSystem.Rotate(
                new Vector2(
                    0f,
                    halfExtents.Y),
                rotation);

        Vector2 p0 = center + right + up;
        Vector2 p1 = center + right - up;
        Vector2 p2 = center - right + up;
        Vector2 p3 = center - right - up;

        float minX =
            MathF.Min(
                MathF.Min(p0.X, p1.X),
                MathF.Min(p2.X, p3.X));

        float maxX =
            MathF.Max(
                MathF.Max(p0.X, p1.X),
                MathF.Max(p2.X, p3.X));

        float minY =
            MathF.Min(
                MathF.Min(p0.Y, p1.Y),
                MathF.Min(p2.Y, p3.Y));

        float maxY =
            MathF.Max(
                MathF.Max(p0.Y, p1.Y),
                MathF.Max(p2.Y, p3.Y));

        return new Rectangle(
            (int)MathF.Floor(minX),
            (int)MathF.Floor(minY),
            (int)MathF.Ceiling(maxX - minX),
            (int)MathF.Ceiling(maxY - minY));
    }


    // ============================================================
    // Primitive tests
    // ============================================================

    private static bool PointInsideOrientedBox(
        Vector2 point,
        Vector2 center,
        Vector2 halfExtents,
        float rotation)
    {
        Vector2 local =
            TransformSystem.Rotate(
                point - center,
                -rotation);

        return
            MathF.Abs(local.X) <= halfExtents.X &&
            MathF.Abs(local.Y) <= halfExtents.Y;
    }


    // ============================================================
    // Helpers
    // ============================================================

    private static bool IsInside(
        PixelMask mask,
        int x,
        int y)
    {
        return
            x >= 0 &&
            y >= 0 &&
            x < mask.Width &&
            y < mask.Height;
    }

    private static void ValidateCollisionConfiguration(
        Entity entity)
    {
        if (entity.CollisionMode == CollisionMode.PixelPerfect &&
            entity.Texture == null)
        {
            throw new InvalidOperationException(
                $"Entity {entity.Id} uses PixelPerfect collision " +
                "but has no texture.");
        }

        if (entity.CollisionMode == CollisionMode.RotatedBox &&
            (entity.CollisionSize.X < 0f ||
             entity.CollisionSize.Y < 0f))
        {
            throw new InvalidOperationException(
                $"Entity {entity.Id} has an invalid collision size.");
        }
    }
}