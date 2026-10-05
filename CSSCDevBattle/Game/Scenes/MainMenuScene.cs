using Microsoft.Xna.Framework;

public class MainMenuScene : Scene
{
  public MainMenuScene()
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
    
  }
  public override void Tick(GameTime gameTime)
  {
    throw new System.NotImplementedException();
  }
}