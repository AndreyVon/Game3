using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    [Header("Score Settings")]
    [SerializeField] private int score = 0;

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;

    public int Score => score;

    private void Start()
    {
        RefreshScoreText();
    }

    public void AddScore(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        score += amount;

        Debug.Log($"ScoreManager: добавлено очков: {amount}. Всего: {score}");

        RefreshScoreText();
    }

    public void ResetScore()
    {
        score = 0;

        RefreshScoreText();
    }

    private void RefreshScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Очки: {score}";
        }
    }
}