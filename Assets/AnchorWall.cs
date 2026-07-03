using UnityEngine;

/// <summary>
/// 标记可被 Anchor 命中的活动区域边框。
/// </summary>
public class AnchorWall : MonoBehaviour
{
    public static bool IsHittable(Collider2D col)
    {
        if (col == null) return false;
        if (col.CompareTag("Wall")) return true;
        return col.GetComponent<AnchorWall>() != null;
    }
}
