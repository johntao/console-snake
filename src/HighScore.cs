using Microsoft.Extensions.Options;

class HighScore
{
    public int MaxLength;
    public string MinTime = "99:99";
    readonly int _startLength;
    readonly Gameplay _opt;
    readonly Renderer _rdr;
    readonly int _yOffset;
    public HighScore(IOptions<Config> cfg, Renderer rdr)
    {
        _rdr = rdr;
        (_opt, _, _, _, _) = cfg.Value;
        if (_opt.UseLevel) _yOffset = 7;
        _startLength = cfg.Value.Gameplay.StartingLength;
    }
    //| Lvl | Speed | Len | Time | HighScore |
    private string _highScoreText = string.Empty;
    public string HighScoreText
    {
        get => _highScoreText;
        private set
        {
            _highScoreText = value;
            if (_opt.UseLevel) _rdr.RendorDashboardPartial(23 + _yOffset, (value + "").PadLeft(9));
        }
    }
    public void SetHighScore(Dashboard board)
    {
        board.Sw.Stop();
        var time = board.Sw.Elapsed.ToString("mm\\:ss");
        board.Sw.Reset();
        bool isFirstRun = board.CurrentLength == 0;
        if (isFirstRun || board.CurrentLength < MaxLength)
        {
            board.CurrentLength = _startLength;
        }
        else if (board.CurrentLength == MaxLength)
        {
            board.CurrentLength = _startLength;
            if (time.CompareTo(MinTime) < 0)
                MinTime = time;
        }
        else if (board.CurrentLength > MaxLength)
        {
            MaxLength = board.CurrentLength;
            board.CurrentLength = _startLength;
            MinTime = time;
        }
        HighScoreText = $"{MaxLength}@{MinTime}";
    }
    public override string ToString() => HighScoreText;
}
