using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public static class TransformSystem
{
    public static void ResolveAll(
        IEnumerable<Entity> entities)
    {
        var resolved = new HashSet<Entity>(
            ReferenceEqualityComparer.Instance);

        var resolving = new HashSet<Entity>(
            ReferenceEqualityComparer.Instance);

        foreach (Entity entity in entities)
        {
            Resolve(
                entity,
                resolved,
                resolving);
        }
    }

    private static void Resolve(
        Entity entity,
        HashSet<Entity> resolved,
        HashSet<Entity> resolving)
    {
        if (resolved.Contains(entity))
            return;

        if (!resolving.Add(entity))
        {
            throw new InvalidOperationException(
                "Circular entity attachment detected.");
        }

        Entity? parent = entity.Attachment?.Parent;

        if (parent == null)
        {
            entity.WorldPosition = entity.Position;
            entity.WorldRotation = entity.Rotation;
            entity.WorldScale = entity.Scale;
        }
        else
        {
            Resolve(
                parent,
                resolved,
                resolving);

            EntityAttachment attachment =
                entity.Attachment!;

            Vector2 parentScale =
                parent.WorldScale;

            Vector2 worldPosition =
                parent.WorldPosition
                + Rotate(
                    Multiply(
                        entity.Position,
                        parentScale),
                    parent.WorldRotation);

            float worldRotation =
                parent.WorldRotation
                + entity.Rotation;

            Vector2 worldScale =
                attachment.InheritScale
                    ? Multiply(
                        parent.WorldScale,
                        entity.Scale)
                    : entity.Scale;

            entity.WorldPosition = worldPosition;
            entity.WorldRotation = worldRotation;
            entity.WorldScale = worldScale;
        }

        resolving.Remove(entity);
        resolved.Add(entity);
    }


    // ============================================================
    // Math
    // ============================================================

    public static Vector2 TransformPoint(
        Entity entity,
        Vector2 localTexturePoint)
    {
        Vector2 relative =
            Multiply(
                localTexturePoint - entity.Origin,
                entity.WorldScale);

        return entity.WorldPosition
            + Rotate(
                relative,
                entity.WorldRotation);
    }

    public static Vector2 InverseTransformPoint(
        Entity entity,
        Vector2 worldPoint)
    {
        Vector2 relative =
            Rotate(
                worldPoint - entity.WorldPosition,
                -entity.WorldRotation);

        if (MathF.Abs(entity.WorldScale.X) < 0.000001f ||
            MathF.Abs(entity.WorldScale.Y) < 0.000001f)
        {
            throw new InvalidOperationException(
                "Cannot inverse-transform with zero scale.");
        }

        return new Vector2(
            relative.X / entity.WorldScale.X
                + entity.Origin.X,

            relative.Y / entity.WorldScale.Y
                + entity.Origin.Y);
    }

    public static Vector2 TransformOffset(
        Entity entity,
        Vector2 localOffset)
    {
        return entity.WorldPosition
            + Rotate(
                Multiply(
                    localOffset,
                    entity.WorldScale),
                entity.WorldRotation);
    }

    public static Vector2 Rotate(
        Vector2 value,
        float radians)
    {
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);

        return new Vector2(
            value.X * cos - value.Y * sin,
            value.X * sin + value.Y * cos);
    }

    public static Vector2 Multiply(
        Vector2 a,
        Vector2 b)
    {
        return new Vector2(
            a.X * b.X,
            a.Y * b.Y);
    }

    public static Vector2 Abs(
        Vector2 value)
    {
        return new Vector2(
            MathF.Abs(value.X),
            MathF.Abs(value.Y));
    }
}