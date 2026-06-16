# Win Conditions System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add win/lose conditions to CookingMatch3 — target score, timer, optional vegetable collection goals, with UI feedback.

**Architecture:** LevelData (ScriptableObject) stores per-level config. LevelManager (MonoBehaviour) tracks runtime state (timer, score, vegetables) and triggers win/lose. BoardManager routes scoring through LevelManager instead of directly to ScoreManager.

**Tech Stack:** Unity 2022.3.7f1, C#, UnityEngine.UI, TMPro

---

## File Structure

| File | Action | Purpose |
|------|--------|---------|
| `Assets/_Project/Scripts/Levels/LevelData.cs` | **Create** | ScriptableObject — level configuration (targetScore, timeLimit, vegetableGoals) |
| `Assets/_Project/Scripts/Levels/LevelManager.cs` | **Create** | MonoBehaviour — runtime state tracker, win/lose logic, UI events |
| `Assets/_Project/Scripts/UI/LevelGoalUI.cs` | **Create** | UI panel showing vegetable goals progress |
| `Assets/_Project/Scripts/UI/TimerUI.cs` | **Create** | Timer display (countdown) |
| `Assets/_Project/Scripts/UI/WinLoseScreen.cs` | **Create** | Win/Lose popup with restart button |
| `Assets/_Project/Scripts/Board/BoardManager.cs:52-57` | **Modify** | Add LevelManager reference, route scoring through it |
| `Assets/_Project/Scripts/Board/BoardManager.cs:844-868` | **Modify** | Update AddScoreForMatches to notify LevelManager of collected vegetables |
| `Assets/_Project/Scripts/UI/ScoreManager.cs` | **Modify** | Add event for score changes (optional, for decoupling) |

---

### Task 1: Create LevelData ScriptableObject

**Covers:** Level configuration storage

**Files:**
- Create: `Assets/_Project/Scripts/Levels/LevelData.cs`

- [ ] **Step 1: Create LevelData ScriptableObject**

```csharp
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
```

- [ ] **Step 2: Verify compilation**

Open Unity Editor, check Console for errors. LevelData should appear in Create menu under "CookingMatch3/Level Data".

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Levels/LevelData.cs
git commit -m "feat: add LevelData ScriptableObject for level configuration"
```

---

### Task 2: Create LevelManager

**Covers:** Runtime state tracking, win/lose logic

**Files:**
- Create: `Assets/_Project/Scripts/Levels/LevelManager.cs`

- [ ] **Step 1: Create LevelManager MonoBehaviour**

```csharp
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

        // Reset vegetable goals
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

        // Reset UI
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
        bool vegetablesMet = levelData.AreVegetableGoalsComplete();

        // We check蔬菜 goals using runtimeGoals, not levelData
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
```

- [ ] **Step 2: Verify compilation**

Check Console for errors. LevelManager should compile without issues.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Levels/LevelManager.cs
git commit -m "feat: add LevelManager for runtime state tracking and win/lose logic"
```

---

### Task 3: Create TimerUI

**Covers:** Timer display

**Files:**
- Create: `Assets/_Project/Scripts/UI/TimerUI.cs`

- [ ] **Step 1: Create TimerUI**

```csharp
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
```

- [ ] **Step 2: Verify compilation**

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/UI/TimerUI.cs
git commit -m "feat: add TimerUI for countdown display"
```

---

### Task 4: Create LevelGoalUI

**Covers:** Vegetable goals display

**Files:**
- Create: `Assets/_Project/Scripts/UI/LevelGoalUI.cs`

- [ ] **Step 1: Create LevelGoalUI**

```csharp
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

        // Clear existing
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
            countText.text = $"{current}/{target}";

        if (checkmark != null)
            checkmark.SetActive(complete);
    }
}
```

- [ ] **Step 2: Verify compilation**

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/UI/LevelGoalUI.cs
git commit -m "feat: add LevelGoalUI for vegetable goals display"
```

---

### Task 5: Create WinLoseScreen

**Covers:** Win/Lose popup

**Files:**
- Create: `Assets/_Project/Scripts/UI/WinLoseScreen.cs`

- [ ] **Step 1: Create WinLoseScreen**

```csharp
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

        // Update stars
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
        // TODO: implement level progression
        Debug.Log("WinLoseScreen: следующий уровень (пока не реализован)");
    }
}
```

