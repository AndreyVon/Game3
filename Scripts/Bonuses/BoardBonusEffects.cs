using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardBonusEffects : MonoBehaviour
{
    [Header("Knife Effect")]
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
    public Sprite crossCutSprite;
    public float crossCutSpriteLengthScale = 1.3f;
    public float crossCutSpriteWidthScale = 0.25f;
    public float crossCutSpriteTravelDistance = 1.2f;
    public float crossCutSpriteRotationPunch = 1f;
    public float crossCutDuration = 0.7f;
    public float crossCutDiagonalDelay = 0.4f;
    public int crossCutSortingOrder = 500;
    public Color crossCutColor = Color.white;

    [HideInInspector] public float crossCutLinePadding = 11f;
    [HideInInspector] public float crossCutCoreWidth = 0.05f;
    [HideInInspector] public float crossCutGlowWidth = 0.12f;
    [HideInInspector] public float crossCutSegmentLength = 0.22f;

    [Header("Knife Sweep Trail")]
    public Sprite knifeSweepTrailSprite;
    public bool useCrossCutSpriteAsKnifeTrail = true;
    public float knifeSweepTrailLengthScale = 0.6f;
    public float knifeSweepTrailWidthScale = 0.1f;
    public float knifeSweepTrailFollowOffset = 0.65f;
    public float knifeSweepTrailRotationOffsetZ = 0f;
    public float knifeSweepTrailRotationPunch = 1f;
    public int knifeSweepTrailSortingOrder = 499;
    public Color knifeSweepTrailColor = Color.white;

    [Header("Tests")]
    public int testHorizontalKnifeRowY = 1;
    public int testVerticalKnifeColumnX = 1;
    public float testKnifePreviewTime = 2f;

    private TileSpawner tileSpawner;
    private int width;
    private int height;

    private static Material crossCutLineMaterial;

    public void Initialize(TileSpawner tileSpawner, int width, int height)
    {
        this.tileSpawner = tileSpawner;
        this.width = width;
        this.height = height;
    }

    public IEnumerator PlayEffectRoutine(BoardBonusResult bonusResult, List<Tile> matchedTiles)
    {
        if (bonusResult == null || bonusResult.BonusType == BoardBonusType.None)
        {
            yield return PlayKnifeEffectRoutine(matchedTiles);
            yield break;
        }

        if (bonusResult.BonusType == BoardBonusType.CrossCut)
        {
            yield return PlayCrossCutEffectRoutine();
            yield break;
        }

        if (bonusResult.BonusType == BoardBonusType.HorizontalKnife)
        {
            yield return PlayHorizontalKnifeSweepRoutine(bonusResult.RowY);
            yield break;
        }

        if (bonusResult.BonusType == BoardBonusType.VerticalKnife)
        {
            yield return PlayVerticalKnifeSweepRoutine(bonusResult.ColumnX);
            yield break;
        }

        yield return PlayKnifeEffectRoutine(matchedTiles);
    }

    private IEnumerator PlayHorizontalKnifeSweepRoutine(int rowY)
    {
        if (knifeEffectPrefab == null)
        {
            Debug.LogWarning("BoardBonusEffects: knifeEffectPrefab не назначен. Горизонтальный пролёт ножа пропущен.");
            yield break;
        }

        if (tileSpawner == null)
        {
            Debug.LogWarning("BoardBonusEffects: tileSpawner не инициализирован.");
            yield break;
        }

        if (rowY < 0 || rowY >= height)
        {
            Debug.LogWarning($"BoardBonusEffects: некорректный rowY для пролёта ножа: {rowY}");
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

        Vector3 sweepDirection = (endPosition - startPosition).normalized;
        float sweepRotationZ = Mathf.Atan2(sweepDirection.y, sweepDirection.x) * Mathf.Rad2Deg;

        Quaternion knifeRotation = Quaternion.Euler(0f, 0f, horizontalKnifeSweepRotationZ);

        KnifeEffect knifeEffect = Instantiate(knifeEffectPrefab, startPosition, knifeRotation);
        knifeEffect.transform.localScale *= horizontalKnifeSweepScale;

        SpriteRenderer trailRenderer = CreateKnifeSweepTrailRenderer("Horizontal Knife Trail");

        if (trailRenderer != null)
        {
            trailRenderer.transform.position = startPosition;
            trailRenderer.transform.rotation = Quaternion.Euler(
                0f,
                0f,
                sweepRotationZ + knifeSweepTrailRotationOffsetZ
            );
            trailRenderer.transform.localScale = Vector3.zero;
        }

        Vector3 trailTargetScale = new Vector3(
            knifeSweepTrailLengthScale,
            knifeSweepTrailWidthScale,
            1f
        );

        float elapsedTime = 0f;

        while (elapsedTime < horizontalKnifeSweepDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsedTime / horizontalKnifeSweepDuration);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            Vector3 currentPosition = Vector3.Lerp(startPosition, endPosition, smoothProgress);

            if (knifeEffect != null)
            {
                knifeEffect.transform.position = currentPosition;
            }

            UpdateKnifeSweepTrail(
                trailRenderer,
                progress,
                currentPosition,
                sweepDirection,
                sweepRotationZ,
                trailTargetScale
            );

            yield return null;
        }

        if (trailRenderer != null)
        {
            Destroy(trailRenderer.gameObject);
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
            Debug.LogWarning("BoardBonusEffects: knifeEffectPrefab не назначен. Вертикальный пролёт ножа пропущен.");
            yield break;
        }

        if (tileSpawner == null)
        {
            Debug.LogWarning("BoardBonusEffects: tileSpawner не инициализирован.");
            yield break;
        }

        if (columnX < 0 || columnX >= width)
        {
            Debug.LogWarning($"BoardBonusEffects: некорректный columnX для пролёта ножа: {columnX}");
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

        Vector3 sweepDirection = (endPosition - startPosition).normalized;
        float sweepRotationZ = Mathf.Atan2(sweepDirection.y, sweepDirection.x) * Mathf.Rad2Deg;

        Quaternion knifeRotation = Quaternion.Euler(0f, 0f, verticalKnifeSweepRotationZ);

        KnifeEffect knifeEffect = Instantiate(knifeEffectPrefab, startPosition, knifeRotation);
        knifeEffect.transform.localScale *= verticalKnifeSweepScale;

        SpriteRenderer trailRenderer = CreateKnifeSweepTrailRenderer("Vertical Knife Trail");

        if (trailRenderer != null)
        {
            trailRenderer.transform.position = startPosition;
            trailRenderer.transform.rotation = Quaternion.Euler(
                0f,
                0f,
                sweepRotationZ + knifeSweepTrailRotationOffsetZ
            );
            trailRenderer.transform.localScale = Vector3.zero;
        }

        Vector3 trailTargetScale = new Vector3(
            knifeSweepTrailLengthScale,
            knifeSweepTrailWidthScale,
            1f
        );

        float elapsedTime = 0f;

        while (elapsedTime < verticalKnifeSweepDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsedTime / verticalKnifeSweepDuration);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            Vector3 currentPosition = Vector3.Lerp(startPosition, endPosition, smoothProgress);

            if (knifeEffect != null)
            {
                knifeEffect.transform.position = currentPosition;
            }

            UpdateKnifeSweepTrail(
                trailRenderer,
                progress,
                currentPosition,
                sweepDirection,
                sweepRotationZ,
                trailTargetScale
            );

            yield return null;
        }

        if (trailRenderer != null)
        {
            Destroy(trailRenderer.gameObject);
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
            Debug.LogWarning("BoardBonusEffects: tileSpawner не инициализирован. Эффект крестового разреза пропущен.");
            yield break;
        }

        if (crossCutSprite != null)
        {
            yield return PlayCrossCutSpriteEffectRoutine();
            yield break;
        }

        yield return PlayCrossCutLineEffectRoutine();
    }

    private IEnumerator PlayCrossCutSpriteEffectRoutine()
    {
        Vector3 topLeft = tileSpawner.GetWorldPosition(0, height - 1);
        Vector3 bottomRight = tileSpawner.GetWorldPosition(width - 1, 0);
        Vector3 topRight = tileSpawner.GetWorldPosition(width - 1, height - 1);
        Vector3 bottomLeft = tileSpawner.GetWorldPosition(0, 0);

        Vector3 center = (bottomLeft + topRight) * 0.5f;

        Vector3 mainDirection = (bottomRight - topLeft).normalized;
        Vector3 antiDirection = (bottomLeft - topRight).normalized;

        GameObject effectRoot = new GameObject("Cross Cut Sprite Effect");

        SpriteRenderer mainSlash = CreateCrossCutSpriteRenderer(effectRoot.transform, "Main Slash");
        SpriteRenderer antiSlash = CreateCrossCutSpriteRenderer(effectRoot.transform, "Anti Slash");

        float mainRotationZ = Mathf.Atan2(mainDirection.y, mainDirection.x) * Mathf.Rad2Deg;
        float antiRotationZ = Mathf.Atan2(antiDirection.y, antiDirection.x) * Mathf.Rad2Deg;

        Vector3 targetScale = new Vector3(
            crossCutSpriteLengthScale,
            crossCutSpriteWidthScale,
            1f
        );

        mainSlash.transform.position = center - mainDirection * crossCutSpriteTravelDistance;
        antiSlash.transform.position = center - antiDirection * crossCutSpriteTravelDistance;

        mainSlash.transform.rotation = Quaternion.Euler(0f, 0f, mainRotationZ);
        antiSlash.transform.rotation = Quaternion.Euler(0f, 0f, antiRotationZ);

        mainSlash.transform.localScale = Vector3.zero;
        antiSlash.transform.localScale = Vector3.zero;

        float elapsedTime = 0f;
        float antiDuration = Mathf.Max(0.01f, crossCutDuration - crossCutDiagonalDelay);

        while (elapsedTime < crossCutDuration)
        {
            elapsedTime += Time.deltaTime;

            float mainProgress = Mathf.Clamp01(elapsedTime / crossCutDuration);
            float antiProgress = Mathf.Clamp01((elapsedTime - crossCutDiagonalDelay) / antiDuration);

            ApplyCrossCutSpriteProgress(
                mainSlash,
                mainProgress,
                center,
                mainDirection,
                mainRotationZ,
                targetScale
            );

            ApplyCrossCutSpriteProgress(
                antiSlash,
                antiProgress,
                center,
                antiDirection,
                antiRotationZ,
                targetScale
            );

            yield return null;
        }

        if (effectRoot != null)
        {
            Destroy(effectRoot);
        }
    }

    private IEnumerator PlayCrossCutLineEffectRoutine()
    {
        GameObject effectRoot = new GameObject("Cross Cut Line Effect");

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

    private SpriteRenderer CreateCrossCutSpriteRenderer(Transform parent, string objectName)
    {
        GameObject spriteObject = new GameObject(objectName);
        spriteObject.transform.SetParent(parent, false);

        SpriteRenderer spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = crossCutSprite;
        spriteRenderer.sortingOrder = crossCutSortingOrder;
        spriteRenderer.color = new Color(
            crossCutColor.r,
            crossCutColor.g,
            crossCutColor.b,
            0f
        );

        return spriteRenderer;
    }

    private void ApplyCrossCutSpriteProgress(
        SpriteRenderer spriteRenderer,
        float progress,
        Vector3 center,
        Vector3 direction,
        float baseRotationZ,
        Vector3 targetScale
    )
    {
        if (spriteRenderer == null)
        {
            return;
        }

        progress = Mathf.Clamp01(progress);

        float moveProgress = Mathf.SmoothStep(0f, 1f, progress);
        float alpha = Mathf.Sin(progress * Mathf.PI);

        float punch = Mathf.Sin(progress * Mathf.PI);
        float scaleX = targetScale.x * Mathf.Lerp(0.75f, 1f, moveProgress);
        float scaleY = targetScale.y * Mathf.Lerp(0.35f, 1f, punch);

        Vector3 startPosition = center - direction * crossCutSpriteTravelDistance;
        Vector3 endPosition = center + direction * crossCutSpriteTravelDistance;

        float rotationPunch = Mathf.Sin(progress * Mathf.PI * 2f) * crossCutSpriteRotationPunch;

        spriteRenderer.transform.position = Vector3.Lerp(startPosition, endPosition, moveProgress);
        spriteRenderer.transform.rotation = Quaternion.Euler(0f, 0f, baseRotationZ + rotationPunch);
        spriteRenderer.transform.localScale = new Vector3(scaleX, scaleY, 1f);

        spriteRenderer.color = new Color(
            crossCutColor.r,
            crossCutColor.g,
            crossCutColor.b,
            alpha * crossCutColor.a
        );
    }

    private Sprite GetKnifeSweepTrailSprite()
    {
        if (knifeSweepTrailSprite != null)
        {
            return knifeSweepTrailSprite;
        }

        if (useCrossCutSpriteAsKnifeTrail)
        {
            return crossCutSprite;
        }

        return null;
    }

    private SpriteRenderer CreateKnifeSweepTrailRenderer(string objectName)
    {
        Sprite trailSprite = GetKnifeSweepTrailSprite();

        if (trailSprite == null)
        {
            return null;
        }

        GameObject trailObject = new GameObject(objectName);

        SpriteRenderer spriteRenderer = trailObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = trailSprite;
        spriteRenderer.sortingOrder = knifeSweepTrailSortingOrder;
        spriteRenderer.color = new Color(
            knifeSweepTrailColor.r,
            knifeSweepTrailColor.g,
            knifeSweepTrailColor.b,
            0f
        );

        return spriteRenderer;
    }

    private void UpdateKnifeSweepTrail(
        SpriteRenderer spriteRenderer,
        float progress,
        Vector3 knifePosition,
        Vector3 sweepDirection,
        float baseRotationZ,
        Vector3 targetScale
    )
    {
        if (spriteRenderer == null)
        {
            return;
        }

        progress = Mathf.Clamp01(progress);

        float alpha = Mathf.Sin(progress * Mathf.PI);
        float punch = Mathf.Sin(progress * Mathf.PI);

        Vector3 trailPosition = knifePosition - sweepDirection * knifeSweepTrailFollowOffset;

        float rotationPunch = Mathf.Sin(progress * Mathf.PI * 2f) * knifeSweepTrailRotationPunch;

        float scaleX = targetScale.x * Mathf.Lerp(0.75f, 1f, progress);
        float scaleY = targetScale.y * Mathf.Lerp(0.35f, 1f, punch);

        spriteRenderer.transform.position = trailPosition;
        spriteRenderer.transform.rotation = Quaternion.Euler(
            0f,
            0f,
            baseRotationZ + knifeSweepTrailRotationOffsetZ + rotationPunch
        );

        spriteRenderer.transform.localScale = new Vector3(scaleX, scaleY, 1f);

        spriteRenderer.color = new Color(
            knifeSweepTrailColor.r,
            knifeSweepTrailColor.g,
            knifeSweepTrailColor.b,
            alpha * knifeSweepTrailColor.a
        );
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
            Debug.LogWarning("BoardBonusEffects: не найден shader 'Sprites/Default' для эффекта крестового разреза.");
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

    private IEnumerator PlayKnifeEffectRoutine(List<Tile> matchedTiles)
    {
        if (knifeEffectPrefab == null)
        {
            Debug.LogWarning("BoardBonusEffects: knifeEffectPrefab не назначен. Эффект ножа пропущен.");
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
            Debug.LogWarning("Тест ножа работает только в Play Mode.");
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
            Debug.LogWarning("BoardBonusEffects: knifeEffectPrefab не назначен.");
            yield break;
        }

        if (tileSpawner == null)
        {
            Debug.LogWarning("BoardBonusEffects: tileSpawner не инициализирован.");
            yield break;
        }

        int rowY = Mathf.Clamp(testHorizontalKnifeRowY, 0, height - 1);

        Vector3 centerPosition = tileSpawner.GetWorldPosition(width / 2, rowY);
        centerPosition += horizontalKnifeSweepOffset;

        Quaternion knifeRotation = Quaternion.Euler(0f, 0f, horizontalKnifeSweepRotationZ);

        KnifeEffect knifeEffect = Instantiate(knifeEffectPrefab, centerPosition, knifeRotation);
        knifeEffect.transform.localScale *= horizontalKnifeSweepScale;

        yield return new WaitForSeconds(testKnifePreviewTime);

        if (knifeEffect != null)
        {
            Destroy(knifeEffect.gameObject);
        }
    }
}