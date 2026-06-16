# Canvas Layout Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move all game elements (board, UI, background) into a single Canvas with proper scaling so everything stays correctly positioned at any screen resolution.

**Architecture:** Replace world-space board with a UI Image inside Canvas. All tiles become RectTransform children of the board area. UI elements (pot, timer, goals, score) are repositioned within the same Canvas hierarchy. CanvasScaler handles all scaling.

**Tech Stack:** Unity 2022.3.7f1, UI Toolkit (Canvas + Image + RectTransform), TextMeshPro

---

## File Structure

| Action | File | Purpose |
|--------|------|---------|
| Modify | `Assets/_Project/Scripts/Board/BoardManager.cs` | Replace SpriteRenderer references with UI Image, adjust tile spawning |
| Modify | `Assets/_Project/Scripts/Board/TileSpawner.cs` | Create tiles as RectTransform children, position in Canvas units |
| Modify | `Assets/_Project/Scripts/Board/TileView.cs` | Ensure tiles work as RectTransform (UI Image) instead of SpriteRenderer |
| Modify | `Assets/_Project/Scripts/FitBackgroundToCamera.cs` | Replace with Canvas-aware scaling or remove (Canvas handles it) |
| Modify | `Assets/_Project/Scenes/GameScene.unity` | Restructure scene hierarchy, reparent elements into Canvas |
| Create | `Assets/_Project/Scripts/UI/GameLayoutSetup.cs` | Runtime layout configuration script |

---

### Task 1: Create GameLayoutSetup Script

**Covers:** S1 (Canvas hierarchy), S2 (scaling)

**Files:**
- Create: `Assets/_Project/Scripts/UI/GameLayoutSetup.cs`

- [ ] **Step 1: Create the GameLayoutSetup script**

```csharp
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
```

- [ ] **Step 2: Verify script compiles**

Run: Open Unity Editor, check Console for compilation errors
Expected: No errors

---

### Task 2: Modify TileSpawner for Canvas-based Tiles

**Covers:** S3 (tile positioning), S4 (tile creation)

**Files:**
- Modify: `Assets/_Project/Scripts/Board/TileSpawner.cs`

- [ ] **Step 1: Add RectTransform-based tile creation method**

Add after the `SetBoardBounds` method (around line 65):

```csharp
private GameLayoutSetup layoutSetup;
private bool useCanvasLayout;

public void SetCanvasLayout(GameLayoutSetup layout)
{
    this.layoutSetup = layout;
    this.useCanvasLayout = true;
}

public GameObject CreateCanvasTile(int x, int y, GameObject prefab, Transform parent)
{
    GameObject tileObj = new GameObject($"Tile_{x}_{y}");
    tileObj.transform.SetParent(parent, false);

    RectTransform rectTransform = tileObj.AddComponent<RectTransform>();
    Image image = tileObj.AddComponent<Image>();

    Vector2 position = layoutSetup.GetTilePosition(x, y);
    rectTransform.anchoredPosition = position;

    float cellSize = layoutSetup.GetCellSize();
    rectTransform.sizeDelta = new Vector2(cellSize * 0.9f, cellSize * 0.9f);

    if (prefab != null)
    {
        SpriteRenderer prefabSprite = prefab.GetComponentInChildren<SpriteRenderer>();
        if (prefabSprite != null && prefabSprite.sprite != null)
        {
            image.sprite = prefabSprite.sprite;
        }
    }

    return tileObj;
}
```

- [ ] **Step 2: Modify GetWorldPosition to support canvas layout**

Replace the existing `GetWorldPosition` method:

```csharp
public Vector3 GetWorldPosition(int x, int y)
{
    if (useCanvasLayout && layoutSetup != null)
    {
        Vector2 pos = layoutSetup.GetTilePosition(x, y);
        return new Vector3(pos.x, pos.y, 0f);
    }

    float z = 0f;

    if (useBoardBounds)
    {
        z = boardSpriteRenderer.transform.position.z;
    }
    else if (parent != null)
    {
        z = parent.position.z;
    }
    else if (gridOrigin != null)
    {
        z = gridOrigin.position.z;
    }

    if (useBoardBounds && boardSpriteRenderer != null)
    {
        return GetBoardBoundedPosition(x, y, z);
    }

    float startX = parent.position.x - ((width - 1) * tileSpacingX * 0.5f);
    float startY = parent.position.y + boardVerticalOffset + ((height - 1) * tileSpacingY * 0.5f);

    float posX = startX + x * tileSpacingX;
    float posY = startY - y * tileSpacingY;

    return new Vector3(posX, posY, z);
}
```

- [ ] **Step 3: Verify script compiles**

Run: Open Unity Editor, check Console for compilation errors
Expected: No errors

---

### Task 3: Modify TileView for UI Image Support

**Covers:** S5 (tile rendering)

