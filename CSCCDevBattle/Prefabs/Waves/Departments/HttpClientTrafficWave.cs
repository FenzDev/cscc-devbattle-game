using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

/// <summary>
/// Client and server exchange spaced, ordered requests and responses.
/// HTML file payloads split off HEAD/BODY DOM hazards while in flight.
/// </summary>
public sealed class HttpClientTrafficWave : DepartmentWaveBase
{
    private sealed class HtmlCarrier
    {
        public DepartmentVisualEntity File = null!;
        public float Age;
        public float BurstDelay;
    }

    private static readonly string[] FileAssets =
    [
        "Battle/Projectiles/Web/ProjectileFileJson",
        "Battle/Projectiles/Web/ProjectileFileHtml",
        "Battle/Projectiles/Web/ProjectileFileCss",
        "Battle/Projectiles/Web/ProjectileFileJs"
    ];

    private const float TrafficHtmlBurstDelay = 1.30f;
    private const float AimedHtmlBurstDelay = 0.62f;

    private readonly List<HtmlCarrier> _htmlCarriers = [];
    private DepartmentVisualEntity _client = null!;
    private DepartmentVisualEntity _server = null!;
    private float _sendTimer;
    private float _responseDelay;
    private float _htmlTimer;
    private int _packetIndex;
    private bool _responsePending;
    private string _responseAsset = "Battle/Projectiles/Web/ProjectileFileJson";
    private float _responseY;
    private int _htmlSplits;

    public HttpClientTrafficWave() => Duration = 18.0f;
    public override Vector2 ArenaSize => new(280f, 240f);

    protected override void OnStart(BattleContext battle)
    {
        Vector2 c = battle.Arena.Center;
        _client = Sprite(battle, new Vector2(battle.Arena.Minimum.X - 120f, c.Y),
            "Battle/Projectiles/Web/Client", 4.0f, Color.White);
        _server = Sprite(battle, new Vector2(battle.Arena.Maximum.X + 120f, c.Y),
            "Battle/Projectiles/Web/Server", 4.0f, Color.White);

        _htmlCarriers.Clear();
        _sendTimer = 0.7f;
        _responseDelay = 1f;
        _htmlTimer = 2.0f;
        _packetIndex = 0;
        _htmlSplits = 0;
        _responsePending = false;
        _responseAsset = FileAssets[0];
        _responseY = c.Y;
        DebugState = "Client/server exchange traffic; HTML files split into HEAD and BODY hazards mid-flight";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        _sendTimer -= deltaTime;
        _htmlTimer -= deltaTime;

        if (_responsePending)
        {
            _responseDelay -= deltaTime;
            if (_responseDelay <= 0f)
                SendResponse(battle);
        }

        if (_sendTimer <= 0f)
        {
            _sendTimer += 1.45f + Random.Shared.NextSingle() * 0.28f;
            SendRequest(battle);
        }

        if (_htmlTimer <= 0f)
        {
            _htmlTimer += 2.25f + Random.Shared.NextSingle() * 0.75f;
            SendHtmlAtPlayer(battle);
        }

        UpdateHtmlCarriers(battle, deltaTime);

        float bob = MathF.Sin(Elapsed * 1.6f) * 4f;
        _client.Position = new Vector2(battle.Arena.Minimum.X - 38f, battle.Arena.Center.Y + bob);
        _server.Position = new Vector2(battle.Arena.Maximum.X + 38f, battle.Arena.Center.Y - bob);
        DebugState = $"HTTP transactions={_packetIndex}; HTML DOM splits={_htmlSplits}; ordered file traffic";
    }

