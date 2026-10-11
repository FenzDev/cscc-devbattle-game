using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public enum BattleFlowState
{
    Idle,
    Dialogue,
    ArenaEntering,
    WaveActive,
    ArenaExiting,
    Complete
}

/// <summary>
/// Builds and advances the encounter step queue. Arena animation itself is
/// delegated to BattleArenaPresentation.
/// </summary>
public sealed class BattleEncounterController
{
    private sealed record EncounterStep(
        Func<BattleWave> Factory,
        int PhaseIndex,
        string PhaseName,
        int WaveNumber,
        int PhaseWaveCount);

    private readonly BattleContext _battle;
    private readonly BattleArenaPresentation _presentation;
    private readonly Random _random = new();

    private Queue<EncounterStep> _steps = new();
    private EncounterStep? _activeStep;
    private BattleWave? _pendingWave;

    public EncounterDefinition? Definition { get; private set; }

    public BattleFlowState State { get; private set; } = BattleFlowState.Idle;

    public bool IsWaveActive => State == BattleFlowState.WaveActive;

    public string StateName => State.ToString();

    public string ProgressMessage
    {
        get
        {
            if (_activeStep is null)
                return "-";

            string progress = _activeStep.WaveNumber == 0
                ? "dialogue"
                : $"wave {_activeStep.WaveNumber}/{_activeStep.PhaseWaveCount}";

            return $"{_activeStep.PhaseName} | {progress}";
        }
    }

    public string CurrentWaveMessage
    {
        get
        {
            BattleWave? wave = _battle.CurrentWave;
            if (wave is null)
                return _pendingWave is null
                    ? "-"
                    : $"Pending: {_pendingWave.GetType().Name}";

            string duration = float.IsFinite(wave.Duration)
                ? $"{wave.Elapsed:0.0}/{wave.Duration:0.0}s"
                : $"{wave.Elapsed:0.0}s / manual";

            return $"{wave.GetType().Name} | {duration}";
        }
    }

    public BattleEncounterController(
        BattleContext battle,
        BattleArenaPresentation presentation)
    {
        _battle = battle ?? throw new ArgumentNullException(nameof(battle));
        _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
        EnsureBoss(new Vector2(Game1.BASE_SCREEN_WIDTH / 2f, 120f));
    }

    public void BeginEncounter(EncounterDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        // Validate and build the new queue before interrupting the current wave.
        Queue<EncounterStep> newSteps = BuildSteps(definition);

        EndCurrentWave();
        HideBoss();
        _pendingWave = null;

        Definition = definition;
        _steps = newSteps;
        _activeStep = null;
        State = BattleFlowState.Idle;

        _presentation.SetDialogueLayout();
        StartNextStep();
    }

    public void StartWave(BattleWave wave)
    {
        ArgumentNullException.ThrowIfNull(wave);

        EncounterDefinition definition = new()
        {
            Name = wave.GetType().Name
        };

        EncounterPhase phase = new()
        {
            Name = wave.GetType().Name,
            BattleWaveCount = 1,
            PlayBetweenWaveDialogAfterFinalWave = false
        };

        phase.BattleWavePool.Add(() => wave);
        definition.Phases.Add(phase);

        BeginEncounter(definition);
    }

    public void FinishCurrentWave()
    {
        _battle.CurrentWave?.Finish();
    }

    public void RestartEncounter()
    {
        if (Definition is not null)
            BeginEncounter(Definition);
    }

    public void Update(float deltaTime)
    {
        _battle.Boss?.Update(deltaTime);

        switch (State)
        {
            case BattleFlowState.Dialogue:
                _battle.CurrentWave?.Update(_battle, deltaTime);

                if (_battle.CurrentWave?.Finished == true)
                {
                    EndCurrentWave();
                    StartNextStep();
                }
                break;

            case BattleFlowState.ArenaEntering:
                if (!_presentation.UpdateTransition(deltaTime))
                    break;

                BattleWave? wave = _pendingWave;
                if (wave is null)
                {
                    State = BattleFlowState.Complete;
                    break;
                }

                StartActiveWave(wave);
                State = BattleFlowState.WaveActive;
                break;

            case BattleFlowState.WaveActive:
                _battle.CurrentWave?.Update(_battle, deltaTime);

                if (_battle.CurrentWave?.Finished == true)
                {
                    EndCurrentWave();
                    _presentation.BeginTransition(
                        _presentation.DialogueArenaCenter,
                        _presentation.DialogueArenaSize,
                        1f);
                    State = BattleFlowState.ArenaExiting;
                }
                break;

            case BattleFlowState.ArenaExiting:
                if (_presentation.UpdateTransition(deltaTime))
                    StartNextStep();
                break;

            case BattleFlowState.Idle:
            case BattleFlowState.Complete:
            default:
                break;
        }
    }

