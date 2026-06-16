using System.Collections.Generic;
using UnityEngine;

public class BoardBonusResolver
{
    private readonly Tile[,] board;
    private readonly int width;
    private readonly int height;

    public BoardBonusResolver(Tile[,] board, int width, int height)
    {
        this.board = board;
        this.width = width;
        this.height = height;
    }

    public BoardBonusResult ResolveBonus(List<Tile> currentMatches)
    {
        BoardBonusResult result = new BoardBonusResult
        {
            BonusType = BoardBonusType.None,
            TilesToRemove = currentMatches
        };

        Tile glowingMatchedTile = FindGlowingTileInMatches(currentMatches);

        if (glowingMatchedTile != null)
        {
            result.BonusType = BoardBonusType.GlowingClearType;
            result.ClearType = glowingMatchedTile.Type;
            result.TilesToRemove = GetAllTilesOfType(glowingMatchedTile.Type);

            Debug.Log($"Светящийся овощ: найден Type {glowingMatchedTile.Type}. Уничтожаем все овощи этого типа.");
            Debug.Log($"Светящийся овощ: всего овощей для удаления: {result.TilesToRemove.Count}");

            return result;
        }

        List<Tile> horizontalFiveMatch = FindHorizontalMatchOfAtLeastFive();
        List<Tile> verticalFiveMatch = FindVerticalMatchOfAtLeastFive();

        if (horizontalFiveMatch != null)
        {
            result.BonusType = BoardBonusType.CrossCut;
            result.RowY = GetMatchRowY(horizontalFiveMatch);
            result.TilesToRemove = MergeUniqueTiles(currentMatches, GetCrossDiagonalTiles());

            Debug.Log("Крестовой разрез: найдено 5+ в ряд по горизонтали. Атакуем диагонали.");
            Debug.Log($"Крестовой разрез: в удаление добавлены обе диагонали, всего овощей: {result.TilesToRemove.Count}");

            return result;
        }

        if (verticalFiveMatch != null)
        {
            result.BonusType = BoardBonusType.CrossCut;
            result.ColumnX = GetMatchColumnX(verticalFiveMatch);
            result.TilesToRemove = MergeUniqueTiles(currentMatches, GetCrossDiagonalTiles());

            Debug.Log("Крестовой разрез: найдено 5+ в ряд по вертикали. Атакуем диагонали.");
            Debug.Log($"Крестовой разрез: в удаление добавлены обе диагонали, всего овощей: {result.TilesToRemove.Count}");

            return result;
        }

        List<Tile> horizontalFourMatch = FindHorizontalMatchOfExactlyFour();
        List<Tile> verticalFourMatch = FindVerticalMatchOfExactlyFour();

        if (horizontalFourMatch != null)
        {
            result.BonusType = BoardBonusType.HorizontalKnife;
            result.RowY = GetMatchRowY(horizontalFourMatch);
            result.TilesToRemove = GetFullRowTiles(result.RowY);

            Debug.Log($"Острый нож: найдено 4 в ряд по горизонтали. Уничтожаем весь ряд Y: {result.RowY}");
            Debug.Log($"Острый нож: в ряд добавлено овощей для удаления: {result.TilesToRemove.Count}");

            return result;
        }

        if (verticalFourMatch != null)
        {
            result.BonusType = BoardBonusType.VerticalKnife;
            result.ColumnX = GetMatchColumnX(verticalFourMatch);
            result.TilesToRemove = GetFullColumnTiles(result.ColumnX);

            Debug.Log($"Острый нож: найдено 4 в ряд по вертикали. Уничтожаем весь столбец X: {result.ColumnX}");
            Debug.Log($"Острый нож: в столбец добавлено овощей для удаления: {result.TilesToRemove.Count}");

            return result;
        }

        return result;
    }

    private Tile FindGlowingTileInMatches(List<Tile> currentMatches)
    {
        if (currentMatches == null)
        {
            return null;
        }

        for (int i = 0; i < currentMatches.Count; i++)
        {
            Tile tile = currentMatches[i];

            if (tile != null && tile.Type >= 0 && tile.IsGlowing)
            {
                return tile;
            }
        }

        return null;
    }

    private List<Tile> GetAllTilesOfType(int type)
    {
        List<Tile> tiles = new List<Tile>();

        if (type < 0)
        {
            return tiles;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile tile = board[x, y];

                if (tile != null && tile.Type == type && tile.View != null)
                {
                    tiles.Add(tile);
                }
            }
        }

