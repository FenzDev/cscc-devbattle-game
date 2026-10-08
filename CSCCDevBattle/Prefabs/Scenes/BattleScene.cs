using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;
using System;
using System.Linq;

public class BattleScene : Scene
{
  // ============================================================
  // Battle state
  // ============================================================

  public BattleContext BattleContext { get; }

  // ============================================================
  // Battle systems
  // ============================================================

  private readonly CollisionWorld2D _collisionWorld;
  private readonly BattleCollisionSystem _collisionSystem;

  // ============================================================
  // Other
  // ============================================================

  private readonly TimersManager.Timer _actionTimer;

  public BattleScene()
  {
    // ========================================================
    // Collision World
    // ========================================================

    Layer defaultLayer =
        new Layer(
            new SpatialHash(
                new SizeF(64f, 64f)
            )
        );

    Layer playerLayer =
        new Layer(
            new SpatialHash(
                new SizeF(64f, 64f)
            )
        );

    Layer obstaclesLayer =
        new Layer(
            new SpatialHash(
                new SizeF(64f, 64f)
            )
        );

    _collisionWorld =
        new CollisionWorld2D(
            defaultLayer
        );

    _collisionWorld.AddLayer(
        "player",
        playerLayer
    );

    _collisionWorld.AddLayer(
        "obstacles",
        obstaclesLayer
    );

    // player <-> obstacles is NOT automatically enabled
    // because both are non-default layers.
    _collisionWorld.EnableCollisionBetweenLayers(
        "player",
        "obstacles"
    );


    // ========================================================
    // Battle State
    // ========================================================

    BattleContext =
        new BattleContext(Timers);


    // ========================================================
    // Collision System
    // ========================================================

    _collisionSystem =
        new BattleCollisionSystem(
            _collisionWorld
        );


    // ========================================================
    // Register initial collision entities
    // ========================================================

    RegisterCollisionEntities();


    // ========================================================
    // Other
    // ========================================================

    _actionTimer =
        Timers.StartAfter(5.0f);

    SetupDebug();
  }


  // ============================================================
  // Collision registration
  // ============================================================

  private void RegisterCollisionEntities()
  {
    foreach (Entity entity
        in BattleContext.Entities)
    {
      RegisterEntityCollision(entity);
    }
  }