    private Queue<EncounterStep> BuildSteps(EncounterDefinition definition)
    {
        Queue<EncounterStep> result = new();

        for (int phaseIndex = 0;
             phaseIndex < definition.Phases.Count;
             phaseIndex++)
        {
            EncounterPhase phase = definition.Phases[phaseIndex];

            if (phase.BattleWaveCount < 0)
            {
                throw new InvalidOperationException(
                    $"Phase '{phase.Name}' has a negative wave count.");
            }

            foreach (Func<DialogWave> openingDialogFactory in phase.OpeningDialogs)
            {
                Func<DialogWave> factory = openingDialogFactory;
                result.Enqueue(new EncounterStep(
                    () => factory(), phaseIndex, phase.Name, 0, phase.BattleWaveCount));
            }

            if (phase.BattleWaveCount > 0 && phase.BattleWavePool.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Phase '{phase.Name}' has waves to play but its BattleWavePool is empty.");
            }

            List<int> attackOrder = BuildAttackOrder(phase);
            int previousDialogIndex = -1;

            for (int waveNumber = 1;
                 waveNumber <= phase.BattleWaveCount;
                 waveNumber++)
            {
                Func<BattleWave> attackFactory = phase.BattleWavePool[attackOrder[waveNumber - 1]];
                result.Enqueue(new EncounterStep(
                    attackFactory, phaseIndex, phase.Name, waveNumber, phase.BattleWaveCount));

                bool addDialogue =
                    phase.BetweenWaveDialogPool.Count > 0 &&
                    (waveNumber < phase.BattleWaveCount ||
                     phase.PlayBetweenWaveDialogAfterFinalWave);

                if (!addDialogue)
                    continue;

                int dialogIndex = PickDifferentIndex(
                    phase.BetweenWaveDialogPool.Count, previousDialogIndex);
                previousDialogIndex = dialogIndex;

                Func<DialogWave> dialogFactory = phase.BetweenWaveDialogPool[dialogIndex];
                result.Enqueue(new EncounterStep(
                    dialogFactory, phaseIndex, phase.Name, waveNumber, phase.BattleWaveCount));
            }
        }

        return result;
    }

    private List<int> BuildAttackOrder(EncounterPhase phase)
    {
        int poolCount = phase.BattleWavePool.Count;
        int waveCount = phase.BattleWaveCount;

        if (phase.ShuffleBattleWavesWithoutReplacement)
        {
            if (waveCount != poolCount)
            {
                throw new InvalidOperationException(
                    $"Phase '{phase.Name}' enables shuffle-without-replacement, so " +
                    $"BattleWaveCount ({waveCount}) must equal its pool size ({poolCount}).");
            }

            List<int> shuffled = new(poolCount);
            for (int i = 0; i < poolCount; i++)
                shuffled.Add(i);

            // Fisher-Yates: every factory appears exactly once, in a fresh order.
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            return shuffled;
        }

        List<int> order = new(waveCount);
        int previousIndex = -1;
        for (int i = 0; i < waveCount; i++)
        {
            int selected = PickDifferentIndex(poolCount, previousIndex);
            order.Add(selected);
            previousIndex = selected;
        }

        return order;
    }

    private int PickDifferentIndex(int count, int previousIndex)
    {
        if (count <= 0)
            throw new InvalidOperationException("Cannot select from an empty factory pool.");

        int selected = _random.Next(count);
        if (count > 1 && selected == previousIndex)
            selected = (selected + 1 + _random.Next(count - 1)) % count;

        return selected;
    }

    private void StartNextStep()
    {
        if (_steps.Count == 0)
        {
            _activeStep = null;
            _pendingWave = null;
            _battle.CurrentWave = null;
            HideBoss();
            State = BattleFlowState.Complete;
            return;
        }

        _activeStep = _steps.Dequeue();
        BattleWave wave = _activeStep.Factory()
            ?? throw new InvalidOperationException("An encounter wave factory returned null.");

        if (wave is DialogWave)
        {
            ShowBossForDialogue();
            StartActiveWave(wave);
            State = BattleFlowState.Dialogue;
            return;
        }

        HideBoss();
        _pendingWave = wave;
        _presentation.BeginTransition(
            _presentation.GetWaveArenaCenter(wave),
            wave.ArenaSize,
            BattleArenaPresentation.ActivePlayerOpacity);
        State = BattleFlowState.ArenaEntering;
    }

    private void StartActiveWave(BattleWave wave)
    {
        _pendingWave = null;
        _battle.CurrentWave = wave;
        wave.Start(_battle);
    }

    private void EndCurrentWave()
    {
        BattleWave? wave = _battle.CurrentWave;
        if (wave is null)
            return;

        HideBoss();
        wave.End(_battle);
        _battle.CurrentWave = null;
    }

    private void ShowBossForDialogue()
    {
        BossCharacter? boss = _battle.Boss;
        if (boss is null)
            return;

        float screenSize = Game1.BASE_SCREEN_WIDTH;
        boss.Position = new Vector2(screenSize / 2f, screenSize / 2f - 40f);
        boss.SetVisible(true);
    }

    private void HideBoss()
    {
        _battle.Boss?.SetVisible(false);
    }

    public BossCharacter EnsureBoss(Vector2 position)
    {
        if (_battle.Boss != null)
            return _battle.Boss;

        _battle.Boss = new BossCharacter();
        _battle.Boss.Spawn(_battle, position);
        return _battle.Boss;
    }
}
