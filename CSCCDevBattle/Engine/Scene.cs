using Microsoft.Xna.Framework;

public abstract class Scene
{
    public TimersManager Timers { get; } = new();
    public DebugSystem Debug { get; } = new();

    /// <summary>When true, Game1 leaves this scene's timers frozen.</summary>
    public virtual bool PausesTimers => false;

    public abstract TransitionState SetupTick(GameTime gameTime);
    public abstract TransitionState CleanupTick(GameTime gameTime);
    public abstract void Tick(GameTime gameTime, Game1 game);
    public abstract void Draw(GameTime gameTime, Game1 game);
}
