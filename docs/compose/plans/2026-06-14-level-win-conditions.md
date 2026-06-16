# Level Win Conditions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add win/lose conditions with timer, score targets, and optional vegetable collection goals to CookingMatch3.

**Architecture:** LevelData ScriptableObject defines level parameters. LevelManager MonoBehaviour tracks state (time, score, vegetables), checks win/lose after each cascade. UI shows timer, goals, and win/lose popups.

**Tech Stack:** Unity 2022.3.7f1, C#, TextMeshPro, Unity UI

---

## File Structure

| File | Responsibility |
|------|---------------|
| `Assets/_Project/Scripts/Levels/LevelData.cs` | ScriptableObject — level definition (target score, time, veggie goals) |
| `Assets/_Project/Scripts/Levels/LevelManager.cs` | Tracks current level state, timer, win/lose checks |
| `Assets/_Project/Scripts/Levels/VegetableGoal.cs` | Serializable data class for a single veggie goal |
| `Assets/_Project/Scripts/UI/TimerUI.cs` | Displays countdown timer |
| `Assets/_Project/Scripts/UI/LevelGoalUI.cs` | Shows level goals (score + veggie collection progress) |
| `Assets/_Project/Scripts/UI/LevelEndPopup.cs` | Win/Lose popup with buttons |
| `Assets/_Project/Scripts/Board/BoardManager.cs` | Modify — call LevelManager instead of direct ScoreManager |
| `Assets/_Project/Scripts/UI/ScoreManager.cs` | Keep — LevelManager will coordinate |

---

### Task 1: Create VegetableGoal data class

**Files:**
- Create: `Assets/_Project/Scripts/Levels/VegetableGoal.cs`

- [ ] **Step 1: Create VegetableGoal.cs**

```csharp
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
```

- [ ] **Step 2: Verify compilation**

Open Unity, check Console for errors. If no errors, task is done.

---

### Task 2: Create LevelData ScriptableObject

**Files:**
- Create: `Assets/_Project/Scripts/Levels/LevelData.cs`