        return tiles;
    }

    private List<Tile> FindHorizontalMatchOfExactlyFour()
    {
        return FindHorizontalMatchByLength(4, false);
    }

    private List<Tile> FindVerticalMatchOfExactlyFour()
    {
        return FindVerticalMatchByLength(4, false);
    }

    private List<Tile> FindHorizontalMatchOfAtLeastFive()
    {
        return FindHorizontalMatchByLength(5, true);
    }

    private List<Tile> FindVerticalMatchOfAtLeastFive()
    {
        return FindVerticalMatchByLength(5, true);
    }

    private List<Tile> FindHorizontalMatchByLength(int targetLength, bool atLeast)
    {
        for (int y = 0; y < height; y++)
        {
            int x = 0;

            while (x < width)
            {
                Tile startTile = board[x, y];

                if (startTile == null || startTile.Type < 0)
                {
                    x++;
                    continue;
                }

                int type = startTile.Type;
                int startX = x;
                int matchLength = 1;

                x++;

                while (
                    x < width &&
                    board[x, y] != null &&
                    board[x, y].Type == type
                )
                {
                    matchLength++;
                    x++;
                }

                bool isNeededMatch = atLeast
                    ? matchLength >= targetLength
                    : matchLength == targetLength;

                if (isNeededMatch)
                {
                    List<Tile> matchTiles = new List<Tile>();

                    for (int matchX = startX; matchX < startX + matchLength; matchX++)
                    {
                        matchTiles.Add(board[matchX, y]);
                    }

                    Debug.Log($"Найден горизонтальный матч из {matchLength} овощей. Ряд Y: {y}, X: {startX}-{startX + matchLength - 1}");

                    return matchTiles;
                }
            }
        }

        return null;
    }

    private List<Tile> FindVerticalMatchByLength(int targetLength, bool atLeast)
    {
        for (int x = 0; x < width; x++)
        {
            int y = 0;

            while (y < height)
            {
                Tile startTile = board[x, y];

                if (startTile == null || startTile.Type < 0)
                {
                    y++;
                    continue;
                }

                int type = startTile.Type;
                int startY = y;
                int matchLength = 1;

                y++;

                while (
                    y < height &&
                    board[x, y] != null &&
                    board[x, y].Type == type
                )
                {
                    matchLength++;
                    y++;
                }

                bool isNeededMatch = atLeast
                    ? matchLength >= targetLength
                    : matchLength == targetLength;

                if (isNeededMatch)
                {
                    List<Tile> matchTiles = new List<Tile>();

                    for (int matchY = startY; matchY < startY + matchLength; matchY++)
                    {
                        matchTiles.Add(board[x, matchY]);
                    }

                    Debug.Log($"Найден вертикальный матч из {matchLength} овощей. Столбец X: {x}, Y: {startY}-{startY + matchLength - 1}");

                    return matchTiles;
                }
            }
        }

        return null;
    }

    private List<Tile> GetFullRowTiles(int rowY)
    {
        List<Tile> rowTiles = new List<Tile>();

        if (rowY < 0 || rowY >= height)
        {
            return rowTiles;
        }

        for (int x = 0; x < width; x++)
        {
            AddTileToList(rowTiles, x, rowY);
        }

        return rowTiles;
    }

    private List<Tile> GetFullColumnTiles(int columnX)
    {
        List<Tile> columnTiles = new List<Tile>();

        if (columnX < 0 || columnX >= width)
        {
            return columnTiles;
        }

        for (int y = 0; y < height; y++)
        {
            AddTileToList(columnTiles, columnX, y);
        }

        return columnTiles;
    }

    private List<Tile> GetCrossDiagonalTiles()
    {
        HashSet<Tile> uniqueTiles = new HashSet<Tile>();

        int steps = Mathf.Min(width, height);

        for (int i = 0; i < steps; i++)
        {
            AddTileToSet(uniqueTiles, i, height - 1 - i);
            AddTileToSet(uniqueTiles, width - 1 - i, height - 1 - i);
        }

        return new List<Tile>(uniqueTiles);
    }

    private void AddTileToList(List<Tile> tiles, int x, int y)
    {
        if (!IsInsideBoard(x, y))
        {
            return;
        }

        Tile tile = board[x, y];

        if (tile == null || tile.Type < 0 || tile.View == null)
        {
            return;
        }

        tiles.Add(tile);
    }

    private void AddTileToSet(HashSet<Tile> tiles, int x, int y)
    {
        if (!IsInsideBoard(x, y))
        {
            return;
        }

        Tile tile = board[x, y];

        if (tile == null || tile.Type < 0 || tile.View == null)
        {
            return;
        }

        tiles.Add(tile);
    }

    private List<Tile> MergeUniqueTiles(List<Tile> first, List<Tile> second)
    {
        HashSet<Tile> uniqueTiles = new HashSet<Tile>();

        if (first != null)
        {
            foreach (Tile tile in first)
            {
                if (tile != null)
                {
                    uniqueTiles.Add(tile);
                }
            }
        }

        if (second != null)
        {
            foreach (Tile tile in second)
            {
                if (tile != null)
                {
                    uniqueTiles.Add(tile);
                }
            }
        }

        return new List<Tile>(uniqueTiles);
    }

    private int GetMatchRowY(List<Tile> matchedTiles)
    {
        if (matchedTiles == null || matchedTiles.Count == 0)
        {
            return -1;
        }

        return matchedTiles[0].Y;
    }

    private int GetMatchColumnX(List<Tile> matchedTiles)
    {
        if (matchedTiles == null || matchedTiles.Count == 0)
        {
            return -1;
        }

        return matchedTiles[0].X;
    }

    private bool IsInsideBoard(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
}