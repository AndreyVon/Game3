using UnityEngine;
using TMPro;

public class TimerUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text timerText;

    [Header("Settings")]
    [SerializeField] private bool showWarningWhenLow = true;
    [SerializeField] private float warningThreshold = 30f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.red;

    private LevelManager levelManager;

    private void Start()
    {
        levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null)
        {
            levelManager.OnTimeChanged += UpdateTimer;
            UpdateTimer(levelManager.TimeRemaining);
        }
    }

    private void OnDestroy()
    {
        if (levelManager != null)
            levelManager.OnTimeChanged -= UpdateTimer;
    }

    private void UpdateTimer(float timeRemaining)
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);

        timerText.text = $"{minutes:00}:{seconds:00}";

        if (showWarningWhenLow && timeRemaining <= warningThreshold)
        {
            timerText.color = warningColor;
        }
        else
        {
            timerText.color = normalColor;
        }
    }
}
