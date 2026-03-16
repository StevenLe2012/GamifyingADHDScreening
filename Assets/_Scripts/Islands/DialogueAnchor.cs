using UnityEngine;

public class DialogueAnchor : MonoBehaviour
{
    [Tooltip("Must match IslandData.islandId, e.g. CARDS, BREAD, SKULL, POISON")]
    public string islandId;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!string.IsNullOrWhiteSpace(islandId))
            islandId = islandId.Trim().ToUpperInvariant();
    }
#endif
}
