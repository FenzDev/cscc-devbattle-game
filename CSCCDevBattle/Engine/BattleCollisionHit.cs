using MonoGame.Extended;
using MonoGame.Extended.Collisions;

public readonly record struct BattleCollisionHit(
    Entity A,
    Entity B,
    bool UsedPixelPerfect,
    CollisionResult2D? GeometricResult)
{
    public bool HasGeometricResult =>
        GeometricResult.HasValue;
}