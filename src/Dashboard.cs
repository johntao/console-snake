using System.Diagnostics;
using Microsoft.Extensions.Options;

class Dashboard
{
    private string _speedDisplay = string.Empty;
    public string SpeedDisplay
    {
        get => _speedDisplay;
        set
        {
            _speedDisplay = value;
            _divBoard.PrintPartial(DashboardColumn.Speed);
        }
    }
    //| Lvl | Speed | Len | Time | HighScore |
    //| Speed | Len | Time | HighScore |
    private int _currentLength;
    public int CurrentSnakeLength
    {
        get => _currentLength;
        set
        {
            _currentLength = value;
            _divBoard.PrintPartial(DashboardColumn.Len);
        }
    }
    private int _level;
    public int Level
    {
        get => _level;
        set
        {
            _level = value;
            if (_optGamplay.UseLevel) _divBoard.PrintPartial(DashboardColumn.Level);
        }
    }
    public Stopwatch Stopwatch { get; }
    readonly GameplayLevel _lvl;
    readonly GameplayMotor _motor;
    readonly Gameplay _optGamplay;
    readonly Visual _optVisual;
    readonly DivDashboard _divBoard;
    readonly HighScore _highscore;
    internal readonly System.Timers.Timer BoardTimer;
    public Dashboard(IOptions<Config> cfgRoot, DivDashboard div, HighScore highScore)
    {
        BoardTimer = new System.Timers.Timer
        {
            Enabled = true,
            Interval = 1000,
        };
        BoardTimer.Elapsed += BoardTimerTick;
        _highscore = highScore;
        _divBoard = div;
        Stopwatch = new Stopwatch();
        (_optGamplay, _lvl, _motor, _optVisual, _) = cfgRoot.Value;
        InitializeDivDashboard();
        Level = _lvl.DefaultLevel;
    }
    private void InitializeDivDashboard()
    {
        if (_optGamplay.UseLevel) _divBoard.Items.Add(new Span { Name = "Lvl", Value = () => Level + "" });
        _divBoard.Items.AddRange(new[]
        {
            new Span {
                Name = "Speed ",
                Value = () => SpeedDisplay,
            },
            new Span {
                Name = "Len",
                Value = () => CurrentSnakeLength + "",
            },
            new Span {
                Name = "Time ",
                Value = () => Stopwatch.Elapsed.ToString("mm\\:ss"),
            },
            new Span {
                Name = "HighScore",
                Value = () => _highscore.ToString(),
            },
        });
    }
    void BoardTimerTick(object? sender, EventArgs e) => _divBoard.PrintPartial(DashboardColumn.Time);
    internal void ResetAndReRenderAll(IMap map)
    {
        _highscore.SetHighScore(this);
        Level = _lvl.DefaultLevel;
        SetSpeedDisplay(1);
        Console.Clear();
        if (_optVisual.UseDashboard) _divBoard.Print();
        map.DivMap.Print(map);
    }
    internal void SetSpeedDisplay(double speedLevel)
    {
        double speedReciprocal = 1 / ((double)_motor.StartingSpeed / 1000);
        SpeedDisplay = $"{speedReciprocal:0}x{speedLevel:0.0}";
    }
    internal void LevelUp(System.Timers.Timer timer)
    {
        if (!_optGamplay.UseLevel) return;
        if (HasHitLevelCap() || !HasHitThreshold())
            return;
        var speedLevel = _lvl.Levels[++Level];
        bool canMoveByTimer = (_motor.MotorEnum & MotorEnum.ByTimer) > 0;
        if (canMoveByTimer && _motor.UseLevelAccelerator)
        {
            SetSpeedDisplay(speedLevel);
            timer.Interval = _motor.StartingSpeed / speedLevel;
        }
        bool HasHitThreshold() => (CurrentSnakeLength % _lvl.Threshold) == 0;
        bool HasHitLevelCap() => (Level + 1) >= _lvl.Levels.Count;
    }
}
