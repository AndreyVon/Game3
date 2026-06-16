public class Tile
{
    public int X { get; private set; }
    public int Y { get; private set; }

    public int Type { get; set; }
    public bool IsGlowing { get; set; }

    public TileView View { get; set; }

    public Tile(int x, int y, int type)
    {
        X = x;
        Y = y;
        Type = type;
        IsGlowing = false;
    }
}