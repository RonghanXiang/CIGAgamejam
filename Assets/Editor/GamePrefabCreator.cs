#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GamePrefabCreator
{
    const string PrefabFolder = "Assets/Prefabs";
    const string SpriteFolder = "Assets/Sprites";
    const string StarButtonPath = PrefabFolder + "/StarButton.prefab";
    const string RollingBallPath = PrefabFolder + "/RollingBall.prefab";
    const string DoorShelfAssemblyPath = PrefabFolder + "/DoorShelfAssembly.prefab";
    const string CenterPillarPath = PrefabFolder + "/CenterPillar.prefab";
    const string WhiteSquarePath = SpriteFolder + "/WhiteSquare.png";
    const string BallCirclePath = SpriteFolder + "/BallCircle.png";

    static readonly Color BlockedWallColor = new Color(1f, 0.92f, 0.2f);

    [InitializeOnLoadMethod]
    static void EnsureOnLoad()
    {
        EditorApplication.delayCall += EnsurePrefabsExist;
    }

    [MenuItem("Tools/Create Game Prefabs")]
    public static void EnsurePrefabsExist()
    {
        EnsureFolder(SpriteFolder);
        EnsureFolder(PrefabFolder);

        Sprite boxSprite = EnsureSprite(WhiteSquarePath, 4, false, 4f);
        Sprite circleSprite = EnsureSprite(BallCirclePath, 32, true, 32f);

        EnsureStarButtonPrefab(boxSprite);
        EnsureRollingBallPrefab(circleSprite);
        EnsureDoorShelfAssemblyPrefab(boxSprite);
        EnsureCenterPillarPrefab(boxSprite);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    static Sprite EnsureSprite(string assetPath, int size, bool circle, float pixelsPerUnit)
    {
        if (!File.Exists(assetPath))
            WriteSpritePng(assetPath, size, circle);

        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = circle;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    static void WriteSpritePng(string path, int size, bool circle)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var center = new Vector2(size * 0.5f - 0.5f, size * 0.5f - 0.5f);
        float radius = size * 0.5f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inside = !circle || Vector2.Distance(new Vector2(x, y), center) <= radius;
                texture.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }

    static void EnsureStarButtonPrefab(Sprite sprite)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(StarButtonPath) != null)
            return;

        const float width = 1.8f;
        const float height = 0.18f;

        var go = new GameObject("StarButton");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(1f, 0.88f, 0.15f);
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(width, height);
        sr.sortingOrder = 4;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(width, height);

        go.AddComponent<StarButton>();
        PrefabUtility.SaveAsPrefabAsset(go, StarButtonPath);
        Object.DestroyImmediate(go);
    }

    static void EnsureRollingBallPrefab(Sprite sprite)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RollingBallPath) != null)
            return;

        var go = new GameObject("RollingBall");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(1f, 0.45f, 0.2f);
        sr.sortingOrder = 2;

        go.AddComponent<CircleCollider2D>();
        go.AddComponent<Rigidbody2D>();
        go.AddComponent<RollingBall>();

        PrefabUtility.SaveAsPrefabAsset(go, RollingBallPath);
        Object.DestroyImmediate(go);
    }

    static void EnsureDoorShelfAssemblyPrefab(Sprite sprite)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(DoorShelfAssemblyPath) != null)
            return;

        const float shelfWidth = 4f;
        const float shelfHeight = 0.2f;

        var root = new GameObject("DoorShelfAssembly");

        var shelfRenderer = root.AddComponent<SpriteRenderer>();
        shelfRenderer.sprite = sprite;
        shelfRenderer.color = BlockedWallColor;
        shelfRenderer.drawMode = SpriteDrawMode.Sliced;
        shelfRenderer.size = new Vector2(shelfWidth, shelfHeight);
        shelfRenderer.sortingOrder = 49;

        var shelfCollider = root.AddComponent<BoxCollider2D>();
        shelfCollider.size = new Vector2(shelfWidth, shelfHeight);

        var motion = root.AddComponent<GravityDoorShelf>();
        motion.travelMinY = -0.5f;
        motion.travelMaxY = 2.4f;

        CreateTrackChild(root.transform, "TrackLeft", sprite);
        CreateTrackChild(root.transform, "TrackRight", sprite);

        PrefabUtility.SaveAsPrefabAsset(root, DoorShelfAssemblyPath);
        Object.DestroyImmediate(root);
    }

    static void CreateTrackChild(Transform parent, string trackName, Sprite sprite)
    {
        var track = new GameObject(trackName);
        track.transform.SetParent(parent);

        var sr = track.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.18f, 0.2f, 0.26f);
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.sortingOrder = 0;
        sr.enabled = false;
    }

    static void EnsureCenterPillarPrefab(Sprite sprite)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(CenterPillarPath) != null)
            return;

        var go = new GameObject("CenterPillar");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Sliced;

        var col = go.AddComponent<BoxCollider2D>();

        var pillar = go.AddComponent<CenterPillar>();
        pillar.width = 1.2f;
        pillar.height = 6f;
        pillar.color = BlockedWallColor;
        pillar.sortingOrder = 2;

        PrefabUtility.SaveAsPrefabAsset(go, CenterPillarPath);
        Object.DestroyImmediate(go);
    }
}
#endif
