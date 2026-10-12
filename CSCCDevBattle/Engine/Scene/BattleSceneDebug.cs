using System;
using Microsoft.Xna.Framework.Input;

/// <summary>Compact, non-redundant battle debug commands and status.</summary>
public sealed class BattleSceneDebug
{
    private readonly Action<Keys, string, Action, Func<string>, KeyTrigger> _registerCommand;
    private readonly Action<string, Func<string>> _registerInfo;
    private readonly BattleContext _battle;
    private readonly BattleDamageSystem _damage;
    private readonly BattleCollisionController _collisions;
    private readonly BattleEncounterController _encounter;
    private readonly Action _startDemoEncounter;
    private readonly Action _startTestWave;
    private string _lastAction = "Ready";

    public bool MagentaScreenEnabled { get; private set; }
    public bool EntityMasksVisible { get; private set; }

    public BattleSceneDebug(
        Action<Keys, string, Action, Func<string>, KeyTrigger> registerCommand,
        Action<string, Func<string>> registerInfo,
        BattleContext battle,
        BattleDamageSystem damage,
        BattleCollisionController collisions,
        BattleEncounterController encounter,
        Action startDemoEncounter,
        Action startTestWave)
    {
        _registerCommand = registerCommand ?? throw new ArgumentNullException(nameof(registerCommand));
        _registerInfo = registerInfo ?? throw new ArgumentNullException(nameof(registerInfo));
        _battle = battle ?? throw new ArgumentNullException(nameof(battle));
        _damage = damage ?? throw new ArgumentNullException(nameof(damage));
        _collisions = collisions ?? throw new ArgumentNullException(nameof(collisions));
        _encounter = encounter ?? throw new ArgumentNullException(nameof(encounter));
        _startDemoEncounter = startDemoEncounter ?? throw new ArgumentNullException(nameof(startDemoEncounter));
        _startTestWave = startTestWave ?? throw new ArgumentNullException(nameof(startTestWave));
    }

    public void Register()
    {
        _registerCommand(Keys.F1, "Start Encounter", () => { _startDemoEncounter(); _lastAction = "Encounter started"; },
            () => _encounter.StateName, KeyTrigger.Released);
        _registerCommand(Keys.F2, "Restart Wave", () =>
        {
            _lastAction = _encounter.RestartCurrentWave() ? "Wave restarted" : "No active attack wave";
        }, () => _lastAction, KeyTrigger.Released);
        _registerCommand(Keys.F3, "Restart Encounter", () => { _encounter.RestartEncounter(); _lastAction = "Encounter restarted"; },
            () => _encounter.StateName, KeyTrigger.Released);
        _registerCommand(Keys.F4, "Finish Wave", () => { _encounter.FinishCurrentWave(); _lastAction = "Finish requested"; },
            () => _battle.CurrentWave?.GetType().Name ?? "No active wave", KeyTrigger.Released);
        _registerCommand(Keys.F5, "Invincibility", () =>
        {
            _battle.InvincibleMode = !_battle.InvincibleMode;
            if (_battle.InvincibleMode) _battle.PlayerHealth = _battle.PlayerMaxHealth;
        }, () => _battle.InvincibleMode ? "ON" : "OFF", KeyTrigger.Released);
        _registerCommand(Keys.F6, "Collision Masks", () => EntityMasksVisible = !EntityMasksVisible,
            () => EntityMasksVisible ? "ON" : "OFF", KeyTrigger.Released);
        _registerCommand(Keys.F7, "Test Wave", () => { _startTestWave(); _lastAction = "Test wave started"; },
            () => _encounter.StateName, KeyTrigger.Released);
        _registerCommand(Keys.F8, "Magenta Screen", () => MagentaScreenEnabled = !MagentaScreenEnabled,
            () => MagentaScreenEnabled ? "ON" : "OFF", KeyTrigger.Released);

        _registerInfo("Flow / wave", () =>
            $"{_encounter.StateName} | {_encounter.ProgressMessage} | {_encounter.CurrentWaveMessage}");
        _registerInfo("HP / cooldown", () =>
            $"{_battle.PlayerHealth:0.#}/{_battle.PlayerMaxHealth:0.#} | {_battle.DamageCooldownRemaining:0.00}s | invincible {(_battle.InvincibleMode ? "ON" : "OFF")}");
        _registerInfo("Entities / collision actors", () =>
            $"{_battle.Entities.Count} / {_collisions.RegisteredEntityCount}");
        _registerInfo("Wave status", () =>
            _battle.CurrentWave is DepartmentWaveBase wave ? wave.DebugState : _lastAction);
    }
}
