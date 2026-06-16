using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class SceneSetupEditor : EditorWindow
{
    [MenuItem("Tools/Setup Canvas Layout")]
    static void ShowWindow()
    {
        GetWindow<SceneSetupEditor>("Canvas Layout Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Canvas Layout Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Create Canvas Hierarchy"))
        {
            CreateCanvasHierarchy();
        }

        GUILayout.Space(10);
        GUILayout.Label("This will:");
        GUILayout.Label("1. Create a new Canvas with CanvasScaler");
        GUILayout.Label("2. Create TopPanel, ScoreBar, BoardArea");
        GUILayout.Label("3. Add GameLayoutSetup component");
        GUILayout.Label("4. Reparent existing UI elements");
    }

    void CreateCanvasHierarchy()
    {
        Canvas existingCanvas = FindObjectOfType<Canvas>();
        if (existingCanvas != null)
        {
            if (EditorUtility.DisplayDialog("Replace Canvas?",
                "An existing Canvas was found. Replace it?",
                "Yes", "Cancel"))
            {
                DestroyImmediate(existingCanvas.gameObject);
            }
            else
            {
                return;
            }
        }

        GameObject canvasObj = new GameObject("GameCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        GameLayoutSetup layoutSetup = canvasObj.AddComponent<GameLayoutSetup>();
        layoutSetup.canvas = canvas;

        GameObject topPanel = CreateUIElement("TopPanel", canvasObj.transform);
        RectTransform topRect = topPanel.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0, 1);
        topRect.anchorMax = new Vector2(1, 1);
        topRect.pivot = new Vector2(0.5f, 1);
        topRect.sizeDelta = new Vector2(0, 200);
        topRect.anchoredPosition = Vector2.zero;

        HorizontalLayoutGroup topLayout = topPanel.AddComponent<HorizontalLayoutGroup>();
        topLayout.spacing = 20;
        topLayout.childAlignment = TextAnchor.MiddleCenter;
        topLayout.childForceExpandWidth = false;
        topLayout.childForceExpandHeight = true;

        layoutSetup.topPanel = topRect;

        GameObject scoreBar = CreateUIElement("ScoreBar", canvasObj.transform);
        RectTransform scoreRect = scoreBar.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0.5f, 1);
        scoreRect.anchorMax = new Vector2(0.5f, 1);
        scoreRect.pivot = new Vector2(0.5f, 1);
        scoreRect.sizeDelta = new Vector2(800, 60);
        scoreRect.anchoredPosition = new Vector2(0, -200);

        layoutSetup.scoreBar = scoreRect;

        GameObject boardArea = CreateUIElement("BoardArea", canvasObj.transform);
        RectTransform boardRect = boardArea.GetComponent<RectTransform>();
        boardRect.anchorMin = new Vector2(0.5f, 0.5f);
        boardRect.anchorMax = new Vector2(0.5f, 0.5f);
        boardRect.pivot = new Vector2(0.5f, 0.5f);
        boardRect.sizeDelta = new Vector2(800, 800);
        boardRect.anchoredPosition = new Vector2(0, -50);

        Image boardBg = boardArea.AddComponent<Image>();
        boardBg.color = new Color(0.2f, 0.15f, 0.1f, 0.8f);

        layoutSetup.boardArea = boardRect;
        layoutSetup.boardBackground = boardBg;

        ReparentExistingUI(canvasObj.transform, topPanel.transform, scoreBar.transform);

        EditorUtility.DisplayDialog("Setup Complete",
            "Canvas hierarchy created.\n\nPlease:\n1. Assign board background sprite\n2. Adjust positions in Scene view\n3. Test at different resolutions",
            "OK");
    }

    GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        obj.AddComponent<CanvasRenderer>();
        return obj;
    }

    void ReparentExistingUI(Transform canvasParent, Transform topPanel, Transform scoreBar)
    {
        TimerUI timer = FindObjectOfType<TimerUI>();
        if (timer != null)
        {
            timer.transform.SetParent(topPanel, false);
        }

        BorschtPotUI pot = FindObjectOfType<BorschtPotUI>();
        if (pot != null)
        {
            pot.transform.SetParent(topPanel, false);
        }

        LevelGoalUI goals = FindObjectOfType<LevelGoalUI>();
        if (goals != null)
        {
            goals.transform.SetParent(topPanel, false);
        }

        ScoreManager score = FindObjectOfType<ScoreManager>();
        if (score != null)
        {
            score.transform.SetParent(scoreBar, false);
        }

        WinLoseScreen winLose = FindObjectOfType<WinLoseScreen>();
        if (winLose != null)
        {
            winLose.transform.SetParent(canvasParent, false);
        }
    }
}