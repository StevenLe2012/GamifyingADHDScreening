// // using System.Collections;
// // using System.Collections.Generic;
// // using System.Linq;
// // using UnityEngine;
// // using UnityEngine.UI; // Canvas
// // using Random = UnityEngine.Random;

// // namespace MoxoCPT
// // {
// //     public class ChangeShapes : MonoBehaviour
// //     {
// //         [Header("Pre-start")]
// //         [SerializeField] private float _secondsTillGameStarts = 5.0f;

// //         [Header("Countdown UI")]
// //         [SerializeField] private ExploreHintUI countdownUI;
// //         [SerializeField] private string countdownStartText = "Get ready…";
// //         [SerializeField] private string countdownEndText = "Go!";

// //         [Header("Countdown UI Placement")]
// //         [SerializeField] private int countdownSortingOrder = 90;
// //         [SerializeField] private bool countdownFacePlayer = false;

// //         [Header("UI Timing")]
// //         [SerializeField] private float postCountdownDelay = 0.35f;

// //         // keep "Go!" on-screen briefly, then fully clear UI before cards start
// //         [SerializeField] private float goHoldSeconds = 1.0f;
// //         [SerializeField] private float uiClearBuffer = 0.05f;

// //         [Header("Exact counts")]
// //         [SerializeField] private int totalTrials = 59;
// //         [SerializeField] private int totalTargets = 36;
// //         [SerializeField] private int totalNonTargets = 23;

// //         [Header("Distractors")]
// //         [SerializeField] private DistractorSystem distractors;

// //         // bind to this rig’s Cards (no singleton required for schedule)
// //         [SerializeField] private Cards cards;

// //         private List<Transform> _schedule;
// //         private bool _hasStarted;
// //         private Coroutine _runner;

// //         // -------- lifecycle guards (important between islands) --------
// //         private void OnEnable()
// //         {
// //             _hasStarted = false;
// //             if (_runner != null) { StopCoroutine(_runner); _runner = null; }
// //             StopAllCoroutines();
// //             HideCountdownUI(immediate: true);
// //             BindCards();
// //         }

// //         private void OnDisable()
// //         {
// //             if (_runner != null) StopCoroutine(_runner);
// //             _runner = null;
// //             StopAllCoroutines();
// //             HideCountdownUI(immediate: true);
// //         }

// //         private void Start() { /* wait for CPT state in Update */ }

// //         private void Update()
// //         {
// //             if (_hasStarted) return;

// //             if (GameManager.Instance != null && GameManager.Instance.State == GameManager.GameState.CPT)
// //                 ForceStart();
// //         }

// //         // --- Cards binding (scoped to this rig) ---
// //         private void BindCards()
// //         {
// //             if (cards && cards.gameObject) return;

// //             cards = GetComponentInParent<Cards>(true);
// //             if (!cards) cards = GetComponentsInChildren<Cards>(true).FirstOrDefault();

// //             if (!cards)
// //             {
// //                 var myRig = GetRigRoot(transform);
// //                 var all = FindObjectsOfType<Cards>(true);
// //                 cards = all.FirstOrDefault(c => c && IsUnder(c.transform, myRig));
// //             }

// //             Debug.Log($"[ChangeShapes] Bound Cards => {(cards ? cards.gameObject.name : "NULL")}");
// //         }

// //         private static Transform GetRigRoot(Transform t)
// //         {
// //             Transform best = t;
// //             while (t != null)
// //             {
// //                 if (t.name.StartsWith("MoxoCPT_", System.StringComparison.OrdinalIgnoreCase))
// //                     best = t;
// //                 t = t.parent;
// //             }
// //             return best;
// //         }

// //         private static bool IsUnder(Transform child, Transform root)
// //         {
// //             if (!child || !root) return false;
// //             for (var p = child; p != null; p = p.parent)
// //                 if (p == root) return true;
// //             return false;
// //         }

// //         /// <summary>
// //         /// Public nudge so IslandTravelManager (or anyone) can guarantee a start.
// //         /// Idempotent: runs only once per enable.
// //         /// </summary>
// //         public void ForceStart()
// //         {
// //             if (_hasStarted) return;
// //             _hasStarted = true;

// //             if (_runner != null) StopCoroutine(_runner);
// //             _runner = StartCoroutine(CoStartAfterIntro());
// //         }

// //         // Wait for IntroScreen to fully dismiss, then run countdown + trials
// //         private IEnumerator CoStartAfterIntro()
// //         {
// //             BindCards();
// //             if (!cards)
// //             {
// //                 Debug.LogError("[ChangeShapes] No Cards bound for this rig.");
// //                 yield break;
// //             }

// //             // adopt island overrides
// //             var island = IslandTravelManager.I ? IslandTravelManager.I.CurrentIsland : null;
// //             if (island != null)
// //             {
// //                 if (island.countdownSeconds > 0f) _secondsTillGameStarts = island.countdownSeconds;
// //                 if (island.totalTrialsOverride > 0) totalTrials = island.totalTrialsOverride;
// //                 if (island.totalTargetsOverride > 0) totalTargets = island.totalTargetsOverride;
// //                 if (island.totalNonTargetsOverride > 0) totalNonTargets = island.totalNonTargetsOverride;
// //                 if (island.cardDurationsOverride != null && island.cardDurationsOverride.Length > 0)
// //                     cards.cardDuration = island.cardDurationsOverride;
// //             }

// //             // wait for IntroScreen to be gone (realtime)
// //             var intro = IntroScreen.Instance ?? FindObjectOfType<IntroScreen>(true);
// //             if (intro != null)
// //             {
// //                 float timeout = 30f;
// //                 while (timeout > 0f)
// //                 {
// //                     bool hidden = !intro.gameObject.activeInHierarchy;
// //                     if (!hidden)
// //                     {
// //                         var cg = intro.GetComponent<CanvasGroup>();
// //                         hidden = cg && cg.alpha <= 0.001f;
// //                     }
// //                     if (hidden) break;

// //                     timeout -= Time.unscaledDeltaTime;
// //                     yield return null;
// //                 }
// //             }

// //             // locate/place countdown UI (but keep hidden until we start it)
// //             if (countdownUI == null)
// //             {
// //                 countdownUI = FindObjectOfType<ExploreHintUI>(true);
// //                 Debug.Log($"[ChangeShapes] Auto-found countdownUI = {(countdownUI ? countdownUI.name : "NULL")}");
// //             }
// //             ApplyCountdownAnchorAndPlace(island);
// //             HideCountdownUI(immediate: true);

// //             // validate targets
// //             if (totalTargets + totalNonTargets != totalTrials)
// //             {
// //                 Debug.LogError($"[ChangeShapes] totals mismatch: targets({totalTargets}) + nonTargets({totalNonTargets}) != trials({totalTrials})");
// //                 yield break;
// //             }

// //             // wait for cards (LOCAL)
// //             yield return new WaitUntil(() =>
// //                 cards != null &&
// //                 cards.cardArr != null &&
// //                 cards.cardArr.Length > 0
// //             );

// //             if (!BuildSchedule()) yield break;

// //             // Tell the score tracker the planned totals (fixes weird counts)
// //             var tracker = CPTScoreRuntime.I;
// //             if (tracker != null)
// //             {
// //                 tracker.SetTotalTargets(totalTargets);
// //                 tracker.SetTotalDistractors(totalNonTargets);
// //             }

// //             yield return StartCoroutine(CoRunCountdownAndTrials());
// //         }

// //         // ----------------- main countdown + trials -----------------
// //         private IEnumerator CoRunCountdownAndTrials()
// //         {
// //             KoalaAnimBus.BroadcastToActiveKoalas(d => d.OnCountdownStart());

// //             // SHOW countdown now (Intro is gone)
// //             if (countdownUI != null)
// //             {
// //                 if (!countdownUI.gameObject.activeSelf) countdownUI.gameObject.SetActive(true);
// //                 var cg = countdownUI.GetComponent<CanvasGroup>();
// //                 if (cg) { cg.alpha = 1f; cg.blocksRaycasts = true; cg.interactable = true; }

// //                 Debug.Log($"[ChangeShapes] Showing countdown ({_secondsTillGameStarts:0.##}s)");
// //                 countdownUI.ShowExploreHint(_secondsTillGameStarts, countdownStartText, countdownEndText);
// //             }

// //             float t = _secondsTillGameStarts;
// //             while (t > 0f)
// //             {
// //                 t -= Time.unscaledDeltaTime;
// //                 yield return null;
// //             }

// //             if (goHoldSeconds > 0f)
// //                 yield return new WaitForSecondsRealtime(goHoldSeconds);

// //             HideCountdownUI(immediate: true);

// //             if (uiClearBuffer > 0f)
// //                 yield return new WaitForSecondsRealtime(uiClearBuffer);

// //             if (postCountdownDelay > 0f)
// //                 yield return new WaitForSecondsRealtime(postCountdownDelay);

// //             Debug.Log("[ChangeShapes] Starting trials.");
// //             distractors?.StartSystem();

