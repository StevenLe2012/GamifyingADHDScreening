// IslandAnchor.cs
using UnityEngine;

public class IslandAnchor : MonoBehaviour
{
    [Tooltip("Must match IslandData.islandId, e.g. CARDS, BREAD, SKULL, POISON, MAIN")]
    public string islandId;

    [Tooltip("Vertical look angle applied when the player arrives (degrees). 0 = horizon.")]
    public float spawnCameraPitch = 0f;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!string.IsNullOrWhiteSpace(islandId))
            islandId = islandId.Trim().ToUpperInvariant();
    }
#endif
}
