using System;
using System.Collections.Generic;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public sealed class DebugSystem
{
    private readonly List<DebugLine> _lines = [];
    private bool _visible;
    public bool Visible => _visible;

    public void RegisterCmd(
        Keys key,
        string description,
        Action action,
        Func<string?> getState,
        KeyTrigger trigger = KeyTrigger.Pressed)
    {
        _lines.Add(new DebugCommand(key, description, action, getState, trigger));
    }

    public void RegisterInfo(string description, Func<string> state)
    {
        _lines.Add(new DebugInfo(description, state));
    }

    public void Update(GameTime gameTime)
    {
#if DEBUG
        if (Input.Pressed(Keys.RightAlt))
            _visible = !_visible;

        foreach (DebugLine line in _lines)
        {
            if (line is not DebugCommand command)
                continue;

            bool triggered = command.Trigger switch
            {
                KeyTrigger.Pressed => Input.Pressed(command.Key),
                KeyTrigger.Down => Input.Down(command.Key),
                KeyTrigger.Released => Input.Released(command.Key),
                _ => false
            };

            if (triggered)
                command.Action();
        }
#endif
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFontBase font, Texture2D pixel)
    {
#if DEBUG
        if (_visible)
            DrawMenu(spriteBatch, font, pixel);
#endif
    }

    private void DrawMenu(SpriteBatch spriteBatch, SpriteFontBase font, Texture2D pixel)
    {
        const int x = 0;
        const int y = 0;
        const int width = 520;
        const int padding = 12;
        const int headerHeight = 46;
        const int lineHeight = 24;

        int height = headerHeight + (_lines.Count * lineHeight) + padding;
        spriteBatch.Draw(pixel, new Rectangle(x, y, width, height), Color.Black * 0.88f);

        spriteBatch.DrawString(
            font,
            "DEBUG  |  ESC Pause/Resume  |  RightAlt Hide",
            new Vector2(x + padding, y + 8),
            Color.Yellow);

        int currentY = y + headerHeight;
        foreach (DebugLine line in _lines)
        {
            if (line is DebugCommand command)
                DrawCommand(spriteBatch, font, command, x, currentY, width, padding);
            else if (line is DebugInfo info)
                DrawInfo(spriteBatch, font, info, x, currentY, width, padding);

            currentY += lineHeight;
        }
    }

    private static void DrawCommand(
        SpriteBatch spriteBatch,
        SpriteFontBase font,
        DebugCommand command,
        int x,
        int y,
        int width,
        int padding)
    {
        spriteBatch.DrawString(font, command.Key.ToString(),
            new Vector2(x + padding, y), Color.LightGray);

        const int descriptionOffset = 42;
        spriteBatch.DrawString(font, command.Description,
            new Vector2(x + descriptionOffset, y), Color.White);

        string? state = command.GetState();
        if (!string.IsNullOrEmpty(state))
            DrawState(spriteBatch, font, state, x, y, width, padding);
    }

    private static void DrawInfo(
        SpriteBatch spriteBatch,
        SpriteFontBase font,
        DebugInfo info,
        int x,
        int y,
        int width,
        int padding)
    {
        spriteBatch.DrawString(font, info.Description,
            new Vector2(x + padding, y), Color.White);

        string? state = info.GetState();
        if (!string.IsNullOrEmpty(state))
            DrawState(spriteBatch, font, state, x, y, width, padding);
    }

    private static void DrawState(
        SpriteBatch spriteBatch,
        SpriteFontBase font,
        string state,
        int x,
        int y,
        int width,
        int padding)
    {
        Vector2 stateSize = font.MeasureString(state);
        spriteBatch.DrawString(font, state,
            new Vector2(x + width - padding - stateSize.X, y),
            Color.LightGreen);
    }
}
