using Microsoft.Xna.Framework;

public sealed class PlayerEntity : Entity
{
    public PlayerEntity()
    {
        Texture =
            Assets.GetTexture(
                "Battle/Player");

        Position = Vector2.Zero;
        Rotation = 0f;
        Scale = new Vector2(2f);

        // --------------------------------------------------------
        // Movement bounds
        // --------------------------------------------------------
        // null = entire texture.
        MovementBounds = new (1,0,6,8);

        // --------------------------------------------------------
        // Collision
        // --------------------------------------------------------

        Rectangle collisionMask =
            new(
                2,
                1,
                4,
                6);

        CollisionMode =
            CollisionMode.RotatedBox;

        CollisionSize =
            collisionMask.Size.ToVector2();

        Vector2 maskCenter =
            new(
                collisionMask.X +
                    collisionMask.Width * 0.5f,

                collisionMask.Y +
                    collisionMask.Height * 0.5f);

        CollisionOffset =
            maskCenter - Origin;

        CollisionRotation = 0f;

        CollisionLayer = "player";

        Behaviours.Add(
            PlayerBehaviourInstance);
    }

    private static readonly Behaviour
        PlayerBehaviourInstance =
            new PlayerBehaviour(240f);
}