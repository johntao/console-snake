using System.Collections.Concurrent;
using System.Diagnostics;
using System.Timers;

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
static class Snake
{
    static readonly Random Rand = new Random();
    static readonly Stopwatch Sw = new Stopwatch();
    static readonly System.Timers.Timer Timer = new System.Timers.Timer(Config.Speed);
    static readonly TileType[,] Arr = new TileType[Config.Bound, Config.Bound];
    static readonly string Border = string.Join(' ', Enumerable.Repeat<char>(Config.Wall, Config.Bound + 2));
    static int Len, Level = Config.DefaultLevel;
    static readonly HighScore HighScore = new();
    static string SpeedDisplay = string.Empty;
    static (int X, int Y) Head, Crate;
    static SpeedDirection Dir;
    static readonly ConcurrentQueue<(int X, int Y)> Steps = new ConcurrentQueue<(int X, int Y)>();
    internal static void Start()
    {
        if (Config.CanMarchByTimer)
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
    static void March(object? sender, EventArgs e)
    {
        bool isMarchByKey = sender == null;
        if (isMarchByKey && !Config.CanMarchByKey) return;
        if (Dir == SpeedDirection.None) return;
        if (!Sw.IsRunning) Sw.Restart();
        switch (Dir)
        {
            case SpeedDirection.Up:
                // forbid run out of border
                if (!Config.CanHitWall && Head.Y == Arr.GetLowerBound(1)) { Reset(); return; }
                // set previous node from head to body
                Arr[Head.X, Head.Y--] = TileType.Body;
                if (Config.CanHitWall && Head.Y < Arr.GetLowerBound(1)) Head.Y = Arr.GetUpperBound(1);
                break;
            case SpeedDirection.Down:
                if (!Config.CanHitWall && Head.Y == Arr.GetUpperBound(1)) { Reset(); return; }
                Arr[Head.X, Head.Y++] = TileType.Body;
                if (Config.CanHitWall && Head.Y > Arr.GetUpperBound(1)) Head.Y = Arr.GetLowerBound(1);
                break;
            case SpeedDirection.Left:
                if (!Config.CanHitWall && Head.X == Arr.GetLowerBound(0)) { Reset(); return; }
                Arr[Head.X--, Head.Y] = TileType.Body;
                if (Config.CanHitWall && Head.X < Arr.GetLowerBound(0)) Head.X = Arr.GetUpperBound(0);
                break;
            case SpeedDirection.Right:
                if (!Config.CanHitWall && Head.X == Arr.GetUpperBound(0)) { Reset(); return; }
                Arr[Head.X++, Head.Y] = TileType.Body;
                if (Config.CanHitWall && Head.X > Arr.GetUpperBound(0)) Head.X = Arr.GetLowerBound(0);
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
    static void NextCrate()
    {
        if ((++Len % Config.Threshold) == 0
            && Config.UseLevel
            && (Level + 1) < Config.Levels.Length)
        {
            var speedLevel = Config.Levels[++Level];
            if (Config.CanMarchByTimer && Config.UseAcceleration)
            {
                SpeedDisplay = $"{Config.Speed / 1000}x{speedLevel}";
                Timer.Interval = Config.Speed / speedLevel;
            }
        }
        Crate = NextCrate(Config.Bound);
        while (Arr[Crate.X, Crate.Y] > 0)
            Crate = NextCrate(Config.Bound);
        Arr[Crate.X, Crate.Y] = TileType.Crate;
    }
    static (int X, int Y) NextCrate(int max) => (Rand.Next(max), Rand.Next(max));
    static void Reset()
    {
        Array.Clear(Arr, 0, Arr.Length);
        Steps.Clear();
        Dir = SpeedDirection.None;
        Arr[0, 0] = TileType.Head;
        Head = default;
        Crate = default;
        Level = Config.DefaultLevel;
        SpeedDisplay = $"{Config.Speed / 1000}x1";
        Timer.Interval = Config.Speed;
        Steps.Enqueue(Head);
        HighScore.SetHighScore(Len, Sw);
        Len = Config.StartLen;
        while (Crate == default)
            Crate = NextCrate(Config.Bound);
        Arr[Crate.X, Crate.Y] = TileType.Crate;

        Render();
    }
    static void Render()
    {
        Console.Clear();
        if (Config.UseDashboard) RendorDashboard();
        if (Config.UseBorder) Console.WriteLine(Border);
        for (int x = 0; x <= Arr.GetUpperBound(0); x++)
        {
            if (Config.UseBorder) Console.Write(Config.Wall + " ");
            for (int y = 0; y <= Arr.GetUpperBound(1); y++)
            {
                Console.Write(Arr[y, x] switch
                {
                    TileType.Crate => Config.Tiles[3],
                    TileType.Head => Config.Tiles[2],
                    TileType.Body => Config.Tiles[1],
                    _ => Config.Tiles[0]
                });
                Console.Write(' ');
            }
            if (Config.UseBorder) Console.Write(Config.Wall);
            Console.WriteLine();
        }
        if (Config.UseBorder) Console.WriteLine(Border);
        Console.WriteLine();
    }
    static void RendorDashboard()
    {
        //Level, Speed, Length, Time, HighScore
        var headers = new List<string> { "Speed", "Len", "Time ", "HighScore" };
        if (Config.UseLevel) headers.Insert(0, "Lvl");
        var headline = $"| {string.Join(" | ", headers)} |";
        var seperate = new string(headline.Select(q => q == '|' ? '|' : '-').ToArray());

        var lens = headers.Select(q => q.Length).ToArray();
        var vals = new List<string> { SpeedDisplay, Len + "", Sw.Elapsed.ToString("mm\\:ss"), HighScore + "" };
        if (Config.UseLevel) vals.Insert(0, Level + "");
        var qq = lens.Zip(vals, (q, w) => w.PadLeft(q));
        var bodyline = $"| {string.Join(" | ", qq)} |";

        Console.WriteLine(headline);
        Console.WriteLine(seperate);
        Console.WriteLine(bodyline);
        Console.WriteLine();
    }
}