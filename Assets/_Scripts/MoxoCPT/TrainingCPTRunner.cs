// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.InputSystem;

// public class TrainingCPTRunner : MonoBehaviour
// {
//     // Session memory so each island runs training only once per app session
//     private static readonly HashSet<string> s_trainedIslands = new();
//     private static readonly List<TrainingCPTRunner> s_all = new();

//     [Header("Wiring")]
//     [Tooltip("Green canvas for correct; keep disabled in scene.")]
//     [SerializeField] private GameObject correctCanvas;
//     [Tooltip("Red canvas for incorrect; keep disabled in scene.")]
//     [SerializeField] private GameObject incorrectCanvas;

//     [Header("Explore Hint UI (notes shown during training)")]
//     [Tooltip("Assign the ExploreHintUI in the scene (same one you use for countdowns).")]
//     [SerializeField] private ExploreHintUI hintUI;
//     [Tooltip("Message to show while a TARGET is displayed.")]
//     [SerializeField] private string targetNote = "TARGET — Press Space";
//     [Tooltip("Message to show while a NON-TARGET is displayed.")]
//     [SerializeField] private string nonTargetNote = "NON-TARGET — Don’t press Space";
//     [Tooltip("Hide the note immediately when player responds.")]
//     [SerializeField] private bool hideNoteOnPress = true;

//     [Header("Audio (optional)")]
//     [Tooltip("If null, the script will create a hidden 2D AudioSource at runtime.")]
//     [SerializeField] private AudioSource audioSource;
//     [SerializeField] private AudioClip correctClip;
//     [SerializeField] private AudioClip incorrectClip;
//     [Range(0f,1f)] [SerializeField] private float sfxVolume = 1f;

//     [Header("Timing")]
//     [Tooltip("Seconds each training card is shown; a countdown is displayed in the note.")]
//     [SerializeField] private float perCardSeconds = 3f;

//     [Header("Cards (assign your training references)")]
//     [Tooltip("Assign ONE Target card transform for training. This same target can be shown multiple times.")]
//     [SerializeField] private Transform targetCard;
//     [Tooltip("Assign FIVE (or more) NonTarget card transforms for training.")]
//     [SerializeField] private List<Transform> nonTargetCards = new List<Transform>();

//     [Header("Training Sequence")]
//     [Tooltip("How many times the target should appear during training (>=1).")]
//     [Min(1)] [SerializeField] private int totalTargetsInTraining = 2;
//     [Tooltip("How many non-targets to show during training (>=1).")]
//     [Min(1)] [SerializeField] private int totalNonTargetsInTraining = 5;
//     [Tooltip("If true, sequence interleaves targets and non-targets.")]
//     [SerializeField] private bool interleaveTargets = true;

//     // runtime
//     private Coroutine _co;
//     private bool _active;

//     // ---------- lifecycle ----------
//     private void Awake()
//     {
//         if (!s_all.Contains(this)) s_all.Add(this);
//         HideAllFeedback();
//         TurnAllTrainingCards(false);
//         EnsureAudioSource();
//     }

//     private void OnDisable()
//     {
//         StopAll();
//     }

//     private void OnDestroy()
//     {
//         s_all.Remove(this);
//     }

//     private void HideAllFeedback()
//     {
//         if (correctCanvas)   correctCanvas.SetActive(false);
//         if (incorrectCanvas) incorrectCanvas.SetActive(false);
//     }

//     private void TurnAllTrainingCards(bool on)
//     {
//         if (targetCard) targetCard.gameObject.SetActive(on);
//         foreach (var t in nonTargetCards)
//             if (t) t.gameObject.SetActive(on);
//     }

//     private void StopAll()
//     {
//         if (_co != null) StopCoroutine(_co);
//         _co = null;
//         _active = false;
//         HideAllFeedback();
//         TurnAllTrainingCards(false);
//         // make sure note is gone if we abort
//         if (hintUI) hintUI.Hide();
//     }

