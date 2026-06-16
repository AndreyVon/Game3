using UnityEngine;

public class TileSpawner
{
    private readonly Tile[,] board;

    private readonly int width;
    private readonly int height;

    private readonly float tileSpacingX;
    private readonly float tileSpacingY;
    private readonly float boardVerticalOffset;

    private readonly GameObject[] vegetablePrefabs;
    private readonly Sprite[] glowingVegetableSprites;

    private readonly Transform parent;
    private readonly float glowingVegetableChance;

    private readonly Transform gridOrigin;

    private bool useBoardBounds;
    private SpriteRenderer boardSpriteRenderer;
    private Vector2 boardGridSize;
    private Vector2 boardGridOffset;

    private GameLayoutSetup layoutSetup;
    private bool useCanvasLayout;

    public TileSpawner(
        Tile[,] board,
        int width,
        int height,
        float tileSpacingX,
        float tileSpacingY,
        float boardVerticalOffset,
        GameObject[] vegetablePrefabs,
        Sprite[] glowingVegetableSprites,
        Transform parent,
        float glowingVegetableChance,
        Transform gridOrigin = null
    )
    {
        this.board = board;

        this.width = width;
        this.height = height;

        this.tileSpacingX = Mathf.Abs(tileSpacingX);
        this.tileSpacingY = Mathf.Abs(tileSpacingY);
        this.boardVerticalOffset = boardVerticalOffset;

        this.vegetablePrefabs = vegetablePrefabs;
        this.glowingVegetableSprites = glowingVegetableSprites;

        this.parent = parent;
        this.glowingVegetableChance = glowingVegetableChance;

        this.gridOrigin = gridOrigin;
    }

    public void SetBoardBounds(SpriteRenderer spriteRenderer, Vector2 sizeOverride, Vector2 offset)
    {
        this.boardSpriteRenderer = spriteRenderer;
        this.boardGridSize = sizeOverride;
        this.boardGridOffset = offset;
        this.useBoardBounds = true;
    }

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
        UnityEngine.UI.Image image = tileObj.AddComponent<UnityEngine.UI.Image>();

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

        if (useBoardBounds)
        {
            return GetBoardBoundsWorldPosition(x, y, z);
        }

        if (gridOrigin != null)
        {
            return new Vector3(
                gridOrigin.position.x + x * tileSpacingX,
                gridOrigin.position.y - y * tileSpacingY,
                z
            );
        }

        Vector3 parentPosition = parent != null ? parent.position : Vector3.zero;

        float startX = parentPosition.x - ((width - 1) * tileSpacingX * 0.5f);
        float startY = parentPosition.y + boardVerticalOffset + ((height - 1) * tileSpacingY * 0.5f);

