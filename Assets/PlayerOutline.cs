using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PlayerOutline : MonoBehaviour
{
    [SerializeField] Color outlineColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] float lineWidth = 0.04f;
    [SerializeField] int sortingOrder = 3;

    BoxCollider2D box;
    LineRenderer line;

    void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        line = CreateOutlineLine();
    }

    void LateUpdate() => UpdateOutline();

    LineRenderer CreateOutlineLine()
    {
        var go = new GameObject("PlayerOutline");
        go.transform.SetParent(transform, false);

        var lr = go.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.numCapVertices = 2;
        lr.sortingOrder = sortingOrder;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.startColor = outlineColor;
        lr.endColor = outlineColor;
        lr.positionCount = 4;
        return lr;
    }

    void UpdateOutline()
    {
        Vector2 half = box.size * 0.5f;
        Vector2 offset = box.offset;

        line.SetPosition(0, TransformCorner(-half.x, -half.y, offset));
        line.SetPosition(1, TransformCorner(half.x, -half.y, offset));
        line.SetPosition(2, TransformCorner(half.x, half.y, offset));
        line.SetPosition(3, TransformCorner(-half.x, half.y, offset));
    }

    Vector3 TransformCorner(float localX, float localY, Vector2 offset)
    {
        return transform.TransformPoint(new Vector3(localX + offset.x, localY + offset.y, 0f));
    }
}
