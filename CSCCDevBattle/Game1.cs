using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class Game1 : Game
{
  public const int BASE_SCREEN_WIDTH = 600;
  public const int BASE_SCREEN_HEIGHT = 600;
  private const int DEBUG_SCREEN_WIDTH = 600;
  private const int DEBUG_SCREEN_HEIGHT = 600;

  public static Game1 Singleton { get; private set; } = null!;

  private readonly GraphicsDeviceManager _graphics;
  public SpriteBatch SpriteBatch { get; private set; } = null!;
  public Texture2D Pixel { get; private set; } = null!;
  public SpriteFontBase FontTitle { get; private set; } = null!;
  public SpriteFontBase FontText { get; private set; } = null!;
  public SpriteFontBase FontDebug { get; private set; } = null!;
  public Effect? PostEffect { get; set; }

#if DEBUG
  public bool DebugEnabled { get; private set; } = true;
#else
  public bool DebugEnabled { get; private set; } = false;
#endif

  public bool IsPaused { get; private set; }
  public Scene? CurrentScene { get; private set; }

  private RenderTarget2D _screenRenderTarget = null!;
  private FontSystem _sansFontSystem = null!;
  private FontSystem _monoFontSystem = null!;
  private bool _startingScene;
  private bool _leavingScene;
  private Scene? _nextScene;

  public Game1()
  {
    _graphics = new GraphicsDeviceManager(this);
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

  protected override void LoadContent()
  {
    SpriteBatch = new SpriteBatch(GraphicsDevice);
    Pixel = new Texture2D(GraphicsDevice, 1, 1);
    Pixel.SetData([Color.White]);

    _sansFontSystem = new FontSystem(new());
    using (var font = TitleContainer.OpenStream("Content/Fonts/SansFont.ttf"))
      _sansFontSystem.AddFont(font);

    _monoFontSystem = new FontSystem(new());
    using (var font = TitleContainer.OpenStream("Content/Fonts/MonoFont.ttf"))
      _monoFontSystem.AddFont(font);

    FontTitle = _sansFontSystem.GetFont(48);
    FontText = _sansFontSystem.GetFont(28);
    FontDebug = _monoFontSystem.GetFont(16);
    _screenRenderTarget = new RenderTarget2D(GraphicsDevice, BASE_SCREEN_WIDTH, BASE_SCREEN_HEIGHT);
    GoToScene(new BattleScene());
  }

  protected override void Update(GameTime gameTime)
  {
    Input.Update();

    if (CurrentScene is null)
    {
      base.Update(gameTime);
      return;
    }

    // Escape toggles pause only after the current scene's transition has settled.
    if (!_startingScene && !_leavingScene && Input.Pressed(Keys.Escape))
      IsPaused = !IsPaused;

    if (IsPaused)
    {
      // Keep debug commands usable, but freeze scene updates and game timers.
      if (DebugEnabled)
        CurrentScene.Debug.Update(gameTime);

      base.Update(gameTime);
      return;
    }

    if (_startingScene)
    {
      if (CurrentScene.SetupTick(gameTime) == TransitionState.Finished)
        _startingScene = false;
    }
    else if (_leavingScene)
    {
      if (CurrentScene.CleanupTick(gameTime) == TransitionState.Finished)
      {
        _leavingScene = false;
        CurrentScene = _nextScene;
        _startingScene = true;
      }
    }
    else
    {
      CurrentScene.Timers.Update(gameTime);
      if (DebugEnabled)
        CurrentScene.Debug.Update(gameTime);
      CurrentScene.Tick(gameTime, this);
    }

    base.Update(gameTime);
  }

  protected override void Draw(GameTime gameTime)
  {
    GraphicsDevice.SetRenderTarget(_screenRenderTarget);
    GraphicsDevice.Clear(Color.Black);
    CurrentScene?.Draw(gameTime, this);

    if (IsPaused)
      DrawPauseOverlay();

    GraphicsDevice.SetRenderTarget(null);
    GraphicsDevice.Clear(Color.Black);

    Viewport viewport = GraphicsDevice.Viewport;
    float scale = System.MathF.Min(
      viewport.Width / (float)BASE_SCREEN_WIDTH,
      viewport.Height / (float)BASE_SCREEN_HEIGHT);

    int width = (int)(BASE_SCREEN_WIDTH * scale);
    int height = (int)(BASE_SCREEN_HEIGHT * scale);
    Rectangle destination = new(
      (viewport.Width - width) / 2,
      (viewport.Height - height) / 2,
      width,
      height);

    SpriteBatch.Begin(samplerState: SamplerState.PointClamp, effect: PostEffect);
    SpriteBatch.Draw(_screenRenderTarget, destination, Color.White);
    SpriteBatch.End();

    // Debug remains readable above the pause overlay.
    if (DebugEnabled)
    {
      SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
      CurrentScene?.Debug.Draw(SpriteBatch, FontDebug, Pixel);
      SpriteBatch.End();
    }
  }

  private void DrawPauseOverlay()
  {
    SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
    SpriteBatch.Draw(
      Pixel,
      new Rectangle(0, 0, BASE_SCREEN_WIDTH, BASE_SCREEN_HEIGHT),
      Color.Black * 0.62f);

    const string title = "PAUSED";
    Vector2 titleSize = FontTitle.MeasureString(title);
    Vector2 titlePosition = new(
      (BASE_SCREEN_WIDTH - titleSize.X) * 0.5f,
      (BASE_SCREEN_HEIGHT - titleSize.Y) * 0.5f - 12f);
    FontTitle.DrawText(SpriteBatch, title, titlePosition, Color.Yellow);

    const string hint = "Press ESC to resume";
    Vector2 hintSize = FontText.MeasureString(hint);
    Vector2 hintPosition = new(
      (BASE_SCREEN_WIDTH - hintSize.X) * 0.5f,
      titlePosition.Y + titleSize.Y + 14f);
    FontText.DrawText(SpriteBatch, hint, hintPosition, new Color(255, 235, 150));
    SpriteBatch.End();
  }

  public void GoToScene(Scene scene)
  {
    if (_startingScene || _leavingScene)
      return;

    _nextScene = scene;
    if (CurrentScene is null)
    {
      CurrentScene = scene;
      _startingScene = true;
    }
    else
    {
      _leavingScene = true;
    }
  }
}
