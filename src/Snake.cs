using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

class Snake
{
    static readonly Random Rand = new Random();
    static (int X, int Y) Head, Food;
    static SpeedDirection Dir;
    static readonly ConcurrentQueue<(int X, int Y)> Bodies = new ConcurrentQueue<(int X, int Y)>();
    readonly System.Timers.Timer MoveTimer;
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
        MoveTimer = new System.Timers.Timer(_motor.StartingSpeed);
        TheMap = map;
        Border = string.Join(' ', Enumerable.Repeat<string>(_mapOpts.Wall, _mapOpts.SideLength + 2));
    }
    private static AutoResetEvent MoveWaiter = new AutoResetEvent(false);
    internal void Start()
    {
        Console.CursorVisible = false;
        bool canMoveByTimer = (_motor.MotorEnum & MotorEnum.ByTimer) > 0;
        if (canMoveByTimer)
        {
            MoveTimer.Elapsed += Move;
            MoveTimer.Enabled = true;
        }
        Reset();
        while (true)
        {
            var key = Console.ReadKey().Key;
            if (key is ConsoleKey.Escape) return;
            Dir = ChangeDirection(key, Dir);
            bool canMoveByKey = (_motor.MotorEnum & MotorEnum.ByKey) > 0;
            if (canMoveByKey) Move(null, EventArgs.Empty);
            MoveWaiter.WaitOne();
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
    void Move(object? sender, EventArgs e)
    {
        if (Dir == SpeedDirection.None) return;
        if (!_board.Stopwatch.IsRunning) _board.Stopwatch.Restart(); // Start once user changed the direction
        TheMap[Head] = TileType.Body; // Mark old head position as bodyTile
        (Head.X, Head.Y, var isHit) = StepForward(Dir, TheMap); // Move head, check if hitBorder
        if (isHit && !_opt.CanPassWall) { Reset(); return; } // Loss condition
        Bodies.Enqueue(Head); // Enqueue the new snake head
        if (TheMap[Head] is TileType.Food)
        {
            ++_board.CurrentSnakeLength;
            if (Bodies.Count == TheMap.Length - 1) { Reset(); return; } // Win condition
            NextFood();
        }
        if (Bodies.Count > _board.CurrentSnakeLength)
        {
            var isOut = Bodies.TryDequeue(out var body); // Dequeue the snake tail
            if (isOut) TheMap[body] = TileType.None;
        }
        // Loss condition, this one MUST followed after dequeue, in order to handle straight line full-width snake case.
        if (TheMap[Head] == TileType.Body) { Reset(); return; }
        TheMap[Head] = TileType.Head; // Mark new head position as headTile
        MoveWaiter.Set();
        static (int x, int y, bool isHit) StepForward(SpeedDirection dir, IMap map) => dir switch
        {
            SpeedDirection.Up when Head.X-- == map.TopBound => (map.BottomBound, Head.Y, true),
            SpeedDirection.Down when Head.X++ == map.BottomBound => (map.TopBound, Head.Y, true),
            SpeedDirection.Left when Head.Y-- == map.LeftBound => (Head.X, map.RightBound, true),
            SpeedDirection.Right when Head.Y++ == map.RightBound => (Head.X, map.LeftBound, true),
            _ => (Head.X, Head.Y, false)
        };
    }
    void NextFood()
    {
        _board.LevelUp(MoveTimer);
        Food = RandomPlace(_mapOpts.SideLength);
        while (TheMap[Food] > 0)
            Food = RandomPlace(_mapOpts.SideLength);
        TheMap[Food] = TileType.Food;
    }
    static (int X, int Y) RandomPlace(int max) => (Rand.Next(max), Rand.Next(max));
    void Reset()
    {
        TheMap.Clear();
        Bodies.Clear();
        Dir = SpeedDirection.None;
        TheMap[0, 0] = TileType.Head;
        Head = default;
        Food = default;
        MoveTimer.Interval = _motor.StartingSpeed;
        Bodies.Enqueue(Head);
        while (Food == default)
            Food = RandomPlace(_mapOpts.SideLength);
        TheMap[Food] = TileType.Food;
        MoveWaiter.Set();
        _board.ResetAndReRenderAll(TheMap);
    }
}