// //             var logger = GetLogger();

// //             for (int trial = 0; trial < totalTrials; trial++)
// //             {
// //                 float dur = GetCardDuration(); // stimulus ON duration
// //                 float isi = dur;               // stimulus OFF duration (your design)

// //                 // Prevent previous-trial input from leaking into this trial
// //                 Interact._buttonPressed = false;

// //                 var card = _schedule[trial];
// //                 if (!card)
// //                 {
// //                     Debug.LogWarning($"[ChangeShapes] Schedule card is null at trial {trial}. Skipping.");
// //                     continue;
// //                 }

// //                 // SOURCE OF TRUTH: decide target/non-target from the scheduled card
// //                 bool isTargetTrial = card.CompareTag("Target");

// //                 // Show card FIRST (stimulus becomes visible now)
// //                 TurnCardOn(card);
// //                 cards.UpdateCurCard(card);

// //                 // Timestamp after visible (ms since app start)
// //                 long onsetMs = (long)(Time.realtimeSinceStartup * 1000.0f);

// //                 // Create a FRESH report per trial (CRITICAL FIX)
// //                 var report = new Report();
// //                 report.ResetReport();
// //                 report.StimulusOnsetMs = onsetMs;
// //                 report.StimulusType = isTargetTrial ? "target" : "non_target";

// //                 // Eye logger
// //                 if (logger != null && logger.IsLogging)
// //                     logger.MarkNewTrial(trial);

// //                 // Collect responses for full trial window (ON + ISI)
// //                 // NOTE: This assumes your Interact.StartReport signature includes isTargetTrial.
// //                 StartCoroutine(Interact.StartReport(report, dur + isi, trial, dur, onsetMs, isTargetTrial));

// //                 // Stimulus ON
// //                 yield return new WaitForSeconds(dur);
// //                 TurnCardOff(cards.curCard);

// //                 // Stimulus OFF (ISI)
// //                 yield return new WaitForSeconds(isi);
// //             }

// //             distractors?.StopSystem();

// //             if (cards != null && cards.curCard != null)
// //                 TurnCardOff(cards.curCard);

// //             MoxoCPTManager.Instance?.OnGameEnd();
// //         }

// //         // ----------------- helpers -----------------
// //         private bool BuildSchedule()
// //         {
// //             var all = cards.cardArr;
// //             if (all == null || all.Length == 0)
// //             {
// //                 Debug.LogError("[ChangeShapes] Cards array empty.");
// //                 return false;
// //             }

// //             var targets = all.Where(t => t.CompareTag("Target")).ToArray();
// //             var nontargets = all.Where(t => t.CompareTag("NonTarget")).ToArray();
// //             if (targets.Length == 0 || nontargets.Length == 0)
// //             {
// //                 Debug.LogError("[ChangeShapes] Need at least one Target and one NonTarget.");
// //                 return false;
// //             }

// //             _schedule = new List<Transform>(totalTrials);
// //             for (int i = 0; i < totalTargets; i++) _schedule.Add(targets[Random.Range(0, targets.Length)]);
// //             for (int i = 0; i < totalNonTargets; i++) _schedule.Add(nontargets[Random.Range(0, nontargets.Length)]);

// //             // Fisher–Yates
// //             for (int i = 0; i < _schedule.Count; i++)
// //             {
// //                 int j = Random.Range(i, _schedule.Count);
// //                 (_schedule[i], _schedule[j]) = (_schedule[j], _schedule[i]);
// //             }
// //             return true;
// //         }

// //         private float GetCardDuration()
// //             => cards.cardDuration[Random.Range(0, cards.numDurations)];

// //         private void TurnCardOn(Transform card)
// //         {
// //             if (!card) return;
// //             card.gameObject.SetActive(true);
// //             cards.isActive = true;
// //         }

// //         private void TurnCardOff(Transform card)
// //         {
// //             if (!card) return;
// //             card.gameObject.SetActive(false);
// //             cards.isActive = false;
// //         }

// //         // ---------- placement ----------
// //         private void ApplyCountdownAnchorAndPlace(IslandData island)
// //         {
// //             if (!countdownUI) return;

// //             CountdownAnchor anchor = null;
// //             var islandId = island != null ? island.islandId?.Trim().ToUpperInvariant() : null;
// //             if (!string.IsNullOrEmpty(islandId))
// //             {
// //                 anchor = FindObjectsOfType<CountdownAnchor>(true)
// //                     .FirstOrDefault(a => a.islandId == islandId);

// //                 if (anchor && anchor.overrideCountdownSeconds > 0f)
// //                     _secondsTillGameStarts = anchor.overrideCountdownSeconds;
// //             }

// //             Transform target = null;
// //             if (anchor) target = anchor.transform;
// //             else
// //             {
// //                 var pm = FindObjectOfType<PlayerModeManager>(true);
// //                 target = pm && pm.CurrentPlayerRoot
// //                     ? pm.CurrentPlayerRoot
// //                     : (Camera.main ? Camera.main.transform : null);
// //             }
// //             if (!target) return;

// //             var tr = countdownUI.transform;
// //             tr.position = target.position;
// //             tr.rotation = target.rotation;

// //             if (countdownFacePlayer)
// //             {
// //                 var pm = FindObjectOfType<PlayerModeManager>(true);
// //                 var player = pm && pm.CurrentPlayerRoot ? pm.CurrentPlayerRoot
// //                                                        : (Camera.main ? Camera.main.transform : target);
// //                 tr.LookAt(player.position, Vector3.up);
// //                 tr.Rotate(0f, 180f, 0f, Space.Self);
// //             }

// //             var canvas = countdownUI.GetComponentInChildren<Canvas>(true);
// //             if (canvas && canvas.renderMode == RenderMode.WorldSpace)
// //             {
// //                 var cam = GetActiveRigCamera();
// //                 if (cam) canvas.worldCamera = cam;
// //                 canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, countdownSortingOrder);
// //             }
// //         }

// //         private Camera GetActiveRigCamera()
// //         {
// //             var pm = FindObjectOfType<PlayerModeManager>(true);
// //             if (pm && pm.CurrentPlayerRoot)
// //             {
// //                 var cam = pm.CurrentPlayerRoot.GetComponentInChildren<Camera>(true);
// //                 if (cam) return cam;
// //             }
// //             if (Camera.main) return Camera.main;
// //             return FindObjectsOfType<Camera>(true).FirstOrDefault();
// //         }

// //         private static EyeTrackLogger GetLogger()
// //             => EyeTrackLogger.I ?? FindObjectOfType<EyeTrackLogger>(true);

// //         private void HideCountdownUI(bool immediate)
// //         {
// //             if (!countdownUI) return;

// //             var cg = countdownUI.GetComponent<CanvasGroup>();
// //             if (cg)
// //             {
// //                 cg.alpha = 0f;
// //                 cg.blocksRaycasts = false;
// //                 cg.interactable = false;
// //             }

// //             if (immediate)
// //                 countdownUI.gameObject.SetActive(false);
// //         }
// //     }
// // }
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;
// using UnityEngine.UI; // Canvas
// using Random = UnityEngine.Random;

// namespace MoxoCPT
// {
//     public class ChangeShapes : MonoBehaviour
//     {
//         [Header("Pre-start")]
//         [SerializeField] private float _secondsTillGameStarts = 5.0f;

//         [Header("Countdown UI")]
//         [SerializeField] private ExploreHintUI countdownUI;
//         [SerializeField] private string countdownStartText = "Get ready…";
//         [SerializeField] private string countdownEndText = "Go!";

//         [Header("Countdown UI Placement")]
//         [SerializeField] private int countdownSortingOrder = 90;
//         [SerializeField] private bool countdownFacePlayer = false;

//         [Header("UI Timing")]
//         [SerializeField] private float postCountdownDelay = 0.35f;
//         [SerializeField] private float goHoldSeconds = 1.0f;
//         [SerializeField] private float uiClearBuffer = 0.05f;

//         [Header("Exact counts (GLOBAL)")]
//         [SerializeField] private int totalTrials = 53;
//         [SerializeField] private int totalTargets = 33;
//         [SerializeField] private int totalNonTargets = 20;

//         [Header("Balanced Duration Buckets (exact counts)")]
//         [Tooltip("Totals must match: targets=33, nonTargets=20 for 53 trials.")]
//         [SerializeField] private List<DurationBucket> durationBuckets = new List<DurationBucket>()
//         {
//             new DurationBucket { durationSeconds = 0.5f, targetCount = 20, nonTargetCount = 12 }, // 32
//             new DurationBucket { durationSeconds = 1.0f, targetCount = 10, nonTargetCount = 6  }, // 16
//             new DurationBucket { durationSeconds = 3.0f, targetCount = 3,  nonTargetCount = 2  }, // 5
//         };

//         [Header("Shuffle Constraints")]
//         [SerializeField] private int maxConsecutiveTargets = 3;
//         [SerializeField] private int maxConsecutiveSameDuration = 3;
//         [SerializeField] private int scheduleBuildAttempts = 500;

