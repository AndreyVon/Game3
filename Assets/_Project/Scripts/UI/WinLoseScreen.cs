using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WinLoseScreen : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    [Header("Win UI")]
    [SerializeField] private TMP_Text winScoreText;
    [SerializeField] private GameObject[] stars; // 3 star objects
    [SerializeField] private Button nextLevelButton;

    [Header("Lose UI")]
    [SerializeField] private TMP_Text loseScoreText;
    [SerializeField] private Button retryButton;

    private LevelManager levelManager;

    private void Start()
    {
        levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null)
        {
            levelManager.OnLevelWon += ShowWinScreen;
            levelManager.OnLevelLost += ShowLoseScreen;
        }

        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);

        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryClicked);

        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);
    }

    private void OnDestroy()
    {
        if (levelManager != null)
        {
            levelManager.OnLevelWon -= ShowWinScreen;
            levelManager.OnLevelLost -= ShowLoseScreen;
        }

        if (retryButton != null)
            retryButton.onClick.RemoveListener(OnRetryClicked);

        if (nextLevelButton != null)
            nextLevelButton.onClick.RemoveListener(OnNextLevelClicked);
    }

    private void ShowWinScreen(int starsCount)
    {
        if (winPanel != null) winPanel.SetActive(true);
        if (losePanel != null) losePanel.SetActive(false);

        if (winScoreText != null && levelManager != null)
            winScoreText.text = $"Счёт: {levelManager.CurrentScore}";

        if (stars != null)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] != null)
                    stars[i].SetActive(i < starsCount);
            }
        }
    }

    private void ShowLoseScreen()
    {
        if (losePanel != null) losePanel.SetActive(true);
        if (winPanel != null) winPanel.SetActive(false);

        if (loseScoreText != null && levelManager != null)
            loseScoreText.text = $"Счёт: {levelManager.CurrentScore}";
    }

    private void OnRetryClicked()
    {
        if (levelManager != null)
            levelManager.RestartLevel();

        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    private void OnNextLevelClicked()
    {
        Debug.Log("WinLoseScreen: следующий уровень (пока не реализован)");
    }
}
