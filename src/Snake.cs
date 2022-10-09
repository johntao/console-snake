using System.Collections.Concurrent;
using System.Diagnostics;
using System.Timers;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.HighPerformance;

class HighScore
{
    public int MaxLength;
    public string MinTime = "99:99";
    readonly int _startLength;
    readonly Dashboard _db;
    readonly Gameplay _opt;
    readonly Renderer _rdr;
    readonly int _yOffset;
    public HighScore(IOptions<Config> cfg, Dashboard db, Renderer rdr)
    {
        _rdr = rdr;
        (_opt, _, _, _, _) = cfg.Value;
        if (_opt.UseLevel) _yOffset = 7;
        _db = db;
        _startLength = cfg.Value.Gameplay.StartingLength;
    }
    //| Lvl | Speed | Len | Time | HighScore |
    private string _highScoreText = string.Empty;
    public string HighScoreText
    {
        get => _highScoreText;
        private set
        {
            _highScoreText = value;
            if (_opt.UseLevel) _rdr.UpdatePoint(23 + _yOffset, (value + "").PadLeft(9));
        }
    }
    public void SetHighScore()
    {
        _db.Sw.Stop();
        var time = _db.Sw.Elapsed.ToString("mm\\:ss");
        _db.Sw.Reset();
        bool isFirstRun = _db.CurrentLength == 0;
        if (isFirstRun || _db.CurrentLength < MaxLength)
        {
            _db.CurrentLength = _startLength;
        }
        else if (_db.CurrentLength == MaxLength)
        {
            _db.CurrentLength = _startLength;
            if (time.CompareTo(MinTime) < 0)
                MinTime = time;
        }
        else if (_db.CurrentLength > MaxLength)
        {
            MaxLength = _db.CurrentLength;
            _db.CurrentLength = _startLength;
            MinTime = time;
        }
        HighScoreText = $"{MaxLength}@{MinTime}";
    }
    public override string ToString() => HighScoreText;
}
interface IMap
{
    //__|y_____
    // x| → →
    // ↓|
    // ↓|
    public int TopBound { get; } // zero-based
    public int BottomBound { get; } // zero-based
    public int LeftBound { get; } // zero-based
    public int RightBound { get; } // zero-based
    public int Length { get; }
    public TileType this[int x, int y] { get; set; }
    public TileType this[(int x, int y) point] { get; set; }
    public Renderer Renderer { get; }
    public ReadOnlySpan<TileType> this[int x] { get; }
    public void Clear();
}
class Map2dArray : IMap
{
    private readonly TileType[,] _map;
    public int TopBound { get; } // zero-based
    public int BottomBound { get; } // zero-based
    public int LeftBound { get; } // zero-based
    public int RightBound { get; } // zero-based
    public int Length { get; }
    public bool IsStart { get; set; }
    public Renderer Renderer { get; }
    public Map2dArray(IOptions<Config> cfg, Renderer renderer)
    {
        var opt = cfg.Value.VisualMap;
        _map = new TileType[opt.SideLength, opt.SideLength];
        TopBound = 0;
        BottomBound = opt.SideLength - 1;
        LeftBound = 0;
        RightBound = opt.SideLength - 1;
        Length = opt.SideLength * opt.SideLength - 1;
        Renderer = renderer;
    }
    public TileType this[int x, int y]
    {
        get => _map[x, y];
        set
        {
            _map[x, y] = value;
            Renderer.UpdatePoint(x, y, value);
        }
    }
    public ReadOnlySpan<TileType> this[int x]
    {
        get
        {
            // 0 1 2 | 0*3
            // 3 4 5 | 1*3
            // 6 7 8 | 2*3
            var width = RightBound + 1;
            var rtn = _map.AsSpan()[(x * width)..width];
            return rtn;
        }
    }
    public TileType this[(int x, int y) point]
    {
        get => _map[point.x, point.y];
        set
        {
            _map[point.x, point.y] = value;
            Renderer.UpdatePoint(point.x, point.y, value);
        }
    }
    public void Clear()
    {
        Array.Clear(_map, 0, _map.Length);
    }
}
public class RenderArgs : EventArgs
{
    public (int X, int Y) Position { get; set; }
    internal TileType TileType { get; set; }
}
class MapJaggedArray : IMap
{
    public bool IsStart { get; set; }
    private readonly TileType[][] _map;
    public int TopBound { get; } // zero-based
    public int BottomBound { get; } // zero-based
    public int LeftBound { get; } // zero-based
    public int RightBound { get; } // zero-based
    public int Length { get; }
    public Renderer Renderer { get; }
    public MapJaggedArray(IOptions<Config> cfg, Renderer renderer)
    {
        var opt = cfg.Value.VisualMap;
        _map = new TileType[opt.SideLength][];
        for (int i = 0; i < opt.SideLength; i++)
            _map[i] = new TileType[opt.SideLength];
        TopBound = 0;
        BottomBound = opt.SideLength - 1;
        LeftBound = 0;
        RightBound = opt.SideLength - 1;
        Length = opt.SideLength * opt.SideLength - 1;
        Renderer = renderer;
    }
    public TileType this[int x, int y]
    {
        get => _map[x][y];
        set
        {
            _map[x][y] = value;
            Renderer.UpdatePoint(x, y, value);
        }
    }
    public ReadOnlySpan<TileType> this[int x] => _map.AsSpan(x)[0];
    public TileType this[(int x, int y) point]
    {
        get => _map[point.x][point.y];
        set
        {
            _map[point.x][point.y] = value;
            Renderer.UpdatePoint(point.x, point.y, value);
        }
    }
    public void Clear()
    {
        foreach (var item in _map)
            Array.Clear(item, 0, item.Length);
    }
}
class Renderer
{
    // public event EventHandler? Render;
    readonly VisualMap _mapOpts;
    readonly Visual _visual;
    readonly Gameplay _opt;
    // readonly HighScore _hs;
    readonly int _xOffset;
    // readonly Dashboard _db;
    public Renderer(IOptions<Config> cfg) //, Dashboard db, HighScore hs, 
    {
        // _db = db;
        (_opt, _, _, _visual, _mapOpts) = cfg.Value;
        if (_visual.UseDashboard) _xOffset = 4;
        // Render += (sender, args) =>
        // {
        // (int x, int y) = args.Position;
        // Console.SetCursorPosition(y, x);
        // Console.Write(args.Character);
        // };
    }
    public void UpdatePoint(int x, int y, TileType value)
    {
        Console.SetCursorPosition(y * 2, x + _xOffset);
        Console.Write(TileToString(value));
    }

