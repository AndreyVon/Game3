using UnityEngine;
using UnityEngine.UI;

public class BorschtPotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image potImage;
    [SerializeField] private Image progressFill;

    [Header("Settings")]
    [SerializeField] private int pointsToFillPot = 100;

    [Header("Pot Sprites")]
    [SerializeField] private Sprite[] potSprites;

    private int currentPoints;

    private void Awake()
    {
        if (potImage == null)
            potImage = GetComponent<Image>();

        UpdatePotUI();
    }

    public void AddProgress(int points)
    {
        currentPoints += points;

        if (currentPoints > pointsToFillPot)
            currentPoints = pointsToFillPot;

        UpdatePotUI();
    }

    public void ResetProgress()
    {
        currentPoints = 0;
        UpdatePotUI();
    }

    private void UpdatePotUI()
    {
        float progress = 0f;

        if (pointsToFillPot > 0)
            progress = (float)currentPoints / pointsToFillPot;

        progress = Mathf.Clamp01(progress);

        if (progressFill != null)
            progressFill.fillAmount = progress;

        UpdatePotSprite(progress);
    }

    private void UpdatePotSprite(float progress)
    {
        if (potImage == null)
            return;

        if (potSprites == null || potSprites.Length == 0)
            return;

        int spriteIndex = Mathf.FloorToInt(progress * (potSprites.Length - 1));

        spriteIndex = Mathf.Clamp(spriteIndex, 0, potSprites.Length - 1);

        potImage.sprite = potSprites[spriteIndex];
    }
}