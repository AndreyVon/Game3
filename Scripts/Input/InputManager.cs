using UnityEngine;

public class InputManager : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public BoardManager boardManager;

    private Tile selectedTile;

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (boardManager == null)
        {
            boardManager = FindObjectOfType<BoardManager>();
        }

        if (mainCamera == null)
        {
            Debug.LogError("InputManager: mainCamera не найдена");
        }

        if (boardManager == null)
        {
            Debug.LogError("InputManager: boardManager не найден");
        }
    }

    private void Update()
    {
        if (boardManager == null || mainCamera == null)
        {
            return;
        }

        if (boardManager.IsBusy)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    private void HandleClick()
    {
        Vector3 mousePosition = Input.mousePosition;
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);
        worldPosition.z = 0f;

        Debug.Log($"InputManager: WorldPos = ({worldPosition.x:F2}, {worldPosition.y:F2})");

        RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);

        if (hit.collider == null)
        {
            Debug.Log("InputManager: клик мимо овощей");
            DeselectCurrentTile();
            return;
        }

        TileView clickedTileView = hit.collider.GetComponent<TileView>();

        if (clickedTileView == null)
        {
            clickedTileView = hit.collider.GetComponentInParent<TileView>();
        }

        if (clickedTileView == null)
        {
            Debug.Log("InputManager: на объекте нет TileView");
            DeselectCurrentTile();
            return;
        }

        Tile clickedTile = clickedTileView.Tile;

        if (clickedTile == null)
        {
            Debug.Log("InputManager: clickedTile == null");
            DeselectCurrentTile();
            return;
        }

        Debug.Log($"InputManager: нажата клетка ({clickedTile.X}, {clickedTile.Y}) Type = {clickedTile.Type}");

        if (selectedTile == null)
        {
            SelectTile(clickedTile);
            return;
        }

        if (selectedTile == clickedTile)
        {
            DeselectCurrentTile();
            return;
        }

        if (boardManager.AreNeighbors(selectedTile, clickedTile))
        {
            Tile tileToSwap = selectedTile;

            DeselectCurrentTile();

            boardManager.SwapTiles(tileToSwap, clickedTile);
        }
        else
        {
            DeselectCurrentTile();
            SelectTile(clickedTile);
        }
    }

    private void SelectTile(Tile tile)
    {
        selectedTile = tile;

        if (selectedTile != null && selectedTile.View != null)
        {
            selectedTile.View.Select();
        }

        Debug.Log($"InputManager: выбрана клетка ({tile.X}, {tile.Y}) Type = {tile.Type}");
    }

    private void DeselectCurrentTile()
    {
        if (selectedTile != null && selectedTile.View != null)
        {
            selectedTile.View.Deselect();
        }

        selectedTile = null;
    }
}