using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Stores only alpha values for a texture.
/// </summary>
public sealed class PixelMask
{
    private readonly byte[] _alpha;

    public int Width { get; }

    public int Height { get; }

    public PixelMask(Texture2D texture)
    {
        ArgumentNullException.ThrowIfNull(texture);

        Width = texture.Width;
        Height = texture.Height;

        Color[] pixels =
            new Color[Width * Height];

        texture.GetData(pixels);

        _alpha = new byte[pixels.Length];

        for (int i = 0; i < pixels.Length; i++)
        {
            _alpha[i] = pixels[i].A;
        }
    }

    public byte GetAlpha(
        int x,
        int y)
    {
        return _alpha[
            y * Width + x];
    }

    public bool IsSolid(
        int x,
        int y,
        byte threshold)
    {
        return GetAlpha(x, y) >= threshold;
    }
}

/// <summary>
/// Texture -> PixelMask cache.
///
/// ConditionalWeakTable means the texture can still be
/// garbage collected when no longer referenced elsewhere.
/// </summary>
public sealed class PixelMaskCache
{
    private readonly ConditionalWeakTable<
        Texture2D,
        PixelMask> _cache = new();

    public PixelMask Get(Texture2D texture)
    {
        return _cache.GetValue(
            texture,
            static t => new PixelMask(t));
    }
}