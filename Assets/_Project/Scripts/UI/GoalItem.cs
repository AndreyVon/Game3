using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GoalItem : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private GameObject checkmark;

    public void Setup(Sprite icon, int current, int target, bool complete)
    {
        if (iconImage != null && icon != null)
            iconImage.sprite = icon;

        if (countText != null)
            countText.text = $"{Mathf.Min(current, target)}/{target}";

        if (checkmark != null)
            checkmark.SetActive(complete);
    }
}
