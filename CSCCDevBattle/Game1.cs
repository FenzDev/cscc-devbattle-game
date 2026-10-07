using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CSCCDevBattle;

public class Game1 : Game
{
  private const int BASE_SCREEN_WIDTH = 400;
  private const int BASE_SCREEN_HEIGHT = 400;
  private const int DEBUG_SCREEN_WIDTH = 800;
  private const int DEBUG_SCREEN_HEIGHT = 800;

  private readonly GraphicsDeviceManager _graphics;
  public SpriteBatch SpriteBatch { get; private set; } = null!;
  public Texture2D Pixel { get; private set; } = null!;
  public SpriteFontBase FontTitle { get; private set; }
  public SpriteFontBase FontText { get; private set; }

  public Effect PostEffect { get; set; }
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

#if DEBUG
    _graphics.PreferredBackBufferWidth = DEBUG_SCREEN_WIDTH;
    _graphics.PreferredBackBufferHeight = DEBUG_SCREEN_HEIGHT;
    _graphics.IsFullScreen = false;
#else
    _graphics.PreferredBackBufferWidth = BASE_SCREEN_WIDTH;
    _graphics.PreferredBackBufferHeight = BASE_SCREEN_HEIGHT;
    _graphics.IsFullScreen = true;
#endif
  }

  private RenderTarget2D _ScreenRenderTarget;
  private FontSystem _SansFontSystem;
  protected override void LoadContent()
  {
    SpriteBatch = new SpriteBatch(GraphicsDevice);

    Pixel = new Texture2D(GraphicsDevice, 1, 1);
    Pixel.SetData([Color.White]);

    _SansFontSystem = new FontSystem();
    using (var font = TitleContainer.OpenStream("Content/Fonts/SansFont.ttf"))
    {
      _SansFontSystem.AddFont(font);
    }

    FontTitle = _SansFontSystem.GetFont(24);
    FontText = _SansFontSystem.GetFont(16);

    _ScreenRenderTarget = new RenderTarget2D(GraphicsDevice, BASE_SCREEN_WIDTH, BASE_SCREEN_HEIGHT);

    GoToScene(new BattleScene());
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
      CurrentScene.Timers.Update(gameTime);
      CurrentScene.Tick(gameTime, this);
    }

    base.Update(gameTime);
  }

  protected override void Draw(GameTime gameTime)
  {
    GraphicsDevice.SetRenderTarget(_ScreenRenderTarget);

    GraphicsDevice.Clear(Color.Gray);
    CurrentScene.Draw(gameTime, this);

    GraphicsDevice.SetRenderTargets(null);

    SpriteBatch.Begin(
      samplerState: SamplerState.PointClamp,
      effect: PostEffect
    );
    SpriteBatch.Draw(
      _ScreenRenderTarget,
      new Rectangle(Point.Zero, new(
        _graphics.PreferredBackBufferWidth,
        _graphics.PreferredBackBufferHeight
      )),
      Color.White
    );
    SpriteBatch.End();
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