- [ ] **Step 1: Create LevelData.cs**

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "NewLevel", menuName = "CookingMatch3/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Basic Settings")]
    public string levelName = "Уровень 1";

    [Header("Score Goal")]
    [Tooltip("Цель по очкам для победы")]
    public int targetScore = 1000;

    [Header("Timer")]
    [Tooltip("Время в секундах (0 = бесконечно)")]
    public float timeLimit = 120f;

    [Header("Vegetable Goals (optional)")]
    [Tooltip("Дополнительные цели по сбору овощей. Пусто = только очки.")]
    public VegetableGoal[] vegetableGoals = new VegetableGoal[0];

    [Header("Stars")]
    [Tooltip("Порог для 2 звёзд (множитель от targetScore)")]
    public float star2Multiplier = 1.5f;
    [Tooltip("Порог для 3 звёзд (множитель от targetScore)")]
    public float star3Multiplier = 2f;

    public int GetStarThreshold(int stars)
    {
        switch (stars)
        {
            case 1: return targetScore;
            case 2: return Mathf.RoundToInt(targetScore * star2Multiplier);
            case 3: return Mathf.RoundToInt(targetScore * star3Multiplier);
            default: return targetScore;
        }
    }
}
```

- [ ] **Step 2: Verify compilation**

Open Unity, check Console for errors.

---

### Task 3: Create LevelManager

**Files:**
- Create: `Assets/_Project/Scripts/Levels/LevelManager.cs`
- Modify: `Assets/_Project/Scripts/Board/BoardManager.cs` (add LevelManager reference)

- [ ] **Step 1: Create LevelManager.cs**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level Settings")]
    public LevelData currentLevel;

    [Header("References")]
    public ScoreManager scoreManager;
    public BorschtPotUI borschtPotUI;

    public float TimeRemaining { get; private set; }
    public bool IsLevelActive { get; private set; }
    public int CurrentScore => scoreManager != null ? scoreManager.Score : 0;

    private Dictionary<int, int> collectedVegetables = new Dictionary<int, int>();
    private VegetableGoal[] activeGoals;

    public event Action<float> OnTimeTick;
    public event Action<int> OnScoreChanged;
    public event Action<int, int> OnVegetableCollected;
    public event Action<int> OnLevelWin;
    public event Action OnLevelLose;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (currentLevel != null)
        {
            StartLevel(currentLevel);
        }
    }

    private void Update()
    {
        if (!IsLevelActive) return;

        if (currentLevel.timeLimit > 0)
        {
            TimeRemaining -= Time.deltaTime;

            OnTimeTick?.Invoke(TimeRemaining);

            if (TimeRemaining <= 0)
            {
                TimeRemaining = 0;
                CheckLoseCondition();
            }
        }
    }

    public void StartLevel(LevelData level)
    {
        currentLevel = level;
        IsLevelActive = true;
        TimeRemaining = level.timeLimit;

        if (scoreManager != null)
            scoreManager.ResetScore();

        if (borschtPotUI != null)
            borschtPotUI.ResetProgress();

        collectedVegetables.Clear();
        activeGoals = level.vegetableGoals;

        if (activeGoals != null)
        {
            for (int i = 0; i < activeGoals.Length; i++)
            {
                activeGoals[i].Reset();
                if (!collectedVegetables.ContainsKey(activeGoals[i].vegetableType))
                {
                    collectedVegetables[activeGoals[i].vegetableType] = 0;
                }
            }
        }

        Debug.Log($"LevelManager: начат уровень '{level.levelName}', цель: {level.targetScore} очков, время: {level.timeLimit}с");
    }

    public void OnMatch(List<Tile> matchedTiles, int earnedScore)
    {
        if (!IsLevelActive) return;

        if (scoreManager != null)
            scoreManager.AddScore(earnedScore);

        if (borschtPotUI != null)
            borschtPotUI.AddProgress(earnedScore);

        OnScoreChanged?.Invoke(CurrentScore);

        CollectVegetables(matchedTiles);

        CheckWinCondition();
    }

    private void CollectVegetables(List<Tile> matchedTiles)
    {
        if (activeGoals == null || activeGoals.Length == 0) return;

        for (int i = 0; i < matchedTiles.Count; i++)
        {
            Tile tile = matchedTiles[i];
            if (tile == null || tile.Type < 0) continue;

            int vegType = tile.Type;

            if (collectedVegetables.ContainsKey(vegType))
            {
                collectedVegetables[vegType]++;
                OnVegetableCollected?.Invoke(vegType, collectedVegetables[vegType]);

                for (int g = 0; g < activeGoals.Length; g++)
                {
                    if (activeGoals[g].vegetableType == vegType)
                    {
                        activeGoals[g].Add(1);
                    }
                }
            }
        }
    }

    private void CheckWinCondition()
    {
        if (!IsLevelActive) return;

        if (CurrentScore < currentLevel.targetScore) return;

        if (activeGoals != null)
        {
            for (int i = 0; i < activeGoals.Length; i++)
            {
                if (!activeGoals[i].IsComplete) return;
            }
        }

        IsLevelActive = false;
        int stars = CalculateStars();
        Debug.Log($"LevelManager: ПОБЕДА! Очки: {CurrentScore}, звёзд: {stars}");
        OnLevelWin?.Invoke(stars);
    }

    private void CheckLoseCondition()
    {
        if (!IsLevelActive) return;

        IsLevelActive = false;
        Debug.Log($"LevelManager: ПОРАЖЕНИЕ! Время вышло. Очки: {CurrentScore}/{currentLevel.targetScore}");
        OnLevelLose?.Invoke();
    }

    private int CalculateStars()
    {
        int stars = 1;
        if (CurrentScore >= currentLevel.GetStarThreshold(2)) stars = 2;
        if (CurrentScore >= currentLevel.GetStarThreshold(3)) stars = 3;
        return stars;
    }

    public bool HasVegetableGoal(int vegetableType)
    {
        if (activeGoals == null) return false;
        for (int i = 0; i < activeGoals.Length; i++)
        {
            if (activeGoals[i].vegetableType == vegetableType) return true;
        }
        return false;
    }

    public int GetVegetableGoalProgress(int vegetableType)
    {
        if (collectedVegetables.ContainsKey(vegetableType))
            return collectedVegetables[vegetableType];
        return 0;
    }
}
```

