using System.Diagnostics;
using System.Text.Json.Serialization;
using TanksServer.GameLogic;

namespace TanksServer.Models;

internal sealed class Tank(Player owner, FloatPosition position) : IMapEntity
{
    private double _rotation;

    [JsonIgnore] public Player Owner { get; } = owner;

    [JsonIgnore]
    public double Rotation
    {
        get => _rotation;
        set
        {
            var newRotation = (value % 1d + 1d) % 1d;
            Debug.Assert(newRotation is >= 0 and < 1);
            _rotation = newRotation;
        }
    }

    public DateTime NextShotAfter { get; set; }

    public bool Moving { get; set; }

    [JsonIgnore] public FloatPosition Position { get; set; } = position;

    public PixelPosition PixelPosition => Position.ToPixelPosition();

    [JsonIgnore] public PixelBounds Bounds => Position.GetBoundsForCenter(MapService.TileSize);

    public int Orientation => (int)Math.Round(Rotation * 16) % 16;

    public int UsedBullets { get; set; }

    public int MaxBullets { get; set; }

    public DateTime ReloadingUntil { get; set; }

    public required BulletStats BulletStats { get; set; }
}

internal sealed record class BulletStats(double Speed, double Acceleration, bool Explosive, bool Smart);
