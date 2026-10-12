using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

/// <summary>Name-entry title screen. Names are normalized to uppercase before starting battle.</summary>
public sealed class MainMenuScene : Scene
{
    private const float FadeSpeed = 5f;
    private const int MaxNameLength = 14;
    private float _fade = 1f;
    private string _name;

    public MainMenuScene()
    {
        _name = string.IsNullOrWhiteSpace(Game1.Singleton.PlayerName)
            ? "PLAYER"
            : Game1.Singleton.PlayerName.Trim().ToUpperInvariant();
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
        // Uppercase A-Z input is deterministic and independent of keyboard layout.
        for (int i = 0; i < 26; i++)
        {
            if (Input.Pressed((Keys)((int)Keys.A + i)))
                Append(((char)('A' + i)).ToString());
        }

        if (Input.Pressed(Keys.Space))
            Append(" ");
        if (Input.Pressed(Keys.OemMinus))
            Append("-");
        if (Input.Pressed(Keys.Back) && _name.Length > 0)
            _name = _name[..^1];

        if (Input.Pressed(Keys.Enter))
        {
            string normalizedName = _name.Trim().ToUpperInvariant();
            game.PlayerName = string.IsNullOrEmpty(normalizedName) ? "PLAYER" : normalizedName;

            BattleScene battle = new();
            battle.BeginEncounter(DemoEncounterFactory.CreateDepartmentEncounter());
            game.GoToScene(battle);
        }
    }

    private void Append(string value)
    {
        if (_name.Length >= MaxNameLength)
            return;
        if (value == " " && (_name.Length == 0 || _name.EndsWith(' ')))
            return;
        _name += value.ToUpperInvariant();
    }

    public override void Draw(GameTime gameTime, Game1 game)
    {
        GraphicsDevice device = game.GraphicsDevice;
        device.Clear(new Color(8, 10, 17));
        SpriteBatch batch = game.SpriteBatch;
        batch.Begin(samplerState: SamplerState.PointClamp);

        // Restrained retro terminal framing.
        batch.Draw(game.Pixel, new Rectangle(38, 54, 524, 2), new Color(105, 105, 105));
        batch.Draw(game.Pixel, new Rectangle(38, 544, 524, 2), new Color(105, 105, 105));

        DrawCentered(batch, game.FontTitle, "CSCC DEV BATTLE", 110f, Color.Yellow);
        DrawCentered(batch, game.FontText, "A DEPARTMENT SURVIVAL CHALLENGE", 178f, Color.LightGray);
        DrawCentered(batch, game.FontText, "ENTER YOUR NAME", 270f, Color.White);

        Rectangle nameBox = new(110, 316, 380, 54);
        batch.Draw(game.Pixel, nameBox, new Color(18, 20, 29));
        DrawOutline(batch, game.Pixel, nameBox, 2, new Color(185, 185, 185));
        string visibleName = string.IsNullOrEmpty(_name) ? "PLAYER" : _name;
        DrawCentered(batch, game.FontTitle, visibleName, 318f, string.IsNullOrWhiteSpace(_name)? Color.Gray: Color.Yellow);

        DrawCentered(batch, game.FontText, "TYPE TO EDIT  |  BACKSPACE TO DELETE", 405f, Color.LightGray);
        DrawCentered(batch, game.FontText, "ENTER  -  START ENCOUNTER", 465f, Color.White);
        DrawCentered(batch, game.FontText, "MOBILE  /  WEB  /  AI", 510f, new Color(155, 155, 155));

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
