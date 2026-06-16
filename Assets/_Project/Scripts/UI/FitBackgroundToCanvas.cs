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