**Files:**
- Modify: `Assets/_Project/Scripts/Tiles/TileView.cs`

- [ ] **Step 1: Add UI Image support to TileView**

Read the current TileView.cs first, then add UI Image handling:

```csharp
using UnityEngine;
using UnityEngine.UI;

public class TileView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Image uiImage;

    private Color normalColor = Color.white;
    private Color selectedColor = new Color(0.8f, 0.8f, 1f);

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (uiImage == null)
            uiImage = GetComponent<Image>();
    }

    public void SetSprite(Sprite sprite)
    {
        if (uiImage != null)
            uiImage.sprite = sprite;
        else if (spriteRenderer != null)
            spriteRenderer.sprite = sprite;
    }

    public void SetColor(Color color)
    {
        if (uiImage != null)
            uiImage.color = color;
        else if (spriteRenderer != null)
            spriteRenderer.color = color;
    }

    public void Select()
    {
        SetColor(selectedColor);
    }

    public void Deselect()
    {
        SetColor(normalColor);
    }

    public void SetSortOrder(int order)
    {
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = order;
    }
}
```

- [ ] **Step 2: Verify script compiles**

Run: Open Unity Editor, check Console for compilation errors
Expected: No errors

---

### Task 4: Modify BoardManager for Canvas Integration

**Covers:** S1 (hierarchy), S6 (integration)

**Files:**
- Modify: `Assets/_Project/Scripts/Board/BoardManager.cs`

- [ ] **Step 1: Add Canvas layout support to BoardManager**

In the `BoardManager.cs` file, add new fields after the existing header fields:

```csharp
[Header("Canvas Layout")]
public GameLayoutSetup layoutSetup;
public RectTransform boardArea;
```

- [ ] **Step 2: Modify AutoFindSceneReferences to find layout**

In `AutoFindSceneReferences()` method, add after the existing references:

```csharp
if (layoutSetup == null)
{
    layoutSetup = FindObjectOfType<GameLayoutSetup>();
}

if (boardArea == null && layoutSetup != null)
{
    boardArea = layoutSetup.boardArea;
}
```

- [ ] **Step 3: Modify InitializeBoardSystems to use canvas layout**

In `InitializeBoardSystems()` method, add canvas layout support:

```csharp
private void InitializeBoardSystems()
{
    board = new Tile[width, height];

    bool useCanvas = layoutSetup != null && boardArea != null;

    if (useCanvas)
    {
        tileSpawner = new TileSpawner(
            board,
            width,
            height,
            tileSpacingX,
            tileSpacingY,
            boardVerticalOffset,
            vegetablePrefabs,
            glowingVegetableSprites,
            boardArea,
            glowingVegetableChance
        );

        tileSpawner.SetCanvasLayout(layoutSetup);
    }
    else
    {
        Transform gridOrigin = null;

        if (AreMarkersReady())
        {
            gridOrigin = topLeftCellCenter;
        }

        tileSpawner = new TileSpawner(
            board,
            width,
            height,
            tileSpacingX,
            tileSpacingY,
            boardVerticalOffset,
            vegetablePrefabs,
            glowingVegetableSprites,
            transform,
            glowingVegetableChance,
            gridOrigin
        );

        if (boardSpriteRenderer != null)
        {
            tileSpawner.SetBoardBounds(boardSpriteRenderer, boardGridSize, boardGridOffset);
        }
    }

    matchFinder = new MatchFinder(board, width, height);
    boardAnimator = new BoardAnimator(tileSpawner);

    boardCollapseFiller = new BoardCollapseFiller(
        board,
        width,
        height,
        tileSpawner,
        boardAnimator,
        fallDuration
    );

    boardBonusResolver = new BoardBonusResolver(board, width, height);

    if (boardBonusEffects != null)
    {
        boardBonusEffects.Initialize(tileSpawner, width, height);
    }
}
```

- [ ] **Step 4: Modify InitializeBoard to create UI tiles**

In `InitializeBoard()` method, modify the tile creation loop:

```csharp
private void InitializeBoard()
{
    if (vegetablePrefabs == null || vegetablePrefabs.Length == 0)
    {
        Debug.LogError("BoardManager: vegetablePrefabs пустой.");
        return;
    }

    bool useCanvas = layoutSetup != null && boardArea != null;

    for (int x = 0; x < width; x++)
    {
        for (int y = 0; y < height; y++)
        {
            if (useCanvas)
            {
                CreateCanvasTile(x, y);
            }
            else
            {
                tileSpawner.CreateInitialTile(x, y);
            }
        }
    }

    matchFinder.HasAnyMatches();

    if (!HasPossibleMoves())
    {
        StartCoroutine(ShuffleBoardUntilPlayableRoutine());
    }
}

private void CreateCanvasTile(int x, int y)
{
    int randomType = Random.Range(0, vegetablePrefabs.Length);

    GameObject tileObj = tileSpawner.CreateCanvasTile(x, y, vegetablePrefabs[randomType], boardArea);

    TileView view = tileObj.GetComponent<TileView>();
    if (view == null)
        view = tileObj.AddComponent<TileView>();

    Image image = tileObj.GetComponent<Image>();
    if (image != null && vegetablePrefabs[randomType] != null)
    {
        SpriteRenderer prefabSprite = vegetablePrefabs[randomType].GetComponentInChildren<SpriteRenderer>();
        if (prefabSprite != null)
            image.sprite = prefabSprite.sprite;
    }

    Tile tile = new Tile(x, y, randomType);
    tile.View = view;
    board[x, y] = tile;
}
```

