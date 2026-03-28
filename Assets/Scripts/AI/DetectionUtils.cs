using UnityEngine;

public static class DetectionUtils
{
    /// <summary>
    /// Check if an AI can detect the player, accounting for distance and visibility.
    /// No line-of-sight raycast — just distance scaled by the player's visibility score.
    /// </summary>
    public static bool CanDetectPlayer(
        Vector3 aiPosition,
        float baseDetectionRange,
        PlayerVisibility visibility,
        Transform playerTransform,
        float visibilityMultiplier = 1f)
    {
        if (playerTransform == null) return false;

        float distance = Vector3.Distance(aiPosition, playerTransform.position);

        // If no visibility component, fall back to raw distance
        if (visibility == null)
            return distance <= baseDetectionRange;

        // Effective range scales with visibility; higher multiplier = worse eyesight
        float effectiveRange = baseDetectionRange * visibility.Visibility * (1f / Mathf.Max(0.01f, visibilityMultiplier));
        return distance <= effectiveRange;
    }
}
