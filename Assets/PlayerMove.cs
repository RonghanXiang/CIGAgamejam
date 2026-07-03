using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerMove : MonoBehaviour
{
    [SerializeField] float moveSpeed = 12f;
    [SerializeField] float jumpForce = 12f;
    [SerializeField] float gravityStrength = 25f;
    [SerializeField] float groundCheckDistance = 0.1f;
    [SerializeField] float coyoteTime = 0.12f;
    [SerializeField] float jumpBufferTime = 0.12f;

    Rigidbody2D rb;
    BoxCollider2D box;

    Vector2 gravityDirection = Vector2.down;
    bool canMove = true;
    bool jumpRequested;
    float coyoteTimer;
    float jumpBufferTimer;

    static Sprite whiteSquareSprite;

    public Vector2 GravityDirection => gravityDirection;
    public bool IsGrounded => coyoteTimer > 0f;
    public bool CanMove => canMove;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        box.sharedMaterial = new PhysicsMaterial2D("PlayerMaterial")
        {
            friction = 0f,
            bounciness = 0f
        };

        Physics2D.gravity = Vector2.zero;

        WorldGravity.Set(gravityDirection, gravityStrength);

        SetupVisual();

        if (GetComponent<PlayerOutline>() == null)
            gameObject.AddComponent<PlayerOutline>();
    }

    void SetupVisual()
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetWhiteSquareSprite();
        spriteRenderer.color = Color.white;
        spriteRenderer.drawMode = SpriteDrawMode.Sliced;
        spriteRenderer.size = box.size;
        spriteRenderer.sortingOrder = 1;
    }

    static Sprite GetWhiteSquareSprite()
    {
        if (whiteSquareSprite != null)
            return whiteSquareSprite;

        var texture = new Texture2D(4, 4);
        var pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;

        whiteSquareSprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
        return whiteSquareSprite;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump"))
            jumpBufferTimer = jumpBufferTime;

        if (jumpBufferTimer > 0f)
            jumpBufferTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        bool onSurface = CheckGrounded();
        if (onSurface)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.fixedDeltaTime;

        bool wantJump = canMove && jumpBufferTimer > 0f && coyoteTimer > 0f;

        if (!canMove)
        {
            jumpBufferTimer = 0f;
            AlignToGravity();
            return;
        }

        float input = 0f;
        if (Input.GetKey(KeyCode.A)) input -= 1f;
        if (Input.GetKey(KeyCode.D)) input += 1f;

        Vector2 tangent = new Vector2(-gravityDirection.y, gravityDirection.x);
        float speedAlongGravity = Vector2.Dot(rb.velocity, gravityDirection);

        if (wantJump)
        {
            speedAlongGravity = -jumpForce;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }
        else if (!onSurface)
        {
            speedAlongGravity += gravityStrength * Time.fixedDeltaTime;
        }

        rb.velocity = tangent * (input * moveSpeed) + gravityDirection * speedAlongGravity;
        AlignToGravity();
    }

    bool CheckGrounded()
    {
        Bounds b = box.bounds;
        float reach = ProjectExtentsOntoGravity(b.extents) + groundCheckDistance;
        RaycastHit2D[] hits = Physics2D.RaycastAll(b.center, gravityDirection, reach);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.collider.transform == transform) continue;
            if (hit.collider.isTrigger) continue;
            if (Vector2.Dot(hit.normal, -gravityDirection) > 0.4f)
                return true;
        }

        return false;
    }

    float ProjectExtentsOntoGravity(Vector3 extents)
    {
        Vector2 absGrav = new Vector2(Mathf.Abs(gravityDirection.x), Mathf.Abs(gravityDirection.y));
        return absGrav.x * extents.x + absGrav.y * extents.y;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!canMove || collision.collider.isTrigger) return;

        ContactPoint2D best = default;
        float bestScore = float.MinValue;

        for (int i = 0; i < collision.contactCount; i++)
        {
            var contact = collision.GetContact(i);
            float score = Vector2.Dot(contact.normal, -gravityDirection);
            if (score > bestScore)
            {
                bestScore = score;
                best = contact;
            }
        }

        if (bestScore < 0.5f) return;

        float fallSpeed = Vector2.Dot(rb.velocity, gravityDirection);
        if (fallSpeed > 1.5f || !CheckGrounded())
            SetGravityFromSurfaceNormal(best.normal);
    }

    void AlignToGravity()
    {
        float angle = Mathf.Atan2(-gravityDirection.x, -gravityDirection.y) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void SetGravityFromSurfaceNormal(Vector2 surfaceNormal)
    {
        if (surfaceNormal.sqrMagnitude < 0.001f) return;
        gravityDirection = (-surfaceNormal).normalized;
        WorldGravity.Set(gravityDirection, gravityStrength);

        float fallSpeed = Vector2.Dot(rb.velocity, gravityDirection);
        if (fallSpeed < 0f)
            rb.velocity -= fallSpeed * gravityDirection;
    }

    public void SetMovementEnabled(bool enabled)
    {
        canMove = enabled;
        if (!enabled)
        {
            Vector2 tangent = new Vector2(-gravityDirection.y, gravityDirection.x);
            float lateral = Vector2.Dot(rb.velocity, tangent);
            rb.velocity -= tangent * lateral;
        }
    }
}