//     // ---------- static helpers ----------
//     public static bool HasCompletedForIsland(string islandId)
//         => string.IsNullOrWhiteSpace(islandId) ? true : s_trainedIslands.Contains(islandId.Trim().ToUpperInvariant());

//     public static void ForceStopAll()
//     {
//         foreach (var t in s_all) if (t) t.StopAll();
//     }

//     public static void MarkCompletedForIsland(string islandId)
//     {
//         if (string.IsNullOrWhiteSpace(islandId)) return;
//         s_trainedIslands.Add(islandId.Trim().ToUpperInvariant());
//     }

//     public static void ClearCompletedForIsland(string islandId)
//     {
//         if (string.IsNullOrWhiteSpace(islandId)) return;
//         s_trainedIslands.Remove(islandId.Trim().ToUpperInvariant());
//     }

//     // ---------- audio helpers ----------
//     private void EnsureAudioSource()
//     {
//         if (audioSource)
//         {
//             audioSource.spatialBlend = 0f; // 2D
//             audioSource.playOnAwake = false;
//             audioSource.loop = false;
//             audioSource.mute = false;
//             return;
//         }

//         var go = new GameObject("TrainingCPTRunner_Audio");
//         go.transform.SetParent(transform, false);
//         audioSource = go.AddComponent<AudioSource>();
//         audioSource.spatialBlend = 0f;
//         audioSource.playOnAwake = false;
//         audioSource.loop = false;
//         audioSource.mute = false;
//         audioSource.volume = 1f;
//         go.hideFlags = HideFlags.HideInHierarchy;
//     }

//     private void PlaySfx(AudioClip clip)
//     {
//         if (!clip) return;

//         if (audioSource && !audioSource.gameObject.activeInHierarchy)
//             audioSource.gameObject.SetActive(true);

//         if (audioSource && audioSource.enabled)
//         {
//             audioSource.PlayOneShot(clip, Mathf.Clamp01(sfxVolume));
//         }
//         else
//         {
//             var listener = FindObjectOfType<AudioListener>();
//             Vector3 pos = listener ? listener.transform.position : Vector3.zero;
//             AudioSource.PlayClipAtPoint(clip, pos, Mathf.Clamp01(sfxVolume));
//         }
//     }

//     // ---------- order builder ----------
//     private List<(Transform card, bool isTarget)> BuildOrder()
//     {
//         var order = new List<(Transform, bool)>();

//         if (!targetCard)
//         {
//             Debug.LogWarning("[Training] Missing Target reference.");
//             return order;
//         }
//         if (nonTargetCards == null || nonTargetCards.Count == 0)
//         {
//             Debug.LogWarning("[Training] Need at least one NonTarget reference.");
//             return order;
//         }

//         int tRemaining = Mathf.Max(1, totalTargetsInTraining);
//         int nRemaining = Mathf.Max(1, totalNonTargetsInTraining);

//         int nonIndex = 0;

//         if (!interleaveTargets)
//         {
//             for (int i = 0; i < tRemaining; i++) order.Add((targetCard, true));
//             for (int i = 0; i < nRemaining; i++)
//             {
//                 var nt = nonTargetCards[nonIndex % nonTargetCards.Count];
//                 nonIndex++;
//                 order.Add((nt, false));
//             }
//             return order;
//         }

//         int ntBetween = Mathf.Max(1, nRemaining / tRemaining);

//         while (tRemaining > 0 || nRemaining > 0)
//         {
//             if (tRemaining > 0)
//             {
//                 order.Add((targetCard, true));
//                 tRemaining--;
//             }

//             for (int i = 0; i < ntBetween && nRemaining > 0; i++)
//             {
//                 var nt = nonTargetCards[nonIndex % nonTargetCards.Count];
//                 nonIndex++;
//                 order.Add((nt, false));
//                 nRemaining--;
//             }
//         }

//         while (nRemaining > 0)
//         {
//             var nt = nonTargetCards[nonIndex % nonTargetCards.Count];
//             nonIndex++;
//             order.Add((nt, false));
//             nRemaining--;
//         }

