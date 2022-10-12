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
            _renderer.RendorDashboardPartial(2 + _yOffset, (value + "").PadLeft(6));
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
            _renderer.RendorDashboardPartial(11 + _yOffset, (value + "").PadLeft(3));
        }
    }
    private int _level;
    public int Level
    {
        get => _level;
        set
        {
            _level = value;
            if (_optGamplay.UseLevel) _renderer.RendorDashboardPartial(2, (value + "").PadLeft(3));
        }
    }
    public Stopwatch Stopwatch { get; }
    readonly GameplayLevel _lvl;
    readonly GameplayMotor _motor;
    readonly Gameplay _optGamplay;
    readonly Renderer _renderer;
    readonly int _yOffset;
    readonly HighScore _highscore;
    internal readonly System.Timers.Timer BoardTimer;
    // readonly Visual _visual;
    public Dashboard(IOptions<Config> cfgRoot, Renderer renderer, HighScore highScore)
    {
        BoardTimer = new System.Timers.Timer
        {
            Enabled = true,
            Interval = 1000,
        };
        BoardTimer.Elapsed += BoardTimerTick;
        _highscore = highScore;
        _renderer = renderer;
        Stopwatch = new Stopwatch();
        (_optGamplay, _lvl, _motor, _, _) = cfgRoot.Value;
        if (_optGamplay.UseLevel) _yOffset = 6;
        Level = _lvl.DefaultLevel;
    }
    void BoardTimerTick(object? sender, EventArgs e)
        => _renderer.RendorDashboardPartial(17 + _yOffset, Stopwatch.Elapsed.ToString("mm\\:ss"));
    internal void ResetAndReRenderAll(IMap map)
    {
        _highscore.SetHighScore(this);
        Level = _lvl.DefaultLevel;
        SetSpeedDisplay(1);
        _renderer.ClearAll(map, _highscore, this);
    }

    internal void SetSpeedDisplay(double speedLevel)
    {
        double speedReciprocal = 1 / ((double)_motor.StartingSpeed / 1000);
        SpeedDisplay = $"{speedReciprocal:0}x{speedLevel:0.0}";
    }

    internal void LevelUp(System.Timers.Timer timer)
    {
        if (!_optGamplay.UseLevel) return;
        if (!HasHitThreshold() || !HasHitLevelCap())
            return;
        var speedLevel = _lvl.Levels[++Level];
        bool canMoveByTimer = (_motor.MotorEnum & MotorEnum.ByTimer) > 0;
        if (canMoveByTimer && _motor.UseLevelAccelerator)
        {
            SetSpeedDisplay(speedLevel);
            timer.Interval = _motor.StartingSpeed / speedLevel;
        }

        bool HasHitThreshold() => (CurrentSnakeLength % _lvl.Threshold) == 0;
        bool HasHitLevelCap() => (Level + 1) < _lvl.Levels.Count;
    }
}
