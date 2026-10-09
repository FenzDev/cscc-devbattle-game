using System;
using System.Collections.Generic;
using System.Linq;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;

public class BattleScene : Scene
{
    // ============================================================
    // Configuration
    // ============================================================

    private const float SpatialHashCellSize = 64f;

    private const float FadeSpeed = 5f;
    private enum BattleFlowState
    {
        Idle,
        Dialogue,
        ArenaEntering,
        WaveActive,
        ArenaExiting,
        Complete
    }

    private sealed record EncounterStep(
        Func<BattleWave> Factory,
        int PhaseIndex,
        string PhaseName,
        int WaveNumber,
        int PhaseWaveCount);

    private Queue<EncounterStep> _encounterSteps = new();

    private EncounterDefinition? _encounterDefinition;
    private EncounterStep? _activeStep;

    private BattleWave? _pendingWave;

    private readonly Random _encounterRandom = new();

    private BattleFlowState _flowState = BattleFlowState.Idle;

    private const float ArenaTransitionDuration = 0.35f;
    private const float ActivePlayerOpacity = 0.18f;
    private const float HudHeight = 76f;

    private float _playerVisualAlpha = 1f;

    private float _transitionElapsed;

    private Vector2 _transitionStartArenaCenter;
    private Vector2 _transitionStartArenaSize;
    private Vector2 _transitionTargetArenaCenter;
    private Vector2 _transitionTargetArenaSize;

    private Vector2 _transitionStartPlayerPosition;
    private Vector2 _transitionTargetPlayerPosition;

    private float _transitionStartPlayerAlpha;
    private float _transitionTargetPlayerAlpha;

    // HP Animation
    private float _hpFillRatio = 1f;
    private float _hpTrailRatio = 1f;

    private float _lastObservedPlayerHealth = float.NaN;

    private float _hpTrailDelay;
    private float _hpFlashRemaining;
    private float _hpShakeRemaining;

    private float _playerHitFlashRemaining;

    private const float HpTrailDelay = 0.45f;
    private const float HpTrailSpeed = 2.5f;
    private const float HpFlashDuration = 0.22f;
    private const float HpShakeDuration = 0.18f;
    private const float PlayerHitFlashDuration = 0.20f;

    // ============================================================
    // Battle state
    // ============================================================

    public BattleContext BattleContext { get; }

    // ============================================================
    // Collision
    // ============================================================

    private readonly CollisionWorld2D _collisionWorld;

    private readonly BattleCollisionSystem _collisionSystem;

    // ============================================================
    // Debug
    // ============================================================

    private bool _debugIsMagentaScreen;

    private bool _debugShowEntityMasks;

    // ============================================================
    // Scene transition
    // ============================================================

    private float _fade = 1f;


    // ============================================================
    // Constructor
    // ============================================================

    public BattleScene()
    {
        // --------------------------------------------------------
        // 1. Create collision world and layers
        // --------------------------------------------------------

        Layer defaultLayer = CreateSpatialLayer();

        Layer playerLayer = CreateSpatialLayer();

        Layer obstaclesLayer = CreateSpatialLayer();

        Layer solidLayer = CreateSpatialLayer();

        _collisionWorld = new CollisionWorld2D(
            defaultLayer);

        _collisionWorld.AddLayer(
            "player",
            playerLayer);

        _collisionWorld.AddLayer(
            "obstacles",
            obstaclesLayer);

        _collisionWorld.AddLayer(
            "solid",
            solidLayer);

        _collisionWorld.EnableCollisionBetweenLayers(
            "player",
            "obstacles");

        _collisionWorld.EnableCollisionBetweenLayers(
            "player",
            "solid");


        // --------------------------------------------------------
        // 2. Initialize battle state
        // --------------------------------------------------------

        BattleContext = new BattleContext(Timers);

        // Initial player configuration.
        // Ideally, this default belongs in PlayerEntity itself.
        BattleContext.Player.Scale = new Vector2(2f);


        // --------------------------------------------------------
        // 3. Create collision system
        // --------------------------------------------------------

        _collisionSystem = new BattleCollisionSystem(
            _collisionWorld);


        // --------------------------------------------------------
        // 4. Resolve initial transforms before registering shapes
        // --------------------------------------------------------

        TransformSystem.ResolveAll(
            BattleContext.Entities);

        RegisterCollisionEntities();

        SynchronizeCollisionWorld();


        // --------------------------------------------------------
        // 5. Debug
        // --------------------------------------------------------

        SetupDebug();
    }


    // ============================================================
    // Collision world setup
    // ============================================================

    private static Layer CreateSpatialLayer()
    {
        return new Layer(
            new SpatialHash(
                new SizeF(
                    SpatialHashCellSize,
                    SpatialHashCellSize)));
    }


    // ============================================================
    // Debug registration
    // ============================================================

    private void SetupDebug()
    {
        Debug.RegisterCmd(
            Keys.F1,
            "Magenta Screen",
            () =>
            {
                _debugIsMagentaScreen =
                    !_debugIsMagentaScreen;
            },
            () => _debugIsMagentaScreen
                ? "Enabled"
                : "Disabled");

        Debug.RegisterCmd(
            Keys.F2,
            "Entity Masks",
            () =>
            {
                _debugShowEntityMasks =
                    !_debugShowEntityMasks;
            },
            () => _debugShowEntityMasks
                ? "Enabled"
                : "Disabled");

        Debug.RegisterCmd(
            Keys.F3,
            "Restart Test Wave",
            () =>
            {
                StartWave(new TestWave());
            },
            () => BattleContext.CurrentWave == null
                ? "No active wave"
                : BattleContext.CurrentWave.GetType().Name);

        Debug.RegisterInfo(
            "Entities/Collision Actors",
            () => $"{BattleContext.Entities.Count}/{BattleContext.Entities.Count(
                entity =>
                    entity.Enabled &&
                    entity.CollisionMode != CollisionMode.None)}");

