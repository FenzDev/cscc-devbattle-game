using System;

public sealed class PlayerHitFlashBehaviour : Behaviour
{
    private const float FlashDuration = 0.20f;
    private const float BlinkInterval = 0.075f;

    private float _flashRemaining;
    private float _blinkElapsed;

    private int _lastDamageEventCount;
    private bool _wasInvulnerable;

    public override void Update(
        Entity entity,
        BattleContext battle,
        float deltaTime)
    {
        if (entity is not PlayerEntity player)
            return;

        deltaTime = MathF.Max(0f, deltaTime);

        // Detect a successfully applied hit.
        if (_lastDamageEventCount != battle.DamageEventCount)
        {
            _lastDamageEventCount = battle.DamageEventCount;
            _flashRemaining = FlashDuration;
        }

        // Brief white flash that gradually fades.
        _flashRemaining = MathF.Max(
            0f,
            _flashRemaining - deltaTime);

        player.HitFlashAmount = FlashDuration > 0f
            ? _flashRemaining / FlashDuration
            : 0f;

        // Flicker while temporary damage immunity is active.
        bool invulnerable =
            battle.DamageCooldownRemaining > 0f;

        if (invulnerable)
        {
            if (!_wasInvulnerable)
                _blinkElapsed = 0f;

            _blinkElapsed += deltaTime;

            bool blinkOn =
                ((int)(_blinkElapsed / BlinkInterval) % 2) == 0;

            player.DamageBlinkOpacity = blinkOn ? 0.25f : 1f;
        }
        else
        {
            player.DamageBlinkOpacity = 1f;
            _blinkElapsed = 0f;
        }

        _wasInvulnerable = invulnerable;
    }
}