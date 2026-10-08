using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public sealed class BattleArena
{
  private readonly ArenaWallEntity _top;
  private readonly ArenaWallEntity _bottom;
  private readonly ArenaWallEntity _left;
  private readonly ArenaWallEntity _right;

  private Vector2 _size;

  public Vector2 Center { get; set; }

  public Vector2 Size
  {
    get => _size;
    set => Resize(value);
  }

  public float BorderThickness { get; }

  public Vector2 HalfSize =>
      _size * 0.5f;

  public Vector2 Minimum =>
      Center - HalfSize;

  public Vector2 Maximum =>
      Center + HalfSize;

  /// <summary>
  /// The area inside the arena borders.
  /// </summary>
  public Vector2 InnerMinimum =>
      Minimum + new Vector2(BorderThickness);

  public Vector2 InnerMaximum =>
      Maximum - new Vector2(BorderThickness);

  public BattleArena(
      BattleContext battle,
      Vector2 center,
      Vector2 size,
      float borderThickness = 4f)
  {
    ArgumentNullException.ThrowIfNull(battle);

    if (size.X <= 0f || size.Y <= 0f)
      throw new ArgumentOutOfRangeException(nameof(size));

    if (borderThickness <= 0f)
      throw new ArgumentOutOfRangeException(
          nameof(borderThickness));

    if (size.X <= borderThickness * 2f ||
        size.Y <= borderThickness * 2f)
    {
      throw new ArgumentException(
          "Arena must be larger than twice its border thickness.",
          nameof(size));
    }

    Center = center;
    _size = size;
    BorderThickness = borderThickness;

    _top = new ArenaWallEntity();
    _bottom = new ArenaWallEntity();
    _left = new ArenaWallEntity();
    _right = new ArenaWallEntity();

    battle.Spawn(_top);
    battle.Spawn(_bottom);
    battle.Spawn(_left);
    battle.Spawn(_right);

    UpdateWalls();
  }

  public void Resize(Vector2 newSize)
  {
    if (newSize.X <= 0f || newSize.Y <= 0f)
      throw new ArgumentOutOfRangeException(
          nameof(newSize));

    if (newSize.X <= BorderThickness * 2f ||
        newSize.Y <= BorderThickness * 2f)
    {
      throw new ArgumentException(
          "Arena must be larger than twice its border thickness.",
          nameof(newSize));
    }

    _size = newSize;

    UpdateWalls();
  }

  public void Move(Vector2 newCenter)
  {
    Center = newCenter;

    UpdateWalls();
  }

  public void Set(
      Vector2 center,
      Vector2 size)
  {
    Center = center;
    Resize(size);
  }

  /// <summary>
  /// Keeps a point inside the inner arena.
  /// </summary>
  public Vector2 ClampPoint(Vector2 point)
  {
    return new Vector2(
        MathHelper.Clamp(
            point.X,
            InnerMinimum.X,
            InnerMaximum.X),

        MathHelper.Clamp(
            point.Y,
            InnerMinimum.Y,
            InnerMaximum.Y));
  }

  /// <summary>
  /// Keeps a box center inside the arena, accounting for
  /// the half-size of the box.
  /// </summary>
  public Vector2 ClampBoxCenter(
      Vector2 center,
      Vector2 halfExtents)
  {
    float minX =
        InnerMinimum.X + halfExtents.X;

    float maxX =
        InnerMaximum.X - halfExtents.X;

    float minY =
        InnerMinimum.Y + halfExtents.Y;

    float maxY =
        InnerMaximum.Y - halfExtents.Y;

    return new Vector2(
        MathHelper.Clamp(
            center.X,
            minX,
            maxX),

        MathHelper.Clamp(
            center.Y,
            minY,
            maxY));
  }

  public Vector2 ClampEntityPosition(
  Entity entity,
  Vector2 desiredPosition)
  {
    if (entity.Texture == null)
      return ClampPoint(desiredPosition);

    Rectangle bounds =
        entity.GetMovementBounds();

    Vector2 origin =
        entity.Origin;

    Vector2 scale =
        entity.WorldScale;

    // --------------------------------------------------------
    // Unrotated movement bounds
    // --------------------------------------------------------

    float left =
        (bounds.Left - origin.X) *
        scale.X;

    float right =
        (bounds.Right - origin.X) *
        scale.X;

    float top =
        (bounds.Top - origin.Y) *
        scale.Y;

    float bottom =
        (bounds.Bottom - origin.Y) *
        scale.Y;

    float minX =
        InnerMinimum.X - left;

    float maxX =
        InnerMaximum.X - right;

    float minY =
        InnerMinimum.Y - top;

    float maxY =
        InnerMaximum.Y - bottom;

    return new Vector2(
        MathHelper.Clamp(
            desiredPosition.X,
            minX,
            maxX),

        MathHelper.Clamp(
            desiredPosition.Y,
            minY,
            maxY));
  }

  private void UpdateWalls()
  {
    Vector2 half = HalfSize;

    float halfThickness =
        BorderThickness * 0.5f;

    // --------------------------------------------------------
    // Top
    // --------------------------------------------------------

    _top.Position = new Vector2(
        Center.X,
        Maximum.Y - halfThickness);

    _top.CollisionSize = new Vector2(
        _size.X,
        BorderThickness);

    // --------------------------------------------------------
    // Bottom
    // --------------------------------------------------------

    _bottom.Position = new Vector2(
        Center.X,
        Minimum.Y + halfThickness);

    _bottom.CollisionSize = new Vector2(
        _size.X,
        BorderThickness);

    // --------------------------------------------------------
    // Left
    // --------------------------------------------------------

    _left.Position = new Vector2(
        Minimum.X + halfThickness,
        Center.Y);

    _left.CollisionSize = new Vector2(
        BorderThickness,
        _size.Y);

    // --------------------------------------------------------
    // Right
    // --------------------------------------------------------

    _right.Position = new Vector2(
        Maximum.X - halfThickness,
        Center.Y);

    _right.CollisionSize = new Vector2(
        BorderThickness,
        _size.Y);
  }

  public IEnumerable<Entity> GetWalls()
  {
    yield return _top;
    yield return _bottom;
    yield return _left;
    yield return _right;
  }
}