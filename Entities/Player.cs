using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using BattleGame.Core;

namespace BattleGame.Entities;

public sealed class Player
{
    public Vector2 Position;
    public float Radius = 8f;

    public float Speed = 150f;
    public float DashSpeed = 560f;
    public float DashDuration = 0.12f;
    public float DashCooldown = 0.55f;

    public bool Invulnerable;

    private float _dashTimer;
    private float _dashCooldownTimer;
    private Vector2 _dashDirection;

    public bool IsDashing => _dashTimer > 0;

    public void Reset(Vector2 position)
    {
        Position = position;
        _dashTimer = 0;
        _dashCooldownTimer = 0;
    }

    public void Update(GameTime gameTime, Input input, Rectangle bounds)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _dashCooldownTimer -= dt;

        Vector2 movement = input.Movement();

        if (input.Pressed(Microsoft.Xna.Framework.Input.Keys.LeftShift) ||
            input.Pressed(Microsoft.Xna.Framework.Input.Keys.RightShift))
        {
            if (_dashCooldownTimer <= 0 && movement != Vector2.Zero)
            {
                _dashDirection = movement;
                _dashTimer = DashDuration;
                _dashCooldownTimer = DashCooldown;
            }
        }

        if (_dashTimer > 0)
        {
            Position += _dashDirection * DashSpeed * dt;
            _dashTimer -= dt;
        }
        else
        {
            Position += movement * Speed * dt;
        }

        Position.X = MathHelper.Clamp(Position.X, bounds.Left + Radius, bounds.Right - Radius);
        Position.Y = MathHelper.Clamp(Position.Y, bounds.Top + Radius, bounds.Bottom - Radius);
    }

    public void Draw(SpriteBatch sb, Texture2D pixel)
    {
        var rect = new Rectangle(
            (int)(Position.X - Radius),
            (int)(Position.Y - Radius),
            (int)(Radius * 2),
            (int)(Radius * 2));

        sb.Draw(pixel, rect, Color.White);
    }
}
