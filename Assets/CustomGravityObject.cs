using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class CustomGravityObject : MonoBehaviour
{
    [SerializeField] float groundCheckDistance = 0.06f;

    Rigidbody2D rb;
    Collider2D col;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.gravityScale = 0f;
    }

    void FixedUpdate()
    {
        Vector2 gravityDir = WorldGravity.Direction.normalized;

        if (IsGrounded(gravityDir))
            RemoveFallSpeed(gravityDir);
        else
            rb.velocity += gravityDir * (WorldGravity.Strength * Time.fixedDeltaTime);
    }

    void RemoveFallSpeed(Vector2 gravityDir)
    {
        float fallSpeed = Vector2.Dot(rb.velocity, gravityDir);
        if (fallSpeed > 0f)
            rb.velocity -= gravityDir * fallSpeed;
    }

    bool IsGrounded(Vector2 gravityDir)
    {
        Bounds bounds = col.bounds;
        float reach = ProjectExtents(bounds.extents, gravityDir) + groundCheckDistance;

        RaycastHit2D[] hits = Physics2D.RaycastAll(bounds.center, gravityDir, reach);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.collider == col)
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
}