    private string TileToString(TileType tile) => tile switch
    {
        TileType.Crate => _mapOpts.Crate,
        TileType.Head => _mapOpts.Head,
        TileType.Body => _mapOpts.Body,
        _ => _mapOpts.None
    };

    public void CleanMap(IMap map, HighScore _hs, Dashboard _db)
    {
        Console.Clear();
        if (_visual.UseDashboard) RendorDashboard(_hs, _db);
        for (int i = 0; i <= map.BottomBound; i++)
        {
            var arr = map[i].ToArray().Select(q => TileToString(q));
            Console.WriteLine(string.Join(' ', arr));
        }
        Console.WriteLine();
    }

    private void RendorDashboard(HighScore _hs, Dashboard _db)
    {
        //Level, Speed, Length, Time, HighScore
        var headers = new List<string> { "Speed", "Len", "Time ", "HighScore" };
        if (_opt.UseLevel) headers.Insert(0, "Lvl");
        var headline = $"| {string.Join(" | ", headers)} |";
        var separator = new string(headline.Select(q => q == '|' ? '|' : '-').ToArray());

        var lens = headers.Select(q => q.Length).ToArray();
        var vals = new List<string> { _db.SpeedDisplay, _db.CurrentLength + "", _db.Sw.Elapsed.ToString("mm\\:ss"), _hs + "" };
        if (_opt.UseLevel) vals.Insert(0, _db.Level + "");
        var qq = lens.Zip(vals, (q, w) => w.PadLeft(q));
        var bodyline = $"| {string.Join(" | ", qq)} |";

        Console.WriteLine(headline);
        Console.WriteLine(separator);
        Console.WriteLine(bodyline);
        Console.WriteLine();
    }
    public void UpdatePoint(int y, string value)
    {
        Console.SetCursorPosition(y, 2);
        Console.Write(value);
    }
}
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
class Snake
{
    static readonly Random Rand = new Random();
    static (int X, int Y) Head, Crate;
    static SpeedDirection Dir;
    static readonly ConcurrentQueue<(int X, int Y)> Steps = new ConcurrentQueue<(int X, int Y)>();
    readonly System.Timers.Timer Timer;
    readonly IMap TheMap;
    readonly string Border;
    readonly Gameplay _opt; readonly GameplayMotor _motor; readonly GameplayLevel _lvl; readonly Visual _visual; readonly VisualMap _mapOpts;
    readonly HighScore _hs;
    readonly Renderer _rdr;
    readonly Dashboard _db;
    public Snake(IOptions<Config> cfg, HighScore hs, IMap map, Renderer renderer, Dashboard db)
    {
        _db = db;
        _hs = hs;
        (_opt, _lvl, _motor, _visual, _mapOpts) = cfg.Value;
        Timer = new System.Timers.Timer(_motor.StartingSpeed);
        TheMap = map;
        Border = string.Join(' ', Enumerable.Repeat<string>(_mapOpts.Wall, _mapOpts.SideLength + 2));
        _rdr = renderer;
    }
    internal void Start()
    {
        Console.CursorVisible = false;
        bool canMarchByTimer = (_motor.MotorEnum & MotorEnum.ByTimer) > 0;
        if (canMarchByTimer)
        {
            Timer.Elapsed += March;
            Timer.Enabled = true;
        }
        Reset();
        while (true)
        {
            var key = Console.ReadKey().Key;
            if (key is ConsoleKey.Escape) return;
            Dir = ChangeDirection(key, Dir);
            March(null, EventArgs.Empty);
            // March2(Dir);
        }
        static SpeedDirection ChangeDirection(ConsoleKey key, SpeedDirection dir) => key switch
        {
            ConsoleKey.UpArrow or ConsoleKey.W when dir is not SpeedDirection.Down => SpeedDirection.Up,
            ConsoleKey.DownArrow or ConsoleKey.S when dir is not SpeedDirection.Up => SpeedDirection.Down,
            ConsoleKey.LeftArrow or ConsoleKey.A when dir is not SpeedDirection.Right => SpeedDirection.Left,
            ConsoleKey.RightArrow or ConsoleKey.D when dir is not SpeedDirection.Left => SpeedDirection.Right,
            _ => dir
        };
    }