  private void RegisterEntityCollision(
      Entity entity)
  {
    if (!entity.Enabled)
      return;

    if (entity.CollisionMode ==
        CollisionMode.None)
    {
      return;
    }

    if (string.IsNullOrWhiteSpace(
            entity.CollisionLayer))
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
  // Debug
  // ============================================================

  private bool _debugIsMagentaScreen;
  private bool _debugShowEntityMasks;

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
            : "Disabled"
    );

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
            : "Disabled"
    );

    Debug.RegisterCmd(
        Keys.F3,
        "Start Wave (Hold Ctrl to switch)",
        () =>
        {
          StartWave(new TestWave());
        },
        () => "Test"
    );

    Debug.RegisterInfo(
        "Number of Entities",
        () =>
            BattleContext.Entities.Count.ToString()
    );
  }


  // ============================================================
  // Transition
  // ============================================================

  private float _fade = 1.0f;

  private const float FADE_SPEED = 5.0f;

  public override TransitionState CleanupTick(
      GameTime gameTime)
  {
    float deltaTime =
        (float)gameTime.ElapsedGameTime.TotalSeconds;

    _fade += FADE_SPEED * deltaTime;

    if (_fade < 1.0f)
      return TransitionState.InProgress;

    _fade = 1.0f;

    return TransitionState.Finished;
  }

  public override TransitionState SetupTick(
      GameTime gameTime)
  {
    float deltaTime =
        (float)gameTime.ElapsedGameTime.TotalSeconds;

    _fade -= FADE_SPEED * deltaTime;

    if (_fade > 0.0f)
      return TransitionState.InProgress;

    _fade = 0.0f;

    return TransitionState.Finished;
  }


  // ============================================================
  // Battle update
  // ============================================================
  private void UpdateBattle(float deltaTime)
  {
    // ============================================================
    // 1. Wave script
    // ============================================================

    BattleWave? wave =
        BattleContext.CurrentWave;

    if (wave != null)
    {
      wave.Update(
          BattleContext,
          deltaTime);

      if (wave.Finished)
      {
        wave.End(
            BattleContext);

        BattleContext.CurrentWave = null;

        // IMPORTANT:
        // Remove wave entities immediately.
        RemoveDestroyedEntities();
      }
    }


    // ============================================================
    // 2. Behaviours
    // ============================================================

    foreach (Entity entity
        in BattleContext.Entities.ToArray())
    {
      if (!entity.Enabled ||
          entity.DestroyRequested)
      {
        continue;
      }

      foreach (Behaviour behaviour
          in entity.Behaviours.ToArray())
      {
        behaviour.Update(
            entity,
            BattleContext,
            deltaTime);
      }
    }


    // ============================================================
    // 3. Resolve attachments
    // ============================================================

    TransformSystem.ResolveAll(
        BattleContext.Entities);


    // ============================================================
    // 4. Register new collision entities
    // ============================================================

    RegisterNewCollisionEntities();


    // ============================================================
    // 5. Synchronize collision
    // ============================================================

    _collisionSystem.Synchronize();


    // ============================================================
    // 6. Rebuild dynamic layers
    // ============================================================

    _collisionWorld.RebuildDynamicLayers();


    // ============================================================
    // 7. Player vs obstacles
    // ============================================================

    foreach (BattleCollisionHit hit
        in _collisionSystem.Query(
            BattleContext.Player,
            "obstacles"))
    {
      HandlePlayerCollision(hit);
    }


    // ============================================================
    // 8. Remove anything destroyed during the frame
    // ============================================================

    RemoveDestroyedEntities();
  }

  // ============================================================
  // Detect newly spawned collision entities
  // ============================================================

  private void RegisterNewCollisionEntities()
  {
    foreach (Entity entity
        in BattleContext.Entities)
    {
      if (!entity.Enabled)
        continue;

      if (entity.CollisionMode ==
          CollisionMode.None)
      {
        continue;
      }

      if (_collisionWorld.Contains(entity))
        continue;

      RegisterEntityCollision(entity);
    }
  }


  // ============================================================
  // Remove destroyed entities
  // ============================================================

  private void RemoveDestroyedEntities()
  {
    foreach (Entity entity
        in BattleContext.Entities.ToArray())
    {
      if (!entity.DestroyRequested)
        continue;

      if (_collisionWorld.Contains(entity))
      {
        _collisionWorld.Remove(entity);
      }

      BattleContext.Remove(entity);
    }
  }


  // ============================================================
  // Collision response
  // ============================================================

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
    // }
  }


  // ============================================================
  // Scene Tick
  // ============================================================

  public override void Tick(
      GameTime gameTime,
      Game1 game)
  {
    float deltaTime =
        (float)gameTime.ElapsedGameTime.TotalSeconds;

    if (Input.Down(Keys.R))
    {
      game.GoToScene(
          new BattleScene());

      return;
    }

    BattleContext.Player.Scale = new(2.0f);

    UpdateBattle(deltaTime);
  }
  private void StartWave(BattleWave wave)
  {
    ArgumentNullException.ThrowIfNull(wave);

    BattleWave? currentWave =
        BattleContext.CurrentWave;

    if (currentWave != null)
    {
      currentWave.End(
          BattleContext);
    }

    BattleContext.CurrentWave =
        wave;

    wave.Start(
        BattleContext);
  }


  // ============================================================
  // Draw
  // ============================================================
  public override void Draw(
      GameTime gameTime,
      Game1 game)
  {
    if (_debugIsMagentaScreen)
      game.GraphicsDevice.Clear(Color.Magenta);
    else
      game.GraphicsDevice.Clear(Color.Black);


    // --------------------------------------------------------
    // Game
    // --------------------------------------------------------

    game.SpriteBatch.Begin(
        samplerState: SamplerState.PointClamp
    );

    game.SpriteBatch.DrawString(
        game.FontText,
        "Hello\nHi you",
        new Vector2(5, 5),
        Color.White
    );

    // Arena
    DrawArena(game);

    // Entities
    DrawEntities(game);

    game.SpriteBatch.End();


    // --------------------------------------------------------
    // Collision masks
    // --------------------------------------------------------

    if (_debugShowEntityMasks)
    {
      DrawEntityMasks(game);
    }


    // --------------------------------------------------------
    // Fade
    // --------------------------------------------------------

    game.SpriteBatch.Begin();

    game.SpriteBatch.Draw(
        game.Pixel,
        new Rectangle(
            0,
            0,
            game.GraphicsDevice.Viewport.Width,
            game.GraphicsDevice.Viewport.Height
        ),
        Color.Black * _fade
    );

    game.SpriteBatch.End();
  }


  // ============================================================
  // Entity rendering
  // ============================================================

  private void DrawEntities(
      Game1 game)
  {
    foreach (Entity entity
        in BattleContext.Entities)
    {
      if (!entity.Enabled ||
          !entity.Visible ||
          entity.Texture == null)
      {
        continue;
      }

      game.SpriteBatch.Draw(
          entity.Texture,
          entity.WorldPosition,
          null,
          entity.Tint,
          entity.WorldRotation,
          entity.Origin,
          entity.WorldScale,
          SpriteEffects.None,
          0f
      );
    }
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

    // Top
    game.SpriteBatch.Draw(
        game.Pixel,
        new Rectangle(
            (int)min.X,
            (int)min.Y,
            (int)arena.Size.X,
            (int)thickness),
        Color.White);

    // Bottom
    game.SpriteBatch.Draw(
        game.Pixel,
        new Rectangle(
            (int)min.X,
            (int)(max.Y - thickness),
            (int)arena.Size.X,
            (int)thickness),
        Color.White);

    // Left
    game.SpriteBatch.Draw(
        game.Pixel,
        new Rectangle(
            (int)min.X,
            (int)min.Y,
            (int)thickness,
            (int)arena.Size.Y),
        Color.White);

    // Right
    game.SpriteBatch.Draw(
        game.Pixel,
        new Rectangle(
            (int)(max.X - thickness),
            (int)min.Y,
            (int)thickness,
            (int)arena.Size.Y),
        Color.White);
  }

  // ============================================================
  // Debug masks
  // ============================================================

  private void DrawEntityMasks(
      Game1 game)
  {
    game.SpriteBatch.Begin(
        samplerState: SamplerState.PointClamp
    );

    foreach (Entity entity
        in BattleContext.Entities)
    {
      if (!entity.Enabled)
        continue;

      switch (entity.CollisionMode)
      {
        case CollisionMode.None:
          break;

        case CollisionMode.RotatedBox:
          DrawRotatedBoxMask(
              game,
              entity);
          break;

        case CollisionMode.PixelPerfect:
          DrawPixelMask(
              game,
              entity);
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
        entity.WorldRotation
        + entity.CollisionRotation;

    game.SpriteBatch.Draw(
        game.Pixel,
        center,
        null,
        Color.Red * 0.35f,
        rotation,
        new Vector2(
            0.5f,
            0.5f),
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
}