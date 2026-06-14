using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "CookingMatch3/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public string levelName = "Level 1";
    public int levelIndex = 1;

    [Header("Win Condition: Score")]
    [Tooltip("Целевой счёт для победы")]
    public int targetScore = 500;

    [Header("Win Condition: Time")]
    [Tooltip("Лимит времени в секундах (0 = без ограничения)")]
    public float timeLimit = 120f;

    [Header("Win Condition: Vegetable Goals")]
    [Tooltip("Цели по сбору овощей (пусто = не нужно)")]
    public VegetableGoal[] vegetableGoals;

    [Header("Stars")]
    [Tooltip("Множители очков для звёзд: 1 звезда = targetScore, 2 = multiplier2, 3 = multiplier3")]
    public float star2Multiplier = 1.5f;
    public float star3Multiplier = 2.0f;

    public bool HasTimeLimit => timeLimit > 0f;
    public bool HasVegetableGoals => vegetableGoals != null && vegetableGoals.Length > 0;

    public int GetStarsForScore(int score)
    {
        if (score >= targetScore * star3Multiplier) return 3;
        if (score >= targetScore * star2Multiplier) return 2;
        if (score >= targetScore) return 1;
        return 0;
    }

    public bool AreVegetableGoalsComplete()
    {
        if (!HasVegetableGoals) return true;

        for (int i = 0; i < vegetableGoals.Length; i++)
        {
            if (!vegetableGoals[i].IsComplete)
                return false;
        }
        return true;
    }
}