- [ ] **Step 2: Add LevelManager reference to BoardManager**

Open `Assets/_Project/Scripts/Board/BoardManager.cs`, add field near line 56:

```csharp
    [Header("Level Manager")]
    public LevelManager levelManager;
```

- [ ] **Step 3: Modify AddScoreForMatches to notify LevelManager**

In `BoardManager.cs`, modify `AddScoreForMatches` method (line 844). Replace the body:

```csharp
    private void AddScoreForMatches(int matchedVegetablesCount, int cascadeIndex, List<Tile> matchedTiles = null)
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
            levelManager.OnMatch(matchedTiles ?? new List<Tile>(), earnedScore);
        }
        else
        {
            if (scoreManager != null)
                scoreManager.AddScore(earnedScore);
            AddPotProgressForMatch(earnedScore);
        }
    }
```

- [ ] **Step 4: Update call sites**

In `BoardManager.cs`, find line 408:
```csharp
            AddScoreForMatches(currentMatches.Count, cascadeIndex);
```
Change to:
```csharp
            AddScoreForMatches(currentMatches.Count, cascadeIndex, currentMatches);
```

Find line 899 (DebugActivateCrossCutBonusRoutine):
```csharp
        AddScoreForMatches(tilesToRemove.Count, 1);
```
Change to:
```csharp
        AddScoreForMatches(tilesToRemove.Count, 1, tilesToRemove);
```

- [ ] **Step 5: Verify compilation**

Open Unity, check Console for errors.

---

### Task 4: Create TimerUI

**Files:**
- Create: `Assets/_Project/Scripts/UI/TimerUI.cs`

- [ ] **Step 1: Create TimerUI.cs**

```csharp
using UnityEngine;
using TMPro;

public class TimerUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text timerText;

    [Header("Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.red;
    [SerializeField] private float warningThreshold = 30f;

    private LevelManager levelManager;

    private void Start()
    {
        levelManager = LevelManager.Instance;

        if (levelManager != null)
        {
            levelManager.OnTimeTick += UpdateTimer;
        }

        UpdateTimer(levelManager != null ? levelManager.TimeRemaining : 0);
    }

    private void OnDestroy()
    {
        if (levelManager != null)
        {
            levelManager.OnTimeTick -= UpdateTimer;
        }
    }

    private void UpdateTimer(float timeRemaining)
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);

        timerText.text = $"{minutes:00}:{seconds:00}";

        timerText.color = timeRemaining <= warningThreshold ? warningColor : normalColor;
    }
}
```

- [ ] **Step 2: Verify compilation**

Open Unity, check Console for errors.

---

### Task 5: Create LevelGoalUI

**Files:**
- Create: `Assets/_Project/Scripts/UI/LevelGoalUI.cs`

- [ ] **Step 1: Create LevelGoalUI.cs**

