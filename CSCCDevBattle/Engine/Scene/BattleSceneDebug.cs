using System;
using Microsoft.Xna.Framework.Input;

/// <summary>Compact, non-duplicated battle debug controls and status.</summary>
public sealed class BattleSceneDebug
{
    private readonly Action<Keys, string, Action, Func<string>, KeyTrigger> _registerCommand;
    private readonly Action<string, Func<string>> _registerInfo;
    private readonly BattleContext _battle;
    private readonly BattleCollisionController _collisions;
    private readonly BattleEncounterController _encounter;
    private readonly Action _startDemoEncounter;
    private readonly Action _startTestWave;
    private string _lastCommandResult = "Ready";

    public bool MagentaScreenEnabled { get; private set; }
    public bool EntityMasksVisible { get; private set; }

    // Keep the old constructor shape so BattleScene integration does not need
    // unrelated edits; HUD and damage details are now folded into one status line.
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
        _collisions = collisions ?? throw new ArgumentNullException(nameof(collisions));
        _encounter = encounter ?? throw new ArgumentNullException(nameof(encounter));
        _startDemoEncounter = startDemoEncounter ?? throw new ArgumentNullException(nameof(startDemoEncounter));
        _startTestWave = startTestWave ?? throw new ArgumentNullException(nameof(startTestWave));
        ArgumentNullException.ThrowIfNull(damage);
        ArgumentNullException.ThrowIfNull(hud);
    }

    public void Register()
    {
        _registerCommand(Keys.F1, "Start Encounter", _startDemoEncounter,
            () => "", KeyTrigger.Released);

        _registerCommand(Keys.F2, "Restart Current Wave", () =>
        {
            _lastCommandResult = _encounter.RestartCurrentWave()
                ? "Restarting wave"
                : "No attack wave to restart";
        }, () => _lastCommandResult, KeyTrigger.Released);

        _registerCommand(Keys.F3, "Restart Encounter", () =>
        {
            if (_encounter.Definition is null)
            {
                _lastCommandResult = "No encounter to restart";
                return;
            }

            _encounter.RestartEncounter();
            _lastCommandResult = "Encounter restarted";
        }, () => "", KeyTrigger.Released);

        _registerCommand(Keys.F4, "Finish Current Wave", () =>
        {
            if (_battle.CurrentWave is null)
            {
                _lastCommandResult = "No active wave";
                return;
            }

            _encounter.FinishCurrentWave();
            _lastCommandResult = "Finish requested";
        }, () => "", KeyTrigger.Released);

        _registerCommand(Keys.F5, "Invincible", () =>
        {
            _battle.InvincibleMode = !_battle.InvincibleMode;
            if (_battle.InvincibleMode)
                _battle.PlayerHealth = _battle.PlayerMaxHealth;
        }, () => _battle.InvincibleMode ? "On" : "Off", KeyTrigger.Released);

        _registerCommand(Keys.F6, "Collision Masks", () =>
            EntityMasksVisible = !EntityMasksVisible,
            () => EntityMasksVisible ? "On" : "Off", KeyTrigger.Released);

        _registerCommand(Keys.F7, "Start Test Wave", _startTestWave,
            () => "", KeyTrigger.Released);

        _registerCommand(Keys.F8, "Magenta Screen", () =>
            MagentaScreenEnabled = !MagentaScreenEnabled,
            () => MagentaScreenEnabled ? "On" : "Off", KeyTrigger.Released);

        // One compact status line per concern; elapsed time, wave state and pattern
        // details share one line instead of appearing as separate near-duplicates.
        _registerInfo("FLOW", () => $"{_encounter.StateName} | {_encounter.ProgressMessage}");
        _registerInfo("WAVE", GetWaveStatus);
        _registerInfo("PLAYER", () =>
            $"{_battle.PlayerHealth:0.#}/{_battle.PlayerMaxHealth:0.#} HP | " +
            $"CD {_battle.DamageCooldownRemaining:0.0}s | " +
            $"Inv {(_battle.InvincibleMode ? "on" : "off")}");
        _registerInfo("WORLD", () =>
            $"{_battle.Entities.Count} entities / {_collisions.RegisteredEntityCount} actors | " +
            $"Arena {_battle.Arena.Size.X:0}x{_battle.Arena.Size.Y:0}");
    }

    private string GetWaveStatus()
    {
        if (_battle.CurrentWave is not { } wave)
            return _encounter.CurrentWaveMessage;

        string duration = float.IsFinite(wave.Duration)
            ? $"{wave.Elapsed:0.0}/{wave.Duration:0.0}s"
            : $"{wave.Elapsed:0.0}s/manual";

        string detail = wave is DepartmentWaveBase department
            ? department.DebugState
            : "";

        if (detail.Length > 38)
            detail = detail[..35] + "...";

        return string.IsNullOrEmpty(detail)
            ? $"{wave.GetType().Name} {duration}"
            : $"{wave.GetType().Name} {duration} | {detail}";
    }
}
