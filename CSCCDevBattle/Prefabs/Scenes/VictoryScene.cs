using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>Personalized victory screen for the completed CSCC Dev Battle.</summary>
public sealed class VictoryScene : Scene
{
    private const float FadeSpeed = 5f;
    private readonly string _playerName;
    private float _fade = 1f;

    public VictoryScene(string playerName)
    {
        _playerName = string.IsNullOrWhiteSpace(playerName) ? "PLAYER" : playerName.Trim().ToUpperInvariant();
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
        game.GraphicsDevice.Clear(new Color(8, 10, 14));
        SpriteBatch batch = game.SpriteBatch;
        batch.Begin(samplerState: SamplerState.PointClamp);

        if (game.PlayerName.Equals("envy", StringComparison.OrdinalIgnoreCase))
        {
            batch.Draw(Assets.GetTexture("envy"), new Rectangle(96,74,400,528-74), Color.White);
        }

        batch.Draw(game.Pixel, new Rectangle(34, 72, 532, 2), new Color(130, 130, 130));
        batch.Draw(game.Pixel, new Rectangle(34, 528, 532, 2), new Color(130, 130, 130));
        DrawCentered(batch, game.FontTitle, "VICTORY", 126f, Color.Yellow);
        DrawCentered(batch, game.FontText, _playerName, 232f, Color.FromNonPremultiplied(0.45f,0.5f,1.0f,1.0f));
        DrawCentered(batch, game.FontText, "YOU WON THE", 292f, Color.LightGray);
        DrawCentered(batch, game.FontTitle, "CSCC DEV BATTLE", 340f, Color.FromNonPremultiplied(0.45f,0.5f,1.0f,1.0f));
        DrawCentered(batch, game.FontText, "PRESS ANY KEY TO CONTINUE", 474f, Color.LightGray);

        if (_fade > 0f)
            batch.Draw(game.Pixel, new Rectangle(0, 0, Game1.BASE_SCREEN_WIDTH, Game1.BASE_SCREEN_HEIGHT), Color.Black * _fade);
        batch.End();
    }

    private static void DrawCentered(SpriteBatch batch, FontStashSharp.SpriteFontBase font, string text, float y, Color color)
    {
        Vector2 size = font.MeasureString(text);
        font.DrawText(batch, text, new Vector2((Game1.BASE_SCREEN_WIDTH - size.X) * 0.5f, y), color);
    }
}
