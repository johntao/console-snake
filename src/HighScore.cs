using Microsoft.Extensions.Options;

class HighScore
{
    public int MaxLength;
    public string MinTime = "99:99";
    readonly int _startLength;
    readonly Dashboard _db;
    readonly Gameplay _opt;
    readonly Renderer _rdr;
    readonly int _yOffset;
    public HighScore(IOptions<Config> cfg, Dashboard db, Renderer rdr)
    {
        _rdr = rdr;
        (_opt, _, _, _, _) = cfg.Value;
        if (_opt.UseLevel) _yOffset = 7;
        _db = db;
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
            if (_opt.UseLevel) _rdr.UpdatePoint(23 + _yOffset, (value + "").PadLeft(9));
        }
    }
    public void SetHighScore()
    {
        _db.Sw.Stop();
        var time = _db.Sw.Elapsed.ToString("mm\\:ss");
        _db.Sw.Reset();
        bool isFirstRun = _db.CurrentLength == 0;
        if (isFirstRun || _db.CurrentLength < MaxLength)
        {
            _db.CurrentLength = _startLength;
        }
        else if (_db.CurrentLength == MaxLength)
        {
            _db.CurrentLength = _startLength;
            if (time.CompareTo(MinTime) < 0)
                MinTime = time;
        }
        else if (_db.CurrentLength > MaxLength)
        {
            MaxLength = _db.CurrentLength;
            _db.CurrentLength = _startLength;
            MinTime = time;
        }
        HighScoreText = $"{MaxLength}@{MinTime}";
    }
    public override string ToString() => HighScoreText;
}
