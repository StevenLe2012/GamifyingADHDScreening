// // // IslandData.cs
// // using UnityEngine;

// // [CreateAssetMenu(menuName = "Moxo/Island Data")]
// // public class IslandData : ScriptableObject
// // {
// //     [Header("Identity")]
// //     public string islandId;                 // e.g. MAIN, CARDS, BREAD, SKULL, POISON, DRAGON
// //     public string displayName;
// //     public Sprite icon;

// //     [Header("Intro / Flow")]
// //     [TextArea] public string introTitle = "Welcome";
// //     [TextArea] public string introBody  = "Get ready!";
// //     [Min(0f)]  public float countdownSeconds = 5f;
// //     public bool startsMoxoOnArrival = true;

// //     [Header("MOXO Overrides (optional)")]
// //     [Tooltip("If > 0, overrides ChangeShapes.totalTrials for this island.")]
// //     public int totalTrialsOverride = 0;

// //     [Tooltip("If > 0, overrides ChangeShapes.totalTargets for this island.")]
// //     public int totalTargetsOverride = 0;

// //     [Tooltip("If > 0, overrides ChangeShapes.totalNonTargets for this island.")]
// //     public int totalNonTargetsOverride = 0;

// //     [Tooltip("If not empty, overrides Cards.cardDuration for this island.")]
// //     public float[] cardDurationsOverride;

// // #if UNITY_EDITOR
// //     private void OnValidate()
// //     {
// //         if (!string.IsNullOrWhiteSpace(islandId))
// //             islandId = islandId.Trim().ToUpperInvariant();

// //         // light sanity: if both target/non-target overrides are set, keep them coherent
// //         if (totalTargetsOverride < 0) totalTargetsOverride = 0;
// //         if (totalNonTargetsOverride < 0) totalNonTargetsOverride = 0;
// //         if (totalTrialsOverride < 0) totalTrialsOverride = 0;

// //         // (no hard enforcement; ChangeShapes already checks sums)
// //     }
// // #endif
// // }


// using UnityEngine;

// [CreateAssetMenu(menuName = "Moxo/Island Data")]
// public class IslandData : ScriptableObject
// {
//     [Header("Identity")]
//     public string islandId;                 // e.g. MAIN, CARDS, BREAD, SKULL, POISON, DRAGON
//     public string displayName;
//     public Sprite icon;

//     [Header("Intro / Flow (arrival)")]
//     [TextArea] public string introTitle = "Welcome";
//     [TextArea] public string introBody  = "Get ready!";
//     [Min(0f)]  public float countdownSeconds = 5f;
//     public bool startsMoxoOnArrival = true;

//     [Header("Training Intro (before training)")]
//     [TextArea] public string trainingIntroTitle = "";   // optional override; falls back to introTitle if empty
//     [TextArea] public string trainingIntroBody  = "";   // optional override; falls back to introBody  if empty

//     [Header("Ready Intro (after training, before real game)")]
//     [TextArea] public string readyIntroTitle = "Ready to start?";
//     [TextArea] public string readyIntroBody  = "Press Space to begin the real test.";

//     [Header("MOXO Overrides (optional)")]
//     [Tooltip("If > 0, overrides ChangeShapes.totalTrials for this island.")]
//     public int totalTrialsOverride = 0;

//     [Tooltip("If > 0, overrides ChangeShapes.totalTargets for this island.")]
//     public int totalTargetsOverride = 0;

//     [Tooltip("If > 0, overrides ChangeShapes.totalNonTargets for this island.")]
//     public int totalNonTargetsOverride = 0;

//     [Tooltip("If not empty, overrides Cards.cardDuration for this island.")]
//     public float[] cardDurationsOverride;

// #if UNITY_EDITOR
//     private void OnValidate()
//     {
//         if (!string.IsNullOrWhiteSpace(islandId))
//             islandId = islandId.Trim().ToUpperInvariant();