```csharp
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class VegetableGoalUIEntry
{
    public Image icon;
    public TMP_Text countText;
}

public class LevelGoalUI : MonoBehaviour
{
    [Header("Score")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text scoreTargetText;

    [Header("Vegetable Goals")]
    [SerializeField] private GameObject vegetableGoalsPanel;
    [SerializeField] private VegetableGoalUIEntry[] vegetableGoalEntries;

    private LevelManager levelManager;

    private void Start()
    {
        levelManager = LevelManager.Instance;

        if (levelManager != null)
        {
            levelManager.OnScoreChanged += UpdateScore;
            levelManager.OnVegetableCollected += UpdateVegetableGoal;
        }

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (levelManager != null)
        {
            levelManager.OnScoreChanged -= UpdateScore;
            levelManager.OnVegetableCollected -= UpdateVegetableGoal;
        }
    }

    private void RefreshUI()
    {
        if (levelManager == null || levelManager.currentLevel == null) return;

        UpdateScore(levelManager.CurrentScore);

        LevelData level = levelManager.currentLevel;

        bool hasVegGoals = level.vegetableGoals != null && level.vegetableGoals.Length > 0;

        if (vegetableGoalsPanel != null)
            vegetableGoalsPanel.SetActive(hasVegGoals);

        if (hasVegGoals && vegetableGoalEntries != null)
        {
            for (int i = 0; i < vegetableGoalEntries.Length && i < level.vegetableGoals.Length; i++)
            {
                VegetableGoal goal = level.vegetableGoals[i];
                VegetableGoalUIEntry entry = vegetableGoalEntries[i];

                if (entry.countText != null)
                    entry.countText.text = $"0/{goal.targetCount}";
            }
        }
    }

    private void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = score.ToString();

        if (scoreTargetText != null && levelManager != null && levelManager.currentLevel != null)
            scoreTargetText.text = $"/{levelManager.currentLevel.targetScore}";
    }

    private void UpdateVegetableGoal(int vegType, int count)
    {
        if (levelManager == null || levelManager.currentLevel == null) return;

        LevelData level = levelManager.currentLevel;
        if (level.vegetableGoals == null) return;

        for (int i = 0; i < level.vegetableGoals.Length; i++)
        {
            if (level.vegetableGoals[i].vegetableType == vegType)
            {
                if (vegetableGoalEntries != null && i < vegetableGoalEntries.Length)
                {
                    VegetableGoalUIEntry entry = vegetableGoalEntries[i];
                    if (entry.countText != null)
                        entry.countText.text = $"{count}/{level.vegetableGoals[i].targetCount}";
                }
                break;
            }
        }
    }
}
```

- [ ] **Step 2: Verify compilation**

Open Unity, check Console for errors.

---

### Task 6: Create LevelEndPopup

**Files:**
- Create: `Assets/_Project/Scripts/UI/LevelEndPopup.cs`

