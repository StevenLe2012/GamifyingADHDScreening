// CountdownAnchor.cs
using UnityEngine;

public class CountdownAnchor : MonoBehaviour
{
    [Tooltip("Must match IslandData.islandId (e.g., CARDS, BREAD, SKULL, POISON).")]
    public string islandId;

    [Header("Placement")]
    [Tooltip("Meters forward from this anchor to place the countdown UI.")]
    public float forwardOffset = 1.25f;
    [Tooltip("Meters upward from this anchor to place the countdown UI.")]
    public float heightOffset = 0.0f;
    [Tooltip("Uniform scale applied to the countdown UI at this island.")]
    public float uiScale = 1.0f;

    [Header("Timing (optional)")]
    [Tooltip("If > 0, overrides the default countdown seconds for this island.")]
    public float overrideCountdownSeconds = 0f;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!string.IsNullOrWhiteSpace(islandId))
            islandId = islandId.Trim().ToUpperInvariant();
        uiScale = Mathf.Max(0.001f, uiScale);
    }
#endif
}
