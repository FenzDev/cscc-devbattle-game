using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class BattleScene : Scene
{
    private const float FadeSpeed = 5f;

    public BattleContext BattleContext { get; }
    public override bool PausesTimers => _isPaused;
    public bool IsPaused => _isPaused;

    private readonly BattleDamageSystem _damageSystem;
    private readonly BattleCollisionController _collisions;
    private readonly BattleEntitySystem _entities;
    private readonly BattleArenaPresentation _arenaPresentation;
    private readonly BattleEncounterController _encounter;
    private readonly BattleHud _hud;
    private readonly BattleWorldRenderer _renderer;
    private readonly BattleSceneDebug _battleDebug;

    private float _fade = 1f;
    private bool _isPaused;
    private bool _terminalSceneRequested;

    public BattleScene()
    {
        BattleContext = new BattleContext(Timers)
        {
            PlayerName = NormalizePlayerName(Game1.Singleton.PlayerName)
        };
        BattleContext.Player.Scale = new Vector2(2f);

        _damageSystem = new BattleDamageSystem(BattleContext);
        _collisions = new BattleCollisionController(BattleContext, _damageSystem);
        _entities = new BattleEntitySystem(BattleContext);
        _arenaPresentation = new BattleArenaPresentation(BattleContext);
        _encounter = new BattleEncounterController(BattleContext, _arenaPresentation);
        _hud = new BattleHud(BattleContext);
        _renderer = new BattleWorldRenderer(BattleContext);

        TransformSystem.ResolveAll(BattleContext.Entities);
        _collisions.RegisterInitialEntities();
        _collisions.Synchronize();

        _battleDebug = new BattleSceneDebug(
            registerCommand: (key, name, action, state, trigger) => Debug.RegisterCmd(key, name, action, state, trigger),
            registerInfo: (name, value) => Debug.RegisterInfo(name, value),
            battle: BattleContext,
            damage: _damageSystem,
            collisions: _collisions,
            encounter: _encounter,
            startDemoEncounter: () => BeginEncounter(DemoEncounterFactory.CreateDepartmentEncounter()),
            startTestWave: () => StartWave(new TestWave()));

        _battleDebug.Register();
    }

    private static string NormalizePlayerName(string? name) =>
        string.IsNullOrWhiteSpace(name) ? "PLAYER" : name.Trim().ToUpperInvariant();

    public void BeginEncounter(EncounterDefinition definition) => _encounter.BeginEncounter(definition);
    public void StartWave(BattleWave wave) => _encounter.StartWave(wave);
    public bool TryDamagePlayer(float amount) => _damageSystem.TryDamagePlayer(amount);
    public float HealPlayer(float amount) => _damageSystem.HealPlayer(amount);

    public override TransitionState SetupTick(GameTime gameTime)
    {
        _fade = MathF.Max(0f, _fade - FadeSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds);
        return _fade > 0f ? TransitionState.InProgress : TransitionState.Finished;
    }

    public override TransitionState CleanupTick(GameTime gameTime)
    {
        _fade = MathF.Min(1f, _fade + FadeSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds);
        return _fade < 1f ? TransitionState.InProgress : TransitionState.Finished;
    }

    public override void Tick(GameTime gameTime, Game1 game)
    {
        if (Input.Pressed(Keys.Escape))
            _isPaused = !_isPaused;

        if (_isPaused)
            return;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _damageSystem.UpdateCooldown(deltaTime);
        UpdateBattle(deltaTime);

        if (BattleContext.IsPlayerDead)
        {
            if (!_terminalSceneRequested)
            {
                _terminalSceneRequested = true;
                game.GoToScene(new GameOverScene(
                    BattleContext.Player.Texture,
                    BattleContext.Player.Position,
                    BattleContext.Player.Rotation,
                    BattleContext.Arena.Center,
                    BattleContext.Arena.Size,
                    BattleContext.PlayerName));
            }
            return;
        }

        _hud.Update(deltaTime);

        if (!_terminalSceneRequested &&
            _encounter.State == BattleFlowState.Complete &&
            string.Equals(_encounter.Definition?.Name, "The Department Gauntlet", StringComparison.Ordinal))
        {
            _terminalSceneRequested = true;
            game.GoToScene(new VictoryScene(BattleContext.PlayerName));
        }
    }

    private void UpdateBattle(float deltaTime)
    {
        _encounter.Update(deltaTime);
        BattleContext.PlayerMovementEnabled = _encounter.IsWaveActive;
        _entities.UpdateBehaviours(deltaTime);
        _entities.ResolveTransforms();
        _collisions.RefreshRegistrations();
        _collisions.Synchronize();

        if (_encounter.IsWaveActive)
        {
            _collisions.ResolvePlayerSolidCollisions();
            _collisions.CheckPlayerObstacleCollisions();
        }

        _collisions.RemoveDestroyedEntities();
    }

    public override void Draw(GameTime gameTime, Game1 game)
    {
        game.GraphicsDevice.Clear(_battleDebug.MagentaScreenEnabled ? Color.Magenta : Color.Black);
        game.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _renderer.DrawArenaAndEntities(game, _arenaPresentation.PlayerVisualAlpha);
        game.SpriteBatch.End();

        if (_battleDebug.EntityMasksVisible)
            _renderer.DrawEntityMasks(game);

        game.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _hud.DrawDialoguePanel(game);
        _hud.DrawPlayerHud(game);
        game.SpriteBatch.End();
        _renderer.DrawFade(game, _fade);

        if (_isPaused)
            DrawPauseOverlay(game);
    }

    private static void DrawPauseOverlay(Game1 game)
    {
        SpriteBatch batch = game.SpriteBatch;
        batch.Begin(samplerState: SamplerState.PointClamp);
        batch.Draw(game.Pixel,
            new Rectangle(0, 0, Game1.BASE_SCREEN_WIDTH, Game1.BASE_SCREEN_HEIGHT),
            Color.Black * 0.64f);

        const string paused = "PAUSED";
        const string hint = "PRESS ESCAPE TO RESUME";
        Vector2 pausedSize = game.FontTitle.MeasureString(paused);
        Vector2 hintSize = game.FontText.MeasureString(hint);
        game.FontTitle.DrawText(batch, paused,
            new Vector2((Game1.BASE_SCREEN_WIDTH - pausedSize.X) * 0.5f, 245f), Color.Yellow);
        game.FontText.DrawText(batch, hint,
            new Vector2((Game1.BASE_SCREEN_WIDTH - hintSize.X) * 0.5f, 310f), Color.Yellow);
        batch.End();
    }
}
