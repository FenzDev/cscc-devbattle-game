using System;
using Microsoft.Xna.Framework;

/// <summary>CSS mutates DOM blocks; the JS gun then makes one block interactive.</summary>
public sealed class CssJsGunWave : DepartmentWaveBase
{
    private DepartmentVisualEntity _gun = null!;
    private readonly DepartmentVisualEntity[] _targets = new DepartmentVisualEntity[4];
    private readonly Vector2[] _targetOrigins = new Vector2[4];
    private float _shotTimer;
    private int _shotCount;
    private bool _switchedToJs;
    private float _interactiveAge;

    public CssJsGunWave() => Duration = 12f;
    public override Vector2 ArenaSize => new(280f, 240f);

    protected override void OnStart(BattleContext battle)
    {
        Vector2 center = battle.Arena.Center;
        Vector2 min = battle.Arena.InnerMinimum;
        _gun = Sprite(battle, new Vector2(min.X - 36f, center.Y),
            "Battle/Projectiles/Web/CSSGun", 2.8f, Color.White);

        string[] targetAssets =
        {
            "Battle/Projectiles/Web/ProjectileDOMDiv",
            "Battle/Projectiles/Web/ProjectileDOMH1",
            "Battle/Projectiles/Web/ProjectileDOMP",
            "Battle/Projectiles/Web/ProjectileDOMHtml"
        };
        Vector2[] offsets =
        {
            new(6f, -48f), new(54f, -12f), new(10f, 42f), new(76f, 48f)
        };
        for (int i = 0; i < _targets.Length; i++)
        {
            _targetOrigins[i] = center + offsets[i];
            _targets[i] = Sprite(battle, _targetOrigins[i], targetAssets[i], 1.55f,
                new Color(150, 205, 255));
            _targets[i].CollisionSize *= 0.8f;
        }

        _shotTimer = 0.5f;
        _shotCount = 0;
        _switchedToJs = false;
        _interactiveAge = 0f;
        DebugState = "CSS gun entering from the left; blocks are about to be restyled";
    }

    protected override void OnUpdate(BattleContext battle, float deltaTime)
    {
        Vector2 min = battle.Arena.InnerMinimum;
        Vector2 c = battle.Arena.Center;
        float gunTargetX = min.X + 25f;
        _gun.Position = new Vector2(
            MathHelper.Lerp(_gun.Position.X, gunTargetX, MathHelper.Clamp(deltaTime * 3.2f, 0f, 1f)),
            c.Y + MathF.Sin(Elapsed * 2.2f) * 8f);

        if (!_switchedToJs && Elapsed >= 5.35f)
        {
            _switchedToJs = true;
            _gun.Texture = Assets.GetTexture("Battle/Projectiles/Web/JSGun");
            _gun.Tint = new Color(255, 245, 175);
            for (int i = 1; i < _targets.Length; i++)
                _targets[i].CollisionMode = CollisionMode.None;
            _targets[0].ContactDamage = 1f;
            _targets[0].CollisionLayer = "obstacles";
            _targets[0].CollisionMode = CollisionMode.RotatedBox;
            _targets[0].Tint = new Color(255, 140, 200);
            _interactiveAge = 0f;
        }

        _shotTimer -= deltaTime;
        if (_shotTimer <= 0f)
        {
            _shotTimer += _switchedToJs ? 1.15f : 0.82f;
            FireScriptToken(battle);
        }

        if (!_switchedToJs)
        {
            for (int i = 0; i < _targets.Length; i++)
            {
                float pulse = 1.15f + 0.65f * (0.5f + 0.5f * MathF.Sin(Elapsed * 3.2f + i * 1.3f));
                _targets[i].Scale = new Vector2(pulse);
                _targets[i].Position = _targetOrigins[i] + new Vector2(
                    MathF.Sin(Elapsed * 1.8f + i) * 9f,
                    MathF.Sin(Elapsed * 2.7f + i * 1.2f) * 10f);
                _targets[i].Rotation = MathF.Sin(Elapsed * 2.0f + i) * 0.45f;
                _targets[i].Tint = new Color(
                    (byte)(110 + 120 * (0.5f + 0.5f * MathF.Sin(Elapsed * 2.4f + i))),
                    (byte)(115 + 125 * (0.5f + 0.5f * MathF.Sin(Elapsed * 2.8f + i * 1.7f))),
                    (byte)(130 + 120 * (0.5f + 0.5f * MathF.Sin(Elapsed * 3.1f + i * 0.8f))));
                _targets[i].CollisionMode = pulse > 1.55f ? CollisionMode.RotatedBox : CollisionMode.None;
                _targets[i].CollisionLayer = "obstacles";
                _targets[i].ContactDamage = 1f;
            }
        }
        else
        {
            _interactiveAge += deltaTime;
            DepartmentVisualEntity interactive = _targets[0];
            Vector2 direction = Direction(interactive.Position, battle.Player.Position);
            float speed = 26f + MathF.Min(34f, _interactiveAge * 4f);
            interactive.Position += direction * speed * deltaTime;
            interactive.Position += new Vector2(0f, MathF.Sin(Elapsed * 3.4f) * 8f * deltaTime);
            interactive.Rotation = MathF.Atan2(direction.Y, direction.X) * 0.25f;
            interactive.Scale = new Vector2(1.65f + 0.15f * MathF.Sin(Elapsed * 4f));
        }

        DebugState = _switchedToJs
            ? $"JS mode: interactive DOM object pursuing player; script shots={_shotCount}"
            : $"CSS mode: restyling animated DOM blocks; CSS tokens={_shotCount}";
    }

    private void FireScriptToken(BattleContext battle)
    {
        Vector2 origin = _gun.Position + new Vector2(10f, Random.Shared.NextSingle() * 18f - 9f);
        if (!_switchedToJs)
        {
            int targetIndex = _shotCount % _targets.Length;
            Vector2 direction = Direction(origin, _targets[targetIndex].Position);
            SpriteProjectile(battle, origin, "Battle/Projectiles/Web/ProjectileFileCss",
                direction * 190f, 1.05f, new Color(100, 225, 255),
                lifetime: 1.2f, damaging: false, destroyOnContact: false);
        }
        else
        {
            Vector2 direction = Direction(origin, _targets[0].Position);
            SpriteProjectile(battle, origin, "Battle/Projectiles/Web/ProjectileFileJs",
                direction * 200f, 1.1f, new Color(255, 225, 125),
                lifetime: 1.4f, damaging: false, destroyOnContact: false);
        }
        _shotCount++;
    }
}
