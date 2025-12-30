using UnityEngine;

// Drop one of these in each island. Set islandId to match IslandData.islandId.
public class CountdownAnchor : MonoBehaviour
{
    [Tooltip("Must match IslandData.islandId (e.g., CARDS, BREAD, SKULL, POISON).")]
    public string islandId;

    [Header("Optional")]
    [Tooltip("If > 0, overrides the default/island countdown seconds for this island.")]
    public float overrideCountdownSeconds = -1f;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!string.IsNullOrWhiteSpace(islandId))
            islandId = islandId.Trim().ToUpperInvariant();
    }
#endif
}
