// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Globalization;
// using System.IO;
// using System.Text;
// using UnityEngine;

// namespace MoxoCPT
// {
//     /// <summary>
//     /// DistractorSystem (DP-only).
//     ///
//     /// ChangeShapes controls phase timing and must call:
//     ///   - distractors.StartDP(islandId, runSeed);
//     ///   - (each DP trial, before/when stimulus shows) distractors.SetTrialContext(trialIndex, phaseTrialIndex);
//     ///   - distractors.StopDP();
//     ///
//     /// This system NEVER runs in NDP unless you accidentally call StartDP().
//     /// </summary>
//     public class DistractorSystem : MonoBehaviour
//     {
//         [System.Serializable]
//         public class Entry
//         {
//             public GameObject obj;
//             public AudioClip clip;
//             public AudioSource audioSource;
//             [HideInInspector] public bool running;

//             [Header("Scheduling")]
//             [Tooltip("Time-share target for this distractor during DP. If <= 0, fallback WEIGHTS[] is used by index.\n" +
//                      "Shares can sum > 1 when overlaps happen.")]
//             [Min(0f)] public float weight = 0f; // 0 = use fallback WEIGHTS
//         }

//         [Header("Population")]
//         [SerializeField] private bool autoFindByTag = true;
//         [SerializeField] private string distractorTag = "Distractor";
//         [SerializeField] private List<Entry> entries = new List<Entry>();

//         [Header("Timing (Active ON)")]
//         [SerializeField] private float minOnSeconds = 3.5f;
//         [SerializeField] private float maxOnSeconds = 15.0f;
//         [SerializeField] private float onStepSeconds = 0.5f;

//         [Header("Timing (Idle/OFF between activations)")]
//         [SerializeField] private float minOffSeconds = 0.5f;
//         [SerializeField] private float maxOffSeconds = 0.5f;

//         [Header("Concurrency")]
//         [SerializeField] private bool allowOverlap = true;
//         [SerializeField] private int maxSimultaneous = 0; // 0 = unlimited

//         [Header("Global audio settings")]
//         [SerializeField] private bool loopAudioWhileVisible = true;
//         [SerializeField] private float audioVolume = 1.0f;

//         [Header("Debug / Safety")]
//         [SerializeField] private bool debugLogs = true;

//         // Kept for inspector compatibility, but intentionally ignored (DP must be explicitly started)
//         [SerializeField] private bool autoStartWhenCPT = false; // DO NOT USE
//         [SerializeField] private bool startImmediatelyForDebug = false;

//         // ---------------- DP-only weighted scheduling ----------------
//         // Fallback weights if Entry.weight <= 0
//         private static readonly double[] WEIGHTS = new double[]
//         {
//             0.10, // D1
//             0.15, // D2
//             0.20, // D3
//             0.20, // D4
//             0.35, // D5
//             0.40  // D6
//         };

//         // DP runtime
//         private bool _dpRunning = false;
//         private Coroutine _dpCo;
//         private System.Random _rng;

//         // timebase: ONE source of truth for CSV timestamps
//         private long _dpStartMs;         // ms since app start (Time.realtimeSinceStartup * 1000)
//         private float _dpStartRealtime;  // seconds since app start (for quota math)

//         private string _dpIslandId = "";

//         // ---- Trial context snapshot (set by ChangeShapes during DP) ----
//         private int _curTrialIndex = -1;
//         private int _curPhaseTrialIndex = -1;

//         // active tracking
//         private readonly HashSet<int> _activeIdx = new HashSet<int>();
//         private Coroutine[] _offCos;                 // per-entry deactivate coroutine
//         private float[] _activeStartRealtime;        // per-entry activation start time (seconds)
//         private long[] _activeStartMs;               // per-entry activation start time (ms)
//         private double[] _activeSecondsAccum;        // achieved active time per distractor
//         private int _activeCount = 0;

//         // pending per-entry snapshot created at ON time (used at OFF time)
//         private PendingEvent[] _pending;
//         private bool[] _pendingValid;

//         private struct PendingEvent
//         {
//             public int trialIndexAtOn;
//             public int phaseTrialIndexAtOn;

//             public long onsetMs;
//             public long dpElapsedOnsetMs;

//             public int plannedDurationMs;

//             public int activeCountAtOnset;
//             public string activeSetAtOnset;

//             public string distractorId;
//             public string distractorName;
//             public float weightTarget;
//         }

//         // repetition avoidance
//         private int _lastSingle = -1;                 // last started distractor
//         private int _lastPairA = -1, _lastPairB = -1; // last pair (sorted)
//         private int _sameSingleStreak = 0;
//         private int _samePairStreak = 0;

//         private void Log(string msg)
//         {
//             if (debugLogs) Debug.Log($"[Distractors] {msg}", this);
//         }

//         private static long NowMs()
//             => (long)(Time.realtimeSinceStartup * 1000.0f);

//         [ContextMenu("Set Default Weights (D1..D6)")]
//         private void SetDefaultWeights()
//         {
//             float[] defaults = { 0.10f, 0.15f, 0.20f, 0.20f, 0.35f, 0.40f };
//             for (int i = 0; i < entries.Count; i++)
//             {
//                 if (entries[i] == null) continue;
//                 entries[i].weight = (i < defaults.Length) ? defaults[i] : defaults[defaults.Length - 1];
//             }
//             Log("Default weights applied to entries (Entry.weight).");
//         }

//         private void Awake()
//         {
//             if (autoFindByTag)
//             {
//                 entries.Clear();
//                 var gos = GameObject.FindGameObjectsWithTag(distractorTag);
//                 foreach (var go in gos) entries.Add(new Entry { obj = go });
//                 Log($"Auto-found {entries.Count} distractors with tag '{distractorTag}'.");
//             }

//             if (autoStartWhenCPT)
//             {
//                 autoStartWhenCPT = false;
//                 Log("autoStartWhenCPT was enabled, but is ignored/disabled to prevent NDP violations.");
//             }

//             foreach (var e in entries)
//                 if (e?.obj) e.obj.SetActive(false);

//             if (startImmediatelyForDebug)
//                 StartDP("DEBUG_ISLAND", 12345);
//         }

//         // ---------------- Public API ----------------

//         /// <summary>
//         /// ChangeShapes should call this during DP each trial (before stimulus ON).
//         /// This lets distractor CSV rows carry trial_index and phase_trial_index for joining.
//         /// </summary>
//         public void SetTrialContext(int trialIndex, int phaseTrialIndex)
//         {
//             _curTrialIndex = trialIndex;
//             _curPhaseTrialIndex = phaseTrialIndex;
//         }

//         public void StartDP(string islandId, int seed)
//         {
//             if (_dpRunning) return;

//             int assigned = 0;
//             foreach (var e in entries) if (e != null && e.obj != null) assigned++;
//             if (assigned == 0)
//             {
//                 Debug.LogWarning("[Distractors] No valid entries. Turn on Auto Find By Tag or assign objects in Entries.", this);
//                 return;
//             }

//             // Enforce DP constraints
//             allowOverlap = true;
//             if (maxSimultaneous != 2) maxSimultaneous = 2;

//             PrepareEntries();

//             _dpRunning = true;
//             _dpIslandId = islandId ?? "";

//             LoggingDistractors.SetCachedIslandId(_dpIslandId);

//             // ✅ One timebase for everything
//             _dpStartMs = NowMs();
//             _dpStartRealtime = Time.realtimeSinceStartup;

//             // Create distractor CSV (separate from trial CSV)
//             LoggingDistractors.CreateDistractorCSV();

//             int combinedSeed = CombineSeeds(seed, HashIslandId(islandId), unchecked((int)0xC0DEC0DE));
//             _rng = new System.Random(combinedSeed);

//             _activeIdx.Clear();
//             _activeCount = 0;

//             _offCos = new Coroutine[entries.Count];
//             _activeStartRealtime = new float[entries.Count];
//             _activeStartMs = new long[entries.Count];
//             _activeSecondsAccum = new double[entries.Count];

//             _pending = new PendingEvent[entries.Count];
//             _pendingValid = new bool[entries.Count];

//             _curTrialIndex = -1;
//             _curPhaseTrialIndex = -1;

//             _lastSingle = -1;
//             _lastPairA = -1;
//             _lastPairB = -1;
//             _sameSingleStreak = 0;
//             _samePairStreak = 0;

//             for (int i = 0; i < entries.Count; i++)
//                 ForceOff(i);

//             _dpCo = StartCoroutine(CoRunDP());
//             Log($"DP START (islandId='{_dpIslandId}', seed={seed}, combinedSeed={combinedSeed}). entries={assigned}");
//         }

//         public void StopDP()
//         {
//             if (!_dpRunning) return;

//             _dpRunning = false;

//             if (_dpCo != null) StopCoroutine(_dpCo);
//             _dpCo = null;

//             if (_offCos != null)
//             {
//                 for (int i = 0; i < _offCos.Length; i++)
//                 {
//                     if (_offCos[i] != null) StopCoroutine(_offCos[i]);
//                     _offCos[i] = null;
//                 }
//             }

//             // finalize + log any active events, then turn off
//             foreach (var idx in new List<int>(_activeIdx))
//             {
//                 FinalizeAndLogEvent(idx); // writes OFF row
//                 AddActiveTimePartial(idx);
//                 ForceOff(idx);
//             }

//             _activeIdx.Clear();
//             _activeCount = 0;