- [ ] **Step 5: Verify script compiles**

Run: Open Unity Editor, check Console for compilation errors
Expected: No errors

---

### Task 5: Update Scene Hierarchy

**Covers:** S1 (scene structure), S7 (visual layout)

**Files:**
- Modify: `Assets/_Project/Scenes/GameScene.unity`

- [ ] **Step 1: Create new Canvas hierarchy in Unity Editor**

In Unity Editor:
1. Delete existing Canvas object
2. Create new Canvas with CanvasScaler (Scale With Screen Size, 1920x1080, Match=0.5)
3. Create child objects:
   - TopPanel (with HorizontalLayoutGroup)
   - ScoreBar
   - BoardArea (with Image component)
4. Add GameLayoutSetup component to Canvas
5. Assign references in GameLayoutSetup

- [ ] **Step 2: Reparent UI elements**

Move existing UI elements into new hierarchy:
1. TimerUI → TopPanel
2. BorschtPotUI → TopPanel
3. LevelGoalUI → TopPanel
4. ScoreManager/ScoreText → ScoreBar
5. WinLoseScreen → Canvas (overlay)

- [ ] **Step 3: Configure BoardArea**

1. Set BoardArea Image to board background sprite
2. Set anchoring to center
3. Configure LayoutElement with preferred width/height

- [ ] **Step 4: Test layout in Editor**

1. Enter Play mode
2. Verify all elements scale correctly
3. Check tile positioning within BoardArea

---

### Task 6: Adjust FitBackgroundToCamera

**Covers:** S8 (background scaling)

**Files:**
- Modify: `Assets/_Project/Scripts/FitBackgroundToCamera.cs`

- [ ] **Step 1: Update script for Canvas-aware background**

```csharp
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class FitBackgroundToCanvas : MonoBehaviour
{
    private Image image;
    private Canvas canvas;

    private void Awake()
    {
        image = GetComponent<Image>();
        canvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        FitToCanvas();
    }

    private void FitToCanvas()
    {
        if (canvas == null || image == null) return;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        RectTransform thisRect = GetComponent<RectTransform>();

        thisRect.anchorMin = Vector2.zero;
        thisRect.anchorMax = Vector2.one;
        thisRect.sizeDelta = Vector2.zero;
        thisRect.anchoredPosition = Vector2.zero;
    }
}
```

- [ ] **Step 2: Replace FitBackgroundToCamera in scene**

1. Remove FitBackgroundToCamera component from background object
2. Add FitBackgroundToCanvas component
3. Ensure background is child of Canvas with stretch anchors

- [ ] **Step 3: Verify background scales correctly**

Run: Enter Play mode, resize game window
Expected: Background fills entire canvas without distortion

---

### Task 7: Final Testing and Cleanup

**Covers:** S9 (verification)

**Files:**
- Verify: All modified scripts

- [ ] **Step 1: Full integration test**

1. Enter Play mode
2. Play through a complete level
3. Verify:
   - Board scales correctly at different resolutions
   - Pot stays in position
   - Timer displays correctly
   - Goals show properly
   - Score updates correctly
   - Win/lose screens appear correctly

- [ ] **Step 2: Resolution testing**

Test at multiple resolutions:
1. 1920x1080 (reference)
2. 1280x720
3. 3840x2160
4. Mobile resolutions (1080x1920 portrait)

- [ ] **Step 3: Clean up debug code**

Remove any debug logs or test code added during development.

- [ ] **Step 4: Commit changes**

```bash
git add Assets/_Project/Scripts/
git commit -m "fix: migrate game layout to Canvas-based system for proper scaling"
```

---

## Verification Checklist

After completing all tasks:

- [ ] Board tiles scale proportionally at all resolutions
- [ ] Pot (BorschtPotUI) stays centered at top
- [ ] Timer displays correctly in top-left
- [ ] Vegetable goals show properly in top-right
- [ ] Score bar appears below the board
- [ ] Background fills entire screen
- [ ] Win/lose screens overlay correctly
- [ ] No visual glitches during gameplay
- [ ] All UI elements maintain relative positions
