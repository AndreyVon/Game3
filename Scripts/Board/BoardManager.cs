using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VegetableFlyingPiecesSet
{
    public string vegetableName;
    public Sprite[] pieceSprites;
}

public class BoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    public int width = 8;
    public int height = 8;
    public float tileSpacing = 1f;

    [Tooltip("Смещение игрового поля овощей вверх/вниз. X не меняется, поле всегда остаётся по центру.")]
    public float boardVerticalOffset = 0f;

    [Header("Animation Settings")]
    public float swapDuration = 0.25f;
    public float invalidSwapPause = 0.1f;
    public float removeDuration = 0.2f;
    public float fallDuration = 0.25f;

    [Header("No Moves Shuffle Settings")]
    public float noMovesPause = 0.5f;
    public float noMovesDropDownDuration = 0.35f;
    public int maxShuffleAttempts = 100;

    [Header("Score")]
    public ScoreManager scoreManager;
    public int pointsPerVegetable = 10;

    [Header("Borscht Pot")]
    public BorschtPotUI borschtPotUI;

    [Header("Effects")]
    public KnifeEffect knifeEffectPrefab;
    public Vector3 knifeEffectOffset = new Vector3(-0.2f, -0.15f, 0f);
    public float knifeEffectScale = 0.85f;

    [Header("Horizontal Knife Sweep")]
    public float horizontalKnifeSweepDuration = 0.45f;
    public float horizontalKnifeSweepPadding = 2f;
    public Vector3 horizontalKnifeSweepOffset = new Vector3(0f, 0.55f, 0f);
    public float horizontalKnifeSweepScale = 2f;
    public float horizontalKnifeSweepRotationZ = 0f;

    [Header("Vertical Knife Sweep")]
    public float verticalKnifeSweepDuration = 0.45f;
    public float verticalKnifeSweepPadding = 2f;
    public Vector3 verticalKnifeSweepOffset = new Vector3(0.55f, 0f, 0f);
    public float verticalKnifeSweepScale = 2f;
    public float verticalKnifeSweepRotationZ = 90f;

    [Header("Cross Cut Effect")]
    public float crossCutDuration = 0.45f;
    public float crossCutLinePadding = 0.7f;
    public float crossCutCoreWidth = 0.05f;
    public float crossCutGlowWidth = 0.12f;
    public float crossCutSegmentLength = 0.22f; // Доля диагонали, а не world units
    public float crossCutDiagonalDelay = 0.08f;
    public int crossCutSortingOrder = 500;
    public Color crossCutColor = Color.white;

    [Header("Horizontal Knife Sweep Test")]
    public int testHorizontalKnifeRowY = 1;
    public float testKnifePreviewTime = 2f;

    [Header("Vertical Knife Sweep Test")]
    public int testVerticalKnifeColumnX = 1;

    [Header("Flying Pieces Effect")]
    public FlyingPiecesEffect flyingPiecesEffect;
    public VegetableFlyingPiecesSet[] flyingPiecesByVegetableType;

    [Header("Cross Cut Test")]
    public KeyCode testCrossCutKey = KeyCode.C;

    [Header("Vegetable Prefabs")]
    public GameObject[] vegetablePrefabs;

    private Tile[,] board;

    private MatchFinder matchFinder;
    private TileSpawner tileSpawner;
    private BoardAnimator boardAnimator;
    private BoardCollapseFiller boardCollapseFiller;

    private static Material crossCutLineMaterial;

    public bool IsBusy { get; private set; }

    private void Start()
    {
        AutoFindSceneReferences();
        InitializeBoardSystems();
        InitializeBoard();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (Input.GetKeyDown(testCrossCutKey))
        {
            StartCoroutine(PlayCrossCutEffectRoutine());
        }
    }

    private void AutoFindSceneReferences()
    {
        if (scoreManager == null)
        {
            scoreManager = FindObjectOfType<ScoreManager>();
        }

        if (borschtPotUI == null)
        {
            borschtPotUI = FindObjectOfType<BorschtPotUI>();
        }

        if (flyingPiecesEffect == null)
        {
            flyingPiecesEffect = FindObjectOfType<FlyingPiecesEffect>();
        }
    }

    private void InitializeBoardSystems()
    {
        board = new Tile[width, height];

        tileSpawner = new TileSpawner(
            board,
            width,
            height,
            tileSpacing,
            boardVerticalOffset,
            vegetablePrefabs,
            transform
        );

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
    }

    private void InitializeBoard()
    {
        if (vegetablePrefabs == null || vegetablePrefabs.Length == 0)
        {
            Debug.LogError("BoardManager: vegetablePrefabs пустой. Добавь префабы овощей в Inspector.");
            return;
        }

        if (vegetablePrefabs.Length < 3)
        {
            Debug.LogWarning("BoardManager: желательно иметь минимум 3 разных овоща.");
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tileSpawner.CreateInitialTile(x, y);
            }
        }

        Debug.Log($"BoardManager: создана доска {width}x{height} без стартовых совпадений");

        matchFinder.HasAnyMatches();

        if (!HasPossibleMoves())
        {
            Debug.LogWarning("Стартовое поле создано без возможных ходов. Обновляем поле.");
            StartCoroutine(ShuffleBoardUntilPlayableRoutine());
        }
        else
        {
            Debug.Log("Стартовое поле имеет возможные ходы.");
        }
    }

    public Tile GetTile(int x, int y)
    {
        if (IsInsideBoard(x, y))
        {
            return board[x, y];
        }

        return null;
    }

    public bool IsInsideBoard(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public bool AreNeighbors(Tile tile1, Tile tile2)
    {
        if (tile1 == null || tile2 == null)
        {
            return false;
        }

        int deltaX = Mathf.Abs(tile1.X - tile2.X);
        int deltaY = Mathf.Abs(tile1.Y - tile2.Y);

        return deltaX + deltaY == 1;
    }

    private IEnumerator PlayHorizontalKnifeSweepRoutine(int rowY)
    {
        if (knifeEffectPrefab == null)
        {
            Debug.LogWarning("BoardManager: knifeEffectPrefab не назначен. Горизонтальный пролёт ножа пропущен.");
            yield break;
        }

        if (rowY < 0 || rowY >= height)
        {
            Debug.LogWarning($"BoardManager: некорректный rowY для пролёта ножа: {rowY}");
            yield break;
        }

        Vector3 leftPosition = tileSpawner.GetWorldPosition(0, rowY);
        Vector3 rightPosition = tileSpawner.GetWorldPosition(width - 1, rowY);

        Vector3 startPosition =
            leftPosition +
            Vector3.left * horizontalKnifeSweepPadding +
            horizontalKnifeSweepOffset;

        Vector3 endPosition =
            rightPosition +
            Vector3.right * horizontalKnifeSweepPadding +
            horizontalKnifeSweepOffset;

        Quaternion knifeRotation = Quaternion.Euler(
            0f,
            0f,
            horizontalKnifeSweepRotationZ
        );

        KnifeEffect knifeEffect = Instantiate(
            knifeEffectPrefab,
            startPosition,
            knifeRotation
        );

        knifeEffect.transform.localScale *= horizontalKnifeSweepScale;

        Debug.Log(
            $"Горизонтальный нож: RowY={rowY}, Start={startPosition}, End={endPosition}, " +
            $"Scale={horizontalKnifeSweepScale}, RotationZ={horizontalKnifeSweepRotationZ}"
        );

        float elapsedTime = 0f;

        while (elapsedTime < horizontalKnifeSweepDuration)
        {
            elapsedTime += Time.deltaTime;

            float t = elapsedTime / horizontalKnifeSweepDuration;
            t = Mathf.Clamp01(t);

            knifeEffect.transform.position = Vector3.Lerp(
                startPosition,
                endPosition,
                t
            );

            yield return null;
        }

        if (knifeEffect != null)
        {
            Destroy(knifeEffect.gameObject);
        }
    }

    private IEnumerator PlayVerticalKnifeSweepRoutine(int columnX)
    {
        if (knifeEffectPrefab == null)
        {
            Debug.LogWarning("BoardManager: knifeEffectPrefab не назначен. Вертикальный пролёт ножа пропущен.");
            yield break;
        }

        if (columnX < 0 || columnX >= width)
        {
            Debug.LogWarning($"BoardManager: некорректный columnX для пролёта ножа: {columnX}");
            yield break;
        }

        Vector3 bottomPosition = tileSpawner.GetWorldPosition(columnX, 0);
        Vector3 topPosition = tileSpawner.GetWorldPosition(columnX, height - 1);

        Vector3 startPosition =
            bottomPosition +
            Vector3.down * verticalKnifeSweepPadding +
            verticalKnifeSweepOffset;

        Vector3 endPosition =
            topPosition +
            Vector3.up * verticalKnifeSweepPadding +
            verticalKnifeSweepOffset;

        Quaternion knifeRotation = Quaternion.Euler(
            0f,
            0f,
            verticalKnifeSweepRotationZ
        );

        KnifeEffect knifeEffect = Instantiate(
            knifeEffectPrefab,
            startPosition,
            knifeRotation
        );

        knifeEffect.transform.localScale *= verticalKnifeSweepScale;

        Debug.Log(
            $"Вертикальный нож: ColumnX={columnX}, Start={startPosition}, End={endPosition}, " +
            $"Scale={verticalKnifeSweepScale}, RotationZ={verticalKnifeSweepRotationZ}"
        );

        float elapsedTime = 0f;

        while (elapsedTime < verticalKnifeSweepDuration)
        {
            elapsedTime += Time.deltaTime;

            float t = elapsedTime / verticalKnifeSweepDuration;
            t = Mathf.Clamp01(t);

            knifeEffect.transform.position = Vector3.Lerp(
                startPosition,
                endPosition,
                t
            );

            yield return null;
        }

        if (knifeEffect != null)
        {
            Destroy(knifeEffect.gameObject);
        }
    }

    private IEnumerator PlayCrossCutEffectRoutine()
    {
        if (tileSpawner == null)
        {
            Debug.LogWarning("BoardManager: tileSpawner не инициализирован. Эффект крестового разреза пропущен.");
            yield break;
        }

        GameObject effectRoot = new GameObject("Cross Cut Effect");

        LineRenderer mainCore = CreateCrossCutLineRenderer(effectRoot.transform, "Main Diagonal Core");
        LineRenderer mainGlow = CreateCrossCutLineRenderer(effectRoot.transform, "Main Diagonal Glow");
        LineRenderer antiCore = CreateCrossCutLineRenderer(effectRoot.transform, "Anti Diagonal Core");
        LineRenderer antiGlow = CreateCrossCutLineRenderer(effectRoot.transform, "Anti Diagonal Glow");

        Vector3 topLeft = tileSpawner.GetWorldPosition(0, height - 1);
        Vector3 bottomRight = tileSpawner.GetWorldPosition(width - 1, 0);
        Vector3 topRight = tileSpawner.GetWorldPosition(width - 1, height - 1);
        Vector3 bottomLeft = tileSpawner.GetWorldPosition(0, 0);

        Vector3 mainDirection = (bottomRight - topLeft).normalized;
        Vector3 antiDirection = (bottomLeft - topRight).normalized;

        Vector3 mainStart = topLeft - mainDirection * crossCutLinePadding;
        Vector3 mainEnd = bottomRight + mainDirection * crossCutLinePadding;

        Vector3 antiStart = topRight - antiDirection * crossCutLinePadding;
        Vector3 antiEnd = bottomLeft + antiDirection * crossCutLinePadding;

        float elapsedTime = 0f;
        float antiDuration = Mathf.Max(0.01f, crossCutDuration - crossCutDiagonalDelay);

        while (elapsedTime < crossCutDuration)
        {
            elapsedTime += Time.deltaTime;

            float mainProgress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsedTime / crossCutDuration));
            float antiProgress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsedTime - crossCutDiagonalDelay) / antiDuration));

            UpdateCrossCutLine(mainCore, mainGlow, mainStart, mainEnd, mainProgress);
            UpdateCrossCutLine(antiCore, antiGlow, antiStart, antiEnd, antiProgress);

            yield return null;
        }

        if (effectRoot != null)
        {
            Destroy(effectRoot);
        }
    }

    private LineRenderer CreateCrossCutLineRenderer(Transform parent, string objectName)
    {
        GameObject lineObject = new GameObject(objectName);
        lineObject.transform.SetParent(parent, false);

        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.numCapVertices = 4;
        lineRenderer.numCornerVertices = 4;
        lineRenderer.sortingOrder = crossCutSortingOrder;

        Material material = GetCrossCutMaterial();
        if (material != null)
        {
            lineRenderer.material = material;
        }

        return lineRenderer;
    }

    private static Material GetCrossCutMaterial()
    {
        if (crossCutLineMaterial != null)
        {
            return crossCutLineMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            Debug.LogWarning("BoardManager: не найден shader 'Sprites/Default' для эффекта крестового разреза.");
            return null;
        }

        crossCutLineMaterial = new Material(shader);
        crossCutLineMaterial.name = "CrossCutLineMaterial";

        return crossCutLineMaterial;
    }

    private void UpdateCrossCutLine(
        LineRenderer core,
        LineRenderer glow,
        Vector3 start,
        Vector3 end,
        float progress
    )
    {
        progress = Mathf.Clamp01(progress);

        // Двигаем короткий отрезок вдоль диагонали, чтобы это выглядело как разрез.
        float tailProgress = Mathf.Clamp01(progress - crossCutSegmentLength);

        Vector3 segmentStart = Vector3.Lerp(start, end, tailProgress);
        Vector3 segmentEnd = Vector3.Lerp(start, end, progress);

        float alpha = Mathf.Clamp01(progress / 0.12f);

        ApplyCrossCutLine(core, segmentStart, segmentEnd, crossCutCoreWidth, alpha);
        ApplyCrossCutLine(glow, segmentStart, segmentEnd, crossCutGlowWidth, alpha * 0.55f);
    }

    private void ApplyCrossCutLine(LineRenderer lineRenderer, Vector3 start, Vector3 end, float width, float alpha)
    {
        if (lineRenderer == null)
        {
            return;
        }

        Color lineColor = new Color(
            crossCutColor.r,
            crossCutColor.g,
            crossCutColor.b,
            alpha * crossCutColor.a
        );

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
    }

    [ContextMenu("TEST/Horizontal Knife Sweep")]
    private void TestHorizontalKnifeSweep()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Тест пролёта ножа работает только в Play Mode.");
            return;
        }

        StartCoroutine(PlayHorizontalKnifeSweepRoutine(testHorizontalKnifeRowY));
    }

    [ContextMenu("TEST/Vertical Knife Sweep")]
    private void TestVerticalKnifeSweep()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Тест пролёта ножа работает только в Play Mode.");
            return;
        }

        StartCoroutine(PlayVerticalKnifeSweepRoutine(testVerticalKnifeColumnX));
    }

    [ContextMenu("TEST/Cross Cut Effect")]
    private void TestCrossCutEffect()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Тест эффекта работает только в Play Mode.");
            return;
        }

        StartCoroutine(PlayCrossCutEffectRoutine());
    }

    [ContextMenu("TEST/Horizontal Knife Preview")]
    private void TestHorizontalKnifePreview()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Тест ножа работает только в Play Mode.");
            return;
        }

        StartCoroutine(TestHorizontalKnifePreviewRoutine());
    }

    private IEnumerator TestHorizontalKnifePreviewRoutine()
    {
        if (knifeEffectPrefab == null)
        {
            Debug.LogWarning("BoardManager: knifeEffectPrefab не назначен.");
            yield break;
        }

        int rowY = Mathf.Clamp(testHorizontalKnifeRowY, 0, height - 1);

        Vector3 centerPosition = tileSpawner.GetWorldPosition(width / 2, rowY);
        centerPosition += horizontalKnifeSweepOffset;

        Quaternion knifeRotation = Quaternion.Euler(
            0f,
            0f,
            horizontalKnifeSweepRotationZ
        );

        KnifeEffect knifeEffect = Instantiate(
            knifeEffectPrefab,
            centerPosition,
            knifeRotation
        );

        knifeEffect.transform.localScale *= horizontalKnifeSweepScale;

        Debug.Log(
            $"Тест ножа: RowY={rowY}, Position={centerPosition}, " +
            $"Scale={horizontalKnifeSweepScale}, RotationZ={horizontalKnifeSweepRotationZ}"
        );

        yield return new WaitForSeconds(testKnifePreviewTime);

        if (knifeEffect != null)
        {
            Destroy(knifeEffect.gameObject);
        }
    }

    public void SwapTiles(Tile firstTile, Tile secondTile)
    {
        if (IsBusy)
        {
            Debug.Log("SwapTiles: сейчас идёт анимация, обмен невозможен");
            return;
        }

        if (firstTile == null || secondTile == null)
        {
            Debug.LogWarning("SwapTiles: один из Tile == null");
            return;
        }

        if (firstTile.View == null || secondTile.View == null)
        {
            Debug.Log("SwapTiles: нельзя менять пустые клетки");
            return;
        }

        if (firstTile.Type < 0 || secondTile.Type < 0)
        {
            Debug.Log("SwapTiles: нельзя менять пустые клетки");
            return;
        }

        if (!AreNeighbors(firstTile, secondTile))
        {
            Debug.Log("SwapTiles: клетки не соседи");
            return;
        }

        if (firstTile.Type == secondTile.Type)
        {
            Debug.Log("SwapTiles: одинаковые типы, обмен не нужен");
            return;
        }

        StartCoroutine(SwapTilesRoutine(firstTile, secondTile));
    }

    private IEnumerator SwapTilesRoutine(Tile firstTile, Tile secondTile)
    {
        IsBusy = true;

        Debug.Log($"SwapTiles: ({firstTile.X}, {firstTile.Y}) <-> ({secondTile.X}, {secondTile.Y})");

        if (firstTile.View != null)
        {
            firstTile.View.Deselect();
        }

        if (secondTile.View != null)
        {
            secondTile.View.Deselect();
        }

        yield return boardAnimator.AnimateAndSwapTileContentsRoutine(
            firstTile,
            secondTile,
            swapDuration
        );

        List<Tile> swapMatches = matchFinder.FindMatchesCreatedBySwap(firstTile, secondTile);

        if (swapMatches.Count > 0)
        {
            Debug.Log($"Обмен успешный. Совпавших овощей: {swapMatches.Count}");

            matchFinder.LogMatches(swapMatches);

            yield return ResolveMatchesCollapseAndFillRoutine(swapMatches);
        }
        else
        {
            Debug.Log("Обмен не создал новых совпадений. Возвращаем овощи обратно.");

            yield return new WaitForSeconds(invalidSwapPause);

            yield return boardAnimator.AnimateAndSwapTileContentsRoutine(
                firstTile,
                secondTile,
                swapDuration
            );

            Debug.Log("Овощи возвращены на прежние места.");
        }

        IsBusy = false;
    }

    private IEnumerator ResolveMatchesCollapseAndFillRoutine(List<Tile> initialMatches)
    {
        List<Tile> currentMatches = initialMatches;

        int cascadeIndex = 0;
        int maxCascadeCount = 20;

        while (currentMatches != null && currentMatches.Count > 0)
        {
            cascadeIndex++;

            Debug.Log($"Каскад #{cascadeIndex}. Совпавших овощей: {currentMatches.Count}");

            List<Tile> horizontalFiveMatch = FindHorizontalMatchOfAtLeastFive();
            List<Tile> verticalFiveMatch = FindVerticalMatchOfAtLeastFive();

            bool isCrossCutSweep = false;
            bool isHorizontalKnifeSweep = false;
            bool isVerticalKnifeSweep = false;

            int horizontalKnifeRowY = -1;
            int verticalKnifeColumnX = -1;

            if (horizontalFiveMatch != null)
            {
                horizontalKnifeRowY = GetMatchRowY(horizontalFiveMatch);

                Debug.Log("Крестовой разрез: найдено 5+ в ряд по горизонтали. Атакуем диагонали.");

                currentMatches = MergeUniqueTiles(currentMatches, GetCrossDiagonalTiles());

                Debug.Log($"Крестовой разрез: в удаление добавлены обе диагонали, всего овощей: {currentMatches.Count}");

                isCrossCutSweep = true;
            }
            else if (verticalFiveMatch != null)
            {
                verticalKnifeColumnX = GetMatchColumnX(verticalFiveMatch);

                Debug.Log("Крестовой разрез: найдено 5+ в ряд по вертикали. Атакуем диагонали.");

                currentMatches = MergeUniqueTiles(currentMatches, GetCrossDiagonalTiles());

                Debug.Log($"Крестовой разрез: в удаление добавлены обе диагонали, всего овощей: {currentMatches.Count}");

                isCrossCutSweep = true;
            }

            if (!isCrossCutSweep)
            {
                List<Tile> horizontalFourMatch = FindHorizontalMatchOfExactlyFour();
                List<Tile> verticalFourMatch = FindVerticalMatchOfExactlyFour();

                if (horizontalFourMatch != null)
                {
                    horizontalKnifeRowY = GetMatchRowY(horizontalFourMatch);

                    Debug.Log($"Острый нож: найдено 4 в ряд по горизонтали. Уничтожаем весь ряд Y: {horizontalKnifeRowY}");

                    currentMatches = GetFullRowTiles(horizontalKnifeRowY);

                    Debug.Log($"Острый нож: в ряд добавлено овощей для удаления: {currentMatches.Count}");

                    isHorizontalKnifeSweep = true;
                }
                else if (verticalFourMatch != null)
                {
                    verticalKnifeColumnX = GetMatchColumnX(verticalFourMatch);

                    Debug.Log($"Острый нож: найдено 4 в ряд по вертикали. Уничтожаем весь столбец X: {verticalKnifeColumnX}");

                    currentMatches = GetFullColumnTiles(verticalKnifeColumnX);

                    Debug.Log($"Острый нож: в столбец добавлено овощей для удаления: {currentMatches.Count}");

                    isVerticalKnifeSweep = true;
                }
            }

            AddScoreForMatches(currentMatches.Count, cascadeIndex);

            if (isCrossCutSweep)
            {
                yield return PlayCrossCutEffectRoutine();
            }
            else if (isHorizontalKnifeSweep)
            {
                yield return PlayHorizontalKnifeSweepRoutine(horizontalKnifeRowY);
            }
            else if (isVerticalKnifeSweep)
            {
                yield return PlayVerticalKnifeSweepRoutine(verticalKnifeColumnX);
            }
            else
            {
                yield return PlayKnifeEffectRoutine(currentMatches);
            }

            PlayFlyingPiecesEffect(currentMatches);

            yield return boardAnimator.RemoveMatchedTilesRoutine(
                currentMatches,
                removeDuration
            );

            Debug.Log($"Каскад #{cascadeIndex}: совпавшие овощи удалены.");

            yield return boardCollapseFiller.CollapseColumnsRoutine();

            Debug.Log($"Каскад #{cascadeIndex}: овощи упали вниз.");

            yield return boardCollapseFiller.FillEmptyCellsRoutine();

            Debug.Log($"Каскад #{cascadeIndex}: пустые клетки заполнены новыми овощами.");

            currentMatches = matchFinder.FindAllMatches();

            if (currentMatches.Count > 0)
            {
                Debug.Log($"После заполнения появились новые совпадения: {currentMatches.Count}");

                matchFinder.LogMatches(currentMatches);
            }

            if (cascadeIndex >= maxCascadeCount)
            {
                Debug.LogWarning("ResolveMatchesCollapseAndFillRoutine: слишком много каскадов. Останавливаем цикл.");
                break;
            }
        }

        Debug.Log("Каскады завершены. Новых совпадений нет.");

        if (!HasPossibleMoves())
        {
            Debug.LogWarning("На поле нет возможных ходов. Обновляем поле через падение вниз.");
            yield return ShuffleBoardUntilPlayableRoutine();
        }
        else
        {
            Debug.Log("На поле есть возможные ходы.");
        }
    }

    private List<Tile> FindHorizontalMatchOfExactlyFour()
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

                if (matchLength == 4)
                {
                    List<Tile> fourMatchTiles = new List<Tile>();

                    for (int matchX = startX; matchX < startX + matchLength; matchX++)
                    {
                        fourMatchTiles.Add(board[matchX, y]);
                    }

                    Debug.Log($"Найден горизонтальный матч из 4 овощей. Ряд Y: {y}, X: {startX}-{startX + 3}");

                    return fourMatchTiles;
                }
            }
        }

        return null;
    }

    private List<Tile> FindVerticalMatchOfExactlyFour()
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

                if (matchLength == 4)
                {
                    List<Tile> fourMatchTiles = new List<Tile>();

                    for (int matchY = startY; matchY < startY + matchLength; matchY++)
                    {
                        fourMatchTiles.Add(board[x, matchY]);
                    }

                    Debug.Log($"Найден вертикальный матч из 4 овощей. Столбец X: {x}, Y: {startY}-{startY + 3}");

                    return fourMatchTiles;
                }
            }
        }

        return null;
    }

    private List<Tile> FindHorizontalMatchOfAtLeastFive()
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

                if (matchLength >= 5)
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

    private List<Tile> FindVerticalMatchOfAtLeastFive()
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

                if (matchLength >= 5)
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
            Tile tile = board[x, rowY];

            if (tile == null)
            {
                continue;
            }

            if (tile.Type < 0)
            {
                continue;
            }

            if (tile.View == null)
            {
                continue;
            }

            rowTiles.Add(tile);
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
            Tile tile = board[columnX, y];

            if (tile == null)
            {
                continue;
            }

            if (tile.Type < 0)
            {
                continue;
            }

            if (tile.View == null)
            {
                continue;
            }

            columnTiles.Add(tile);
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

    private void AddTileToSet(HashSet<Tile> tiles, int x, int y)
    {
        if (!IsInsideBoard(x, y))
        {
            return;
        }

        Tile tile = board[x, y];

        if (tile == null)
        {
            return;
        }

        if (tile.Type < 0)
        {
            return;
        }

        if (tile.View == null)
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
            for (int i = 0; i < first.Count; i++)
            {
                if (first[i] != null)
                {
                    uniqueTiles.Add(first[i]);
                }
            }
        }

        if (second != null)
        {
            for (int i = 0; i < second.Count; i++)
            {
                if (second[i] != null)
                {
                    uniqueTiles.Add(second[i]);
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

    private bool HasPossibleMoves()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile currentTile = board[x, y];

                if (currentTile == null || currentTile.Type < 0)
                {
                    continue;
                }

                if (CanSwapCreateMatch(x, y, x + 1, y))
                {
                    Debug.Log($"Возможный ход найден: ({x}, {y}) <-> ({x + 1}, {y})");
                    return true;
                }

                if (CanSwapCreateMatch(x, y, x, y + 1))
                {
                    Debug.Log($"Возможный ход найден: ({x}, {y}) <-> ({x}, {y + 1})");
                    return true;
                }
            }
        }

        return false;
    }

    private bool CanSwapCreateMatch(int x1, int y1, int x2, int y2)
    {
        if (!IsInsideBoard(x1, y1) || !IsInsideBoard(x2, y2))
        {
            return false;
        }

        Tile tile1 = board[x1, y1];
        Tile tile2 = board[x2, y2];

        if (tile1 == null || tile2 == null)
        {
            return false;
        }

        if (tile1.Type < 0 || tile2.Type < 0)
        {
            return false;
        }

        if (tile1.Type == tile2.Type)
        {
            return false;
        }

        int firstType = tile1.Type;
        int secondType = tile2.Type;

        tile1.Type = secondType;
        tile2.Type = firstType;

        bool createsMatch = HasMatchAt(x1, y1) || HasMatchAt(x2, y2);

        tile1.Type = firstType;
        tile2.Type = secondType;

        return createsMatch;
    }

    private bool HasMatchAt(int x, int y)
    {
        if (!IsInsideBoard(x, y))
        {
            return false;
        }

        Tile centerTile = board[x, y];

        if (centerTile == null || centerTile.Type < 0)
        {
            return false;
        }

        int type = centerTile.Type;

        int horizontalCount = 1;

        int checkX = x - 1;

        while (
            IsInsideBoard(checkX, y) &&
            board[checkX, y] != null &&
            board[checkX, y].Type == type
        )
        {
            horizontalCount++;
            checkX--;
        }

        checkX = x + 1;

        while (
            IsInsideBoard(checkX, y) &&
            board[checkX, y] != null &&
            board[checkX, y].Type == type
        )
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

        while (
            IsInsideBoard(x, checkY) &&
            board[x, checkY] != null &&
            board[x, checkY].Type == type
        )
        {
            verticalCount++;
            checkY--;
        }

        checkY = y + 1;

        while (
            IsInsideBoard(x, checkY) &&
            board[x, checkY] != null &&
            board[x, checkY].Type == type
        )
        {
            verticalCount++;
            checkY++;
        }

        return verticalCount >= 3;
    }

    private IEnumerator ShuffleBoardUntilPlayableRoutine()
    {
        IsBusy = true;

        Debug.LogWarning("ShuffleBoardUntilPlayableRoutine: ходов нет, обновляем поле через падение вниз.");

        yield return new WaitForSeconds(noMovesPause);

        int attempt = 0;

        while (attempt < maxShuffleAttempts)
        {
            attempt++;

            yield return DropAllTilesDownAndClearRoutine();

            yield return boardCollapseFiller.FillEmptyCellsRoutine();

            List<Tile> matchesAfterShuffle = matchFinder.FindAllMatches();

            bool hasMatches = matchesAfterShuffle != null && matchesAfterShuffle.Count > 0;
            bool hasPossibleMoves = HasPossibleMoves();

            Debug.Log(
                $"Обновление поля. Попытка {attempt}. " +
                $"Есть стартовые совпадения: {hasMatches}. " +
                $"Есть возможные ходы: {hasPossibleMoves}"
            );

            if (!hasMatches && hasPossibleMoves)
            {
                Debug.Log($"Поле успешно обновлено за {attempt} попыток.");
                IsBusy = false;
                yield break;
            }

            Debug.LogWarning("Новое поле получилось неподходящим. Повторяем обновление.");

            yield return new WaitForSeconds(0.1f);
        }

        Debug.LogError(
            $"ShuffleBoardUntilPlayableRoutine: не удалось создать играбельное поле за {maxShuffleAttempts} попыток."
        );

        IsBusy = false;
    }

    private IEnumerator DropAllTilesDownAndClearRoutine()
    {
        List<GameObject> tileObjects = new List<GameObject>();
        List<Vector3> startPositions = new List<Vector3>();
        List<Vector3> targetPositions = new List<Vector3>();

        float dropDistance = (height + 2) * tileSpacing;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile tile = board[x, y];

                if (tile == null)
                {
                    continue;
                }

                if (tile.View != null)
                {
                    GameObject tileObject = tile.View.gameObject;

                    tileObjects.Add(tileObject);
                    startPositions.Add(tileObject.transform.position);
                    targetPositions.Add(tileObject.transform.position + Vector3.down * dropDistance);
                }

                tile.Type = -1;
                tile.View = null;
            }
        }

        float elapsedTime = 0f;

        while (elapsedTime < noMovesDropDownDuration)
        {
            elapsedTime += Time.deltaTime;

            float t = elapsedTime / noMovesDropDownDuration;
            t = Mathf.Clamp01(t);

            float easedT = t * t;

            for (int i = 0; i < tileObjects.Count; i++)
            {
                if (tileObjects[i] == null)
                {
                    continue;
                }

                tileObjects[i].transform.position = Vector3.Lerp(
                    startPositions[i],
                    targetPositions[i],
                    easedT
                );
            }

            yield return null;
        }

        for (int i = 0; i < tileObjects.Count; i++)
        {
            if (tileObjects[i] != null)
            {
                Destroy(tileObjects[i]);
            }
        }
    }

    private IEnumerator PlayKnifeEffectRoutine(List<Tile> matchedTiles)
    {
        if (knifeEffectPrefab == null)
        {
            Debug.LogWarning("BoardManager: knifeEffectPrefab не назначен. Эффект ножа пропущен.");
            yield break;
        }

        if (matchedTiles == null || matchedTiles.Count == 0)
        {
            yield break;
        }

        int activeKnifeEffects = 0;

        foreach (Tile tile in matchedTiles)
        {
            if (tile == null || tile.View == null)
            {
                continue;
            }

            Vector3 knifePosition = tile.View.transform.position + knifeEffectOffset;

            KnifeEffect knifeEffect = Instantiate(
                knifeEffectPrefab,
                knifePosition,
                Quaternion.identity
            );

            knifeEffect.transform.localScale *= knifeEffectScale;

            activeKnifeEffects++;

            StartCoroutine(PlaySingleKnifeEffectRoutine(knifeEffect, () =>
            {
                activeKnifeEffects--;
            }));
        }

        while (activeKnifeEffects > 0)
        {
            yield return null;
        }
    }

    private IEnumerator PlaySingleKnifeEffectRoutine(KnifeEffect knifeEffect, System.Action onComplete)
    {
        if (knifeEffect != null)
        {
            yield return knifeEffect.Play();
        }

        onComplete?.Invoke();
    }

    private void PlayFlyingPiecesEffect(List<Tile> matchedTiles)
    {
        if (flyingPiecesEffect == null)
        {
            Debug.LogWarning("BoardManager: flyingPiecesEffect не назначен. Полёт кусочков пропущен.");
            return;
        }

        if (flyingPiecesByVegetableType == null || flyingPiecesByVegetableType.Length == 0)
        {
            Debug.LogWarning("BoardManager: flyingPiecesByVegetableType пустой. Полёт кусочков пропущен.");
            return;
        }

        if (matchedTiles == null || matchedTiles.Count == 0)
        {
            return;
        }

        Dictionary<int, List<Tile>> tilesByType = new Dictionary<int, List<Tile>>();

        foreach (Tile tile in matchedTiles)
        {
            if (tile == null || tile.View == null)
            {
                continue;
            }

            int vegetableType = tile.Type;

            if (vegetableType < 0)
            {
                continue;
            }

            if (!tilesByType.ContainsKey(vegetableType))
            {
                tilesByType.Add(vegetableType, new List<Tile>());
            }

            tilesByType[vegetableType].Add(tile);
        }

        foreach (KeyValuePair<int, List<Tile>> group in tilesByType)
        {
            int vegetableType = group.Key;
            List<Tile> tilesOfThisType = group.Value;

            Sprite[] pieceSprites = GetFlyingPieceSpritesForVegetableType(vegetableType);

            if (pieceSprites == null || pieceSprites.Length == 0)
            {
                Debug.LogWarning($"BoardManager: нет кусочков для овоща Type {vegetableType}.");
                continue;
            }

            Vector3 startPosition = GetMatchedTilesCenter(tilesOfThisType);

            flyingPiecesEffect.Play(pieceSprites, startPosition);
        }
    }

    private Sprite[] GetFlyingPieceSpritesForVegetableType(int vegetableType)
    {
        if (vegetableType < 0)
        {
            return null;
        }

        if (flyingPiecesByVegetableType == null)
        {
            return null;
        }

        if (vegetableType >= flyingPiecesByVegetableType.Length)
        {
            Debug.LogWarning(
                $"BoardManager: vegetableType {vegetableType} больше размера flyingPiecesByVegetableType {flyingPiecesByVegetableType.Length}."
            );

            return null;
        }

        VegetableFlyingPiecesSet piecesSet = flyingPiecesByVegetableType[vegetableType];

        if (piecesSet == null)
        {
            return null;
        }

        return piecesSet.pieceSprites;
    }

    private Vector3 GetMatchedTilesCenter(List<Tile> matchedTiles)
    {
        Vector3 sum = Vector3.zero;
        int count = 0;

        foreach (Tile tile in matchedTiles)
        {
            if (tile == null || tile.View == null)
            {
                continue;
            }

            sum += tile.View.transform.position;
            count++;
        }

        if (count == 0)
        {
            return transform.position;
        }

        return sum / count;
    }

    private void AddScoreForMatches(int matchedVegetablesCount, int cascadeIndex)
    {
        if (matchedVegetablesCount <= 0)
        {
            return;
        }

        int cascadeMultiplier = Mathf.Max(1, cascadeIndex);

        int earnedScore = matchedVegetablesCount * pointsPerVegetable * cascadeMultiplier;

        Debug.Log(
            $"Очки за совпадение: {matchedVegetablesCount} овощей × {pointsPerVegetable} × каскад {cascadeMultiplier} = {earnedScore}"
        );

        if (scoreManager != null)
        {
            scoreManager.AddScore(earnedScore);
        }
        else
        {
            Debug.LogWarning("BoardManager: scoreManager не назначен. Очки не начислены.");
        }

        AddPotProgressForMatch(earnedScore);
    }

    private void AddPotProgressForMatch(int points)
    {
        if (borschtPotUI != null)
        {
            borschtPotUI.AddProgress(points);

            Debug.Log($"Кастрюля получила +{points} очков прогресса");
        }
        else
        {
            Debug.LogWarning("BoardManager: borschtPotUI не назначен. Кастрюля не заполняется.");
        }
    }
}