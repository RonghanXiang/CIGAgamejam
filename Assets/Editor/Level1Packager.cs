#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Level1Packager
{
    const string Level1ScenePath = "Assets/Levels/Level1/Level1.unity";
    const string Level1EntryPath = "Assets/Levels/Level1/Level1Entry.asset";
    const string LevelSelectScenePath = "Assets/Scenes/LevelSelect.unity";
    const string ThumbnailPath = "Assets/Levels/Level1/Level1Thumbnail.png";
    const string PackageOutputPath = "Builds/Packages/Level1.unitypackage";
    const string PlayerOutputPath = "Builds/Windows/My project (4).exe";

    static readonly string[] Level1PackageAssets =
    {
        Level1ScenePath,
        Level1EntryPath,
        ThumbnailPath,
        "Assets/Prefabs/DoorShelfAssembly.prefab",
        "Assets/Prefabs/CenterPillar.prefab",
        "Assets/Resources/Prefabs/DoorShelfAssembly.prefab",
        "Assets/Resources/Prefabs/CenterPillar.prefab",
        "Assets/Sprites/WhiteSquare.png",
        "Assets/PlayAreaBounds.cs",
        "Assets/GameLevel.cs",
        "Assets/GameVictory.cs",
        "Assets/VictoryDoor.cs",
        "Assets/GravityDoorShelf.cs",
        "Assets/CenterPillar.cs",
        "Assets/CameraFollow.cs",
        "Assets/PlayerMove.cs",
        "Assets/PlayerSpawn.cs",
        "Assets/AnchorWeapon.cs",
        "Assets/AnchorWall.cs",
        "Assets/WorldGravity.cs",
        "Assets/LevelEntry.cs",
        "Assets/LevelCatalog.cs"
    };

    [MenuItem("Tools/Game/Prepare Level 1")]
    public static void PrepareLevel1()
    {
        GamePrefabCreator.EnsurePrefabsExist();
        EnsureResourcesPrefabs();
        GenerateThumbnailAsset();
        AssignLevel1SceneReferences();
        AssignLevel1EntryThumbnail();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Level 1 prepared: prefab refs, thumbnail, and Resources copies are ready.");
    }

    [MenuItem("Tools/Game/Export Level 1 Package")]
    public static void ExportLevel1Package()
    {
        PrepareLevel1();

        string outputPath = Path.Combine(Directory.GetCurrentDirectory(), PackageOutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        AssetDatabase.ExportPackage(
            Level1PackageAssets,
            PackageOutputPath,
            ExportPackageOptions.IncludeDependencies | ExportPackageOptions.Interactive);

        Debug.Log($"Level 1 package exported to: {PackageOutputPath}");
        EditorUtility.RevealInFinder(outputPath);
    }

    [MenuItem("Tools/Game/Build Game (Windows)")]
    public static void BuildGameWindows()
    {
        PrepareLevel1();

        var scenes = new[]
        {
            LevelSelectScenePath,
            Level1ScenePath
        };

        foreach (string scenePath in scenes)
        {
            if (!File.Exists(scenePath))
            {
                Debug.LogError($"Build failed: missing scene {scenePath}");
                return;
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(PlayerOutputPath));

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = PlayerOutputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {PlayerOutputPath}");
            EditorUtility.RevealInFinder(Path.GetFullPath(PlayerOutputPath));
        }
        else
        {
            Debug.LogError($"Build failed: {report.summary.result}");
        }
    }

    static void EnsureResourcesPrefabs()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Prefabs");

        CopyAssetIfMissing("Assets/Prefabs/DoorShelfAssembly.prefab", "Assets/Resources/Prefabs/DoorShelfAssembly.prefab");
        CopyAssetIfMissing("Assets/Prefabs/CenterPillar.prefab", "Assets/Resources/Prefabs/CenterPillar.prefab");
    }

    static void CopyAssetIfMissing(string sourcePath, string targetPath)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(targetPath) != null)
            return;

        if (!AssetDatabase.CopyAsset(sourcePath, targetPath))
            Debug.LogWarning($"Could not copy {sourcePath} to {targetPath}");
    }

    static void AssignLevel1SceneReferences()
    {
        var scene = EditorSceneManager.OpenScene(Level1ScenePath, OpenSceneMode.Single);
        var playArea = Object.FindObjectOfType<PlayAreaBounds>();
        if (playArea == null)
        {
            Debug.LogError("Level1 scene is missing PlayAreaBounds.");
            return;
        }

        var doorShelf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/DoorShelfAssembly.prefab");
        var pillar = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CenterPillar.prefab");

        var serialized = new SerializedObject(playArea);
        serialized.FindProperty("doorShelfAssemblyPrefab").objectReferenceValue = doorShelf;
        serialized.FindProperty("centerPillarPrefab").objectReferenceValue = pillar;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        playArea.Build();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (!EditorBuildSettings.scenes.AnyContains(Level1ScenePath))
            Debug.LogWarning("Level1 is not in Build Settings. Add it before building the game.");
    }

    static void AssignLevel1EntryThumbnail()
    {
        var entry = AssetDatabase.LoadAssetAtPath<LevelEntry>(Level1EntryPath);
        var thumbnail = AssetDatabase.LoadAssetAtPath<Sprite>(ThumbnailPath);
        if (entry == null || thumbnail == null)
            return;

        entry.thumbnail = thumbnail;
        EditorUtility.SetDirty(entry);
    }

    static void GenerateThumbnailAsset()
    {
        EnsureFolder("Assets/Levels/Level1");

        const int width = 320;
        const int height = 180;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        Color background = new Color(0.18f, 0.24f, 0.34f);
        Color accent = new Color(0.2f, 0.95f, 0.45f);
        Color wall = new Color(1f, 0.92f, 0.2f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
                texture.SetPixel(x, y, background);
        }

        FillRect(texture, 24, 24, width - 48, height - 48, new Color(1f, 1f, 1f, 0.12f));
        FillRect(texture, 40, 36, 72, height - 72, wall);
        FillRect(texture, width - 112, 36, 72, height - 72, wall);
        FillRect(texture, width - 56, height / 2 - 18, 24, 36, accent);
        texture.Apply();

        File.WriteAllBytes(ThumbnailPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(ThumbnailPath, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(ThumbnailPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
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

static class BuildSettingsSceneExtensions
{
    public static bool AnyContains(this EditorBuildSettingsScene[] scenes, string scenePath)
    {
        foreach (EditorBuildSettingsScene scene in scenes)
        {
            if (scene.path == scenePath)
                return true;
        }

        return false;
    }
}
#endif
