using Microsoft.Xna.Framework;

public class BattleScene : Scene
{
  public BattleScene()
  {

  }

  public override TransitionState CleanupTick()
  {
    return TransitionState.Finished;
  }

  public override TransitionState SetupTick()
  {
    return TransitionState.Finished;
  }

  public override void Draw(GameTime gameTime)
  {
    throw new System.NotImplementedException();
  }


  public override void Tick(GameTime gameTime)
  {
    throw new System.NotImplementedException();
  }
}