public sealed class RotateBehaviour : Behaviour
{
    private readonly float _speed;

    public RotateBehaviour(float speed)
    {
        _speed = speed;
    }

    public override void Update(
        Entity entity,
        BattleContext battle,
        float deltaTime)
    {
        entity.Rotation +=
            _speed * deltaTime;
    }
}