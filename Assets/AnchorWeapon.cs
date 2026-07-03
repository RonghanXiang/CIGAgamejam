using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMove))]
public class AnchorWeapon : MonoBehaviour
{
    [SerializeField] float flySpeed = 32f;
    [SerializeField] float attachDistance = 0.55f;
    [SerializeField] float previewLineWidth = 0.045f;
    [SerializeField] int previewHoldFrames = 10;

    static readonly Color PreviewColor = new Color(0.4f, 0.95f, 1f, 0.85f);

    PlayerMove player;
    Rigidbody2D rb;
    BoxCollider2D playerCollider;
    Camera mainCamera;
    Transform dashRoot;

    readonly List<LineRenderer> dashPool = new List<LineRenderer>();
    int dashPoolUsed;

    bool isFlying;
    bool snapHasPreview;
    int previewHoldCounter;
    Vector3 snapScreenLineStart;
    Vector3 snapScreenLineEnd;
    readonly Vector3[] snapScreenOutlineCorners = new Vector3[4];
    Vector2 anchorPoint;
    Vector2 anchorNormal;
    Vector2 flyDirection;

    void Awake()
    {
        player = GetComponent<PlayerMove>();
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<BoxCollider2D>();
        mainCamera = Camera.main;

        dashRoot = new GameObject("DashLines").transform;
    }

    void LateUpdate()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (isFlying)
        {
            UpdateFlyVisuals();
            return;
        }

        if (previewHoldCounter == 0)
            CapturePreviewSnapshot();

        RenderPreviewSnapshot();

