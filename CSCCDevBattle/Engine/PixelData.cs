using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed class PixelData
{
    public Texture2D Texture { get; }

    public Color[] Pixels { get; }

    public PixelData(Texture2D texture)
    {
        Texture = texture;

        Pixels = new Color[
            texture.Width * texture.Height
        ];

        texture.GetData(Pixels);
    }

    public Color GetPixel(int x, int y)
    {
        return Pixels[y * Texture.Width + x];
    }
}