//             Log("DP STOP.");
//             PrintDPSummary();
//         }

//         public void PrintDPSummary()
//         {
//             if (_activeSecondsAccum == null || _activeSecondsAccum.Length == 0)
//             {
//                 Log("No DP summary available (DP not run yet).");
//                 return;
//             }

//             double dpElapsed = Math.Max(0.0001, Time.realtimeSinceStartup - _dpStartRealtime);

//             Log("DP SUMMARY (active time seconds and achieved shares; shares can sum > 1 due to overlap):");

//             for (int i = 0; i < entries.Count; i++)
//             {
//                 string name = (entries[i] != null && entries[i].obj != null) ? entries[i].obj.name : $"D{i + 1}";
//                 double secs = _activeSecondsAccum[i];
//                 double achievedShare = secs / dpElapsed;
//                 double w = GetWeight(i);

//                 Log($"  D{i + 1} '{name}': time={secs:0.00}s | achievedShare={achievedShare:0.000} | weightTarget={w:0.000}");
//             }
//         }

//         // Legacy API safety
//         public void StartSystem()
//         {
//             var island = IslandTravelManager.I != null ? IslandTravelManager.I.CurrentIsland : null;
//             string islandId = island != null ? island.islandId : "UNKNOWN_ISLAND";
//             int seed = Environment.TickCount;
//             Log("StartSystem() called. Mapping to StartDP() (DP-only).");
//             StartDP(islandId, seed);
//         }

//         public void StopSystem()
//         {
//             Log("StopSystem() called. Mapping to StopDP().");
//             StopDP();
//         }

//         // ---------------- Core DP loop ----------------

//         private IEnumerator CoRunDP()
//         {
//             float initialStagger = NextRange(_rng, 0.0f, 0.35f);
//             yield return new WaitForSeconds(initialStagger);

//             while (_dpRunning)
//             {
//                 float off = NextOffDelay(_rng);
//                 if (off > 0f) yield return new WaitForSeconds(off);
//                 if (!_dpRunning) yield break;

//                 if (!allowOverlap)
//                 {
//                     if (_activeCount > 0)
//                         yield return new WaitUntil(() => !_dpRunning || _activeCount == 0);
//                     if (!_dpRunning) yield break;
//                 }
//                 else
//                 {
//                     if (maxSimultaneous > 0 && _activeCount >= maxSimultaneous)
//                         yield return new WaitUntil(() => !_dpRunning || _activeCount < maxSimultaneous);
//                     if (!_dpRunning) yield break;
//                 }

//                 int idx = PickNextIndexWeightedQuota();
//                 if (idx < 0) continue;

//                 float on = NextOnDuration(_rng);
//                 Activate(idx, on);
//             }
//         }

//         // ---------------- Weighted quota picking ----------------

//         private int PickNextIndexWeightedQuota()
//         {
//             int n = entries.Count;
//             if (n == 0) return -1;

//             double elapsed = Math.Max(0.0001, Time.realtimeSinceStartup - _dpStartRealtime);

//             List<int> candidates = new List<int>(n);
//             for (int i = 0; i < n; i++)
//             {
//                 if (!IsValid(i)) continue;
//                 if (_activeIdx.Contains(i)) continue;
//                 candidates.Add(i);
//             }

//             if (candidates.Count == 0) return -1;

//             int chosen = -1;
//             double bestScore = double.NegativeInfinity;

//             for (int c = 0; c < candidates.Count; c++)
//             {
//                 int i = candidates[c];
//                 double w = GetWeight(i);
//                 double desired = w * elapsed;
//                 double achieved = _activeSecondsAccum[i];
//                 double deficit = desired - achieved;

//                 double score = deficit;

//                 if (i == _lastSingle)
//                     score -= 2.0 + _sameSingleStreak * 1.5;

//                 if (_activeCount == 1)
//                 {
//                     int other = GetOnlyActiveIndex();
//                     if (other >= 0)
//                     {
//                         GetSortedPair(other, i, out int a, out int b);
//                         if (a == _lastPairA && b == _lastPairB)
//                             score -= 2.0 + _samePairStreak * 1.5;
//                     }
//                 }

//                 score += NextRange(_rng, -0.02f, 0.02f);

//                 if (score > bestScore)
//                 {
//                     bestScore = score;
//                     chosen = i;
//                 }
//             }

//             if (chosen < 0)
//                 return WeightedRandomCandidate(candidates);

//             if (chosen == _lastSingle && candidates.Count > 1 && _sameSingleStreak >= 1)
//             {
//                 int retry = WeightedRandomCandidate(candidates, exclude: chosen);
//                 if (retry >= 0) chosen = retry;
//             }

//             return chosen;
//         }

//         private int WeightedRandomCandidate(List<int> candidates, int exclude = -1)
//         {
//             double total = 0.0;
//             for (int k = 0; k < candidates.Count; k++)
//             {
//                 int i = candidates[k];
//                 if (i == exclude) continue;

//                 double w = GetWeight(i);

//                 if (i == _lastSingle) w *= 0.15;

//                 if (_activeCount == 1)
//                 {
//                     int other = GetOnlyActiveIndex();
//                     if (other >= 0)
//                     {
//                         GetSortedPair(other, i, out int a, out int b);
//                         if (a == _lastPairA && b == _lastPairB) w *= 0.15;
//                     }
//                 }

//                 total += Math.Max(0.0001, w);
//             }

//             double r = _rng.NextDouble() * total;
//             double acc = 0.0;

//             for (int k = 0; k < candidates.Count; k++)
//             {
//                 int i = candidates[k];
//                 if (i == exclude) continue;

//                 double w = GetWeight(i);

//                 if (i == _lastSingle) w *= 0.15;

//                 if (_activeCount == 1)
//                 {
//                     int other = GetOnlyActiveIndex();
//                     if (other >= 0)
//                     {
//                         GetSortedPair(other, i, out int a, out int b);
//                         if (a == _lastPairA && b == _lastPairB) w *= 0.15;
//                     }
//                 }

//                 w = Math.Max(0.0001, w);
//                 acc += w;
//                 if (acc >= r) return i;
//             }

//             return candidates.Count > 0 ? candidates[0] : -1;
//         }

//         // ---------------- Activation / deactivation ----------------

//         private void Activate(int idx, float onSeconds)
//         {
//             if (!IsValid(idx)) return;
//             if (_activeIdx.Contains(idx)) return;
//             if (!_dpRunning) return;

//             if (!allowOverlap && _activeCount > 0) return;
//             if (maxSimultaneous > 0 && _activeCount >= maxSimultaneous) return;

//             var e = entries[idx];

//             _activeIdx.Add(idx);
//             _activeCount++;
//             e.running = true;

//             if (idx == _lastSingle) _sameSingleStreak++;
//             else _sameSingleStreak = 0;
//             _lastSingle = idx;

//             if (_activeCount == 2)
//             {
//                 int other = GetOtherActiveIndex(idx);
//                 if (other >= 0)
//                 {
//                     GetSortedPair(other, idx, out int a, out int b);

//                     if (a == _lastPairA && b == _lastPairB) _samePairStreak++;
//                     else _samePairStreak = 0;

//                     _lastPairA = a;
//                     _lastPairB = b;
//                 }
//             }

//             // ✅ ON timestamps (single timebase)
//             long onsetMs = NowMs();
//             long dpElapsedOnsetMs = onsetMs - _dpStartMs;

//             e.obj.SetActive(true);

//             _activeStartRealtime[idx] = Time.realtimeSinceStartup;
//             _activeStartMs[idx] = onsetMs;

//             // Create pending snapshot AT ON (Rule 2)
//             CreatePendingEvent(idx, onsetMs, dpElapsedOnsetMs, onSeconds);

//             // ✅ Rule 3 safety: write ON row immediately
//             LoggingDistractors.AppendDistractorEvent(_pending[idx], eventType: "ON", offsetMs: "", dpElapsedOffsetMs: "", actualDurationMs: "");

//             if (debugLogs)
//             {
//                 string label = $"D{idx + 1}";
//                 Log($"ON: {label} '{e.obj.name}' for {onSeconds:0.0}s (active={_activeCount}) trial={_pending[idx].trialIndexAtOn}/{_pending[idx].phaseTrialIndexAtOn}");
//             }

//             if (e.clip != null && e.audioSource != null)
//             {
//                 e.audioSource.clip = e.clip;
//                 e.audioSource.loop = loopAudioWhileVisible;
//                 e.audioSource.volume = audioVolume;
//                 e.audioSource.Play();
//             }

//             if (_offCos != null && idx >= 0 && idx < _offCos.Length && _offCos[idx] != null)
//                 StopCoroutine(_offCos[idx]);

//             _offCos[idx] = StartCoroutine(CoDeactivateAfter(idx, onSeconds));
//         }

//         private IEnumerator CoDeactivateAfter(int idx, float onSeconds)
//         {
//             yield return new WaitForSeconds(onSeconds);

//             if (!_dpRunning)
//                 yield break;

//             // finalize CSV event before turning off (writes OFF row)
//             FinalizeAndLogEvent(idx);

//             AddActiveTimePartial(idx);
//             ForceOff(idx);

//             if (debugLogs && IsValid(idx))
//             {
//                 string label = $"D{idx + 1}";
//                 var e = entries[idx];
//                 Log($"OFF: {label} '{(e != null && e.obj ? e.obj.name : "NULL")}' (active={_activeCount})");
//             }
//         }

