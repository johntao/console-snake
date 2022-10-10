using Microsoft.Extensions.Options;

class Renderer
{
    readonly VisualMap _optMap;
    readonly Visual _optVisual;
    readonly Gameplay _optGameplay;
    readonly int _xOffset;
    public Renderer(IOptions<Config> cfgRoot)
    {
        (_optGameplay, _, _, _optVisual, _optMap) = cfgRoot.Value;
        if (_optVisual.UseDashboard) _xOffset = 4;
    }
    public void RendorMapPartial(int row, int col, TileType value)
    {
        Console.SetCursorPosition(col * 2, row + _xOffset);
        Console.Write(TileToString(value));
    }

    private string TileToString(TileType tile) => tile switch
    {
        TileType.Food => _optMap.Crate,
        TileType.Head => _optMap.Head,
        TileType.Body => _optMap.Body,
        _ => _optMap.None
    };

    public void ClearAll(IMap map, HighScore highScore, Dashboard board)
    {
        Console.Clear();
        if (_optVisual.UseDashboard) RendorDashboard(highScore, board);
        for (int i = 0; i <= map.BottomBound; i++)
        {
            var arr = map[i].ToArray().Select(q => TileToString(q));
            Console.WriteLine(string.Join(' ', arr));
        }
        Console.WriteLine();
    }

    private void RendorDashboard(HighScore highScore, Dashboard board)
    {
        //Level, Speed, Length, Time, HighScore
        var headers = new List<string> { "Speed ", "Len", "Time ", "HighScore" };
        if (_optGameplay.UseLevel) headers.Insert(0, "Lvl");
        var headline = $"| {string.Join(" | ", headers)} |";
        var separator = new string(headline.Select(q => q == '|' ? '|' : '-').ToArray());

        var lens = headers.Select(q => q.Length).ToArray();
        var vals = new List<string> { board.SpeedDisplay, board.CurrentSnakeLength + "", board.Stopwatch.Elapsed.ToString("mm\\:ss"), highScore + "" };
        if (_optGameplay.UseLevel) vals.Insert(0, board.Level + "");
        var valWithPads = lens.Zip(vals, (q, w) => w.PadLeft(q));
        var bodyline = $"| {string.Join(" | ", valWithPads)} |";

        Console.WriteLine(headline);
        Console.WriteLine(separator);
        Console.WriteLine(bodyline);
        Console.WriteLine();
    }
    public void RendorDashboardPartial(int col, string value)
    {
        Console.SetCursorPosition(col, 2);
        Console.Write(value);
    }
}
