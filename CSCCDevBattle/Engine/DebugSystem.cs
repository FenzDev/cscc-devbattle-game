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

    public bool Visible =>
        _visible;


    // ============================================================
    // Registration
    // ============================================================

    public void RegisterCmd(
        Keys key,
        string description,
        Action action,
        Func<string?> getState,
        KeyTrigger trigger = KeyTrigger.Pressed)
    {
        _lines.Add(
            new DebugCommand(
                key,
                description,
                action,
                getState,
                trigger
            )
        );
    }

    public void RegisterInfo(
        string description,
        Func<string> state)
    {
        _lines.Add(
            new DebugInfo(
                description,
                state
            )
        );
    }


    // ============================================================
    // Update
    // ============================================================

    public void Update(
        GameTime gameTime)
    {
#if DEBUG

        if (Input.Pressed(Keys.RightAlt))
            _visible = !_visible;

        foreach (DebugLine line in _lines)
        {
            // -----------------------------------------------
            // Command
            // -----------------------------------------------

            if (line is DebugCommand command)
            {
                bool triggered = command.Trigger switch
                {
                    KeyTrigger.Pressed =>
                        Input.Pressed(command.Key),

                    KeyTrigger.Down =>
                        Input.Down(command.Key),

                    KeyTrigger.Released =>
                        Input.Released(command.Key),

                    _ => false
                };

                if (triggered)
                    command.Action();
            }

            // -----------------------------------------------
            // Info
            // -----------------------------------------------

        }

#endif
    }


    // ============================================================
    // Draw
    // ============================================================

    public void Draw(
        SpriteBatch spriteBatch,
        SpriteFontBase font,
        Texture2D pixel)
    {
#if DEBUG

        if (!_visible)
            return;

        DrawMenu(
            spriteBatch,
            font,
            pixel
        );

#endif
    }


    // ============================================================
    // Menu
    // ============================================================

    private void DrawMenu(
        SpriteBatch spriteBatch,
        SpriteFontBase font,
        Texture2D pixel)
    {
        const int x = 0;
        const int y = 0;

        const int width = 400;

        const int padding = 12;
        const int headerHeight = 46;
        const int lineHeight = 32;

        int height =
            headerHeight +
            (_lines.Count * lineHeight) +
            padding;

        // --------------------------------------------------------
        // Background
        // --------------------------------------------------------

        spriteBatch.Draw(
            pixel,
            new Rectangle(
                x,
                y,
                width,
                height
            ),
            Color.Black * 0.88f
        );


        // --------------------------------------------------------
        // Header
        // --------------------------------------------------------

        spriteBatch.DrawString(
            font,
            "DEBUG",
            new Vector2(
                x + padding,
                y + 8
            ),
            Color.Yellow
        );


        // --------------------------------------------------------
        // Lines
        // --------------------------------------------------------

        int currentY =
            y + headerHeight;

        foreach (DebugLine line in _lines)
        {
            // ====================================================
            // Command
            // ====================================================

            if (line is DebugCommand command)
            {
                DrawCommand(
                    spriteBatch,
                    font,
                    command,
                    x,
                    currentY,
                    width,
                    padding
                );
            }

            // ====================================================
            // Info
            // ====================================================

            else if (line is DebugInfo info)
            {
                DrawInfo(
                    spriteBatch,
                    font,
                    info,
                    x,
                    currentY,
                    width,
                    padding
                );
            }

            currentY += lineHeight;
        }
    }


    // ============================================================
    // Command rendering
    // ============================================================

    private static void DrawCommand(
        SpriteBatch spriteBatch,
        SpriteFontBase font,
        DebugCommand command,
        int x,
        int y,
        int width,
        int padding)
    {
        // Key
        string keyText =
            command.Key.ToString();

        spriteBatch.DrawString(
            font,
            keyText,
            new Vector2(
                x + padding,
                y
            ),
            Color.LightGray
        );


        // Description
        const int descriptionOffset = 42;

        spriteBatch.DrawString(
            font,
            command.Description,
            new Vector2(
                x + descriptionOffset,
                y
            ),
            Color.White
        );


        // State
        if (command.GetState() is string state)
        {
            DrawState(
                spriteBatch,
                font,
                state,
                x,
                y,
                width,
                padding
            );
        }
    }


    // ============================================================
    // Info rendering
    // ============================================================

    private static void DrawInfo(
        SpriteBatch spriteBatch,
        SpriteFontBase font,
        DebugInfo info,
        int x,
        int y,
        int width,
        int padding)
    {
        // Info has no key.
        const int descriptionOffset = 12;

        spriteBatch.DrawString(
            font,
            info.Description,
            new Vector2(
                x + descriptionOffset,
                y
            ),
            Color.White
        );


        if (info.GetState() is string state)
        {
            DrawState(
                spriteBatch,
                font,
                state,
                x,
                y,
                width,
                padding
            );
        }
    }


    // ============================================================
    // State rendering
    // ============================================================

    private static void DrawState(
        SpriteBatch spriteBatch,
        SpriteFontBase font,
        string state,
        int x,
        int y,
        int width,
        int padding)
    {
        Vector2 stateSize =
            font.MeasureString(state);

        spriteBatch.DrawString(
            font,
            state,
            new Vector2(
                x + width - padding - stateSize.X,
                y
            ),
            Color.LightGreen
        );
    }
}