using System.Collections.Concurrent;
using System.Diagnostics;
using System.Timers;
using Microsoft.Extensions.Options;

class HighScore
{
    public int MaxLength;
    public string MinTime = "99:99";
    readonly int _startLength;
    public HighScore(IOptions<Config> cfg) => _startLength = cfg.Value.Gameplay.StartingLength;
    public void SetHighScore(ref int CurrentLength, Stopwatch sw)
    {
        sw.Stop();
        var time = sw.Elapsed.ToString("mm\\:ss");
        sw.Reset();
        bool isFirstRun = CurrentLength == 0;
        if (isFirstRun || CurrentLength < MaxLength)
        {
            CurrentLength = _startLength;
        }
        else if (CurrentLength == MaxLength)
        {
            CurrentLength = _startLength;
            if (time.CompareTo(MinTime) < 0)
                MinTime = time;
        }
        else if (CurrentLength > MaxLength)
        {
            MaxLength = CurrentLength;
            CurrentLength = _startLength;
            MinTime = time;
        }
    }
    public override string ToString() => $"{MaxLength}@{MinTime}";
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
    public Map2dArray(int height, int width)
    {
        _map = new TileType[height, width];
        TopBound = 0;
        BottomBound = height - 1;
        LeftBound = 0;
        RightBound = width - 1;
        Length = height * width - 1;
    }
    public TileType this[int x, int y]
    {
        get => _map[x, y];
        set => _map[x, y] = value;
    }
    public TileType this[(int x, int y) point]
    {
        get => _map[point.x, point.y];
        set => _map[point.x, point.y] = value;
    }
    public void Clear()
    {
        Array.Clear(_map, 0, _map.Length);
    }
}
class MapJaggedArray
{
    private readonly TileType[][] _map;
    public int TopBound { get; } // zero-based
    public int BottomBound { get; } // zero-based
    public int LeftBound { get; } // zero-based
    public int RightBound { get; } // zero-based
    public int Length { get; }
    public MapJaggedArray(int height, int width)
    {
        _map = new TileType[height][];
        for (int i = 0; i < width; i++)
            _map[i] = new TileType[width];
        TopBound = 0;
        BottomBound = height - 1;
        LeftBound = 0;
        RightBound = width - 1;
        Length = height * width - 1;
    }
    public TileType this[int x, int y]
    {
        get => _map[x][y];
        set => _map[x][y] = value;
    }
    public TileType this[(int x, int y) point]
    {
        get => _map[point.x][point.y];
        set => _map[point.x][point.y] = value;
    }
    public void Clear()
    {
        foreach (var item in _map)
            Array.Clear(item, 0, item.Length);
    }
}
class Snake
{
    static readonly Random Rand = new Random();
    static readonly Stopwatch Sw = new Stopwatch();
    static string SpeedDisplay = string.Empty;
    static (int X, int Y) Head, Crate;
    static SpeedDirection Dir;
    static readonly ConcurrentQueue<(int X, int Y)> Steps = new ConcurrentQueue<(int X, int Y)>();
    readonly System.Timers.Timer Timer;
    readonly IMap TheMap;
    readonly string Border;
    int CurrentLength, Level;
    readonly Gameplay _opt;
    readonly GameplayLevel _lvl;
    readonly GameplayMotor _motor;
    readonly Visual _visual;
    readonly VisualMap _mapOpts;
    readonly HighScore _hs;
    public Snake(IOptions<Config> cfg, HighScore hs)
    {
        _hs = hs;
        var q = cfg.Value;
        (_opt, _lvl, _motor, _visual, _mapOpts) = q;
        Level = _lvl.DefaultLevel;
        Timer = new System.Timers.Timer(_motor.StartingSpeed);
        TheMap = new Map2dArray(_mapOpts.SideLength, _mapOpts.SideLength);
        Border = string.Join(' ', Enumerable.Repeat<string>(_mapOpts.Wall, _mapOpts.SideLength + 2));
    }
    internal void Start()
    {
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
    void March(object? sender, EventArgs e)
    {
        bool isMarchByKey = sender == null;
        bool canMarchByKey = (_motor.MotorEnum & MotorEnum.ByKey) > 0;
        if (isMarchByKey && !canMarchByKey) return;
        if (Dir == SpeedDirection.None) return;
        if (!Sw.IsRunning) Sw.Restart();
        TheMap[Head] = TileType.Body;
        (Head.X, Head.Y, var isHit) = CanPassWall(Dir, TheMap);
        if (isHit && !_opt.CanPassWall) { Reset(); return; }
        if (Head == Crate)
        {
            ++CurrentLength;
            if (Steps.Count == TheMap.Length - 1) { Reset(); return; } // Win condition
            NextCrate();
        }
        if (TheMap[Head] == TileType.Body) { Reset(); return; } // Loss condition
        Steps.Enqueue(Head);
        TheMap[Head] = TileType.Head;
        if (Steps.Count > CurrentLength)
        {
            var isOut = Steps.TryDequeue(out var step);
            if (isOut) TheMap[step] = TileType.None;
        }
        Render();
        static (int x, int y, bool isHit) CanPassWall(SpeedDirection dir, IMap map) => dir switch
        {
            SpeedDirection.Up when Head.Y-- == map.LeftBound => (Head.X, map.RightBound, true),
            SpeedDirection.Down when Head.Y++ == map.RightBound => (Head.X, map.LeftBound, true),
            SpeedDirection.Left when Head.X-- == map.TopBound => (map.BottomBound, Head.Y, true),
            SpeedDirection.Right when Head.X++ == map.BottomBound => (map.TopBound, Head.Y, true),
            _ => (Head.X, Head.Y, false)
        };
    }
    void NextCrate()
    {
        if (_opt.UseLevel
            && (CurrentLength % _lvl.Threshold) == 0
            && (Level + 1) < _lvl.Levels.Count)
        {
            var speedLevel = _lvl.Levels[++Level];
            bool canMarchByTimer = (_motor.MotorEnum & MotorEnum.ByTimer) > 0;
            if (canMarchByTimer && _motor.UseLevelAccelerator)
            {
                SpeedDisplay = GetSpeedDisplay(speedLevel);
                Timer.Interval = _motor.StartingSpeed / speedLevel;
            }
        }
        Crate = NextCrate(_mapOpts.SideLength);
        while (TheMap[Crate] > 0)
            Crate = NextCrate(_mapOpts.SideLength);
        TheMap[Crate] = TileType.Crate;
    }

    private string GetSpeedDisplay(double speedLevel) => $"{1 / ((double)_motor.StartingSpeed / 1000):0.#}x{speedLevel}";

    static (int X, int Y) NextCrate(int max) => (Rand.Next(max), Rand.Next(max));
    void Reset()
    {
        TheMap.Clear();
        Steps.Clear();
        Dir = SpeedDirection.None;
        TheMap[0, 0] = TileType.Head;
        Head = default;
        Crate = default;
        Level = _lvl.DefaultLevel;
        SpeedDisplay = GetSpeedDisplay(1);
        Timer.Interval = _motor.StartingSpeed;
        Steps.Enqueue(Head);
        _hs.SetHighScore(ref CurrentLength, Sw);
        while (Crate == default)
            Crate = NextCrate(_mapOpts.SideLength);
        TheMap[Crate.X, Crate.Y] = TileType.Crate;
        Render();
    }
    void Render()
    {
        Console.Clear();
        if (_visual.UseDashboard) RendorDashboard();
        if (_visual.UseBorder) Console.WriteLine(Border);
        for (int x = 0; x <= TheMap.BottomBound; x++)
        {
            if (_visual.UseBorder) Console.Write(_mapOpts.Wall + " ");
            for (int y = 0; y <= TheMap.RightBound; y++)
            {
                Console.Write(TheMap[y, x] switch
                {
                    TileType.Crate => _mapOpts.Crate,
                    TileType.Head => _mapOpts.Head,
                    TileType.Body => _mapOpts.Body,
                    _ => _mapOpts.None
                });
                Console.Write(' ');
            }
            if (_visual.UseBorder) Console.Write(_mapOpts.Wall);
            Console.WriteLine();
        }
        if (_visual.UseBorder) Console.WriteLine(Border);
        Console.WriteLine();
    }
    void RendorDashboard()
    {
        //Level, Speed, Length, Time, HighScore
        var headers = new List<string> { "Speed", "Len", "Time ", "HighScore" };
        if (_opt.UseLevel) headers.Insert(0, "Lvl");
        var headline = $"| {string.Join(" | ", headers)} |";
        var seperate = new string(headline.Select(q => q == '|' ? '|' : '-').ToArray());

        var lens = headers.Select(q => q.Length).ToArray();
        var vals = new List<string> { SpeedDisplay, CurrentLength + "", Sw.Elapsed.ToString("mm\\:ss"), _hs + "" };
        if (_opt.UseLevel) vals.Insert(0, Level + "");
        var qq = lens.Zip(vals, (q, w) => w.PadLeft(q));
        var bodyline = $"| {string.Join(" | ", qq)} |";

        Console.WriteLine(headline);
        Console.WriteLine(seperate);
        Console.WriteLine(bodyline);
        Console.WriteLine();
    }
}