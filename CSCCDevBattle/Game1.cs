using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CSCCDevBattle;

public class Game1 : Game
{
  private readonly GraphicsDeviceManager _graphics;
  public SpriteBatch SpriteBatch { get; private set; } = null!;
  public Texture2D Pixel { get; private set; } = null!;
#if DEBUG
  public bool DebugEnabled { get; private set; } = true;
#else
    public bool DebugEnabled { get; private set; } = false;
#endif

  public Scene CurrentScene { get; private set; }

  public Game1()
  {
    _graphics = new GraphicsDeviceManager(this);

    Content.RootDirectory = "Content";
    IsMouseVisible = true;

    _graphics.PreferredBackBufferWidth = 640;
    _graphics.PreferredBackBufferHeight = 640;
    #if DEBUG
    _graphics.IsFullScreen = false;
    #else
    _graphics.IsFullScreen = true;
    #endif
  }

  protected override void LoadContent()
  {
    SpriteBatch = new SpriteBatch(GraphicsDevice);

    Pixel = new Texture2D(GraphicsDevice, 1, 1);
    Pixel.SetData([Color.White]);

    GoToScene(new MainMenuScene());
  }

  // private bool _SceneIsRunning = false;
  private bool _StartingScene = false;
  private bool _LeavingScene = false;
  private Scene _NextScene = null;
  protected override void Update(GameTime gameTime)
  {
    Input.Update();

    if (CurrentScene is null)
    {
      base.Update(gameTime);
      return;
    }

    if (_StartingScene)
    {
      if (CurrentScene.SetupTick(gameTime) == TransitionState.Finished)
      {
        _StartingScene = false;
      }
    }
    else if (_LeavingScene)
    {
      if (CurrentScene.CleanupTick(gameTime) == TransitionState.Finished)
      {
        _LeavingScene = false;

        CurrentScene = _NextScene;
        _StartingScene = true;
      }
    }
    else
    {
      CurrentScene.Tick(gameTime, this);
    }

    base.Update(gameTime);
  }

  protected override void Draw(GameTime gameTime)
  {
    GraphicsDevice.Clear(Color.Gray);

    CurrentScene.Draw(gameTime, this);
  }

  public void GoToScene(Scene scene)
  {
    if (_StartingScene || _LeavingScene)
      return;

    _NextScene = scene;

    if (CurrentScene is null)
    {
      CurrentScene = scene;
      _StartingScene = true;
    }
    else
    {
      _LeavingScene = true;
    }
  }

}