//         return order;
//     }

//     // ---------- main API ----------
//     public IEnumerator RunTrainingForActiveIsland(string islandId = null)
//     {
//         islandId = (islandId ?? "").Trim().ToUpperInvariant();

//         if (HasCompletedForIsland(islandId))
//             yield break;

//         if (GameManager.Instance != null && GameManager.Instance.State == GameManager.GameState.CPT)
//             yield break;

//         _active = true;
//         HideAllFeedback();
//         TurnAllTrainingCards(false);
//         EnsureAudioSource();

//         if (!FindObjectOfType<AudioListener>())
//             Debug.LogWarning("[Training] No AudioListener found in scene. Sounds may be inaudible.");

//         // Stop distractors; keep real cards off during training
//         var distractors = FindObjectOfType<MoxoCPT.DistractorSystem>(true);
//         distractors?.StopSystem();

//         var cardsActive = FindObjectOfType<MoxoCPT.CardsActive>(true);
//         if (cardsActive) cardsActive.SetCardsActive(false);

//         var order = BuildOrder();
//         if (order.Count == 0)
//         {
//             Debug.LogWarning("[Training] Invalid training order. Skipping training.");
//             MarkCompletedForIsland(islandId);
//             yield break;
//         }

//         foreach (var step in order)
//         {
//             if (!_active) yield break;

//             // Show exactly this card
//             TurnAllTrainingCards(false);
//             step.card.gameObject.SetActive(true);

//             // ---- Show instructional note on your ExploreHintUI ----
//             if (hintUI)
//             {
//                 // We use empty endMessage so it auto-hides when the timer completes.
//                 var note = step.isTarget ? targetNote : nonTargetNote;
//                 hintUI.ShowExploreHint(perCardSeconds, note, "");
//             }

//             bool pressed = false;
//             float t = perCardSeconds;
//             while (t > 0f)
//             {
//                 if (!_active) yield break;

//                 var kb = Keyboard.current;
//                 var gp = Gamepad.current;
//                 if ((kb != null && kb.spaceKey.wasPressedThisFrame) ||
//                     (gp != null && gp.buttonSouth.wasPressedThisFrame))
//                 {
//                     pressed = true;
//                     if (hideNoteOnPress && hintUI) hintUI.Cancel(); // hide note early on response
//                     break;
//                 }

//                 // Abort if real game started somehow
//                 if (GameManager.Instance != null && GameManager.Instance.State == GameManager.GameState.CPT)
//                 {
//                     if (hintUI) hintUI.Hide();
//                     StopAll();
//                     yield break;
//                 }

//                 t -= Time.unscaledDeltaTime;
//                 yield return null;
//             }

//             // If time expired (or just after press), ensure note is hidden
//             if (hintUI) hintUI.Hide();

//             // Evaluate & give feedback (training only; not logged)
//             bool correct = (step.isTarget && pressed) || (!step.isTarget && !pressed);

//             if (correctCanvas)   correctCanvas.SetActive(correct);
//             if (incorrectCanvas) incorrectCanvas.SetActive(!correct);

//             PlaySfx(correct ? correctClip : incorrectClip);

//             yield return new WaitForSecondsRealtime(0.7f);
//             HideAllFeedback();
//             yield return new WaitForSecondsRealtime(0.15f);
//         }

//         TurnAllTrainingCards(false);
//         _active = false;

//         MarkCompletedForIsland(islandId);
//     }
// }
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Reflection;

public class TrainingCPTRunner : MonoBehaviour
{
    // Session memory so each island runs training only once per app session
    private static readonly HashSet<string> s_trainedIslands = new();
    private static readonly List<TrainingCPTRunner> s_all = new();

    [Header("Island binding")]
    [Tooltip("Optional. If set, this trainer only runs for this island ID (e.g., POISON, SKULL). If empty, we try to auto-detect from a parent IslandMoxoGroup.")]
    [SerializeField] private string islandIdTag = "";

