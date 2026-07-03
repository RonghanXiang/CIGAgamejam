using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectCell : MonoBehaviour
{
    [SerializeField] int levelIndex = 1;
    [SerializeField] LevelEntry levelEntry;
    [SerializeField] Button button;
    [SerializeField] Image thumbnailImage;
    [SerializeField] Text label;
    [SerializeField] Sprite lockedThumbnail;

    public int LevelIndex => levelIndex;

    public LevelEntry LevelEntry
    {
        get => levelEntry;
        set => levelEntry = value;
    }

    void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
        if (thumbnailImage == null)
            thumbnailImage = transform.Find("Thumbnail")?.GetComponent<Image>();
        if (label == null)
            label = transform.Find("Label")?.GetComponent<Text>();

        ApplyVisuals();
    }

    public void ApplyVisuals()
    {
        if (button == null)
            button = GetComponent<Button>();

        bool available = levelEntry != null && levelEntry.isAvailable;
        var cellImage = button != null ? button.targetGraphic as Image : GetComponent<Image>();
        Color cellColor = available
            ? new Color(0.14f, 0.16f, 0.22f, 0.95f)
            : new Color(0.1f, 0.1f, 0.12f, 0.85f);

        if (cellImage != null)
            cellImage.color = cellColor;

        if (button != null)
        {
            button.interactable = available;
            var colors = button.colors;
            colors.normalColor = cellColor;
            colors.highlightedColor = available
                ? new Color(0.2f, 0.24f, 0.32f, 0.98f)
                : cellColor;
            colors.pressedColor = available
                ? new Color(0.1f, 0.12f, 0.18f, 0.98f)
                : cellColor;
            colors.disabledColor = cellColor;
            button.colors = colors;
        }

        if (thumbnailImage != null)
        {
            Sprite sprite = available && levelEntry != null && levelEntry.thumbnail != null
                ? levelEntry.thumbnail
                : lockedThumbnail;
            thumbnailImage.sprite = sprite;
            thumbnailImage.color = available ? Color.white : new Color(0.55f, 0.55f, 0.55f, 1f);
        }

        if (label != null)
        {
            label.text = $"第{levelIndex}关";
            label.color = available ? Color.white : new Color(0.55f, 0.55f, 0.58f, 1f);
        }

        WireButton();
    }

    void WireButton()
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        if (levelEntry == null || !levelEntry.isAvailable)
            return;

        string sceneName = levelEntry.sceneName;
        button.onClick.AddListener(() => SceneManager.LoadScene(sceneName));
    }
}
