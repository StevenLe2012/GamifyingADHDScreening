using System.Collections;
using UnityEngine;
using MoxoCPT;

/// <summary>
/// Backwards-compat shims for older call sites that still use RunTraining(...).
/// All paths forward to TrainingCPTRunner.RunTrainingForActiveIsland(string).
/// Remove this once you’ve updated all call sites to the new API.
/// </summary>
public static class TrainingCPTRunnerCompat
{
    // Core forwarder
    private static IEnumerator Forward(TrainingCPTRunner trainer, string islandId)
    {
        if (trainer == null) yield break;
        islandId = (islandId ?? IslandTravelManager.I?.CurrentIsland?.islandId ?? "").Trim().ToUpperInvariant();
        yield return trainer.RunTrainingForActiveIsland(islandId);
    }

    // --- Old forms that passed only the island id ---
    public static IEnumerator RunTraining(this TrainingCPTRunner trainer, string islandId)
        => Forward(trainer, islandId);

    // --- Old forms that passed (Transform/GameObject/Cards, islandId) ---
    public static IEnumerator RunTraining(this TrainingCPTRunner trainer, Transform /*rigRoot*/ _, string islandId)
        => Forward(trainer, islandId);

    public static IEnumerator RunTraining(this TrainingCPTRunner trainer, GameObject /*rigRoot*/ _, string islandId)
        => Forward(trainer, islandId);

    public static IEnumerator RunTraining(this TrainingCPTRunner trainer, Cards /*cards*/ _, string islandId)
        => Forward(trainer, islandId);

    // --- Old forms that passed (islandId, Transform/GameObject/Cards) (reversed order) ---
    public static IEnumerator RunTraining(this TrainingCPTRunner trainer, string islandId, Transform /*rigRoot*/ _)
        => Forward(trainer, islandId);

    public static IEnumerator RunTraining(this TrainingCPTRunner trainer, string islandId, GameObject /*rigRoot*/ _)
        => Forward(trainer, islandId);

    public static IEnumerator RunTraining(this TrainingCPTRunner trainer, string islandId, Cards /*cards*/ _)
        => Forward(trainer, islandId);
}
