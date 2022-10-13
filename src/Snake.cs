using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

class Snake : BackgroundService
{
    static readonly Random Rand = new Random();
    static (int row, int col) Head, Food;
    static SpeedDirection Dir;
    static readonly ConcurrentQueue<(int row, int col)> Bodies = new ConcurrentQueue<(int row, int col)>();
    readonly System.Timers.Timer MoveTimer;
    readonly IMap TheMap;
    readonly string Border;
    readonly Gameplay _optGameplay;
    readonly GameplayMotor _optMotor;
    readonly VisualMap _optMaps;
    readonly Dashboard _board;
    readonly IHostApplicationLifetime appLifetime;
    public Snake(IOptions<Config> cfg, IMap map, Dashboard board, IHostApplicationLifetime appLifetime)
    {
        this.appLifetime = appLifetime;
        _board = board;
        (_optGameplay, _, _optMotor, _, _optMaps) = cfg.Value;
        MoveTimer = new System.Timers.Timer(_optMotor.StartingSpeed);
        TheMap = map;
        Border = string.Join(' ', Enumerable.Repeat<string>(_optMaps.Wall, _optMaps.SideLength + 2));
    }
    private static AutoResetEvent MoveWaiter = new AutoResetEvent(false);
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.Run(Start);
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        MoveTimer.Dispose();
        _board.BoardTimer.Dispose();
        Console.Clear();
        Console.WriteLine("Shutting Down...");
        return base.StopAsync(cancellationToken);
    }
    // private void Start(object? state)
    // private void Start(CancellationToken stoppingToken)
    private void Start()
    {
        // var stoppingToken = (CancellationToken)state!;
        Console.CursorVisible = false;
        bool canMoveByTimer = (_optMotor.MotorEnum & MotorEnum.ByTimer) > 0;
        if (canMoveByTimer)
        {
            MoveTimer.Elapsed += Move;
            MoveTimer.Enabled = true;
        }
        Reset();
        // while (!stoppingToken.IsCancellationRequested)
        while (true)
        {
            var key = Console.ReadKey().Key;
            if (key is ConsoleKey.Escape) { appLifetime.StopApplication(); return; }
            Dir = ChangeDirection(key, Dir);
            var canMoveByKey = (_optMotor.MotorEnum & MotorEnum.ByKey) > 0;
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
        switch (MoveExec())
        {
            case GameResult.Win:
            case GameResult.Loss:
                Reset();
                return;
        }
        TheMap[Head] = TileType.Head; // Mark new head position as headTile
        MoveWaiter.Set();
    }
    private GameResult MoveExec()
    {
        (Head.row, Head.col, var isBorderHit) = StepForward(Dir, TheMap); // Move head, check if the snake hit the border
        if (isBorderHit && !_optGameplay.CanPassWall) return GameResult.Loss;
        Bodies.Enqueue(Head); // Enqueue the new snake head
        switch (TheMap[Head])
        {
            case TileType.Food:
                ++_board.CurrentSnakeLength;
                if (Bodies.Count == TheMap.Length) return GameResult.Win;
                NextFood();
                break;
            case TileType.Body:
                Bodies.TryPeek(out var tail);
                if (Head != tail) return GameResult.Loss;
                goto case TileType.None;
            case TileType.None:
                var isOut = Bodies.TryDequeue(out tail);
                if (isOut) TheMap[tail] = TileType.None;
                break;
        }
        return GameResult.None;
        static (int x, int y, bool isHit) StepForward(SpeedDirection dir, IMap map) => dir switch
        {
            SpeedDirection.Up when Head.row-- == map.TopBound => (map.BottomBound, Head.col, true),
            SpeedDirection.Down when Head.row++ == map.BottomBound => (map.TopBound, Head.col, true),
            SpeedDirection.Left when Head.col-- == map.LeftBound => (Head.row, map.RightBound, true),
            SpeedDirection.Right when Head.col++ == map.RightBound => (Head.row, map.LeftBound, true),
            _ => (Head.row, Head.col, false)
        };
    }
    void NextFood()
    {
        _board.LevelUp(MoveTimer);
        Food = RandomPlace(_optMaps.SideLength);
        while (TheMap[Food] > 0)
            Food = RandomPlace(_optMaps.SideLength);
        TheMap[Food] = TileType.Food;
    }
    static (int row, int col) RandomPlace(int max) => (Rand.Next(max), Rand.Next(max));
    void Reset()
    {
        TheMap.Clear();
        Bodies.Clear();
        Dir = SpeedDirection.None;
        TheMap[0, 0] = TileType.Head;
        Head = default;
        Food = default;
        MoveTimer.Interval = _optMotor.StartingSpeed;
        Bodies.Enqueue(Head);
        while (Food == default)
            Food = RandomPlace(_optMaps.SideLength);
        TheMap[Food] = TileType.Food;
        MoveWaiter.Set();
        _board.ResetAndReRenderAll(TheMap);
    }
}