//         private void AddActiveTimePartial(int idx)
//         {
//             if (_activeStartRealtime == null || _activeSecondsAccum == null) return;
//             if (idx < 0 || idx >= _activeStartRealtime.Length) return;

//             float start = _activeStartRealtime[idx];
//             if (start <= 0f) return;

//             float now = Time.realtimeSinceStartup;
//             float dt = Mathf.Max(0f, now - start);

//             _activeSecondsAccum[idx] += dt;
//             _activeStartRealtime[idx] = 0f;
//         }

//         private void ForceOff(int idx)
//         {
//             if (!IsValid(idx)) return;

//             var e = entries[idx];
//             if (e == null || e.obj == null) return;

//             if (e.audioSource && e.audioSource.isPlaying) e.audioSource.Stop();

//             e.obj.SetActive(false);
//             e.running = false;

//             if (_activeIdx.Remove(idx))
//                 _activeCount = Mathf.Max(0, _activeCount - 1);

//             if (_offCos != null && idx >= 0 && idx < _offCos.Length)
//                 _offCos[idx] = null;
//         }

//         // ---------------- CSV snapshot helpers ----------------

//         private void CreatePendingEvent(int idx, long onsetMs, long dpElapsedOnsetMs, float plannedOnSeconds)
//         {
//             if (_pending == null || _pendingValid == null) return;
//             if (idx < 0 || idx >= _pending.Length) return;

//             string pid = (GameManager.Instance != null) ? (GameManager.Instance.ParticipantId ?? "") : "";
//             string sessionId = LoggingReport.CurrentSessionId ?? "";

//             string dId = $"D{idx + 1}";
//             string dName = (entries[idx] != null && entries[idx].obj != null) ? entries[idx].obj.name : dId;

//             float w = (float)GetWeight(idx);

//             _pending[idx] = new PendingEvent
//             {
//                 trialIndexAtOn = _curTrialIndex,
//                 phaseTrialIndexAtOn = _curPhaseTrialIndex,

//                 onsetMs = onsetMs,
//                 dpElapsedOnsetMs = dpElapsedOnsetMs,

//                 plannedDurationMs = Mathf.RoundToInt(plannedOnSeconds * 1000f),

//                 activeCountAtOnset = _activeCount,
//                 activeSetAtOnset = BuildActiveSetString(),

//                 distractorId = dId,
//                 distractorName = dName,
//                 weightTarget = w
//             };

//             _pendingValid[idx] = true;
//         }

//         private void FinalizeAndLogEvent(int idx)
//         {
//             if (_pending == null || _pendingValid == null) return;
//             if (idx < 0 || idx >= _pending.Length) return;
//             if (!_pendingValid[idx]) return;

//             var p = _pending[idx];

//             long offsetMs = NowMs();
//             long dpElapsedOffsetMs = offsetMs - _dpStartMs;

//             // actual duration from ms timebase
//             long onsetMs = _activeStartMs != null ? _activeStartMs[idx] : 0;
//             int actualMs = (onsetMs > 0) ? (int)Mathf.Max(0, (offsetMs - onsetMs)) : -1;

//             // write OFF row (Rule 3)
//             LoggingDistractors.AppendDistractorEvent(
//                 p,
//                 eventType: "OFF",
//                 offsetMs: offsetMs.ToString(CultureInfo.InvariantCulture),
//                 dpElapsedOffsetMs: dpElapsedOffsetMs.ToString(CultureInfo.InvariantCulture),
//                 actualDurationMs: (actualMs >= 0 ? actualMs.ToString(CultureInfo.InvariantCulture) : "")
//             );

//             _pendingValid[idx] = false;
//         }

//         private string BuildActiveSetString()
//         {
//             if (_activeIdx == null || _activeIdx.Count == 0) return "";

//             int[] arr = new int[_activeIdx.Count];
//             int p = 0;
//             foreach (var i in _activeIdx) arr[p++] = i;
//             Array.Sort(arr);

//             var sb = new StringBuilder(16);
//             for (int i = 0; i < arr.Length; i++)
//             {
//                 if (i > 0) sb.Append('|');
//                 sb.Append('D').Append(arr[i] + 1);
//             }
//             return sb.ToString();
//         }

//         // ---------------- Helpers ----------------

//         private void PrepareEntries()
//         {
//             for (int i = 0; i < entries.Count; i++)
//             {
//                 var e = entries[i];
//                 if (e == null || e.obj == null) continue;

//                 if (e.audioSource == null)
//                 {
//                     e.audioSource = e.obj.GetComponent<AudioSource>();
//                     if (e.audioSource == null) e.audioSource = e.obj.AddComponent<AudioSource>();
//                 }

//                 e.audioSource.playOnAwake = false;
//                 e.audioSource.loop = loopAudioWhileVisible;
//                 e.audioSource.volume = audioVolume;

//                 e.running = false;
//                 e.obj.SetActive(false);
//             }
//         }

//         private bool IsValid(int idx)
//         {
//             return idx >= 0 && idx < entries.Count && entries[idx] != null && entries[idx].obj != null;
//         }

//         private double GetWeight(int idx)
//         {
//             // inspector weight first
//             if (IsValid(idx))
//             {
//                 float wInspector = entries[idx].weight;
//                 if (wInspector > 0f) return wInspector;
//             }

//             // fallback defaults
//             if (idx < 0) return 0.0;
//             if (idx < WEIGHTS.Length) return WEIGHTS[idx];
//             return WEIGHTS[WEIGHTS.Length - 1];
//         }

//         private float NextOnDuration(System.Random rng)
//         {
//             float step = Mathf.Max(0.0001f, onStepSeconds);
//             float min = Mathf.Max(0f, minOnSeconds);
//             float max = Mathf.Max(min, maxOnSeconds);

//             int steps = Mathf.Max(0, Mathf.RoundToInt((max - min) / step));
//             int k = rng.Next(0, steps + 1);
//             return min + k * step;
//         }

//         private float NextOffDelay(System.Random rng)
//         {
//             float min = Mathf.Max(0f, minOffSeconds);
//             float max = Mathf.Max(min, maxOffSeconds);
//             return NextRange(rng, min, max);
//         }

//         private static float NextRange(System.Random rng, float min, float max)
//         {
//             if (max <= min) return min;
//             return (float)(min + (max - min) * rng.NextDouble());
//         }

//         private int GetOnlyActiveIndex()
//         {
//             if (_activeIdx.Count != 1) return -1;
//             foreach (var i in _activeIdx) return i;
//             return -1;
//         }

//         private int GetOtherActiveIndex(int except)
//         {
//             foreach (var i in _activeIdx)
//                 if (i != except) return i;
//             return -1;
//         }

//         private static void GetSortedPair(int a, int b, out int lo, out int hi)
//         {
//             if (a <= b) { lo = a; hi = b; }
//             else { lo = b; hi = a; }
//         }

//         private static int HashIslandId(string islandId)
//         {
//             if (string.IsNullOrWhiteSpace(islandId)) return 0;
//             unchecked
//             {
//                 int h = 23;
//                 string s = islandId.Trim().ToUpperInvariant();
//                 for (int i = 0; i < s.Length; i++)
//                     h = h * 31 + s[i];
//                 return h;
//             }
//         }

//         private static int CombineSeeds(int a, int b, int c)
//         {
//             unchecked
//             {
//                 int h = 17;
//                 h = h * 31 + a;
//                 h = h * 31 + b;
//                 h = h * 31 + c;
//                 return h;
//             }
//         }

//         // =====================================================================
//         //  LoggingDistractors (self-contained CSV writer)
//         // =====================================================================
//         private static class LoggingDistractors
//         {
//             private const string CSVSeperator = ",";
//             private static readonly string[] Headers = new[]
//             {
//                 "participant_id",
//                 "session_id",
//                 "island_id",
//                 "phase",
//                 "event_type",              // ON or OFF
//                 "trial_index",
//                 "phase_trial_index",
//                 "distractor_id",
//                 "distractor_name",
//                 "weight_target",
//                 "onset_ms",
//                 "offset_ms",
//                 "planned_duration_ms",
//                 "actual_duration_ms",
//                 "dp_elapsed_onset_ms",
//                 "dp_elapsed_offset_ms",
//                 "active_count_at_onset",
//                 "active_set_at_onset"
//             };

//             public static void CreateDistractorCSV()
//             {
//                 var path = GetCSVPath();
//                 EnsureDirectory(path);

//                 if (File.Exists(path)) return;

//                 using (var sw = File.CreateText(path))
//                     sw.WriteLine(string.Join(CSVSeperator, Headers));
//             }

//             public static void AppendDistractorEvent(
//                 PendingEvent p,
//                 string eventType,
//                 string offsetMs,
//                 string dpElapsedOffsetMs,
//                 string actualDurationMs
//             )
//             {
//                 var path = GetCSVPath();
//                 EnsureDirectory(path);

//                 if (!File.Exists(path))
//                     CreateDistractorCSV();

//                 var inv = CultureInfo.InvariantCulture;

//                 string pid = (GameManager.Instance != null) ? (GameManager.Instance.ParticipantId ?? "") : "";
//                 string sessionId = LoggingReport.CurrentSessionId ?? "";

//                 string trialIndex = (p.trialIndexAtOn >= 0) ? p.trialIndexAtOn.ToString(inv) : "";
//                 string phaseTrial = (p.phaseTrialIndexAtOn >= 0) ? p.phaseTrialIndexAtOn.ToString(inv) : "";

