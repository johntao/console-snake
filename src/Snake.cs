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
        _opt = q.Gameplay;
        _lvl = q.GameplayLevel;
        _motor = q.GameplayMotor;
        _visual = q.Visual;
        _map = q.VisualMap;
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
            switch (key)
            {
                case ConsoleKey.Escape: return;
                case ConsoleKey.UpArrow:
                case ConsoleKey.W:
                    if (Dir == SpeedDirection.Down) continue;
                    Dir = SpeedDirection.Up; break;
                case ConsoleKey.DownArrow:
                case ConsoleKey.S:
                    if (Dir == SpeedDirection.Up) continue;
                    Dir = SpeedDirection.Down; break;
                case ConsoleKey.LeftArrow:
                case ConsoleKey.A:
                    if (Dir == SpeedDirection.Right) continue;
                    Dir = SpeedDirection.Left; break;
                case ConsoleKey.RightArrow:
                case ConsoleKey.D:
                    if (Dir == SpeedDirection.Left) continue;
                    Dir = SpeedDirection.Right; break;
            }
            March(null, EventArgs.Empty);
        }
    }
    void March(object? sender, EventArgs e)
    {
        bool isMarchByKey = sender == null;
        bool canMarchByKey = (_motor.MotorEnum & MotorEnum.ByKey) > 0;
        if (isMarchByKey && !canMarchByKey) return;
        if (Dir == SpeedDirection.None) return;
        if (!Sw.IsRunning) Sw.Restart();
        switch (Dir)
        {
            case SpeedDirection.Up:
                // forbid run out of border
                if (!_opt.CanPassWall && Head.Y == Arr.GetLowerBound(1)) { Reset(); return; }
                // set previous node from head to body
                Arr[Head.X, Head.Y--] = TileType.Body;
                if (_opt.CanPassWall && Head.Y < Arr.GetLowerBound(1)) Head.Y = Arr.GetUpperBound(1);
                break;
            case SpeedDirection.Down:
                if (!_opt.CanPassWall && Head.Y == Arr.GetUpperBound(1)) { Reset(); return; }
                Arr[Head.X, Head.Y++] = TileType.Body;
                if (_opt.CanPassWall && Head.Y > Arr.GetUpperBound(1)) Head.Y = Arr.GetLowerBound(1);
                break;
            case SpeedDirection.Left:
                if (!_opt.CanPassWall && Head.X == Arr.GetLowerBound(0)) { Reset(); return; }
                Arr[Head.X--, Head.Y] = TileType.Body;
                if (_opt.CanPassWall && Head.X < Arr.GetLowerBound(0)) Head.X = Arr.GetUpperBound(0);
                break;
            case SpeedDirection.Right:
                if (!_opt.CanPassWall && Head.X == Arr.GetUpperBound(0)) { Reset(); return; }
                Arr[Head.X++, Head.Y] = TileType.Body;
                if (_opt.CanPassWall && Head.X > Arr.GetUpperBound(0)) Head.X = Arr.GetLowerBound(0);
                break;
        }
        if (Head == Crate)
        {
            if (Steps.Count == Arr.Length - 1) { ++Len; Reset(); return; }
            NextCrate();
        }
        if (Arr[Head.X, Head.Y] == TileType.Body) { Reset(); return; }
        Steps.Enqueue(Head);
        Arr[Head.X, Head.Y] = TileType.Head;
        if (Steps.Count > Len)
        {
            var isOut = Steps.TryDequeue(out var step);
            if (isOut) Arr[step.X, step.Y] = TileType.None;
        }
        Render();
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