using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class VictoryDoor : MonoBehaviour
{
    BoxCollider2D col;

    void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other) => TryWin(other);

    void OnTriggerStay2D(Collider2D other) => TryWin(other);

    void TryWin(Collider2D other)
    {
        var player = other.GetComponent<PlayerMove>() ?? other.GetComponentInParent<PlayerMove>();
        if (player == null)
            return;

        if (player.IsGrounded)
            return;

        GameVictory.Trigger();
    }
}
