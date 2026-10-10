using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class BattleScene : Scene
{
    private const float FadeSpeed = 5f;

    public BattleContext BattleContext { get; }

    private readonly BattleDamageSystem _damageSystem;
    private readonly BattleCollisionController _collisions;
    private readonly BattleEntitySystem _entities;
    private readonly BattleArenaPresentation _arenaPresentation;
    private readonly BattleEncounterController _encounter;
    private readonly BattleHud _hud;
    private readonly BattleWorldRenderer _renderer;
    private readonly BattleSceneDebug _battleDebug;

    private float _fade = 1f;

    public BattleScene()
    {
        BattleContext = new BattleContext(Timers);
        BattleContext.Player.Scale = new Vector2(2f);

        _damageSystem = new BattleDamageSystem(BattleContext);
        _collisions = new BattleCollisionController(
            BattleContext,
            _damageSystem);
        _entities = new BattleEntitySystem(BattleContext);
        _arenaPresentation = new BattleArenaPresentation(BattleContext);
        _encounter = new BattleEncounterController(
            BattleContext,
            _arenaPresentation);
        _hud = new BattleHud(BattleContext);
        _renderer = new BattleWorldRenderer(BattleContext);

        TransformSystem.ResolveAll(BattleContext.Entities);
        _collisions.RegisterInitialEntities();
        _collisions.Synchronize();

        _battleDebug = new BattleSceneDebug(
            registerCommand: (key, name, action, state, trigger) =>
                Debug.RegisterCmd(key, name, action, state, trigger),
            registerInfo: (name, value) =>
                Debug.RegisterInfo(name, value),
            battle: BattleContext,
            damage: _damageSystem,
            collisions: _collisions,
            encounter: _encounter,
            hud: _hud,
            startDemoEncounter: () => BeginEncounter(
                DemoEncounterFactory.CreateDepartmentEncounter()),
            startTestWave: () => StartWave(new TestWave()));

        _battleDebug.Register();
    }

    public void BeginEncounter(EncounterDefinition definition)
    {
        _encounter.BeginEncounter(definition);
    }

    public void StartWave(BattleWave wave)
    {
        _encounter.StartWave(wave);
    }

    public bool TryDamagePlayer(float amount)
    {
        return _damageSystem.TryDamagePlayer(amount);
    }

    public float HealPlayer(float amount)
    {
        return _damageSystem.HealPlayer(amount);
    }

    public override TransitionState SetupTick(GameTime gameTime)
    {
        float deltaTime =
            (float)gameTime.ElapsedGameTime.TotalSeconds;

        _fade -= FadeSpeed * deltaTime;

        if (_fade > 0f)
            return TransitionState.InProgress;

        _fade = 0f;
        return TransitionState.Finished;
    }

    public override TransitionState CleanupTick(GameTime gameTime)
    {
        float deltaTime =
            (float)gameTime.ElapsedGameTime.TotalSeconds;

        _fade += FadeSpeed * deltaTime;

        if (_fade < 1f)
            return TransitionState.InProgress;

        _fade = 1f;
        return TransitionState.Finished;
    }

    public override void Tick(GameTime gameTime, Game1 game)
    {
        float deltaTime =
            (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (Input.Down(Keys.R))
        {
            game.GoToScene(new BattleScene());
            return;
        }

        // Update this before collision checks so a newly-triggered
        // damage cooldown isn't shortened on the frame it starts.
        _damageSystem.UpdateCooldown(deltaTime);

        UpdateBattle(deltaTime);
        _hud.Update(deltaTime);
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
        game.GraphicsDevice.Clear(
            _battleDebug.MagentaScreenEnabled
                ? Color.Magenta
                : Color.Black);

        game.SpriteBatch.Begin(
            samplerState: SamplerState.PointClamp);

        _renderer.DrawArenaAndEntities(
            game,
            _arenaPresentation.PlayerVisualAlpha);

        game.SpriteBatch.End();

        if (_battleDebug.EntityMasksVisible)
            _renderer.DrawEntityMasks(game);

        game.SpriteBatch.Begin();
        _hud.DrawDialoguePanel(game);
        _hud.DrawPlayerHud(game);
        game.SpriteBatch.End();

        _renderer.DrawFade(game, _fade);
    }
}
