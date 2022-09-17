using Microsoft.Extensions.Configuration;
using Tomlyn.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tomlyn;

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(q =>
    {
        var path = Path.Join(Directory.GetCurrentDirectory(), "appsettings.toml");
        if (!File.Exists(path))
        {
            TomlModelOptions opt = new() { ConvertPropertyName = q => q, };
            var toml = Toml.FromModel(new ConfigPOCO(), opt);
            File.WriteAllText(path, TomlTableFormatHelper.Do(toml));
        }
        q.SetBasePath(Directory.GetCurrentDirectory())
                    .AddTomlFile("appsettings.toml", optional: true, reloadOnChange: true);
    })
    .ConfigureServices((q, s) => s
        .AddSingleton<Snake>()
        .Configure<ConfigPOCO>(q.Configuration)
        )
    .Build();
var game = host.Services.GetRequiredService<Snake>();
game.Start();

// await host.RunAsync();
