// IslandMoxoGroup.cs
using UnityEngine;

public class IslandMoxoGroup : MonoBehaviour
{
    [Tooltip("Must match IslandData.islandId (e.g., CARDS, BREAD, SKULL, POISON).")]
    public string islandId;

    [Tooltip("The MOXO root (your 'MoxoCPT Manager' GameObject) under this island.")]
    public GameObject moxoRoot;
}