//         [Header("Counterbalancing (4 schedules)")]
//         [Tooltip("If true: schedule is chosen by participantId hash (recommended). If false: use manualScheduleIndex.")]
//         [SerializeField] private bool autoScheduleFromParticipantId = true;

//         [Range(0, 3)]
//         [SerializeField] private int manualScheduleIndex = 0;

//         [Tooltip("Base seed. Schedules A/B/C/D are baseSeed + 0/1/2/3.")]
//         [SerializeField] private int baseSeed = 100000;

//         [Header("Distractors")]
//         [SerializeField] private DistractorSystem distractors;

//         [Header("Rig Cards")]
//         [SerializeField] private Cards cards;

//         private bool _hasStarted;
//         private Coroutine _runner;

//         [System.Serializable]
//         public class DurationBucket
//         {
//             public float durationSeconds;
//             public int targetCount;
//             public int nonTargetCount;
//         }

//         private struct TrialPlan
//         {
//             public Transform card;
//             public bool isTarget;
//             public float durationSeconds; // ON duration
//         }

//         private List<TrialPlan> _plan;
//         private int _scheduleIndex;
//         private int _scheduleSeed;
//         private string _scheduleId; // "A".."D"

//         private void OnEnable()
//         {
//             _hasStarted = false;
//             if (_runner != null) { StopCoroutine(_runner); _runner = null; }
//             StopAllCoroutines();
//             HideCountdownUI(immediate: true);
//             BindCards();
//         }

//         private void OnDisable()
//         {
//             if (_runner != null) StopCoroutine(_runner);
//             _runner = null;
//             StopAllCoroutines();
//             HideCountdownUI(immediate: true);
//         }

//         private void Update()
//         {
//             if (_hasStarted) return;

//             if (GameManager.Instance != null && GameManager.Instance.State == GameManager.GameState.CPT)
//                 ForceStart();
//         }

//         private void BindCards()
//         {
//             if (cards && cards.gameObject) return;

//             cards = GetComponentInParent<Cards>(true);
//             if (!cards) cards = GetComponentsInChildren<Cards>(true).FirstOrDefault();

//             Debug.Log($"[ChangeShapes] Bound Cards => {(cards ? cards.gameObject.name : "NULL")}");
//         }

//         public void ForceStart()
//         {
//             if (_hasStarted) return;
//             _hasStarted = true;

//             if (_runner != null) StopCoroutine(_runner);
//             _runner = StartCoroutine(CoStartAfterIntro());
//         }

//         private IEnumerator CoStartAfterIntro()
//         {
//             BindCards();
//             if (!cards)
//             {
//                 Debug.LogError("[ChangeShapes] No Cards bound for this rig.");
//                 yield break;
//             }

//             // Wait for cards
//             yield return new WaitUntil(() =>
//                 cards != null &&
//                 cards.cardArr != null &&
//                 cards.cardArr.Length > 0
//             );

//             if (totalTargets + totalNonTargets != totalTrials)
//             {
//                 Debug.LogError($"[ChangeShapes] totals mismatch: targets({totalTargets}) + nonTargets({totalNonTargets}) != trials({totalTrials})");
//                 yield break;
//             }

//             if (!ValidateBucketTotals()) yield break;

//             ChooseSchedule();
//             if (!BuildBalancedPlan()) yield break;

//             // Score tracker planned totals
//             var tracker = CPTScoreRuntime.I;
//             if (tracker != null)
//             {
//                 tracker.SetTotalTargets(totalTargets);
//                 tracker.SetTotalDistractors(totalNonTargets);
//             }

//             yield return StartCoroutine(CoRunCountdownAndTrials());
//         }

//         private void ChooseSchedule()
//         {
//             int idx = manualScheduleIndex;

//             if (autoScheduleFromParticipantId)
//             {
//                 string pid = (GameManager.Instance != null) ? GameManager.Instance.ParticipantId : "";
//                 idx = Mathf.Abs(StableHash(pid)) % 4;
//             }

//             _scheduleIndex = Mathf.Clamp(idx, 0, 3);
//             _scheduleSeed = baseSeed + _scheduleIndex;
//             _scheduleId = ((char)('A' + _scheduleIndex)).ToString();

//             Debug.Log($"[ChangeShapes] Using schedule {_scheduleId} (index={_scheduleIndex}) seed={_scheduleSeed}");
//         }

//         private static int StableHash(string s)
//         {
//             // FNV-1a 32-bit
//             unchecked
//             {
//                 const int fnvPrime = 16777619;
//                 int hash = (int)2166136261;

//                 if (string.IsNullOrEmpty(s))
//                     s = "UNKNOWN";

//                 for (int i = 0; i < s.Length; i++)
//                 {
//                     hash ^= s[i];
//                     hash *= fnvPrime;
//                 }
//                 return hash;
//             }
//         }

//         private bool ValidateBucketTotals()
//         {
//             int bucketTargets = durationBuckets.Sum(b => Mathf.Max(0, b.targetCount));
//             int bucketNonTargets = durationBuckets.Sum(b => Mathf.Max(0, b.nonTargetCount));

//             if (bucketTargets != totalTargets || bucketNonTargets != totalNonTargets)
//             {
//                 Debug.LogError(
//                     $"[ChangeShapes] Duration bucket totals mismatch.\n" +
//                     $"Buckets: targets={bucketTargets}, nonTargets={bucketNonTargets}\n" +
//                     $"Expected: targets={totalTargets}, nonTargets={totalNonTargets}\n" +
//                     $"Fix durationBuckets.");
//                 return false;
//             }

//             if (durationBuckets.Count == 0 || durationBuckets.All(b => b.durationSeconds <= 0f))
//             {
//                 Debug.LogError("[ChangeShapes] durationBuckets invalid.");
//                 return false;
//             }

//             return true;
//         }

//         private bool BuildBalancedPlan()
//         {
//             var all = cards.cardArr;
//             var targets = all.Where(t => t && t.CompareTag("Target")).ToArray();
//             var nontargets = all.Where(t => t && t.CompareTag("NonTarget")).ToArray();

//             if (targets.Length == 0 || nontargets.Length == 0)
//             {
//                 Debug.LogError("[ChangeShapes] Need at least one Target and one NonTarget card under this rig.");
//                 return false;
//             }

//             // Pool of (isTarget, duration)
//             var pool = new List<(bool isTarget, float dur)>(totalTrials);
//             foreach (var b in durationBuckets)
//             {
//                 for (int i = 0; i < b.targetCount; i++) pool.Add((true, b.durationSeconds));
//                 for (int i = 0; i < b.nonTargetCount; i++) pool.Add((false, b.durationSeconds));
//             }

//             if (pool.Count != totalTrials)
//             {
//                 Debug.LogError($"[ChangeShapes] Internal pool mismatch: pool={pool.Count}, totalTrials={totalTrials}");
//                 return false;
//             }

//             Random.InitState(_scheduleSeed);

//             List<(bool isTarget, float dur)> ordered = null;

//             for (int attempt = 0; attempt < Mathf.Max(1, scheduleBuildAttempts); attempt++)
//             {
//                 var temp = new List<(bool isTarget, float dur)>(pool);
//                 FisherYates(temp);

//                 if (SatisfiesConstraints(temp))
//                 {
//                     ordered = temp;
//                     break;
//                 }

//                 // Reseed to explore different shuffles but still deterministic per schedule
//                 Random.InitState(_scheduleSeed + attempt + 1);
//             }

//             if (ordered == null)
//             {
//                 Debug.LogError("[ChangeShapes] Could not build constrained schedule. Relax constraints or raise attempts.");
//                 return false;
//             }

//             // Assign actual cards within each type
//             _plan = new List<TrialPlan>(totalTrials);
//             for (int i = 0; i < ordered.Count; i++)
//             {
//                 bool isTarget = ordered[i].isTarget;
//                 float dur = ordered[i].dur;

//                 var card = isTarget
//                     ? targets[Random.Range(0, targets.Length)]
//                     : nontargets[Random.Range(0, nontargets.Length)];

//                 _plan.Add(new TrialPlan { card = card, isTarget = isTarget, durationSeconds = dur });
//             }

//             return true;
//         }

//         private static void FisherYates<T>(IList<T> list)
//         {
//             for (int i = 0; i < list.Count; i++)
//             {
//                 int j = Random.Range(i, list.Count);
//                 (list[i], list[j]) = (list[j], list[i]);
//             }
//         }

//         private bool SatisfiesConstraints(List<(bool isTarget, float dur)> seq)
//         {
//             int targetRun = 0;
//             int durRun = 0;
//             float lastDur = float.NaN;

//             for (int i = 0; i < seq.Count; i++)
//             {
//                 // target streak
//                 if (seq[i].isTarget) targetRun++;
//                 else targetRun = 0;

//                 if (maxConsecutiveTargets > 0 && targetRun > maxConsecutiveTargets)
//                     return false;

