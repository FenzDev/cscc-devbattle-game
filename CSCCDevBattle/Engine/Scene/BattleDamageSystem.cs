using System;

/// <summary>
/// Owns the rules for applying damage, healing, and damage cooldowns.
/// Health and damage-event state remain stored in BattleContext.
/// </summary>
public sealed class BattleDamageSystem
{
    private readonly BattleContext _battle;

    public BattleDamageSystem(BattleContext battle)
    {
        _battle = battle ?? throw new ArgumentNullException(nameof(battle));
    }

    public bool TryDamagePlayer(float amount)
    {
        if (!float.IsFinite(amount) || amount <= 0f)
            return false;

        if (_battle.InvincibleMode ||
            _battle.IsPlayerDead ||
            _battle.DamageCooldownRemaining > 0f)
        {
            return false;
        }

        float maxHealth = MathF.Max(0f, _battle.PlayerMaxHealth);
        float previousHealth = Math.Clamp(
            _battle.PlayerHealth,
            0f,
            maxHealth);

        if (previousHealth <= 0f)
            return false;

        _battle.PlayerHealth = Math.Clamp(
            previousHealth - amount,
            0f,
            maxHealth);

        _battle.LastDamageAmount =
            previousHealth - _battle.PlayerHealth;

        if (_battle.LastDamageAmount <= 0f)
            return false;

        _battle.DamageEventCount++;
        _battle.DamageCooldownRemaining = MathF.Max(
            0f,
            _battle.DamageCooldownDuration);

        return true;
    }

    public float HealPlayer(float amount)
    {
        if (!float.IsFinite(amount) || amount <= 0f)
            return 0f;

        float maxHealth = MathF.Max(0f, _battle.PlayerMaxHealth);
        float previousHealth = Math.Clamp(
            _battle.PlayerHealth,
            0f,
            maxHealth);

        _battle.PlayerHealth = Math.Clamp(
            previousHealth + amount,
            0f,
            maxHealth);

        return _battle.PlayerHealth - previousHealth;
    }

    public void UpdateCooldown(float deltaTime)
    {
        _battle.DamageCooldownRemaining = MathF.Max(
            0f,
            _battle.DamageCooldownRemaining - MathF.Max(0f, deltaTime));
    }
}