        Debug.RegisterInfo(
            "Current Wave",
            () => BattleContext.CurrentWave?.GetType().Name
                ?? "None");

        Debug.RegisterCmd(
            Keys.F3,
            "Start Demo Encounter",
            () => BeginEncounter(CreateDemoEncounter()),
            () => _flowState.ToString(),
            KeyTrigger.Released);

        Debug.RegisterCmd(
            Keys.F4,
            "Finish Current Wave",
            () => BattleContext.CurrentWave?.Finish(),
            () => BattleContext.CurrentWave is { } wave
                ? $"Running: {wave.GetType().Name}"
                : "No active wave",
            KeyTrigger.Released);

        Debug.RegisterCmd(
            Keys.F5,
            "Restart Encounter",
            () =>
            {
                if (_encounterDefinition is not null)
                    BeginEncounter(_encounterDefinition);
            },
            () => _encounterDefinition is null
                ? "No encounter"
                : $"{_encounterDefinition.Name} | {_flowState}",
            KeyTrigger.Released);

        Debug.RegisterCmd(
            Keys.F6,
            "Invincible Mode",
            () =>
            {
                BattleContext.InvincibleMode =
                    !BattleContext.InvincibleMode;

                if (BattleContext.InvincibleMode)
                {
                    BattleContext.PlayerHealth = BattleContext.PlayerMaxHealth;
                }
            },
            () => BattleContext.InvincibleMode
                ? "Enabled"
                : "Disabled",
            KeyTrigger.Released);

