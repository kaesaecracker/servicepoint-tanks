using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace TanksServer.Graphics;

internal sealed class SpriteSheet(Sprite[,] sprites)
{
    public static SpriteSheet FromImageFile(string filePath, int spriteWidth, int spriteHeight)
    {
        using var image = Image.Load<Rgba32>(filePath);

        var spritesPerRow = image.Width / spriteWidth;
        if (image.Width % spriteWidth != 0)
            throw new InvalidOperationException("invalid sprite dimensions");

        var spritesPerColumn = image.Height / spriteHeight;
        if (image.Height % spriteHeight != 0)
            throw new InvalidOperationException("invalid sprite dimensions");

        var sprites = new Sprite[spritesPerRow, spritesPerColumn];

        var whitePixel = new Rgba32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
        for (var spriteY = 0; spriteY < spritesPerColumn; spriteY++)
            for (var spriteX = 0; spriteX < spritesPerRow; spriteX++)
            {
                var data = new bool?[spriteWidth, spriteHeight];
                for (var dy = 0; dy < spriteHeight; dy++)
                    for (var dx = 0; dx < spriteWidth; dx++)
                    {
                        var x = spriteX * spriteWidth + dx;
                        var y = spriteY * spriteHeight + dy;

                        var pixelValue = image[x, y];
                        data[dx, dy] = pixelValue.A == 0
                            ? null
                            : pixelValue == whitePixel;
                    }

                sprites[spriteX, spriteY] = new Sprite(data);
            }

        return new SpriteSheet(sprites);
    }

    public Sprite this[int x, int y] => sprites[x, y];

    public Sprite this[int index] => sprites[index % sprites.GetLength(1), index / sprites.GetLength(1)];
}
