using UnityEngine;

[DefaultExecutionOrder(50)]
public class PlayerSpawn : MonoBehaviour
{
    void Awake()
    {
        var area = FindObjectOfType<PlayAreaBounds>();
        if (area == null)
            return;

        Vector2 spawn = area.GetLeftBottomSpawnPosition();
        transform.position = spawn;

        if (TryGetComponent<Rigidbody2D>(out var rb))
            rb.position = spawn;
    }
}