        return new Vector3(
            startX + x * tileSpacingX,
            startY - y * tileSpacingY,
            z
        );
    }

    private Vector3 GetBoardBoundsWorldPosition(int x, int y, float z)
    {
        Vector3 localPos = GetBoardBoundsLocalPosition(x, y);
        return boardSpriteRenderer.transform.TransformPoint(localPos);
    }

    private Vector3 GetBoardBoundsLocalPosition(int x, int y)
    {
        Vector2 gridSize;
        if (boardGridSize.x > 0 && boardGridSize.y > 0)
        {
            gridSize = boardGridSize;
        }
        else
        {
            gridSize = boardSpriteRenderer.sprite.rect.size / boardSpriteRenderer.sprite.pixelsPerUnit;
        }

        float cellWidth = gridSize.x / width;
        float cellHeight = gridSize.y / height;

        float halfW = gridSize.x * 0.5f;
        float halfH = gridSize.y * 0.5f;

        float localX = -halfW + cellWidth * (x + 0.5f) + boardGridOffset.x;
        float localY = halfH - cellHeight * (y + 0.5f) + boardGridOffset.y;

        return new Vector3(localX, localY, 0f);
    }

    private Transform GetVegetableParent()
    {
        if (useBoardBounds)
        {
            return boardSpriteRenderer.transform;
        }
        return parent;
    }

    public Vector3 GetSpawnPositionAboveBoard(int x, int extraRowsAbove = 1)
    {
        if (useBoardBounds)
        {
            Vector3 topRowWorld = GetBoardBoundsWorldPosition(x, 0, 0);
            Vector3 secondRowWorld = GetBoardBoundsWorldPosition(x, 1, 0);
            float rowSpacing = Vector3.Distance(topRowWorld, secondRowWorld);
            Vector3 aboveDir = (topRowWorld - secondRowWorld).normalized;
            return topRowWorld + aboveDir * rowSpacing * extraRowsAbove;
        }

        int spawnY = -Mathf.Max(1, extraRowsAbove);
        return GetWorldPosition(x, spawnY);
    }

    public void CreateInitialTile(int x, int y)
    {
        if (!IsInsideBoard(x, y))
        {
            Debug.LogWarning($"TileSpawner: попытка создать стартовый овощ вне поля ({x}, {y}).");
            return;
        }

        int type = GetRandomTypeWithoutInitialMatch(x, y);
        bool isGlowing = ShouldCreateGlowingVegetable(type);

        CreateVegetableView(
            x,
            y,
            type,
            isGlowing,
            GetWorldPosition(x, y)
        );
    }

    public Tile CreateTile(int x, int y)
    {
        CreateRandomTile(x, y);

        return IsInsideBoard(x, y) ? board[x, y] : null;
    }

    public Tile CreateTile(int x, int y, Vector3 spawnPosition)
    {
        CreateRandomTileAtPosition(x, y, spawnPosition);

        return IsInsideBoard(x, y) ? board[x, y] : null;
    }

    public void CreateRandomTile(int x, int y)
    {
        if (!IsInsideBoard(x, y))
        {
            Debug.LogWarning($"TileSpawner: попытка создать овощ вне поля ({x}, {y}).");
            return;
        }

        int type = GetRandomTypeWithoutImmediateMatch(x, y);
        bool isGlowing = ShouldCreateGlowingVegetable(type);

        CreateVegetableView(
            x,
            y,
            type,
            isGlowing,
            GetWorldPosition(x, y)
        );
    }

    public void CreateRandomTileAtPosition(int x, int y, Vector3 spawnPosition)
    {
        if (!IsInsideBoard(x, y))
        {
            Debug.LogWarning($"TileSpawner: попытка создать овощ вне поля ({x}, {y}).");
            return;
        }

        int type = GetRandomTypeWithoutImmediateMatch(x, y);
        bool isGlowing = ShouldCreateGlowingVegetable(type);

        CreateVegetableView(
            x,
            y,
            type,
            isGlowing,
            spawnPosition
        );
    }

    public TileView CreateVegetableView(int x, int y, Vector3 position)
    {
        int type = GetRandomTypeWithoutImmediateMatch(x, y);
        bool isGlowing = ShouldCreateGlowingVegetable(type);

        return CreateVegetableView(
            x,
            y,
            type,
            isGlowing,
            position
        );
    }

    public TileView CreateVegetableView(int x, int y, int type, Vector3 position)
    {
        bool isGlowing = ShouldCreateGlowingVegetable(type);

        return CreateVegetableView(
            x,
            y,
            type,
            isGlowing,
            position
        );
    }

    public TileView CreateVegetableView(int x, int y, int type, bool isGlowing, Vector3 position)
    {
        if (!IsInsideBoard(x, y))
        {
            Debug.LogWarning($"TileSpawner: попытка создать View вне поля ({x}, {y}).");
            return null;
        }

        if (vegetablePrefabs == null || vegetablePrefabs.Length == 0)
        {
            Debug.LogError("TileSpawner: vegetablePrefabs пустой.");
            return null;
        }

        if (type < 0 || type >= vegetablePrefabs.Length)
        {
            Debug.LogWarning($"TileSpawner: неверный type {type}. Будет выбран случайный тип.");
            type = GetRandomVegetableType();
            isGlowing = ShouldCreateGlowingVegetable(type);
        }

        GameObject prefab = vegetablePrefabs[type];

        if (prefab == null)
        {
            Debug.LogError($"TileSpawner: prefab для овоща Type {type} не назначен.");
            return null;
        }

        EnsureTileExists(x, y);

        Tile tile = board[x, y];

        if (tile.View != null)
        {
            Object.Destroy(tile.View.gameObject);
            tile.View = null;
        }

        Transform vegParent = GetVegetableParent();

        GameObject tileObject = Object.Instantiate(
            prefab,
            position,
            Quaternion.identity,
            vegParent
        );

        if (useBoardBounds)
        {
            tileObject.transform.localPosition = GetBoardBoundsLocalPosition(x, y);
        }

        TileView tileView = tileObject.GetComponent<TileView>();

        if (tileView == null)
        {
            tileView = tileObject.AddComponent<TileView>();
        }

        tile.Type = type;
        tile.IsGlowing = isGlowing;
        tile.View = tileView;

        ApplyGlowingSpriteIfNeeded(tileObject, type, isGlowing);

        tileView.Initialize(tile);
        tileView.RefreshName();
        tileView.RefreshGlowState();

        return tileView;
    }

    public GameObject CreateTileView(int x, int y, int type, bool isGlowing, Vector3 position)
    {
        TileView tileView = CreateVegetableView(
            x,
            y,
            type,
            isGlowing,
            position
        );

        if (tileView == null)
        {
            return null;
        }

        return tileView.gameObject;
    }

    public void MoveTileToCell(Tile tile, int targetX, int targetY)
    {
        if (tile == null)
        {
            return;
        }

        if (!IsInsideBoard(targetX, targetY))
        {
            Debug.LogWarning($"TileSpawner: MoveTileToCell вне поля ({targetX}, {targetY}).");
            return;
        }

        if (tile.View != null)
        {
            if (useBoardBounds)
            {
                tile.View.transform.localPosition = GetBoardBoundsLocalPosition(targetX, targetY);
            }
            else
            {
                tile.View.transform.position = GetWorldPosition(targetX, targetY);
            }
            tile.View.RefreshName();
        }
    }

    public float GetTileSpacingX()
    {
        return tileSpacingX;
    }

    public float GetTileSpacingY()
    {
        return tileSpacingY;
    }

    public int GetRandomVegetableType()
    {
        if (vegetablePrefabs == null || vegetablePrefabs.Length == 0)
        {
            Debug.LogError("TileSpawner: невозможно выбрать овощ, vegetablePrefabs пустой.");
            return -1;
        }

        return Random.Range(0, vegetablePrefabs.Length);
    }

    public int GetRandomTypeWithoutImmediateMatch(int x, int y)
    {
        if (vegetablePrefabs == null || vegetablePrefabs.Length == 0)
        {
            Debug.LogError("TileSpawner: невозможно выбрать овощ, vegetablePrefabs пустой.");
            return -1;
        }

        int selectedType;
        int attempts = 0;
        int maxAttempts = 100;

        do
        {
            selectedType = GetRandomVegetableType();
            attempts++;
        }
        while (
            WouldCreateImmediateMatch(x, y, selectedType) &&
            attempts < maxAttempts
        );

        return selectedType;
    }

    public bool ShouldCreateGlowingVegetable(int type)
    {
        if (type < 0)
        {
            return false;
        }

        if (glowingVegetableChance <= 0f)
        {
            return false;
        }

        if (glowingVegetableSprites == null || glowingVegetableSprites.Length == 0)
        {
            return false;
        }

        if (type >= glowingVegetableSprites.Length)
        {
            return false;
        }

        if (glowingVegetableSprites[type] == null)
        {
            return false;
        }

        return Random.value < glowingVegetableChance;
    }

    private int GetRandomTypeWithoutInitialMatch(int x, int y)
    {
        if (vegetablePrefabs == null || vegetablePrefabs.Length == 0)
        {
            Debug.LogError("TileSpawner: невозможно выбрать стартовый овощ, vegetablePrefabs пустой.");
            return -1;
        }

        int selectedType;
        int attempts = 0;
        int maxAttempts = 100;

        do
        {
            selectedType = GetRandomVegetableType();
            attempts++;
        }
        while (
            WouldCreateInitialMatch(x, y, selectedType) &&
            attempts < maxAttempts
        );

        return selectedType;
    }

    private bool WouldCreateInitialMatch(int x, int y, int type)
    {
        if (type < 0)
        {
            return false;
        }

        bool horizontalMatch =
            x >= 2 &&
            board[x - 1, y] != null &&
            board[x - 2, y] != null &&
            board[x - 1, y].Type == type &&
            board[x - 2, y].Type == type;

        if (horizontalMatch)
        {
            return true;
        }

        bool verticalMatch =
            y >= 2 &&
            board[x, y - 1] != null &&
            board[x, y - 2] != null &&
            board[x, y - 1].Type == type &&
            board[x, y - 2].Type == type;

        return verticalMatch;
    }

    private bool WouldCreateImmediateMatch(int x, int y, int type)
    {
        if (type < 0)
        {
            return false;
        }

        bool leftMatch =
            x >= 2 &&
            board[x - 1, y] != null &&
            board[x - 2, y] != null &&
            board[x - 1, y].Type == type &&
            board[x - 2, y].Type == type;

        if (leftMatch)
        {
            return true;
        }

        bool rightMatch =
            x <= width - 3 &&
            board[x + 1, y] != null &&
            board[x + 2, y] != null &&
            board[x + 1, y].Type == type &&
            board[x + 2, y].Type == type;

        if (rightMatch)
        {
            return true;
        }

        bool horizontalMiddleMatch =
            x > 0 &&
            x < width - 1 &&
            board[x - 1, y] != null &&
            board[x + 1, y] != null &&
            board[x - 1, y].Type == type &&
            board[x + 1, y].Type == type;

        if (horizontalMiddleMatch)
        {
            return true;
        }

        bool upperMatch =
            y >= 2 &&
            board[x, y - 1] != null &&
            board[x, y - 2] != null &&
            board[x, y - 1].Type == type &&
            board[x, y - 2].Type == type;

        if (upperMatch)
        {
            return true;
        }

        bool lowerMatch =
            y <= height - 3 &&
            board[x, y + 1] != null &&
            board[x, y + 2] != null &&
            board[x, y + 1].Type == type &&
            board[x, y + 2].Type == type;

        if (lowerMatch)
        {
            return true;
        }

        bool verticalMiddleMatch =
            y > 0 &&
            y < height - 1 &&
            board[x, y - 1] != null &&
            board[x, y + 1] != null &&
            board[x, y - 1].Type == type &&
            board[x, y + 1].Type == type;

        return verticalMiddleMatch;
    }

    private void ApplyGlowingSpriteIfNeeded(GameObject tileObject, int type, bool isGlowing)
    {
        if (!isGlowing)
        {
            return;
        }

        if (tileObject == null)
        {
            return;
        }

        if (glowingVegetableSprites == null || glowingVegetableSprites.Length == 0)
        {
            return;
        }

        if (type < 0 || type >= glowingVegetableSprites.Length)
        {
            return;
        }

        Sprite glowingSprite = glowingVegetableSprites[type];

        if (glowingSprite == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = tileObject.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            spriteRenderer = tileObject.GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = glowingSprite;
        }
    }

    private void EnsureTileExists(int x, int y)
    {
        if (!IsInsideBoard(x, y))
        {
            return;
        }

        if (board[x, y] == null)
        {
            board[x, y] = new Tile(x, y, -1);
        }
    }

    private bool IsInsideBoard(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
}
