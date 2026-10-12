using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public sealed class PlayerBehaviour : Behaviour
{
    private readonly float _speed;

    public PlayerBehaviour(float speed = 220f)
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

        if (Input.Down(Keys.Left) || Input.Down(Keys.A))
            movement.X -= 1f;

        if (Input.Down(Keys.Right) || Input.Down(Keys.D))
            movement.X += 1f;

        if (Input.Down(Keys.Up) || Input.Down(Keys.W))
            movement.Y -= 1f;

        if (Input.Down(Keys.Down) || Input.Down(Keys.S))
            movement.Y += 1f;

        if (movement != Vector2.Zero)
        {
            movement.Normalize();

            float speedMultiplier = MathF.Max(0f, battle.PlayerMovementSpeedMultiplier);
            entity.Position += movement * _speed * speedMultiplier * deltaTime;
        }

        if (entity is not PlayerEntity player)
            return;

        entity.Position = battle.Arena.ClampEntityPosition(player, entity.Position);
    }
}
