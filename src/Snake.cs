using System.Collections.Concurrent;
using System.Diagnostics;
using System.Timers;
using Microsoft.Extensions.Options;

class HighScore
{
    public int Length;
    public string Time = "99:99";
    public void SetHighScore(int len, Stopwatch sw)
    {
        sw.Stop();
        bool isFirstRun = len == 0;
        if (isFirstRun || len < Length) return;
        Length = len;
        var time = sw.Elapsed.ToString("mm\\:ss");
        if (time.CompareTo(Time) >= 0) return;
        Time = time;
    }
    public override string ToString() => $"{Length}@{Time}";
}
class ExceedBorder : Exception { }
class Snake
{
    static readonly Random Rand = new Random();
    static readonly Stopwatch Sw = new Stopwatch();
    static readonly HighScore HighScore = new();
    static string SpeedDisplay = string.Empty;
    static (int X, int Y) Head, Crate;
    static SpeedDirection Dir;
    static readonly ConcurrentQueue<(int X, int Y)> Steps = new ConcurrentQueue<(int X, int Y)>();
    readonly System.Timers.Timer Timer;
    readonly TileType[,] Arr;
    readonly string Border;
    int Len, Level;
    readonly Gameplay _opt;
    readonly GameplayLevel _lvl;
    readonly GameplayMotor _motor;
    readonly Visual _visual;
    readonly VisualMap _map;
    public Snake(IOptions<Config> cfg)
    {
        var q = cfg.Value;
        (_opt, _lvl, _motor, _visual, _map) = q;
        Level = _lvl.DefaultLevel;
        Timer = new System.Timers.Timer(_motor.StartingSpeed);
        Arr = new TileType[_map.SideLength, _map.SideLength];
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
        Arr[Head.X, Head.Y] = TileType.Body;
        Func<SpeedDirection, TileType[,], (int X, int Y)> marchForward = _opt.CanPassWall ? CanPassWall : CanNotPassWall;
        try { (Head.X, Head.Y) = marchForward(Dir, Arr); }
        catch (ExceedBorder) { Reset(); return; }
        if (Head == Crate)
        {
            if (Steps.Count == Arr.Length - 1) { ++Len; Reset(); return; } // Win condition
            NextCrate();
        }
        if (Arr[Head.X, Head.Y] == TileType.Body) { Reset(); return; } // Loss condition
        Steps.Enqueue(Head);
        Arr[Head.X, Head.Y] = TileType.Head;
        if (Steps.Count > Len)
        {
            var isOut = Steps.TryDequeue(out var step);
            if (isOut) Arr[step.X, step.Y] = TileType.None;
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
        if ((++Len % _lvl.Threshold) == 0
            && _opt.UseLevel
            && (Level + 1) < _lvl.Levels.Count)
        {
            var speedLevel = _lvl.Levels[++Level];
            bool canMarchByTimer = (_motor.MotorEnum & MotorEnum.ByTimer) > 0;
            if (canMarchByTimer && _motor.UseLevelAccelerator)
            {
                SpeedDisplay = $"{_motor.StartingSpeed / 1000}x{speedLevel}";
                Timer.Interval = _motor.StartingSpeed / speedLevel;
            }
        }
        Crate = NextCrate(_map.SideLength);
        while (Arr[Crate.X, Crate.Y] > 0)
            Crate = NextCrate(_map.SideLength);
        Arr[Crate.X, Crate.Y] = TileType.Crate;
    }
    static (int X, int Y) NextCrate(int max) => (Rand.Next(max), Rand.Next(max));
    void Reset()
    {
        Array.Clear(Arr, 0, Arr.Length);
        Steps.Clear();
        Dir = SpeedDirection.None;
        Arr[0, 0] = TileType.Head;
        Head = default;
        Crate = default;
        Level = _lvl.DefaultLevel;
        SpeedDisplay = $"{_motor.StartingSpeed / 1000}x1";
        Timer.Interval = _motor.StartingSpeed;
        Steps.Enqueue(Head);
        HighScore.SetHighScore(Len, Sw);
        Len = _opt.StartingLength;
        while (Crate == default)
            Crate = NextCrate(_map.SideLength);
        Arr[Crate.X, Crate.Y] = TileType.Crate;
        Render();
    }
    void Render()
    {
        Console.Clear();
        if (_visual.UseDashboard) RendorDashboard();
        if (_visual.UseBorder) Console.WriteLine(Border);
        for (int x = 0; x <= Arr.GetUpperBound(0); x++)
        {
            if (_visual.UseBorder) Console.Write(_map.Wall + " ");
            for (int y = 0; y <= Arr.GetUpperBound(1); y++)
            {
                Console.Write(Arr[y, x] switch
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
        var vals = new List<string> { SpeedDisplay, Len + "", Sw.Elapsed.ToString("mm\\:ss"), HighScore + "" };
        if (_opt.UseLevel) vals.Insert(0, Level + "");
        var qq = lens.Zip(vals, (q, w) => w.PadLeft(q));
        var bodyline = $"| {string.Join(" | ", qq)} |";

        Console.WriteLine(headline);
        Console.WriteLine(seperate);
        Console.WriteLine(bodyline);
        Console.WriteLine();
    }
}