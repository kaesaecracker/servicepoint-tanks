using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace TanksServer.Graphics;

internal sealed class Sprite(bool?[,] data)
{
    public static Sprite FromImageFile(string filePath)
    {
        using var image = Image.Load<Rgba32>(filePath);
        var data = new bool?[image.Width, image.Height];

        var whitePixel = new Rgba32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
        for (var y = 0; y < image.Height; y++)
            for (var x = 0; x < image.Width; x++)
            {
                var pixelValue = image[x, y];
                data[x, y] = pixelValue.A == 0
                    ? null
                    : pixelValue == whitePixel;
            }

        return new Sprite(data);
    }

    public bool? this[int x, int y] => data[x, y];

    public int Width => data.GetLength(0);

    public int Height => data.GetLength(1);

    public bool[,] ToBoolArray()
    {
        var result = new bool[Width, Height];

        for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
                result[x, y] = this[x, y] ?? false;

        return result;
    }
}
