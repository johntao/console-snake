
enum SpeedDirection { None, Up, Down, Left, Right }

enum TileType { None, Body, Head, Crate }

public class ConfigPOCO
{
    public int Bound { get; set; } = 3;
    public int StartLen { get; set; } = 1;
    public int Speed { get; set; } = 1000;
    public bool CanHitWall { get; set; } = true;
    public bool UseBorder { get; set; } = false;
    public bool CanSpeedUp { get; set; } = true;
    public bool UseSpeed { get; set; } = false;
    public bool UseAcceleration { get; set; } = false;
    public bool UseLevel { get; set; } = false;
    public Level Level { get; set; } = new();
    public TileSet TileSet { get; set; } = new();
}

public class Level
{
    public List<double> Levels { get; set; } = new() { 1.0, 1.5, 2.0, 2.5, 3.0, 3.5 };
    public int Threshold { get; set; } = 5;
    public int DefLevel { get; set; } = 0;
}

public class TileSet
{
    public char None { get; set; } = '·';
    public char Body { get; set; } = '○';
    public char Head { get; set; } = '●';
    public char Crate { get; set; } = '▲';
    public char Wall { get; set; } = '▣';
}