//                 // duration streak
//                 if (i == 0 || !Mathf.Approximately(seq[i].dur, lastDur))
//                 {
//                     durRun = 1;
//                     lastDur = seq[i].dur;
//                 }
//                 else
//                 {
//                     durRun++;
//                     if (maxConsecutiveSameDuration > 0 && durRun > maxConsecutiveSameDuration)
//                         return false;
//                 }
//             }

//             return true;
//         }

//         private IEnumerator CoRunCountdownAndTrials()
//         {
//             // Countdown UI (kept simple here; reuse your existing intro logic if needed)
//             if (countdownUI != null)
//             {
//                 if (!countdownUI.gameObject.activeSelf) countdownUI.gameObject.SetActive(true);
//                 countdownUI.ShowExploreHint(_secondsTillGameStarts, countdownStartText, countdownEndText);
//             }

//             float t = _secondsTillGameStarts;
//             while (t > 0f)
//             {
//                 t -= Time.unscaledDeltaTime;
//                 yield return null;
//             }

//             if (goHoldSeconds > 0f)
//                 yield return new WaitForSecondsRealtime(goHoldSeconds);

//             HideCountdownUI(immediate: true);

//             if (uiClearBuffer > 0f)
//                 yield return new WaitForSecondsRealtime(uiClearBuffer);

//             if (postCountdownDelay > 0f)
//                 yield return new WaitForSecondsRealtime(postCountdownDelay);

//             distractors?.StartSystem();

//             for (int trial = 0; trial < totalTrials; trial++)
//             {
//                 Interact._buttonPressed = false;

//                 TrialPlan tp = _plan[trial];
//                 if (!tp.card)
//                 {
//                     Debug.LogWarning($"[ChangeShapes] Plan card is null at trial {trial}. Skipping.");
//                     continue;
//                 }

//                 float dur = tp.durationSeconds; // ON
//                 float isi = dur;                // OFF

//                 // Show stimulus now
//                 TurnCardOn(tp.card);
//                 cards.UpdateCurCard(tp.card);

//                 // Timestamp after visible
//                 long onsetMs = (long)(Time.realtimeSinceStartup * 1000.0f);

//                 // FRESH report per trial
//                 var report = new Report();
//                 report.ResetReport();

//                 report.ScheduleId = _scheduleId;
//                 report.ScheduleSeed = _scheduleSeed;

//                 // Seed the known stimulus fields now
//                 report.StimulusOnsetMs = onsetMs;
//                 report.StimulusType = tp.isTarget ? "target" : "non_target";

//                 // Collect responses across ON + ISI window
//                 StartCoroutine(Interact.StartReport(report, dur + isi, trial, dur, onsetMs, tp.isTarget));

//                 yield return new WaitForSeconds(dur);
//                 TurnCardOff(cards.curCard);

//                 yield return new WaitForSeconds(isi);
//             }

//             distractors?.StopSystem();
//             if (cards != null && cards.curCard != null) TurnCardOff(cards.curCard);

//             MoxoCPTManager.Instance?.OnGameEnd();
//         }

//         private void TurnCardOn(Transform card)
//         {
//             if (!card) return;
//             card.gameObject.SetActive(true);
//             cards.isActive = true;
//         }

//         private void TurnCardOff(Transform card)
//         {
//             if (!card) return;
//             card.gameObject.SetActive(false);
//             cards.isActive = false;
//         }

//         private void HideCountdownUI(bool immediate)
//         {
//             if (!countdownUI) return;
//             if (immediate) countdownUI.gameObject.SetActive(false);
//         }

