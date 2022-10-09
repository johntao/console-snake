using System.Collections.Concurrent;
using System.Timers;
using Microsoft.Extensions.Options;

class Snake
{
    static readonly Random Rand = new Random();
    static (int X, int Y) Head, Crate;
    static SpeedDirection Dir;
    static readonly ConcurrentQueue<(int X, int Y)> Steps = new ConcurrentQueue<(int X, int Y)>();
    readonly System.Timers.Timer Timer;
    readonly IMap TheMap;
    readonly string Border;
    readonly Gameplay _opt;
    readonly GameplayMotor _motor;
    readonly VisualMap _mapOpts;
    readonly Dashboard _board;
    public Snake(IOptions<Config> cfg, IMap map, Dashboard board)
    {
        _board = board;
        (_opt, _, _motor, _, _mapOpts) = cfg.Value;
        Timer = new System.Timers.Timer(_motor.StartingSpeed);
        TheMap = map;
        Border = string.Join(' ', Enumerable.Repeat<string>(_mapOpts.Wall, _mapOpts.SideLength + 2));
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
        if (!_board.Sw.IsRunning) _board.Sw.Restart();
        TheMap[Head] = TileType.Body;
        (Head.X, Head.Y, var isHit) = CanPassWall(Dir, TheMap);
        if (isHit && !_opt.CanPassWall) { Reset(); return; }
        if (Head == Crate)
        {
            ++_board.CurrentLength;
            if (Steps.Count == TheMap.Length - 1) { Reset(); return; } // Win condition
            NextCrate();
        }
        if (TheMap[Head] == TileType.Body) { Reset(); return; } // Loss condition, seems a bit early to put it here...
        Steps.Enqueue(Head);
        TheMap[Head] = TileType.Head;
        if (Steps.Count > _board.CurrentLength)
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
        _board.LevelUp(Timer);
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
        Timer.Interval = _motor.StartingSpeed;
        Steps.Enqueue(Head);
        while (Crate == default)
            Crate = NextCrate(_mapOpts.SideLength);
        TheMap[Crate] = TileType.Crate;
        _board.ResetAndReRenderAll(TheMap);
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