using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public class GameVictory : MonoBehaviour
{
    static GameVictory instance;
    static bool hasWon;
    static int starCount;

    GameObject victoryPanel;
    Text starLabel;
    Text hudStarLabel;

    void Awake()
    {
        instance = this;
        hasWon = false;
        starCount = 0;
        Time.timeScale = 1f;
    }

    void Start() => BuildUI();

    void OnDestroy()
    {
        Time.timeScale = 1f;
        hasWon = false;
        starCount = 0;
        if (instance == this)
            instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        if (instance != null) return;
        if (FindObjectOfType<GameVictory>() != null) return;

        var cam = Camera.main;
        if (cam != null)
            cam.gameObject.AddComponent<GameVictory>();
        else
            new GameObject("GameVictory").AddComponent<GameVictory>();
    }

    public static void AddStar()
    {
        EnsureExists();
        starCount++;
        if (instance != null)
            instance.RefreshStarDisplay();
    }

    public static void RemoveStar()
    {
        EnsureExists();
        if (starCount <= 0)
            return;

        starCount--;
        if (instance != null)
            instance.RefreshStarDisplay();
    }

    public static void Trigger()
    {
        EnsureExists();
        if (hasWon) return;

        hasWon = true;
        Time.timeScale = 0f;

        if (instance != null)
            instance.ShowPanel();
    }

    public static int StarCount => starCount;

    void BuildUI()
    {
        var canvasGo = new GameObject("VictoryCanvas");
        canvasGo.transform.SetParent(transform);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasGo.AddComponent<GraphicRaycaster>();

        victoryPanel = new GameObject("VictoryPanel");
        victoryPanel.transform.SetParent(canvasGo.transform, false);

        var panelImage = victoryPanel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.72f);

        var panelRect = victoryPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var box = new GameObject("Box");
        box.transform.SetParent(victoryPanel.transform, false);
        var boxImage = box.AddComponent<Image>();
        boxImage.color = new Color(0.12f, 0.14f, 0.2f, 0.95f);

        var boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(420f, 260f);

        CreateLabel(box.transform, "Title", "游戏胜利", 42, new Vector2(0f, 60f), new Color(1f, 0.88f, 0.2f), FontStyle.Bold);
        CreateLabel(box.transform, "Hint", "恭喜通关！", 22, new Vector2(0f, 10f), new Color(0.9f, 0.92f, 0.95f), FontStyle.Normal);
        starLabel = CreateLabel(box.transform, "Stars", "★ 0", 28, new Vector2(0f, -35f), new Color(1f, 0.92f, 0.25f), FontStyle.Bold);
        CreateLevelSelectButton(box.transform);

        EnsureEventSystem();

        hudStarLabel = CreateHudStarLabel(canvasGo.transform);

        HidePanel();
        RefreshStarDisplay();
    }

    Text CreateHudStarLabel(Transform parent)
    {
        var go = new GameObject("HudStars");
        go.transform.SetParent(parent, false);

        var text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 32;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.UpperLeft;
        text.color = new Color(1f, 0.92f, 0.25f);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -24f);
        rect.sizeDelta = new Vector2(160f, 48f);
        return text;
    }

    void RefreshStarDisplay()
    {
        string display = $"★ {starCount}";
        if (starLabel != null)
            starLabel.text = display;
        if (hudStarLabel != null)
            hudStarLabel.text = display;
    }

    Text CreateLabel(Transform parent, string name, string content, int fontSize, Vector2 pos, Color color, FontStyle style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var text = go.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(380f, 60f);
        rect.anchoredPosition = pos;
        return text;
    }

    void ShowPanel()
    {
        RefreshStarDisplay();

        if (victoryPanel != null)
            victoryPanel.SetActive(true);
    }

    void HidePanel()
    {
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
    }

    void EnsureEventSystem()
    {
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null)
            return;

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    void CreateLevelSelectButton(Transform parent)
    {
        var buttonGo = new GameObject("LevelSelectButton");
        buttonGo.transform.SetParent(parent, false);

        var image = buttonGo.AddComponent<Image>();
        image.color = new Color(0.22f, 0.55f, 0.95f, 1f);

        var button = buttonGo.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(0.32f, 0.65f, 1f, 1f);
        colors.pressedColor = new Color(0.16f, 0.42f, 0.78f, 1f);
        button.colors = colors;
        button.onClick.AddListener(ReturnToLevelSelect);

        var rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(240f, 52f);
        rect.anchoredPosition = new Vector2(0f, -95f);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(buttonGo.transform, false);

        var label = labelGo.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = "回到选关";
        label.fontSize = 24;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;

        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    void ReturnToLevelSelect()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(LevelCatalog.LevelSelectSceneName);
    }
}
