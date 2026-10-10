
using System;
using Microsoft.Xna.Framework;

public enum BossPose
{
    Pockets = 1,
    Pointing = 2,
    HandsRaised = 3
}

public sealed class BossCharacter
{
    public BossPartEntity Legs { get; }
    public BossPartEntity Torso { get; }
    public BossPartEntity Head { get; }

    public BossPose Pose { get; private set; } = BossPose.Pockets;
    public bool IsTired { get; private set; }

    public bool IdleBobEnabled { get; set; } = true;
    public float IdleBobSpeed { get; set; } = 2.5f;
    public float IdleBobAmplitude { get; set; } = 1f;

    private float _idleTime;
    private bool _spawned;

    public BossCharacter()
    {
        Legs = CreatePart("Battle/BossLegs");
        Torso = CreatePart("Battle/BossTorso1");
        Head = CreatePart("Battle/BossHead");

        // The torso is now the root of the character.
        Torso.Scale = new Vector2(4f);

        // Legs render behind the torso.
        Legs.AttachTo(
            Torso,
            new Vector2(0f, -2f), // Move legs up by 2 local units
            visualOrder: AttachmentVisualOrder.Back);

        // Head renders in front of the torso.
        Head.AttachTo(
            Torso,
            Vector2.Zero,
            visualOrder: AttachmentVisualOrder.Front);

        SetVisible(false);
    }

    private static BossPartEntity CreatePart(string texturePath)
    {
        return new BossPartEntity
        {
            Texture = Assets.GetTexture(texturePath),
            Visible = false,
            Scale = Vector2.One
        };
    }

    public void SetVisible(bool visible)
    {
        Legs.Visible = visible;
        Torso.Visible = visible;
        Head.Visible = visible;
    }


    /// <summary>
    /// The position of the whole character is the root's position.
    /// </summary>
    public Vector2 Position
    {
        get => Torso.Position;
        set => Torso.Position = value;
    }

    /// <summary>
    /// Adds all three parts to the battle exactly once.
    /// Call this when the boss is first created, not every wave.
    /// </summary>
    public void Spawn(BattleContext context, Vector2 position)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_spawned)
            throw new InvalidOperationException(
                "This boss has already been spawned.");

        Position = position;

        context.Spawn(Legs);
        context.Spawn(Torso);
        context.Spawn(Head);

        _spawned = true;
    }

    public void SetPose(BossPose pose)
    {
        string torsoTexture = pose switch
        {
            BossPose.Pockets =>
                "Battle/BossTorso1",

            BossPose.Pointing =>
                "Battle/BossTorso2",

            BossPose.HandsRaised =>
                "Battle/BossTorso3",

            _ => throw new ArgumentOutOfRangeException(
                nameof(pose), pose, "Unknown boss pose.")
        };

        Pose = pose;
        Torso.Texture = Assets.GetTexture(torsoTexture);
    }

    public void SetTired(bool tired)
    {
        IsTired = tired;

        Head.Texture = Assets.GetTexture(
            tired
                ? "Battle/BossHeadTired"
                : "Battle/BossHead");
    }

    /// <summary>
    /// Call once per frame from your existing battle update.
    /// </summary>
    public void Update(float deltaSeconds)
    {
        if (!IdleBobEnabled)
        {
            Head.Position = Vector2.Zero;
            return;
        }

        _idleTime += MathF.Max(0f, deltaSeconds);

        float bob = MathF.Sin(_idleTime * IdleBobSpeed)
                    * IdleBobAmplitude;

        // Whole-pixel movement suits pixel-art sprites.
        Head.Position = new Vector2(0f, -20f) + new Vector2(0f, MathF.Round(bob));
    }
}
