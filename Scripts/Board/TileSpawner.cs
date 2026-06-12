using System.Collections.Generic;
using UnityEngine;

public class TileSpawner
{
    private Tile[,] board;
    private int width;
    private int height;
    private float tileSpacing;
    private float boardVerticalOffset;
    private GameObject[] vegetablePrefabs;
    private Transform parent;

    public TileSpawner(
        Tile[,] board,
        int width,
        int height,
        float tileSpacing,
        float boardVerticalOffset,
        GameObject[] vegetablePrefabs,
        Transform parent
    )
    {
        this.board = board;
        this.width = width;
        this.height = height;
        this.tileSpacing = tileSpacing;
        this.boardVerticalOffset = boardVerticalOffset;
        this.vegetablePrefabs = vegetablePrefabs;
        this.parent = parent;
    }

    public void CreateInitialTile(int x, int y)
    {
        int randomType = GetRandomTypeWithoutStartingMatch(x, y);

        Tile tile = new Tile(x, y, randomType);

        Vector3 position = GetWorldPosition(x, y);

        TileView tileView = CreateVegetableView(randomType, position);

        tile.View = tileView;

        tileView.Initialize(tile);
        tileView.RefreshName();

        board[x, y] = tile;
    }

    public TileView CreateVegetableView(int type, Vector3 position)
    {
        GameObject vegetable = UnityEngine.Object.Instantiate(
            vegetablePrefabs[type],
            position,
            Quaternion.identity,
            parent
        );

        vegetable.transform.localScale = new Vector3(0.3f, 0.3f, 1f);

        Collider2D collider = vegetable.GetComponent<Collider2D>();

        if (collider == null)
        {
            BoxCollider2D boxCollider = vegetable.AddComponent<BoxCollider2D>();
            boxCollider.size = new Vector2(2.5f, 2.5f);
        }

        TileView tileView = vegetable.GetComponent<TileView>();

        if (tileView == null)
        {
            tileView = vegetable.AddComponent<TileView>();
        }

        return tileView;
    }

    public int GetRandomTypeWithoutImmediateMatch(int x, int y)
    {
        List<int> availableTypes = new List<int>();

        for (int i = 0; i < vegetablePrefabs.Length; i++)
        {
            availableTypes.Add(i);
        }

        ShuffleList(availableTypes);

        for (int i = 0; i < availableTypes.Count; i++)
        {
            int type = availableTypes[i];

            if (!WouldCreateMatchAt(x, y, type))
            {
                return type;
            }
        }

        return Random.Range(0, vegetablePrefabs.Length);
    }

    private int GetRandomTypeWithoutStartingMatch(int x, int y)
    {
        List<int> availableTypes = new List<int>();

        for (int i = 0; i < vegetablePrefabs.Length; i++)
        {
            availableTypes.Add(i);
        }

        ShuffleList(availableTypes);

        for (int i = 0; i < availableTypes.Count; i++)
        {
            int type = availableTypes[i];

            if (!WouldCreateStartingMatch(x, y, type))
            {
                return type;
            }
        }

        Debug.LogWarning($"TileSpawner: не удалось подобрать тип без совпадения для клетки ({x}, {y}). Используем случайный тип.");

        return Random.Range(0, vegetablePrefabs.Length);
    }

    private bool WouldCreateStartingMatch(int x, int y, int type)
    {
        bool createsHorizontalMatch = false;
        bool createsVerticalMatch = false;

        if (x >= 2)
        {
            Tile tile1 = board[x - 1, y];
            Tile tile2 = board[x - 2, y];

            if (tile1 != null && tile2 != null)
            {
                if (tile1.Type == type && tile2.Type == type)
                {
                    createsHorizontalMatch = true;
                }
            }
        }

        if (y >= 2)
        {
            Tile tile1 = board[x, y - 1];
            Tile tile2 = board[x, y - 2];

            if (tile1 != null && tile2 != null)
            {
                if (tile1.Type == type && tile2.Type == type)
                {
                    createsVerticalMatch = true;
                }
            }
        }

        return createsHorizontalMatch || createsVerticalMatch;
    }

    private bool WouldCreateMatchAt(int x, int y, int type)
    {
        int horizontalCount = 1;

        int checkX = x - 1;

        while (checkX >= 0 && board[checkX, y] != null && board[checkX, y].Type == type)
        {
            horizontalCount++;
            checkX--;
        }

        checkX = x + 1;

        while (checkX < width && board[checkX, y] != null && board[checkX, y].Type == type)
        {
            horizontalCount++;
            checkX++;
        }

        if (horizontalCount >= 3)
        {
            return true;
        }

        int verticalCount = 1;

        int checkY = y - 1;

        while (checkY >= 0 && board[x, checkY] != null && board[x, checkY].Type == type)
        {
            verticalCount++;
            checkY--;
        }

        checkY = y + 1;

        while (checkY < height && board[x, checkY] != null && board[x, checkY].Type == type)
        {
            verticalCount++;
            checkY++;
        }

        return verticalCount >= 3;
    }

    private void ShuffleList(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);

            int temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    public Vector3 GetWorldPosition(int x, int y)
    {
        Vector2 boardOffset = new Vector2(
            -((width - 1) * tileSpacing) / 2f,
            -((height - 1) * tileSpacing) / 2f
        );

        return new Vector3(
            x * tileSpacing + boardOffset.x,
            y * tileSpacing + boardOffset.y + boardVerticalOffset,
            0f
        );
    }
}