// IntroAnchor.cs
using UnityEngine;

public class IntroAnchor : MonoBehaviour
{
    [Tooltip("Must match IslandData.islandId (e.g., CARDS, BREAD, SKULL, POISON).")]
    public string islandId;

    [Tooltip("The local character for this island. Shown while the intro screen is visible; hidden when it closes.")]
    public GameObject localCharacter;

    [Tooltip("Optional local stimuli shown next to the local character, INTRO ONLY (not shown during the " +
             "reward or results screens). Appears stimuliDelaySeconds after the character does, and is " +
             "hidden as soon as the intro closes. Disable it in the scene; the system enables/disables it.")]
    public GameObject localStimuli;

    [Tooltip("Delay (seconds) after the local character appears before the local stimuli appears. Ignored if localStimuli is not assigned.")]
    public float stimuliDelaySeconds = 5f;

    [Header("Reward (shown after MOXO ends, before results — skipped on redo)")]
    [Tooltip("GameObject enabled briefly after a successful MOXO game. Disable it in the scene; the system enables/disables it automatically.")]
    public GameObject rewardObject;

    [Tooltip("Sound played when the reward appears. Uses the reward object's own AudioSource if it has one; otherwise plays at the anchor position.")]
    public AudioClip rewardSound;

    [Tooltip("How long the reward is shown (seconds). 0 = use the clip length; if no clip, defaults to 2s.")]
    public float rewardDuration = 0f;

    [Header("Results Screen")]
    [Tooltip("Voice over clip played when the results screen appears on this island. Overrides the shared clip on IntroScreen.")]
    public AudioClip resultsVoice;

    [Tooltip("Title shown on the results screen for this island. Leave empty to use the shared default.")]
    public string resultsTitle;

    [Tooltip("Body template for this island's results. Use {HIT}, {TOTAL}, {FA}, {D_TOTAL} tokens. Leave empty to use the shared default.")]
    [TextArea]
    public string resultsBodyTemplate;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!string.IsNullOrWhiteSpace(islandId))
            islandId = islandId.Trim().ToUpperInvariant();
    }
#endif
}
