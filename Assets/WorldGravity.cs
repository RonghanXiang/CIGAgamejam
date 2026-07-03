using UnityEngine;

public static class WorldGravity
{
    public static Vector2 Direction { get; private set; } = Vector2.down;
    public static float Strength { get; private set; } = 25f;

    public static void Set(Vector2 direction, float strength)
    {
        if (direction.sqrMagnitude < 0.001f)
            return;

        Direction = direction.normalized;
        Strength = strength;
    }
}