//         if (totalTargetsOverride < 0) totalTargetsOverride = 0;
//         if (totalNonTargetsOverride < 0) totalNonTargetsOverride = 0;
//         if (totalTrialsOverride < 0) totalTrialsOverride = 0;
//     }
// #endif
// }
using UnityEngine;

[CreateAssetMenu(menuName = "Moxo/Island Data")]
public class IslandData : ScriptableObject
{
    [Header("Identity")]
    public string islandId;                 // e.g. MAIN, CARDS, BREAD, SKULL, POISON, DRAGON
    public string displayName;
    public Sprite icon;

    [Header("Intro / Flow (arrival)")]
    [TextArea] public string introTitle = "Welcome";
    [TextArea] public string introBody  = "Get ready!";
    [Min(0f)]  public float countdownSeconds = 5f;

    // If true → MOXO flow (training + ready intro) is used.
    // If false → non-MOXO flow; see dialogue fields below.
    public bool hasMoxoGame = true;

    [Header("Non-MOXO Dialogue (used when hasMoxoGame = false)")]
    public bool startsDialogueOnArrival = true;          // show intro, then start dialogue handler
    public string dialogueHandlerType = "DragonHandler"; // class name to find in scene (under island rig)

    [Header("Training Intro (before training)")]
    [TextArea] public string trainingIntroTitle = "";   // optional override; falls back to introTitle if empty
    [TextArea] public string trainingIntroBody  = "";   // optional override; falls back to introBody  if empty

    [Header("Ready Intro (after training, before real game)")]
    [TextArea] public string readyIntroTitle = "Ready to start?";
    [TextArea] public string readyIntroBody  = "Press Space to begin the real test.";

    [Header("Intro Voice Over (optional)")]
    [Tooltip("Voice over clip for NON-MOXO intro when arriving on this island.")]
    public AudioClip nonMoxoIntroVoice;

    [Tooltip("Voice over clip for MOXO pre-training intro on this island.")]
    public AudioClip trainingIntroVoice;

    [Tooltip("Voice over clip for MOXO 'ready to start' intro on this island.")]
    public AudioClip readyIntroVoice;

    // =========================
    // CONSISTENCY CONTROL
    // =========================
    [Header("CPT Consistency")]
    [Tooltip("If enabled, this island will NOT override trial counts or durations. Use this for research consistency.")]
    public bool lockCptToGlobalDefaults = true;

    [Header("MOXO Overrides (optional)")]
    [Tooltip("If > 0, overrides ChangeShapes.totalTrials for this island (ignored if lockCptToGlobalDefaults = true).")]
    public int totalTrialsOverride = 0;

    [Tooltip("If > 0, overrides ChangeShapes.totalTargets for this island (ignored if lockCptToGlobalDefaults = true).")]
    public int totalTargetsOverride = 0;

    [Tooltip("If > 0, overrides ChangeShapes.totalNonTargets for this island (ignored if lockCptToGlobalDefaults = true).")]
    public int totalNonTargetsOverride = 0;

    [Tooltip("If not empty, overrides Cards.cardDuration for this island (ignored if lockCptToGlobalDefaults = true).")]
    public float[] cardDurationsOverride;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!string.IsNullOrWhiteSpace(islandId))
            islandId = islandId.Trim().ToUpperInvariant();

        if (totalTargetsOverride < 0) totalTargetsOverride = 0;
        if (totalNonTargetsOverride < 0) totalNonTargetsOverride = 0;
        if (totalTrialsOverride < 0) totalTrialsOverride = 0;

        // Optional: if you lock CPT, you can also zero overrides to avoid confusion
        // (comment out if you still want to keep values around for later)
        // if (lockCptToGlobalDefaults)
        // {
        //     totalTrialsOverride = 0;
        //     totalTargetsOverride = 0;
        //     totalNonTargetsOverride = 0;
        //     cardDurationsOverride = null;
        // }
    }
#endif
}
