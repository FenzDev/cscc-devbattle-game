using Microsoft.Xna.Framework;

public sealed class MoveBehaviour : Behaviour
{
    private readonly Vector2 _velocity;

    public MoveBehaviour(Vector2 velocity)
    {
        _velocity = velocity;
    }

    public override void Update(
        Entity entity,
        BattleContext battle,
        float deltaTime)
    {
        entity.Position +=
            _velocity * deltaTime;
    }
}