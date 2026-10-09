using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class Game1 : Game
{
  public const int BASE_SCREEN_WIDTH = 600;
  public const int BASE_SCREEN_HEIGHT = 600;
  private const int DEBUG_SCREEN_WIDTH = 600;
  private const int DEBUG_SCREEN_HEIGHT = 600;

  public static Game1 Singleton { get; private set; }

  private readonly GraphicsDeviceManager _graphics;
  public SpriteBatch SpriteBatch { get; private set; } = null!;
  public Texture2D Pixel { get; private set; } = null!;
  public SpriteFontBase FontTitle { get; private set; }
  public SpriteFontBase FontText { get; private set; }
  public SpriteFontBase FontDebug { get; private set; }

  public Effect PostEffect { get; set; }
#if DEBUG
  public bool DebugEnabled { get; private set; } = true;
#else
    public bool DebugEnabled { get; private set; } = false;
#endif

  public Scene CurrentScene { get; private set; }

  public Game1()
  {
    _graphics = new(this);
    Singleton = this;


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
  private FontSystem _MonoFontSystem;
  protected override void LoadContent()
  {
    SpriteBatch = new SpriteBatch(GraphicsDevice);

    Pixel = new Texture2D(GraphicsDevice, 1, 1);
    Pixel.SetData([Color.White]);

    _SansFontSystem = new FontSystem(new()
    {
      // FontResolutionFactor = 2
    });
    using (var font = TitleContainer.OpenStream("Content/Fonts/SansFont.ttf"))
    {
      _SansFontSystem.AddFont(font);
    }
    _MonoFontSystem = new FontSystem(new()
    {
      // FontResolutionFactor = 2
    });
    using (var font = TitleContainer.OpenStream("Content/Fonts/MonoFont.ttf"))
    {
      _MonoFontSystem.AddFont(font);
    }

    FontTitle = _SansFontSystem.GetFont(48);
    FontText = _SansFontSystem.GetFont(28);
    FontDebug = _MonoFontSystem.GetFont(16);

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
      if (DebugEnabled)
      {
        CurrentScene.Debug.Update(gameTime);
      }
      CurrentScene.Tick(gameTime, this);
    }

    base.Update(gameTime);
  }

  protected override void Draw(GameTime gameTime)
  {
    // Render the game at its internal resolution.
    GraphicsDevice.SetRenderTarget(_ScreenRenderTarget);
    GraphicsDevice.Clear(Color.Black);

    CurrentScene?.Draw(gameTime, this);

    // Switch back to the actual fullscreen backbuffer.
    GraphicsDevice.SetRenderTarget(null);

    GraphicsDevice.Clear(Color.Black);

    Viewport viewport = GraphicsDevice.Viewport;

    // Preserve the square aspect ratio.
    float scale = System.MathF.Min(
        viewport.Width / (float)BASE_SCREEN_WIDTH,
        viewport.Height / (float)BASE_SCREEN_HEIGHT
    );

    int width = (int)(BASE_SCREEN_WIDTH * scale);
    int height = (int)(BASE_SCREEN_HEIGHT * scale);

    // Center the game image.
    Rectangle destination = new Rectangle(
        (viewport.Width - width) / 2,
        (viewport.Height - height) / 2,
        width,
        height
    );

    SpriteBatch.Begin(
        samplerState: SamplerState.PointClamp,
        effect: PostEffect
    );

    SpriteBatch.Draw(
        _ScreenRenderTarget,
        destination,
        Color.White
    );

    SpriteBatch.End();

    // Debug UI is drawn directly on the actual screen.
    if (DebugEnabled)
    {
      SpriteBatch.Begin(
          samplerState: SamplerState.PointClamp
      );

      CurrentScene?.Debug.Draw(
          SpriteBatch,
          FontDebug,
          Pixel
      );

      SpriteBatch.End();
    }
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
