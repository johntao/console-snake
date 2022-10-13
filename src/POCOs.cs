
enum SpeedDirection { None, Up, Down, Left, Right }

enum TileType { None, Body, Head, Food }

enum GameResult { None, Loss, Win }

public enum MotorEnum { None, ByKey, ByTimer, ByTimerAndKey, }

public class Config
{
    public Gameplay Gameplay { get; set; } = new();
    public GameplayLevel GameplayLevel { get; set; } = new();
    public GameplayMotor GameplayMotor { get; set; } = new();
    public Visual Visual { get; set; } = new();
    public VisualMap VisualMap { get; set; } = new();
    public void Deconstruct(out Gameplay gameplay, out GameplayLevel gameplayLevel, out GameplayMotor gameplayMotor, out Visual visual, out VisualMap visualMap) =>
    (gameplay, gameplayLevel, gameplayMotor, visual, visualMap) =
    (Gameplay, GameplayLevel, GameplayMotor, Visual, VisualMap);
}

public class Gameplay
{
    public int StartingLength { get; set; } = 1;
    public bool CanPassWall { get; set; } = true;
    public bool UseLevel { get; set; } = false;
}
public class GameplayMotor
{
    public byte MotorType { get; set; } = (byte)MotorEnum.ByTimerAndKey;
    public MotorEnum MotorEnum;
    public int StartingSpeed { get; set; } = 1000;
    public bool UseLevelAccelerator { get; set; } = false;
}
public class GameplayLevel
{
    public List<double> Levels { get; set; } = new();
    public int Threshold { get; set; } = 5;
    public int DefaultLevel { get; set; } = 0;
}
public class Visual
{
    public bool UseBorder { get; set; } = false;
    public bool UseDashboard { get; set; } = true;
}
public class VisualMap
{
    public int SideLength { get; set; } = 10;
    public string None { get; set; } = "·";
    public string Body { get; set; } = "○";
    public string Head { get; set; } = "●";
    public string Crate { get; set; } = "▲";
    public string Wall { get; set; } = "▣";
}