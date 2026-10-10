using System;
using Microsoft.Xna.Framework.Input;

/// <summary>
/// Centralizes BattleScene debug command and information registration.
/// </summary>
public sealed class BattleSceneDebug
{
    private readonly Action<Keys, string, Action, Func<string>, KeyTrigger> _registerCommand;
    private readonly Action<string, Func<string>> _registerInfo;
    private readonly BattleContext _battle;
    private readonly BattleDamageSystem _damage;
    private readonly BattleCollisionController _collisions;
    private readonly BattleEncounterController _encounter;
    private readonly BattleHud _hud;
    private readonly Action _startDemoEncounter;
    private readonly Action _startTestWave;

    public bool MagentaScreenEnabled { get; private set; }
    public bool EntityMasksVisible { get; private set; }

    public BattleSceneDebug(
        Action<Keys, string, Action, Func<string>, KeyTrigger> registerCommand,
        Action<string, Func<string>> registerInfo,
        BattleContext battle,
        BattleDamageSystem damage,
        BattleCollisionController collisions,
        BattleEncounterController encounter,
        BattleHud hud,
        Action startDemoEncounter,
        Action startTestWave)
    {
        _registerCommand = registerCommand ?? throw new ArgumentNullException(nameof(registerCommand));
        _registerInfo = registerInfo ?? throw new ArgumentNullException(nameof(registerInfo));
        _battle = battle ?? throw new ArgumentNullException(nameof(battle));
        _damage = damage ?? throw new ArgumentNullException(nameof(damage));
        _collisions = collisions ?? throw new ArgumentNullException(nameof(collisions));
        _encounter = encounter ?? throw new ArgumentNullException(nameof(encounter));
        _hud = hud ?? throw new ArgumentNullException(nameof(hud));
        _startDemoEncounter = startDemoEncounter ?? throw new ArgumentNullException(nameof(startDemoEncounter));
        _startTestWave = startTestWave ?? throw new ArgumentNullException(nameof(startTestWave));
    }

    public void Register()
    {
        _registerCommand(
            Keys.F1,
            "Magenta Screen",
            () => MagentaScreenEnabled = !MagentaScreenEnabled,
            () => MagentaScreenEnabled ? "Enabled" : "Disabled",
            KeyTrigger.Released);

        _registerCommand(
            Keys.F2,
            "Entity Masks",
            () => EntityMasksVisible = !EntityMasksVisible,
            () => EntityMasksVisible ? "Enabled" : "Disabled",
            KeyTrigger.Released);

        // F3 is deliberately registered only once (it previously had two commands).
        _registerCommand(
            Keys.F3,
            "Start Demo Encounter",
            _startDemoEncounter,
            () => _encounter.StateName,
            KeyTrigger.Released);

        _registerCommand(
            Keys.F4,
            "Finish Current Wave",
            _encounter.FinishCurrentWave,
            () => _battle.CurrentWave is { } wave
                ? $"Running: {wave.GetType().Name}"
                : "No active wave",
            KeyTrigger.Released);

        _registerCommand(
            Keys.F5,
            "Restart Encounter",
            _encounter.RestartEncounter,
            () => _encounter.Definition is null
                ? "No encounter"
                : $"{_encounter.Definition.Name} | {_encounter.StateName}",
            KeyTrigger.Released);

        _registerCommand(
            Keys.F6,
            "Invincible Mode",
            () =>
            {
                _battle.InvincibleMode = !_battle.InvincibleMode;
                if (_battle.InvincibleMode)
                    _battle.PlayerHealth = _battle.PlayerMaxHealth;
            },
            () => _battle.InvincibleMode ? "Enabled" : "Disabled",
            KeyTrigger.Released);

        _registerCommand(
            Keys.F7,
            "Test Damage (-1 HP)",
            () => _damage.TryDamagePlayer(1f),
            () => $"HP {_battle.PlayerHealth:0.#}/{_battle.PlayerMaxHealth:0.#}",
            KeyTrigger.Released);

        _registerCommand(
            Keys.F8,
            "Heal (+1 HP)",
            () => _damage.HealPlayer(1f),
            () => $"HP {_battle.PlayerHealth:0.#}/{_battle.PlayerMaxHealth:0.#}",
            KeyTrigger.Released);

        _registerCommand(
            Keys.F9,
            "Start Single Test Wave",
            _startTestWave,
            () => _encounter.StateName,
            KeyTrigger.Released);

        _registerInfo(
            "Entities/Collision Actors",
            () => $"{_battle.Entities.Count}/{_collisions.RegisteredEntityCount}");

        _registerInfo("Flow State", () => _encounter.StateName);
        _registerInfo("Encounter", () => _encounter.Definition?.Name ?? "None");
        _registerInfo("Phase / Progress", () => _encounter.ProgressMessage);
        _registerInfo("Current Wave", () => _encounter.CurrentWaveMessage);
        _registerInfo("Arena", () =>
            $"{_battle.Arena.Size.X:0} x {_battle.Arena.Size.Y:0}");
        _registerInfo("Damage Blink Opacity", () => $"{_battle.Player.DamageBlinkOpacity:0.00}");
        _registerInfo("HP Bar Debug", () => _hud.HealthBarDebugMessage);
        _registerInfo("Damage Cooldown", () => $"{_battle.DamageCooldownRemaining:0.00}s");
        _registerInfo("Invincible Mode", () => _battle.InvincibleMode ? "Enabled" : "Disabled");
    }
}
