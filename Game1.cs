using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CSCCDevBattle;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;

    public SpriteBatch SpriteBatch { get; private set; } = null!;
    public Texture2D Pixel { get; private set; } = null!;

    public bool DebugEnabled { get; private set; } = true;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);

        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _graphics.PreferredBackBufferWidth = 960;
        _graphics.PreferredBackBufferHeight = 540;
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        SpriteBatch = new SpriteBatch(GraphicsDevice);

        Pixel = new Texture2D(GraphicsDevice, 1, 1);
        Pixel.SetData(new[] { Color.White });
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Gray);

        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
    }

}
