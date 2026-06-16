using System.Collections.Generic;

public class BoardBonusResult
{
    public BoardBonusType BonusType;
    public List<Tile> TilesToRemove;
    public int RowY = -1;
    public int ColumnX = -1;
    public int ClearType = -1;

    public bool HasBonus
    {
        get { return BonusType != BoardBonusType.None; }
    }
}