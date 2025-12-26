using System.Collections.Generic;
using UnityEngine;

public class IslandProgress : MonoBehaviour
{
    public static IslandProgress I { get; private set; }
    void Awake(){ I = this; }

    [SerializeField] private List<IslandData> allIslands;   // drop your 4 game islands here (not Main Island)
    private HashSet<string> _visited = new HashSet<string>();

    public IEnumerable<IslandData> Remaining()
    {
        foreach (var i in allIslands)
            if (!_visited.Contains(i.islandId))
                yield return i;
    }

    public bool AllDone => _visited.Count >= allIslands.Count;
    public void MarkCompleted(IslandData island){ if (island) _visited.Add(island.islandId); }
    public void ResetProgress(){ _visited.Clear(); }
}
