using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>Shows the fallen player at their actual battle position; any key returns to the menu.</summary>
public sealed class GameOverScene : Scene
{
    private const float FadeSpeed = 5f;
    private readonly Texture2D? _playerTexture;
    private readonly Vector2 _playerPosition;
    private readonly float _playerRotation;
    private readonly Vector2 _arenaCenter;
    private readonly Vector2 _arenaSize;
    private readonly string _playerName;
    private float _fade = 1f;

    public GameOverScene(Texture2D? playerTexture, Vector2 playerPosition, float playerRotation,
        Vector2 arenaCenter, Vector2 arenaSize, string playerName)
    {
        _playerTexture = playerTexture;
        _playerPosition = playerPosition;
        _playerRotation = playerRotation;
        _arenaCenter = arenaCenter;
        _arenaSize = arenaSize;
        _playerName = string.IsNullOrWhiteSpace(playerName) ? "PLAYER" : playerName.ToUpperInvariant();
    }

    public override TransitionState SetupTick(GameTime gameTime)
    {
        _fade = MathF.Max(0f, _fade - FadeSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds);
        return _fade > 0f ? TransitionState.InProgress : TransitionState.Finished;
    }

    public override TransitionState CleanupTick(GameTime gameTime)
    {
        _fade = MathF.Min(1f, _fade + FadeSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds);
        return _fade < 1f ? TransitionState.InProgress : TransitionState.Finished;
    }

    public override void Tick(GameTime gameTime, Game1 game)
    {
        if (Input.AnyKeyPressed())
            game.GoToScene(new MainMenuScene());
    }

    public override void Draw(GameTime gameTime, Game1 game)
    {
        game.GraphicsDevice.Clear(Color.Black);
        SpriteBatch batch = game.SpriteBatch;
        batch.Begin(samplerState: SamplerState.PointClamp);

        // Retain the arena's last location to ground the death pose in the battle that ended.
        Vector2 half = _arenaSize * 0.5f;
        Rectangle arena = new((int)(_arenaCenter.X - half.X), (int)(_arenaCenter.Y - half.Y),
            (int)_arenaSize.X, (int)_arenaSize.Y);
        DrawOutline(batch, game.Pixel, arena, 3, new Color(80, 32, 36));

        if (_playerTexture is not null)
        {
            Vector2 origin = new(_playerTexture.Width * 0.5f, _playerTexture.Height * 0.5f);
            // Sideways, dimmed, and cracked: a simple broken pose using the existing player sprite.
            batch.Draw(_playerTexture, _playerPosition, null, new Color(150, 150, 150),
                _playerRotation + MathHelper.PiOver2, origin, new Vector2(2f), SpriteEffects.None, 0f);
            // batch.Draw(game.Pixel,
            //     new Vector2(_playerPosition.X - 3f, _playerPosition.Y), null,
            //     new Color(90, 20, 24), 0.45f, new Vector2(0.5f), new Vector2(7f, 1.5f),
            //     SpriteEffects.None, 0f);
            // batch.Draw(game.Pixel,
            //     new Rectangle((int)_playerPosition.X - 11, (int)_playerPosition.Y + 8, 3, 2),
            //     new Color(140, 45, 45));
            // batch.Draw(game.Pixel,
            //     new Rectangle((int)_playerPosition.X + 8, (int)_playerPosition.Y - 7, 2, 2),
            //     new Color(140, 45, 45));
        }

        DrawCentered(batch, game.FontTitle, "GAME OVER", 70f, new Color(255, 75, 75));
        DrawCentered(batch, game.FontText, $"{_playerName} WAS DEFEATED", 125f, Color.White);
        DrawCentered(batch, game.FontText, "WAKE UP... YOU CAN MAKE THE DIFFERENCE...", 465f, Color.FromNonPremultiplied(0x6b,0xa3,0xff,0xff));
        DrawCentered(batch, game.FontText, "PRESS ANY KEY TO RETURN", 510f, Color.Yellow);

        if (_fade > 0f)
            batch.Draw(game.Pixel, new Rectangle(0, 0, Game1.BASE_SCREEN_WIDTH, Game1.BASE_SCREEN_HEIGHT), Color.Black * _fade);
        batch.End();
    }

    private static void DrawCentered(SpriteBatch batch, FontStashSharp.SpriteFontBase font, string text, float y, Color color)
    {
        Vector2 size = font.MeasureString(text);
        font.DrawText(batch, text, new Vector2((Game1.BASE_SCREEN_WIDTH - size.X) * 0.5f, y), color);
    }

    private static void DrawOutline(SpriteBatch batch, Texture2D pixel, Rectangle r, int thickness, Color color)
    {
        batch.Draw(pixel, new Rectangle(r.X, r.Y, r.Width, thickness), color);
        batch.Draw(pixel, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), color);
        batch.Draw(pixel, new Rectangle(r.X, r.Y, thickness, r.Height), color);
        batch.Draw(pixel, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), color);
    }
}
