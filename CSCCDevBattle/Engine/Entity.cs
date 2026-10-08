using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;

public enum CollisionMode
{
    None,
    RotatedBox,
    PixelPerfect
}

public enum AttachmentVisualOrder
{
    Back,
    Front
}

/// <summary>
/// Parent relationship for an Entity.
///
/// Position is relative to the parent's origin.
/// Rotation is relative to the parent's rotation.
/// Scale may optionally inherit from the parent.
///
/// VisualOrder only affects rendering order.
/// It has no effect on collision.
/// </summary>
public sealed class EntityAttachment
{
    public Entity Parent { get; }

    public bool InheritScale { get; set; } = true;

    public AttachmentVisualOrder VisualOrder { get; set; }
        = AttachmentVisualOrder.Front;

    public EntityAttachment(
        Entity parent,
        bool inheritScale = true,
        AttachmentVisualOrder visualOrder = AttachmentVisualOrder.Front)
    {
        ArgumentNullException.ThrowIfNull(parent);

        Parent = parent;
        InheritScale = inheritScale;
        VisualOrder = visualOrder;
    }
}

/// <summary>
/// Pure entity state.
///
/// Entity does not Update itself.
/// Entity does not Draw itself.
/// External systems execute behaviour, resolve transforms,
/// synchronize collision and render it.
/// </summary>
public abstract class Entity : ICollisionActor
{
    private static int _nextId;

    private Vector2? _originOverride;

    protected Entity()
    {
        Behaviours = [];
    }

    // ============================================================
    // Identity
    // ============================================================

    public int Id { get; } = _nextId++;


    // ============================================================
    // Lifecycle
    // ============================================================

    public bool Enabled { get; set; } = true;

    public bool DestroyRequested { get; set; }


    // ============================================================
    // Local transform
    // ============================================================

    /// <summary>
    /// World position when unattached.
    /// Local position relative to Parent when attached.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// Local rotation.
    /// </summary>
    public float Rotation { get; set; }

    /// <summary>
    /// Local scale.
    /// </summary>
    public Vector2 Scale { get; set; } = new(2f);


    // ============================================================
    // Resolved world transform
    // ============================================================

    /// <summary>
    /// Calculated by TransformSystem.
    /// </summary>
    public Vector2 WorldPosition { get; internal set; }

    public float WorldRotation { get; internal set; }

    public Vector2 WorldScale { get; internal set; } = Vector2.One;


    // ============================================================
    // Visual
    // ============================================================

    /// <summary>
    /// Null means the entity has no visible sprite.
    /// </summary>
    public Texture2D? Texture { get; set; }

    public bool Visible { get; set; } = true;

    public Color Tint { get; set; } = Color.White;

    /// <summary>
    /// Defaults automatically to the texture center.
    /// </summary>
    public Vector2 Origin
    {
        get
        {
            if (_originOverride.HasValue)
                return _originOverride.Value;

            if (Texture == null)
                return Vector2.Zero;

            return new Vector2(
                Texture.Width * 0.5f,
                Texture.Height * 0.5f);
        }
    }

    /// <summary>
    /// Null means automatic centered origin.
    /// </summary>
    public Vector2? OriginOverride
    {
        get => _originOverride;
        set => _originOverride = value;
    }

    public void ResetOrigin()
    {
        _originOverride = null;
    }


    // ============================================================
    // Attachment
    // ============================================================

    public EntityAttachment? Attachment { get; private set; }

    public bool IsAttached =>
        Attachment != null;

    public void AttachTo(
        Entity parent,
        Vector2 localPosition,
        float localRotation = 0f,
        bool inheritScale = true,
        AttachmentVisualOrder visualOrder = AttachmentVisualOrder.Front)
    {
        ArgumentNullException.ThrowIfNull(parent);

        if (ReferenceEquals(parent, this))
            throw new InvalidOperationException(
                "An entity cannot be attached to itself.");

        if (WouldCreateCycle(parent))
            throw new InvalidOperationException(
                "This attachment would create a cycle.");

        Attachment = new EntityAttachment(
            parent,
            inheritScale,
            visualOrder);

        Position = localPosition;
        Rotation = localRotation;
    }

    public void Detach(bool keepWorldTransform = true)
    {
        if (Attachment == null)
            return;

        if (keepWorldTransform)
        {
            Position = WorldPosition;
            Rotation = WorldRotation;
            Scale = WorldScale;
        }

        Attachment = null;
    }

    private bool WouldCreateCycle(Entity possibleParent)
    {
        Entity? current = possibleParent;

        while (current != null)
        {
            if (ReferenceEquals(current, this))
                return true;

            current = current.Attachment?.Parent;
        }

        return false;
    }


    // ============================================================
    // Behaviours
    // ============================================================

    /// <summary>
    /// Behaviour instances may be shared between many entities,
    /// provided the Behaviour itself is stateless.
    /// </summary>
    public List<Behaviour> Behaviours { get; }


    // ============================================================
    // Collision configuration
    // ============================================================

    public CollisionMode CollisionMode { get; set; }
        = CollisionMode.None;
        
    public Rectangle? MovementBounds { get; set; }

    public Rectangle GetMovementBounds()
{
    if (MovementBounds.HasValue)
        return MovementBounds.Value;

    if (Texture == null)
        return Rectangle.Empty;

    return new Rectangle(
        0,
        0,
        Texture.Width,
        Texture.Height);
}

    // -----------------------------
    // Rotated box
    // -----------------------------

    /// <summary>
    /// Collision box size in local entity units.
    /// </summary>
    public Vector2 CollisionSize { get; set; }

    /// <summary>
    /// Collision box center relative to the entity origin.
    /// </summary>
    public Vector2 CollisionOffset { get; set; }

    /// <summary>
    /// Additional rotation relative to Entity rotation.
    /// </summary>
    public float CollisionRotation { get; set; }

    public string? CollisionLayer { get; set; }

    // -----------------------------
    // Pixel perfect
    // -----------------------------

    /// <summary>
    /// Alpha >= this value is considered solid.
    /// </summary>
    public byte PixelAlphaThreshold { get; set; } = 1;



    // ============================================================
    // ICollisionActor
    // ============================================================


    /// <summary>
    /// Updated by BattleCollisionSystem.
    ///
    /// None      -> default CollisionShape2D
    /// Box       -> OrientedBoundingBox2D
    /// Pixel     -> transformed AABB used only for broadphase
    /// </summary>
    public CollisionShape2D Shape { get; internal set; } = default;
}