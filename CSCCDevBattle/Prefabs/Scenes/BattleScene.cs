using System;
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
            "Entities",
            () => BattleContext.Entities.Count.ToString());

        Debug.RegisterInfo(
            "Current Wave",
            () => BattleContext.CurrentWave?.GetType().Name
                ?? "None");

        Debug.RegisterInfo(
            "Collision Actors",
            () => BattleContext.Entities.Count(
                entity =>
                    entity.Enabled &&
                    entity.CollisionMode != CollisionMode.None
            ).ToString());
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
    }


    // ============================================================
    // Main battle update pipeline
    // ============================================================

    private void UpdateBattle(float deltaTime)
    {
        // --------------------------------------------------------
        // 1. Update the active wave
        // --------------------------------------------------------

        UpdateCurrentWave(deltaTime);


        // --------------------------------------------------------
        // 2. Update entity behaviours
        // --------------------------------------------------------

        UpdateBehaviours(deltaTime);


        // --------------------------------------------------------
        // 3. Remove entities destroyed by behaviours
        // --------------------------------------------------------

        RemoveDestroyedEntities();


        // --------------------------------------------------------
        // 4. Resolve entity attachment transforms
        // --------------------------------------------------------

        TransformSystem.ResolveAll(
            BattleContext.Entities);


        // --------------------------------------------------------
        // 5. Register newly spawned collision entities
        // --------------------------------------------------------

        RegisterNewCollisionEntities();


        // --------------------------------------------------------
        // 6. Synchronize collision shapes and broadphase
        // --------------------------------------------------------

        SynchronizeCollisionWorld();


        // --------------------------------------------------------
        // 7. Resolve solid collisions
        // --------------------------------------------------------

        ResolvePlayerSolidCollisions();


        // --------------------------------------------------------
        // 8. Process damaging obstacle collisions
        // --------------------------------------------------------

        CheckPlayerObstacleCollisions();


        // --------------------------------------------------------
        // 9. Remove entities destroyed during collision handling
        // --------------------------------------------------------

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

    private void StartWave(BattleWave wave)
    {
        ArgumentNullException.ThrowIfNull(wave);

        // End the existing wave before replacing it.
        BattleWave? currentWave =
            BattleContext.CurrentWave;

        if (currentWave != null)
        {
            currentWave.End(BattleContext);

            BattleContext.CurrentWave = null;

            // Remove the old wave's entities and collision actors.
            RemoveDestroyedEntities();
        }

        // Start the new wave.
        BattleContext.CurrentWave = wave;

        wave.Start(BattleContext);

        // Register entities spawned in OnStart immediately.
        TransformSystem.ResolveAll(
            BattleContext.Entities);

        RegisterNewCollisionEntities();

        SynchronizeCollisionWorld();
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

        // Example:
        //
        // if (other is ProjectileEntity projectile)
        // {
        //     BattleContext.Player.TakeDamage(
        //         projectile.Damage);
        //
        //     projectile.DestroyRequested = true;
        // }
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


    // ============================================================
    // Drawing
    // ============================================================

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

        game.SpriteBatch.DrawString(
            game.FontText,
            "Hello\nHi you",
            new Vector2(5f, 5f),
            Color.White);

        game.SpriteBatch.End();


        // --------------------------------------------------------
        // Collision debug overlay
        // --------------------------------------------------------

        if (_debugShowEntityMasks)
            DrawEntityMasks(game);


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

    private static void DrawEntity(
        Game1 game,
        Entity entity)
    {
        game.SpriteBatch.Draw(
            entity.Texture!,
            entity.WorldPosition,
            null,
            entity.Tint,
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