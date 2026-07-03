using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class RollingBall : MonoBehaviour
{
    public const float DefaultMass = 50f;
    public const float DefaultRadius = 0.35f;

    [SerializeField] float groundCheckDistance = 0.06f;

    Rigidbody2D rb;
    CircleCollider2D circle;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        circle = GetComponent<CircleCollider2D>();

        rb.gravityScale = 0f;
        rb.mass = DefaultMass;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        circle.radius = DefaultRadius;
        circle.sharedMaterial = GetNoBounceMaterial();
    }

    void FixedUpdate()
    {
        Vector2 gravityDir = WorldGravity.Direction.normalized;

        if (IsGrounded(gravityDir))
            RemoveFallSpeed(gravityDir);
        else
            rb.velocity += gravityDir * (WorldGravity.Strength * Time.fixedDeltaTime);

        StabilizeOnButton();
    }

    void StabilizeOnButton()
    {
        if (StarButton.Instance == null || !StarButton.Instance.IsSupporting(circle))
            return;

        Vector2 up = -WorldGravity.Direction.normalized;
        float liftSpeed = Vector2.Dot(rb.velocity, up);
        if (liftSpeed > 0f)
            rb.velocity -= up * liftSpeed;
    }

    void RemoveFallSpeed(Vector2 gravityDir)
    {
        float fallSpeed = Vector2.Dot(rb.velocity, gravityDir);
        if (fallSpeed > 0f)
            rb.velocity -= gravityDir * fallSpeed;
    }

    bool IsGrounded(Vector2 gravityDir)
    {
        Bounds bounds = circle.bounds;
        float reach = ProjectExtents(bounds.extents, gravityDir) + groundCheckDistance;

        RaycastHit2D[] hits = Physics2D.RaycastAll(bounds.center, gravityDir, reach);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.collider == circle)
                continue;
            if (hit.collider.isTrigger)
                continue;
            if (Vector2.Dot(hit.normal, -gravityDir) > 0.4f)
                return true;
        }

        return false;
    }

    static float ProjectExtents(Vector3 extents, Vector2 gravityDir)
    {
        Vector2 abs = new Vector2(Mathf.Abs(gravityDir.x), Mathf.Abs(gravityDir.y));
        return abs.x * extents.x + abs.y * extents.y;
    }

    static PhysicsMaterial2D ballMaterial;

    static PhysicsMaterial2D GetNoBounceMaterial()
    {
        if (ballMaterial != null)
            return ballMaterial;

        ballMaterial = new PhysicsMaterial2D("RollingBallNoBounce")
        {
            friction = 0f,
            bounciness = 0f
        };
        return ballMaterial;
    }
}
