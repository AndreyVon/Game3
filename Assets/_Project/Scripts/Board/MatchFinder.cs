using System.Collections.Generic;
using UnityEngine;

public class MatchFinder
{
    private Tile[,] board;
    private int width;
    private int height;

    public MatchFinder(Tile[,] board, int width, int height)
    {
        this.board = board;
        this.width = width;
        this.height = height;
    }

    public List<Tile> FindMatchesCreatedBySwap(Tile firstTile, Tile secondTile)
    {
        List<Tile> result = new List<Tile>();

        List<Tile> firstTileMatches = FindMatchesForTile(firstTile);
        List<Tile> secondTileMatches = FindMatchesForTile(secondTile);

        AddUniqueTiles(result, firstTileMatches);
        AddUniqueTiles(result, secondTileMatches);

        return result;
    }

    public List<Tile> FindMatchesForTile(Tile tile)
    {
        List<Tile> result = new List<Tile>();

        if (tile == null || tile.Type < 0)
        {
            return result;
        }

        List<Tile> horizontalMatches = FindHorizontalMatchesForTile(tile);
        List<Tile> verticalMatches = FindVerticalMatchesForTile(tile);

        AddUniqueTiles(result, horizontalMatches);
        AddUniqueTiles(result, verticalMatches);

        return result;
    }

    private List<Tile> FindHorizontalMatchesForTile(Tile tile)
    {
        List<Tile> matches = new List<Tile>();

        if (tile == null || tile.Type < 0)
        {
            return matches;
        }

        int type = tile.Type;

        List<Tile> line = new List<Tile>();
        line.Add(tile);

        int x = tile.X - 1;

        while (x >= 0)
        {
            Tile currentTile = board[x, tile.Y];

            if (currentTile != null && currentTile.Type == type && currentTile.Type >= 0)
            {
                line.Add(currentTile);
                x--;
            }
            else
            {
                break;
            }
        }

        x = tile.X + 1;

        while (x < width)
        {
            Tile currentTile = board[x, tile.Y];

            if (currentTile != null && currentTile.Type == type && currentTile.Type >= 0)
            {
                line.Add(currentTile);
                x++;
            }
            else
            {
                break;
            }
        }

        if (line.Count >= 3)
        {
            AddUniqueTiles(matches, line);
        }

        return matches;
    }

    private List<Tile> FindVerticalMatchesForTile(Tile tile)
    {
        List<Tile> matches = new List<Tile>();

        if (tile == null || tile.Type < 0)
        {
            return matches;
        }

        int type = tile.Type;

        List<Tile> line = new List<Tile>();
        line.Add(tile);

        int y = tile.Y - 1;

        while (y >= 0)
        {
            Tile currentTile = board[tile.X, y];

            if (currentTile != null && currentTile.Type == type && currentTile.Type >= 0)
            {
                line.Add(currentTile);
                y--;
            }
            else
            {
                break;
            }
        }

        y = tile.Y + 1;

        while (y < height)
        {
            Tile currentTile = board[tile.X, y];

            if (currentTile != null && currentTile.Type == type && currentTile.Type >= 0)
            {
                line.Add(currentTile);
                y++;
            }
            else
            {
                break;
            }
        }

        if (line.Count >= 3)
        {
            AddUniqueTiles(matches, line);
        }

        return matches;
    }

    public List<Tile> FindAllMatches()
    {
        List<Tile> matchedTiles = new List<Tile>();

        FindHorizontalMatches(matchedTiles);
        FindVerticalMatches(matchedTiles);

        return matchedTiles;
    }

    private void FindHorizontalMatches(List<Tile> matchedTiles)
    {
        for (int y = 0; y < height; y++)
        {
            int matchLength = 1;

            for (int x = 1; x < width; x++)
            {
                Tile currentTile = board[x, y];
                Tile previousTile = board[x - 1, y];

                if (
                    currentTile != null &&
                    previousTile != null &&
                    currentTile.Type >= 0 &&
                    previousTile.Type >= 0 &&
                    currentTile.Type == previousTile.Type
                )
                {
                    matchLength++;
                }
                else
                {
                    if (matchLength >= 3)
                    {
                        AddHorizontalMatch(matchedTiles, x - matchLength, y, matchLength);
                    }

                    matchLength = 1;
                }
            }

            if (matchLength >= 3)
            {
                AddHorizontalMatch(matchedTiles, width - matchLength, y, matchLength);
            }
        }
    }

    private void FindVerticalMatches(List<Tile> matchedTiles)
    {
        for (int x = 0; x < width; x++)
        {
            int matchLength = 1;

            for (int y = 1; y < height; y++)
            {
                Tile currentTile = board[x, y];
                Tile previousTile = board[x, y - 1];

                if (
                    currentTile != null &&
                    previousTile != null &&
                    currentTile.Type >= 0 &&
                    previousTile.Type >= 0 &&
                    currentTile.Type == previousTile.Type
                )
                {
                    matchLength++;
                }
                else
                {
                    if (matchLength >= 3)
                    {
                        AddVerticalMatch(matchedTiles, x, y - matchLength, matchLength);
                    }

                    matchLength = 1;
                }
            }

            if (matchLength >= 3)
            {
                AddVerticalMatch(matchedTiles, x, height - matchLength, matchLength);
            }
        }
    }

    private void AddHorizontalMatch(List<Tile> matchedTiles, int startX, int y, int length)
    {
        for (int i = 0; i < length; i++)
        {
            Tile tile = board[startX + i, y];

            if (tile != null && tile.Type >= 0 && !matchedTiles.Contains(tile))
            {
                matchedTiles.Add(tile);
            }
        }
    }

    private void AddVerticalMatch(List<Tile> matchedTiles, int x, int startY, int length)
    {
        for (int i = 0; i < length; i++)
        {
            Tile tile = board[x, startY + i];

            if (tile != null && tile.Type >= 0 && !matchedTiles.Contains(tile))
            {
                matchedTiles.Add(tile);
            }
        }
    }

    private void AddUniqueTiles(List<Tile> targetList, List<Tile> tilesToAdd)
    {
        for (int i = 0; i < tilesToAdd.Count; i++)
        {
            Tile tile = tilesToAdd[i];

            if (tile != null && !targetList.Contains(tile))
            {
                targetList.Add(tile);
            }
        }
    }

    public bool HasAnyMatches()
    {
        List<Tile> matches = FindAllMatches();

        if (matches.Count > 0)
        {
            Debug.Log($"Найдено совпадений: {matches.Count}");
            LogMatches(matches);
            return true;
        }

        Debug.Log("Совпадений нет");
        return false;
    }

    public void LogMatches(List<Tile> matches)
    {
        if (matches == null)
        {
            return;
        }

        foreach (Tile tile in matches)
        {
            if (tile != null)
            {
                Debug.Log($"Match Tile: ({tile.X}, {tile.Y}) Type = {tile.Type}");
            }
        }
    }
}