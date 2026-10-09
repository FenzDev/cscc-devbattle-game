using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public sealed class PlayerBehaviour : Behaviour
{
  private readonly float _speed;

  public PlayerBehaviour(
      float speed = 240f)
  {
    _speed = speed;
  }

  public override void Update(
      Entity entity,
      BattleContext battle,
      float deltaTime)
  {
    
    if (!battle.PlayerMovementEnabled)
    {
      entity.Visible = false;  
      return;
    }
    entity.Visible = true;  

    Vector2 movement = Vector2.Zero;

    // --------------------------------------------------------
    // Horizontal
    // --------------------------------------------------------

    if (Input.Down(Keys.Left) ||
        Input.Down(Keys.A))
    {
      movement.X -= 1f;
    }

    if (Input.Down(Keys.Right) ||
        Input.Down(Keys.D))
    {
      movement.X += 1f;
    }

    // --------------------------------------------------------
    // Vertical
    // --------------------------------------------------------

    if (Input.Down(Keys.Up) ||
        Input.Down(Keys.W))
    {
      movement.Y -= 1f;
    }

    if (Input.Down(Keys.Down) ||
        Input.Down(Keys.S))
    {
      movement.Y += 1f;
    }

    // --------------------------------------------------------
    // Normalize diagonal movement
    // --------------------------------------------------------

    if (movement != Vector2.Zero)
    {
      movement.Normalize();

      entity.Position +=
          movement *
          _speed *
          deltaTime;
    }

    // --------------------------------------------------------
    // Keep player inside the arena
    // --------------------------------------------------------

    if (entity is not PlayerEntity player)
      return;

    entity.Position =
    battle.Arena.ClampEntityPosition(
      player,
      entity.Position);
  }
}