    private void SendRequest(BattleContext battle)
    {
        if (_responsePending)
            return; // Keep request/response exchanges ordered.

        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 max = battle.Arena.InnerMaximum;
        int lane = _packetIndex % 3;
        float laneY = min.Y + 38f + lane * ((max.Y - min.Y - 76f) / 2f);
        float jitter = Random.Shared.NextSingle() * 10f - 5f;
        _responseY = MathHelper.Clamp(
            laneY + (lane % 2 == 0 ? 18f : -18f) + jitter,
            min.Y + 18f,
            max.Y - 18f);

        SpriteProjectile(
            battle,
            new Vector2(battle.Arena.Minimum.X - 16f, laneY),
            "Battle/Projectiles/Web/ProjectileRequestHTTP",
            new Vector2(165f, 0f),
            1.6f,
            Color.White,
            lifetime: 2.35f,
            damaging: true,
            pixelPerfect: true);

        // File type and speed are selected per transaction; response travels back
        // from the server on a neighboring lane, not stacked directly on the request.
        _responseAsset = FileAssets[Random.Shared.Next(FileAssets.Length)];
        _responseDelay = 1.35f;
        _responsePending = true;
        _packetIndex++;
    }

    private void SendResponse(BattleContext battle)
    {
        Vector2 start = new(battle.Arena.Maximum.X + 16f, _responseY);
        float speed = _responseAsset.EndsWith("Json", StringComparison.Ordinal) ? 165f
            : _responseAsset.EndsWith("Html", StringComparison.Ordinal) ? 112f
            : _responseAsset.EndsWith("Css", StringComparison.Ordinal) ? 128f
            : 145f;

        DepartmentVisualEntity file = SpriteProjectile(
            battle, start, _responseAsset, new Vector2(-speed, 0f),
            1.65f, Color.White, lifetime: 2.6f,
            damaging: true, pixelPerfect: true);

        if (_responseAsset.EndsWith("Html", StringComparison.Ordinal))
            TrackHtmlCarrier(file, TrafficHtmlBurstDelay);

        _responsePending = false;
    }

    private void SendHtmlAtPlayer(BattleContext battle)
    {
        Vector2 start = new(
            battle.Arena.InnerMaximum.X - 5f,
            battle.Arena.InnerMinimum.Y + 20f +
            Random.Shared.NextSingle() * (battle.Arena.Size.Y - 40f));
        Vector2 direction = Direction(start, battle.Player.Position);

        DepartmentVisualEntity file = SpriteProjectile(
            battle, start, "Battle/Projectiles/Web/ProjectileFileHtml",
            direction * (124f + Random.Shared.NextSingle() * 20f),
            1.7f, Color.White, lifetime: 3.0f,
            damaging: true, pixelPerfect: true);

        TrackHtmlCarrier(file, AimedHtmlBurstDelay);
    }

    private void TrackHtmlCarrier(DepartmentVisualEntity file, float burstDelay)
    {
        _htmlCarriers.Add(new HtmlCarrier
        {
            File = file,
            Age = 0f,
            BurstDelay = burstDelay
        });
    }

    private void UpdateHtmlCarriers(BattleContext battle, float deltaTime)
    {
        // Track only HTML carriers until they fire once. The HTML file continues
        // along its original route after emitting the DOM parts.
        for (int i = _htmlCarriers.Count - 1; i >= 0; i--)
        {
            HtmlCarrier carrier = _htmlCarriers[i];
            if (carrier.File.DestroyRequested)
            {
                _htmlCarriers.RemoveAt(i);
                continue;
            }

            carrier.Age += deltaTime;
            if (carrier.Age < carrier.BurstDelay)
                continue;

            EmitDomChildren(battle, carrier.File.Position);
            _htmlCarriers.RemoveAt(i);
            _htmlSplits++;
        }
    }

    private void EmitDomChildren(BattleContext battle, Vector2 origin)
    {
        // HTML -> HEAD + BODY. Both children aim toward the player's current
        // position, with a small split angle so the pair makes a readable dodge.
        Vector2 towardPlayer = Direction(origin, battle.Player.Position);
        Vector2 headDirection = Rotate(towardPlayer, -0.22f);
        Vector2 bodyDirection = Rotate(towardPlayer, 0.22f);

        SpriteProjectile(
            battle, origin, "Battle/Projectiles/Web/ProjectileDOMHead",
            headDirection * 132f,
            1.3f, Color.White, lifetime: 2.8f,
            damaging: true, pixelPerfect: true);

        SpriteProjectile(
            battle, origin, "Battle/Projectiles/Web/ProjectileDOMBody",
            bodyDirection * 144f,
            1.4f, Color.White, lifetime: 2.8f,
            damaging: true, pixelPerfect: true);
    }
}
