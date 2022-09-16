using System.Collections.Concurrent;
using System.Diagnostics;
using System.Timers;

static class Snake
{
    static readonly Random Rand = new Random();
    static readonly Stopwatch Sw = new Stopwatch();
    static readonly System.Timers.Timer Timer = new System.Timers.Timer(Config.Speed);
    static readonly TileType[,] Arr = new TileType[Config.Bound, Config.Bound];
    static readonly string Border = string.Join(' ', Enumerable.Repeat<char>(Config.Wall, Config.Bound + 2));
    static int Len, HighScore, Level = Config.DefLevel;
    static (int X, int Y) Head, Crate;
    static SpeedDirection Dir;
    static readonly ConcurrentQueue<(int X, int Y)> Steps = new ConcurrentQueue<(int X, int Y)>();
    internal static void Start()
    {
        if (Config.UseSpeed)
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
            March(null, ElapsedEventArgs.Empty);
        }
    }
    static void March(object? sender, EventArgs e)
    {
        if (!Config.CanSpeedUp && !(e is ElapsedEventArgs)) return;
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
            var lvl = Config.Levels[++Level];
            if (Config.UseSpeed && Config.UseAcceleration) Timer.Interval = Config.Speed / lvl;
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
        Level = Config.DefLevel;
        Timer.Interval = Config.Speed;
        Steps.Enqueue(Head);
        if (Len > HighScore) HighScore = Len;
        Len = Config.StartLen;
        while (Crate == default)
            Crate = NextCrate(Config.Bound);
        Arr[Crate.X, Crate.Y] = TileType.Crate;
        Sw.Stop();
        Render();
    }
    static void Render()
    {
        Console.Clear();
        RendorDashboard();
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
        var headers = new List<string> { "Time ", "Score", "HighScore" };
        if (Config.UseLevel) headers.Insert(1, "Level");
        var headline = $"| {string.Join(" | ", headers)} |";
        var seperate = new string(headline.Select(q => q == '|' ? '|' : '-').ToArray());

        var lens = headers.Select(q => q.Length).ToArray();
        var vals = new List<string> { Sw.Elapsed.ToString("mm\\:ss"), Len + "", HighScore + "" };
        if (Config.UseLevel) vals.Insert(1, Level + "");
        var qq = lens.Zip(vals, (q, w) => w.PadLeft(q));
        var bodyline = $"| {string.Join(" | ", qq)} |";

        Console.WriteLine(headline);
        Console.WriteLine(seperate);
        Console.WriteLine(bodyline);
        Console.WriteLine();
    }
}