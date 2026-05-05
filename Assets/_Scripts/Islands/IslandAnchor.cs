// IslandAnchor.cs
using UnityEngine;

public class IslandAnchor : MonoBehaviour
{
    [Tooltip("Must match IslandData.islandId, e.g. CARDS, BREAD, SKULL, POISON, MAIN")]
    public string islandId;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!string.IsNullOrWhiteSpace(islandId))
            islandId = islandId.Trim().ToUpperInvariant();
    }
#endif
}
