using System.Linq.Expressions;
using Microsoft.Xna.Framework;

public sealed class TestWave : BattleWave
{
    private static readonly Behaviour MoveDown =
        new MoveBehaviour(
            new Vector2(
                0f,
                600f));

    private static readonly Behaviour Spin =
        new RotateBehaviour(32f);

    private static readonly Behaviour Spiral =
        new SpiralBehaviour(200f, 0.4f);

    private TimersManager.Timer _spawnRoutine = null!;

    public override float Duration =>
        20f;


    protected override void OnStart(
        BattleContext battle)
    {
        _spawnRoutine =
            battle.Timers.StartEvery(
                this,
                1.0f);

        Spawn(battle,
            new SolidBlockEntity(battle.Arena.Center, new Vector2(20f)),
            [

            ]);
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
                            battle.Player.Position.Y - 200f)
                },
                Spiral,
                Spin);
        }
    }


    protected override void OnEnd(
        BattleContext battle)
    {
        // Optional final wave-specific logic.
    }
}