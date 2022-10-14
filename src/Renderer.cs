using Microsoft.Extensions.Options;

abstract class DivBase
{
    protected int RowOffset { get; set; }
    protected int MarginBottom { get; set; } = 1;
    protected void PrintMargin()
    {
        for (int i = 0; i < MarginBottom; i++)
            Console.WriteLine();
    }
}
internal class DivDashboard : DivBase
{
    public List<Span> Items { get; set; } = new List<Span>();
    readonly Gameplay _optGameplay;
    public DivDashboard(IOptions<Config> cfg)
    {
        (_optGameplay, _, _, _, _) = cfg.Value;
    }
    internal void Print()
    {
        var col = 2;
        Console.WriteLine($"| {string.Join(" | ", Items.Select(q => q.Name))} |");
        Console.WriteLine($"|-{string.Join("-|-", Items.Select(q => new string('-', q.PaddingSize)))}-|");
        Console.WriteLine($"| {string.Join(" | ", Items.Select(q => q.Value().PadLeft(q.PaddingSize)))} |");
        foreach (var item in Items)
        {
            item.Col = col;
            col += item.PaddingSize + 3;
        }
        PrintMargin();
    }
    internal void PrintPartial(DashboardColumn dc)
    {
        int v;
        if (dc is DashboardColumn.Level)
            v = 0;
        else
            v = (int)dc + (_optGameplay.UseLevel ? 0 : -1);
        Items[v].Update();
    }
}
internal class DivMap : DivBase
{
    readonly VisualMap OptMap;
    public DivMap(IOptions<Config> cfg)
    {
        (_, _, _, var visual, OptMap) = cfg.Value;
        if (visual.UseDashboard) RowOffset = 4;
    }
    internal void PrintPartial(int row, int col, TileType value)
    {
        Console.SetCursorPosition(col * 2, row + RowOffset);
        Console.Write(TileToString(value));
    }
    internal void Print(IMap map)
    {
        for (int i = 0; i <= map.BottomBound; i++)
        {
            var arr = map[i].ToArray().Select(q => TileToString(q));
            Console.WriteLine(string.Join(' ', arr));
        }
        PrintMargin();
    }
    private string TileToString(TileType tile) => tile switch
    {
        TileType.Food => OptMap.Crate,
        TileType.Head => OptMap.Head,
        TileType.Body => OptMap.Body,
        _ => OptMap.None
    };
}
internal class Span
{
    public int Col { get; set; }
    public string Name { get; set; } = default!;
    public Func<string> Value { get; set; } = default!;
    public int PaddingSize => Name.Length;
    public void Update()
    {
        Console.SetCursorPosition(Col, 2);
        Console.Write(Value().PadLeft(PaddingSize));
    }
}
