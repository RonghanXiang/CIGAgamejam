using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DefaultExecutionOrder(-100)]
public class PlayAreaBounds : MonoBehaviour
{
    const string DoorShelfAssemblyPrefabPath = "Assets/Prefabs/DoorShelfAssembly.prefab";
    const string CenterPillarPrefabPath = "Assets/Prefabs/CenterPillar.prefab";
    const string DoorShelfAssemblyResourcesPath = "Prefabs/DoorShelfAssembly";
    const string CenterPillarResourcesPath = "Prefabs/CenterPillar";

    [SerializeField] Vector2 innerSize = new Vector2(36f, 10f);
    [SerializeField] float borderThickness = 0.6f;
    [SerializeField] Color areaColor = new Color(1f, 1f, 1f, 0.4f);
    [SerializeField] Color borderColor = Color.white;
    [SerializeField] Color blockedWallColor = new Color(1f, 0.92f, 0.2f);
    [SerializeField] GameObject doorShelfAssemblyPrefab;
    [SerializeField] GameObject centerPillarPrefab;

    static Sprite boxSprite;
    static PhysicsMaterial2D zeroFrictionMaterial;

    public Vector2 InnerSize => innerSize;

    void Awake() => Build();

    void OnValidate()
    {
        if (!Application.isPlaying)
            Build();
    }

    public void Build()
    {
        EnsurePrefabReferences();
        ClearChildren();

        float hw = innerSize.x * 0.5f;
        float hh = innerSize.y * 0.5f;
        float t = borderThickness;
        Vector2 center = transform.position;

        CreateQuad("AreaFill", center, innerSize, areaColor, -1, false, false);
        CreateQuad("Bottom", center + new Vector2(0f, -hh - t * 0.5f), new Vector2(innerSize.x + t * 2f, t), borderColor, 0, true, true);
        CreateQuad("Top", center + new Vector2(0f, hh + t * 0.5f), new Vector2(innerSize.x + t * 2f, t), borderColor, 0, true, true);
        CreateQuad("Left", center + new Vector2(-hw - t * 0.5f, 0f), new Vector2(t, innerSize.y), blockedWallColor, 0, true, false);
        CreateQuad("Right", center + new Vector2(hw + t * 0.5f, 0f), new Vector2(t, innerSize.y), blockedWallColor, 0, true, false);

        // 内侧边框碰撞体，与可见活动区域边缘对齐，供 Anchor 精确命中
        const float innerEdge = 0.12f;
        CreateQuad("InnerBottom", center + new Vector2(0f, -hh), new Vector2(innerSize.x, innerEdge), borderColor, 1, true, true);
        CreateQuad("InnerTop", center + new Vector2(0f, hh), new Vector2(innerSize.x, innerEdge), borderColor, 1, true, true);
        CreateQuad("InnerLeft", center + new Vector2(-hw, 0f), new Vector2(innerEdge, innerSize.y), blockedWallColor, 1, true, false);
        CreateQuad("InnerRight", center + new Vector2(hw, 0f), new Vector2(innerEdge, innerSize.y), blockedWallColor, 1, true, false);

        Vector2 doorPosition = GetVictoryDoorPosition(center, hw);
        CreateVictoryDoor(doorPosition);
        PlaceDoorShelfAssembly(doorPosition, center, hw, hh);
        PlaceCenterPillar(center, hh, hw);

        if (Application.isPlaying)
            EnsureCameraFollow();
    }

    void Start()
    {
        if (!Application.isPlaying)
            return;

        EnsurePrefabReferences();
        EnsureCameraFollow();

        if (transform.Find("CenterPillar") == null || transform.Find("DoorShelfAssembly") == null)
            Build();
    }

    void EnsureCameraFollow()
    {
        var cam = Camera.main;
        if (cam == null) return;
        if (cam.GetComponent<CameraFollow>() == null)
            cam.gameObject.AddComponent<CameraFollow>();
    }

    public Vector2 GetLeftBottomSpawnPosition()
    {
        float hw = innerSize.x * 0.5f;
        float hh = innerSize.y * 0.5f;
        Vector2 center = transform.position;

        const float playerHalfSize = 0.5f;
        const float edgeMargin = 0.4f;

        return center + new Vector2(
            -hw + edgeMargin + playerHalfSize,
            -hh + playerHalfSize);
    }

    Vector2 GetVictoryDoorPosition(Vector2 center, float halfWidth)
    {
        const float edgeInset = 0.9f;
        // 门放在关卡垂直正中；跳跃高度约 2.88，上下均无法触及 y≈0 附近的小体积门
        return center + new Vector2(
            halfWidth - edgeInset,
            0f);
    }

    void EnsurePrefabReferences()
    {
#if UNITY_EDITOR
        var shelfFromPath = AssetDatabase.LoadAssetAtPath<GameObject>(DoorShelfAssemblyPrefabPath);
        var pillarFromPath = AssetDatabase.LoadAssetAtPath<GameObject>(CenterPillarPrefabPath);
        if (shelfFromPath != null)
            doorShelfAssemblyPrefab = shelfFromPath;
        if (pillarFromPath != null)
            centerPillarPrefab = pillarFromPath;
#endif
        if (doorShelfAssemblyPrefab == null)
            doorShelfAssemblyPrefab = Resources.Load<GameObject>(DoorShelfAssemblyResourcesPath);
        if (centerPillarPrefab == null)
            centerPillarPrefab = Resources.Load<GameObject>(CenterPillarResourcesPath);
    }

