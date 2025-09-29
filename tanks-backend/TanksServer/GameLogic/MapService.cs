using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using TanksServer.Graphics;

namespace TanksServer.GameLogic;

internal sealed class MapService
{
    public static readonly ulong TilesPerRow = 44;
    public static readonly ulong TilesPerColumn = ServicePointConstants.TileHeight;
    public static readonly ulong TileSize = ServicePointConstants.TileSize;
    public static readonly ulong PixelsPerRow = TilesPerRow * TileSize;
    public static readonly ulong PixelsPerColumn = TilesPerColumn * TileSize;

    private readonly ConcurrentDictionary<string, MapPrototype> _mapPrototypes = new();
    private readonly ConcurrentDictionary<string, Bitmap> _mapPreviews = new();

    public IEnumerable<string> MapNames => _mapPrototypes.Keys;

    public Map Current { get; private set; }

    public MapService()
    {
        var dir = Path.Combine(Program.AssetsDir, "maps");
        foreach (var file in Directory.EnumerateFiles(dir, "*.txt"))
            LoadMapString(file);
        foreach (var file in Directory.EnumerateFiles(dir, "*.png"))
            LoadMapPng(file);
        Current = GetRandomMap();
    }

    public bool TryGetPrototype(string name, [MaybeNullWhen(false)] out MapPrototype map)
        => _mapPrototypes.TryGetValue(name, out map);

    public void SwitchTo(MapPrototype prototype) => Current = prototype.CreateInstance();

    public bool TryGetPreview(string name, [MaybeNullWhen(false)] out Bitmap pixelGrid)
    {
        if (_mapPreviews.TryGetValue(name, out pixelGrid))
            return true; // already generated
        if (!_mapPrototypes.TryGetValue(name, out var prototype))
            return false; // name not found

        pixelGrid = new Bitmap(PixelsPerRow, PixelsPerColumn);
        DrawMapStep.Draw(pixelGrid, prototype.CreateInstance());

        _mapPreviews.TryAdd(name, pixelGrid); // another thread may have added the map already
        return true; // new preview
    }

    private void LoadMapPng(string file)
    {
        var name = MapNameFromFilePath(file);
        var prototype = new SpriteMapPrototype(name, Sprite.FromImageFile(file));
        var added = _mapPrototypes.TryAdd(name, prototype);
        Debug.Assert(added);
    }

    private void LoadMapString(string file)
    {
        var name = MapNameFromFilePath(file);
        var map = File.ReadAllText(file).ReplaceLineEndings(string.Empty).Trim();
        var prototype = new TextMapPrototype(name, map);
        _mapPrototypes.TryAdd(name, prototype);
    }

    private static string MapNameFromFilePath(string filePath) => Path.GetFileName(filePath).ToUpperInvariant();

    private Map GetRandomMap()
    {
        var chosenMapIndex = Random.Shared.Next(_mapPrototypes.Count);
        var chosenMapName = _mapPrototypes.Keys.Skip(chosenMapIndex).First();
        return _mapPrototypes[chosenMapName].CreateInstance();
    }
}
