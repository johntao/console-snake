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
class ExceedBorder : Exception { }
class Snake
{
    static readonly Random Rand = new Random();
    static readonly Stopwatch Sw = new Stopwatch();
    static string SpeedDisplay = string.Empty;
    static (int X, int Y) Head, Crate;
    static SpeedDirection Dir;
    static readonly ConcurrentQueue<(int X, int Y)> Steps = new ConcurrentQueue<(int X, int Y)>();
    readonly System.Timers.Timer Timer;
    readonly TileType[,] TheMap;
    readonly string Border;
    int CurrentLength, Level;
    readonly Gameplay _opt;
    readonly GameplayLevel _lvl;
    readonly GameplayMotor _motor;
    readonly Visual _visual;
    readonly VisualMap _map;
    readonly HighScore _hs;
    public Snake(IOptions<Config> cfg, HighScore hs)
    {
        _hs = hs;
        var q = cfg.Value;
        (_opt, _lvl, _motor, _visual, _map) = q;
        Level = _lvl.DefaultLevel;
        Timer = new System.Timers.Timer(_motor.StartingSpeed);
        TheMap = new TileType[_map.SideLength, _map.SideLength];
        Border = string.Join(' ', Enumerable.Repeat<string>(_map.Wall, _map.SideLength + 2));
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
        TheMap[Head.X, Head.Y] = TileType.Body;
        Func<SpeedDirection, TileType[,], (int X, int Y)> marchForward = _opt.CanPassWall ? CanPassWall : CanNotPassWall;
        try { (Head.X, Head.Y) = marchForward(Dir, TheMap); }
        catch (ExceedBorder) { Reset(); return; }
        if (Head == Crate)
        {
            ++CurrentLength;
            if (Steps.Count == TheMap.Length - 1) { Reset(); return; } // Win condition
            NextCrate();
        }
        if (TheMap[Head.X, Head.Y] == TileType.Body) { Reset(); return; } // Loss condition
        Steps.Enqueue(Head);
        TheMap[Head.X, Head.Y] = TileType.Head;
        if (Steps.Count > CurrentLength)
        {
            var isOut = Steps.TryDequeue(out var step);
            if (isOut) TheMap[step.X, step.Y] = TileType.None;
        }
        Render();
        static (int X, int Y) CanNotPassWall(SpeedDirection dir, TileType[,] arr) => dir switch
        {
            SpeedDirection.Up when Head.Y-- == arr.GetLowerBound(1) => throw new ExceedBorder(),
            SpeedDirection.Down when Head.Y++ == arr.GetUpperBound(1) => throw new ExceedBorder(),
            SpeedDirection.Left when Head.X-- == arr.GetLowerBound(0) => throw new ExceedBorder(),
            SpeedDirection.Right when Head.X++ == arr.GetUpperBound(0) => throw new ExceedBorder(),
            _ => Head
        };
        static (int X, int Y) CanPassWall(SpeedDirection dir, TileType[,] arr) => dir switch
        {
            SpeedDirection.Up when --Head.Y < arr.GetLowerBound(1) => (Head.X, arr.GetUpperBound(1)),
            SpeedDirection.Down when ++Head.Y > arr.GetUpperBound(1) => (Head.X, arr.GetLowerBound(1)),
            SpeedDirection.Left when --Head.X < arr.GetLowerBound(0) => (arr.GetUpperBound(0), Head.Y),
            SpeedDirection.Right when ++Head.X > arr.GetUpperBound(0) => (arr.GetLowerBound(0), Head.Y),
            _ => Head
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
        Crate = NextCrate(_map.SideLength);
        while (TheMap[Crate.X, Crate.Y] > 0)
            Crate = NextCrate(_map.SideLength);
        TheMap[Crate.X, Crate.Y] = TileType.Crate;
    }

    private string GetSpeedDisplay(double speedLevel) => $"{1 / ((double)_motor.StartingSpeed / 1000):0.#}x{speedLevel}";

    static (int X, int Y) NextCrate(int max) => (Rand.Next(max), Rand.Next(max));
    void Reset()
    {
        Array.Clear(TheMap, 0, TheMap.Length);
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
            Crate = NextCrate(_map.SideLength);
        TheMap[Crate.X, Crate.Y] = TileType.Crate;
        Render();
    }
    void Render()
    {
        Console.Clear();
        if (_visual.UseDashboard) RendorDashboard();
        if (_visual.UseBorder) Console.WriteLine(Border);
        for (int x = 0; x <= TheMap.GetUpperBound(0); x++)
        {
            if (_visual.UseBorder) Console.Write(_map.Wall + " ");
            for (int y = 0; y <= TheMap.GetUpperBound(1); y++)
            {
                Console.Write(TheMap[y, x] switch
                {
                    TileType.Crate => _map.Crate,
                    TileType.Head => _map.Head,
                    TileType.Body => _map.Body,
                    _ => _map.None
                });
                Console.Write(' ');
            }
            if (_visual.UseBorder) Console.Write(_map.Wall);
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