        Debug.RegisterInfo(
    "HP Bar Debug",
    () =>
        $"HP={BattleContext.PlayerHealth:0.#}/" +
        $"{BattleContext.PlayerMaxHealth:0.#} | " +
        $"Fill={_hpFillRatio:0.00} | " +
        $"Trail={_hpTrailRatio:0.00} | " +
        $"Last={_lastObservedPlayerHealth:0.#}");
    }

    private EncounterDefinition CreateDemoEncounter()
    {
        EncounterDefinition encounter = new()
        {
            Name = "Department Encounter"
        };

        EncounterPhase webPhase = new()
        {
            Name = "Web",
            BattleWaveCount = 2,
            PlayBetweenWaveDialogAfterFinalWave = true
        };

        webPhase.OpeningDialogs.Add(
            () => new DialogWave(
                "SYSTEM",
                "A web request has been detected.",
                2.5f));

        webPhase.BattleWavePool.Add(
            () => new TestWave());

        webPhase.BattleWavePool.Add(
            () => new TestWave());

        webPhase.BetweenWaveDialogPool.Add(
            () => new DialogWave(
                "SYSTEM",
                "The request has been processed.",
                1.8f));

        encounter.Phases.Add(webPhase);

        EncounterPhase aiPhase = new()
        {
            Name = "AI",
            BattleWaveCount = 2,
            PlayBetweenWaveDialogAfterFinalWave = true
        };

        aiPhase.OpeningDialogs.Add(
            () => new DialogWave(
                "SYSTEM",
                "A new process is starting.",
                2f,
                shakeAmount: 1.5f));

        aiPhase.BattleWavePool.Add(
            () => new TestWave { Duration = 3f });

        aiPhase.BattleWavePool.Add(
            () => new TestWave { Duration = 5f });

        aiPhase.BetweenWaveDialogPool.Add(
            () => new DialogWave(
                "SYSTEM",
                "Processing continues...",
                1.5f));

        encounter.Phases.Add(aiPhase);

        return encounter;
    }

    // ============================================================
    // Scene transitions
    // ============================================================

    public override TransitionState SetupTick(
        GameTime gameTime)
    {
        float deltaTime =
            (float)gameTime.ElapsedGameTime.TotalSeconds;

        _fade -= FadeSpeed * deltaTime;

        if (_fade > 0f)
            return TransitionState.InProgress;

        _fade = 0f;

        return TransitionState.Finished;
    }

    public override TransitionState CleanupTick(
        GameTime gameTime)
    {
        float deltaTime =
            (float)gameTime.ElapsedGameTime.TotalSeconds;

        _fade += FadeSpeed * deltaTime;

        if (_fade < 1f)
            return TransitionState.InProgress;

        _fade = 1f;

        return TransitionState.Finished;
    }


    // ============================================================
    // Scene tick
    // ============================================================

    public override void Tick(
        GameTime gameTime,
        Game1 game)
    {
        float deltaTime =
            (float)gameTime.ElapsedGameTime.TotalSeconds;

        // --------------------------------------------------------
        // Input
        // --------------------------------------------------------

        if (Input.Down(Keys.R))
        {
            game.GoToScene(new BattleScene());
            return;
        }

        // --------------------------------------------------------
        // Battle simulation
        // --------------------------------------------------------

        UpdateBattle(deltaTime);

        UpdateHudAnimations(deltaTime);
    }


    // ============================================================
    // Main battle update pipeline
    // ============================================================

    public void BeginEncounter(EncounterDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        // Validate/build the new sequence before interrupting the current one.
        Queue<EncounterStep> steps = BuildEncounterSteps(definition);

        EndCurrentWave();

        _pendingWave = null;
        RemoveDestroyedEntities();

        _encounterDefinition = definition;
        _encounterSteps = steps;
        _activeStep = null;

        _flowState = BattleFlowState.Idle;
        _playerVisualAlpha = 1f;

        // Start in the lower, dialogue-friendly layout.
        SetArenaPresentationImmediately(
            GetDialogueArenaCenter(),
            GetDialogueArenaSize(),
            1f);

        StartNextEncounterStep();
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
    private void UpdateFlow(float deltaTime)
    {
        switch (_flowState)
        {
            case BattleFlowState.Dialogue:
                {
                    BattleContext.CurrentWave?.Update(
                        BattleContext,
                        deltaTime);

                    if (BattleContext.CurrentWave?.Finished == true)
                    {
                        EndCurrentWave();
                        StartNextEncounterStep();
                    }

                    break;
                }

            case BattleFlowState.ArenaEntering:
                {
                    if (!UpdateArenaTransition(deltaTime))
                        break;

                    BattleWave? wave = _pendingWave;

                    if (wave is null)
                    {
                        _flowState = BattleFlowState.Complete;
                        break;
                    }

                    StartActiveWave(wave);
                    _flowState = BattleFlowState.WaveActive;

                    break;
                }

            case BattleFlowState.WaveActive:
                {
                    BattleContext.CurrentWave?.Update(
                        BattleContext,
                        deltaTime);

                    if (BattleContext.CurrentWave?.Finished == true)
                    {
                        EndCurrentWave();

                        BeginArenaTransition(
                            GetDialogueArenaCenter(),
                            GetDialogueArenaSize(),
                            1f);

                        _flowState = BattleFlowState.ArenaExiting;
                    }

                    break;
                }

            case BattleFlowState.ArenaExiting:
                {
                    if (UpdateArenaTransition(deltaTime))
                        StartNextEncounterStep();

                    break;
                }

            case BattleFlowState.Idle:
            case BattleFlowState.Complete:
            default:
                break;
        }
    }

    private Vector2 GetDialogueArenaSize()
    {
        return new Vector2(Game1.BASE_SCREEN_WIDTH - 40f, 120f);
    }

    private Vector2 GetDialogueArenaCenter()
    {
        float screenSize = Game1.BASE_SCREEN_WIDTH;
        Vector2 dialogueSize = GetDialogueArenaSize();

        // Keep the entire arena above the HUD, with a small gap.
        float hudTop = screenSize - HudHeight;
        float centerY = hudTop - 12f - dialogueSize.Y / 2f;

        return new Vector2(screenSize / 2f, centerY);
    }

    private Vector2 GetWaveArenaCenter(BattleWave wave)
    {
        float screenSize = Game1.BASE_SCREEN_WIDTH;

        return wave.ArenaCenter ??
            new Vector2(screenSize / 2f, screenSize / 2f);
    }

    private void SetArenaPresentationImmediately(
        Vector2 center,
        Vector2 size,
        float playerAlpha)
    {
        BattleContext.Arena.Center = center;
        BattleContext.Arena.Size = size;

        // Keep the player centered when establishing a new encounter.
        BattleContext.Player.Position = center;

        _playerVisualAlpha = playerAlpha;
    }

    private void BeginArenaTransition(
        Vector2 targetCenter,
        Vector2 targetSize,
        float targetPlayerAlpha)
    {
        _transitionElapsed = 0f;

        _transitionStartArenaCenter = BattleContext.Arena.Center;
        _transitionStartArenaSize = BattleContext.Arena.Size;

        _transitionTargetArenaCenter = targetCenter;
        _transitionTargetArenaSize = targetSize;

        _transitionStartPlayerPosition = BattleContext.Player.Position;
        _transitionTargetPlayerPosition = targetCenter;

        _transitionStartPlayerAlpha = _playerVisualAlpha;
        _transitionTargetPlayerAlpha = targetPlayerAlpha;
    }

    private bool UpdateArenaTransition(float deltaTime)
    {
        _transitionElapsed += MathF.Max(0f, deltaTime);

        float t = MathHelper.Clamp(
            _transitionElapsed / ArenaTransitionDuration,
            0f,
            1f);

        // Smoothstep easing produces a gentler start and finish.
        float eased = t * t * (3f - 2f * t);

        BattleContext.Arena.Center = Vector2.Lerp(
            _transitionStartArenaCenter,
            _transitionTargetArenaCenter,
            eased);

        BattleContext.Arena.Size = Vector2.Lerp(
            _transitionStartArenaSize,
            _transitionTargetArenaSize,
            eased);

        BattleContext.Player.Position = Vector2.Lerp(
            _transitionStartPlayerPosition,
            _transitionTargetPlayerPosition,
            eased);

        _playerVisualAlpha = MathHelper.Lerp(
            _transitionStartPlayerAlpha,
            _transitionTargetPlayerAlpha,
            eased);

        return t >= 1f;
    }

    private Queue<EncounterStep> BuildEncounterSteps(
        EncounterDefinition definition)
    {
        Queue<EncounterStep> steps = new();

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

            // Phase-entry dialogues all play in their listed order.
            foreach (Func<DialogWave> dialogFactory in phase.OpeningDialogs)
            {
                Func<DialogWave> factory = dialogFactory;

                steps.Enqueue(new EncounterStep(
                    () => factory(),
                    phaseIndex,
                    phase.Name,
                    0,
                    phase.BattleWaveCount));
            }

            if (phase.BattleWaveCount > 0 &&
                phase.BattleWavePool.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Phase '{phase.Name}' has waves to play " +
                    "but its BattleWavePool is empty.");
            }

            int previousAttackIndex = -1;
            int previousDialogIndex = -1;

            for (int waveNumber = 1;
                 waveNumber <= phase.BattleWaveCount;
                 waveNumber++)
            {
                // Pick a random attack. Avoid consecutive repeats when
                // there is more than one choice in the pool.
                int attackIndex = PickDifferentIndex(
                    phase.BattleWavePool.Count,
                    previousAttackIndex);

                previousAttackIndex = attackIndex;

                Func<BattleWave> attackFactory =
                    phase.BattleWavePool[attackIndex];

                steps.Enqueue(new EncounterStep(
                    attackFactory,
                    phaseIndex,
                    phase.Name,
                    waveNumber,
                    phase.BattleWaveCount));

                bool shouldAddDialogue =
                    phase.BetweenWaveDialogPool.Count > 0 &&
                    (waveNumber < phase.BattleWaveCount ||
                     phase.PlayBetweenWaveDialogAfterFinalWave);

                if (!shouldAddDialogue)
                    continue;

                int dialogIndex = PickDifferentIndex(
                    phase.BetweenWaveDialogPool.Count,
                    previousDialogIndex);

                previousDialogIndex = dialogIndex;

                Func<DialogWave> interWaveDialogFactory =
                    phase.BetweenWaveDialogPool[dialogIndex];

                steps.Enqueue(new EncounterStep(
                    () => interWaveDialogFactory(),
                    phaseIndex,
                    phase.Name,
                    waveNumber,
                    phase.BattleWaveCount));
            }
        }

        return steps;
    }

    private int PickDifferentIndex(int count, int previousIndex)
    {
        if (count <= 0)
        {
            throw new InvalidOperationException(
                "Cannot select from an empty factory pool.");
        }

        int selected = _encounterRandom.Next(count);

        if (count > 1 && selected == previousIndex)
        {
            selected = (selected + 1 + _encounterRandom.Next(count - 1))
                % count;
        }

        return selected;
    }

    private void StartNextEncounterStep()
    {
        if (_encounterSteps.Count == 0)
        {
            _activeStep = null;
            _pendingWave = null;
            BattleContext.CurrentWave = null;
            _flowState = BattleFlowState.Complete;
            return;
        }

        _activeStep = _encounterSteps.Dequeue();

        BattleWave wave = _activeStep.Factory()
            ?? throw new InvalidOperationException(
                "An encounter wave factory returned null.");

        if (wave is DialogWave)
        {
            StartActiveWave(wave);
            _flowState = BattleFlowState.Dialogue;
            return;
        }

        _pendingWave = wave;

        BeginArenaTransition(
            GetWaveArenaCenter(wave),
            wave.ArenaSize,
            ActivePlayerOpacity);

        _flowState = BattleFlowState.ArenaEntering;
    }

    private void StartActiveWave(BattleWave wave)
    {
        _pendingWave = null;

        BattleContext.CurrentWave = wave;
        wave.Start(BattleContext);
    }

    private void EndCurrentWave()
    {
        BattleWave? wave = BattleContext.CurrentWave;

        if (wave is null)
            return;

        wave.End(BattleContext);
        BattleContext.CurrentWave = null;

        // This removes entities that the wave marked for destruction
        // and unregisters them from the collision world.
        RemoveDestroyedEntities();
    }

    private void UpdateBattle(float deltaTime)
    {
        UpdateFlow(deltaTime);

        // Movement input is enabled only during an active attack.
        BattleContext.PlayerMovementEnabled =
            _flowState == BattleFlowState.WaveActive;

        UpdateBehaviours(deltaTime);

        RemoveDestroyedEntities();

        TransformSystem.ResolveAll(BattleContext.Entities);

        RegisterNewCollisionEntities();
        SynchronizeCollisionWorld();

        if (_flowState == BattleFlowState.WaveActive)
        {
            ResolvePlayerSolidCollisions();
            CheckPlayerObstacleCollisions();
            UpdateDamageCooldown(deltaTime);
        }

        RemoveDestroyedEntities();
    }


    // ============================================================
    // Wave lifecycle
    // ============================================================

    private void UpdateCurrentWave(float deltaTime)
    {
        BattleWave? wave =
            BattleContext.CurrentWave;

        if (wave == null)
            return;

        wave.Update(
            BattleContext,
            deltaTime);

        if (!wave.Finished)
            return;

        wave.End(BattleContext);

        BattleContext.CurrentWave = null;

        // End() marks wave-owned entities for destruction.
        RemoveDestroyedEntities();
    }



    // ============================================================
    // Behaviour update
    // ============================================================

    private void UpdateBehaviours(float deltaTime)
    {
        // Snapshot the collection because behaviours may spawn
        // new entities or request entity destruction.
        foreach (Entity entity in BattleContext.Entities.ToArray())
        {
            if (!entity.Enabled || entity.DestroyRequested)
                continue;

            Behaviour[] behaviours =
                entity.Behaviours.ToArray();

            foreach (Behaviour behaviour in behaviours)
            {
                behaviour.Update(
                    entity,
                    BattleContext,
                    deltaTime);
            }
        }
    }


    // ============================================================
    // Collision registration
    // ============================================================

    private void RegisterCollisionEntities()
    {
        foreach (Entity entity in BattleContext.Entities)
        {
            RegisterEntityCollision(entity);
        }
    }

    private void RegisterNewCollisionEntities()
    {
        foreach (Entity entity in BattleContext.Entities)
        {
            // Disabled or collisionless entities should not remain
            // registered in the collision world.
            if (!entity.Enabled ||
                entity.DestroyRequested ||
                entity.CollisionMode == CollisionMode.None)
            {
                if (_collisionWorld.Contains(entity))
                    _collisionSystem.Unregister(entity);

                continue;
            }

            // The actor is already registered.
            if (_collisionWorld.Contains(entity))
                continue;

            RegisterEntityCollision(entity);
        }
    }

    private void RegisterEntityCollision(Entity entity)
    {
        if (!entity.Enabled ||
            entity.DestroyRequested ||
            entity.CollisionMode == CollisionMode.None)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(entity.CollisionLayer))
        {
            throw new InvalidOperationException(
                $"Entity {entity.Id} has collision enabled " +
                "but no CollisionLayer.");
        }

        _collisionSystem.Register(
            entity,
            entity.CollisionLayer);
    }


    // ============================================================
    // Collision synchronization
    // ============================================================

    private void SynchronizeCollisionWorld()
    {
        _collisionSystem.Synchronize();

        _collisionWorld.RebuildDynamicLayers();
    }


    // ============================================================
    // Solid collision resolution
    // ============================================================

    private void ResolvePlayerSolidCollisions()
    {
        const int maxIterations = 8;

        PlayerEntity player = BattleContext.Player;

        for (int iteration = 0;
             iteration < maxIterations;
             iteration++)
        {
            bool corrected = false;

            foreach (BattleCollisionHit hit in
                _collisionSystem.Query(player, "solid"))
            {
                if (!hit.GeometricResult.HasValue)
                    continue;

                Vector2 mtv =
                    hit.GeometricResult.Value
                        .MinimumTranslationVector;

                if (mtv.LengthSquared() < 0.000001f)
                    continue;

                // This assumes the player is not attached to another
                // entity, so its local Position is its world position.
                player.Position += mtv;

                // Resolve the correction before querying again.
                TransformSystem.ResolveAll(
                    BattleContext.Entities);

                SynchronizeCollisionWorld();

                corrected = true;

                // Resolve one overlap at a time, then query again.
                break;
            }

            if (!corrected)
                break;
        }
    }


    // ============================================================
    // Player vs obstacles
    // ============================================================

    private void CheckPlayerObstacleCollisions()
    {
        foreach (BattleCollisionHit hit in
            _collisionSystem.Query(
                BattleContext.Player,
                "obstacles"))
        {
            HandlePlayerCollision(hit);
        }
    }

    private void HandlePlayerCollision(
        BattleCollisionHit hit)
    {
        Entity other = hit.B;

        if (other.ContactDamage > 0f)
        {
            TryDamagePlayer(other.ContactDamage);
        }

        if (other.DestroyOnPlayerContact)
        {
            other.DestroyRequested = true;
        }
    }

    public bool TryDamagePlayer(float amount)
    {
        if (!float.IsFinite(amount) || amount <= 0f)
            return false;

        if (BattleContext.InvincibleMode ||
            BattleContext.IsPlayerDead ||
            BattleContext.DamageCooldownRemaining > 0f)
        {
            return false;
        }

        float maxHealth = MathF.Max(0f, BattleContext.PlayerMaxHealth);
        float previousHealth = Math.Clamp(
            BattleContext.PlayerHealth, 0f, maxHealth);

        if (previousHealth <= 0f)
            return false;

        BattleContext.PlayerHealth = Math.Clamp(
            previousHealth - amount,
            0f,
            maxHealth);

        BattleContext.LastDamageAmount = previousHealth - BattleContext.PlayerHealth;

        if (BattleContext.LastDamageAmount <= 0f)
            return false;

        BattleContext.DamageEventCount++;

        BattleContext.DamageCooldownRemaining = MathF.Max(
            0f, BattleContext.DamageCooldownDuration);

        return true;
    }

    public float HealPlayer(float amount)
    {
        if (!float.IsFinite(amount) || amount <= 0f)
            return 0f;

        float maxHealth = MathF.Max(0f, BattleContext.PlayerMaxHealth);
        float previousHealth = Math.Clamp(
            BattleContext.PlayerHealth, 0f, maxHealth);

        BattleContext.PlayerHealth = Math.Clamp(
            previousHealth + amount,
            0f,
            maxHealth);

        return BattleContext.PlayerHealth - previousHealth;
    }

    public void UpdateDamageCooldown(float deltaTime)
    {
        BattleContext.DamageCooldownRemaining = MathF.Max(
            0f,
            BattleContext.DamageCooldownRemaining - MathF.Max(0f, deltaTime));
    }

    // ============================================================
    // Entity removal
    // ============================================================

    private void RemoveDestroyedEntities()
    {
        foreach (Entity entity in BattleContext.Entities.ToArray())
        {
            if (!entity.DestroyRequested)
                continue;

            // Remove from the collision world first.
            if (_collisionWorld.Contains(entity))
                _collisionWorld.Remove(entity);

            // Then remove from the authoritative entity collection.
            BattleContext.Remove(entity);
        }
    }

    private void UpdateHudAnimations(float deltaTime)
    {
        deltaTime = MathF.Max(0f, deltaTime);

        float maxHealth = MathF.Max(
            0f, BattleContext.PlayerMaxHealth);

        float health = Math.Clamp(
            BattleContext.PlayerHealth,
            0f,
            maxHealth);

        float targetRatio = maxHealth > 0f
            ? health / maxHealth
            : 0f;

        // Initialize without playing a damage animation.
        if (float.IsNaN(_lastObservedPlayerHealth))
        {
            _lastObservedPlayerHealth = health;
            _hpFillRatio = targetRatio;
            _hpTrailRatio = targetRatio;
            return;
        }

        // Count down existing effects.
        _hpFlashRemaining = MathF.Max(
            0f, _hpFlashRemaining - deltaTime);

        _hpShakeRemaining = MathF.Max(
            0f, _hpShakeRemaining - deltaTime);

        _playerHitFlashRemaining = MathF.Max(
            0f, _playerHitFlashRemaining - deltaTime);

        bool tookDamage =
            health < _lastObservedPlayerHealth;

        bool wasHealed =
            health > _lastObservedPlayerHealth;

        if (tookDamage)
        {
            float oldRatio = maxHealth > 0f
                ? Math.Clamp(
                    _lastObservedPlayerHealth / maxHealth,
                    0f,
                    1f)
                : 0f;

            // Preserve the old health as the start of the trail.
            _hpTrailRatio = MathF.Max(
                _hpTrailRatio, oldRatio);

            _hpTrailDelay = HpTrailDelay;
            _hpFlashRemaining = HpFlashDuration;
            _hpShakeRemaining = HpShakeDuration;
            _playerHitFlashRemaining = PlayerHitFlashDuration;
        }
        else if (wasHealed)
        {
            // Healing should not look like pending damage.
            _hpTrailRatio = _hpFillRatio;
            _hpTrailDelay = 0f;
        }

        _lastObservedPlayerHealth = health;

        // Main health bar: relatively quick response.
        _hpFillRatio = MathHelper.Lerp(
            _hpFillRatio,
            targetRatio,
            Math.Clamp(deltaTime * 18f, 0f, 1f));

        // Delayed trail: wait, then slowly catch up.
        if (!tookDamage)
        {
            if (_hpTrailDelay > 0f)
            {
                _hpTrailDelay = MathF.Max(
                    0f, _hpTrailDelay - deltaTime);
            }
            else
            {
                _hpTrailRatio = MathHelper.Lerp(
                    _hpTrailRatio,
                    targetRatio,
                    Math.Clamp(
                        deltaTime * HpTrailSpeed,
                        0f,
                        1f));
            }
        }

        // The trail should never be shorter than the main fill.
        _hpTrailRatio = MathF.Max(
            _hpTrailRatio,
            _hpFillRatio);
    }


    // ============================================================
    // Drawing
    // ============================================================

    private Color GetEntityDrawTint(Entity entity)
    {
        Color tint = entity.Tint;

        bool isPlayerVisual =
            ReferenceEquals(entity, BattleContext.Player) ||
            entity.FadeWithPlayer;

        if (!isPlayerVisual)
            return tint;

        PlayerEntity player = BattleContext.Player;

        // Flash towards white when hit.
        tint = Color.Lerp(
            tint,
            Color.White,
            Math.Clamp(player.HitFlashAmount, 0f, 1f));

        // Preserve the existing arena transition fade,
        // and multiply it by the invulnerability flicker.
        float alpha =
            _playerVisualAlpha * player.DamageBlinkOpacity;

        tint.A = (byte)Math.Clamp(
            (int)MathF.Round(tint.A * alpha),
            0,
            255);

        return tint;
    }
    private void DrawPlayerHud(Game1 game)
    {
        float screenSize = Game1.BASE_SCREEN_WIDTH;

        var spriteBatch = game.SpriteBatch;
        var font = game.FontText;

        float hudTop = screenSize - HudHeight;

        // Separator above the HUD.
        spriteBatch.Draw(
            game.Pixel,
            new Rectangle(
                20,
                (int)hudTop,
                (int)screenSize - 40,
                2),
            Color.White);

        float textY = hudTop + 20f;

        // Player name, bottom left.
        font.DrawText(
            spriteBatch,
            BattleContext.PlayerName,
            new Vector2(24f, textY),
            Color.White);

        float maxHealth = MathF.Max(
            0f, BattleContext.PlayerMaxHealth);

        float health = Math.Clamp(
            BattleContext.PlayerHealth,
            0f,
            maxHealth);

        string hpText =
            $"HP {health:0.#}/{maxHealth:0.#}";

        Vector2 hpTextSize = font.MeasureString(hpText);

        float hpTextX =
            screenSize - hpTextSize.X - 24f;

        float barWidth = 112f;
        float barHeight = 24f;
        float gap = 12f;

        float barX = hpTextX - gap - barWidth;
        float barY = 2f + textY + (hpTextSize.Y - barHeight) / 2f;

        // Shake the bar horizontally and vertically on damage.
        float shakeStrength = _hpShakeRemaining > 0f
            ? _hpShakeRemaining / HpShakeDuration
            : 0f;

        Vector2 shake = new(
            (Random.Shared.NextSingle() * 2f - 1f)
                * 3f * shakeStrength,
            (Random.Shared.NextSingle() * 2f - 1f)
                * 2f * shakeStrength);

        Rectangle outer = new(
            (int)(barX + shake.X),
            (int)(barY + shake.Y),
            (int)barWidth,
            (int)barHeight);

        float flash = Math.Clamp(
            _hpFlashRemaining / HpFlashDuration,
            0f,
            1f);

        Color backgroundColor = Color.Lerp(
            new Color(45, 25, 25),
            new Color(130, 35, 35),
            flash * 0.5f);

        spriteBatch.Draw(
            game.Pixel,
            outer,
            backgroundColor);

        Rectangle inner = new(
            outer.X + 2,
            outer.Y + 2,
            outer.Width - 4,
            outer.Height - 4);

        // Empty bar background.
        spriteBatch.Draw(
            game.Pixel,
            inner,
            new Color(25, 25, 25));

        int fillWidth = Math.Clamp(
            (int)MathF.Round(inner.Width * _hpFillRatio),
            0,
            inner.Width);

        int trailEnd = Math.Clamp(
            (int)MathF.Round(inner.Width * _hpTrailRatio),
            0,
            inner.Width);

        // Main HP color depends on current health.
        Color fillColor = _hpFillRatio > 0.5f
            ? new Color(50, 60, 220)
            : new Color(255, 65, 65);

        // Flash towards white immediately after damage.
        fillColor = Color.Lerp(
            fillColor,
            Color.White,
            flash * 0.8f);

        if (fillWidth > 0)
        {
            spriteBatch.Draw(
                game.Pixel,
                new Rectangle(
                    inner.X,
                    inner.Y,
                    fillWidth,
                    inner.Height),
                fillColor);
        }

        // The light blue segment shows the health just lost.
        int trailWidth = Math.Max(
            0, trailEnd - fillWidth);

        if (trailWidth > 0)
        {
            Color trailColor = Color.Lerp(
                new Color(180, 200, 230),
                Color.White,
                flash * 0.65f);

            spriteBatch.Draw(
                game.Pixel,
                new Rectangle(
                    inner.X + fillWidth,
                    inner.Y,
                    trailWidth,
                    inner.Height),
                trailColor);
        }

        DrawUiOutline(
            spriteBatch,
            game.Pixel,
            outer,
            2,
            Color.Lerp(Color.White, Color.Red, flash));

        // HP number changes color briefly on a hit.
        Color hpColor = Color.Lerp(
            Color.White,
            new Color(255, 100, 100),
            flash);

        font.DrawText(
            spriteBatch,
            hpText,
            new Vector2(hpTextX, textY),
            hpColor);
    }

    private void DrawDialoguePanel(Game1 game)
    {
        if (_flowState != BattleFlowState.Dialogue)
            return;

        if (BattleContext.CurrentWave is not DialogWave dialog)
            return;

        var spriteBatch = game.SpriteBatch;

        int screenSize = Game1.BASE_SCREEN_WIDTH;

        Rectangle box = new(
            20,
            20,
            screenSize - 40,
            86);

        spriteBatch.Draw(game.Pixel, box, Color.Black);

        DrawUiOutline(
            spriteBatch,
            game.Pixel,
            box,
            2,
            Color.White);

        game.FontText.DrawText(
            spriteBatch,
            dialog.Speaker,
            new Vector2(box.X + 14f, box.Y + 8f),
            Color.Yellow);

        DrawDialogText(
            game,
            dialog,
            new Vector2(box.X + 14f, box.Y + 38f));
    }

    private void DrawDialogText(
        Game1 game,
        DialogWave dialog,
        Vector2 position)
    {
        var spriteBatch = game.SpriteBatch;
        var font = game.FontText;

        string text = dialog.Text;

        int visibleCount = Math.Clamp(
            dialog.VisibleCharacterCount,
            0,
            text.Length);

        float lineHeight = font.MeasureString("Ag").Y + 2f;

        int lineStart = 0;
        int lineNumber = 0;

        for (int i = 0; i < visibleCount; i++)
        {
            char character = text[i];

            if (character == '\n')
            {
                lineStart = i + 1;
                lineNumber++;
                continue;
            }

            // Measure the prefix to account for font spacing
            // and kerning between preceding characters.
            string prefix = text.Substring(
                lineStart,
                i - lineStart + 1);

            float characterWidth =
                font.MeasureString(character.ToString()).X;

            float x =
                font.MeasureString(prefix).X - characterWidth;

            float y = lineNumber * lineHeight;

            Vector2 shake = Vector2.Zero;

            if (dialog.ShakeAmount > 0f &&
                !char.IsWhiteSpace(character))
            {
                shake = new Vector2(
                    (Random.Shared.NextSingle() * 2f - 1f)
                        * dialog.ShakeAmount,

                    (Random.Shared.NextSingle() * 2f - 1f)
                        * dialog.ShakeAmount);
            }

            Vector2 characterPosition =
                position + new Vector2(x, y) + shake;

            font.DrawText(
                spriteBatch,
                character.ToString(),
                characterPosition,
                Color.White);
        }
    }
    private static void DrawUiOutline(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        Rectangle rectangle,
        int thickness,
        Color color)
    {
        spriteBatch.Draw(
            pixel,
            new Rectangle(
                rectangle.X,
                rectangle.Y,
                rectangle.Width,
                thickness),
            color);

        spriteBatch.Draw(
            pixel,
            new Rectangle(
                rectangle.X,
                rectangle.Bottom - thickness,
                rectangle.Width,
                thickness),
            color);

        spriteBatch.Draw(
            pixel,
            new Rectangle(
                rectangle.X,
                rectangle.Y,
                thickness,
                rectangle.Height),
            color);

        spriteBatch.Draw(
            pixel,
            new Rectangle(
                rectangle.Right - thickness,
                rectangle.Y,
                thickness,
                rectangle.Height),
            color);
    }

    public override void Draw(
        GameTime gameTime,
        Game1 game)
    {
        game.GraphicsDevice.Clear(
            _debugIsMagentaScreen
                ? Color.Magenta
                : Color.Black);

        // --------------------------------------------------------
        // World
        // --------------------------------------------------------

        game.SpriteBatch.Begin(
            samplerState: SamplerState.PointClamp);

        DrawArena(game);

        DrawEntities(game);

        // game.SpriteBatch.DrawString(
        //     game.FontText,
        //     "Hello\nHi you",
        //     new Vector2(5f, 5f),
        //     Color.White);

        game.SpriteBatch.End();


        // --------------------------------------------------------
        // Collision debug overlay
        // --------------------------------------------------------

        if (_debugShowEntityMasks)
            DrawEntityMasks(game);

        // --------------------------------------------------------
        // Draw HUD/UI
        // --------------------------------------------------------
        game.SpriteBatch.Begin();
        DrawDialoguePanel(game);
        DrawPlayerHud(game);
        game.SpriteBatch.End();

        // --------------------------------------------------------
        // Scene fade
        // --------------------------------------------------------

        DrawFade(game);
    }


    // ============================================================
    // Entity rendering
    // ============================================================

    private void DrawEntities(Game1 game)
    {
        // Back attachments appear behind their parents.
        foreach (Entity entity in BattleContext.Entities)
        {
            if (!IsDrawable(entity))
                continue;

            if (entity.Attachment?.VisualOrder !=
                AttachmentVisualOrder.Back)
            {
                continue;
            }

            DrawEntity(game, entity);
        }

        // Root entities.
        foreach (Entity entity in BattleContext.Entities)
        {
            if (!IsDrawable(entity))
                continue;

            if (entity.Attachment != null)
                continue;

            DrawEntity(game, entity);
        }

        // Front attachments appear over their parents.
        foreach (Entity entity in BattleContext.Entities)
        {
            if (!IsDrawable(entity))
                continue;

            if (entity.Attachment?.VisualOrder !=
                AttachmentVisualOrder.Front)
            {
                continue;
            }

            DrawEntity(game, entity);
        }
    }

    private static bool IsDrawable(Entity entity)
    {
        return entity.Enabled &&
               entity.Visible &&
               !entity.DestroyRequested &&
               entity.Texture != null;
    }

    private void DrawEntity(
        Game1 game,
        Entity entity)
    {
        game.SpriteBatch.Draw(
            entity.Texture!,
            entity.WorldPosition,
            null,
            GetEntityDrawTint(entity),
            entity.WorldRotation,
            entity.Origin,
            entity.WorldScale,
            SpriteEffects.None,
            0f);
    }


    // ============================================================
    // Arena rendering
    // ============================================================

    private void DrawArena(Game1 game)
    {
        BattleArena arena = BattleContext.Arena;

        Vector2 min = arena.Minimum;

        Vector2 max = arena.Maximum;

        float thickness = arena.BorderThickness;

        int x = (int)min.X;
        int y = (int)min.Y;

        int width = (int)arena.Size.X;
        int height = (int)arena.Size.Y;

        int border = (int)thickness;

        // Top.
        game.SpriteBatch.Draw(
            game.Pixel,
            new Rectangle(
                x,
                y,
                width,
                border),
            Color.White);

        // Bottom.
        game.SpriteBatch.Draw(
            game.Pixel,
            new Rectangle(
                x,
                (int)(max.Y - thickness),
                width,
                border),
            Color.White);

        // Left.
        game.SpriteBatch.Draw(
            game.Pixel,
            new Rectangle(
                x,
                y,
                border,
                height),
            Color.White);

        // Right.
        game.SpriteBatch.Draw(
            game.Pixel,
            new Rectangle(
                (int)(max.X - thickness),
                y,
                border,
                height),
            Color.White);
    }


    // ============================================================
    // Collision debug rendering
    // ============================================================

    private void DrawEntityMasks(Game1 game)
    {
        game.SpriteBatch.Begin(
            samplerState: SamplerState.PointClamp);

        foreach (Entity entity in BattleContext.Entities)
        {
            if (!entity.Enabled || entity.DestroyRequested)
                continue;

            switch (entity.CollisionMode)
            {
                case CollisionMode.None:
                    break;

                case CollisionMode.RotatedBox:
                    DrawRotatedBoxMask(game, entity);
                    break;

                case CollisionMode.PixelPerfect:
                    DrawPixelMask(game, entity);
                    break;
            }
        }

        game.SpriteBatch.End();
    }

    private static void DrawRotatedBoxMask(
        Game1 game,
        Entity entity)
    {
        Vector2 center =
            TransformSystem.TransformOffset(
                entity,
                entity.CollisionOffset);

        Vector2 size =
            TransformSystem.Multiply(
                entity.CollisionSize,
                TransformSystem.Abs(
                    entity.WorldScale));

        float rotation =
            entity.WorldRotation +
            entity.CollisionRotation;

        game.SpriteBatch.Draw(
            game.Pixel,
            center,
            null,
            Color.Red * 0.35f,
            rotation,
            new Vector2(0.5f, 0.5f),
            size,
            SpriteEffects.None,
            0f);
    }

    private static void DrawPixelMask(
        Game1 game,
        Entity entity)
    {
        if (entity.Texture == null)
            return;

        game.SpriteBatch.Draw(
            entity.Texture,
            entity.WorldPosition,
            null,
            Color.Red * 0.45f,
            entity.WorldRotation,
            entity.Origin,
            entity.WorldScale,
            SpriteEffects.None,
            0f);
    }


    // ============================================================
    // Fade rendering
    // ============================================================

    private void DrawFade(Game1 game)
    {
        if (_fade <= 0f)
            return;

        game.SpriteBatch.Begin();

        game.SpriteBatch.Draw(
            game.Pixel,
            new Rectangle(
                0,
                0,
                game.GraphicsDevice.Viewport.Width,
                game.GraphicsDevice.Viewport.Height),
            Color.Black * _fade);

        game.SpriteBatch.End();
    }

}