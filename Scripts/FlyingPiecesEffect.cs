using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FlyingPiecesEffect : MonoBehaviour
{
    [Header("Canvas")]
    public Canvas canvas;
    public RectTransform piecesLayer;

    [Header("Target")]
    public RectTransform targetPoint;

    [Header("Piece Visual")]
    public Vector2 pieceSize = new Vector2(90f, 90f);
    public float pieceScale = 0.6f;
    public int piecesPerEffect = 3;
    public bool disableRaycastTarget = true;

    [Header("Flight Timing")]
    public float flyDuration = 1f;
    public float delayBetweenPieces = 0.04f;

    [Header("Start Scatter")]
    public float startPopDistance = 0.45f;
    public float startPopDuration = 0.18f;

    [Header("Flight Curve")]
    public float arcHeight = 1.2f;
    public AnimationCurve flightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Rotation")]
    public float minRotationSpeed = 90f;
    public float maxRotationSpeed = 180f;

    [Header("Pot Squash & Stretch")]
    public RectTransform potToSquash;
    public bool squashPotOnPieceArrive = true;
    public float squashDuration = 0.22f;
    public Vector2 squashScale = new Vector2(1.06f, 0.94f);
    public Vector2 stretchScale = new Vector2(0.98f, 1.03f);
    public bool restartSquashOnEachPiece = true;

    [Header("End")]
    public bool destroyPiecesOnArrive = true;
    public float destroyDelay = 0.05f;

    [Header("Debug")]
    public bool showWarnings = true;

    private Coroutine potSquashCoroutine;
    private Vector3 potOriginalScale;
    private bool potOriginalScaleSaved;

    public void Play(Sprite[] pieceSprites, Vector3 worldStartPosition)
    {
        if (pieceSprites == null || pieceSprites.Length == 0)
        {
            if (showWarnings)
            {
                Debug.LogWarning("FlyingPiecesEffect: pieceSprites пустой. Проверь Flying Pieces By Vegetable Type в BoardManager.");
            }

            return;
        }

        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }

        if (canvas == null)
        {
            if (showWarnings)
            {
                Debug.LogWarning("FlyingPiecesEffect: Canvas не найден.");
            }

            return;
        }

        if (piecesLayer == null)
        {
            if (showWarnings)
            {
                Debug.LogWarning("FlyingPiecesEffect: piecesLayer не назначен. Перетащи сюда Canvas/FlyingPiecesLayer.");
            }

            return;
        }

        if (targetPoint == null)
        {
            if (showWarnings)
            {
                Debug.LogWarning("FlyingPiecesEffect: targetPoint не назначен. Перетащи сюда PotTarget.");
            }

            return;
        }

        Vector2 startAnchoredPosition = WorldToLayerPosition(worldStartPosition);

        StartCoroutine(PlayRoutine(pieceSprites, startAnchoredPosition));
    }

    private IEnumerator PlayRoutine(Sprite[] pieceSprites, Vector2 startAnchoredPosition)
    {
        int piecesCount = Mathf.Max(1, piecesPerEffect);

        for (int i = 0; i < piecesCount; i++)
        {
            Sprite randomSprite = pieceSprites[Random.Range(0, pieceSprites.Length)];

            RectTransform pieceRect = CreatePiece(randomSprite, startAnchoredPosition);

            StartCoroutine(AnimatePieceRoutine(pieceRect, startAnchoredPosition));

            if (delayBetweenPieces > 0f)
            {
                yield return new WaitForSeconds(delayBetweenPieces);
            }
        }
    }

    private RectTransform CreatePiece(Sprite sprite, Vector2 startAnchoredPosition)
    {
        GameObject pieceObject = new GameObject("FlyingPiece");

        pieceObject.transform.SetParent(piecesLayer, false);

        RectTransform rectTransform = pieceObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = pieceSize;
        rectTransform.anchoredPosition = startAnchoredPosition;
        rectTransform.localScale = Vector3.one * pieceScale;

        Image image = pieceObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = !disableRaycastTarget;

        return rectTransform;
    }

    private IEnumerator AnimatePieceRoutine(RectTransform pieceRect, Vector2 startAnchoredPosition)
    {
        if (pieceRect == null)
        {
            yield break;
        }

        Vector2 randomDirection = Random.insideUnitCircle.normalized;

        if (randomDirection == Vector2.zero)
        {
            randomDirection = Vector2.right;
        }

        Vector2 popPosition = startAnchoredPosition + randomDirection * startPopDistance;

        float randomRotationDirection = Random.value > 0.5f ? 1f : -1f;
        float rotationSpeed = Random.Range(minRotationSpeed, maxRotationSpeed) * randomRotationDirection;

        if (startPopDuration > 0f)
        {
            float popTimer = 0f;

            while (popTimer < startPopDuration)
            {
                if (pieceRect == null)
                {
                    yield break;
                }

                popTimer += Time.deltaTime;

                float t = Mathf.Clamp01(popTimer / startPopDuration);
                float easedT = Mathf.SmoothStep(0f, 1f, t);

                pieceRect.anchoredPosition = Vector2.Lerp(startAnchoredPosition, popPosition, easedT);
                pieceRect.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

                yield return null;
            }
        }

        Vector2 flightStartPosition = pieceRect.anchoredPosition;
        Vector2 targetAnchoredPosition = GetTargetPositionInLayer();

        float timer = 0f;

        while (timer < flyDuration)
        {
            if (pieceRect == null)
            {
                yield break;
            }

            timer += Time.deltaTime;

            float rawT = Mathf.Clamp01(timer / flyDuration);
            float curvedT = flightCurve.Evaluate(rawT);

            Vector2 linearPosition = Vector2.Lerp(flightStartPosition, targetAnchoredPosition, curvedT);

            float arc = Mathf.Sin(rawT * Mathf.PI) * arcHeight;

            Vector2 finalPosition = linearPosition + Vector2.up * arc;

            pieceRect.anchoredPosition = finalPosition;
            pieceRect.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

            yield return null;
        }

        if (pieceRect != null)
        {
            pieceRect.anchoredPosition = targetAnchoredPosition;

            TriggerPotSquash();

            if (destroyPiecesOnArrive)
            {
                Destroy(pieceRect.gameObject, destroyDelay);
            }
        }
    }

    private void TriggerPotSquash()
    {
        if (!squashPotOnPieceArrive)
        {
            return;
        }

        if (potToSquash == null)
        {
            if (showWarnings)
            {
                Debug.LogWarning("FlyingPiecesEffect: potToSquash не назначен. Squash & Stretch кастрюли пропущен.");
            }

            return;
        }

        if (!potOriginalScaleSaved)
        {
            potOriginalScale = potToSquash.localScale;
            potOriginalScaleSaved = true;
        }

        if (potSquashCoroutine != null)
        {
            if (restartSquashOnEachPiece)
            {
                StopCoroutine(potSquashCoroutine);
                potToSquash.localScale = potOriginalScale;
            }
            else
            {
                return;
            }
        }

        potSquashCoroutine = StartCoroutine(PotSquashRoutine());
    }

    private IEnumerator PotSquashRoutine()
    {
        if (potToSquash == null)
        {
            yield break;
        }

        Vector3 originalScale = potOriginalScale;

        Vector3 squashTargetScale = new Vector3(
            originalScale.x * squashScale.x,
            originalScale.y * squashScale.y,
            originalScale.z
        );

        Vector3 stretchTargetScale = new Vector3(
            originalScale.x * stretchScale.x,
            originalScale.y * stretchScale.y,
            originalScale.z
        );

        float firstPhaseDuration = squashDuration * 0.35f;
        float secondPhaseDuration = squashDuration * 0.35f;
        float thirdPhaseDuration = squashDuration * 0.30f;

        yield return ScalePotRoutine(originalScale, squashTargetScale, firstPhaseDuration);
        yield return ScalePotRoutine(squashTargetScale, stretchTargetScale, secondPhaseDuration);
        yield return ScalePotRoutine(stretchTargetScale, originalScale, thirdPhaseDuration);

        if (potToSquash != null)
        {
            potToSquash.localScale = originalScale;
        }

        potSquashCoroutine = null;
    }

    private IEnumerator ScalePotRoutine(Vector3 fromScale, Vector3 toScale, float duration)
    {
        if (duration <= 0f)
        {
            if (potToSquash != null)
            {
                potToSquash.localScale = toScale;
            }

            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            if (potToSquash == null)
            {
                yield break;
            }

            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / duration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            potToSquash.localScale = Vector3.Lerp(fromScale, toScale, easedT);

            yield return null;
        }

        if (potToSquash != null)
        {
            potToSquash.localScale = toScale;
        }
    }

    private Vector2 WorldToLayerPosition(Vector3 worldPosition)
    {
        Camera worldCamera = Camera.main;

        if (worldCamera == null)
        {
            if (showWarnings)
            {
                Debug.LogWarning("FlyingPiecesEffect: Camera.main не найдена. Проверь тег MainCamera на камере.");
            }

            return Vector2.zero;
        }

        Vector2 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);

        Camera uiCamera = GetUICamera();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            piecesLayer,
            screenPosition,
            uiCamera,
            out Vector2 localPoint
        );

        return localPoint;
    }

    private Vector2 GetTargetPositionInLayer()
    {
        Camera uiCamera = GetUICamera();

        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(
            uiCamera,
            targetPoint.position
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            piecesLayer,
            screenPosition,
            uiCamera,
            out Vector2 localPoint
        );

        return localPoint;
    }

    private Camera GetUICamera()
    {
        if (canvas == null)
        {
            return null;
        }

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera;
    }
}