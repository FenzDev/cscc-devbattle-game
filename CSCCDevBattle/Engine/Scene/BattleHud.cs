using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// Owns dialogue rendering, the player HUD, and HP-bar animation state.
/// </summary>
public sealed class BattleHud
{
    private const float HpTrailDelay = 0.45f;
    private const float HpTrailSpeed = 2.5f;
    private const float HpFlashDuration = 0.22f;
    private const float HpShakeDuration = 0.18f;

    private readonly BattleContext _battle;

    private float _hpFillRatio = 1f;
    private float _hpTrailRatio = 1f;
    private float _lastObservedPlayerHealth;
    private float _hpTrailDelayRemaining;
    private float _hpFlashRemaining;
    private float _hpShakeRemaining;

    public string HealthBarDebugMessage =>
        $"HP={_battle.PlayerHealth:0.#}/{_battle.PlayerMaxHealth:0.#} | " +
        $"Fill={_hpFillRatio:0.00} | Trail={_hpTrailRatio:0.00} | " +
        $"Last={_lastObservedPlayerHealth:0.#}";

    public BattleHud(BattleContext battle)
    {
        _battle = battle ?? throw new ArgumentNullException(nameof(battle));

        float maxHealth = MathF.Max(0f, _battle.PlayerMaxHealth);
        float health = Math.Clamp(_battle.PlayerHealth, 0f, maxHealth);
        float ratio = maxHealth > 0f ? health / maxHealth : 0f;

        _lastObservedPlayerHealth = health;
        _hpFillRatio = ratio;
        _hpTrailRatio = ratio;
    }

    public void Update(float deltaTime)
    {
        deltaTime = MathF.Max(0f, deltaTime);

        float maxHealth = MathF.Max(0f, _battle.PlayerMaxHealth);
        float health = Math.Clamp(_battle.PlayerHealth, 0f, maxHealth);
        float targetRatio = maxHealth > 0f ? health / maxHealth : 0f;

        _hpFlashRemaining = MathF.Max(0f, _hpFlashRemaining - deltaTime);
        _hpShakeRemaining = MathF.Max(0f, _hpShakeRemaining - deltaTime);

        bool tookDamage = health < _lastObservedPlayerHealth;
        bool wasHealed = health > _lastObservedPlayerHealth;

        if (tookDamage)
        {
            float oldRatio = maxHealth > 0f
                ? Math.Clamp(_lastObservedPlayerHealth / maxHealth, 0f, 1f)
                : 0f;

            _hpTrailRatio = MathF.Max(_hpTrailRatio, oldRatio);
            _hpTrailDelayRemaining = HpTrailDelay;
            _hpFlashRemaining = HpFlashDuration;
            _hpShakeRemaining = HpShakeDuration;
        }
        else if (wasHealed)
        {
            // Healing should not leave an orange/blue damage trail.
            _hpTrailRatio = _hpFillRatio;
            _hpTrailDelayRemaining = 0f;
        }

        _lastObservedPlayerHealth = health;

        _hpFillRatio = MathHelper.Lerp(
            _hpFillRatio,
            targetRatio,
            Math.Clamp(deltaTime * 18f, 0f, 1f));

        if (!tookDamage)
        {
            if (_hpTrailDelayRemaining > 0f)
            {
                _hpTrailDelayRemaining = MathF.Max(
                    0f,
                    _hpTrailDelayRemaining - deltaTime);
            }
            else
            {
                _hpTrailRatio = MathHelper.Lerp(
                    _hpTrailRatio,
                    targetRatio,
                    Math.Clamp(deltaTime * HpTrailSpeed, 0f, 1f));
            }
        }

        _hpFillRatio = Math.Clamp(_hpFillRatio, 0f, 1f);
        _hpTrailRatio = Math.Clamp(
            MathF.Max(_hpTrailRatio, _hpFillRatio),
            0f,
            1f);
    }

