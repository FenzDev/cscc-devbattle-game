using Microsoft.Xna.Framework;

public sealed class TestWave : BattleWave
{
    private static readonly Behaviour MoveDown =
        new MoveBehaviour(
            new Vector2(
                0f,
                200f));

    private static readonly Behaviour Spin =
        new RotateBehaviour(2f);

    private TimersManager.Timer _spawnRoutine = null!;

    public override float Duration =>
        6f;


    protected override void OnStart(
        BattleContext battle)
    {
        _spawnRoutine =
            battle.Timers.StartEvery(
                this,
                1.0f);


        Spawn(
            battle,
            new ProjectileEntity
            {
                Position =
                    new Vector2(
                        battle.Arena.Center.X,
                        battle.Arena.Minimum.Y)
            },
            MoveDown,
            Spin);
    }


    protected override void OnUpdate(
        BattleContext battle,
        float deltaTime)
    {
        while (_spawnRoutine.HasElapsed())
        {
            Spawn(
                battle,
                new ProjectileEntity
                {
                    Position =
                        new Vector2(
                            battle.Player.Position.X,
                            battle.Arena.Minimum.Y)
                },
                MoveDown,
                Spin);
        }
    }


    protected override void OnEnd(
        BattleContext battle)
    {
        // Optional final wave-specific logic.
    }
}