    [Header("Wiring")]
    [Tooltip("Green canvas for correct; keep disabled in scene.")]
    [SerializeField] private GameObject correctCanvas;
    [Tooltip("Red canvas for incorrect; keep disabled in scene.")]
    [SerializeField] private GameObject incorrectCanvas;

    [Header("Explore Hint UI (notes shown during training)")]
    [Tooltip("Assign the ExploreHintUI in the scene (same one you use for countdowns).")]
    [SerializeField] private ExploreHintUI hintUI;
    [Tooltip("Message to show while a TARGET is displayed.")]
    [SerializeField] private string targetNote = "TARGET — Press Space";
    [Tooltip("Message to show while a NON-TARGET is displayed.")]
    [SerializeField] private string nonTargetNote = "NON-TARGET — Don’t press Space";

    // ===================== NEW: INSTRUCTION CARD + NOTE =====================
    [Header("Instruction (NEW)")]
    [Tooltip("Optional: an instruction card shown during training (in addition to target/non-target).")]
    [SerializeField] private Transform instructionCard;

    [Tooltip("Message to show while the INSTRUCTION card is displayed.")]
    [SerializeField] private string instructionNote = "Instructions — press Space for TARGET only.";

    [Tooltip("Seconds the instruction card is shown (can be different from perCardSeconds).")]
    [SerializeField] private float instructionSeconds = 4f;

    [Tooltip("If true, instruction card is shown once at the start of training.")]
    [SerializeField] private bool showInstructionFirst = true;

    [Tooltip("If true, Space/A will skip the instruction card early.")]
    [SerializeField] private bool allowSkipInstructionOnPress = true;
    // =======================================================================

    [Header("Training Flow")]
    [Tooltip("If true, the card + hint + feedback remain visible until the countdown ends, then advance automatically.")]
    [SerializeField] private bool lockCardUntilCountdownEnds = true; // (kept for inspector compatibility)

    [Header("Feedback Display")]
    [Tooltip("How long to keep Correct/Incorrect visible before moving to the next card.")]
    [SerializeField] private float feedbackHoldSeconds = 0.7f;

