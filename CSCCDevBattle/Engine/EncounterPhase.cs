using System;
using System.Collections.Generic;

public sealed class EncounterPhase
{
    public string Name { get; set; } = "Phase";

    // Number of attacks selected from BattleWavePool.
    public int BattleWaveCount { get; set; } = 3;

    // Whether to play an inter-wave dialogue after the
    // final attack in this phase, too.
    public bool PlayBetweenWaveDialogAfterFinalWave { get; set; } = true;

    // All these dialogues run sequentially at phase entry.
    public List<Func<DialogWave>> OpeningDialogs { get; } = [];

    // One attack factory is randomly selected for each wave.
    public List<Func<BattleWave>> BattleWavePool { get; } = [];

    // One dialogue is randomly selected after each attack.
    // Multiple entries are alternatives, not a sequence.
    public List<Func<DialogWave>> BetweenWaveDialogPool { get; } = [];
}