        previewHoldCounter++;
        if (previewHoldCounter >= Mathf.Max(1, previewHoldFrames))
            previewHoldCounter = 0;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TryFire();
    }

    void FixedUpdate()
    {
        if (!isFlying) return;

        float remaining = Vector2.Dot(anchorPoint - rb.position, flyDirection);
        if (remaining <= attachDistance)
        {
            LandOnWall();
            return;
        }

        rb.velocity = flyDirection * flySpeed;
    }

    void CapturePreviewSnapshot()
    {
        snapHasPreview = false;

        if (mainCamera == null)
            return;

        Vector2 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = GetAimDirection(mouseWorld);
        if (direction.sqrMagnitude < 0.01f)
            return;

        if (!TryResolveAnchorTarget(direction, out RaycastHit2D hit))
            return;

        Vector2 playerCenter = rb.position;
        if (!TryComputeLanding(playerCenter, hit.point, out Vector2 landing))
            return;

        float landingAngle = GetLandingAngle(hit.normal);
        snapScreenLineStart = mainCamera.WorldToScreenPoint(playerCenter);
        snapScreenLineEnd = mainCamera.WorldToScreenPoint(landing);
        CaptureOutlineScreenCorners(landing, landingAngle, snapScreenOutlineCorners);
        snapHasPreview = true;
    }

    void RenderPreviewSnapshot()
    {
        ClearDashPool();

        if (!snapHasPreview || mainCamera == null)
            return;

        Vector2 lineStart = ScreenPointToWorld(snapScreenLineStart);
        Vector2 lineEnd = ScreenPointToWorld(snapScreenLineEnd);
        DrawSolidLine(lineStart, lineEnd, previewLineWidth, PreviewColor, 21);

        Vector2 c0 = ScreenPointToWorld(snapScreenOutlineCorners[0]);
        Vector2 c1 = ScreenPointToWorld(snapScreenOutlineCorners[1]);
        Vector2 c2 = ScreenPointToWorld(snapScreenOutlineCorners[2]);
        Vector2 c3 = ScreenPointToWorld(snapScreenOutlineCorners[3]);
        float outlineWidth = previewLineWidth * 0.85f;
        DrawSolidLine(c0, c1, outlineWidth, PreviewColor, 22);
        DrawSolidLine(c1, c2, outlineWidth, PreviewColor, 22);
        DrawSolidLine(c2, c3, outlineWidth, PreviewColor, 22);
        DrawSolidLine(c3, c0, outlineWidth, PreviewColor, 22);
    }

    void CaptureOutlineScreenCorners(Vector2 center, float angleDeg, Vector3[] screenCorners)
    {
        GetOutlineCorners(center, angleDeg,
            out Vector2 c0, out Vector2 c1, out Vector2 c2, out Vector2 c3);

        screenCorners[0] = mainCamera.WorldToScreenPoint(c0);
        screenCorners[1] = mainCamera.WorldToScreenPoint(c1);
        screenCorners[2] = mainCamera.WorldToScreenPoint(c2);
        screenCorners[3] = mainCamera.WorldToScreenPoint(c3);
    }

    Vector2 ScreenPointToWorld(Vector3 screenPoint)
    {
        screenPoint.z = mainCamera.WorldToScreenPoint(rb.position).z;
        Vector3 world = mainCamera.ScreenToWorldPoint(screenPoint);
        return new Vector2(world.x, world.y);
    }

    void UpdateFlyVisuals()
    {
        ClearDashPool();
        DrawPullPreview(rb.position, anchorPoint, anchorNormal);
    }

    void DrawPullPreview(Vector2 playerCenter, Vector2 hitPoint, Vector2 surfaceNormal)
    {
        if (!TryComputeLanding(playerCenter, hitPoint, out Vector2 landing))
            return;

        float landingAngle = GetLandingAngle(surfaceNormal);
        DrawSolidLine(playerCenter, landing, previewLineWidth, PreviewColor, 21);
        DrawPlayerOutline(landing, landingAngle, previewLineWidth * 0.85f, PreviewColor, 22);
    }

    bool TryComputeLanding(Vector2 playerCenter, Vector2 hitPoint, out Vector2 landing)
    {
        Vector2 toHit = hitPoint - playerCenter;
        if (toHit.sqrMagnitude < 0.001f)
        {
            landing = playerCenter;
            return false;
        }

        Vector2 pullDir = toHit.normalized;
        landing = GetPredictedLandingPosition(playerCenter, hitPoint, pullDir);
        return true;
    }

    Vector2 GetPredictedLandingPosition(Vector2 playerCenter, Vector2 hitPoint, Vector2 pullDir)
    {
        float remaining = (hitPoint - playerCenter).magnitude;
        float travel = Mathf.Max(0f, remaining - attachDistance);
        return playerCenter + pullDir * travel;
    }

    float GetLandingAngle(Vector2 surfaceNormal)
    {
        Vector2 normal = surfaceNormal.normalized;
        return Mathf.Atan2(normal.x, normal.y) * Mathf.Rad2Deg;
    }

    void DrawPlayerOutline(Vector2 center, float angleDeg, float width, Color color, int sortingOrder)
    {
        GetOutlineCorners(center, angleDeg, out Vector2 c0, out Vector2 c1, out Vector2 c2, out Vector2 c3);
        DrawSolidLine(c0, c1, width, color, sortingOrder);
        DrawSolidLine(c1, c2, width, color, sortingOrder);
        DrawSolidLine(c2, c3, width, color, sortingOrder);
        DrawSolidLine(c3, c0, width, color, sortingOrder);
    }

    void GetOutlineCorners(Vector2 center, float angleDeg,
        out Vector2 c0, out Vector2 c1, out Vector2 c2, out Vector2 c3)
    {
        Vector2 half = playerCollider.size * 0.5f;
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        Vector2 LocalToWorld(Vector2 local)
        {
            return center + new Vector2(
                local.x * cos - local.y * sin,
                local.x * sin + local.y * cos);
        }

        c0 = LocalToWorld(new Vector2(-half.x, -half.y));
        c1 = LocalToWorld(new Vector2(half.x, -half.y));
        c2 = LocalToWorld(new Vector2(half.x, half.y));
        c3 = LocalToWorld(new Vector2(-half.x, half.y));
    }

    void DrawSolidLine(Vector2 start, Vector2 end, float width, Color color, int sortingOrder)
    {
        if ((end - start).sqrMagnitude < 0.001f)
            return;

        AcquireLine(width, color, sortingOrder, start, end);
    }

    void AcquireLine(float width, Color color, int sortingOrder, Vector2 a, Vector2 b)
    {
        LineRenderer line;
        if (dashPoolUsed < dashPool.Count)
        {
            line = dashPool[dashPoolUsed];
        }
        else
        {
            line = CreateLine($"PreviewLine{dashPool.Count}", width, width, color, sortingOrder);
            dashPool.Add(line);
        }

        dashPoolUsed++;
        line.startWidth = width;
        line.endWidth = width;
        line.startColor = color;
        line.endColor = color;
        line.sortingOrder = sortingOrder;
        line.positionCount = 2;
        line.SetPosition(0, a);
        line.SetPosition(1, b);
        line.enabled = true;
    }

    void ClearDashPool()
    {
        for (int i = 0; i < dashPoolUsed; i++)
            dashPool[i].enabled = false;
        dashPoolUsed = 0;
    }

    LineRenderer CreateLine(string lineName, float startWidth, float endWidth, Color color, int sortingOrder)
    {
        var lineGo = new GameObject(lineName);
        lineGo.transform.SetParent(dashRoot);

        var line = lineGo.AddComponent<LineRenderer>();
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startWidth = startWidth;
        line.endWidth = endWidth;
        line.startColor = color;
        line.endColor = color;
        line.sortingOrder = sortingOrder;
        line.useWorldSpace = true;
        line.numCapVertices = 2;
        line.enabled = false;
        return line;
    }

    Vector2 GetRayOrigin(Vector2 direction)
    {
        Bounds b = playerCollider.bounds;
        Vector2 center = rb.position;
        float radius = Mathf.Abs(direction.x) * b.extents.x + Mathf.Abs(direction.y) * b.extents.y + 0.05f;
        return center + direction * radius;
    }

    Vector2 GetAimDirection(Vector2 mouseWorld)
    {
        Vector2 delta = mouseWorld - rb.position;
        if (delta.sqrMagnitude < 0.01f)
            return Vector2.zero;

        return delta.normalized;
    }

    bool TryResolveAnchorTarget(Vector2 direction, out RaycastHit2D hit)
    {
        hit = default;
        Vector2 origin = GetRayOrigin(direction);
        if (!TryRaycastAnchorWall(origin, direction, out hit))
            return false;

        Vector2 playerCenter = rb.position;
        Vector2 pullDir = (hit.point - playerCenter).normalized;
        float travel = Mathf.Max(0f, (hit.point - playerCenter).magnitude - attachDistance);
        return IsFlightPathClear(playerCenter, pullDir, travel, hit.collider);
    }

    bool IsFlightPathClear(Vector2 playerCenter, Vector2 direction, float distance, Collider2D destination)
    {
        if (distance <= 0.001f)
            return true;

        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            playerCenter,
            playerCollider.size * 0.98f,
            playerCollider.transform.eulerAngles.z,
            direction,
            distance);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.collider.transform == transform)
                continue;
            if (hit.collider == destination)
                continue;
            if (hit.collider.isTrigger)
                continue;
            if (AnchorWall.IsHittable(hit.collider))
                continue;

            return false;
        }

        return true;
    }

    bool TryRaycastAnchorWall(Vector2 origin, Vector2 direction, out RaycastHit2D result)
    {
        result = default;
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, Mathf.Infinity);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.collider.transform == transform)
                continue;

            if (AnchorWall.IsHittable(hit.collider))
            {
                result = hit;
                return true;
            }

            if (!hit.collider.isTrigger)
                return false;
        }

        return false;
    }

    void TryFire()
    {
        if (isFlying || mainCamera == null) return;

        Vector2 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = GetAimDirection(mouseWorld);
        if (direction.sqrMagnitude < 0.001f) return;

        if (!TryResolveAnchorTarget(direction, out RaycastHit2D hit)) return;

        anchorPoint = hit.point;
        anchorNormal = hit.normal;
        flyDirection = (hit.point - rb.position).normalized;
        isFlying = true;

        player.SetMovementEnabled(false);
        rb.velocity = flyDirection * flySpeed;
    }

    void LandOnWall()
    {
        rb.velocity = Vector2.zero;
        player.SetGravityFromSurfaceNormal(anchorNormal);
        FinishFly();
    }

    void FinishFly()
    {
        isFlying = false;
        player.SetMovementEnabled(true);
        previewHoldCounter = 0;
        ClearDashPool();
    }
}
