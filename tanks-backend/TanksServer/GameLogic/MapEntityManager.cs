namespace TanksServer.GameLogic;

internal sealed class MapEntityManager(
    ILogger<MapEntityManager> logger,
    IOptions<GameRules> options
)
{
    private readonly GameRules _rules = options.Value;
    private readonly HashSet<Bullet> _bullets = [];
    private readonly HashSet<PowerUp> _powerUps = [];
    private readonly Dictionary<Player, Tank> _playerTanks = [];
    private readonly TimeSpan _bulletTimeout = TimeSpan.FromMilliseconds(options.Value.BulletTimeoutMs);

    public IEnumerable<Bullet> Bullets => _bullets;
    public IEnumerable<Tank> Tanks => _playerTanks.Values;
    public IEnumerable<PowerUp> PowerUps => _powerUps;

    public void SpawnBullet(Player tankOwner, FloatPosition position, double rotation, BulletStats stats)
    {
        _bullets.Add(new Bullet
        {
            Owner = tankOwner,
            Position = position,
            Rotation = rotation,
            Timeout = DateTime.Now + _bulletTimeout,
            OwnerCollisionAfter = DateTime.Now + TimeSpan.FromSeconds(1),
            Speed = _rules.BulletSpeed,
            Stats = stats
        });
    }

    public void RemoveWhere(Predicate<Bullet> predicate) => _bullets.RemoveWhere(predicate);

    public void SpawnTank(Player player, FloatPosition position)
    {
        var tank = new Tank(player, position)
        {
            Rotation = Random.Shared.NextDouble(),
            MaxBullets = _rules.MagazineSize,
            BulletStats = new BulletStats(_rules.BulletSpeed, 0, false, false)
        };
        _playerTanks[player] = tank;
        logger.LogInformation("Tank added for player {}", player.Name);
    }

    public void SpawnPowerUp(FloatPosition position, PowerUpType type)
    {
        var powerUp = new PowerUp
        {
            Position = position,
            Type = type
        };
        _powerUps.Add(powerUp);
    }

    public void RemoveWhere(Predicate<PowerUp> predicate) => _powerUps.RemoveWhere(predicate);

    public void Remove(Tank tank)
    {
        logger.LogInformation("Tank removed for player {}", tank.Owner.Name);
        _playerTanks.Remove(tank.Owner);
    }

    public Tank? GetCurrentTankOfPlayer(Player player) => _playerTanks.GetValueOrDefault(player);

    public IEnumerable<IMapEntity> AllEntities => Bullets
        .Cast<IMapEntity>()
        .Concat(Tanks)
        .Concat(PowerUps);
}
