using UnityEngine;
using System;

public class LevelManager : MonoBehaviour
{
    [Header("Level Config")]
    public LevelData levelData;

    [Header("References")]
    public ScoreManager scoreManager;
    public BorschtPotUI borschtPotUI;

    [Header("Runtime State (read-only)")]
    [SerializeField] private float timeRemaining;
    [SerializeField] private int currentScore;
    [SerializeField] private bool isLevelActive;
    [SerializeField] private VegetableGoal[] runtimeGoals;

    public float TimeRemaining => timeRemaining;
    public int CurrentScore => currentScore;
    public bool IsLevelActive => isLevelActive;
    public VegetableGoal[] RuntimeGoals => runtimeGoals;

    public event Action<int> OnScoreChanged;
    public event Action<float> OnTimeChanged;
    public event Action<VegetableGoal[]> OnGoalsChanged;
    public event Action<int> OnLevelWon; // stars count
    public event Action OnLevelLost;

    private void Start()
    {
        if (levelData == null)
        {
            Debug.LogError("LevelManager: levelData не назначен!");
            return;
        }

        StartLevel();
    }

    private void Update()
    {
        if (!isLevelActive) return;

        if (levelData.HasTimeLimit)
        {
            timeRemaining -= Time.deltaTime;
            OnTimeChanged?.Invoke(timeRemaining);

            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                CheckLoseCondition();
            }
        }
    }

    public void StartLevel()
    {
        if (levelData == null) return;

        currentScore = 0;
        timeRemaining = levelData.timeLimit;
        isLevelActive = true;

        if (levelData.HasVegetableGoals)
        {
            runtimeGoals = new VegetableGoal[levelData.vegetableGoals.Length];
            for (int i = 0; i < levelData.vegetableGoals.Length; i++)
            {
                runtimeGoals[i] = new VegetableGoal
                {
                    vegetableType = levelData.vegetableGoals[i].vegetableType,
                    targetCount = levelData.vegetableGoals[i].targetCount,
                    currentCount = 0
                };
            }
        }
        else
        {
            runtimeGoals = null;
        }

        if (scoreManager != null)
            scoreManager.ResetScore();

        if (borschtPotUI != null)
            borschtPotUI.ResetProgress();

        OnScoreChanged?.Invoke(0);
        OnTimeChanged?.Invoke(timeRemaining);
        OnGoalsChanged?.Invoke(runtimeGoals);

        Debug.Log($"LevelManager: уровень '{levelData.levelName}' начат. Цель: {levelData.targetScore} очков, время: {levelData.timeLimit}с");
    }

    public void AddScore(int amount)
    {
        if (!isLevelActive) return;
        if (amount <= 0) return;

        currentScore += amount;

        if (scoreManager != null)
            scoreManager.AddScore(amount);

        OnScoreChanged?.Invoke(currentScore);

        CheckWinCondition();
    }

    public void CollectVegetable(int vegetableType, int count)
    {
        if (!isLevelActive) return;
        if (runtimeGoals == null) return;

        for (int i = 0; i < runtimeGoals.Length; i++)
        {
            if (runtimeGoals[i].vegetableType == vegetableType)
            {
                runtimeGoals[i].Add(count);
                OnGoalsChanged?.Invoke(runtimeGoals);

                Debug.Log($"LevelManager: собран овощ type={vegetableType},now={runtimeGoals[i].currentCount}/{runtimeGoals[i].targetCount}");

                CheckWinCondition();
                return;
            }
        }
    }

    private void CheckWinCondition()
    {
        if (!isLevelActive) return;

        bool scoreMet = currentScore >= levelData.targetScore;
        bool vegetablesMet = true;

        if (runtimeGoals != null)
        {
            for (int i = 0; i < runtimeGoals.Length; i++)
            {
                if (!runtimeGoals[i].IsComplete)
                {
                    vegetablesMet = false;
                    break;
                }
            }
        }

        if (scoreMet && vegetablesMet)
        {
            WinLevel();
        }
    }

    private void WinLevel()
    {
        isLevelActive = false;

        int stars = levelData.GetStarsForScore(currentScore);

        Debug.Log($"LevelManager: ПОБЕДА! Счёт: {currentScore}, Звёзд: {stars}");

        OnLevelWon?.Invoke(stars);
    }

    private void CheckLoseCondition()
    {
        if (!isLevelActive) return;

        isLevelActive = false;

        Debug.Log($"LevelManager: ПОРАЖЕНИЕ! Время вышло. Счёт: {currentScore}");

        OnLevelLost?.Invoke();
    }

    public void RestartLevel()
    {
        StartLevel();
    }
}
