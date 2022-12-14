using Microsoft.Extensions.Configuration;
using Tomlyn.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tomlyn;
await Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(q =>
    {
        var path = Path.Join(Directory.GetCurrentDirectory(), "appsettings.toml");
        if (!File.Exists(path))
        {
            TomlModelOptions opt = new() { ConvertPropertyName = q => q, };
            var toml = Toml.FromModel(new Config(), opt);
            File.WriteAllText(path, TomlTableFormatHelper.Do(toml));
        }
        q.SetBasePath(Directory.GetCurrentDirectory())
                    .AddTomlFile("appsettings.toml", optional: true, reloadOnChange: true);
    })
    .ConfigureLogging(q => q.ClearProviders())
    .ConfigureServices((q, s) => s
        .AddHostedService<Snake>()
        .Configure<HostOptions>(q => q.ShutdownTimeout = TimeSpan.FromMilliseconds(200))
        .AddSingleton<HighScore>()
        .AddSingleton<DivDashboard>()
        .AddSingleton<DivMap>()
        .AddSingleton<Dashboard>()
        .AddSingleton<IMap, MapJaggedArray>()
        .Configure<Config>(q.Configuration)
        .Configure<Config>(q =>
        {
            q.GameplayMotor.MotorEnum = (MotorEnum)q.GameplayMotor.MotorType;
            if (q.GameplayLevel.Levels.Count == 0) q.GameplayLevel.Levels.AddRange(new[] { 1.0, 1.5, 2.0, 2.5, 3.0, 3.5 });
        })
        )
    .RunConsoleAsync();


