using System;
using System.Collections.Generic;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CSCCDevBattle;

public sealed class DebugSystem
{
  private readonly List<DebugCommand> _commands = [];

  private bool _visible;

  public bool Visible => _visible;

  public void Register(
     Keys key,
     string description,
     Action action,
     Func<string?> getState,
     KeyTrigger trigger = KeyTrigger.Pressed)
  {
    _commands.Add(
        new DebugCommand(
            key,
            description,
            action,
            getState,
            trigger
        )
    );
  }

  public void Update(GameTime gameTime)
  {
#if DEBUG

    if (Input.Pressed(Keys.RightAlt))
      _visible = !_visible;

    foreach (var command in _commands)
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

#endif
  }

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
        (_commands.Count * lineHeight) +
        padding;

    // Background
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

    // Header
    spriteBatch.DrawString(
        font,
        "DEBUG",
        new Vector2(
            x + padding,
            y + 8
        ),
        Color.Yellow
    );

    int currentY = y + headerHeight;

    foreach (var command in _commands)
    {
      // -------------------------
      // Key
      // -------------------------

      string keyText = command.Key.ToString();

      spriteBatch.DrawString(
          font,
          keyText,
          new Vector2(
              x + padding,
              currentY
          ),
          Color.LightGray
      );

      // -------------------------
      // Description
      // -------------------------

      // Keep the gap small.
      const int descriptionOffset = 42;

      spriteBatch.DrawString(
          font,
          command.Description,
          new Vector2(
              x + descriptionOffset,
              currentY
          ),
          Color.White
      );

      // -------------------------
      // State / Output
      // -------------------------

      if (command.GetState() is string state)
      {
        Vector2 stateSize = font.MeasureString(state);

        spriteBatch.DrawString(
            font,
            state,
            new Vector2(
                x + width - padding - stateSize.X,
                currentY
            ),
            Color.LightGreen
        );
      }

      currentY += lineHeight;
    }
  }
}