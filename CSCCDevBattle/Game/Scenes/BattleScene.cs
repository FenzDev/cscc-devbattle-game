using CSCCDevBattle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using FontStashSharp;
using Microsoft.Xna.Framework.Graphics;

public class BattleScene : Scene
{
  public BattleContext BattleContext = new();
  private TimersManager.Timer actionTimer;

  public BattleScene()
  {
    actionTimer = Timers.StartAfter(5.0f);

    SetupDebug();
  }

  bool _DebugIsMagentaScreen;
  private void SetupDebug()
  {
    Debug.Register(
    Keys.F1,
    "Magenta Screen",
    () =>
    {
      _DebugIsMagentaScreen = !_DebugIsMagentaScreen;
    },
    () => _DebugIsMagentaScreen
      ? "Enabled"
      : "Disabled"
);
  }


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
    if (_DebugIsMagentaScreen)
      game.GraphicsDevice.Clear(Color.Red);
    else
      game.GraphicsDevice.Clear(Color.Black);
    // Game
    game.SpriteBatch.Begin(
      samplerState: SamplerState.PointClamp
    );

    game.SpriteBatch.DrawString(
      game.FontText,
      "Hello\nHi you",
      new(5, 5),
      Color.White
    );
    game.SpriteBatch.End();

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
      game.GoToScene(new BattleScene());
    }
  }
}