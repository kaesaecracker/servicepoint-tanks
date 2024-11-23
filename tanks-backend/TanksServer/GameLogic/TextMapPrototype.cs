namespace TanksServer.GameLogic;

internal sealed class TextMapPrototype : MapPrototype
{
    public override string Name { get; }

    public string Text { get; }

    public TextMapPrototype(string name, string text)
    {
        if ((ulong)text.Length != MapService.TilesPerColumn * MapService.TilesPerRow)
            throw new ArgumentException($"cannot load map {name}: invalid length");
        Name = name;
        Text = text;
    }


    public override Map CreateInstance()
    {
        var walls = new bool[MapService.PixelsPerRow, MapService.PixelsPerColumn];

        for (ulong tileX = 0; tileX < MapService.TilesPerRow; tileX++)
            for (ulong tileY = 0; tileY < MapService.TilesPerColumn; tileY++)
            {
                var tile = new TilePosition(tileX, tileY);
                if (Text[(int)(tileX + tileY * MapService.TilesPerRow)] != '#')
                    continue;

                for (byte pixelInTileX = 0; pixelInTileX < MapService.TileSize; pixelInTileX++)
                    for (byte pixelInTileY = 0; pixelInTileY < MapService.TileSize; pixelInTileY++)
                    {
                        var (x, y) = tile.ToPixelPosition().GetPixelRelative(pixelInTileX, pixelInTileY);
                        walls[x, y] = true;
                    }
            }

        return new Map(Name, walls);
    }
}
