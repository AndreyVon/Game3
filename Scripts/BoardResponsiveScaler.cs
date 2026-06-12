using UnityEngine;

[ExecuteAlways]
public class BoardResponsiveScaler : MonoBehaviour
{
    [Header("References")]
    public Camera targetCamera;
    public SpriteRenderer boardSpriteRenderer;

    [Header("Portrait Layout")]
    [Range(0.1f, 1f)]
    public float portraitWidthPercent = 0.82f;

    [Range(0.1f, 1f)]
    public float portraitHeightPercent = 0.58f;

    [Range(0f, 1f)]
    public float portraitViewportX = 0.5f;

    [Range(0f, 1f)]
    public float portraitViewportY = 0.38f;

    [Header("Landscape Layout")]
    [Range(0.1f, 1f)]
    public float landscapeWidthPercent = 0.42f;

    [Range(0.1f, 1f)]
    public float landscapeHeightPercent = 0.7f;

    [Range(0f, 1f)]
    public float landscapeViewportX = 0.5f;

    [Range(0f, 1f)]
    public float landscapeViewportY = 0.42f;

    [Header("Extra Settings")]
    public float additionalScaleMultiplier = 1f;

    [Tooltip("Если включено, скрипт будет применять масштаб прямо в редакторе.")]
    public bool applyInEditMode = false;

    [Tooltip("Если включено, скрипт будет применять масштаб во время игры.")]
    public bool applyInPlayMode = true;

    public bool updateOnRuntimeResolutionChange = true;

    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Awake()
    {
        if (Application.isPlaying && applyInPlayMode)
        {
            ApplyScaleAndPosition();
        }
    }

    private void Start()
    {
        if (Application.isPlaying && applyInPlayMode)
        {
            ApplyScaleAndPosition();
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (applyInEditMode)
            {
                ApplyScaleAndPosition();
            }

            return;
        }
#endif

        if (!Application.isPlaying)
        {
            return;
        }

        if (!applyInPlayMode)
        {
            return;
        }

        if (!updateOnRuntimeResolutionChange)
        {
            return;
        }

        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            ApplyScaleAndPosition();
        }
    }

    public void ApplyScaleAndPosition()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            Debug.LogWarning("BoardResponsiveScaler: targetCamera не назначена.");
            return;
        }

        if (!targetCamera.orthographic)
        {
            Debug.LogWarning("BoardResponsiveScaler: камера должна быть Orthographic.");
            return;
        }

        if (boardSpriteRenderer == null)
        {
            Debug.LogWarning("BoardResponsiveScaler: boardSpriteRenderer не назначен.");
            return;
        }

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        float visibleHeight = targetCamera.orthographicSize * 2f;
        float visibleWidth = visibleHeight * targetCamera.aspect;

        bool isPortrait = targetCamera.aspect < 1f;

        float widthPercent = isPortrait ? portraitWidthPercent : landscapeWidthPercent;
        float heightPercent = isPortrait ? portraitHeightPercent : landscapeHeightPercent;

        float viewportX = isPortrait ? portraitViewportX : landscapeViewportX;
        float viewportY = isPortrait ? portraitViewportY : landscapeViewportY;

        Vector3 originalScale = transform.localScale;

        transform.localScale = Vector3.one;

        Bounds baseBounds = boardSpriteRenderer.bounds;

        Vector3 boardCenterOffsetFromRoot = baseBounds.center - transform.position;
        Vector3 boardBaseSize = baseBounds.size;

        transform.localScale = originalScale;

        if (boardBaseSize.x <= 0f || boardBaseSize.y <= 0f)
        {
            return;
        }

        float targetBoardWidth = visibleWidth * widthPercent;
        float targetBoardHeight = visibleHeight * heightPercent;

        float scaleByWidth = targetBoardWidth / boardBaseSize.x;
        float scaleByHeight = targetBoardHeight / boardBaseSize.y;

        float finalScale = Mathf.Min(scaleByWidth, scaleByHeight);
        finalScale *= additionalScaleMultiplier;

        transform.localScale = new Vector3(finalScale, finalScale, originalScale.z);

        Vector3 viewportPosition = new Vector3(
            viewportX,
            viewportY,
            Mathf.Abs(targetCamera.transform.position.z - transform.position.z)
        );

        Vector3 desiredBoardCenterWorldPosition = targetCamera.ViewportToWorldPoint(viewportPosition);

        Vector3 scaledBoardCenterOffset = boardCenterOffsetFromRoot * finalScale;

        Vector3 newRootPosition = desiredBoardCenterWorldPosition - scaledBoardCenterOffset;

        transform.position = new Vector3(
            newRootPosition.x,
            newRootPosition.y,
            transform.position.z
        );
    }

    [ContextMenu("Capture Current Layout As Auto")]
    public void CaptureCurrentLayoutAsAuto()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null || boardSpriteRenderer == null)
        {
            Debug.LogWarning("BoardResponsiveScaler: назначь targetCamera и boardSpriteRenderer перед Capture.");
            return;
        }

        if (targetCamera.aspect < 1f)
        {
            CaptureCurrentLayoutAsPortrait();
        }
        else
        {
            CaptureCurrentLayoutAsLandscape();
        }
    }

    [ContextMenu("Capture Current Layout As Landscape")]
    public void CaptureCurrentLayoutAsLandscape()
    {
        CaptureCurrentLayout(false);
    }

    [ContextMenu("Capture Current Layout As Portrait")]
    public void CaptureCurrentLayoutAsPortrait()
    {
        CaptureCurrentLayout(true);
    }

    private void CaptureCurrentLayout(bool capturePortrait)
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            Debug.LogWarning("BoardResponsiveScaler: targetCamera не назначена.");
            return;
        }

        if (!targetCamera.orthographic)
        {
            Debug.LogWarning("BoardResponsiveScaler: камера должна быть Orthographic.");
            return;
        }

        if (boardSpriteRenderer == null)
        {
            Debug.LogWarning("BoardResponsiveScaler: boardSpriteRenderer не назначен.");
            return;
        }

        float visibleHeight = targetCamera.orthographicSize * 2f;
        float visibleWidth = visibleHeight * targetCamera.aspect;

        Bounds currentBounds = boardSpriteRenderer.bounds;

        float currentWidthPercent = currentBounds.size.x / visibleWidth;
        float currentHeightPercent = currentBounds.size.y / visibleHeight;

        Vector3 viewportCenter = targetCamera.WorldToViewportPoint(currentBounds.center);

        if (capturePortrait)
        {
            portraitWidthPercent = currentWidthPercent;
            portraitHeightPercent = currentHeightPercent;
            portraitViewportX = viewportCenter.x;
            portraitViewportY = viewportCenter.y;

            Debug.Log(
                $"BoardResponsiveScaler: сохранён Portrait layout. " +
                $"Width: {portraitWidthPercent}, Height: {portraitHeightPercent}, " +
                $"X: {portraitViewportX}, Y: {portraitViewportY}"
            );
        }
        else
        {
            landscapeWidthPercent = currentWidthPercent;
            landscapeHeightPercent = currentHeightPercent;
            landscapeViewportX = viewportCenter.x;
            landscapeViewportY = viewportCenter.y;

            Debug.Log(
                $"BoardResponsiveScaler: сохранён Landscape layout. " +
                $"Width: {landscapeWidthPercent}, Height: {landscapeHeightPercent}, " +
                $"X: {landscapeViewportX}, Y: {landscapeViewportY}"
            );
        }
    }
}