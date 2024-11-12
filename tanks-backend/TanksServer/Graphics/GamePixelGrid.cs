using System.Collections;
using System.Diagnostics;

namespace TanksServer.Graphics;

internal sealed class GamePixelGrid : IEnumerable<GamePixel>
{
    public ulong Width { get; }
    public ulong Height { get; }

    private readonly GamePixel[,] _pixels;

    public GamePixelGrid(ulong width, ulong height)
    {
        Width = width;
        Height = height;

        _pixels = new GamePixel[width, height];
        for (ulong y = 0; y < height; y++)
            for (ulong x = 0; x < width; x++)
                this[x, y] = new GamePixel();
    }

    public GamePixel this[ulong x, ulong y]
    {
        get
        {
            Debug.Assert(y * Width + x < (ulong)_pixels.Length);
            return _pixels[x, y];
        }
        set => _pixels[x, y] = value;
    }

    public void Clear()
    {
        foreach (var pixel in _pixels)
            pixel.Clear();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<GamePixel> GetEnumerator()
    {
        for (ulong y = 0; y < Height; y++)
            for (ulong x = 0; x < Width; x++)
                yield return this[x, y];
    }
}
