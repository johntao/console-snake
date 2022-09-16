using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.Extensions.Configuration;

static class Config
{
    internal readonly static int Bound, StartLen, Speed, Threshold, DefLevel;
    internal readonly static char[] Tiles = new char[Enum.GetValues(typeof(TileType)).Length];
    internal readonly static double[] Levels;
    internal readonly static char Wall;
    internal readonly static bool CanSpeedUp, CanHitWall, UseBorder, UseSpeed, UseAcceleration, UseLevel;
    static Config()
    {
        var path = Path.Join(Directory.GetCurrentDirectory(), "appsettings.json");
        if (!File.Exists(path))
        {
            using var fs = File.Create(path);
            JsonSerializer.Serialize<ConfigPOCO>(fs, new(), new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Latin1Supplement, UnicodeRanges.GeometricShapes),
                WriteIndented = true
            });
        }
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        Bound = int.Parse(config[nameof(Bound)]);
        StartLen = int.Parse(config[nameof(StartLen)]);
        Speed = int.Parse(config[nameof(Speed)]);
        CanSpeedUp = bool.Parse(config[nameof(CanSpeedUp)]);
        CanHitWall = bool.Parse(config[nameof(CanHitWall)]);
        UseBorder = bool.Parse(config[nameof(UseBorder)]);
        UseSpeed = bool.Parse(config[nameof(UseSpeed)]);
        UseLevel = bool.Parse(config[nameof(UseLevel)]);
        UseAcceleration = bool.Parse(config[nameof(UseAcceleration)]);
        var set = config.GetSection("TileSet");
        Tiles[(int)TileType.None] = char.Parse(set[nameof(TileType.None)]);
        Tiles[(int)TileType.Body] = char.Parse(set[nameof(TileType.Body)]);
        Tiles[(int)TileType.Head] = char.Parse(set[nameof(TileType.Head)]);
        Tiles[(int)TileType.Crate] = char.Parse(set[nameof(TileType.Crate)]);
        Wall = char.Parse(set[nameof(Wall)]);
        var level = config.GetSection("Level");
        DefLevel = int.Parse(level[nameof(DefLevel)]);
        Threshold = int.Parse(level[nameof(Threshold)]);
        Levels = Array.ConvertAll(level.GetSection(nameof(Levels)).GetChildren().Select(q => q.Value).ToArray(), double.Parse);
    }
}