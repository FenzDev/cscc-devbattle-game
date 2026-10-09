using System.Threading;
using Microsoft.Xna.Framework;

public sealed class SpiralBehaviour : Behaviour
{
    private readonly float _speed;
    private readonly float _spiralAmount;

    public SpiralBehaviour(float speed, float spiralAmount)
    {
        _speed = speed;
        _spiralAmount = spiralAmount;
    }

    public override void Update(
        Entity entity,
        BattleContext battle,
        float deltaTime)
    {
        Vector2 towardsArenaCenter = battle.Player.Position - entity.Position;

        if (towardsArenaCenter == Vector2.Zero) return;

        towardsArenaCenter.Normalize();

        Vector2 tangent = new Vector2(-towardsArenaCenter.Y, towardsArenaCenter.X);

        Vector2 spiralDirection = tangent + towardsArenaCenter * _spiralAmount;
        spiralDirection.Normalize();

        // Example: assign velocity along the tangent
        Vector2 velocity = spiralDirection * _speed * deltaTime;
        entity.Position += velocity;
    }
}