- [ ] **Step 2: Verify compilation**

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/UI/WinLoseScreen.cs
git commit -m "feat: add WinLoseScreen for win/lose popup"
```

---

### Task 6: Integrate LevelManager into BoardManager

**Covers:** Routing scoring through LevelManager

**Files:**
- Modify: `Assets/_Project/Scripts/Board/BoardManager.cs:52-57` — add LevelManager reference
- Modify: `Assets/_Project/Scripts/Board/BoardManager.cs:844-868` — update AddScoreForMatches

- [ ] **Step 1: Add LevelManager field to BoardManager**

In `BoardManager.cs`, add after line 57 (`public BorschtPotUI borschtPotUI;`):

```csharp
[Header("Level Manager")]
public LevelManager levelManager;
```

- [ ] **Step 2: Update AutoFindSceneReferences to find LevelManager**

In `AutoFindSceneReferences()` method (around line 141), add after the borschtPotUI block:

```csharp
if (levelManager == null)
{
    levelManager = FindObjectOfType<LevelManager>();
}
```

- [ ] **Step 3: Update AddScoreForMatches to notify LevelManager**

Replace the `AddScoreForMatches` method (lines 844-868) with:

```csharp
private void AddScoreForMatches(int matchedVegetablesCount, int cascadeIndex)
{
    if (matchedVegetablesCount <= 0)
    {
        return;
    }

    int cascadeMultiplier = Mathf.Max(1, cascadeIndex);

    int earnedScore = matchedVegetablesCount * pointsPerVegetable * cascadeMultiplier;

    Debug.Log(
        $"Очки за совпадение: {matchedVegetablesCount} овощей × {pointsPerVegetable} × каскад {cascadeMultiplier} = {earnedScore}"
    );

    if (levelManager != null)
    {
        levelManager.AddScore(earnedScore);
    }
    else if (scoreManager != null)
    {
        scoreManager.AddScore(earnedScore);
    }
    else
    {
        Debug.LogWarning("BoardManager: ни levelManager, ни scoreManager не назначены. Очки не начислены.");
    }

    AddPotProgressForMatch(earnedScore);
}
```

- [ ] **Step 4: Update ResolveMatchesCollapseAndFillRoutine to collect vegetables**

In `ResolveMatchesCollapseAndFillRoutine`, after `AddScoreForMatches` call (around line 408), add vegetable collection logic:

```csharp
// Collect vegetables for level goals
CollectVegetablesFromMatch(currentMatches);
```

Add new method after `AddScoreForMatches`:

```csharp
private void CollectVegetablesFromMatch(List<Tile> matchedTiles)
{
    if (levelManager == null || matchedTiles == null) return;

    // Count vegetables by type
    Dictionary<int, int> vegetablesByType = new Dictionary<int, int>();

    for (int i = 0; i < matchedTiles.Count; i++)
    {
        Tile tile = matchedTiles[i];
        if (tile == null || tile.Type < 0) continue;

        if (vegetablesByType.ContainsKey(tile.Type))
            vegetablesByType[tile.Type]++;
        else
            vegetablesByType[tile.Type] = 1;
    }

    // Notify LevelManager
    foreach (var kvp in vegetablesByType)
    {
        levelManager.CollectVegetable(kvp.Key, kvp.Value);
    }
}
```

- [ ] **Step 5: Verify compilation**

Check Console for errors.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Scripts/Board/BoardManager.cs
git commit -m "feat: integrate LevelManager into BoardManager for scoring and vegetable collection"
```

---

### Task 7: Create Test Level Data Asset

**Covers:** Creating a test level for verification

**Files:**
- Create: `Assets/_Project/Scripts/Levels/TestLevel.asset` (via Unity Editor)

- [ ] **Step 1: Create LevelData asset in Unity Editor**

1. Right-click in Project window → Create → CookingMatch3 → Level Data
2. Name it "TestLevel"
3. Set in Inspector:
   - Level Name: "Тестовый уровень"
   - Level Index: 1
   - Target Score: 500
   - Time Limit: 120
   - Vegetable Goals: (add 2-3 goals based on your vegetable types)
   - Star 2 Multiplier: 1.5
   - Star 3 Multiplier: 2.0

- [ ] **Step 2: Add LevelManager to scene**

1. Create empty GameObject named "LevelManager"
2. Add `LevelManager` component
3. Assign `levelData` = TestLevel asset
4. Assign `scoreManager` and `borschtPotUI` references

- [ ] **Step 3: Wire up UI**

1. Create Timer Text (TMP) → add `TimerUI` component → assign text reference
2. Create Level Goal Panel → add `LevelGoalUI` component → create goal item prefab
3. Create Win/Lose panels → add `WinLoseScreen` component → assign panel references

- [ ] **Step 4: Test in Play Mode**

1. Press Play
2. Verify timer counts down
3. Make matches — verify score updates
4. Verify vegetable goals update when collecting matching vegetables
5. Reach target score — verify win screen appears with correct stars
6. Let timer run out — verify lose screen appears
7. Click retry — verify level restarts

- [ ] **Step 5: Commit scene changes**

```bash
git add Assets/Scenes/
git commit -m "feat: add test level with win conditions UI wired up"
```

---

## Self-Review Checklist

- [x] **Spec coverage:** All brainstorm items covered — LevelData, LevelManager, UI (Timer, Goals, Win/Lose), integration
- [x] **Placeholder scan:** No TBD/TODO in implementation steps (only "TODO: implement level progression" in WinLoseScreen which is intentional future work)
- [x] **Type consistency:** LevelManager uses `AddScore(int)`, `CollectVegetable(int, int)`, events match UI subscriptions
- [x] **File paths:** All paths verified against existing structure
- [x] **Code completeness:** Every step contains full code blocks

---

## Execution Handoff

After completing all tasks:

1. **Verify in Unity Editor** — all scripts compile, no errors in Console
2. **Test play mode** — timer, scoring, vegetable collection, win/lose triggers
3. **Create real levels** — duplicate TestLevel asset, adjust parameters per level

**Known limitations (intentional):**
- Level progression (next level button) not implemented — requires SceneManager integration
- Stars display uses placeholder star objects — needs actual star sprites
- No save system for completed levels — future enhancement