- [ ] **Step 1: Create LevelEndPopup.cs**

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelEndPopup : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject popupPanel;

    [Header("Win")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private TMP_Text winScoreText;
    [SerializeField] private Image[] starImages;
    [SerializeField] private Sprite starActiveSprite;
    [SerializeField] private Sprite starInactiveSprite;

    [Header("Lose")]
    [SerializeField] private GameObject losePanel;
    [SerializeField] private TMP_Text loseScoreText;

    [Header("Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button menuButton;

    private LevelManager levelManager;

    private void Start()
    {
        levelManager = LevelManager.Instance;

        if (levelManager != null)
        {
            levelManager.OnLevelWin += ShowWin;
            levelManager.OnLevelLose += ShowLose;
        }

        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryClicked);

        if (menuButton != null)
            menuButton.onClick.AddListener(OnMenuClicked);

        Hide();
    }

    private void OnDestroy()
    {
        if (levelManager != null)
        {
            levelManager.OnLevelWin -= ShowWin;
            levelManager.OnLevelLose -= ShowLose;
        }

        if (retryButton != null)
            retryButton.onClick.RemoveListener(OnRetryClicked);

        if (menuButton != null)
            menuButton.onClick.RemoveListener(OnMenuClicked);
    }

    private void ShowWin(int stars)
    {
        if (popupPanel != null)
            popupPanel.SetActive(true);

        if (winPanel != null)
            winPanel.SetActive(true);

        if (losePanel != null)
            losePanel.SetActive(false);

        if (winScoreText != null && levelManager != null)
            winScoreText.text = $"Очки: {levelManager.CurrentScore}";

        UpdateStars(stars);
    }

    private void ShowLose()
    {
        if (popupPanel != null)
            popupPanel.SetActive(true);

        if (winPanel != null)
            winPanel.SetActive(false);

        if (losePanel != null)
            losePanel.SetActive(true);

        if (loseScoreText != null && levelManager != null)
            loseScoreText.text = $"Очки: {levelManager.CurrentScore}/{levelManager.currentLevel.targetScore}";
    }

    private void UpdateStars(int stars)
    {
        if (starImages == null) return;

        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] != null)
            {
                starImages[i].sprite = i < stars ? starActiveSprite : starInactiveSprite;
            }
        }
    }

    private void Hide()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);
    }

    private void OnRetryClicked()
    {
        Hide();
        if (levelManager != null && levelManager.currentLevel != null)
        {
            levelManager.StartLevel(levelManager.currentLevel);
        }
    }

    private void OnMenuClicked()
    {
        Hide();
        Debug.Log("LevelEndPopup: переход в главное меню (пока не реализован)");
    }
}
```

- [ ] **Step 2: Verify compilation**

Open Unity, check Console for errors.

---

### Task 7: Create test LevelData asset

**Files:**
- Create: `Assets/_Project/Data/Levels/Level1.asset` (created in Unity Editor)

- [ ] **Step 1: Create test level in Unity Editor**

1. Right-click in `Assets/_Project/Data/Levels/` folder
2. Create → CookingMatch3 → Level Data
3. Name it "Level1"
4. Set: levelName = "Уровень 1", targetScore = 500, timeLimit = 90
5. Leave vegetableGoals empty (first level is just score)

- [ ] **Step 2: Verify level asset exists**

Check that the .asset file was created.

---

### Task 8: Wire up scene

**Files:**
- Modify: `Assets/_Project/Scenes/GameScene.unity`

- [ ] **Step 1: Create LevelManager GameObject**

1. In GameScene, create empty GameObject named "LevelManager"
2. Add LevelManager component
3. Assign Level1 to currentLevel field
4. Assign ScoreManager reference
5. Assign BorschtPotUI reference

- [ ] **Step 2: Update BoardManager**

1. Select BoardManager GameObject
2. Assign LevelManager reference to the new levelManager field
3. Keep existing ScoreManager and BorschtPotUI references (LevelManager will coordinate)

- [ ] **Step 3: Add Timer UI**

1. In Canvas, create TextMeshPro object named "TimerText"
2. Position it at top-center
3. Add TimerUI component to Canvas (or new GameObject)
4. Assign TimerText reference

- [ ] **Step 4: Add Level Goal UI**

1. In Canvas, create panel named "LevelGoalPanel"
2. Add score text, target text, and optional vegetable goal entries
3. Add LevelGoalUI component
4. Assign all references

- [ ] **Step 5: Add Level End Popup**

1. In Canvas, create panel named "LevelEndPopup" (disabled by default)
2. Create win panel with score text and 3 star images
3. Create lose panel with score text
4. Add retry and menu buttons
5. Add LevelEndPopup component
6. Assign all references

- [ ] **Step 6: Test in Play Mode**

1. Press Play
2. Timer should count down
3. Make matches — score should update, goals should track
4. Reach target score — win popup should appear
5. Let timer run out — lose popup should appear
6. Retry button should restart level

---

### Task 9: Cleanup

**Files:**
- Modify: `Assets/_Project/Scripts/Board/BoardManager.cs`

- [ ] **Step 1: Remove debug key C**

In `BoardManager.cs`, find the Update method and remove the debug CrossCut code (pressing C key).

- [ ] **Step 2: Activate ScoreText**

In scene, set ScoreText GameObject to active (m_IsActive: 1).

- [ ] **Step 3: Final test**

Play the game end-to-end. Verify:
- Timer counts down
- Score updates
- Win popup appears at target score
- Lose popup appears when time runs out
- Retry works
- No console errors
