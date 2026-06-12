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

    [Header("Bonus Effects")]
    public BoardBonusEffects boardBonusEffects;

    [Header("Flying Pieces Effect")]
    public FlyingPiecesEffect flyingPiecesEffect;
    public VegetableFlyingPiecesSet[] flyingPiecesByVegetableType;

    [Header("Vegetable Prefabs")]
    public GameObject[] vegetablePrefabs;

    private Tile[,] board;

    private MatchFinder matchFinder;
    private TileSpawner tileSpawner;
    private BoardAnimator boardAnimator;
    private BoardCollapseFiller boardCollapseFiller;
    private BoardBonusResolver boardBonusResolver;

    public bool IsBusy { get; private set; }

    private void Start()
    {
        AutoFindSceneReferences();
        InitializeBoardSystems();
        InitializeBoard();
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

        if (boardBonusEffects == null)
        {
            boardBonusEffects = GetComponent<BoardBonusEffects>();
        }

        if (boardBonusEffects == null)
        {
            boardBonusEffects = gameObject.AddComponent<BoardBonusEffects>();
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

        boardBonusResolver = new BoardBonusResolver(board, width, height);

        if (boardBonusEffects != null)
        {
            boardBonusEffects.Initialize(tileSpawner, width, height);
        }
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

            BoardBonusResult bonusResult = boardBonusResolver.ResolveBonus(currentMatches);

            if (bonusResult != null && bonusResult.TilesToRemove != null)
            {
                currentMatches = bonusResult.TilesToRemove;
            }

            AddScoreForMatches(currentMatches.Count, cascadeIndex);

            if (boardBonusEffects != null)
            {
                yield return boardBonusEffects.PlayEffectRoutine(bonusResult, currentMatches);
            }
            else
            {
                Debug.LogWarning("BoardManager: boardBonusEffects не назначен. Эффект удаления пропущен.");
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