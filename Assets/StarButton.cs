using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class StarButton : MonoBehaviour
{
    public static StarButton Instance { get; private set; }

    public const float MaxLiftMass = 10f;

    [SerializeField] float maxPressDepth = 0.12f;
    [SerializeField] float pressedThreshold = 0.35f;
    [SerializeField] float reboundClearanceHeight = 1.4f;

    static PhysicsMaterial2D noBounceMaterial;

    readonly List<Collider2D> overlapBuffer = new List<Collider2D>(8);
    ContactFilter2D contactFilter;

    BoxCollider2D platformCollider;
    Vector3 restWorldPosition;
    float currentDepth;
    bool wasPressed;

    public bool IsPressed => currentDepth >= maxPressDepth * pressedThreshold;
    public float PressDepth => currentDepth;

    void Awake()
    {
        Instance = this;
        platformCollider = GetComponent<BoxCollider2D>();
        platformCollider.sharedMaterial = GetNoBounceMaterial();

        contactFilter = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = true,
            layerMask = Physics2D.AllLayers
        };

        restWorldPosition = transform.position;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void FixedUpdate()
    {
        float targetDepth = MeasurePressDepth();

        if (targetDepth >= currentDepth)
            currentDepth = targetDepth;
        else if (IsClearAbove())
            currentDepth = targetDepth;

        ApplyDepth(currentDepth);
        UpdateStarReward();
    }

    void UpdateStarReward()
    {
        bool pressed = IsPressed;
        if (pressed && !wasPressed)
            GameVictory.AddStar();
        else if (!pressed && wasPressed)
            GameVictory.RemoveStar();

        wasPressed = pressed;
    }

    void ApplyDepth(float depth)
    {
        Vector2 sinkDir = WorldGravity.Direction.normalized;
        Vector3 newPos = restWorldPosition + (Vector3)(sinkDir * depth);
        Vector3 delta = newPos - transform.position;
        transform.position = newPos;

        if (delta.sqrMagnitude <= 0.000001f)
            return;

        CarryLightRiders((Vector2)delta);
    }

    void CarryLightRiders(Vector2 delta)
    {
        foreach (var rider in GetRiders())
        {
            if (rider == null || rider.mass >= MaxLiftMass)
                continue;

            rider.MovePosition(rider.position + delta);
            KillSeparationVelocity(rider);
        }
    }

    IEnumerable<Rigidbody2D> GetRiders()
    {
        overlapBuffer.Clear();
        platformCollider.OverlapCollider(contactFilter, overlapBuffer);

        Vector2 up = GetUp();
        Bounds platform = platformCollider.bounds;

        foreach (var col in overlapBuffer)
        {
            if (col == platformCollider || col.isTrigger)
                continue;

            Rigidbody2D rb = col.attachedRigidbody;
            if (rb == null)
                continue;

            if (GetContactRatio(col, platform, up) <= 0.01f)
                continue;

            yield return rb;
        }
    }

    bool IsClearAbove()
    {
        Vector2 up = GetUp();
        Bounds platform = platformCollider.bounds;
        float halfHeight = GetExtentAlong(up, platform.extents);
        Vector2 checkCenter = (Vector2)platform.center + up * (halfHeight + reboundClearanceHeight * 0.5f);
        Vector2 checkSize = new Vector2(platform.size.x * 1.05f, reboundClearanceHeight);
        float angle = Vector2.SignedAngle(Vector2.up, up);

        Collider2D[] hits = Physics2D.OverlapBoxAll(checkCenter, checkSize, angle);
        foreach (var col in hits)
        {
            if (col == platformCollider || col.isTrigger)
                continue;
            if (col.attachedRigidbody == null)
                continue;
            if (GetHorizontalOverlap(platform, col.bounds) <= 0.01f)
                continue;

            return false;
        }

        return true;
    }

    static float GetExtentAlong(Vector2 axis, Vector3 extents)
    {
        Vector2 abs = new Vector2(Mathf.Abs(axis.x), Mathf.Abs(axis.y));
        return abs.x * extents.x + abs.y * extents.y;
    }

    public bool IsSupporting(Collider2D col)
    {
        if (col == null)
            return false;

        Rigidbody2D rb = col.attachedRigidbody;
        if (rb == null)
            return false;

        overlapBuffer.Clear();
        platformCollider.OverlapCollider(contactFilter, overlapBuffer);

        foreach (var other in overlapBuffer)
        {
            if (other == col)
                return GetContactRatio(other, platformCollider.bounds, GetUp()) > 0.01f;
        }

        return false;
    }

    float MeasurePressDepth()
    {
        overlapBuffer.Clear();
        platformCollider.OverlapCollider(contactFilter, overlapBuffer);

        Vector2 up = GetUp();
        Bounds platform = platformCollider.bounds;
        float ratio = 0f;

        foreach (var col in overlapBuffer)
        {
            if (col == platformCollider || col.isTrigger)
                continue;
            if (col.attachedRigidbody == null)
                continue;

            ratio = Mathf.Max(ratio, GetContactRatio(col, platform, up));
        }

        return maxPressDepth * Mathf.Clamp01(ratio);
    }

    float GetContactRatio(Collider2D col, Bounds platform, Vector2 up)
    {
        float platformTop = Dot(platform.max, up);
        float objTop = Dot(col.bounds.max, up);
        float objBottom = Dot(col.bounds.min, up);

        if (objTop < platformTop - 0.1f)
            return 0f;
        if (objBottom > platformTop + 0.3f)
            return 0f;

        float overlap = GetHorizontalOverlap(platform, col.bounds);

        if (col is CircleCollider2D circle)
            overlap = Mathf.Max(overlap, GetCircleOverlap(circle, platform));

        return overlap;
    }

    float GetCircleOverlap(CircleCollider2D circle, Bounds platform)
    {
        Vector2 center = circle.transform.TransformPoint(circle.offset);
        float scale = Mathf.Max(Mathf.Abs(circle.transform.lossyScale.x), Mathf.Abs(circle.transform.lossyScale.y));
        float radius = circle.radius * scale;

        float left = Mathf.Max(platform.min.x, center.x - radius);
        float right = Mathf.Min(platform.max.x, center.x + radius);
        if (right <= left)
            return 0f;

        return (right - left) / Mathf.Max(0.001f, platform.size.x);
    }

    static void KillSeparationVelocity(Rigidbody2D body)
    {
        Vector2 up = GetUp();
        float liftSpeed = Vector2.Dot(body.velocity, up);
        if (liftSpeed > 0f)
            body.velocity -= up * liftSpeed;
    }

    static float GetHorizontalOverlap(Bounds a, Bounds b)
    {
        float left = Mathf.Max(a.min.x, b.min.x);
        float right = Mathf.Min(a.max.x, b.max.x);
        if (right <= left)
            return 0f;

        return (right - left) / Mathf.Max(0.001f, a.size.x);
    }

    static PhysicsMaterial2D GetNoBounceMaterial()
    {
        if (noBounceMaterial != null)
            return noBounceMaterial;

        noBounceMaterial = new PhysicsMaterial2D("StarButtonNoBounce")
        {
            friction = 0f,
            bounciness = 0f
        };
        return noBounceMaterial;
    }

    static Vector2 GetUp() => -WorldGravity.Direction.normalized;

    static float Dot(Vector2 point, Vector2 axis) => Vector2.Dot(point, axis);
}
