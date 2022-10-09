using Microsoft.Extensions.Options;

class Renderer
{
    // public class RenderArgs : EventArgs
    // {
    //     public (int X, int Y) Position { get; set; }
    //     internal TileType TileType { get; set; }
    // }
    // public event EventHandler? Render;
    readonly VisualMap _mapOpts;
    readonly Visual _visual;
    readonly Gameplay _opt;
    // readonly HighScore _hs;
    readonly int _xOffset;
    // readonly Dashboard _db;
    public Renderer(IOptions<Config> cfg) //, Dashboard db, HighScore hs, 
    {
        // _db = db;
        (_opt, _, _, _visual, _mapOpts) = cfg.Value;
        if (_visual.UseDashboard) _xOffset = 4;
        // Render += (sender, args) =>
        // {
        // (int x, int y) = args.Position;
        // Console.SetCursorPosition(y, x);
        // Console.Write(args.Character);
        // };
    }
    public void UpdatePoint(int x, int y, TileType value)
    {
        Console.SetCursorPosition(y * 2, x + _xOffset);
        Console.Write(TileToString(value));
    }

    private string TileToString(TileType tile) => tile switch
    {
        TileType.Crate => _mapOpts.Crate,
        TileType.Head => _mapOpts.Head,
        TileType.Body => _mapOpts.Body,
        _ => _mapOpts.None
    };

    public void CleanMap(IMap map, HighScore _hs, Dashboard _db)
    {
        Console.Clear();
        if (_visual.UseDashboard) RendorDashboard(_hs, _db);
        for (int i = 0; i <= map.BottomBound; i++)
        {
            var arr = map[i].ToArray().Select(q => TileToString(q));
            Console.WriteLine(string.Join(' ', arr));
        }
        Console.WriteLine();
    }

    private void RendorDashboard(HighScore _hs, Dashboard _db)
    {
        //Level, Speed, Length, Time, HighScore
        var headers = new List<string> { "Speed", "Len", "Time ", "HighScore" };
        if (_opt.UseLevel) headers.Insert(0, "Lvl");
        var headline = $"| {string.Join(" | ", headers)} |";
        var separator = new string(headline.Select(q => q == '|' ? '|' : '-').ToArray());

        var lens = headers.Select(q => q.Length).ToArray();
        var vals = new List<string> { _db.SpeedDisplay, _db.CurrentLength + "", _db.Sw.Elapsed.ToString("mm\\:ss"), _hs + "" };
        if (_opt.UseLevel) vals.Insert(0, _db.Level + "");
        var qq = lens.Zip(vals, (q, w) => w.PadLeft(q));
        var bodyline = $"| {string.Join(" | ", qq)} |";

        Console.WriteLine(headline);
        Console.WriteLine(separator);
        Console.WriteLine(bodyline);
        Console.WriteLine();
    }
    public void UpdatePoint(int y, string value)
    {
        Console.SetCursorPosition(y, 2);
        Console.Write(value);
    }
}
