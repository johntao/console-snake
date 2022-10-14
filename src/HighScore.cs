using Microsoft.Extensions.Options;

class HighScore
{
    public int MaxLength;
    public string MinTime = "99:99";
    readonly int _startLength;
    readonly Gameplay _optGameplay;
    readonly DivDashboard _div;
    public HighScore(IOptions<Config> cfgRoot, DivDashboard div)
    {
        _div = div;
        (_optGameplay, _, _, _, _) = cfgRoot.Value;
        _startLength = cfgRoot.Value.Gameplay.StartingLength;
    }
    //| Lvl | Speed | Len | Time | HighScore |
    private string _highScoreText = string.Empty;
    public string HighScoreText
    {
        get => _highScoreText;
        private set
        {
            _highScoreText = value;
            _div.PrintPartial(DashboardColumn.HighScore);
        }
    }
    public void SetHighScore(Dashboard board)
    {
        board.Stopwatch.Stop();
        var time = board.Stopwatch.Elapsed.ToString("mm\\:ss");
        board.Stopwatch.Reset();
        bool isFirstRun = board.CurrentSnakeLength == 0;
        if (isFirstRun || board.CurrentSnakeLength < MaxLength)
        {
            board.CurrentSnakeLength = _startLength;
        }
        else if (board.CurrentSnakeLength == MaxLength)
        {
            board.CurrentSnakeLength = _startLength;
            if (time.CompareTo(MinTime) < 0)
                MinTime = time;
        }
        else if (board.CurrentSnakeLength > MaxLength)
        {
            MaxLength = board.CurrentSnakeLength;
            board.CurrentSnakeLength = _startLength;
            MinTime = time;
        }
        HighScoreText = $"{MaxLength}@{MinTime}";
    }
    public override string ToString() => HighScoreText;
}
