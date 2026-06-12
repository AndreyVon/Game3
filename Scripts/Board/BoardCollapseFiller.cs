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
            int targetY = 0;

            for (int currentY = 0; currentY < height; currentY++)
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
                            Debug.LogWarning($"BoardCollapseFiller: targetTile == null в ({x}, {targetY})");
                            continue;
                        }

                        TileView movingView = currentTile.View;
                        int movingType = currentTile.Type;

                        Vector3 startPosition = movingView.transform.position;
                        Vector3 targetPosition = tileSpawner.GetWorldPosition(x, targetY);

                        targetTile.Type = movingType;
                        targetTile.View = movingView;

                        currentTile.Type = -1;
                        currentTile.View = null;

                        movingView.Initialize(targetTile);
                        movingView.RefreshName();
                        movingView.Deselect();

                        movements.Add(new TileMovement(movingView, startPosition, targetPosition));

                        Debug.Log($"ќвощ падает: ({x}, {currentY}) -> ({x}, {targetY})");
                    }

                    targetY++;
                }
            }

            for (int emptyY = targetY; emptyY < height; emptyY++)
            {
                Tile emptyTile = board[x, emptyY];

                if (emptyTile != null)
                {
                    emptyTile.Type = -1;
                    emptyTile.View = null;
                }
            }
        }

        if (movements.Count == 0)
        {
            Debug.Log("BoardCollapseFiller: падать нечему");
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

            for (int y = 0; y < height; y++)
            {
                Tile tile = board[x, y];

                if (tile == null)
                {
                    continue;
                }

                if (tile.Type < 0 || tile.View == null)
                {
                    int randomType = tileSpawner.GetRandomTypeWithoutImmediateMatch(x, y);

                    tile.Type = randomType;

                    Vector3 targetPosition = tileSpawner.GetWorldPosition(x, y);

                    int spawnY = height + spawnedInColumn;
                    Vector3 spawnPosition = tileSpawner.GetWorldPosition(x, spawnY);

                    TileView tileView = tileSpawner.CreateVegetableView(randomType, spawnPosition);

                    tile.View = tileView;

                    tileView.Initialize(tile);
                    tileView.RefreshName();
                    tileView.Deselect();

                    movements.Add(new TileMovement(tileView, spawnPosition, targetPosition));

                    spawnedInColumn++;

                    Debug.Log($"—оздан новый овощ: ({x}, {y}) Type = {randomType}");
                }
            }
        }

        if (movements.Count == 0)
        {
            Debug.Log("BoardCollapseFiller: пустых клеток нет");
            yield break;
        }

        yield return boardAnimator.AnimateMovementsRoutine(movements, fallDuration);
    }
}