    // private void March2(SpeedDirection dir)
    // {
    //     switch (dir)
    //     {
    //         case SpeedDirection.Up: Console.CursorTop--; break;
    //         case SpeedDirection.Down: Console.CursorTop++; break;
    //         case SpeedDirection.Right: Console.CursorLeft++; break;
    //         case SpeedDirection.Left: Console.CursorLeft--; break;
    //     }
    // }

    void March(object? sender, EventArgs e)
    {
        bool isMarchByKey = sender == null;
        bool canMarchByKey = (_motor.MotorEnum & MotorEnum.ByKey) > 0;
        if (isMarchByKey && !canMarchByKey) return;
        if (Dir == SpeedDirection.None) return;
        if (!_db.Sw.IsRunning) _db.Sw.Restart();
        TheMap[Head] = TileType.Body;
        (Head.X, Head.Y, var isHit) = CanPassWall(Dir, TheMap);
        if (isHit && !_opt.CanPassWall) { Reset(); return; }
        if (Head == Crate)
        {
            ++_db.CurrentLength;
            if (Steps.Count == TheMap.Length - 1) { Reset(); return; } // Win condition
            NextCrate();
        }
        if (TheMap[Head] == TileType.Body) { Reset(); return; } // Loss condition, seems a bit early to put it here...
        Steps.Enqueue(Head);
        TheMap[Head] = TileType.Head;
        if (Steps.Count > _db.CurrentLength)
        {
            var isOut = Steps.TryDequeue(out var step);
            if (isOut) TheMap[step] = TileType.None;
        }
        static (int x, int y, bool isHit) CanPassWall(SpeedDirection dir, IMap map) => dir switch
        {
            SpeedDirection.Up when Head.X-- == map.TopBound => (map.BottomBound, Head.Y, true),
            SpeedDirection.Down when Head.X++ == map.BottomBound => (map.TopBound, Head.Y, true),
            SpeedDirection.Left when Head.Y-- == map.LeftBound => (Head.X, map.RightBound, true),
            SpeedDirection.Right when Head.Y++ == map.RightBound => (Head.X, map.LeftBound, true),
            _ => (Head.X, Head.Y, false)
        };
    }