    void PlaceCenterPillar(Vector2 center, float halfHeight, float halfWidth)
    {
        if (centerPillarPrefab == null)
        {
            Debug.LogWarning("PlayAreaBounds: missing CenterPillar prefab.");
            return;
        }

        float pillarHeight = 6f;
        var pillarSettings = centerPillarPrefab.GetComponent<CenterPillar>();
        if (pillarSettings != null)
            pillarHeight = pillarSettings.height;

        float floorY = center.y - halfHeight;
        float leftThirdX = center.x - halfWidth + innerSize.x / 3f;
        Vector3 position = new Vector3(leftThirdX, floorY + pillarHeight * 0.5f, 0f);
        PlacePrefab(centerPillarPrefab, "CenterPillar", position);
    }

    void PlaceDoorShelfAssembly(Vector2 doorPosition, Vector2 areaCenter, float halfWidth, float halfHeight)
    {
        if (doorShelfAssemblyPrefab == null)
        {
            Debug.LogWarning("PlayAreaBounds: missing DoorShelfAssembly prefab.");
            return;
        }

        const float victoryDoorWidth = 1f;
        const float victoryDoorHeight = 0.8f;
        const float doorShelfHeight = 0.2f;
        const float shelfWidth = victoryDoorWidth * 4f;

        float innerLeft = areaCenter.x - halfWidth;
        float innerRight = areaCenter.x + halfWidth;
        float doorRight = doorPosition.x + victoryDoorWidth * 0.5f;

        float shelfRight = Mathf.Min(doorRight, innerRight);
        float shelfLeft = shelfRight - shelfWidth;
        if (shelfLeft < innerLeft)
        {
            shelfLeft = innerLeft;
        }

        Vector3 position = new Vector3(
            (shelfLeft + shelfRight) * 0.5f,
            doorPosition.y - victoryDoorHeight * 0.5f - doorShelfHeight * 0.5f,
            0f);

        var instance = PlacePrefab(doorShelfAssemblyPrefab, "DoorShelfAssembly", position);
        if (instance == null)
            return;

        float quarter = innerSize.y * 0.25f;
        float shelfHalfHeight = doorShelfHeight * 0.5f;
        float areaBottom = areaCenter.y - halfHeight;
        float areaTop = areaCenter.y + halfHeight;
        float minCenterY = Mathf.Max(areaBottom + quarter + shelfHalfHeight, position.y);
        float maxCenterY = areaTop - quarter - shelfHalfHeight;

        var shelfMotion = instance.GetComponent<GravityDoorShelf>();
        if (shelfMotion != null)
        {
            shelfMotion.travelMinY = minCenterY;
            shelfMotion.travelMaxY = maxCenterY;
        }
    }

    GameObject PlacePrefab(GameObject prefab, string objectName, Vector3 position)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
            instance.name = objectName;
            instance.transform.position = position;
            return instance;
        }
#endif
        var go = Instantiate(prefab, position, Quaternion.identity, transform);
        go.name = objectName;
        return go;
    }

    const float VictoryDoorWidth = 1f;
    const float VictoryDoorHeight = 0.8f;

    void CreateVictoryDoor(Vector2 position)
    {
        var door = new GameObject("VictoryDoor");
        door.transform.SetParent(transform);
        door.transform.position = position;

        var sr = door.AddComponent<SpriteRenderer>();
        sr.sprite = GetBoxSprite();
        sr.color = new Color(0.2f, 0.95f, 0.45f);
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(VictoryDoorWidth, VictoryDoorHeight);
        sr.sortingOrder = 50;

        var col = door.AddComponent<BoxCollider2D>();
        col.size = new Vector2(VictoryDoorWidth, VictoryDoorHeight);
        col.isTrigger = true;

        door.AddComponent<VictoryDoor>();
    }

    void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            if (child.name == "VictoryCanvas")
                continue;

            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    GameObject CreateQuad(string objectName, Vector2 position, Vector2 size, Color color, int sortingOrder, bool collider, bool anchorTarget)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(transform);
        go.transform.position = position;

        if (anchorTarget)
            go.tag = "Wall";

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetBoxSprite();
        sr.color = color;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = size;
        sr.sortingOrder = sortingOrder;

        if (collider)
        {
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.sharedMaterial = GetZeroFrictionMaterial();
            if (objectName.StartsWith("Inner"))
                col.isTrigger = true;
        }

        if (anchorTarget)
        {
            go.AddComponent<AnchorWall>();
            if (objectName.StartsWith("Inner"))
                sr.enabled = false;
        }

        return go;
    }

    static PhysicsMaterial2D GetZeroFrictionMaterial()
    {
        if (zeroFrictionMaterial != null) return zeroFrictionMaterial;
        zeroFrictionMaterial = new PhysicsMaterial2D("ZeroFriction")
        {
            friction = 0f,
            bounciness = 0f
        };
        return zeroFrictionMaterial;
    }

    static Sprite GetBoxSprite()
    {
        if (boxSprite != null) return boxSprite;

        var tex = new Texture2D(4, 4);
        var pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        boxSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return boxSprite;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.6f);
        Gizmos.DrawWireCube(transform.position, new Vector3(innerSize.x, innerSize.y, 0f));
    }
}
