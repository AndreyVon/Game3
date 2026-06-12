using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardAnimator
{
    private TileSpawner tileSpawner;

    public BoardAnimator(TileSpawner tileSpawner)
    {
        this.tileSpawner = tileSpawner;
    }

    public IEnumerator AnimateAndSwapTileContentsRoutine(
        Tile firstTile,
        Tile secondTile,
        float swapDuration
    )
    {
        TileView firstView = firstTile.View;
        TileView secondView = secondTile.View;

        if (firstView == null || secondView == null)
        {
            Debug.LogWarning("BoardAnimator: у одной из клеток нет TileView");
            yield break;
        }

        int firstType = firstTile.Type;
        int secondType = secondTile.Type;

        Vector3 firstStartPosition = firstView.transform.position;
        Vector3 secondStartPosition = secondView.transform.position;

        Vector3 firstTargetPosition = tileSpawner.GetWorldPosition(secondTile.X, secondTile.Y);
        Vector3 secondTargetPosition = tileSpawner.GetWorldPosition(firstTile.X, firstTile.Y);

        firstView.SetSpeedTrailActive(true);
        secondView.SetSpeedTrailActive(true);

        float elapsedTime = 0f;

        while (elapsedTime < swapDuration)
        {
            elapsedTime += Time.deltaTime;

            float t = elapsedTime / swapDuration;
            t = Mathf.Clamp01(t);

            float easedT = Mathf.SmoothStep(0f, 1f, t);

            firstView.transform.position = Vector3.Lerp(
                firstStartPosition,
                firstTargetPosition,
                easedT
            );

            secondView.transform.position = Vector3.Lerp(
                secondStartPosition,
                secondTargetPosition,
                easedT
            );

            yield return null;
        }

        firstView.transform.position = firstTargetPosition;
        secondView.transform.position = secondTargetPosition;

        firstView.SetSpeedTrailActive(false);
        secondView.SetSpeedTrailActive(false);

        firstTile.View = secondView;
        secondTile.View = firstView;

        firstTile.Type = secondType;
        secondTile.Type = firstType;

        firstTile.View.Initialize(firstTile);
        secondTile.View.Initialize(secondTile);

        firstTile.View.transform.position = tileSpawner.GetWorldPosition(firstTile.X, firstTile.Y);
        secondTile.View.transform.position = tileSpawner.GetWorldPosition(secondTile.X, secondTile.Y);

        firstTile.View.Deselect();
        secondTile.View.Deselect();

        firstTile.View.RefreshName();
        secondTile.View.RefreshName();

        Debug.Log($"Swap завершён: ({firstTile.X}, {firstTile.Y}) Type = {firstTile.Type}, ({secondTile.X}, {secondTile.Y}) Type = {secondTile.Type}");
    }

    public IEnumerator RemoveMatchedTilesRoutine(List<Tile> matchedTiles, float removeDuration)
    {
        if (matchedTiles == null || matchedTiles.Count == 0)
        {
            yield break;
        }

        Debug.Log($"BoardAnimator: удаляем {matchedTiles.Count} овощей");

        Dictionary<TileView, Vector3> originalScales = new Dictionary<TileView, Vector3>();

        for (int i = 0; i < matchedTiles.Count; i++)
        {
            Tile tile = matchedTiles[i];

            if (tile != null && tile.View != null)
            {
                if (!originalScales.ContainsKey(tile.View))
                {
                    originalScales.Add(tile.View, tile.View.transform.localScale);
                }
            }
        }

        float elapsedTime = 0f;

        while (elapsedTime < removeDuration)
        {
            elapsedTime += Time.deltaTime;

            float t = elapsedTime / removeDuration;
            t = Mathf.Clamp01(t);

            foreach (KeyValuePair<TileView, Vector3> pair in originalScales)
            {
                TileView tileView = pair.Key;

                if (tileView != null)
                {
                    tileView.transform.localScale = Vector3.Lerp(pair.Value, Vector3.zero, t);
                }
            }

            yield return null;
        }

        for (int i = 0; i < matchedTiles.Count; i++)
        {
            Tile tile = matchedTiles[i];

            if (tile == null)
            {
                continue;
            }

            if (tile.View != null)
            {
                Object.Destroy(tile.View.gameObject);
            }

            tile.View = null;
            tile.Type = -1;

            Debug.Log($"Клетка очищена: ({tile.X}, {tile.Y})");
        }
    }

    public IEnumerator AnimateMovementsRoutine(List<TileMovement> movements, float duration)
    {
        if (movements == null || movements.Count == 0)
        {
            yield break;
        }

        for (int i = 0; i < movements.Count; i++)
        {
            TileMovement movement = movements[i];

            if (movement != null && movement.View != null)
            {
                movement.View.SetSpeedTrailActive(true);
            }
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float t = elapsedTime / duration;
            t = Mathf.Clamp01(t);

            float easedT = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < movements.Count; i++)
            {
                TileMovement movement = movements[i];

                if (movement != null && movement.View != null)
                {
                    movement.View.transform.position = Vector3.Lerp(
                        movement.StartPosition,
                        movement.TargetPosition,
                        easedT
                    );
                }
            }

            yield return null;
        }

        for (int i = 0; i < movements.Count; i++)
        {
            TileMovement movement = movements[i];

            if (movement != null && movement.View != null)
            {
                movement.View.transform.position = movement.TargetPosition;
                movement.View.SetSpeedTrailActive(false);
            }
        }
    }
}