    void NextCrate()
    {
        if (_opt.UseLevel
            && (_db.CurrentLength % _lvl.Threshold) == 0
            && (_db.Level + 1) < _lvl.Levels.Count)
        {
            var speedLevel = _lvl.Levels[++_db.Level];
            bool canMarchByTimer = (_motor.MotorEnum & MotorEnum.ByTimer) > 0;
            if (canMarchByTimer && _motor.UseLevelAccelerator)
            {
                _db.SetSpeedDisplay(speedLevel); // = GetSpeedDisplay(speedLevel);
                Timer.Interval = _motor.StartingSpeed / speedLevel;
            }
        }
        Crate = NextCrate(_mapOpts.SideLength);
        while (TheMap[Crate] > 0)
            Crate = NextCrate(_mapOpts.SideLength);
        TheMap[Crate] = TileType.Crate;
    }

    static (int X, int Y) NextCrate(int max) => (Rand.Next(max), Rand.Next(max));
    void Reset()
    {
        TheMap.Clear();
        Steps.Clear();
        Dir = SpeedDirection.None;
        TheMap[0, 0] = TileType.Head;
        Head = default;
        Crate = default;
        _db.Reset();
        Timer.Interval = _motor.StartingSpeed;
        Steps.Enqueue(Head);
        _hs.SetHighScore();
        while (Crate == default)
            Crate = NextCrate(_mapOpts.SideLength);
        TheMap[Crate] = TileType.Crate;
        _rdr.CleanMap(TheMap, _hs, _db);
    }
    // void RenderAll()
    // {
    //     Console.Clear();
    //     // if (_visual.UseDashboard) RendorDashboard();
    //     // if (_visual.UseBorder) Console.WriteLine(Border);
    //     for (int x = 0; x <= TheMap.BottomBound; x++)
    //     {
    //         // if (_visual.UseBorder) Console.Write(_mapOpts.Wall + " ");
    //         for (int y = 0; y <= TheMap.RightBound; y++)
    //         {
    //             Console.Write(TheMap[x, y] switch
    //             {
    //                 TileType.Crate => _mapOpts.Crate,
    //                 TileType.Head => _mapOpts.Head,
    //                 TileType.Body => _mapOpts.Body,
    //                 _ => _mapOpts.None
    //             });
    //             Console.Write(' ');
    //         }
    //         // if (_visual.UseBorder) Console.Write(_mapOpts.Wall);
    //         Console.WriteLine();
    //     }
    //     // if (_visual.UseBorder) Console.WriteLine(Border);
    //     Console.WriteLine();
    //     // Action q = () => Console.WriteLine();
    // }
}