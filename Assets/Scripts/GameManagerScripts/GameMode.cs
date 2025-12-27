using UnityEngine;

public static class GameMode
{
    // When true obstacles should not kill or apply forces to player.
    public static bool IsCinematic = true;

    // When true, player should not die — useful for quick in-editor testing and a "Test Mode".
    public static bool IsTestMode = false;

    // Timestamp (Time.time) until which spawns are suppressed. Set via SetInitialSpawnSuppression.
    private static float noSpawnUntil = 0f;

    // Call to set a random or fixed delay at game start (seconds).
    public static void SetInitialSpawnSuppression(float seconds)
    {
        noSpawnUntil = Time.time + Mathf.Max(0f, seconds);
    }

    // Whether the initial no-spawn window is still active.
    public static bool IsInitialSpawnSuppressed => Time.time < noSpawnUntil;
}
