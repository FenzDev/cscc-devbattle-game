using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CSCCDevBattle;

public class Game1 : Game
{
  private readonly GraphicsDeviceManager _graphics;
  public static SpriteBatch SpriteBatch { get; private set; } = null!;
  public static Texture2D Pixel { get; private set; } = null!;
#if DEBUG
  public static bool DebugEnabled { get; private set; } = true;
#else
    public bool DebugEnabled { get; private set; } = false;
#endif

  public static Scene CurrentScene { get; private set; }

  public Game1()
  {
    _graphics = new GraphicsDeviceManager(this);

    Content.RootDirectory = "Content";
    IsMouseVisible = true;

    _graphics.PreferredBackBufferWidth = 960;
    _graphics.PreferredBackBufferHeight = 540;
  }

  protected override void LoadContent()
  {
    SpriteBatch = new SpriteBatch(GraphicsDevice);

    Pixel = new Texture2D(GraphicsDevice, 1, 1);
    Pixel.SetData([Color.White]);

    CurrentScene = new MainMenuScene();
  }

  // private bool _SceneIsRunning = false;
  private bool _StartingScene = true;
  private bool _LeavingScene = false;
  private Scene _NextScene = null;
  protected override void Update(GameTime gameTime)
  {
    if (CurrentScene is null)
    {
      base.Update(gameTime);
      return;
    } 

    if (_StartingScene && CurrentScene.SetupTick(gameTime) == TransitionState.Finished)
    {
      _StartingScene = false;
    }
    else if (_LeavingScene && CurrentScene.CleanupTick(gameTime) == TransitionState.Finished)
    {
      _LeavingScene = false;
      CurrentScene = _NextScene;
      if (_NextScene is not null)
      {
        _StartingScene = true;
      }
    } else
    {
      CurrentScene.Tick(gameTime);
    }

    base.Update(gameTime);
  }

  protected override void Draw(GameTime gameTime)
  {
    GraphicsDevice.Clear(Color.Gray);

    SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
  }

  public void GoToScene(Scene scene)
  {
    _NextScene = scene;
    if (_LeavingScene || _StartingScene) return;
    if (CurrentScene is null)
    {
    }
  }

}
