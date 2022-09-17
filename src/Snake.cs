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
    readonly ConfigPOCO _opt;
    readonly Level _optLvl;
    readonly TileSet _optTs;
    public Snake(IOptions<ConfigPOCO> cfg)
    {
        _opt = cfg.Value;
        _optLvl = _opt.Level;
        _optTs = _opt.TileSet;
        Level = _optLvl.DefaultLevel;
        Timer = new System.Timers.Timer(_opt.Speed);
        Arr = new TileType[_opt.Bound, _opt.Bound];
        Border = string.Join(' ', Enumerable.Repeat<string>(_optTs.Wall, _opt.Bound + 2));
    }
    internal void Start()
    {
        if (_opt.CanMarchByTimer)
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
        if (isMarchByKey && !_opt.CanMarchByKey) return;
        if (Dir == SpeedDirection.None) return;
        if (!Sw.IsRunning) Sw.Restart();
        switch (Dir)
        {
            case SpeedDirection.Up:
                // forbid run out of border
                if (!_opt.CanHitWall && Head.Y == Arr.GetLowerBound(1)) { Reset(); return; }
                // set previous node from head to body
                Arr[Head.X, Head.Y--] = TileType.Body;
                if (_opt.CanHitWall && Head.Y < Arr.GetLowerBound(1)) Head.Y = Arr.GetUpperBound(1);
                break;
            case SpeedDirection.Down:
                if (!_opt.CanHitWall && Head.Y == Arr.GetUpperBound(1)) { Reset(); return; }
                Arr[Head.X, Head.Y++] = TileType.Body;
                if (_opt.CanHitWall && Head.Y > Arr.GetUpperBound(1)) Head.Y = Arr.GetLowerBound(1);
                break;
            case SpeedDirection.Left:
                if (!_opt.CanHitWall && Head.X == Arr.GetLowerBound(0)) { Reset(); return; }
                Arr[Head.X--, Head.Y] = TileType.Body;
                if (_opt.CanHitWall && Head.X < Arr.GetLowerBound(0)) Head.X = Arr.GetUpperBound(0);
                break;
            case SpeedDirection.Right:
                if (!_opt.CanHitWall && Head.X == Arr.GetUpperBound(0)) { Reset(); return; }
                Arr[Head.X++, Head.Y] = TileType.Body;
                if (_opt.CanHitWall && Head.X > Arr.GetUpperBound(0)) Head.X = Arr.GetLowerBound(0);
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
        if ((++Len % _optLvl.Threshold) == 0
            && _opt.UseLevel
            && (Level + 1) < _optLvl.Levels.Count)
        {
            var speedLevel = _optLvl.Levels[++Level];
            if (_opt.CanMarchByTimer && _opt.UseAcceleration)
            {
                SpeedDisplay = $"{_opt.Speed / 1000}x{speedLevel}";
                Timer.Interval = _opt.Speed / speedLevel;
            }
        }
        Crate = NextCrate(_opt.Bound);
        while (Arr[Crate.X, Crate.Y] > 0)
            Crate = NextCrate(_opt.Bound);
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
        Level = _optLvl.DefaultLevel;
        SpeedDisplay = $"{_opt.Speed / 1000}x1";
        Timer.Interval = _opt.Speed;
        Steps.Enqueue(Head);
        HighScore.SetHighScore(Len, Sw);
        Len = _opt.StartLen;
        while (Crate == default)
            Crate = NextCrate(_opt.Bound);
        Arr[Crate.X, Crate.Y] = TileType.Crate;

        Render();
    }
    void Render()
    {
        Console.Clear();
        if (_opt.UseDashboard) RendorDashboard();
        if (_opt.UseBorder) Console.WriteLine(Border);
        for (int x = 0; x <= Arr.GetUpperBound(0); x++)
        {
            if (_opt.UseBorder) Console.Write(_optTs.Wall + " ");
            for (int y = 0; y <= Arr.GetUpperBound(1); y++)
            {
                Console.Write(Arr[y, x] switch
                {
                    TileType.Crate => _optTs.Crate,
                    TileType.Head => _optTs.Head,
                    TileType.Body => _optTs.Body,
                    _ => _optTs.None
                });
                Console.Write(' ');
            }
            if (_opt.UseBorder) Console.Write(_optTs.Wall);
            Console.WriteLine();
        }
        if (_opt.UseBorder) Console.WriteLine(Border);
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