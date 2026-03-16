using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class IslandProgress : MonoBehaviour
{
    public static IslandProgress I { get; private set; }
    void Awake(){ I = this; }

    [Header("Base Islands (no hub)")]
    [Tooltip("Drop your 4 standard game islands here (CARDS, BREAD, POISON, SKULL, etc.).")]
    [SerializeField] private List<IslandData> allIslands = new List<IslandData>();

    [Header("Bonus / Unlockable")]
    [Tooltip("Optional bonus island (e.g., DRAGON). Leave null to disable bonus logic.")]
    [SerializeField] private IslandData bonusIsland;
    [Tooltip("How many islands must be completed before the bonus island unlocks.")]
    [SerializeField] private int bonusUnlockThreshold = 4;

    // Internal state
    private readonly HashSet<string> _visited = new HashSet<string>(); // uppercased ids
    private bool _bonusUnlocked;

    // ---------- Query ----------

    /// <summary> Islands the player has not completed yet. If the bonus is unlocked, it will be included here. </summary>
    public IEnumerable<IslandData> Remaining(bool includeBonus = true)
    {
        // base islands
        foreach (var i in allIslands)
        {
            if (!IsCompleted(i?.islandId))
                yield return i;
        }

        // bonus island
        if (includeBonus && _bonusUnlocked && bonusIsland && !IsCompleted(bonusIsland.islandId))
            yield return bonusIsland;
    }

    public bool AllDone =>
        // All base islands complete AND (bonus is either complete or not configured/unlocked)
        allIslands.All(i => IsCompleted(i?.islandId)) &&
        (!bonusIsland || (_bonusUnlocked ? IsCompleted(bonusIsland.islandId) : true));

    public int CompletedCount => _visited.Count;

    public bool IsCompleted(string islandId)
    {
        if (string.IsNullOrWhiteSpace(islandId)) return false;
        return _visited.Contains(islandId.Trim().ToUpperInvariant());
    }

    public bool BonusUnlocked => _bonusUnlocked;

    // ---------- Mutations ----------

    public void MarkCompleted(IslandData island)
    {
        if (!island) return;
        MarkCompletedById(island.islandId);
    }

    public void MarkCompletedById(string islandId)
    {
        if (string.IsNullOrWhiteSpace(islandId)) return;
        var idU = islandId.Trim().ToUpperInvariant();
        if (_visited.Add(idU))
        {
            Debug.Log($"[IslandProgress] Completed += {idU}. Count={_visited.Count}");
            EnsureBonusUnlocked();
        }
    }

    public void ResetProgress()
    {
        _visited.Clear();
        _bonusUnlocked = false;
        Debug.Log("[IslandProgress] Progress reset.");
    }

    // ---------- Helpers ----------

    private void EnsureBonusUnlocked()
    {
        if (_bonusUnlocked) return;
        if (!bonusIsland) return; // no bonus configured
        if (_visited.Count >= bonusUnlockThreshold)
        {
            _bonusUnlocked = true;
            Debug.Log($"[IslandProgress] Bonus unlocked → {bonusIsland.islandId}");
        }
    }
}
