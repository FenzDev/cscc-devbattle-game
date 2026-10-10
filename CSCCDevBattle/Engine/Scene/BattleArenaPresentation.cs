using System;
using Microsoft.Xna.Framework;

/// <summary>
/// Owns the animated arena/player presentation when entering and leaving attacks.
/// It does not decide which encounter step comes next.
/// </summary>
public sealed class BattleArenaPresentation
{
    public const float HudHeight = 76f;
    public const float TransitionDuration = 0.35f;
    public const float ActivePlayerOpacity = 0.18f;

    private readonly BattleContext _battle;

    private float _transitionElapsed;

    private Vector2 _startArenaCenter;
    private Vector2 _startArenaSize;
    private Vector2 _targetArenaCenter;
    private Vector2 _targetArenaSize;

    private Vector2 _startPlayerPosition;
    private Vector2 _targetPlayerPosition;

    private float _startPlayerAlpha;
    private float _targetPlayerAlpha;

    public float PlayerVisualAlpha { get; private set; } = 1f;

    public BattleArenaPresentation(BattleContext battle)
    {
        _battle = battle ?? throw new ArgumentNullException(nameof(battle));
    }

    public Vector2 DialogueArenaSize =>
        new(Game1.BASE_SCREEN_WIDTH - 40f, 120f);

    public Vector2 DialogueArenaCenter
    {
        get
        {
            float screenSize = Game1.BASE_SCREEN_WIDTH;
            Vector2 size = DialogueArenaSize;
            float hudTop = screenSize - HudHeight;
            float centerY = hudTop - 12f - size.Y / 2f;

            return new Vector2(screenSize / 2f, centerY);
        }
    }

    public Vector2 GetWaveArenaCenter(BattleWave wave)
    {
        float screenSize = Game1.BASE_SCREEN_WIDTH;

        return wave.ArenaCenter ??
               new Vector2(screenSize / 2f, screenSize / 2f);
    }

    public void SetDialogueLayout()
    {
        _battle.Arena.Center = DialogueArenaCenter;
        _battle.Arena.Size = DialogueArenaSize;
        _battle.Player.Position = DialogueArenaCenter;
        PlayerVisualAlpha = 1f;
        _transitionElapsed = TransitionDuration;
    }

    public void BeginTransition(
        Vector2 targetCenter,
        Vector2 targetSize,
        float targetPlayerAlpha)
    {
        _transitionElapsed = 0f;

        _startArenaCenter = _battle.Arena.Center;
        _startArenaSize = _battle.Arena.Size;
        _targetArenaCenter = targetCenter;
        _targetArenaSize = targetSize;

        _startPlayerPosition = _battle.Player.Position;
        _targetPlayerPosition = targetCenter;

        _startPlayerAlpha = PlayerVisualAlpha;
        _targetPlayerAlpha = MathHelper.Clamp(targetPlayerAlpha, 0f, 1f);
    }

    public bool UpdateTransition(float deltaTime)
    {
        _transitionElapsed += MathF.Max(0f, deltaTime);

        float t = MathHelper.Clamp(
            _transitionElapsed / TransitionDuration,
            0f,
            1f);

        // Smoothstep: gentle acceleration and deceleration.
        float eased = t * t * (3f - 2f * t);

        _battle.Arena.Center = Vector2.Lerp(
            _startArenaCenter,
            _targetArenaCenter,
            eased);

        _battle.Arena.Size = Vector2.Lerp(
            _startArenaSize,
            _targetArenaSize,
            eased);

        _battle.Player.Position = Vector2.Lerp(
            _startPlayerPosition,
            _targetPlayerPosition,
            eased);

        PlayerVisualAlpha = MathHelper.Lerp(
            _startPlayerAlpha,
            _targetPlayerAlpha,
            eased);

        return t >= 1f;
    }
}
