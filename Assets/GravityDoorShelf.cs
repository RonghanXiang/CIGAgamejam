using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class GravityDoorShelf : MonoBehaviour
{
    static readonly Color TrackColor = new Color(0.18f, 0.2f, 0.26f);

    public float travelMinY = -0.5f;
    public float travelMaxY = 2.4f;

    [SerializeField] float trackWidth = 0.1f;
    [SerializeField] int trackSortingOrder = 0;

    float speedAlongGravity;
    float halfHeight;
    float halfWidth;
    Vector2 initialPosition;

    SpriteRenderer leftTrackRenderer;
    SpriteRenderer rightTrackRenderer;

    static Sprite trackSprite;

    void Awake()
    {
        var col = GetComponent<BoxCollider2D>();
        halfHeight = col.size.y * 0.5f;
        halfWidth = col.size.x * 0.5f;
        initialPosition = transform.position;
        BindTracks();
        UpdateTracks();
    }

    void LateUpdate() => UpdateTracks();

    void FixedUpdate()
    {
        Vector2 gravityDir = WorldGravity.Direction.normalized;
        speedAlongGravity += WorldGravity.Strength * Time.fixedDeltaTime;

        Vector2 position = transform.position;
        Vector2 next = position + gravityDir * (speedAlongGravity * Time.fixedDeltaTime);
        Vector2 clamped = ClampToTravelBounds(next);

        if ((clamped - next).sqrMagnitude > 0.000001f)
            speedAlongGravity = 0f;

        transform.position = clamped;
    }

    void BindTracks()
    {
        leftTrackRenderer = transform.Find("TrackLeft")?.GetComponent<SpriteRenderer>();
        rightTrackRenderer = transform.Find("TrackRight")?.GetComponent<SpriteRenderer>();

        if (leftTrackRenderer == null)
            leftTrackRenderer = CreateTrackRenderer("TrackLeft");
        if (rightTrackRenderer == null)
            rightTrackRenderer = CreateTrackRenderer("TrackRight");
    }

    SpriteRenderer CreateTrackRenderer(string trackName)
    {
        var track = new GameObject(trackName);
        track.transform.SetParent(transform);

        var sr = track.AddComponent<SpriteRenderer>();
        sr.sprite = GetTrackSprite();
        sr.color = TrackColor;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.sortingOrder = trackSortingOrder;
        return sr;
    }

    void UpdateTracks()
    {
        if (leftTrackRenderer == null || rightTrackRenderer == null)
            return;

        if (!TryGetTravelBounds(out float minCenterY, out float maxCenterY))
        {
            leftTrackRenderer.enabled = false;
            rightTrackRenderer.enabled = false;
            return;
        }

        float trackBottom = minCenterY - halfHeight;
        float trackTop = maxCenterY + halfHeight;
        float trackHeight = trackTop - trackBottom;
        float trackCenterY = (trackBottom + trackTop) * 0.5f;
        float localTrackCenterY = trackCenterY - transform.position.y;

        ApplyTrackVisual(leftTrackRenderer, -halfWidth, localTrackCenterY, trackHeight);
        ApplyTrackVisual(rightTrackRenderer, halfWidth, localTrackCenterY, trackHeight);
    }

    void ApplyTrackVisual(SpriteRenderer sr, float localX, float localCenterY, float height)
    {
        sr.enabled = height > 0.001f;
        if (!sr.enabled)
            return;

        sr.transform.localPosition = new Vector3(localX, localCenterY, 0f);
        sr.size = new Vector2(trackWidth, height);
    }

    Vector2 ClampToTravelBounds(Vector2 position)
    {
        if (!TryGetTravelBounds(out float minY, out float maxY))
            return position;

        position.y = Mathf.Clamp(position.y, minY, maxY);
        return position;
    }

    bool TryGetTravelBounds(out float minCenterY, out float maxCenterY)
    {
        minCenterY = travelMinY;
        maxCenterY = travelMaxY;
        return maxCenterY > minCenterY;
    }

    static Sprite GetTrackSprite()
    {
        if (trackSprite != null)
            return trackSprite;

        var tex = new Texture2D(4, 4);
        var pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        trackSprite = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
        return trackSprite;
    }
}