    [Header("Audio (optional)")]
    [Tooltip("If null, the script will create a hidden 2D AudioSource at runtime.")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip correctClip;
    [SerializeField] private AudioClip incorrectClip;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

    [Header("Timing")]
    [Tooltip("Seconds each training card is shown; a countdown is displayed in the note.")]
    [SerializeField] private float perCardSeconds = 3f;

    [Header("Cards (assign your training references)")]
    [Tooltip("Assign ONE Target card transform for training. This same target can be shown multiple times.")]
    [SerializeField] private Transform targetCard;
    [Tooltip("Assign FIVE (or more) NonTarget card transforms for training.")]
    [SerializeField] private List<Transform> nonTargetCards = new List<Transform>();

    [Header("Training Sequence")]
    [Tooltip("How many times the target should appear during training (>=1).")]
    [Min(1)] [SerializeField] private int totalTargetsInTraining = 2;
    [Tooltip("How many non-targets to show during training (>=1).")]
    [Min(1)] [SerializeField] private int totalNonTargetsInTraining = 5;
    [Tooltip("If true, sequence interleaves targets and non-targets.")]
    [SerializeField] private bool interleaveTargets = true;

    [Header("Parent/Root Safety")]
    [Tooltip("Optional: a root that exclusively contains *training* cards. If set, we keep this ON even when real CardsActive is turned OFF.")]
    [SerializeField] private Transform trainingRoot;

    [Header("Debug")]
    [SerializeField] private bool logVerbose = true;

    // runtime
    private Coroutine _co;
    private bool _active;

    private static string Norm(string s) => string.IsNullOrWhiteSpace(s) ? "" : s.Trim().ToUpperInvariant();

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(islandIdTag))
        {
            var group = GetComponentInParent<IslandMoxoGroup>(true);
            if (group && !string.IsNullOrWhiteSpace(group.islandId))
                islandIdTag = Norm(group.islandId);
        }
        else islandIdTag = Norm(islandIdTag);
    }

    public bool BelongsToIsland(string islandId)
    {
        var tag = Norm(islandIdTag);
        if (!string.IsNullOrEmpty(tag)) return tag == Norm(islandId);

        var group = GetComponentInParent<IslandMoxoGroup>(true);
        return group && !string.IsNullOrWhiteSpace(group.islandId) && Norm(group.islandId) == Norm(islandId);
    }

    private void Awake()
    {
        if (!s_all.Contains(this)) s_all.Add(this);
        AutoBindCardsIfNeeded();
        HideAllFeedback();
        TurnAllTrainingCards(false);
        EnsureAudioSource();
    }

    private void OnDisable() => StopAll();
    private void OnDestroy() => s_all.Remove(this);

    private void HideAllFeedback()
    {
        if (correctCanvas) correctCanvas.SetActive(false);
        if (incorrectCanvas) incorrectCanvas.SetActive(false);
    }

    private void TurnAllTrainingCards(bool on)
    {
        if (targetCard) targetCard.gameObject.SetActive(on);
        foreach (var t in nonTargetCards)
            if (t) t.gameObject.SetActive(on);

        // NEW: instruction card participates in the “turn off everything” behavior
        if (instructionCard) instructionCard.gameObject.SetActive(on);
    }

    private void StopAll()
    {
        if (_co != null) StopCoroutine(_co);
        _co = null;
        _active = false;
        HideAllFeedback();
        TurnAllTrainingCards(false);
        if (hintUI) hintUI.Hide();
    }

    public static bool HasCompletedForIsland(string islandId)
        => string.IsNullOrWhiteSpace(islandId) ? true : s_trainedIslands.Contains(Norm(islandId));

    public static void ForceStopAll()
    {
        foreach (var t in s_all) if (t) t.StopAll();
    }

    public static void MarkCompletedForIsland(string islandId)
    {
        if (string.IsNullOrWhiteSpace(islandId)) return;
        s_trainedIslands.Add(Norm(islandId));
    }

    public static void ClearCompletedForIsland(string islandId)
    {
        if (string.IsNullOrWhiteSpace(islandId)) return;
        s_trainedIslands.Remove(Norm(islandId));
    }

    private void EnsureAudioSource()
    {
        if (audioSource)
        {
            audioSource.spatialBlend = 0f;
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.mute = false;
            return;
        }

        var go = new GameObject("TrainingCPTRunner_Audio");
        go.transform.SetParent(transform, false);
        audioSource = go.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.mute = false;
        audioSource.volume = 1f;
        go.hideFlags = HideFlags.HideInHierarchy;
    }

    private void PlaySfx(AudioClip clip)
    {
        if (!clip) return;

        if (audioSource && !audioSource.gameObject.activeInHierarchy)
            audioSource.gameObject.SetActive(true);

        if (audioSource && audioSource.enabled)
        {
            audioSource.PlayOneShot(clip, Mathf.Clamp01(sfxVolume));
        }
        else
        {
            var listener = FindObjectOfType<AudioListener>();
            Vector3 pos = listener ? listener.transform.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(clip, pos, Mathf.Clamp01(sfxVolume));
        }
    }

    private void AutoBindCardsIfNeeded()
    {
        if (!targetCard)
        {
            var tagged = GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t && t.CompareTag("Target"));
            if (tagged) targetCard = tagged;
            else
            {
                targetCard = GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t && t.name.ToLowerInvariant().Contains("target") && !t.name.ToLowerInvariant().Contains("non"));
            }
        }

        if (nonTargetCards == null || nonTargetCards.Count == 0 || nonTargetCards.All(t => t == null))
        {
            nonTargetCards = GetComponentsInChildren<Transform>(true)
                .Where(t => t && (t.CompareTag("NonTarget") || t.name.ToLowerInvariant().Contains("nontarget") || t.name.ToLowerInvariant().Contains("non_target")))
                .Distinct()
                .ToList();
        }

        if (logVerbose)
        {
            Debug.Log($"[Training] AutoBind → Target={(targetCard ? targetCard.name : "NULL")}, NonTargets={nonTargetCards.Count}, Instruction={(instructionCard ? instructionCard.name : "NULL")} (islandTag='{Norm(islandIdTag)}')");
        }
    }

    private static void ActivateWithParents(GameObject go)
    {
        if (!go) return;
        var stack = new Stack<Transform>();
        for (var p = go.transform; p != null; p = p.parent) stack.Push(p);
        while (stack.Count > 0)
        {
            var tr = stack.Pop();
            if (!tr.gameObject.activeSelf) tr.gameObject.SetActive(true);
        }
    }

    // ===================== CHANGED: order supports Instruction/Target/NonTarget =====================
    // kind: 0=Instruction, 1=Target, 2=NonTarget
    private List<(Transform card, int kind)> BuildOrder()
    {
        var order = new List<(Transform, int)>();

        if (showInstructionFirst && instructionCard != null)
            order.Add((instructionCard, 0));

        if (!targetCard)
        {
            Debug.LogWarning("[Training] Missing Target reference.");
            return order;
        }
        if (nonTargetCards == null || nonTargetCards.Count == 0 || nonTargetCards.All(t => t == null))
        {
            Debug.LogWarning("[Training] Need at least one NonTarget reference.");
            return order;
        }

        int tRemaining = Mathf.Max(1, totalTargetsInTraining);
        int nRemaining = Mathf.Max(1, totalNonTargetsInTraining);

        int nonIndex = 0;

        if (!interleaveTargets)
        {
            for (int i = 0; i < tRemaining; i++) order.Add((targetCard, 1));
            for (int i = 0; i < nRemaining; i++)
            {
                var nt = nonTargetCards[nonIndex % nonTargetCards.Count];
                nonIndex++;
                order.Add((nt, 2));
            }
            return order;
        }

        int ntBetween = Mathf.Max(1, nRemaining / tRemaining);

        while (tRemaining > 0 || nRemaining > 0)
        {
            if (tRemaining > 0)
            {
                order.Add((targetCard, 1));
                tRemaining--;
            }

            for (int i = 0; i < ntBetween && nRemaining > 0; i++)
            {
                var nt = nonTargetCards[nonIndex % nonTargetCards.Count];
                nonIndex++;
                order.Add((nt, 2));
                nRemaining--;
            }
        }

        while (nRemaining > 0)
        {
            var nt = nonTargetCards[nonIndex % nonTargetCards.Count];
            nonIndex++;
            order.Add((nt, 2));
            nRemaining--;
        }

        return order;
    }
    // =================================================================================================

    public IEnumerator RunTrainingForActiveIsland(string islandId = null)
    {
        islandId = (islandId ?? "").Trim().ToUpperInvariant();
        yield return null;

        if (GameManager.Instance != null && GameManager.Instance.State == GameManager.GameState.CPT)
        {
            Debug.LogWarning($"[Training] State was CPT at training start for '{islandId}'. Forcing PrepareCPT so training can run.");
            GameManager.Instance.UpdateGameState(GameManager.GameState.PrepareCPT);
            yield return null;
        }

        if (HasCompletedForIsland(islandId))
        {
            Debug.Log($"[Training] Already completed for '{islandId}'. Skipping.");
            yield break;
        }

        _active = true;
        AutoBindCardsIfNeeded();
        HideAllFeedback();
        TurnAllTrainingCards(false);
        EnsureAudioSource();

        var distractors = FindObjectOfType<MoxoCPT.DistractorSystem>(true);
        distractors?.StopSystem();

        var cardsActive = FindObjectOfType<MoxoCPT.CardsActive>(true);
        if (cardsActive) cardsActive.SetCardsActive(false);

        if (trainingRoot) ActivateWithParents(trainingRoot.gameObject);

        var order = BuildOrder();
        if (order.Count == 0)
        {
            Debug.LogWarning($"[Training] INVALID ORDER for '{islandId}'. Marking as completed to avoid loops.");
            MarkCompletedForIsland(islandId);
            yield break;
        }

        if (perCardSeconds < 0.25f) perCardSeconds = 0.5f;
        if (instructionSeconds < 0.25f) instructionSeconds = 0.5f;

        foreach (var step in order)
        {
            if (!_active) yield break;

            TurnAllTrainingCards(false);
            HideAllFeedback();

            if (!step.card) continue;

            ActivateWithParents(step.card.gameObject);
            step.card.gameObject.SetActive(true);

            bool isInstruction = (step.kind == 0);
            bool isTarget = (step.kind == 1);
            bool isNonTarget = (step.kind == 2);

            float seconds = isInstruction ? instructionSeconds : perCardSeconds;
            string note = isInstruction ? instructionNote : (isTarget ? targetNote : nonTargetNote);

            if (hintUI)
                hintUI.ShowExploreHint(seconds, note, "");

            bool pressed = false;
            bool feedbackShown = false;
            float t = seconds;

            while (t > 0f)
            {
                if (!_active) yield break;

                if (GameManager.Instance != null && GameManager.Instance.State == GameManager.GameState.CPT)
                {
                    Debug.LogWarning("[Training] Aborted because state switched to CPT mid-sequence.");
                    if (hintUI) hintUI.Hide();
                    StopAll();
                    yield break;
                }

                var kb = Keyboard.current;
                var gp = Gamepad.current;
                bool confirm = (kb != null && kb.spaceKey.wasPressedThisFrame) ||
                               (gp != null && gp.buttonSouth.wasPressedThisFrame);

                if (confirm)
                {
                    pressed = true;

                    // NEW: instruction can be skipped early (optional)
                    if (isInstruction && allowSkipInstructionOnPress)
                        break;
                }

                // Target/NonTarget feedback on press
                if (!isInstruction && !feedbackShown && pressed)
                {
                    bool correct = isTarget; // target+press => correct, non-target+press => incorrect
                    if (correctCanvas) correctCanvas.SetActive(correct);
                    if (incorrectCanvas) incorrectCanvas.SetActive(!correct);
                    PlaySfx(correct ? correctClip : incorrectClip);
                    feedbackShown = true;
                }

                t -= Time.unscaledDeltaTime;
                yield return null;
            }

            // NEW: instruction has no correctness feedback; just proceed
            if (isInstruction)
            {
                if (hintUI) hintUI.Hide();
                step.card.gameObject.SetActive(false);
                continue;
            }

            // If no press, decide correctness now (non-target + no press should show CORRECT)
            if (!feedbackShown)
            {
                bool correct = isNonTarget; // non-target+no press => correct, target+no press => incorrect
                if (correctCanvas) correctCanvas.SetActive(correct);
                if (incorrectCanvas) incorrectCanvas.SetActive(!correct);
                PlaySfx(correct ? correctClip : incorrectClip);
                feedbackShown = true;
            }

            if (feedbackHoldSeconds > 0f)
                yield return new WaitForSecondsRealtime(feedbackHoldSeconds);

            if (hintUI) hintUI.Hide();
            HideAllFeedback();
            step.card.gameObject.SetActive(false);
        }

        TurnAllTrainingCards(false);
        _active = false;

        MarkCompletedForIsland(islandId);
        Debug.Log($"[Training] Completed for '{islandId}'.");
    }

    public static void ClearAllTrainingProgress()
    {
        ClearCompletedForIsland("");
        var f = typeof(TrainingCPTRunner).GetField("s_trainedIslands", BindingFlags.Static | BindingFlags.NonPublic);
        if (f != null) f.SetValue(null, new System.Collections.Generic.HashSet<string>());
        Debug.Log("[Training] Cleared ALL training flags for this session.");
    }
}
