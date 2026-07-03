using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(150)]
public class GameLevel : MonoBehaviour
{
    public static GameLevel Current { get; private set; }

    public int levelIndex = 1;
    public string displayName = "第一关";

    Text levelLabel;
    Text cameraHintLabel;

    void Awake()
    {
        Current = this;
    }

    void OnDestroy()
    {
        if (Current == this)
            Current = null;
    }

    void Start()
    {
        BuildLevelHud();
    }

    void Update()
    {
        RefreshCameraHint();
    }

    void BuildLevelHud()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
            return;

        var go = new GameObject("LevelHud");
        go.transform.SetParent(canvas.transform, false);

        levelLabel = go.AddComponent<Text>();
        levelLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        levelLabel.fontSize = 28;
        levelLabel.fontStyle = FontStyle.Bold;
        levelLabel.alignment = TextAnchor.UpperCenter;
        levelLabel.color = Color.white;
        levelLabel.text = GetDisplayText();

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -24f);
        rect.sizeDelta = new Vector2(420f, 48f);

        var hintGo = new GameObject("CameraHint");
        hintGo.transform.SetParent(canvas.transform, false);

        cameraHintLabel = hintGo.AddComponent<Text>();
        cameraHintLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cameraHintLabel.fontSize = 22;
        cameraHintLabel.alignment = TextAnchor.UpperCenter;
        cameraHintLabel.color = new Color(0.82f, 0.88f, 0.95f, 1f);

        var hintRect = hintGo.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 1f);
        hintRect.anchorMax = new Vector2(0.5f, 1f);
        hintRect.pivot = new Vector2(0.5f, 1f);
        hintRect.anchoredPosition = new Vector2(0f, -68f);
        hintRect.sizeDelta = new Vector2(520f, 36f);

        RefreshCameraHint();
    }

    void RefreshCameraHint()
    {
        if (cameraHintLabel == null)
            return;

        var cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cameraFollow == null || !cameraFollow.IsFollowingPlayer)
        {
            cameraHintLabel.text = "镜头跟随玩家：否 (快捷键:U)";
            return;
        }

        string state = cameraFollow.AlignRotationToGravity ? "是" : "否";
        cameraHintLabel.text = $"镜头跟随玩家：{state} (快捷键:U)";
    }

    public string GetDisplayText() => $"第 {levelIndex} 关 · {displayName}";
}
