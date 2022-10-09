using Microsoft.Extensions.Options;
using Microsoft.Toolkit.HighPerformance;

interface IMap
{
    //__|y_____
    // x| → →
    // ↓|
    // ↓|
    public int TopBound { get; } // zero-based
    public int BottomBound { get; } // zero-based
    public int LeftBound { get; } // zero-based
    public int RightBound { get; } // zero-based
    public int Length { get; }
    public TileType this[int x, int y] { get; set; }
    public TileType this[(int x, int y) point] { get; set; }
    public Renderer Renderer { get; }
    public ReadOnlySpan<TileType> this[int x] { get; }
    public void Clear();
}
class Map2dArray : IMap
{
    private readonly TileType[,] _map;
    public int TopBound { get; } // zero-based
    public int BottomBound { get; } // zero-based
    public int LeftBound { get; } // zero-based
    public int RightBound { get; } // zero-based
    public int Length { get; }
    public bool IsStart { get; set; }
    public Renderer Renderer { get; }
    public Map2dArray(IOptions<Config> cfg, Renderer renderer)
    {
        var opt = cfg.Value.VisualMap;
        _map = new TileType[opt.SideLength, opt.SideLength];
        TopBound = 0;
        BottomBound = opt.SideLength - 1;
        LeftBound = 0;
        RightBound = opt.SideLength - 1;
        Length = opt.SideLength * opt.SideLength - 1;
        Renderer = renderer;
    }
    public TileType this[int x, int y]
    {
        get => _map[x, y];
        set
        {
            _map[x, y] = value;
            Renderer.RendorMapPartial(x, y, value);
        }
    }
    public ReadOnlySpan<TileType> this[int x]
    {
        get
        {
            // 0 1 2 | 0*3
            // 3 4 5 | 1*3
            // 6 7 8 | 2*3
            var width = RightBound + 1;
            var rtn = _map.AsSpan()[(x * width)..width];
            return rtn;
        }
    }
    public TileType this[(int x, int y) point]
    {
        get => _map[point.x, point.y];
        set
        {
            _map[point.x, point.y] = value;
            Renderer.RendorMapPartial(point.x, point.y, value);
        }
    }
    public void Clear()
    {
        Array.Clear(_map, 0, _map.Length);
    }
}

class MapJaggedArray : IMap
{
    public bool IsStart { get; set; }
    private readonly TileType[][] _map;
    public int TopBound { get; } // zero-based
    public int BottomBound { get; } // zero-based
    public int LeftBound { get; } // zero-based
    public int RightBound { get; } // zero-based
    public int Length { get; }
    public Renderer Renderer { get; }
    public MapJaggedArray(IOptions<Config> cfg, Renderer renderer)
    {
        var opt = cfg.Value.VisualMap;
        _map = new TileType[opt.SideLength][];
        for (int i = 0; i < opt.SideLength; i++)
            _map[i] = new TileType[opt.SideLength];
        TopBound = 0;
        BottomBound = opt.SideLength - 1;
        LeftBound = 0;
        RightBound = opt.SideLength - 1;
        Length = opt.SideLength * opt.SideLength - 1;
        Renderer = renderer;
    }
    public TileType this[int x, int y]
    {
        get => _map[x][y];
        set
        {
            _map[x][y] = value;
            Renderer.RendorMapPartial(x, y, value);
        }
    }
    public ReadOnlySpan<TileType> this[int x] => _map.AsSpan(x)[0];
    public TileType this[(int x, int y) point]
    {
        get => _map[point.x][point.y];
        set
        {
            _map[point.x][point.y] = value;
            Renderer.RendorMapPartial(point.x, point.y, value);
        }
    }
    public void Clear()
    {
        foreach (var item in _map)
            Array.Clear(item, 0, item.Length);
    }
}
