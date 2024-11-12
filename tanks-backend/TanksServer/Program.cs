using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using TanksServer.GameLogic;
using TanksServer.Graphics;
using TanksServer.Interactivity;

namespace TanksServer;

public static class Program
{
    [RequiresUnreferencedCode("Calls Endpoints.Map")]
    [RequiresDynamicCode("Calls Endpoints.Map")]
    public static async Task Main(string[] args)
    {
        var app = Configure(args);

        var clientFileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "client"));

        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = clientFileProvider });
        app.UseStaticFiles(new StaticFileOptions { FileProvider = clientFileProvider });

        app.Services.GetRequiredService<Endpoints>().Map(app);

        await app.RunAsync();
    }

    [RequiresUnreferencedCode("Calls Microsoft.Extensions.DependencyInjection.OptionsConfigurationServiceCollectionExtensions.Configure<TOptions>(IConfiguration)")]
    [RequiresDynamicCode("Calls Microsoft.Extensions.DependencyInjection.OptionsConfigurationServiceCollectionExtensions.Configure<TOptions>(IConfiguration)")]
    private static WebApplication Configure(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        builder.Logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.IncludeScopes = true;
            options.TimestampFormat = "HH:mm:ss ";
        });

        builder.Services.AddCors(options => options
            .AddDefaultPolicy(policy => policy
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowAnyOrigin())
        );

        builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, new AppSerializerContext()));

        builder.Services.AddHttpLogging(_ => { });

        var healthCheckBuilder = builder.Services.AddHealthChecks();
        healthCheckBuilder.AddCheck<UpdatesPerSecondCounter>("updates check");

        builder.Services.AddSingleton<MapService>();
        builder.Services.AddSingleton<MapEntityManager>();
        builder.Services.AddSingleton<ControlsServer>();
        builder.Services.AddSingleton<PlayerServer>();
        builder.Services.AddSingleton<ClientScreenServer>();
        builder.Services.AddSingleton<TankSpawnQueue>();
        builder.Services.AddSingleton<Endpoints>();
        builder.Services.AddSingleton<BufferPool>();
        builder.Services.AddSingleton<EmptyTileFinder>();
        builder.Services.AddSingleton<ChangeToRequestedMap>();
        builder.Services.AddSingleton<UpdatesPerSecondCounter>();

        builder.Services.AddHostedService<GameTickWorker>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<ControlsServer>());
        builder.Services.AddHostedService(sp => sp.GetRequiredService<ClientScreenServer>());

        builder.Services.AddSingleton<ITickStep, ChangeToRequestedMap>(sp =>
            sp.GetRequiredService<ChangeToRequestedMap>());
        builder.Services.AddSingleton<ITickStep, MoveBullets>();
        builder.Services.AddSingleton<ITickStep, CollideBullets>();
        builder.Services.AddSingleton<ITickStep, RotateTanks>();
        builder.Services.AddSingleton<ITickStep, MoveTanks>();
        builder.Services.AddSingleton<ITickStep, ShootFromTanks>();
        builder.Services.AddSingleton<ITickStep, CollectPowerUp>();
        builder.Services.AddSingleton<ITickStep>(sp => sp.GetRequiredService<TankSpawnQueue>());
        builder.Services.AddSingleton<ITickStep, SpawnPowerUp>();
        builder.Services.AddSingleton<ITickStep, GeneratePixelsTickStep>();
        builder.Services.AddSingleton<ITickStep, PlayerServer>(sp => sp.GetRequiredService<PlayerServer>());
        builder.Services.AddSingleton<ITickStep, UpdatesPerSecondCounter>(sp =>
            sp.GetRequiredService<UpdatesPerSecondCounter>());

        builder.Services.AddSingleton<IDrawStep, DrawMapStep>();
        builder.Services.AddSingleton<IDrawStep, DrawPowerUpsStep>();
        builder.Services.AddSingleton<IDrawStep, DrawTanksStep>();
        builder.Services.AddSingleton<IDrawStep, DrawBulletsStep>();

        builder.Services.AddSingleton<IFrameConsumer, ClientScreenServer>(sp =>
            sp.GetRequiredService<ClientScreenServer>());

        builder.Services.Configure<GameRules>(builder.Configuration.GetSection("GameRules"));
        builder.Services.Configure<HostConfiguration>(builder.Configuration.GetSection("Host"));
        builder.Services.Configure<DisplayConfiguration>(builder.Configuration.GetSection("ServicePointDisplay"));

        builder.Services.AddSingleton<IFrameConsumer, SendToServicePointDisplay>();
        builder.Services.AddSingleton<Connection>(sp =>
        {
            var config = sp.GetRequiredService<IOptions<DisplayConfiguration>>().Value;
            var connection = new Connection($"{config.Hostname}:{config.Port}");
            return connection;
        });

        var app = builder.Build();

        app.UseCors();
        app.UseWebSockets();
        app.UseHttpLogging();

        return app;
    }
}
