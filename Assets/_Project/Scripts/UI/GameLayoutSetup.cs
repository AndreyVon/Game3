using UnityEngine;
using UnityEngine.UI;

public class GameLayoutSetup : MonoBehaviour
{
    [Header("References")]
    public Canvas canvas;
    public Image boardBackground;
    public RectTransform boardArea;
    public RectTransform topPanel;
    public RectTransform scoreBar;

    [Header("Board Settings")]
    public int boardWidth = 8;
    public int boardHeight = 8;
    public float boardPadding = 20f;

    [Header("Layout")]
    public float topPanelHeight = 200f;
    public float scoreBarHeight = 60f;

    private CanvasScaler canvasScaler;

    private void Awake()
    {
        canvasScaler = canvas.GetComponent<CanvasScaler>();
        SetupLayout();
    }

    private void SetupLayout()
    {
        if (canvasScaler != null)
        {
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
        }

        SetupBoardArea();
        SetupTopPanel();
        SetupScoreBar();
    }

    private void SetupBoardArea()
    {
        if (boardArea == null) return;

        boardArea.anchorMin = new Vector2(0.5f, 0.5f);
        boardArea.anchorMax = new Vector2(0.5f, 0.5f);
        boardArea.pivot = new Vector2(0.5f, 0.5f);

        float availableHeight = 1080f - topPanelHeight - scoreBarHeight - boardPadding * 2;
        float availableWidth = 1920f - boardPadding * 2;

        float cellSize = Mathf.Min(
            availableWidth / boardWidth,
            availableHeight / boardHeight
        );

        float boardWidthPixels = cellSize * boardWidth;
        float boardHeightPixels = cellSize * boardHeight;

        boardArea.sizeDelta = new Vector2(boardWidthPixels, boardHeightPixels);
        boardArea.anchoredPosition = new Vector2(0, -topPanelHeight / 2 + scoreBarHeight / 2);

        if (boardBackground != null)
        {
            boardBackground.rectTransform.sizeDelta = boardArea.sizeDelta;
        }
    }

    private void SetupTopPanel()
    {
        if (topPanel == null) return;

        topPanel.anchorMin = new Vector2(0, 1);
        topPanel.anchorMax = new Vector2(1, 1);
        topPanel.pivot = new Vector2(0.5f, 1);
        topPanel.sizeDelta = new Vector2(0, topPanelHeight);
        topPanel.anchoredPosition = Vector2.zero;
    }

    private void SetupScoreBar()
    {
        if (scoreBar == null) return;

        scoreBar.anchorMin = new Vector2(0.5f, 1);
        scoreBar.anchorMax = new Vector2(0.5f, 1);
        scoreBar.pivot = new Vector2(0.5f, 1);
        scoreBar.sizeDelta = new Vector2(800, scoreBarHeight);
        scoreBar.anchoredPosition = new Vector2(0, -topPanelHeight);
    }

    public float GetCellSize()
    {
        float availableHeight = 1080f - topPanelHeight - scoreBarHeight - boardPadding * 2;
        float availableWidth = 1920f - boardPadding * 2;

        return Mathf.Min(
            availableWidth / boardWidth,
            availableHeight / boardHeight
        );
    }

    public Vector2 GetTilePosition(int x, int y)
    {
        float cellSize = GetCellSize();
        float startX = -boardArea.sizeDelta.x / 2 + cellSize / 2;
        float startY = boardArea.sizeDelta.y / 2 - cellSize / 2;

        return new Vector2(
            startX + x * cellSize,
            startY - y * cellSize
        );
    }
}