//         // (Keep your existing placement/camera helpers if needed)
//     }
// }
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MoxoCPT
{
    public class ChangeShapes : MonoBehaviour
    {
        [Header("Pre-start")]
        [SerializeField] private float _secondsTillGameStarts = 5.0f;

        [Header("Countdown UI")]
        [SerializeField] private ExploreHintUI countdownUI;
        [SerializeField] private string countdownStartText = "Get ready…";
        [SerializeField] private string countdownEndText = "Go!";

        [Header("Mid-phase UI (between NDP and DP)")]
        [SerializeField] private float midPhaseCountdownSeconds = 5f;
        [SerializeField] private string midPhaseMainText = "Ready for more challenge";
        [SerializeField] private string midPhaseEndText = "Go!";

        [Tooltip("Character shown during the mid-phase UI. Disabled in the scene; enabled when the UI appears and hidden when it ends.")]
        [SerializeField] private GameObject midPhaseCharacter;

        [Tooltip("Audio clip played when the mid-phase UI appears.")]
        [SerializeField] private AudioClip midPhaseAudio;

        [Tooltip("AudioSource used to play midPhaseAudio. If unassigned, the clip plays at the countdown UI's world position.")]
        [SerializeField] private AudioSource midPhaseAudioSource;

        [Header("Countdown UI Placement")]
        [SerializeField] private int countdownSortingOrder = 90;
        [SerializeField] private bool countdownFacePlayer = false;

        [Header("UI Timing")]
        [SerializeField] private float postCountdownDelay = 0.35f;
        [SerializeField] private float goHoldSeconds = 1.0f;
        [SerializeField] private float uiClearBuffer = 0.05f;

        [Header("Trials")]
        [SerializeField] private int totalTrialsPerIsland = 84;
        [SerializeField] private int trialsPerPhase = 42;

        [Header("Phase Buckets (MUST sum to trialsPerPhase each phase)")]
        [SerializeField] private List<DurationBucket> ndpBuckets = new List<DurationBucket>()
        {
            new DurationBucket { durationSeconds = 0.5f, targetCount = 17, nonTargetCount = 12 },
            new DurationBucket { durationSeconds = 1.0f, targetCount = 10, nonTargetCount = 2  },
            new DurationBucket { durationSeconds = 3.0f, targetCount = 1,  nonTargetCount = 0  },
        };

        [SerializeField] private List<DurationBucket> dpBuckets = new List<DurationBucket>()
        {
            new DurationBucket { durationSeconds = 0.5f, targetCount = 17, nonTargetCount = 12 },
            new DurationBucket { durationSeconds = 1.0f, targetCount = 10, nonTargetCount = 2  },
            new DurationBucket { durationSeconds = 3.0f, targetCount = 1,  nonTargetCount = 0  },
        };

        [Header("Shuffle Constraints (optional)")]
        [SerializeField] private int maxConsecutiveTargets = 3;
        [SerializeField] private int maxConsecutiveSameDuration = 3;
        [SerializeField] private int buildAttempts = 200;

        [Header("Distractors (DP only)")]
        [SerializeField] private DistractorSystem distractors;

        [Header("Rig Cards")]
        [SerializeField] private Cards cards;

        [Header("Progress Display")]
        [Tooltip("If false, the progress UI is disabled even when a display is assigned.")]
        [SerializeField] private bool showProgressDisplay = false;
        [Tooltip("Optional UI label showing 'Progress: n/total cards'. Hidden during training.")]
        [SerializeField] private CPTProgressDisplay progressDisplay;

        [Header("Inter-stimulus interval (ISI)")]
        [Tooltip("Gap after stimulus offset before the next trial (seconds). Durations are drawn in [min,max] using the study seed below.")]
        [SerializeField] private float isiMinSeconds = 0.8f;
        [SerializeField] private float isiMaxSeconds = 1.2f;
        [Tooltip("Seeds 42 base ISIs (uniform [min,max]); each base appears once in NDP and once in DP → same total ISI in both phases for every participant. Combined multiset over 84 trials: each value ×2. Change for a new protocol version.")]
        [SerializeField] private int interStimulusScheduleSeed = unchecked((int)0x4D4F584Fu); // "MOXO"
        [Tooltip("If true: independent Fisher–Yates order within NDP and within DP, seeded with Participant ID (same multiset, different trial-to-ISI mapping per person). If false: fixed global order for everyone.")]
        [SerializeField] private bool shuffleIsiOrderPerParticipant = true;

        // NEW: equal distribution for non-target types
        [Header("Non-Target Distribution (NEW)")]
        [Tooltip("If true, non-target trials are evenly distributed across NonTarget1..NonTargetN based on trailing number in card name.")]
        [SerializeField] private bool equalizeNonTargetTypes = true;

        [Tooltip("How many non-target types exist (e.g., 5 for NonTarget1..NonTarget5, or 6 if you add NonTarget6).")]
        [SerializeField] private int nonTargetTypeCount = 5;

        // NEW: koala visibility per phase
        [Header("Koala Visibility (NEW)")]
        [Tooltip("Disable this GameObject during DP and re-enable during NDP. Leave empty to auto-find by tag/name.")]
        [SerializeField] private GameObject koalaObject;

        [Tooltip("If true, tries to auto-find koala if koalaObject is not assigned.")]
        [SerializeField] private bool autoFindKoala = true;


        private bool _hasStarted;
        private Coroutine _runner;

        // run-level seed (changes every run, consistent inside the run)
        private int _runSeed = 0;

        // cached island id for DP calls
        private string _currentIslandId = "";

        [System.Serializable]
        public class DurationBucket
        {
            public float durationSeconds;
            public int targetCount;
            public int nonTargetCount;
        }

        private struct TrialPlan
        {
            public Transform card;
            public bool isTarget;
            public float durationSeconds; // stimulus ON
        }

        private List<TrialPlan> _ndpPlan;
        private List<TrialPlan> _dpPlan;

        /// <summary>42 base ISIs (study seed). Each appears once in NDP and once in DP → equal phase ISI sums. Perms map phase trial index → base index.</summary>
        private float[] _isiBaseValues;
        private int[] _isiPermNdp;
        private int[] _isiPermDp;
        private int _isiPermSeedNdp;
        private int _isiPermSeedDp;
        private int _trialPlanSeedNdp;
        private int _trialPlanSeedDp;

        // -------- lifecycle guards --------
        private void OnEnable()
        {
            _hasStarted = false;
            if (_runner != null) { StopCoroutine(_runner); _runner = null; }
            StopAllCoroutines();
            HideCountdownUI(immediate: true);
            BindCards();

            // Safety: ensure distractors are not running when enabling (NDP protection)
            distractors?.StopDP();

            // Koala is visible for the whole MOXO game, starting from training.
            BindKoala();
            SetKoalaActive(true);
            KoalaAnimBus.BroadcastToActiveKoalas(k => k.EnsureStandingIdle());
        }

        private void OnDisable()
        {
            if (_runner != null) StopCoroutine(_runner);
            _runner = null;
            StopAllCoroutines();
            HideCountdownUI(immediate: true);

            // Safety: always stop distractors if this rig disables
            distractors?.StopDP();

            // NEW: restore koala if rig disables mid-DP
            SetKoalaActive(true);
        }

        private void Update()
        {
            if (_hasStarted) return;

            if (GameManager.Instance != null && GameManager.Instance.State == GameManager.GameState.CPT)
                ForceStart();
        }

        public void ForceStart()
        {
            if (_hasStarted) return;
            _hasStarted = true;

            if (_runner != null) StopCoroutine(_runner);
            _runner = StartCoroutine(CoStartAfterIntro());
        }

        // --- Cards binding (scoped to this rig) ---
        private void BindCards()
        {
            if (cards && cards.gameObject) return;

            cards = GetComponentInParent<Cards>(true);
            if (!cards) cards = GetComponentsInChildren<Cards>(true).FirstOrDefault();

            if (!cards)
            {
                var myRig = GetRigRoot(transform);
                var all = FindObjectsOfType<Cards>(true);
                cards = all.FirstOrDefault(c => c && IsUnder(c.transform, myRig));
            }

            Debug.Log($"[ChangeShapes] Bound Cards => {(cards ? cards.gameObject.name : "NULL")}");
        }

        private static Transform GetRigRoot(Transform t)
        {
            Transform best = t;
            while (t != null)
            {
                if (t.name.StartsWith("MoxoCPT_", System.StringComparison.OrdinalIgnoreCase))
                    best = t;
                t = t.parent;
            }
            return best;
        }

        private static bool IsUnder(Transform child, Transform root)
        {
            if (!child || !root) return false;
            for (var p = child; p != null; p = p.parent)
                if (p == root) return true;
            return false;
        }

        private IEnumerator CoStartAfterIntro()
        {
            BindCards();
            if (!cards)
            {
                Debug.LogError("[ChangeShapes] No Cards bound for this rig.");
                yield break;
            }

            // Koala is visible for the whole MOXO game.
            BindKoala();
            SetKoalaActive(true);

            var island = IslandTravelManager.I ? IslandTravelManager.I.CurrentIsland : null;
            _currentIslandId = (island != null) ? (island.islandId ?? "") : "";

            if (island != null)
            {
                if (island.countdownSeconds > 0f) _secondsTillGameStarts = island.countdownSeconds;
                if (island.cardDurationsOverride != null && island.cardDurationsOverride.Length > 0)
                    cards.cardDuration = island.cardDurationsOverride;
            }

            // Build a run-level seed ONCE per start
            string pid = (GameManager.Instance != null) ? (GameManager.Instance.ParticipantId ?? "") : "";
            string sid = LoggingReport.CurrentSessionId ?? "";
            _runSeed = HashCombine(
                System.Environment.TickCount,
                (int)(Time.realtimeSinceStartup * 1000f),
                StableHash(pid),
                StableHash(sid)
            );

            // wait for IntroScreen to be gone (realtime)
            var intro = IntroScreen.Instance ?? FindObjectOfType<IntroScreen>(true);
            if (intro != null)
            {
                float timeout = 30f;
                while (timeout > 0f)
                {
                    bool hidden = !intro.gameObject.activeInHierarchy;
                    if (!hidden)
                    {
                        var cg = intro.GetComponent<CanvasGroup>();
                        hidden = cg && cg.alpha <= 0.001f;
                    }
                    if (hidden) break;

                    timeout -= Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            if (countdownUI == null)
                countdownUI = FindObjectOfType<ExploreHintUI>(true);

            ApplyCountdownAnchorAndPlace(island);
            HideCountdownUI(immediate: true);

            // wait for cards ready
            yield return new WaitUntil(() =>
                cards != null &&
                cards.cardArr != null &&
                cards.cardArr.Length > 0
            );

            if (trialsPerPhase * 2 != totalTrialsPerIsland)
            {
                Debug.LogError($"[ChangeShapes] totalTrialsPerIsland must equal trialsPerPhase*2. Now: {totalTrialsPerIsland} vs {trialsPerPhase * 2}");
                yield break;
            }

            if (!ValidatePhaseBuckets("NDP", ndpBuckets)) yield break;
            if (!ValidatePhaseBuckets("DP", dpBuckets)) yield break;

            BuildGlobalIsiSchedule(pid, _currentIslandId);

            // build randomized plan per phase (participant-specific order)
            if (!BuildPlanForPhase("NDP", _currentIslandId, ndpBuckets, pid, out _ndpPlan, out _trialPlanSeedNdp)) yield break;
            if (!BuildPlanForPhase("DP", _currentIslandId, dpBuckets, pid, out _dpPlan, out _trialPlanSeedDp)) yield break;

            // Set planned totals used by the results screen.
            // Use the finalized built plans as source of truth so totals remain correct
            // even if buckets/config are edited or a plan build is retried.
            int plannedTargetTotal = 0;
            int plannedNonTargetTotal = 0;
            if (_ndpPlan != null)
            {
                for (int i = 0; i < _ndpPlan.Count; i++)
                {
                    if (_ndpPlan[i].isTarget) plannedTargetTotal++;
                    else plannedNonTargetTotal++;
                }
            }
            if (_dpPlan != null)
            {
                for (int i = 0; i < _dpPlan.Count; i++)
                {
                    if (_dpPlan[i].isTarget) plannedTargetTotal++;
                    else plannedNonTargetTotal++;
                }
            }

            var tracker = CPTScoreRuntime.I;
            if (tracker != null)
            {
                tracker.SetTotalTargets(plannedTargetTotal);
                tracker.SetTotalDistractors(plannedNonTargetTotal);
            }

            float ndpStimSum = 0f, dpStimSum = 0f;
            for (int i = 0; i < _ndpPlan.Count; i++) ndpStimSum += _ndpPlan[i].durationSeconds;
            for (int i = 0; i < _dpPlan.Count; i++) dpStimSum += _dpPlan[i].durationSeconds;
            if (Mathf.Abs(ndpStimSum - dpStimSum) > 0.0001f)
                Debug.LogWarning($"[ChangeShapes] NDP stimulus sum ({ndpStimSum:0.###}s) ≠ DP ({dpStimSum:0.###}s); use matching duration buckets so NDP and DP total time match.");

            LogPlannedCptAndUiDurations();

            yield return StartCoroutine(CoRunCountdownAndTrials());
        }

        private bool ValidatePhaseBuckets(string phaseName, List<DurationBucket> buckets)
        {
            int t = buckets.Sum(b => Mathf.Max(0, b.targetCount));
            int nt = buckets.Sum(b => Mathf.Max(0, b.nonTargetCount));
            int total = t + nt;

            if (total != trialsPerPhase)
            {
                Debug.LogError($"[ChangeShapes] {phaseName} buckets must sum to {trialsPerPhase} trials. Now: {total} (targets={t}, nonTargets={nt})");
                return false;
            }
            if (buckets.Count == 0 || buckets.All(b => b.durationSeconds <= 0f))
            {
                Debug.LogError($"[ChangeShapes] {phaseName} buckets invalid (need durationSeconds > 0).");
                return false;
            }
            return true;
        }

        private void BuildGlobalIsiSchedule(string participantId, string islandId)
        {
            int half = trialsPerPhase;
            float min = Mathf.Min(isiMinSeconds, isiMaxSeconds);
            float max = Mathf.Max(isiMinSeconds, isiMaxSeconds);

            var valueRng = new System.Random(unchecked(interStimulusScheduleSeed));
            _isiBaseValues = new float[half];
            for (int i = 0; i < half; i++)
                _isiBaseValues[i] = (float)(min + (max - min) * valueRng.NextDouble());

            _isiPermNdp = new int[half];
            _isiPermDp = new int[half];
            for (int i = 0; i < half; i++)
            {
                _isiPermNdp[i] = i;
                _isiPermDp[i] = i;
            }

            int study = unchecked(interStimulusScheduleSeed);
            int islandHash = StableHash(string.IsNullOrWhiteSpace(islandId) ? "UNKNOWN_ISLAND" : islandId.Trim());
            if (shuffleIsiOrderPerParticipant)
            {
                string pidKey = string.IsNullOrWhiteSpace(participantId) ? "UNKNOWN" : participantId.Trim();
                int h = StableHash(pidKey);
                _isiPermSeedNdp = HashCombine(study, h, islandHash, unchecked((int)0x4E4450));
                _isiPermSeedDp = HashCombine(study, h, islandHash, unchecked((int)0x445044));
                FisherYates(_isiPermNdp, new System.Random(_isiPermSeedNdp)); // NDP
                FisherYates(_isiPermDp, new System.Random(_isiPermSeedDp)); // DP
            }
            else
            {
                _isiPermSeedNdp = HashCombine(study, islandHash, 1);
                _isiPermSeedDp = HashCombine(study, islandHash, 2);
                FisherYates(_isiPermNdp, new System.Random(_isiPermSeedNdp));
                FisherYates(_isiPermDp, new System.Random(_isiPermSeedDp));
            }
        }

        private int GetIsiBaseIndexForGlobalTrial(int globalTrialIndex)
        {
            int half = trialsPerPhase;
            if (_isiPermNdp == null || _isiPermDp == null || _isiPermNdp.Length != half || _isiPermDp.Length != half)
                return -1;
            if (globalTrialIndex < half)
                return Mathf.Clamp(_isiPermNdp[globalTrialIndex], 0, half - 1);
            int phaseIdx = Mathf.Clamp(globalTrialIndex - half, 0, half - 1);
            return Mathf.Clamp(_isiPermDp[phaseIdx], 0, half - 1);
        }

        private float GetIsiForGlobalTrial(int globalTrialIndex)
        {
            int half = trialsPerPhase;
            if (_isiBaseValues == null || _isiBaseValues.Length != half ||
                _isiPermNdp == null || _isiPermNdp.Length != half ||
                _isiPermDp == null || _isiPermDp.Length != half)
                return Mathf.Max(0f, (Mathf.Min(isiMinSeconds, isiMaxSeconds) + Mathf.Max(isiMinSeconds, isiMaxSeconds)) * 0.5f);

            if (globalTrialIndex < half)
            {
                int k = Mathf.Clamp(_isiPermNdp[globalTrialIndex], 0, half - 1);
                return Mathf.Max(0f, _isiBaseValues[k]);
            }
            else
            {
                int phaseIdx = globalTrialIndex - half;
                phaseIdx = Mathf.Clamp(phaseIdx, 0, half - 1);
                int k = Mathf.Clamp(_isiPermDp[phaseIdx], 0, half - 1);
                return Mathf.Max(0f, _isiBaseValues[k]);
            }
        }

        // -------- NEW helpers for NonTarget type indexing --------
        private static int ExtractTrailingNumber(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return -1;
            s = s.Trim();

            int end = s.Length - 1;
            int start = end;

            while (start >= 0 && char.IsDigit(s[start])) start--;
            start++;

            if (start <= end)
            {
                string digits = s.Substring(start, end - start + 1);
                if (int.TryParse(digits, out int k)) return k;
            }

            return -1;
        }

        private int GetNonTargetTypeIndex(Transform card)
        {
            if (!card) return -1;
            int k = ExtractTrailingNumber(card.name);
            if (k < 1 || k > Mathf.Max(1, nonTargetTypeCount)) return -1;
            return k;
        }

        // Uses local System.Random (does NOT touch UnityEngine.Random state).
        private bool BuildPlanForPhase(string phaseName, string islandId, List<DurationBucket> buckets, string participantId, out List<TrialPlan> plan, out int planSeed)
        {
            plan = null;
            planSeed = 0;

            var all = cards.cardArr;
            var targets = all.Where(t => t && t.CompareTag("Target")).ToArray();
            var nontargets = all.Where(t => t && t.CompareTag("NonTarget")).ToArray();

            if (targets.Length == 0 || nontargets.Length == 0)
            {
                Debug.LogError("[ChangeShapes] Need at least one Target and one NonTarget card in this rig.");
                return false;
            }

            // NEW: group non-targets by type (NonTarget1..NonTargetN)
            var ntByType = nontargets
                .Select(t => new { t, k = GetNonTargetTypeIndex(t) })
                .Where(x => x.k >= 1)
                .GroupBy(x => x.k)
                .ToDictionary(g => g.Key, g => g.Select(x => x.t).Where(x => x).ToArray());

            if (equalizeNonTargetTypes)
            {
                int nTypes = Mathf.Max(1, nonTargetTypeCount);
                for (int k = 1; k <= nTypes; k++)
                {
                    if (!ntByType.ContainsKey(k) || ntByType[k] == null || ntByType[k].Length == 0)
                    {
                        Debug.LogError($"[ChangeShapes] equalizeNonTargetTypes is ON, but NonTarget type {k} is missing. " +
                                       $"Ensure you have cards named like NonTarget{k} (or ending with {k}).");
                        return false;
                    }
                }
            }

            var pool = new List<(bool isTarget, float dur)>(trialsPerPhase);
            foreach (var b in buckets)
            {
                for (int i = 0; i < b.targetCount; i++) pool.Add((true, b.durationSeconds));
                for (int i = 0; i < b.nonTargetCount; i++) pool.Add((false, b.durationSeconds));
            }

            // Study + island + phase + participant: participant-specific trial order while preserving bucket totals.
            string pidKey = string.IsNullOrWhiteSpace(participantId) ? "UNKNOWN" : participantId.Trim();
            int baseSeed = HashCombine(
                unchecked(interStimulusScheduleSeed),
                StableHash(islandId),
                StableHash(phaseName),
                StableHash(pidKey));
            planSeed = baseSeed;

            List<(bool isTarget, float dur)> ordered = null;

            for (int attempt = 0; attempt < Mathf.Max(1, buildAttempts); attempt++)
            {
                var rng = new System.Random(unchecked(baseSeed + attempt));

                var temp = new List<(bool isTarget, float dur)>(pool);
                FisherYates(temp, rng);

                if (SatisfiesConstraints(temp))
                {
                    ordered = temp;
                    break;
                }
            }

            if (ordered == null)
            {
                Debug.LogError($"[ChangeShapes] Could not satisfy constraints for phase {phaseName}. Relax constraints or increase buildAttempts.");
                return false;
            }

            // card picking uses a separate deterministic stream (still phase+island specific)
            var pickRng = new System.Random(HashCombine(baseSeed, unchecked((int)0x51ED270B)));

            // NEW: build an evenly distributed non-target type sequence for this phase
            List<int> ntTypeSeq = null;
            int ntNeeded = ordered.Count(x => !x.isTarget);

            if (equalizeNonTargetTypes)
            {
                int nTypes = Mathf.Max(1, nonTargetTypeCount);

                ntTypeSeq = new List<int>(ntNeeded);

                int baseCount = ntNeeded / nTypes;
                int remainder = ntNeeded % nTypes;

                for (int k = 1; k <= nTypes; k++)
                    for (int i = 0; i < baseCount; i++)
                        ntTypeSeq.Add(k);

                var typeOrder = Enumerable.Range(1, nTypes).ToList();
                FisherYates(typeOrder, pickRng);
                for (int i = 0; i < remainder; i++)
                    ntTypeSeq.Add(typeOrder[i]);

                FisherYates(ntTypeSeq, pickRng);
            }

            var perTypePickCursor = new Dictionary<int, int>();
            int ntCursor = 0;

            plan = new List<TrialPlan>(trialsPerPhase);
            for (int i = 0; i < ordered.Count; i++)
            {
                bool isTarget = ordered[i].isTarget;
                float dur = ordered[i].dur;

                Transform card;

                if (isTarget)
                {
                    card = targets[NextInt(pickRng, 0, targets.Length)];
                }
                else
                {
                    if (equalizeNonTargetTypes && ntTypeSeq != null && ntTypeSeq.Count > 0)
                    {
                        int k = ntTypeSeq[Mathf.Clamp(ntCursor, 0, ntTypeSeq.Count - 1)];
                        ntCursor++;

                        var arr = ntByType[k];

                        if (!perTypePickCursor.TryGetValue(k, out int c)) c = 0;
                        card = arr[c % arr.Length];
                        perTypePickCursor[k] = c + 1;
                    }
                    else
                    {
                        card = nontargets[NextInt(pickRng, 0, nontargets.Length)];
                    }
                }

                plan.Add(new TrialPlan { card = card, isTarget = isTarget, durationSeconds = dur });
            }

            Debug.Log($"[ChangeShapes] Built plan for {phaseName} island='{islandId}' seed={baseSeed}");
            return true;
        }

        private static void FisherYates<T>(IList<T> list, System.Random rng)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int j = NextInt(rng, i, list.Count);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private bool SatisfiesConstraints(List<(bool isTarget, float dur)> seq)
        {
            int targetRun = 0;
            int durRun = 0;
            float lastDur = float.NaN;

            for (int i = 0; i < seq.Count; i++)
            {
                if (seq[i].isTarget) targetRun++;
                else targetRun = 0;

                if (maxConsecutiveTargets > 0 && targetRun > maxConsecutiveTargets)
                    return false;

                if (i == 0 || !Mathf.Approximately(seq[i].dur, lastDur))
                {
                    durRun = 1;
                    lastDur = seq[i].dur;
                }
                else
                {
                    durRun++;
                    if (maxConsecutiveSameDuration > 0 && durRun > maxConsecutiveSameDuration)
                        return false;
                }
            }
            return true;
        }

        // ----------------- countdown + phases -----------------
        private IEnumerator CoRunCountdownAndTrials()
        {
            // Safety: ensure DP distractors are off before anything starts
            distractors?.StopDP();

            yield return StartCoroutine(ShowCountdown(_secondsTillGameStarts, countdownStartText, countdownEndText));

            // Show progress counter for the real game only when explicitly enabled.
            if (showProgressDisplay) progressDisplay?.Show(totalTrialsPerIsland);

            // NDP (NO distractors)
            yield return StartCoroutine(RunPhase("NDP", _ndpPlan, globalStartIndex: 0));

            // mid-phase UI — enable character + play audio, then hide both when done
            if (midPhaseCharacter)
            {
                midPhaseCharacter.SetActive(true);
                // Yield one frame so any OnEnable/Start on the character settles
                // before we start the countdown (prevents scripts on the character
                // from interfering with the coroutine chain).
                yield return null;
            }

            // Show koala standing idle during the mid-phase countdown.
            BindKoala();
            SetKoalaActive(true);
            yield return null;
            KoalaAnimBus.BroadcastToActiveKoalas(k => k.EnsureStandingIdle());

            Debug.Log("[ChangeShapes] Mid-phase: starting countdown UI.");
            PlayMidPhaseAudio();

            yield return StartCoroutine(ShowCountdown(midPhaseCountdownSeconds, midPhaseMainText, midPhaseEndText));

            Debug.Log("[ChangeShapes] Mid-phase: countdown done, hiding character.");
            StopMidPhaseAudio();
            if (midPhaseCharacter) midPhaseCharacter.SetActive(false);

            // DP (START distractors here)
            yield return StartCoroutine(RunPhase("DP", _dpPlan, globalStartIndex: trialsPerPhase));

            // end safety
            distractors?.StopDP();

            SetKoalaActive(true);

            if (cards != null && cards.curCard != null) TurnCardOff(cards.curCard);

            // Hide progress counter before results screen appears.
            if (showProgressDisplay) progressDisplay?.Hide();

            // MoxoCPTManager.OnGameEnd owns the post-game flow (reward + results screen).
            // The happy animation is triggered from there so it persists through results.
            MoxoCPTManager.Instance?.OnGameEnd();
        }

        
        private IEnumerator RunPhase(string phaseName, List<TrialPlan> plan, int globalStartIndex)
        {
            Debug.Log($"[ChangeShapes] Phase {phaseName} starting.");

            // ✅ DP-only distractors control (ChangeShapes owns timing)
            if (phaseName == "DP")
            {
                // DP: koala visible and standing idle (no cheer).
                BindKoala();
                SetKoalaActive(true);

                // Allow Animator to initialize after activation.
                yield return null;

                KoalaStandIdle();

                float dpSeconds = ComputePhaseSeconds(_dpPlan, trialsPerPhase);
                distractors?.StartDP(_currentIslandId, _runSeed, dpSeconds);
            }
            else
            {
                // NDP: koala visible and standing idle throughout.
                BindKoala();
                SetKoalaActive(true);
                yield return null;

                KoalaAnimBus.BroadcastToActiveKoalas(k => k.EnsureStandingIdle());

                distractors?.StopDP();
            }

            var logger = GetLogger();

            for (int i = 0; i < plan.Count; i++)
            {
                // Do NOT clear Interact._pressCount here.
                // StartReport snapshots the counter at trial start, so it naturally
                // ignores any presses that already happened before this trial.

                if (phaseName == "DP")
                    distractors?.SetTrialContext(globalStartIndex + i, i);

                var tp = plan[i];
                if (!tp.card)
                {
                    Debug.LogWarning($"[ChangeShapes] Null card in {phaseName} at phase trial {i}. Skipping.");
                    continue;
                }

                float dur = tp.durationSeconds;
                int globalIdx = globalStartIndex + i;
                float isi = GetIsiForGlobalTrial(globalIdx);

                TurnCardOn(tp.card);
                cards.UpdateCurCard(tp.card);

                // globalStartIndex + i + 1 gives the 1-based card number across both phases.
                if (showProgressDisplay) progressDisplay?.UpdateCount(globalStartIndex + i + 1);

                long onsetMs = (long)(Time.realtimeSinceStartup * 1000.0f);

                var report = new Report();
                report.ResetReport();
                report.Phase = phaseName;
                report.PhaseTrialIndex = i;
                report.TrialIndex = globalStartIndex + i;
                report.StimulusOnsetMs = onsetMs;
                report.StimulusType = GetStimulusTypeLabel(tp.card, tp.isTarget);
                report.StimulusDurationMs = Mathf.RoundToInt(dur * 1000f);
                report.InterStimulusIntervalMs = Mathf.RoundToInt(isi * 1000f);
                report.IsiBaseIndex = GetIsiBaseIndexForGlobalTrial(globalIdx);
                report.InterStimulusScheduleSeed = unchecked(interStimulusScheduleSeed);
                report.TrialPlanSeed = (phaseName == "NDP") ? _trialPlanSeedNdp : _trialPlanSeedDp;
                report.IsiPermutationSeed = (phaseName == "NDP") ? _isiPermSeedNdp : _isiPermSeedDp;
                report.StimulusName = tp.card != null ? tp.card.name : "";

                if (logger != null && logger.IsLogging)
                    logger.MarkNewTrial(report.TrialIndex);

                StartCoroutine(Interact.StartReport(
                    report,
                    dur + isi,
                    report.TrialIndex,
                    dur,
                    onsetMs,
                    tp.isTarget,
                    phaseName,
                    i,
                    true
                ));

                yield return new WaitForSeconds(dur);

                long offsetMs = (long)(Time.realtimeSinceStartup * 1000.0f);
                report.StimulusOffsetMs = offsetMs;

                if (report.StimulusOnsetMs > 0)
                    report.StimulusActualDurationMs = (int)Mathf.Max(0, (float)(offsetMs - report.StimulusOnsetMs));
                else
                    report.StimulusActualDurationMs = -1;

                TurnCardOff(cards.curCard);

                yield return new WaitForSeconds(isi);
            }

            // If we just finished DP, stop distractors immediately
            if (phaseName == "DP")
            {
                distractors?.StopDP();

                // ✅ After DP finishes: keep koala visible but return it to idle
                BindKoala();
                SetKoalaActive(true);
                yield return null;
                KoalaStandIdle();
            }

            Debug.Log($"[ChangeShapes] Phase {phaseName} complete.");
        }

        private float ComputePhaseSeconds(List<TrialPlan> plan, int globalStartIndex)
        {
            if (plan == null || plan.Count == 0) return 0f;

            float sum = 0f;
            for (int i = 0; i < plan.Count; i++)
                sum += plan[i].durationSeconds + GetIsiForGlobalTrial(globalStartIndex + i);

            return sum;
        }

        /// <summary>
        /// Wall time for card+ISI phases (matches what is passed to DistractorSystem as DP budget). Excludes countdown and mid-phase UI.
        /// </summary>
        private void LogPlannedCptAndUiDurations()
        {
            float ndpCards = ComputePhaseSeconds(_ndpPlan, 0);
            float dpCards = ComputePhaseSeconds(_dpPlan, trialsPerPhase);

            float countdownBlock = _secondsTillGameStarts + goHoldSeconds + uiClearBuffer + postCountdownDelay;
            float midBlock = midPhaseCountdownSeconds + goHoldSeconds + uiClearBuffer + postCountdownDelay;

            float isiSumHalf = 0f;
            if (_isiBaseValues != null)
                for (int i = 0; i < _isiBaseValues.Length; i++)
                    isiSumHalf += _isiBaseValues[i];

            Debug.Log(
                "[ChangeShapes] Planned durations: NDP and DP totals match across participants when buckets match (study-seeded stimulus plan + paired ISIs). " +
                $"initial countdown block ≈ {countdownBlock:0.###} s, " +
                $"NDP (stimulus+ISI) = {ndpCards:0.###} s, " +
                $"mid-phase UI block ≈ {midBlock:0.###} s, " +
                $"DP (stimulus+ISI) = {dpCards:0.###} s (equals NDP: {Mathf.Abs(ndpCards - dpCards) < 0.0001f}), " +
                $"sum(ISI) per phase from bases = {isiSumHalf:0.###} s, " +
                $"distractor DP budget = {dpCards:0.###} s."
            );
        }

        private IEnumerator ShowCountdown(float seconds, string mainText, string endText)
        {
            KoalaAnimBus.BroadcastToActiveKoalas(d => d.OnCountdownStart());

            if (countdownUI != null)
            {
                if (!countdownUI.gameObject.activeSelf) countdownUI.gameObject.SetActive(true);
                var cg = countdownUI.GetComponent<CanvasGroup>();
                if (cg) { cg.alpha = 1f; cg.blocksRaycasts = true; cg.interactable = true; }

                countdownUI.ShowExploreHint(seconds, mainText, endText);
            }

            float t = seconds;
            while (t > 0f)
            {
                t -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (goHoldSeconds > 0f)
                yield return new WaitForSecondsRealtime(goHoldSeconds);

            HideCountdownUI(immediate: true);

            if (uiClearBuffer > 0f)
                yield return new WaitForSecondsRealtime(uiClearBuffer);

            if (postCountdownDelay > 0f)
                yield return new WaitForSecondsRealtime(postCountdownDelay);
        }

        private void TurnCardOn(Transform card)
        {
            if (!card) return;
            card.gameObject.SetActive(true);
            cards.isActive = true;
        }

        private void TurnCardOff(Transform card)
        {
            if (!card) return;
            card.gameObject.SetActive(false);
            cards.isActive = false;
        }

        // ---------- placement ----------
        private void ApplyCountdownAnchorAndPlace(IslandData island)
        {
            if (!countdownUI) return;

            CountdownAnchor anchor = null;
            var islandId = island != null ? island.islandId?.Trim().ToUpperInvariant() : null;
            if (!string.IsNullOrEmpty(islandId))
            {
                anchor = FindObjectsOfType<CountdownAnchor>(true)
                    .FirstOrDefault(a => a.islandId == islandId);

                if (anchor && anchor.overrideCountdownSeconds > 0f)
                    _secondsTillGameStarts = anchor.overrideCountdownSeconds;
            }

            Transform target = null;
            if (anchor) target = anchor.transform;
            else
            {
                var pm = FindObjectOfType<PlayerModeManager>(true);
                target = pm && pm.CurrentPlayerRoot
                    ? pm.CurrentPlayerRoot
                    : (Camera.main ? Camera.main.transform : null);
            }
            if (!target) return;

            var tr = countdownUI.transform;
            tr.position = target.position;
            tr.rotation = target.rotation;

            if (countdownFacePlayer)
            {
                var pm = FindObjectOfType<PlayerModeManager>(true);
                var player = pm && pm.CurrentPlayerRoot ? pm.CurrentPlayerRoot
                                                       : (Camera.main ? Camera.main.transform : target);
                tr.LookAt(player.position, Vector3.up);
                tr.Rotate(0f, 180f, 0f, Space.Self);
            }

            var canvas = countdownUI.GetComponentInChildren<Canvas>(true);
            if (canvas && canvas.renderMode == RenderMode.WorldSpace)
            {
                var cam = GetActiveRigCamera();
                if (cam) canvas.worldCamera = cam;
                canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, countdownSortingOrder);
            }
        }

        private Camera GetActiveRigCamera()
        {
            var pm = FindObjectOfType<PlayerModeManager>(true);
            if (pm && pm.CurrentPlayerRoot)
            {
                var cam = pm.CurrentPlayerRoot.GetComponentInChildren<Camera>(true);
                if (cam) return cam;
            }
            if (Camera.main) return Camera.main;
            return FindObjectsOfType<Camera>(true).FirstOrDefault();
        }

        private static EyeTrackLogger GetLogger()
            => EyeTrackLogger.I ?? FindObjectOfType<EyeTrackLogger>(true);

        // Returns the assigned AudioSource, or lazily creates one on this GameObject.
        // ChangeShapes is always active during gameplay, so the source is always usable.
        private AudioSource GetOrCreateMidPhaseAudioSource()
        {
            if (midPhaseAudioSource) return midPhaseAudioSource;
            midPhaseAudioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            midPhaseAudioSource.playOnAwake = false;
            midPhaseAudioSource.spatialBlend = 0f; // 2-D so it's audible regardless of position
            return midPhaseAudioSource;
        }

        private void PlayMidPhaseAudio()
        {
            if (!midPhaseAudio) return;
            try
            {
                var src = GetOrCreateMidPhaseAudioSource();
                if (src) { src.clip = midPhaseAudio; src.Play(); }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ChangeShapes] PlayMidPhaseAudio failed (non-fatal): {e.Message}");
            }
        }

        private void StopMidPhaseAudio()
        {
            if (midPhaseAudioSource && midPhaseAudioSource.isPlaying)
                midPhaseAudioSource.Stop();
        }

        private void HideCountdownUI(bool immediate)
        {
            if (!countdownUI) return;

            var cg = countdownUI.GetComponent<CanvasGroup>();
            if (cg)
            {
                cg.alpha = 0f;
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }

            if (immediate)
                countdownUI.gameObject.SetActive(false);
        }

        // ----------------- koala helpers (NEW) -----------------
        private void BindKoala()
        {
            if (koalaObject != null) return;
            if (!autoFindKoala) return;

            // Try tag first (if you have it)
            koalaObject = SafeFindByTag("NPC_Koala");

            // Fallback to common name
            if (koalaObject == null)
                koalaObject = GameObject.Find("Koala");

            // Last resort: find by partial name
            if (koalaObject == null)
            {
                var all = FindObjectsOfType<GameObject>(true);
                koalaObject = all.FirstOrDefault(go =>
                    go && go.name.ToLowerInvariant().Contains("koala"));
            }

            if (koalaObject != null)
                Debug.Log($"[ChangeShapes] Koala bound => {koalaObject.name}");
        }

        private void SetKoalaActive(bool on)
        {
            if (koalaObject == null) return;
            if (koalaObject.activeSelf == on) return;
            koalaObject.SetActive(on);
        }

        private static GameObject SafeFindByTag(string tag)
        {
            try
            {
                var arr = GameObject.FindGameObjectsWithTag(tag);
                return (arr != null && arr.Length > 0) ? arr[0] : null;
            }
            catch (UnityException) { return null; }
        }

        // ----------------- koala animation helpers (NEW) -----------------
        private void KoalaStandIdle()
        {
            BindKoala();
            // Broadcast to whichever koalas are currently active.
            // Callers are responsible for activating/deactivating the koala object
            // before and after this call — do NOT force-show the koala here.
            KoalaAnimBus.BroadcastToActiveKoalas(k => k.EnsureStandingIdle());
        }

        private void KoalaCheer()
        {
            BindKoala();
            if (koalaObject) SetKoalaActive(true);

            KoalaAnimBus.BroadcastToAllKoalas(k => k.PlayCheer());
        }

        private void KoalaTalkOn()
        {
            BindKoala();
            if (koalaObject) SetKoalaActive(true);

            KoalaAnimBus.BroadcastToAllKoalas(k => k.SetTalking(true));
        }

        private void KoalaTalkOff()
        {
            KoalaAnimBus.BroadcastToAllKoalas(k => k.SetTalking(false));
        }

        // ----------------- koala happy reaction -----------------

        private void OnKoalaCorrectHit()
        {
            // Trigger is consumed by the Animator immediately; the clip plays once
            // and the exit transition returns the koala to idle automatically.
            KoalaAnimBus.BroadcastToActiveKoalas(k => k.PlayHappy());
        }

        // ----------------- hashing helpers -----------------
        private static int StableHash(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            unchecked
            {
                int h = 23;
                for (int i = 0; i < s.Length; i++)
                    h = (h * 31) + s[i];
                return h;
            }
        }


        private static int HashCombine(int a, int b)
        {
            unchecked
            {
                int h = 17;
                h = (h * 31) + a;
                h = (h * 31) + b;
                return h;
            }
        }

        private static string GetStimulusTypeLabel(Transform card, bool isTarget)
        {
            if (isTarget) return "target";
            if (card == null) return "nontarget";

            string name = card.name ?? "";
            name = name.Trim();

            int end = name.Length - 1;
            int start = end;

            while (start >= 0 && char.IsDigit(name[start]))
                start--;

            start++;

            if (start <= end)
            {
                string digits = name.Substring(start, end - start + 1);
                if (int.TryParse(digits, out int k) && k > 0)
                    return $"nontarget_{k}";
            }

            return "nontarget";
        }

        private static int HashCombine(int a, int b, int c)
            => HashCombine(HashCombine(a, b), c);

        private static int HashCombine(int a, int b, int c, int d)
            => HashCombine(HashCombine(a, b, c), d);

        private static int NextInt(System.Random rng, int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return rng.Next(minInclusive, maxExclusive);
        }
    }
}