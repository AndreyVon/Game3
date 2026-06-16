using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardCollapseFiller
{
    private Tile[,] board;
    private int width;
    private int height;
    private TileSpawner tileSpawner;
    private BoardAnimator boardAnimator;
    private float fallDuration;

    public BoardCollapseFiller(
        Tile[,] board,
        int width,
        int height,
        TileSpawner tileSpawner,
        BoardAnimator boardAnimator,
        float fallDuration
    )
    {
        this.board = board;
        this.width = width;
        this.height = height;
        this.tileSpawner = tileSpawner;
        this.boardAnimator = boardAnimator;
        this.fallDuration = fallDuration;
    }

    public IEnumerator CollapseColumnsRoutine()
    {
        List<TileMovement> movements = new List<TileMovement>();

        for (int x = 0; x < width; x++)
        {
            int targetY = height - 1;

            for (int currentY = height - 1; currentY >= 0; currentY--)
            {
                Tile currentTile = board[x, currentY];

                if (currentTile == null)
                {
                    continue;
                }

                if (currentTile.Type >= 0 && currentTile.View != null)
                {
                    if (currentY != targetY)
                    {
                        Tile targetTile = board[x, targetY];

                        if (targetTile == null)
                        {
                            targetTile = new Tile(x, targetY, -1);
                            board[x, targetY] = targetTile;
                        }

                        TileView movingView = currentTile.View;
                        int movingType = currentTile.Type;
                        bool movingIsGlowing = currentTile.IsGlowing;

                        Vector3 startPosition = movingView.transform.position;
                        Vector3 targetPosition = tileSpawner.GetWorldPosition(x, targetY);

                        targetTile.Type = movingType;
                        targetTile.IsGlowing = movingIsGlowing;
                        targetTile.View = movingView;

                        currentTile.Type = -1;
                        currentTile.IsGlowing = false;
                        currentTile.View = null;

                        movingView.Initialize(targetTile);
                        movingView.RefreshName();
                        movingView.Deselect();

                        movements.Add(new TileMovement(movingView, startPosition, targetPosition));
                    }

                    targetY--;
                }
            }

            for (int emptyY = targetY; emptyY >= 0; emptyY--)
            {
                Tile emptyTile = board[x, emptyY];

                if (emptyTile == null)
                {
                    emptyTile = new Tile(x, emptyY, -1);
                    board[x, emptyY] = emptyTile;
                }

                emptyTile.Type = -1;
                emptyTile.IsGlowing = false;
                emptyTile.View = null;
            }
        }

        if (movements.Count == 0)
        {
            Debug.Log("BoardCollapseFiller: ������ ������");
            yield break;
        }

        yield return boardAnimator.AnimateMovementsRoutine(movements, fallDuration);
    }

    public IEnumerator FillEmptyCellsRoutine()
    {
        List<TileMovement> movements = new List<TileMovement>();

        for (int x = 0; x < width; x++)
        {
            int spawnedInColumn = 0;

            for (int y = height - 1; y >= 0; y--)
            {
                Tile tile = board[x, y];

                if (tile == null)
                {
                    tile = new Tile(x, y, -1);
                    board[x, y] = tile;
                }

                if (tile.Type < 0 || tile.View == null)
                {
                    int randomType = tileSpawner.GetRandomTypeWithoutImmediateMatch(x, y);

                    bool isGlowing = tileSpawner.ShouldCreateGlowingVegetable(randomType);

                    Vector3 targetPosition = tileSpawner.GetWorldPosition(x, y);

                    Vector3 spawnPosition = tileSpawner.GetSpawnPositionAboveBoard(x, spawnedInColumn + 1);

                    TileView tileView = tileSpawner.CreateVegetableView(
                        x,
                        y,
                        randomType,
                        isGlowing,
                        spawnPosition
                    );

                    if (tileView == null)
                    {
                        Debug.LogWarning($"BoardCollapseFiller: �� ������� ������� ���� � ({x}, {y})");
                        continue;
                    }

                    tileView.Initialize(tile);
                    tileView.RefreshName();
                    tileView.Deselect();

                    movements.Add(new TileMovement(tileView, spawnPosition, targetPosition));

                    spawnedInColumn++;

                    Debug.Log(
                        $"������ ����� ����: ({x}, {y}) Type = {randomType}, Glowing = {isGlowing}"
                    );
                }
            }
        }

        if (movements.Count == 0)
        {
            Debug.Log("BoardCollapseFiller: ������ ������ ���");
            yield break;
        }

        yield return boardAnimator.AnimateMovementsRoutine(movements, fallDuration);
    }
}