//                 // note: p.onsetMs and p.dpElapsedOnsetMs are always from the SAME timebase
//                 string onset = (p.onsetMs > 0) ? p.onsetMs.ToString(inv) : "";
//                 string dpOn = (p.dpElapsedOnsetMs >= 0) ? p.dpElapsedOnsetMs.ToString(inv) : "";

//                 string planned = (p.plannedDurationMs >= 0) ? p.plannedDurationMs.ToString(inv) : "";
//                 string w = p.weightTarget.ToString(inv);

//                 // island_id and phase come from outer scope; store in fields we pass (below)
//                 // We will embed island_id / phase as empty here; caller writes them via p.distractor fields.
//                 // Instead: caller already includes island/phase in its own state; easiest is to put them inside distractor_name? No.
//                 // So: we read island/phase from current DP state via a safe accessor.
//                 // However, this nested class can't see instance fields. We'll write island_id via a static cache.
//                 // Minimal: use island_id from a static set method.
//                 // To keep copy-paste simple, we store island_id/phase directly inside distractor_name? No.
//                 // Better: keep island_id blank here; BUT you asked to record island_id.
//                 // So we’ll use a static cache set by instance below.
//                 string islandId = _cachedIslandId;
//                 string phase = "DP";

//                 var row = string.Join(CSVSeperator, new[]
//                 {
//                     Escape(pid),
//                     Escape(sessionId),
//                     Escape(islandId),
//                     phase,
//                     eventType,
//                     trialIndex,
//                     phaseTrial,
//                     p.distractorId,
//                     Escape(p.distractorName),
//                     w,
//                     onset,
//                     offsetMs,
//                     planned,
//                     actualDurationMs,
//                     dpOn,
//                     dpElapsedOffsetMs,
//                     p.activeCountAtOnset.ToString(inv),
//                     Escape(p.activeSetAtOnset)
//                 });

//                 using (var sw = File.AppendText(path))
//                     sw.WriteLine(row);
//             }

//             // island id cache (set by instance on StartDP)
//             private static string _cachedIslandId = "";
//             public static void SetCachedIslandId(string islandId) => _cachedIslandId = islandId ?? "";

//             private static string GetCSVPath()
//             {
//                 string pid = (GameManager.Instance != null) ? (GameManager.Instance.ParticipantId ?? "") : "";
//                 if (string.IsNullOrWhiteSpace(pid))
//                     pid = "P_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

//                 string safePid = Sanitize(pid);

//                 var dir = Path.Combine(Environment.CurrentDirectory, "Assets", "Resources", "ParticipantData", "ReportData");
//                 return Path.Combine(dir, $"P_{safePid}_MoxoCPT_Distractors.csv");
//             }

//             private static void EnsureDirectory(string filePath)
//             {
//                 var dir = Path.GetDirectoryName(filePath);
//                 if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
//                     Directory.CreateDirectory(dir);
//             }

//             private static string Sanitize(string s)
//             {
//                 if (string.IsNullOrEmpty(s)) return "Unknown";
//                 foreach (var c in Path.GetInvalidFileNameChars())
//                     s = s.Replace(c, '_');
//                 return s.Trim();
//             }

//             private static string Escape(string s)
//             {
//                 if (string.IsNullOrEmpty(s)) return "";
//                 if (s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r"))
//                 {
//                     s = s.Replace("\"", "\"\"");
//                     return $"\"{s}\"";
//                 }
//                 return s;
//             }
//         }

//         // Make sure island id gets into the static logger cache on StartDP
//         private void OnValidate()
//         {
//             // no-op; just keeps Unity happy
//         }

//         // Hook: ensure island id is cached for CSV writing
//         private void LateUpdate()
//         {
//             // no-op
//         }

//         // Patch: set island id cache in StartDP after we assign _dpIslandId
//         // (We do it here so it also runs in editor if startImmediatelyForDebug is used.)
//         private void SetLoggerIslandCache()
//         {
//             LoggingDistractors.SetCachedIslandId(_dpIslandId);
//         }

//         // tiny adjustment: call SetLoggerIslandCache() at StartDP
//         // (Unity won’t let us “inject” this, so we call it explicitly here)
//         private void Start()
//         {
//             // no-op
//         }

