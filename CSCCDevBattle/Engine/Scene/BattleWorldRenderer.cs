using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// Draw-only world plumbing: arena border, entities, collision masks, and scene fade.
/// </summary>
public sealed class BattleWorldRenderer
{
    private readonly BattleContext _battle;

    public BattleWorldRenderer(BattleContext battle)
    {
        _battle = battle ?? throw new ArgumentNullException(nameof(battle));
    }

    /// <summary>Call between SpriteBatch.Begin and SpriteBatch.End.</summary>
    public void DrawArenaAndEntities(Game1 game, float playerVisualAlpha)
    {
        DrawArena(game);
        DrawEntities(game, playerVisualAlpha);
    }

    public void DrawEntityMasks(Game1 game)
    {
        game.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        foreach (Entity entity in _battle.Entities)
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

    public void DrawFade(Game1 game, float fade)
    {
        if (fade <= 0f)
            return;

        game.SpriteBatch.Begin();
        game.SpriteBatch.Draw(
            game.Pixel,
            new Rectangle(
                0,
                0,
                game.GraphicsDevice.Viewport.Width,
                game.GraphicsDevice.Viewport.Height),
            Color.Black * fade);
        game.SpriteBatch.End();
    }

    private void DrawEntities(Game1 game, float playerVisualAlpha)
    {
        // Back attachments appear behind their parents.
        foreach (Entity entity in _battle.Entities)
        {
            if (!IsDrawable(entity) ||
                entity.Attachment?.VisualOrder != AttachmentVisualOrder.Back)
            {
                continue;
            }

            DrawEntity(game, entity, playerVisualAlpha);
        }

        // Root entities.
        foreach (Entity entity in _battle.Entities)
        {
            if (!IsDrawable(entity) || entity.Attachment is not null)
                continue;

            DrawEntity(game, entity, playerVisualAlpha);
        }

        // Front attachments appear in front of their parents.
        foreach (Entity entity in _battle.Entities)
        {
            if (!IsDrawable(entity) ||
                entity.Attachment?.VisualOrder != AttachmentVisualOrder.Front)
            {
                continue;
            }

            DrawEntity(game, entity, playerVisualAlpha);
        }
    }

    private static bool IsDrawable(Entity entity)
    {
        return entity.Enabled &&
               entity.Visible &&
               !entity.DestroyRequested &&
               entity.Texture is not null;
    }

    private void DrawEntity(
        Game1 game,
        Entity entity,
        float playerVisualAlpha)
    {
        game.SpriteBatch.Draw(
            entity.Texture!,
            entity.WorldPosition,
            null,
            GetEntityDrawTint(entity, playerVisualAlpha),
            entity.WorldRotation,
            entity.Origin,
            entity.WorldScale,
            SpriteEffects.None,
            0f);
    }

    private Color GetEntityDrawTint(Entity entity, float playerVisualAlpha)
    {
        Color tint = entity.Tint;
        bool isPlayerVisual =
            ReferenceEquals(entity, _battle.Player) || entity.FadeWithPlayer;

        if (!isPlayerVisual)
            return tint;

        PlayerEntity player = _battle.Player;
        tint = Color.Lerp(
            tint,
            Color.White,
            MathHelper.Clamp(player.HitFlashAmount, 0f, 1f));

        float alpha = playerVisualAlpha * player.DamageBlinkOpacity;
        tint.A = (byte)Math.Clamp(
            (int)MathF.Round(tint.A * alpha),
            0,
            255);

        return tint;
    }

    private void DrawArena(Game1 game)
    {
        BattleArena arena = _battle.Arena;
        Vector2 min = arena.Minimum;
        Vector2 max = arena.Maximum;

        float thickness = arena.BorderThickness;
        int x = (int)min.X;
        int y = (int)min.Y;
        int width = (int)arena.Size.X;
        int height = (int)arena.Size.Y;
        int border = (int)thickness;

        game.SpriteBatch.Draw(
            game.Pixel,
            new Rectangle(x, y, width, border),
            Color.White);
        game.SpriteBatch.Draw(
            game.Pixel,
            new Rectangle(x, (int)(max.Y - thickness), width, border),
            Color.White);
        game.SpriteBatch.Draw(
            game.Pixel,
            new Rectangle(x, y, border, height),
            Color.White);
        game.SpriteBatch.Draw(
            game.Pixel,
            new Rectangle((int)(max.X - thickness), y, border, height),
            Color.White);
    }

    private static void DrawRotatedBoxMask(Game1 game, Entity entity)
    {
        Vector2 center = TransformSystem.TransformOffset(
            entity,
            entity.CollisionOffset);

        Vector2 size = TransformSystem.Multiply(
            entity.CollisionSize,
            TransformSystem.Abs(entity.WorldScale));

        float rotation = entity.WorldRotation + entity.CollisionRotation;

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

    private static void DrawPixelMask(Game1 game, Entity entity)
    {
        if (entity.Texture is null)
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
