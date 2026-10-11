using System;
using Microsoft.Xna.Framework;

/// <summary>Client and server exchange HTTP requests and differently paced file payloads.</summary>
public sealed class HttpClientTrafficWave : DepartmentWaveBase
{
    private DepartmentVisualEntity _client = null!;
    private DepartmentVisualEntity _server = null!;
    private float _sendTimer;
    private float _domBurstTimer;
    private int _packetIndex;

    public HttpClientTrafficWave() => Duration = 11.5f;
    public override Vector2 ArenaSize => new(280f, 240f);

    protected override void OnStart(BattleContext battle)
    {
        Vector2 c = battle.Arena.Center;
        _client = Sprite(battle, new Vector2(battle.Arena.InnerMinimum.X + 15f, c.Y),
            "Battle/Projectiles/Web/Client", 2.15f, new Color(105, 220, 255));
        _server = Sprite(battle, new Vector2(battle.Arena.InnerMaximum.X - 15f, c.Y),
            "Battle/Projectiles/Web/Server", 2.15f, new Color(255, 190, 115));
        _sendTimer = 0.35f;
        _domBurstTimer = 1.65f;
        _packetIndex = 0;
        DebugState = "Client on the left, server on the right; request and payload traffic warming up";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        _sendTimer -= deltaTime;
        _domBurstTimer -= deltaTime;
        if (_domBurstTimer <= 0f)
        {
            _domBurstTimer += 2.55f;
            SendDomBurst(battle);
        }

        if (_sendTimer <= 0f)
        {
            _sendTimer += 0.48f + Random.Shared.NextSingle() * 0.15f;
            SendPacketPair(battle);
        }

        float bob = MathF.Sin(Elapsed * 2.7f) * 9f;
        _client.Position = new Vector2(battle.Arena.InnerMinimum.X + 16f,
            battle.Arena.Center.Y + bob);
        _server.Position = new Vector2(battle.Arena.InnerMaximum.X - 16f,
            battle.Arena.Center.Y - bob);
        DebugState = $"HTTP exchanges={_packetIndex}; payloads vary by type and speed";
    }

    private void SendPacketPair(BattleContext battle)
    {
        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;
        bool clientToServer = _packetIndex % 2 == 0;
        float baseY = battle.Arena.Center.Y + (( _packetIndex % 5) - 2) * 23f;
        float jitter = Random.Shared.NextSingle() * 18f - 9f;
        float x = clientToServer ? min.X + 24f : max.X - 24f;
        float direction = clientToServer ? 1f : -1f;
        float vertical = Random.Shared.NextSingle() * 50f - 25f;
        Vector2 requestVelocity = new(direction * 165f, vertical);
        Vector2 payloadVelocity;
        string payloadAsset;
        float payloadSpeed;
        Color payloadColor;

        switch (_packetIndex % 4)
        {
            case 0:
                payloadAsset = "Battle/Projectiles/Web/ProjectileFileJson";
                payloadSpeed = 182f;
                payloadColor = new Color(130, 245, 170);
                break;
            case 1:
                payloadAsset = "Battle/Projectiles/Web/ProjectileFileHtml";
                payloadSpeed = 105f;
                payloadColor = new Color(255, 170, 95);
                break;
            case 2:
                payloadAsset = "Battle/Projectiles/Web/ProjectileFileCss";
                payloadSpeed = 132f;
                payloadColor = new Color(105, 220, 255);
                break;
            default:
                payloadAsset = "Battle/Projectiles/Web/ProjectileFileJs";
                payloadSpeed = 150f;
                payloadColor = new Color(255, 230, 105);
                break;
        }

        SpriteProjectile(battle, new Vector2(x, MathHelper.Clamp(baseY + jitter, min.Y + 12f, max.Y - 12f)),
            "Battle/Projectiles/Web/ProjectileRequestHTTP", requestVelocity,
            1.35f, new Color(245, 245, 255), lifetime: 3f,
            damaging: true, pixelPerfect: true);

        // The payload uses a different lane and per-file speed so packets form readable gaps.
        float payloadY = MathHelper.Clamp(baseY - jitter * 0.65f, min.Y + 12f, max.Y - 12f);
        payloadVelocity = new Vector2(direction * payloadSpeed, -vertical * 0.55f);
        SpriteProjectile(battle, new Vector2(x, payloadY), payloadAsset,
            payloadVelocity, 1.35f, payloadColor, lifetime: 3.2f,
            damaging: true, pixelPerfect: true);
        _packetIndex++;
    }
    private void SendDomBurst(BattleContext battle)
    {
        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;
        string[] domAssets =
        {
            "Battle/Projectiles/Web/ProjectileDOMHtml",
            "Battle/Projectiles/Web/ProjectileDOMDiv",
            "Battle/Projectiles/Web/ProjectileDOMP",
            "Battle/Projectiles/Web/ProjectileDOMH1"
        };

        // Every few requests the server emits a small HTML/DOM volley across the arena.
        for (int i = 0; i < 3; i++)
        {
            float y = min.Y + 22f + Random.Shared.NextSingle() * (max.Y - min.Y - 44f);
            float vertical = (Random.Shared.NextSingle() * 46f) - 23f;
            SpriteProjectile(battle, new Vector2(max.X - 18f, y),
                domAssets[(_packetIndex + i) % domAssets.Length],
                new Vector2(-112f - i * 14f, vertical), 1.35f,
                new Color(105, 220, 255), lifetime: 3f,
                damaging: true, pixelPerfect: true);
        }
    }

}