//     }
// }
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace MoxoCPT
{
    /// <summary>
    /// DistractorSystem (DP-only).
    /// ChangeShapes should call:
    ///   - distractors.StartDP(islandId, runSeed);
    ///   - (each DP trial) distractors.SetTrialContext(trialIndex, phaseTrialIndex);
    ///   - distractors.StopDP();
    ///
    /// STRICT MODE (UPDATED TO YOUR NEW GOAL):
    /// - Exactly 6 distractors
    /// - Each distractor has EXACTLY equal total ON time across the DP
    /// - Individual ON segments are randomized between [minOnSeconds, maxOnSeconds] in onStepSeconds steps
    /// - Concurrency is always 1 or 2 (never 0, never >2)
    /// - No OFF gaps (minOffSeconds/maxOffSeconds forced to 0 in strict)
    /// - Schedule is precomputed and ends cleanly (no truncation)
    /// - islandId contributes to seed (counterbalanced per island)
    ///
    /// NOTE ON "STRICT":
    /// - Coroutine timing jitters, so "actual" ms is not perfectly exact in real time.
    /// - In strict mode we write OFF rows using the PLANNED offset and PLANNED duration
    ///   so CSV sums are perfectly equal (no drift).
    /// </summary>
    public class DistractorSystem : MonoBehaviour
    {
        [System.Serializable]
        public class Entry
        {
            public GameObject obj;
            public AudioClip clip;
            public AudioSource audioSource;
            [HideInInspector] public bool running;

            [Header("Scheduling")]
            [Min(0f)] public float weight = 0f; // kept for compatibility; strict mode ignores weights
        }

        [Header("Population")]
        [SerializeField] private bool autoFindByTag = true;
        [SerializeField] private string distractorTag = "Distractor";
        [SerializeField] private List<Entry> entries = new List<Entry>();

        [Header("Timing (Active ON)")]
        [SerializeField] private float minOnSeconds = 3.5f;
        [SerializeField] private float maxOnSeconds = 8.5f;
        [SerializeField] private float onStepSeconds = 0.5f;

        [Header("Timing (Idle/OFF between activations)")]
        [SerializeField] private float minOffSeconds = 0.0f;   // strict mode forces 0
        [SerializeField] private float maxOffSeconds = 0.0f;   // strict mode forces 0

        [Header("Concurrency")]
        [SerializeField] private bool allowOverlap = true;
        [SerializeField] private int maxSimultaneous = 2;

        [Header("Global audio settings")]
        [SerializeField] private bool loopAudioWhileVisible = true;
        [SerializeField] private float audioVolume = 1.0f;

        [Header("Debug / Safety")]
        [SerializeField] private bool debugLogs = true;

        // Kept for inspector compatibility, but intentionally ignored (DP must be explicitly started)
        [SerializeField] private bool autoStartWhenCPT = false; // DO NOT USE
        [SerializeField] private bool startImmediatelyForDebug = false;

        // =========================
        // STRICT SCHEDULING
        // =========================
        [Header("Strict Schedule (DP only)")]
        [Tooltip("If ON, uses a fully precomputed strict schedule: exact equal total time per distractor, 1..2 overlap, no gaps, clean end.")]
        [SerializeField] private bool strictExactSchedule = true;

        [Tooltip("DP duration used for scheduling (seconds). Overridden if StartDP(islandId,seed,dpSecondsOverride) is used.")]
        [SerializeField] private float dpDurationSeconds = 66f;

        // Kept for compatibility with your original script; NOT USED in updated strict mode.
        [Tooltip("Legacy field (ignored in UPDATED strict mode).")]
        [Range(1f, 2f)]
        [SerializeField] private float targetAvgConcurrency = 1.5f;

        // ---------------- DP runtime ----------------
        private bool _dpRunning = false;
        private Coroutine _dpCo;
        private System.Random _rng;

        // timebase (CSV)
        private long _dpStartMs;
        private float _dpStartRealtime;
        private string _dpIslandId = "";

        // trial context snapshot (set by ChangeShapes during DP)
        private int _curTrialIndex = -1;
        private int _curPhaseTrialIndex = -1;

        // active tracking
        private readonly HashSet<int> _activeIdx = new HashSet<int>();
        private float[] _activeStartRealtime;
        private long[] _activeStartMs;
        private double[] _activeSecondsAccum;

        private PendingEvent[] _pending;
        private bool[] _pendingValid;

        private struct PendingEvent
        {
            public int trialIndexAtOn;
            public int phaseTrialIndexAtOn;

            public long onsetMs;
            public long dpElapsedOnsetMs;

            public int plannedDurationMs;

            // strict OFF writing
            public long plannedOffsetMs;
            public long plannedDpElapsedOffsetMs;

            public int activeCountAtOnset;
            public string activeSetAtOnset;

            public string distractorId;
            public string distractorName;
            public float weightTarget; // in strict mode this will be equal share label
        }

        private void Log(string msg)
        {
            if (debugLogs) Debug.Log($"[Distractors] {msg}", this);
        }

        private static long NowMs()
            => (long)(Time.realtimeSinceStartup * 1000.0f);

        private void Awake()
        {
            if (autoFindByTag)
            {
                entries.Clear();
                var gos = GameObject.FindGameObjectsWithTag(distractorTag);
                foreach (var go in gos) entries.Add(new Entry { obj = go });
                Log($"Auto-found {entries.Count} distractors with tag '{distractorTag}'.");
            }

            if (autoStartWhenCPT)
            {
                autoStartWhenCPT = false;
                Log("autoStartWhenCPT was enabled, but is ignored/disabled to prevent NDP violations.");
            }

            foreach (var e in entries)
                if (e?.obj) e.obj.SetActive(false);

            if (startImmediatelyForDebug)
                StartDP("DEBUG_ISLAND", 12345);
        }

        // ---------------- Public API ----------------

        public void SetTrialContext(int trialIndex, int phaseTrialIndex)
        {
            _curTrialIndex = trialIndex;
            _curPhaseTrialIndex = phaseTrialIndex;
        }

        public void StartDP(string islandId, int seed, float dpSecondsOverride)
        {
            // Store the true DP duration coming from ChangeShapes
            dpDurationSeconds = Mathf.Max(0.001f, dpSecondsOverride);

            // In strict mode, dpDurationSeconds MUST be aligned to onStepSeconds
            if (strictExactSchedule)
                dpDurationSeconds = AlignDownToStep(dpDurationSeconds, onStepSeconds);

            StartDP(islandId, seed);
        }

        // Align DOWN so schedule never runs longer than the actual phase
        private static float AlignDownToStep(float seconds, float step)
        {
            step = Mathf.Max(0.0001f, step);
            if (seconds <= 0f) return 0f;

            int steps = Mathf.FloorToInt(seconds / step);
            return steps * step;
        }

        public void StartDP(string islandId, int seed)
        {
            if (_dpRunning) return;

            int assigned = 0;
            foreach (var e in entries) if (e != null && e.obj != null) assigned++;
            if (assigned == 0)
            {
                Debug.LogWarning("[Distractors] No valid entries. Turn on Auto Find By Tag or assign objects in Entries.", this);
                return;
            }

            // enforce DP constraints
            allowOverlap = true;
            maxSimultaneous = 2;

            // strict mode needs step-aligned duration (safety if StartDP(seed) called directly)
            if (strictExactSchedule)
                dpDurationSeconds = AlignDownToStep(dpDurationSeconds, onStepSeconds);

            // strict mode removes any OFF delay
            if (strictExactSchedule)
            {
                minOffSeconds = 0f;
                maxOffSeconds = 0f;
            }

            PrepareEntries();

            _dpRunning = true;
            _dpIslandId = islandId ?? "";
            LoggingDistractors.SetCachedIslandId(_dpIslandId);

            _dpStartMs = NowMs();
            _dpStartRealtime = Time.realtimeSinceStartup;

            LoggingDistractors.CreateDistractorCSV();

            int combinedSeed = CombineSeeds(seed, HashIslandId(islandId), unchecked((int)0xC0DEC0DE));
            _rng = new System.Random(combinedSeed);

            _activeIdx.Clear();

            _activeStartRealtime = new float[entries.Count];
            _activeStartMs = new long[entries.Count];
            _activeSecondsAccum = new double[entries.Count];

            _pending = new PendingEvent[entries.Count];
            _pendingValid = new bool[entries.Count];

            _curTrialIndex = -1;
            _curPhaseTrialIndex = -1;

            for (int i = 0; i < entries.Count; i++)
                ForceOff(i);

            if (_dpCo != null) StopCoroutine(_dpCo);

            _dpCo = strictExactSchedule
                ? StartCoroutine(CoRunDP_StrictExact(combinedSeed))   // ✅ name kept for compatibility
                : StartCoroutine(CoRunDP_LegacyWeighted());

            Log($"DP START (strict={strictExactSchedule}) islandId='{_dpIslandId}', seed={seed}, combinedSeed={combinedSeed}, entries={assigned}");
        }

        public void StopDP()
        {
            if (!_dpRunning) return;

            _dpRunning = false;

            if (_dpCo != null) StopCoroutine(_dpCo);
            _dpCo = null;

            // finalize any active events and turn off
            foreach (var idx in new List<int>(_activeIdx))
            {
                FinalizeAndLogEvent(idx);
                AddActiveTimePartial(idx);
                ForceOff(idx);
            }

            _activeIdx.Clear();

            Log("DP STOP.");
            PrintDPSummary();
        }

        // Legacy compatibility (fix your CS1061 errors)
        public void StopSystem() => StopDP();
        public void StartSystem()
        {
            var island = IslandTravelManager.I != null ? IslandTravelManager.I.CurrentIsland : null;
            string islandId = island != null ? island.islandId : "UNKNOWN_ISLAND";
            int seed = Environment.TickCount;
            Log("StartSystem() called. Mapping to StartDP() (DP-only).");
            StartDP(islandId, seed);
        }

        public void PrintDPSummary()
        {
            if (_activeSecondsAccum == null || _activeSecondsAccum.Length == 0)
            {
                Log("No DP summary available (DP not run yet).");
                return;
            }

            double dpElapsed = Math.Max(0.0001, Time.realtimeSinceStartup - _dpStartRealtime);

            Log("DP SUMMARY (active time seconds and achieved shares; shares can sum > 1 due to overlap):");
            for (int i = 0; i < entries.Count; i++)
            {
                string name = (entries[i] != null && entries[i].obj != null) ? entries[i].obj.name : $"D{i + 1}";
                double secs = _activeSecondsAccum[i];
                double achievedShare = secs / dpElapsed;
                Log($"  D{i + 1} '{name}': time={secs:0.00}s | achievedShare={achievedShare:0.000}");
            }
        }

        // ============================================================
        // STRICT SCHEDULER (UPDATED):
        // - equal total ON time per distractor
        // - randomized segment durations in [minOnSeconds,maxOnSeconds] step=onStepSeconds
        // - concurrency always 1 or 2
        // - no gaps
        // - clean end
        // ============================================================

        private struct ScheduledEvent
        {
            public float t;     // seconds from DP start
            public int idx;     // distractor index
            public bool on;     // true=ON, false=OFF
            public float dur;   // planned duration (only meaningful for ON events)
        }

        // ✅ NAME KEPT for drop-in replacement, but behavior updated to your new goal.
       // ✅ NAME KEPT for drop-in replacement, behavior = equal total time + random chunk durations.
        private IEnumerator CoRunDP_StrictExact(int combinedSeed)
        {
            int n = entries.Count;
            if (n != 6)
            {
                Debug.LogError($"[Distractors] Strict schedule requires exactly 6 distractors. Found {n}.", this);
                yield break;
            }

            float step = Mathf.Max(0.0001f, onStepSeconds);
            int T = Mathf.RoundToInt(dpDurationSeconds / step);
            if (!Mathf.Approximately(T * step, dpDurationSeconds))
            {
                Debug.LogError($"[Distractors] dpDurationSeconds must be a multiple of onStepSeconds. dp={dpDurationSeconds}, step={step}", this);
                yield break;
            }

            // Concurrency constraint: always 1..2
            allowOverlap = true;
            maxSimultaneous = 2;
            minOffSeconds = 0f;
            maxOffSeconds = 0f;

            // convert min/max to steps
            int minSteps = Mathf.RoundToInt(minOnSeconds / step);
            int maxSteps = Mathf.RoundToInt(maxOnSeconds / step);
            minSteps = Mathf.Max(1, minSteps);
            maxSteps = Mathf.Max(minSteps, maxSteps);

            // Target 50% overlap: extraSlots ≈ T/2.
            // extraSlots rounded DOWN to the nearest multiple of 6 so each distractor
            // gets an exactly equal laneB share (b[i] = extraSlots/6, integer).
            // totalSlots is then rounded UP to the nearest multiple of 6 for equal perSlots.
            // Example: T=132 (66s / 0.5s step) → rawExtra=66 → targetExtra=66
            //          → totalSlots=198 → extraSlots=66 → 66/132 = 50% overlap ✅
            int rawExtra = T / 2;
            int targetExtra = LargestMultipleAtMost(rawExtra, 6);
            if (targetExtra < 6) targetExtra = SmallestMultipleAtLeast(6, 6); // guarantee at least 1 overlap step per distractor
            int totalSlots = SmallestMultipleAtLeast(T + targetExtra, 6);

            if (totalSlots < T)
            {
                Debug.LogError($"[Distractors] DP too short to satisfy strict equal-time with 6 distractors under 1..2 concurrency. Tsteps={T}", this);
                yield break;
            }

            int extraSlots = totalSlots - T;     // laneB overlap steps
            int perSlots = totalSlots / 6;       // exact per distractor
            float perSeconds = perSlots * step;

            // Split overlap steps across distractors (b[i]) randomly but exact.
            int[] b = new int[6];
            int baseB = extraSlots / 6;
            int remB = extraSlots % 6;
            for (int i = 0; i < 6; i++) b[i] = baseB;

            var order = new List<int>(6) { 0, 1, 2, 3, 4, 5 };
            Shuffle(order, _rng);
            for (int k = 0; k < remB; k++) b[order[k]]++;

            int[] a = new int[6];
            int sumA = 0;
            for (int i = 0; i < 6; i++)
            {
                a[i] = perSlots - b[i];
                sumA += a[i];
            }

            if (sumA != T)
            {
                Debug.LogError($"[Distractors] Internal laneA sum mismatch: sumA={sumA} expected T={T}.", this);
                yield break;
            }

            // Feasibility: if any total is >0 but < minSteps, chunking cannot work
            for (int i = 0; i < 6; i++)
            {
                if (a[i] > 0 && a[i] < minSteps)
                {
                    Debug.LogError($"[Distractors] Infeasible: D{i + 1} laneA total {a[i] * step:0.00}s is < minOnSeconds {minOnSeconds:0.00}s. Increase DP or lower minOnSeconds.", this);
                    yield break;
                }
                if (b[i] > 0 && b[i] < minSteps)
                {
                    Debug.LogError($"[Distractors] Infeasible: D{i + 1} laneB total {b[i] * step:0.00}s is < minOnSeconds {minOnSeconds:0.00}s. Increase DP or lower minOnSeconds.", this);
                    yield break;
                }
            }

            // --- Planning (NO yields here) ---
            int[] laneA;
            int[] laneB;

            try
            {
                laneA = BuildLaneFromChunkPool(a, minSteps, maxSteps, T, _rng);

                laneB = new int[T];
                for (int t = 0; t < T; t++) laneB[t] = -1;

                bool[] used = new bool[T];
                int placed = 0;

                var laneBChunks = new List<(int idx, int len)>(128);
                for (int i = 0; i < 6; i++)
                    foreach (var len in SplitIntoChunks(b[i], minSteps, maxSteps, _rng))
                        laneBChunks.Add((i, len));

                Shuffle(laneBChunks, _rng);

                foreach (var ch in laneBChunks)
                {
                    if (ch.len <= 0) continue;

                    int start = FindWindowForLaneBChunk(laneA, used, ch.idx, ch.len, _rng);
                    if (start < 0)
                        throw new Exception($"Could not place overlap chunk for D{ch.idx + 1} (lenSteps={ch.len}).");

                    for (int t = start; t < start + ch.len; t++)
                    {
                        laneB[t] = ch.idx;
                        used[t] = true;
                        placed++;
                    }
                }

                if (placed != extraSlots)
                    throw new Exception($"LaneB placement mismatch: placed={placed} expected extraSlots={extraSlots}.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Distractors] Strict scheduler planning failed: {ex.Message}", this);
                yield break;
            }

            var events = BuildEventsFromLanes(laneA, laneB, step);

            Log($"STRICT EQUAL-RANDOM PLAN: dp={dpDurationSeconds:0.00}s step={step:0.00}s");
            Log($"  Tsteps={T}, totalSlots={totalSlots}, extraSlots={extraSlots}, perDistractor={perSeconds:0.00}s");

            // --- Execution (yields allowed, not inside try/catch) ---
            float dpEnd = dpDurationSeconds;
            float t0 = Time.realtimeSinceStartup;

            int eIdx = 0;
            while (_dpRunning && eIdx < events.Count)
            {
                float now = Time.realtimeSinceStartup - t0;
                float wait = events[eIdx].t - now;
                if (wait > 0f) yield return new WaitForSecondsRealtime(wait);
                if (!_dpRunning) yield break;

                var ev = events[eIdx++];
                if (ev.on) ActivatePlanned(ev.idx, ev.dur);
                else DeactivatePlanned(ev.idx);
            }

            float remaining = dpEnd - (Time.realtimeSinceStartup - t0);
            if (remaining > 0f) yield return new WaitForSecondsRealtime(remaining);

            StopDP();
        }

        // ---------- STRICT helper methods (NEW) ----------

        private static int SmallestMultipleAtLeast(int x, int m)
        {
            if (m <= 0) return x;
            int r = x % m;
            return (r == 0) ? x : (x + (m - r));
        }

        private static int LargestMultipleAtMost(int x, int m)
        {
            if (m <= 0) return x;
            return x - (x % m);
        }

        // Split totalSteps into random chunk lengths within [minSteps, maxSteps] summing EXACTLY to totalSteps.
        // Invariant: each iteration either finishes (remaining == 0) or leaves remaining >= minSteps,
        // so no "infeasible tail" can ever be created.
        private static List<int> SplitIntoChunks(int totalSteps, int minSteps, int maxSteps, System.Random rng)
        {
            var chunks = new List<int>(16);
            if (totalSteps <= 0) return chunks;

            if (totalSteps < minSteps)
                throw new Exception($"SplitIntoChunks infeasible: totalSteps={totalSteps} < minSteps={minSteps}.");

            int remaining = totalSteps;

            while (remaining > 0)
            {
                // If remaining fits in one valid chunk, consume it all and finish.
                if (remaining <= maxSteps)
                {
                    chunks.Add(remaining);
                    break;
                }

                // Pick len in [minSteps, min(maxSteps, remaining-minSteps)].
                // Upper bound ensures remaining-len >= minSteps so the next iteration is always valid.
                int lo = minSteps;
                int hi = Mathf.Min(maxSteps, remaining - minSteps);

                if (hi < lo)
                {
                    // Only reachable when maxSteps < 2*minSteps AND remaining > maxSteps.
                    // With minSteps=7, maxSteps=17 this cannot happen (17 >= 14).
                    // Fallback: absorb all remaining into one over-sized chunk to avoid infinite loop.
                    chunks.Add(remaining);
                    break;
                }

                int len = rng.Next(lo, hi + 1);
                chunks.Add(len);
                remaining -= len;
            }

            // Randomize chunk order (totals stay exact, segment sequence is randomised)
            for (int i = 0; i < chunks.Count; i++)
            {
                int j = rng.Next(i, chunks.Count);
                (chunks[i], chunks[j]) = (chunks[j], chunks[i]);
            }

            return chunks;
        }

        // Build lane by pooling chunks across distractors and laying them sequentially to fill exactly T steps.
        // The pool is shuffled with a "no consecutive same-distractor" constraint so that
        // BuildEventsFromLanes never merges two chunks into a single ON event that exceeds maxOnSeconds.
        private static int[] BuildLaneFromChunkPool(int[] stepsPerDistractor, int minSteps, int maxSteps, int T, System.Random rng)
        {
            var pool = new List<(int idx, int len)>(128);

            for (int i = 0; i < 6; i++)
            {
                var chunks = SplitIntoChunks(stepsPerDistractor[i], minSteps, maxSteps, rng);
                foreach (var len in chunks)
                    pool.Add((i, len));
            }

            // Shuffle, then retry until no two consecutive entries share the same distractor index.
            // This prevents BuildEventsFromLanes from merging adjacent same-distractor chunks into
            // a single ON segment that would exceed maxOnSeconds.
            const int maxShuffleAttempts = 200;
            for (int attempt = 0; attempt < maxShuffleAttempts; attempt++)
            {
                for (int i = 0; i < pool.Count; i++)
                {
                    int j = rng.Next(i, pool.Count);
                    (pool[i], pool[j]) = (pool[j], pool[i]);
                }

                bool hasAdjacentSame = false;
                for (int i = 1; i < pool.Count; i++)
                {
                    if (pool[i].idx == pool[i - 1].idx) { hasAdjacentSame = true; break; }
                }

                if (!hasAdjacentSame) break;
                // If still not valid after all attempts, proceed anyway (best-effort).
            }

            int[] lane = new int[T];
            int cursor = 0;

            foreach (var seg in pool)
            {
                for (int k = 0; k < seg.len; k++)
                {
                    if (cursor >= T) break;
                    lane[cursor++] = seg.idx;
                }
                if (cursor >= T) break;
            }

            if (cursor != T)
                throw new Exception($"BuildLaneFromChunkPool failed: cursor={cursor} expected={T}.");

            return lane;
        }

        // Find a window for laneB chunk where:
        // - unused (no existing laneB)
        // - laneA[t] != d (avoid same distractor overlapping itself)
        private static int FindWindowForLaneBChunk(int[] laneA, bool[] used, int d, int len, System.Random rng)
        {
            int T = laneA.Length;
            int tries = Math.Max(120, T * 3);

            for (int k = 0; k < tries; k++)
            {
                int start = rng.Next(0, T - len + 1);
                if (WindowFitsLaneB(laneA, used, d, start, len)) return start;
            }

            for (int start = 0; start <= T - len; start++)
                if (WindowFitsLaneB(laneA, used, d, start, len)) return start;

            return -1;
        }

        private static bool WindowFitsLaneB(int[] laneA, bool[] used, int d, int start, int len)
        {
            for (int t = start; t < start + len; t++)
            {
                if (used[t]) return false;
                if (laneA[t] == d) return false;
            }
            return true;
        }

        private static List<ScheduledEvent> BuildEventsFromLanes(int[] laneA, int[] laneB, float step)
        {
            int T = laneA.Length;
            int n = 6;

            bool[] active = new bool[n];
            bool IsOnAt(int t, int d) => (laneA[t] == d) || (laneB[t] == d);

            var evs = new List<ScheduledEvent>(256);

            for (int t = 0; t <= T; t++)
            {
                float time = t * step;

                for (int d = 0; d < n; d++)
                {
                    bool shouldBeOn = (t < T) && IsOnAt(t, d);

                    if (!active[d] && shouldBeOn)
                    {
                        int t2 = t;
                        while (t2 < T && IsOnAt(t2, d)) t2++;
                        float dur = (t2 - t) * step;

                        evs.Add(new ScheduledEvent { t = time, idx = d, on = true, dur = dur });
                        active[d] = true;
                    }
                    else if (active[d] && !shouldBeOn)
                    {
                        evs.Add(new ScheduledEvent { t = time, idx = d, on = false, dur = 0f });
                        active[d] = false;
                    }
                }
            }

            // OFF before ON at the same timestamp: when lane-A switches from D_old to D_new
            // at step boundary, D_old-OFF and D_new-ON share the same time. If we fired
            // D_new-ON first while a laneB distractor was still active (count=2), the guard
            // `_activeIdx.Count >= 2` would silently drop D_new-ON. Firing OFFs first ensures
            // a free slot exists before any new activation at the same time.
            evs.Sort((a, b) =>
            {
                int c = a.t.CompareTo(b.t);
                if (c != 0) return c;
                if (a.on == b.on) return 0;
                return a.on ? 1 : -1;  // OFF before ON at same time
            });

            return evs;
        }

        private void ActivatePlanned(int idx, float onSeconds)
        {
            if (!_dpRunning) return;
            if (!IsValid(idx)) return;
            if (_activeIdx.Contains(idx)) return;

            if (_activeIdx.Count >= 2) return;

            var e = entries[idx];

            _activeIdx.Add(idx);
            e.running = true;

            long onsetMs = NowMs();
            long dpElapsedOnsetMs = onsetMs - _dpStartMs;

            e.obj.SetActive(true);

            _activeStartRealtime[idx] = Time.realtimeSinceStartup;
            _activeStartMs[idx] = onsetMs;

            float equalShare = 1f / Mathf.Max(1, entries.Count);

            CreatePendingEvent(idx, onsetMs, dpElapsedOnsetMs, onSeconds, equalShare);

            // ON row
            LoggingDistractors.AppendDistractorEvent(_pending[idx], eventType: "ON", offsetMs: "", dpElapsedOffsetMs: "", actualDurationMs: "");

            if (debugLogs)
                Log($"ON: D{idx + 1} '{e.obj.name}' for {onSeconds:0.0}s (active={_activeIdx.Count}) trial={_pending[idx].trialIndexAtOn}/{_pending[idx].phaseTrialIndexAtOn}");

            if (e.clip != null && e.audioSource != null)
            {
                e.audioSource.clip = e.clip;
                e.audioSource.loop = loopAudioWhileVisible;
                e.audioSource.volume = audioVolume;
                e.audioSource.Play();
            }
        }

        private void DeactivatePlanned(int idx)
        {
            if (!IsValid(idx)) return;
            if (!_activeIdx.Contains(idx)) return;

            FinalizeAndLogEvent(idx);
            AddActiveTimePartial(idx);
            ForceOff(idx);

            if (debugLogs && IsValid(idx))
            {
                var e = entries[idx];
                Log($"OFF: D{idx + 1} '{(e != null && e.obj ? e.obj.name : "NULL")}' (active={_activeIdx.Count})");
            }
        }

        // ============================================================
        // LEGACY WEIGHTED LOOP (kept in case you toggle strict OFF)
        // ============================================================

        private IEnumerator CoRunDP_LegacyWeighted()
        {
            while (_dpRunning)
            {
                float off = NextOffDelay(_rng);
                if (off > 0f) yield return new WaitForSecondsRealtime(off);
                if (!_dpRunning) yield break;

                if (maxSimultaneous > 0 && _activeIdx.Count >= maxSimultaneous)
                {
                    yield return new WaitUntil(() => !_dpRunning || _activeIdx.Count < maxSimultaneous);
                    if (!_dpRunning) yield break;
                }

                int idx = PickNextIndexWeightedQuota_Legacy();
                if (idx < 0) continue;

                float on = NextOnDuration(_rng);
                ActivateLegacy(idx, on);
            }
        }

        private int PickNextIndexWeightedQuota_Legacy()
        {
            var candidates = new List<int>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
                if (IsValid(i) && !_activeIdx.Contains(i))
                    candidates.Add(i);

            if (candidates.Count == 0) return -1;
            return candidates[_rng.Next(0, candidates.Count)];
        }

        private void ActivateLegacy(int idx, float onSeconds)
        {
            if (!IsValid(idx)) return;
            if (_activeIdx.Contains(idx)) return;
            if (!_dpRunning) return;
            if (_activeIdx.Count >= 2) return;

            var e = entries[idx];

            _activeIdx.Add(idx);
            e.running = true;

            long onsetMs = NowMs();
            long dpElapsedOnsetMs = onsetMs - _dpStartMs;

            e.obj.SetActive(true);
            _activeStartRealtime[idx] = Time.realtimeSinceStartup;
            _activeStartMs[idx] = onsetMs;

            CreatePendingEvent(idx, onsetMs, dpElapsedOnsetMs, onSeconds, 0.25f);
            LoggingDistractors.AppendDistractorEvent(_pending[idx], eventType: "ON", offsetMs: "", dpElapsedOffsetMs: "", actualDurationMs: "");

            if (e.clip != null && e.audioSource != null)
            {
                e.audioSource.clip = e.clip;
                e.audioSource.loop = loopAudioWhileVisible;
                e.audioSource.volume = audioVolume;
                e.audioSource.Play();
            }

            StartCoroutine(CoDeactivateAfterLegacy(idx, onSeconds));
        }

        private IEnumerator CoDeactivateAfterLegacy(int idx, float onSeconds)
        {
            yield return new WaitForSecondsRealtime(onSeconds);
            if (!_dpRunning) yield break;

            FinalizeAndLogEvent(idx);
            AddActiveTimePartial(idx);
            ForceOff(idx);
        }

        // ---------------- Core bookkeeping / CSV ----------------

        private void CreatePendingEvent(int idx, long onsetMs, long dpElapsedOnsetMs, float plannedOnSeconds, float weightLabel)
        {
            if (_pending == null || _pendingValid == null) return;
            if (idx < 0 || idx >= _pending.Length) return;

            string dId = $"D{idx + 1}";
            string dName = (entries[idx] != null && entries[idx].obj != null) ? entries[idx].obj.name : dId;

            int plannedMs = Mathf.RoundToInt(plannedOnSeconds * 1000f);

            // planned offset computed from onset + planned duration
            long plannedOffsetMs = onsetMs + plannedMs;
            long plannedDpElapsedOffsetMs = plannedOffsetMs - _dpStartMs;

            _pending[idx] = new PendingEvent
            {
                trialIndexAtOn = _curTrialIndex,
                phaseTrialIndexAtOn = _curPhaseTrialIndex,

                onsetMs = onsetMs,
                dpElapsedOnsetMs = dpElapsedOnsetMs,

                plannedDurationMs = plannedMs,

                plannedOffsetMs = plannedOffsetMs,
                plannedDpElapsedOffsetMs = plannedDpElapsedOffsetMs,

                activeCountAtOnset = _activeIdx.Count,
                activeSetAtOnset = BuildActiveSetString(),

                distractorId = dId,
                distractorName = dName,
                weightTarget = weightLabel
            };

            _pendingValid[idx] = true;
        }

        private void FinalizeAndLogEvent(int idx)
        {
            if (_pending == null || _pendingValid == null) return;
            if (idx < 0 || idx >= _pending.Length) return;
            if (!_pendingValid[idx]) return;

            var p = _pending[idx];

            if (strictExactSchedule)
            {
                // STRICT: write exact planned values so CSV totals are perfectly equal (no jitter).
                LoggingDistractors.AppendDistractorEvent(
                    p,
                    eventType: "OFF",
                    offsetMs: p.plannedOffsetMs.ToString(CultureInfo.InvariantCulture),
                    dpElapsedOffsetMs: p.plannedDpElapsedOffsetMs.ToString(CultureInfo.InvariantCulture),
                    actualDurationMs: p.plannedDurationMs.ToString(CultureInfo.InvariantCulture)
                );
            }
            else
            {
                long offsetMs = NowMs();
                long dpElapsedOffsetMs = offsetMs - _dpStartMs;

                long onsetMs = _activeStartMs != null ? _activeStartMs[idx] : 0;
                int actualMs = (onsetMs > 0) ? (int)Mathf.Max(0, (offsetMs - onsetMs)) : -1;

                LoggingDistractors.AppendDistractorEvent(
                    p,
                    eventType: "OFF",
                    offsetMs: offsetMs.ToString(CultureInfo.InvariantCulture),
                    dpElapsedOffsetMs: dpElapsedOffsetMs.ToString(CultureInfo.InvariantCulture),
                    actualDurationMs: (actualMs >= 0 ? actualMs.ToString(CultureInfo.InvariantCulture) : "")
                );
            }

            _pendingValid[idx] = false;
        }

        private string BuildActiveSetString()
        {
            if (_activeIdx == null || _activeIdx.Count == 0) return "";

            int[] arr = new int[_activeIdx.Count];
            int p = 0;
            foreach (var i in _activeIdx) arr[p++] = i;
            Array.Sort(arr);

            var sb = new StringBuilder(16);
            for (int i = 0; i < arr.Length; i++)
            {
                if (i > 0) sb.Append('|');
                sb.Append('D').Append(arr[i] + 1);
            }
            return sb.ToString();
        }

        private void AddActiveTimePartial(int idx)
        {
            if (_activeStartRealtime == null || _activeSecondsAccum == null) return;
            if (idx < 0 || idx >= _activeStartRealtime.Length) return;

            float start = _activeStartRealtime[idx];
            if (start <= 0f) return;

            float now = Time.realtimeSinceStartup;
            float dt = Mathf.Max(0f, now - start);

            _activeSecondsAccum[idx] += dt;
            _activeStartRealtime[idx] = 0f;
        }

        private void ForceOff(int idx)
        {
            if (!IsValid(idx)) return;

            var e = entries[idx];
            if (e == null || e.obj == null) return;

            if (e.audioSource && e.audioSource.isPlaying) e.audioSource.Stop();

            e.obj.SetActive(false);
            e.running = false;

            _activeIdx.Remove(idx);
        }

        private void PrepareEntries()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null || e.obj == null) continue;

                if (e.audioSource == null)
                {
                    e.audioSource = e.obj.GetComponent<AudioSource>();
                    if (e.audioSource == null) e.audioSource = e.obj.AddComponent<AudioSource>();
                }

                e.audioSource.playOnAwake = false;
                e.audioSource.loop = loopAudioWhileVisible;
                e.audioSource.volume = audioVolume;

                e.running = false;
                e.obj.SetActive(false);
            }
        }

        private bool IsValid(int idx)
        {
            return idx >= 0 && idx < entries.Count && entries[idx] != null && entries[idx].obj != null;
        }

        private float NextOnDuration(System.Random rng)
        {
            float step = Mathf.Max(0.0001f, onStepSeconds);
            float min = Mathf.Max(0f, minOnSeconds);
            float max = Mathf.Max(min, maxOnSeconds);

            int steps = Mathf.Max(0, Mathf.RoundToInt((max - min) / step));
            int k = rng.Next(0, steps + 1);
            return min + k * step;
        }

        private float NextOffDelay(System.Random rng)
        {
            float min = Mathf.Max(0f, minOffSeconds);
            float max = Mathf.Max(min, maxOffSeconds);
            return NextRange(rng, min, max);
        }

        private static float NextRange(System.Random rng, float min, float max)
        {
            if (max <= min) return min;
            return (float)(min + (max - min) * rng.NextDouble());
        }

        private static void Shuffle<T>(IList<T> list, System.Random rng)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int j = rng.Next(i, list.Count);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static int HashIslandId(string islandId)
        {
            if (string.IsNullOrWhiteSpace(islandId)) return 0;
            unchecked
            {
                int h = 23;
                string s = islandId.Trim().ToUpperInvariant();
                for (int i = 0; i < s.Length; i++)
                    h = h * 31 + s[i];
                return h;
            }
        }

        private static int CombineSeeds(int a, int b, int c)
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + a;
                h = h * 31 + b;
                h = h * 31 + c;
                return h;
            }
        }

        // =====================================================================
        // LoggingDistractors (self-contained CSV writer)
        // =====================================================================
        private static class LoggingDistractors
        {
#if UNITY_EDITOR
            private const string CSVSeperator = ",";
            private static readonly string[] Headers = new[]
            {
                "participant_id",
                "session_id",
                "island_id",
                "phase",
                "event_type",
                "trial_index",
                "phase_trial_index",
                "distractor_id",
                "distractor_name",
                "weight_target",
                "onset_ms",
                "offset_ms",
                "planned_duration_ms",
                "actual_duration_ms",
                "dp_elapsed_onset_ms",
                "dp_elapsed_offset_ms",
                "active_count_at_onset",
                "active_set_at_onset"
            };
#endif

            public static void CreateDistractorCSV()
            {
#if UNITY_EDITOR
                var path = GetCSVPath();
                EnsureDirectory(path);
                if (File.Exists(path)) return;
                using (var sw = File.CreateText(path))
                    sw.WriteLine(string.Join(CSVSeperator, Headers));
#endif
            }

            public static void AppendDistractorEvent(
                PendingEvent p,
                string eventType,
                string offsetMs,
                string dpElapsedOffsetMs,
                string actualDurationMs
            )
            {
                var inv = CultureInfo.InvariantCulture;

                string pid       = (GameManager.Instance != null) ? (GameManager.Instance.ParticipantId ?? "") : "";
                string sessionId = LoggingReport.CurrentSessionId ?? "";
                string islandId  = _cachedIslandId;
                const string phase = "DP";

#if UNITY_EDITOR
                var path = GetCSVPath();
                EnsureDirectory(path);

                if (!File.Exists(path))
                    CreateDistractorCSV();

                string trialIndex = (p.trialIndexAtOn >= 0) ? p.trialIndexAtOn.ToString(inv) : "";
                string phaseTrial = (p.phaseTrialIndexAtOn >= 0) ? p.phaseTrialIndexAtOn.ToString(inv) : "";
                string onset      = (p.onsetMs > 0) ? p.onsetMs.ToString(inv) : "";
                string dpOn       = (p.dpElapsedOnsetMs >= 0) ? p.dpElapsedOnsetMs.ToString(inv) : "";
                string planned    = (p.plannedDurationMs >= 0) ? p.plannedDurationMs.ToString(inv) : "";
                string w          = p.weightTarget.ToString(inv);

                var row = string.Join(CSVSeperator, new[]
                {
                    Escape(pid),
                    Escape(sessionId),
                    Escape(islandId),
                    phase,
                    eventType,
                    trialIndex,
                    phaseTrial,
                    p.distractorId,
                    Escape(p.distractorName),
                    w,
                    onset,
                    offsetMs,
                    planned,
                    actualDurationMs,
                    dpOn,
                    dpElapsedOffsetMs,
                    p.activeCountAtOnset.ToString(inv),
                    Escape(p.activeSetAtOnset)
                });

                using (var sw = File.AppendText(path))
                    sw.WriteLine(row);
#endif

                UploadToFirebase(p, eventType, offsetMs, dpElapsedOffsetMs,
                                 actualDurationMs, pid, sessionId, islandId, phase, inv);
            }

            private static string _cachedIslandId = "";
            public static void SetCachedIslandId(string islandId) => _cachedIslandId = islandId ?? "";

#if UNITY_EDITOR
            private static string GetCSVPath()
            {
                string pid = (GameManager.Instance != null) ? (GameManager.Instance.ParticipantId ?? "") : "";
                if (string.IsNullOrWhiteSpace(pid))
                    pid = "P_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

                string safePid = Sanitize(pid);
                var dir = Path.Combine(Environment.CurrentDirectory, "Assets", "Resources", "ParticipantData", "ReportData");
                return Path.Combine(dir, $"P_{safePid}_MoxoCPT_Distractors.csv");
            }

            private static void EnsureDirectory(string filePath)
            {
                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }

            private static string Sanitize(string s)
            {
                if (string.IsNullOrEmpty(s)) return "Unknown";
                foreach (var c in Path.GetInvalidFileNameChars())
                    s = s.Replace(c, '_');
                return s.Trim();
            }

            private static string Escape(string s)
            {
                if (string.IsNullOrEmpty(s)) return "";
                if (s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r"))
                {
                    s = s.Replace("\"", "\"\"");
                    return $"\"{s}\"";
                }
                return s;
            }
#endif

            private static void UploadToFirebase(
                PendingEvent p,
                string eventType,
                string offsetMs,
                string dpElapsedOffsetMs,
                string actualDurationMs,
                string pid,
                string sessionId,
                string islandId,
                string phase,
                CultureInfo inv)
            {
                var svc = FirebaseService.Instance;
                if (svc == null) return;

                var safePid = FirebaseService.SanitizeKey(
                    string.IsNullOrWhiteSpace(pid) ? "unknown" : pid);
                var safeSid = FirebaseService.SanitizeKey(sessionId);
                var path    = $"umaki/distractor_events/{safePid}/{safeSid}";

                long.TryParse(offsetMs,          out long offsetMsL);
                long.TryParse(dpElapsedOffsetMs, out long dpOffL);
                int.TryParse(actualDurationMs,   out int  actualMs);

                var sb = new System.Text.StringBuilder(512);
                sb.Append(FirebaseService.JS("participant_id",         pid));
                sb.Append(FirebaseService.JS("session_id",             sessionId));
                sb.Append(FirebaseService.JS("island_id",              islandId));
                sb.Append(FirebaseService.JS("phase",                  phase));
                sb.Append(FirebaseService.JS("event_type",             eventType));
                sb.Append(FirebaseService.JN("trial_index",            p.trialIndexAtOn));
                sb.Append(FirebaseService.JN("phase_trial_index",      p.phaseTrialIndexAtOn));
                sb.Append(FirebaseService.JS("distractor_id",          p.distractorId));
                sb.Append(FirebaseService.JS("distractor_name",        p.distractorName));
                sb.Append(FirebaseService.JN("weight_target",          p.weightTarget));
                sb.Append(FirebaseService.JN("onset_ms",               p.onsetMs));
                sb.Append(FirebaseService.JN("offset_ms",              offsetMsL));
                sb.Append(FirebaseService.JN("planned_duration_ms",    p.plannedDurationMs));
                sb.Append(FirebaseService.JN("actual_duration_ms",     actualMs));
                sb.Append(FirebaseService.JN("dp_elapsed_onset_ms",    p.dpElapsedOnsetMs));
                sb.Append(FirebaseService.JN("dp_elapsed_offset_ms",   dpOffL));
                sb.Append(FirebaseService.JN("active_count_at_onset",  p.activeCountAtOnset));
                sb.Append(FirebaseService.JS("active_set_at_onset",    p.activeSetAtOnset));

                svc.PostJson(path, FirebaseService.WrapJson(sb.ToString()));
            }
        }
    }
}