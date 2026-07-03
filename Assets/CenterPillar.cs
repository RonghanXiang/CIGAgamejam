using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class CenterPillar : MonoBehaviour
{
    public float width = 1.2f;
    public float height = 6f;
    public Color color = new Color(1f, 0.92f, 0.2f);
    public int sortingOrder = 2;

    SpriteRenderer spriteRenderer;
    BoxCollider2D boxCollider;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        ApplySettings();
    }

    void OnValidate()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider2D>();
        ApplySettings();
    }

    void ApplySettings()
    {
        if (spriteRenderer == null || boxCollider == null)
            return;

        spriteRenderer.color = color;
        spriteRenderer.drawMode = SpriteDrawMode.Sliced;
        spriteRenderer.sortingOrder = sortingOrder;
        spriteRenderer.size = new Vector2(width, height);
        boxCollider.size = new Vector2(width, height);
    }
}
