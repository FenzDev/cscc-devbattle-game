
using System;

public sealed class DialogWave : BattleWave
{
    public string Speaker { get; }
    public string Text { get; }

    // Time between characters, in seconds.
    public float SecondsPerCharacter { get; }

    // Maximum random displacement in pixels.
    public float ShakeAmount { get; }

    public int VisibleCharacterCount { get; private set; }

    public bool IsFullyRevealed =>
        VisibleCharacterCount >= Text.Length;

    private float _revealAccumulator;
    private bool _ignoreInputOnFirstUpdate;
    private bool _skipRequested;

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

        Text = text
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');

        SecondsPerCharacter = MathF.Max(
            0f,
            secondsPerCharacter);

        ShakeAmount = MathF.Max(
            0f,
            shakeAmount);

        Duration = MathF.Max(
            MathF.Max(0f, duration),
            Text.Length * SecondsPerCharacter);
    }

    protected override void OnStart(BattleContext battle)
    {
        VisibleCharacterCount = 0;
        _revealAccumulator = 0f;
        _skipRequested = false;

        // Prevent the key that started the encounter from
        // immediately skipping its first dialogue.
        _ignoreInputOnFirstUpdate = true;
    }

    protected override void OnUpdate(
        BattleContext battle,
        float deltaTime)
    {
        bool allowInput = !_ignoreInputOnFirstUpdate;
        _ignoreInputOnFirstUpdate = false;

        if (allowInput && Input.AnyKeyPressed())
        {
            if (!IsFullyRevealed)
            {
                // First press: reveal all text immediately.
                VisibleCharacterCount = Text.Length;
                _revealAccumulator = 0f;
            }
            else
            {
                // Second press: finish the dialogue immediately.
                _skipRequested = true;
            }
        }

        if (!IsFullyRevealed && !_skipRequested)
        {
            if (SecondsPerCharacter <= 0f)
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

        if (Elapsed >= Duration)
        {
            VisibleCharacterCount = Text.Length;
        }

        if (_skipRequested)
        {
            // End this wave rather than waiting for its original timer.
            // BattleWave must support ending early for this to take effect.
            Duration = MathF.Min(Duration, Elapsed);
            VisibleCharacterCount = Text.Length;
        }
    }
}
