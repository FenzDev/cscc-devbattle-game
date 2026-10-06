using CSCCDevBattle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public class MainMenuScene : Scene
{
  private float _fade = 1.0f;

  const float FADE_SPEED = 5.0f;
  public override TransitionState CleanupTick(GameTime gameTime)
  {

    _fade += FADE_SPEED * (float)gameTime.ElapsedGameTime.TotalSeconds;

    if (_fade < 1.0f)
      return TransitionState.InProgress;

    _fade = 1.0f;
    return TransitionState.Finished;
  }

  public override TransitionState SetupTick(GameTime gameTime)
  {
    _fade -= FADE_SPEED * (float)gameTime.ElapsedGameTime.TotalSeconds;

    if (_fade > 0.0f)
      return TransitionState.InProgress;

    _fade = 0.0f;
    return TransitionState.Finished;
  }

  public override void Draw(GameTime gameTime, Game1 game)
  {
    game.GraphicsDevice.Clear(Color.Green);

    game.SpriteBatch.Begin();

    game.SpriteBatch.Draw(
        game.Pixel,
        new Rectangle(
            0,
            0,
            game.GraphicsDevice.Viewport.Width,
            game.GraphicsDevice.Viewport.Height
        ),
        Color.Black * _fade
    );

    game.SpriteBatch.End();
  }

  public override void Tick(GameTime gameTime, Game1 game)
  {
    if (Input.Down(Keys.R))
    {
      game.GoToScene(new MainMenuScene());
    }
  }
}