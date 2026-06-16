using UnityEngine;

[System.Serializable]
public class VegetableGoal
{
    [Tooltip("Индекс типа овоща (порядок в vegetablePrefabs)")]
    public int vegetableType;

    [Tooltip("Сколько нужно собрать")]
    public int targetCount;

    [Tooltip("Сколько уже собрано (runtime)")]
    [HideInInspector]
    public int currentCount;

    public bool IsComplete => currentCount >= targetCount;

    public void Reset()
    {
        currentCount = 0;
    }

    public void Add(int amount)
    {
        currentCount += amount;
    }
}