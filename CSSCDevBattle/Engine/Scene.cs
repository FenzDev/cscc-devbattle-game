using Microsoft.Xna.Framework;

public abstract class Scene {
  protected internal TimersManager Timers { get; } = new TimersManager();

  public abstract TransitionState SetupTick();
  public abstract TransitionState CleanupTick();
  public abstract void Tick(GameTime gameTime);
  public abstract void Draw(GameTime gameTime);

} 