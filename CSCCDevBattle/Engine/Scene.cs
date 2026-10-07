using CSCCDevBattle;
using Microsoft.Xna.Framework;

public abstract class Scene
{
  public TimersManager Timers { get; } = new();
  public DebugSystem Debug { get; } = new();

  public abstract TransitionState SetupTick(GameTime gameTime);
  public abstract TransitionState CleanupTick(GameTime gameTime);
  public abstract void Tick(GameTime gameTime, Game1 game);
  public abstract void Draw(GameTime gameTime, Game1 game);

}