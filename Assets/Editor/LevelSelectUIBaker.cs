#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class LevelSelectUIBaker
{
    const string UiFolder = "Assets/UI/LevelSelect";
    const string PrefabPath = UiFolder + "/LevelSelectUI.prefab";
    const string LockedThumbnailPath = UiFolder + "/LevelLockedThumbnail.png";
    const string LevelSelectScenePath = "Assets/Scenes/LevelSelect.unity";
    const string Level1EntryPath = "Assets/Levels/Level1/Level1Entry.asset";

    const int Columns = 4;
    const int Rows = 3;
    const int TotalLevels = 12;
    static readonly Vector2 CellSize = new Vector2(220f, 240f);
    static readonly Vector2 ThumbnailSize = new Vector2(200f, 112f);
    static readonly Vector2 GridSpacing = new Vector2(24f, 24f);

    [InitializeOnLoadMethod]
    static void EnsureOnLoad()
    {
        EditorApplication.delayCall += () => EnsureLevelSelectUI();
    }

    [MenuItem("Tools/Game/Bake Level Select UI")]
    public static void BakeLevelSelectUI()
    {
        EnsureLevelSelectUI(force: true);
    }

    static void EnsureLevelSelectUI(bool force = false)
    {
        bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
        if (!force && prefabExists && SceneHasLevelSelectUI())
            return;

        EnsureFolder("Assets/UI");
        EnsureFolder(UiFolder);
        EnsureLockedThumbnail();

        if (!prefabExists || force)
        {
            var root = BuildUiRoot();
            try
            {
                SavePrefab(root);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        AttachToScene();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Level Select UI ready at {PrefabPath}");
    }

    static bool SceneHasLevelSelectUI()
    {
        var menu = Object.FindObjectOfType<LevelSelectMenu>();
        return menu != null && menu.GetComponentInChildren<LevelSelectCell>(true) != null;
    }

    static void EnsureLockedThumbnail()
    {
        if (File.Exists(LockedThumbnailPath))
            return;

        const int width = 320;
        const int height = 180;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var background = new Color(0.16f, 0.16f, 0.18f);
        var wall = new Color(0.35f, 0.35f, 0.38f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
                texture.SetPixel(x, y, background);
        }

        FillRect(texture, 24, 24, width - 48, height - 48, new Color(1f, 1f, 1f, 0.08f));
        FillRect(texture, 40, 36, 72, height - 72, wall);
        FillRect(texture, width - 112, 36, 72, height - 72, wall);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = texture.GetPixel(x, y);
                texture.SetPixel(x, y, Color.Lerp(pixel, new Color(0.08f, 0.08f, 0.1f), 0.35f));
            }
        }

        texture.Apply();
        File.WriteAllBytes(LockedThumbnailPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(LockedThumbnailPath, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(LockedThumbnailPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }

    static GameObject BuildUiRoot()
    {
        var root = new GameObject("LevelSelectUI");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        CreateTitle(root.transform);
        CreateGrid(root.transform);
        CreateEscHint(root.transform);
        return root;
    }

    static void CreateTitle(Transform parent)
    {
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(parent, false);

        var text = titleGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = "选关";
        text.fontSize = 48;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        var rect = titleGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -48f);
        rect.sizeDelta = new Vector2(480f, 72f);
    }

    static void CreateEscHint(Transform parent)
    {
        var hintGo = new GameObject("EscHint");
        hintGo.transform.SetParent(parent, false);

        var text = hintGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = "按 ESC 退出";
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.72f, 0.76f, 0.82f, 1f);

        var rect = hintGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 28f);
        rect.sizeDelta = new Vector2(320f, 32f);
    }

    static void CreateGrid(Transform parent)
    {
        var gridGo = new GameObject("LevelGrid");
        gridGo.transform.SetParent(parent, false);

        var gridRect = gridGo.AddComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.pivot = new Vector2(0.5f, 0.5f);
        gridRect.anchoredPosition = new Vector2(0f, -20f);

        float gridWidth = Columns * CellSize.x + (Columns - 1) * GridSpacing.x;
        float gridHeight = Rows * CellSize.y + (Rows - 1) * GridSpacing.y;
        gridRect.sizeDelta = new Vector2(gridWidth, gridHeight);

        var layout = gridGo.AddComponent<GridLayoutGroup>();
        layout.cellSize = CellSize;
        layout.spacing = GridSpacing;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = Columns;
        layout.childAlignment = TextAnchor.MiddleCenter;

        var level1Entry = AssetDatabase.LoadAssetAtPath<LevelEntry>(Level1EntryPath);
        var lockedThumbnail = AssetDatabase.LoadAssetAtPath<Sprite>(LockedThumbnailPath);

        for (int i = 1; i <= TotalLevels; i++)
            CreateLevelCell(gridGo.transform, i, i == 1 ? level1Entry : null, lockedThumbnail);
    }

    static void CreateLevelCell(Transform parent, int levelIndex, LevelEntry entry, Sprite lockedThumbnail)
    {
        bool available = entry != null && entry.isAvailable;

        var cellGo = new GameObject($"Level{levelIndex}");
        cellGo.transform.SetParent(parent, false);

        var cellImage = cellGo.AddComponent<Image>();
        cellImage.color = available
            ? new Color(0.14f, 0.16f, 0.22f, 0.95f)
            : new Color(0.1f, 0.1f, 0.12f, 0.85f);

        var button = cellGo.AddComponent<Button>();
        button.targetGraphic = cellImage;
        button.interactable = available;

        var colors = button.colors;
        colors.normalColor = cellImage.color;
        colors.highlightedColor = available
            ? new Color(0.2f, 0.24f, 0.32f, 0.98f)
            : cellImage.color;
        colors.pressedColor = available
            ? new Color(0.1f, 0.12f, 0.18f, 0.98f)
            : cellImage.color;
        colors.disabledColor = cellImage.color;
        button.colors = colors;

        var layout = cellGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 12, 12);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var thumbnailImage = CreateThumbnail(cellGo.transform, available, entry, lockedThumbnail);
        var label = CreateLabel(cellGo.transform, levelIndex, available);

        var cell = cellGo.AddComponent<LevelSelectCell>();
        var serializedCell = new SerializedObject(cell);
        serializedCell.FindProperty("levelIndex").intValue = levelIndex;
        serializedCell.FindProperty("levelEntry").objectReferenceValue = entry;
        serializedCell.FindProperty("button").objectReferenceValue = button;
        serializedCell.FindProperty("thumbnailImage").objectReferenceValue = thumbnailImage;
        serializedCell.FindProperty("label").objectReferenceValue = label;
        serializedCell.FindProperty("lockedThumbnail").objectReferenceValue = lockedThumbnail;
        serializedCell.ApplyModifiedPropertiesWithoutUndo();
    }

    static Image CreateThumbnail(Transform parent, bool available, LevelEntry entry, Sprite lockedThumbnail)
    {
        var thumbGo = new GameObject("Thumbnail");
        thumbGo.transform.SetParent(parent, false);

        var image = thumbGo.AddComponent<Image>();
        image.sprite = available && entry != null && entry.thumbnail != null ? entry.thumbnail : lockedThumbnail;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = available ? Color.white : new Color(0.55f, 0.55f, 0.55f, 1f);

        var layout = thumbGo.AddComponent<LayoutElement>();
        layout.preferredWidth = ThumbnailSize.x;
        layout.preferredHeight = ThumbnailSize.y;
        layout.minHeight = ThumbnailSize.y;
        return image;
    }

    static Text CreateLabel(Transform parent, int levelIndex, bool available)
    {
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(parent, false);

        var text = labelGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = $"第{levelIndex}关";
        text.fontSize = 26;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = available ? Color.white : new Color(0.55f, 0.55f, 0.58f, 1f);

        var layout = labelGo.AddComponent<LayoutElement>();
        layout.preferredHeight = 36f;
        return text;
    }

    static void SavePrefab(GameObject root)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        else
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
    }

    static void AttachToScene()
    {
        var scene = EditorSceneManager.OpenScene(LevelSelectScenePath, OpenSceneMode.Single);
        EnsureEventSystemInScene();

        var menu = Object.FindObjectOfType<LevelSelectMenu>();
        if (menu == null)
        {
            var menuGo = new GameObject("LevelSelectMenu");
            menu = menuGo.AddComponent<LevelSelectMenu>();
        }

        RemoveOldUi(menu.transform);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, menu.transform);
        instance.name = "LevelSelectUI";

        var level1Entry = AssetDatabase.LoadAssetAtPath<LevelEntry>(Level1EntryPath);
        var cells = instance.GetComponentsInChildren<LevelSelectCell>(true);

        var serializedMenu = new SerializedObject(menu);
        serializedMenu.FindProperty("level1Entry").objectReferenceValue = level1Entry;
        serializedMenu.FindProperty("cells").arraySize = cells.Length;
        for (int i = 0; i < cells.Length; i++)
            serializedMenu.FindProperty("cells").GetArrayElementAtIndex(i).objectReferenceValue = cells[i];
        serializedMenu.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void RemoveOldUi(Transform menuTransform)
    {
        for (int i = menuTransform.childCount - 1; i >= 0; i--)
        {
            var child = menuTransform.GetChild(i);
            if (child.name == "LevelSelectUI" || child.name == "LevelSelectCanvas")
                Object.DestroyImmediate(child.gameObject);
        }
    }

    static void EnsureEventSystemInScene()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
            return;

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    static void FillRect(Texture2D texture, int x, int y, int w, int h, Color color)
    {
        int maxX = Mathf.Min(x + w, texture.width);
        int maxY = Mathf.Min(y + h, texture.height);

        for (int py = y; py < maxY; py++)
        {
            for (int px = x; px < maxX; px++)
                texture.SetPixel(px, py, color);
        }
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string name = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