    public void DrawPlayerHud(Game1 game)
    {
        float screenSize = Game1.BASE_SCREEN_WIDTH;
        SpriteBatch spriteBatch = game.SpriteBatch;
        var font = game.FontText;
        float hudTop = screenSize - BattleArenaPresentation.HudHeight;

        spriteBatch.Draw(
            game.Pixel,
            new Rectangle(20, (int)hudTop, (int)screenSize - 40, 2),
            Color.White);

        float textY = hudTop + 20f;

        font.DrawText(
            spriteBatch,
            _battle.PlayerName,
            new Vector2(24f, textY),
            Color.White);

        float maxHealth = MathF.Max(0f, _battle.PlayerMaxHealth);
        float health = Math.Clamp(_battle.PlayerHealth, 0f, maxHealth);
        string hpText = $"HP {health:0.#}/{maxHealth:0.#}";
        Vector2 hpTextSize = font.MeasureString(hpText);

        float hpTextX = screenSize - hpTextSize.X - 24f;
        const float barWidth = 112f;
        const float barHeight = 24f;
        const float gap = 12f;

        float barX = hpTextX - gap - barWidth;
        float barY = 2f + textY + (hpTextSize.Y - barHeight) / 2f;

        float shakeStrength = _hpShakeRemaining > 0f
            ? _hpShakeRemaining / HpShakeDuration
            : 0f;

        Vector2 shake = new(
            (Random.Shared.NextSingle() * 2f - 1f) * 3f * shakeStrength,
            (Random.Shared.NextSingle() * 2f - 1f) * 2f * shakeStrength);

        Rectangle outer = new(
            (int)(barX + shake.X),
            (int)(barY + shake.Y),
            (int)barWidth,
            (int)barHeight);

        float flash = Math.Clamp(_hpFlashRemaining / HpFlashDuration, 0f, 1f);
        Color backgroundColor = Color.Lerp(
            new Color(45, 25, 25),
            new Color(130, 35, 35),
            flash * 0.5f);

        spriteBatch.Draw(game.Pixel, outer, backgroundColor);

        Rectangle inner = new(
            outer.X + 2,
            outer.Y + 2,
            outer.Width - 4,
            outer.Height - 4);

        spriteBatch.Draw(game.Pixel, inner, new Color(25, 25, 25));

        int fillWidth = Math.Clamp(
            (int)MathF.Round(inner.Width * _hpFillRatio),
            0,
            inner.Width);

        int trailEnd = Math.Clamp(
            (int)MathF.Round(inner.Width * _hpTrailRatio),
            0,
            inner.Width);

        Color fillColor = _hpFillRatio > 0.5f
            ? new Color(50, 60, 220)
            : new Color(255, 65, 65);

        fillColor = Color.Lerp(fillColor, Color.White, flash * 0.8f);

        if (fillWidth > 0)
        {
            spriteBatch.Draw(
                game.Pixel,
                new Rectangle(inner.X, inner.Y, fillWidth, inner.Height),
                fillColor);
        }

        int trailWidth = Math.Max(0, trailEnd - fillWidth);
        if (trailWidth > 0)
        {
            Color trailColor = Color.Lerp(
                new Color(180, 200, 230),
                Color.White,
                flash * 0.65f);

            spriteBatch.Draw(
                game.Pixel,
                new Rectangle(
                    inner.X + fillWidth,
                    inner.Y,
                    trailWidth,
                    inner.Height),
                trailColor);
        }

        DrawUiOutline(spriteBatch, game.Pixel, outer, 2,
            Color.Lerp(Color.White, Color.Red, flash));

        Color hpColor = Color.Lerp(
            Color.White,
            new Color(255, 100, 100),
            flash);

        font.DrawText(spriteBatch, hpText, new Vector2(hpTextX, textY), hpColor);
    }

    public void DrawDialoguePanel(Game1 game)
    {
        if (_battle.CurrentWave is not DialogWave dialog)
            return;

        SpriteBatch spriteBatch = game.SpriteBatch;
        int screenSize = Game1.BASE_SCREEN_WIDTH;
        Rectangle box = new(20, 20, screenSize - 40, 86);

        spriteBatch.Draw(game.Pixel, box, Color.Black);
        DrawUiOutline(spriteBatch, game.Pixel, box, 2, Color.White);

        game.FontText.DrawText(
            spriteBatch,
            dialog.Speaker,
            new Vector2(box.X + 14f, box.Y + 8f),
            Color.Yellow);

        DrawDialogText(
            game,
            dialog,
            new Vector2(box.X + 14f, box.Y + 38f));
    }

    private static void DrawDialogText(
        Game1 game,
        DialogWave dialog,
        Vector2 position)
    {
        SpriteBatch spriteBatch = game.SpriteBatch;
        var font = game.FontText;
        string text = dialog.Text;
        int visibleCount = Math.Clamp(dialog.VisibleCharacterCount, 0, text.Length);
        float lineHeight = font.MeasureString("Ag").Y + 2f;

        int lineStart = 0;
        int lineNumber = 0;

        for (int i = 0; i < visibleCount; i++)
        {
            char character = text[i];

            if (character == '\n')
            {
                lineStart = i + 1;
                lineNumber++;
                continue;
            }

            string prefixBeforeCharacter = text.Substring(lineStart, i - lineStart);
            float x = prefixBeforeCharacter.Length == 0
                ? 0f
                : font.MeasureString(prefixBeforeCharacter).X;
            float y = lineNumber * lineHeight;

            Vector2 shake = Vector2.Zero;
            if (dialog.ShakeAmount > 0f && !char.IsWhiteSpace(character))
            {
                shake = new Vector2(
                    (Random.Shared.NextSingle() * 2f - 1f) * dialog.ShakeAmount,
                    (Random.Shared.NextSingle() * 2f - 1f) * dialog.ShakeAmount);
            }

            font.DrawText(
                spriteBatch,
                character.ToString(),
                position + new Vector2(x, y) + shake,
                Color.White);
        }
    }

    private static void DrawUiOutline(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        Rectangle rectangle,
        int thickness,
        Color color)
    {
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness),
            color);
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X, rectangle.Bottom - thickness, rectangle.Width, thickness),
            color);
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height),
            color);
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.Right - thickness, rectangle.Y, thickness, rectangle.Height),
            color);
    }
}
