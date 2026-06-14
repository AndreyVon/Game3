using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelGoalUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform goalsContainer;
    [SerializeField] private GameObject goalItemPrefab;

    [Header("References")]
    [SerializeField] private Sprite[] vegetableIcons;

    private LevelManager levelManager;

    private void Start()
    {
        levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null)
        {
            levelManager.OnGoalsChanged += UpdateGoalsUI;
            UpdateGoalsUI(levelManager.RuntimeGoals);
        }
    }

    private void OnDestroy()
    {
        if (levelManager != null)
            levelManager.OnGoalsChanged -= UpdateGoalsUI;
    }

    private void UpdateGoalsUI(VegetableGoal[] goals)
    {
        if (goalsContainer == null || goalItemPrefab == null) return;

        foreach (Transform child in goalsContainer)
        {
            Destroy(child.gameObject);
        }

        if (goals == null || goals.Length == 0)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        for (int i = 0; i < goals.Length; i++)
        {
            GameObject goalObj = Instantiate(goalItemPrefab, goalsContainer);
            GoalItem goalItem = goalObj.GetComponent<GoalItem>();

            if (goalItem != null)
            {
                Sprite icon = (vegetableIcons != null && goals[i].vegetableType < vegetableIcons.Length)
                    ? vegetableIcons[goals[i].vegetableType]
                    : null;

                goalItem.Setup(icon, goals[i].currentCount, goals[i].targetCount, goals[i].IsComplete);
            }
        }
    }
}
