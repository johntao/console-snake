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
            _rdr.UpdatePoint(2 + _yOffset, (value + "").PadLeft(5));
        }
    }
    //| Lvl | Speed | Len | Time | HighScore |
    //| Speed | Len | Time | HighScore |
    private int _currentLength;
    public int CurrentLength
    {
        get => _currentLength;
        set
        {
            _currentLength = value;
            _rdr.UpdatePoint(10 + _yOffset, (value + "").PadLeft(3));
        }
    }
    private int _level;
    public int Level
    {
        get => _level;
        set
        {
            _level = value;
            if (_opt.UseLevel) _rdr.UpdatePoint(2, (value + "").PadLeft(3));
        }
    }
    public Stopwatch Sw { get; }
    readonly GameplayLevel _lvl;
    readonly GameplayMotor _motor;
    readonly Gameplay _opt;
    readonly Renderer _rdr;
    readonly int _yOffset;
    public Dashboard(IOptions<Config> cfg, Renderer rdr)
    {
        _rdr = rdr;
        Sw = new Stopwatch();
        (_opt, _lvl, _motor, _, _) = cfg.Value;
        if (_opt.UseLevel) _yOffset = 7;
        Level = _lvl.DefaultLevel;
    }

    internal void Reset()
    {
        Level = _lvl.DefaultLevel;
        SetSpeedDisplay(1);
    }

    internal void SetSpeedDisplay(double speedLevel)
    {
        SpeedDisplay = $"{1 / ((double)_motor.StartingSpeed / 1000):0.#}x{speedLevel}";
    }
}
