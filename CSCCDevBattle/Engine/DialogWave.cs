using System;

public sealed class DialogWave : BattleWave
{
    public string Speaker { get; }
    public string Text { get; }

    // Time between characters, in seconds.
    // 0.035 means approximately 28 characters per second.
    public float SecondsPerCharacter { get; }

    // Maximum random displacement in pixels, independently
    // for X and Y. A value of 2 means offsets from -2 to +2.
    public float ShakeAmount { get; }

    public int VisibleCharacterCount { get; private set; }

    public bool IsFullyRevealed =>
        VisibleCharacterCount >= Text.Length;

    private float _revealAccumulator;
    private bool _ignoreInputOnFirstUpdate;

    public DialogWave(
        string speaker,
        string text,
        float duration = 3f,
        float secondsPerCharacter = 0.035f,
        float shakeAmount = 0f)
    {
        ArgumentNullException.ThrowIfNull(speaker);
        ArgumentNullException.ThrowIfNull(text);

        Speaker = speaker;

        // Normalize Windows and Unix line endings.
        Text = text
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');

        SecondsPerCharacter = MathF.Max(
            0f,
            secondsPerCharacter);

        ShakeAmount = MathF.Max(
            0f,
            shakeAmount);

        // Never allow the typewriter effect to outlast the wave.
        // Duration remains the minimum requested total duration.
        Duration = MathF.Max(
            MathF.Max(0f, duration),
            Text.Length * SecondsPerCharacter);
    }

    protected override void OnStart(BattleContext battle)
    {
        VisibleCharacterCount = 0;
        _revealAccumulator = 0f;

        // Prevent the key used to start an encounter from
        // instantly skipping its first dialogue.
        _ignoreInputOnFirstUpdate = true;
    }

    protected override void OnUpdate(
        BattleContext battle,
        float deltaTime)
    {
        bool allowSkip = !_ignoreInputOnFirstUpdate;
        _ignoreInputOnFirstUpdate = false;

        if (!IsFullyRevealed)
        {
            // Reveal everything immediately when a key is pressed.
            if (allowSkip && Input.AnyKeyPressed())
            {
                VisibleCharacterCount = Text.Length;
                _revealAccumulator = 0f;
            }
            else if (SecondsPerCharacter <= 0f)
            {
                VisibleCharacterCount = Text.Length;
            }
            else
            {
                _revealAccumulator += MathF.Max(0f, deltaTime);

                int charactersToReveal =
                    (int)(_revealAccumulator / SecondsPerCharacter);

                if (charactersToReveal > 0)
                {
                    VisibleCharacterCount = Math.Min(
                        Text.Length,
                        VisibleCharacterCount + charactersToReveal);

                    _revealAccumulator -=
                        charactersToReveal * SecondsPerCharacter;
                }
            }
        }

        // Ensure no character is left unrevealed when the
        // base BattleWave duration reaches its endpoint.
        if (Elapsed >= Duration)
            VisibleCharacterCount = Text.Length;
    }
}