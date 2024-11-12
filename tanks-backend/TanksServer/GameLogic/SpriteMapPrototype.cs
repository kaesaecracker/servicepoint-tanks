using System.IO;
using TanksServer.Graphics;

namespace TanksServer.GameLogic;

internal sealed class SpriteMapPrototype : MapPrototype
{
    public override string Name { get; }

    public Sprite Sprite { get; }

    public SpriteMapPrototype(string name, Sprite sprite)
    {
        if ((ulong)sprite.Width != MapService.PixelsPerRow || (ulong)sprite.Height != MapService.PixelsPerColumn)
            throw new FileLoadException($"invalid image size in file {Name}");

        Name = name;
        Sprite = sprite;
    }

    public override Map CreateInstance() => new(Name, Sprite.ToBoolArray());
}
