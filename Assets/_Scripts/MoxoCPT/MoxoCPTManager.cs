// using System;
// using System.Collections;
// using System.Linq;
// using System.Reflection;
// using UnityEngine;

// namespace MoxoCPT
// {
//     public class MoxoCPTManager : MonoBehaviour
//     {
//         public static MoxoCPTManager Instance;

//         [HideInInspector] public bool isGameOver;

//         [Header("Optional Systems")]
//         [SerializeField] private DistractorSystem distractors;
//         [SerializeField] private EyeTrackLogger eyeLogger;

//         private bool _running = false;     // prevents double starts
//         private bool _eyeLogging = false;  // guards eye logger sessions

//         // --- Robust singleton lifecycle ---
//         private void Awake()
//         {
//             if (Instance == null)
//             {
//                 Instance = this;
//                 DontDestroyOnLoad(gameObject);
//             }
//             else if (Instance != this)
//             {
//                 Destroy(gameObject);
//                 return;
//             }
//         }

//         private void OnEnable()
//         {
//             // if this rig is toggled on for a new island, make sure we become the active singleton
//             Instance = this;
//             InitializeIfNeeded();
//         }

//         private void OnDisable()
//         {
//             if (Instance == this) Instance = null;
//         }

//         /// <summary>Optional late init hook if your rig needs it after being enabled.</summary>
//         public void InitializeIfNeeded()
//         {
//             // Add any one-time lazy wiring you need here (left empty intentionally).
//         }

//         // ---------- External helpers ----------
//         /// <summary>
//         /// Hard end the run (if any), stop subsystems, normalize timeScale, and clear card activity.
//         /// Safe to call repeatedly.
//         /// </summary>
//         public void ForceEndAndCleanup()
//         {
//             try { StopAllCoroutines(); } catch { /* ignore */ }

//             // normalize time so countdowns don’t freeze
//             if (Time.timeScale != 1f) Time.timeScale = 1f;

//             // stop optional systems
//             distractors?.StopSystem();

//             // stop eyelogger if active
//             if (_eyeLogging) { (eyeLogger ?? EyeTrackLogger.I)?.EndSession(); _eyeLogging = false; }

//             // make sure cards are OFF
//             SetCardsActiveSafe(false);

//             // mark state
//             _running = false;
//             isGameOver = true;

//             // switch to a neutral state (island travel will set PrepareCPT)
//             GameManager.Instance?.UpdateGameState(GameManager.GameState.Explore);
//         }

//         /// <summary>
//         /// Reset score/runtime and (optionally) re-arm card decks before a new island.
//         /// Call this right before you intend to start the next countdown.
//         /// </summary>
//         public void PrepareForNewIsland(string islandId = null)
//         {
//             // Score/runtime fresh
//             CPTScoreRuntime.I?.ResetScore();

//             // If your Cards system exposes any reset/prepare API, call it via reflection safely.
//             var cards = GetComponent("CardsActive");
//             if (cards != null)
//             {
//                 var t = cards.GetType();
//                 string[] resetMethods = { "ResetCards", "ResetDeck", "PrepareForIsland", "ClearAndLoadTargets", "InitForIsland" };

//                 foreach (var name in resetMethods)
//                 {
//                     var m = t.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
//                     if (m == null) continue;

//                     var ps = m.GetParameters();
//                     try
//                     {
//                         if (ps.Length == 0)
//                         {
//                             m.Invoke(cards, null);
//                             Debug.Log($"[MOXO] CardsActive.{name}()");
//                             break;
//                         }
//                         else if (ps.Length == 1 && ps[0].ParameterType == typeof(string))
//                         {
//                             m.Invoke(cards, new object[] { islandId ?? "" });
//                             Debug.Log($"[MOXO] CardsActive.{name}(\"{islandId}\")");
//                             break;
//                         }
//                     }
//                     catch (Exception e) { Debug.LogWarning($"[MOXO] CardsActive.{name} threw: {e.Message}"); }
//                 }
//             }
//         }

//         // ---------- Game flow ----------
//         public void OnGameBegin()
//         {
//             // Prevent overlapping runs
//             if (_running)
//             {
//                 Debug.LogWarning("[MoxoCPTManager] OnGameBegin called while already running. Ignoring.");
//                 return;
//             }

//             // Hard normalize + clear any leftovers from prior island
//             ForceEndAndCleanup();

//             // Re-arm runtime
//             isGameOver = false;
//             _running = true;

//             // Fresh score
//             CPTScoreRuntime.I?.ResetScore();

//             // Make sure cards are ON now
//             SetCardsActiveSafe(true);

//             // Logging file for this run
//             LoggingReport.CreateReportCSV();

//             // Game state → CPT
//             if (GameManager.Instance != null)
//             {
//                 GameManager.Instance.UpdateGameState(GameManager.GameState.CPT);
//                 Debug.Log("[MOXO] GameState set to CPT.");
//             }
//             else
//             {
//                 Debug.LogError("[MoxoCPTManager] Cannot set state: GameManager.Instance is null.");
//             }

//             // Animation: begin cheering
//             KoalaAnimBus.BroadcastToActiveKoalas(d => d.OnGameBegin());


//             // Eye logger session
//             var pid = GameManager.Instance != null ? GameManager.Instance.ParticipantId : "";
//             if (string.IsNullOrWhiteSpace(pid))
//             {
//                 Debug.LogWarning("[MOXO] ParticipantId is empty; using timestamp fallback.");
//                 pid = "P_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
//             }

//             if (eyeLogger == null) eyeLogger = EyeTrackLogger.I ?? FindObjectOfType<EyeTrackLogger>(true);
//             Debug.Log($"[MOXO] EyeLogger = {(eyeLogger ? "OK" : "MISSING")}");
//             if (eyeLogger && !_eyeLogging)
//             {
//                 eyeLogger.BeginSession(pid);
//                 _eyeLogging = true;
//             }

//             // NEW: Ensure the active island's ChangeShapes actually starts.
//             NudgeActiveRunnerStart();
//         }

//         public void OnGameEnd()
//         {
            
//             // Animation: stop cheering, stand up
//             KoalaAnimBus.BroadcastToActiveKoalas(d => d.OnGameEnd());

            
//             if (!_running && isGameOver)
//             {
//                 // already ended; avoid double UI transitions
//                 Debug.Log("[MOXO] OnGameEnd called but not running; ignoring duplicate.");
//                 return;
//             }

//             Debug.Log("[MOXO] OnGameEnd()");
//             _running = false;
//             isGameOver = true;

//             // stop optional systems
//             distractors?.StopSystem();

//             if (_eyeLogging) { (eyeLogger ?? EyeTrackLogger.I)?.EndSession(); _eyeLogging = false; }

//             // Freeze gameplay layer now
//             SetCardsActiveSafe(false);

//             int hit   = CPTScoreRuntime.I ? CPTScoreRuntime.I.CorrectTargetsHit : 0;
//             int total = CPTScoreRuntime.I ? CPTScoreRuntime.I.TotalTargets       : 0;
//             Debug.Log($"[MOXO] Score runtime: {hit}/{total}");

//             // Show results → switch to Narrative → trigger Koala dialogue
//             var intro = IntroScreen.Instance ?? FindObjectOfType<IntroScreen>(true);
//             if (intro != null)
//             {
//                 intro.ShowResults(hit, total, () =>
//                 {


//             // Animation: stop cheering, stand up
//             KoalaAnimBus.BroadcastToActiveKoalas(d => d.OnGameEnd());
//                     // Animation: start talking for the post-game dialogue
//                     KoalaAnimBus.BroadcastToActiveKoalas(d => d.OnResultsContinue());
//                     Debug.Log("[MOXO] Results Continue → Narrative → Koala after-game dialogue.");
//                     GameManager.Instance?.UpdateGameState(GameManager.GameState.Narrative);
//                     StartKoalaAfterGameDialogue();
//                 });
//             }
//             else
//             {
//                 Debug.LogWarning("[MOXO] IntroScreen not found; switching state immediately.");
//                 GameManager.Instance?.UpdateGameState(GameManager.GameState.Narrative);
//                 StartKoalaAfterGameDialogue();
//             }
//         }

//         // -------- After-results handoff to KoalaDialogueHandler --------
//         private void StartKoalaAfterGameDialogue()
//         {
//             Debug.Log("[AfterGame] StartKoalaAfterGameDialogue()");
//             StartCoroutine(CoStartKoalaDialogueNextFrame());
//         }

//         private IEnumerator CoStartKoalaDialogueNextFrame()
//         {
//             yield return null; // let UI/state settle

//             GameObject koala =
//                 SafeFindByTag("NPC_Koala") ??
//                 GameObject.Find("Koala");

//             KoalaDialogueHandler handler = null;

//             if (koala)
//             {
//                 handler = koala.GetComponentInChildren<KoalaDialogueHandler>(true);
//                 Debug.Log($"[AfterGame] Found Koala GO: {(koala ? koala.name : "null")}, " +
//                           $"handler={(handler ? "YES" : "NO")}, active={koala.activeInHierarchy}");
//             }

//             if (!handler)
//             {
//                 handler = FindObjectsOfType<KoalaDialogueHandler>(true).FirstOrDefault();
//                 if (handler) koala = handler.gameObject;
//             }

//             if (!handler)
//             {
//                 Debug.LogWarning("[AfterGame] ❌ No KoalaDialogueHandler found in scene. " +
//                                  "Add it to your Koala NPC and wire the DialogueTree + Dialogue UI Object.");
//                 yield break;
//             }

//             if (koala && !koala.activeInHierarchy)
//             {
//                 koala.SetActive(true);
//                 Debug.Log("[AfterGame] Enabled Koala GameObject.");
//             }

//             try
//             {
//                 handler.StartAfterMoxo();
//                 Debug.Log("[AfterGame] ✅ KoalaDialogueHandler.StartAfterMoxo() invoked.");
//             }
//             catch (Exception e)
//             {
//                 Debug.LogWarning($"[AfterGame] KoalaDialogueHandler.StartAfterMoxo threw: {e.Message}");
//             }
//         }

//         // ---------- internals ----------
//         private void SetCardsActiveSafe(bool on)
//         {
//             var cards = GetComponent("CardsActive");
//             if (cards == null)
//             {
//                 Debug.Log($"[MOXO] CardsActive {(on ? "enable" : "disable")} skipped (component missing).");
//                 return;
//             }

//             try
//             {
//                 var m = cards.GetType().GetMethod("SetCardsActive", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
//                 if (m != null)
//                 {
//                     m.Invoke(cards, new object[] { on });
//                 }
//                 else
//                 {
//                     // fallback: common alternatives
//                     var start = cards.GetType().GetMethod("StartCards", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
//                     var stop  = cards.GetType().GetMethod("StopCards",  BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
//                     if (on) start?.Invoke(cards, null); else stop?.Invoke(cards, null);
//                 }
//             }
//             catch (Exception e)
//             {
//                 Debug.LogWarning($"[MOXO] CardsActive toggle threw: {e.Message}");
//             }
//         }

//         /// <summary>
//         /// Find the active ChangeShapes on the active rig and start it.
//         /// Tries public ForceStart()/BeginNow()/StartNow(); otherwise invokes private CoStartAfterIntro() via reflection.
//         /// </summary>
//         private void NudgeActiveRunnerStart()
//         {
//             var runner = FindObjectsOfType<MonoBehaviour>(true)
//                          .FirstOrDefault(mb => mb && mb.isActiveAndEnabled && mb.gameObject.activeInHierarchy
//                                             && mb.GetType().Name == "ChangeShapes");

//             if (!runner)
//             {
//                 Debug.LogWarning("[MOXO] No active ChangeShapes found to start.");
//                 return;
//             }

//             var t = runner.GetType();

//             // 1) Prefer an explicit public starter if present
//             string[] starters = { "ForceStart", "BeginNow", "StartNow" };
//             foreach (var name in starters)
//             {
//                 var m = t.GetMethod(name, BindingFlags.Instance | BindingFlags.Public);
//                 if (m != null && m.GetParameters().Length == 0)
//                 {
//                     try
//                     {
//                         Debug.Log($"[MOXO] Runner.{name}() on {runner.gameObject.name}");
//                         m.Invoke(runner, null);
//                         return;
//                     }
//                     catch (Exception e)
//                     {
//                         Debug.LogWarning($"[MOXO] Runner.{name} threw: {e.Message}");
//                     }
//                 }
//             }

//             // 2) Fallback: invoke the private coroutine CoStartAfterIntro()
//             var co = t.GetMethod("CoStartAfterIntro", BindingFlags.Instance | BindingFlags.NonPublic);
//             if (co != null && typeof(IEnumerator).IsAssignableFrom(co.ReturnType))
//             {
//                 try
//                 {
//                     var enumerator = (IEnumerator)co.Invoke(runner, null);
//                     Debug.Log($"[MOXO] Runner.StartCoroutine(CoStartAfterIntro) on {runner.gameObject.name}");
//                     runner.StartCoroutine(enumerator);
//                     return;
//                 }
//                 catch (Exception e)
//                 {
//                     Debug.LogWarning($"[MOXO] Runner.CoStartAfterIntro threw: {e.Message}");
//                 }
//             }

//             Debug.LogWarning("[MOXO] Could not start ChangeShapes: no known entry points found.");
//         }

//         // Safe tag lookup
//         private static GameObject SafeFindByTag(string tag)
//         {
//             try
//             {
//                 var arr = GameObject.FindGameObjectsWithTag(tag);
//                 return (arr != null && arr.Length > 0) ? arr[0] : null;
//             }
//             catch (UnityException) { return null; }
//         }
//     }
// }


// REAL-START-ONLY MOXO MANAGER
// (IslandTravelManager owns training + ready intro)

// using System;
// using System.Collections;
// using System.Linq;
// using System.Reflection;
// using UnityEngine;

// namespace MoxoCPT
// {
//     public class MoxoCPTManager : MonoBehaviour
//     {
//         public static MoxoCPTManager Instance;

//         [HideInInspector] public bool isGameOver;

//         [Header("Optional Systems")]
//         [SerializeField] private DistractorSystem distractors;
//         [SerializeField] private EyeTrackLogger eyeLogger;

//         private bool _running = false;     // prevents double starts (real game)
//         private bool _eyeLogging = false;  // guards eye logger sessions

//         // --- Singleton lifecycle ---
//         private void Awake()
//         {
//             if (Instance == null)
//             {
//                 Instance = this;
//                 DontDestroyOnLoad(gameObject);
//             }
//             else if (Instance != this)
//             {
//                 Destroy(gameObject);
//                 return;
//             }
//         }

//         private void OnEnable()
//         {
//             Instance = this;
//             InitializeIfNeeded();
//         }

//         private void OnDisable()
//         {
//             if (Instance == this) Instance = null;
//         }

//         public void InitializeIfNeeded() { /* keep empty */ }

//         // ---------- External helpers ----------
//         public void ForceEndAndCleanup()
//         {
//             try { StopAllCoroutines(); } catch { }

//             if (Time.timeScale != 1f) Time.timeScale = 1f;

//             distractors?.StopSystem();

//             if (_eyeLogging) { (eyeLogger ?? EyeTrackLogger.I)?.EndSession(); _eyeLogging = false; }

//             SetCardsActiveSafe(false);

//             _running  = false;
//             isGameOver = true;

//             GameManager.Instance?.UpdateGameState(GameManager.GameState.Explore);
//         }

//         public void PrepareForNewIsland(string islandId = null)
//         {
//             CPTScoreRuntime.I?.ResetScore();

//             var cards = GetComponent("CardsActive");
//             if (cards != null)
//             {
//                 var t = cards.GetType();
//                 string[] resetMethods = { "ResetCards", "ResetDeck", "PrepareForIsland", "ClearAndLoadTargets", "InitForIsland" };

//                 foreach (var name in resetMethods)
//                 {
//                     var m = t.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
//                     if (m == null) continue;

//                     var ps = m.GetParameters();
//                     try
//                     {
//                         if (ps.Length == 0)
//                         {
//                             m.Invoke(cards, null);
//                             Debug.Log($"[MOXO] CardsActive.{name}()");
//                             break;
//                         }
//                         else if (ps.Length == 1 && ps[0].ParameterType == typeof(string))
//                         {
//                             m.Invoke(cards, new object[] { islandId ?? "" });
//                             Debug.Log($"[MOXO] CardsActive.{name}(\"{islandId}\")");
//                             break;
//                         }
//                     }
//                     catch (Exception e) { Debug.LogWarning($"[MOXO] CardsActive.{name} threw: {e.Message}"); }
//                 }
//             }
//         }

//         // ================== PUBLIC ENTRY ==================
//         // Called by IntroScreen (ready intro) after IslandTravelManager has finished training.
//         public void OnGameBegin()
//         {
//             OnGameBeginReal();
//         }

//         // ========== REAL START ==========
//         private void OnGameBeginReal()
//         {
//             if (_running)
//             {
//                 Debug.LogWarning("[MoxoCPTManager] OnGameBeginReal called while already running. Ignoring.");
//                 return;
//             }

//             try { StopAllCoroutines(); } catch { }
//             isGameOver = false;
//             _running = true;

//             // Fresh score
//             CPTScoreRuntime.I?.ResetScore();

//             // Cards ON
//             SetCardsActiveSafe(true);

//             // Create logging file for this run
//             LoggingReport.CreateReportCSV();

//             // Game state → CPT (this is what ChangeShapes waits for)
//             if (GameManager.Instance != null)
//             {
//                 GameManager.Instance.UpdateGameState(GameManager.GameState.CPT);
//                 Debug.Log("[MOXO] GameState set to CPT (real run).");
//             }
//             else
//             {
//                 Debug.LogError("[MoxoCPTManager] Cannot set state: GameManager.Instance is null.");
//             }

//             // Anim
//             KoalaAnimBus.BroadcastToActiveKoalas(d => d.OnGameBegin());

//             // Eye logger
//             var pid = GameManager.Instance != null ? GameManager.Instance.ParticipantId : "";
//             if (string.IsNullOrWhiteSpace(pid))
//             {
//                 Debug.LogWarning("[MOXO] ParticipantId is empty; using timestamp fallback.");
//                 pid = "P_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
//             }

//             if (eyeLogger == null) eyeLogger = EyeTrackLogger.I ?? FindObjectOfType<EyeTrackLogger>(true);
//             Debug.Log($"[MOXO] EyeLogger = {(eyeLogger ? "OK" : "MISSING")}");
//             if (eyeLogger && !_eyeLogging)
//             {
//                 eyeLogger.BeginSession(pid);
//                 _eyeLogging = true;
//             }

//             // Ensure the active island's ChangeShapes actually starts.
//             NudgeActiveRunnerStart();
//         }

//         // ---------- Game end ----------
//         public void OnGameEnd()
//         {
//             KoalaAnimBus.BroadcastToActiveKoalas(d => d.OnGameEnd());

//             if (!_running && isGameOver)
//             {
//                 Debug.Log("[MOXO] OnGameEnd called but not running; ignoring duplicate.");
//                 return;
//             }

//             Debug.Log("[MOXO] OnGameEnd()");
//             _running = false;
//             isGameOver = true;

//             distractors?.StopSystem();

//             if (_eyeLogging) { (eyeLogger ?? EyeTrackLogger.I)?.EndSession(); _eyeLogging = false; }

//             SetCardsActiveSafe(false);

//             int hit   = CPTScoreRuntime.I ? CPTScoreRuntime.I.CorrectTargetsHit : 0;
//             int total = CPTScoreRuntime.I ? CPTScoreRuntime.I.TotalTargets       : 0;
//             Debug.Log($"[MOXO] Score runtime: {hit}/{total}");

//             var intro = IntroScreen.Instance ?? FindObjectOfType<IntroScreen>(true);
//             if (intro != null)
//             {
//                 intro.ShowResults(hit, total, () =>
//                 {
//                     KoalaAnimBus.BroadcastToActiveKoalas(d => d.OnResultsContinue());
//                     Debug.Log("[MOXO] Results Continue → Narrative → Koala after-game dialogue.");
//                     GameManager.Instance?.UpdateGameState(GameManager.GameState.Narrative);
//                     StartKoalaAfterGameDialogue();
//                 });
//             }
//             else
//             {
//                 Debug.LogWarning("[MOXO] IntroScreen not found; switching state immediately.");
//                 GameManager.Instance?.UpdateGameState(GameManager.GameState.Narrative);
//                 StartKoalaAfterGameDialogue();
//             }
//         }

//         private void StartKoalaAfterGameDialogue()
//         {
//             Debug.Log("[AfterGame] StartKoalaAfterGameDialogue()");
//             StartCoroutine(CoStartKoalaDialogueNextFrame());
//         }

//         private IEnumerator CoStartKoalaDialogueNextFrame()
//         {
//             yield return null;

//             GameObject koala =
//                 SafeFindByTag("NPC_Koala") ??
//                 GameObject.Find("Koala");

//             KoalaDialogueHandler handler = null;

//             if (koala)
//             {
//                 handler = koala.GetComponentInChildren<KoalaDialogueHandler>(true);
//                 Debug.Log($"[AfterGame] Found Koala GO: {(koala ? koala.name : "null")}, handler={(handler ? "YES" : "NO")}, active={koala.activeInHierarchy}");
//             }

//             if (!handler)
//             {
//                 handler = FindObjectsOfType<KoalaDialogueHandler>(true).FirstOrDefault();
//             }

//             if (!handler)
//             {
//                 Debug.LogWarning("[AfterGame] ❌ No KoalaDialogueHandler found in scene.");
//                 yield break;
//             }

//             if (koala && !koala.activeInHierarchy)
//                 koala.SetActive(true);

//             try { handler.StartAfterMoxo(); }
//             catch (Exception e) { Debug.LogWarning($"[AfterGame] KoalaDialogueHandler.StartAfterMoxo threw: {e.Message}"); }
//         }

//         // ---------- internals ----------
//         private void SetCardsActiveSafe(bool on)
//         {
//             var cards = GetComponent("CardsActive");
//             if (cards == null)
//             {
//                 Debug.Log($"[MOXO] CardsActive {(on ? "enable" : "disable")} skipped (component missing).");
//                 return;
//             }

//             try
//             {
//                 var m = cards.GetType().GetMethod("SetCardsActive", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
//                 if (m != null)
//                 {
//                     m.Invoke(cards, new object[] { on });
//                 }
//                 else
//                 {
//                     var start = cards.GetType().GetMethod("StartCards", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
//                     var stop  = cards.GetType().GetMethod("StopCards",  BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
//                     if (on) start?.Invoke(cards, null); else stop?.Invoke(cards, null);
//                 }
//             }
//             catch (Exception e)
//             {
//                 Debug.LogWarning($"[MOXO] CardsActive toggle threw: {e.Message}");
//             }
//         }

//         private void NudgeActiveRunnerStart()
//         {
//             var runner = FindObjectsOfType<MonoBehaviour>(true)
//                          .FirstOrDefault(mb => mb && mb.isActiveAndEnabled && mb.gameObject.activeInHierarchy
//                                             && mb.GetType().Name == "ChangeShapes");

//             if (!runner)
//             {
//                 Debug.LogWarning("[MOXO] No active ChangeShapes found to start.");
//                 return;
//             }

//             var t = runner.GetType();

//             string[] starters = { "ForceStart", "BeginNow", "StartNow" };
//             foreach (var name in starters)
//             {
//                 var m = t.GetMethod(name, BindingFlags.Instance | BindingFlags.Public);
//                 if (m != null && m.GetParameters().Length == 0)
//                 {
//                     try { Debug.Log($"[MOXO] Runner.{name}() on {runner.gameObject.name}"); m.Invoke(runner, null); return; }
//                     catch (Exception e) { Debug.LogWarning($"[MOXO] Runner.{name} threw: {e.Message}"); }
//                 }
//             }

//             var co = t.GetMethod("CoStartAfterIntro", BindingFlags.Instance | BindingFlags.NonPublic);
//             if (co != null && typeof(IEnumerator).IsAssignableFrom(co.ReturnType))
//             {
//                 try
//                 {
//                     var enumerator = (IEnumerator)co.Invoke(runner, null);
//                     Debug.Log($"[MOXO] Runner.StartCoroutine(CoStartAfterIntro) on {runner.gameObject.name}");
//                     runner.StartCoroutine(enumerator);
//                     return;
//                 }
//                 catch (Exception e) { Debug.LogWarning($"[MOXO] Runner.CoStartAfterIntro threw: {e.Message}"); }
//             }

//             Debug.LogWarning("[MOXO] Could not start ChangeShapes: no known entry points found.");
//         }

//         private static GameObject SafeFindByTag(string tag)
//         {
//             try
//             {
//                 var arr = GameObject.FindGameObjectsWithTag(tag);
//                 return (arr != null && arr.Length > 0) ? arr[0] : null;
//             }
//             catch (UnityException) { return null; }
//         }
//     }
// }

//Jan 20 - Add safeguard

// using System;
// using System.Collections;
// using System.Linq;
// using System.Reflection;
// using UnityEngine;
// using UnityEngine.InputSystem; // <-- NEW: for Space/A tracking

// namespace MoxoCPT
// {
//     public class MoxoCPTManager : MonoBehaviour
//     {
//         public static MoxoCPTManager Instance;

//         [HideInInspector] public bool isGameOver;

//         [Header("Optional Systems")]
//         [SerializeField] private DistractorSystem distractors;
//         [SerializeField] private EyeTrackLogger eyeLogger;

//         // -------- NEW: Safeguard settings --------
//         [Header("Safeguard (Replay If No Real Participation)")]
//         [SerializeField] private bool enableSafeguard = true;
//         [SerializeField] private string redoTitle = "Let's try that again";
//         [SerializeField, TextArea]
//         private string redoBody = "It looks like you didn't respond or missed every target.\nPress Space to replay the test.";
//         [SerializeField] private float redoDelaySeconds = 1.5f; // brief delay before enabling the Start button
//         // -----------------------------------------

//         private bool _running = false;     // prevents double starts (real game)
//         private bool _eyeLogging = false;  // guards eye logger sessions

//         // -------- NEW: runtime counters for safeguard --------
//         private int _cptKeyPresses = 0;    // Space/A presses during real CPT
//         // -----------------------------------------------------

//         // --- Singleton lifecycle ---
//         private void Awake()
//         {
//             if (Instance == null)
//             {
//                 Instance = this;
//                 DontDestroyOnLoad(gameObject);
//             }
//             else if (Instance != this)
//             {
//                 Destroy(gameObject);
//                 return;
//             }
//         }

//         private void OnEnable()
//         {
//             Instance = this;
//             InitializeIfNeeded();
//         }

//         private void OnDisable()
//         {
//             if (Instance == this) Instance = null;
//         }

//         public void InitializeIfNeeded() { /* keep empty */ }

//         // -------- NEW: track Space/A only while real CPT is running --------
//         private void Update()
//         {
//             if (!_running) return;
//             if (GameManager.Instance == null || GameManager.Instance.State != GameManager.GameState.CPT) return;

//             var kb = Keyboard.current;
//             var gp = Gamepad.current;

//             bool pressedSpace = kb != null && kb.spaceKey.wasPressedThisFrame;
//             bool pressedA     = gp != null && gp.buttonSouth.wasPressedThisFrame;

//             if (pressedSpace || pressedA) _cptKeyPresses++;
//         }
//         // -------------------------------------------------------------------

//         // ---------- External helpers ----------
//         public void ForceEndAndCleanup()
//         {
//             var director = FindObjectOfType<MoxoCameraDirector>(true);
//             director?.ExitMoxoView();

            
//             try { StopAllCoroutines(); } catch { }

//             if (Time.timeScale != 1f) Time.timeScale = 1f;

//             distractors?.StopSystem();

//             if (_eyeLogging) { (eyeLogger ?? EyeTrackLogger.I)?.EndSession(); _eyeLogging = false; }

//             SetCardsActiveSafe(false);

//             _running  = false;
//             isGameOver = true;

//             GameManager.Instance?.UpdateGameState(GameManager.GameState.Explore);
//         }

//         public void PrepareForNewIsland(string islandId = null)
//         {
//             CPTScoreRuntime.I?.ResetScore();

//             var cards = GetComponent("CardsActive");
//             if (cards != null)
//             {
//                 var t = cards.GetType();
//                 string[] resetMethods = { "ResetCards", "ResetDeck", "PrepareForIsland", "ClearAndLoadTargets", "InitForIsland" };

//                 foreach (var name in resetMethods)
//                 {
//                     var m = t.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
//                     if (m == null) continue;

//                     var ps = m.GetParameters();
//                     try
//                     {
//                         if (ps.Length == 0)
//                         {
//                             m.Invoke(cards, null);
//                             Debug.Log($"[MOXO] CardsActive.{name}()");
//                             break;
//                         }
//                         else if (ps.Length == 1 && ps[0].ParameterType == typeof(string))
//                         {
//                             m.Invoke(cards, new object[] { islandId ?? "" });
//                             Debug.Log($"[MOXO] CardsActive.{name}(\"{islandId}\")");
//                             break;
//                         }
//                     }
//                     catch (Exception e) { Debug.LogWarning($"[MOXO] CardsActive.{name} threw: {e.Message}"); }
//                 }
//             }
//         }

//         // ================== PUBLIC ENTRY ==================
//         public void OnGameBegin()
//         {
//             OnGameBeginReal();
//         }

//         // ========== REAL START ==========
//         private void OnGameBeginReal()
//         {
//             if (_running)
//             {
//                 Debug.LogWarning("[MoxoCPTManager] OnGameBeginReal called while already running. Ignoring.");
//                 return;
//             }
//             //Hami: Add camera:
//             var director = FindObjectOfType<MoxoCameraDirector>(true);
//             var rig = FindObjectsOfType<IslandCameraRig>(true)
//                     .FirstOrDefault(r => r && r.gameObject.activeInHierarchy);

//             if (director && rig && rig.moxoCameraAnchor)
//                 director.EnterMoxoView(rig.moxoCameraAnchor);

//             //END CHANGE

//             try { StopAllCoroutines(); } catch { }
//             isGameOver = false;
//             _running = true;

//             // reset safeguard counters
//             _cptKeyPresses = 0;

//             // Fresh score
//             CPTScoreRuntime.I?.ResetScore();

//             // Cards ON
//             SetCardsActiveSafe(true);

//             // Create logging file for this run
//             LoggingReport.CreateReportCSV();

//             // Game state → CPT
//             if (GameManager.Instance != null)
//             {
//                 GameManager.Instance.UpdateGameState(GameManager.GameState.CPT);
//                 Debug.Log("[MOXO] GameState set to CPT (real run).");
//             }
//             else
//             {
//                 Debug.LogError("[MoxoCPTManager] Cannot set state: GameManager.Instance is null.");
//             }

//             // Anim
//             KoalaAnimBus.BroadcastToActiveKoalas(d => d.OnGameBegin());

//             // Eye logger
//             var pid = GameManager.Instance != null ? GameManager.Instance.ParticipantId : "";
//             if (string.IsNullOrWhiteSpace(pid))
//             {
//                 Debug.LogWarning("[MOXO] ParticipantId is empty; using timestamp fallback.");
//                 pid = "P_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
//             }

//             if (eyeLogger == null) eyeLogger = EyeTrackLogger.I ?? FindObjectOfType<EyeTrackLogger>(true);
//             Debug.Log($"[MOXO] EyeLogger = {(eyeLogger ? "OK" : "MISSING")}");
//             if (eyeLogger && !_eyeLogging)
//             {
//                 eyeLogger.BeginSession(pid);
//                 _eyeLogging = true;
//             }

//             // Ensure the active island's ChangeShapes actually starts.
//             NudgeActiveRunnerStart();
//         }

//         // ---------- Game end ----------
//         public void OnGameEnd()
//         {
//             KoalaAnimBus.BroadcastToActiveKoalas(d => d.OnGameEnd());

//             //Hami: Add Camera:
//             var director = FindObjectOfType<MoxoCameraDirector>(true);
//             director?.ExitMoxoView();

            
//             if (!_running && isGameOver)
//             {
//                 Debug.Log("[MOXO] OnGameEnd called but not running; ignoring duplicate.");
//                 return;
//             }

//             Debug.Log("[MOXO] OnGameEnd()");
//             _running = false;
//             isGameOver = true;

//             distractors?.StopSystem();

//             if (_eyeLogging) { (eyeLogger ?? EyeTrackLogger.I)?.EndSession(); _eyeLogging = false; }

//             SetCardsActiveSafe(false);

//             int hit   = CPTScoreRuntime.I ? CPTScoreRuntime.I.CorrectTargetsHit  : 0;
//             int total = CPTScoreRuntime.I ? CPTScoreRuntime.I.TotalTargets       : 0;

//             // NEW: non-target stats
//             int falseAlarms      = CPTScoreRuntime.I ? CPTScoreRuntime.I.FalseAlarms      : 0;
//             int totalDistractors = CPTScoreRuntime.I ? CPTScoreRuntime.I.TotalDistractors : 0;

//             Debug.Log($"[MOXO] Score runtime: Targets {hit}/{total}, Non-target hits {falseAlarms}/{totalDistractors} (key presses during CPT: {_cptKeyPresses})");

//             // -------- NEW: Safeguard check --------
//             if (enableSafeguard)
//             {
//                 bool noButtonPresses = (_cptKeyPresses <= 0);
//                 bool missedAllTargets = (hit <= 0 && total > 0);

//                 if (noButtonPresses || missedAllTargets)
//                 {
//                     ShowReplayPrompt();
//                     return; // don't show normal results; we give a replay prompt instead
//                 }
//             }
//             // --------------------------------------

//             var intro = IntroScreen.Instance ?? FindObjectOfType<IntroScreen>(true);
//             if (intro != null)
//             {
//                 // UPDATED: pass falseAlarms + totalDistractors to match IntroScreen.ShowResults signature
//                 intro.ShowResults(hit, total, falseAlarms, totalDistractors, () =>
//                 {
//                     KoalaAnimBus.BroadcastToActiveKoalas(d => d.OnResultsContinue());
//                     Debug.Log("[MOXO] Results Continue → Narrative → Koala after-game dialogue.");
//                     GameManager.Instance?.UpdateGameState(GameManager.GameState.Narrative);
//                     StartKoalaAfterGameDialogue();
//                 });
//             }
//             else
//             {
//                 Debug.LogWarning("[MOXO] IntroScreen not found; switching state immediately.");
//                 GameManager.Instance?.UpdateGameState(GameManager.GameState.Narrative);
//                 StartKoalaAfterGameDialogue();
//             }
//         }

//         // -------- NEW: Replay prompt when safeguard triggers --------
//         private void ShowReplayPrompt()
//         {
//             Debug.Log("[MOXO] Safeguard triggered → offering replay.");

//             var intro = IntroScreen.Instance ?? FindObjectOfType<IntroScreen>(true);
//             if (!intro)
//             {
//                 Debug.LogWarning("[MOXO] No IntroScreen found for replay prompt. Restarting immediately.");
//                 OnGameBeginReal();
//                 return;
//             }

//             // Show a clean text-only panel and arm Space/A to restart real run
//             intro.ShowTextOnly(redoTitle, redoBody);
//             intro.ArmOnStart(
//                 onStart: () => { OnGameBeginReal(); },
//                 delayButton: redoDelaySeconds > 0f,
//                 delaySeconds: Mathf.Max(0f, redoDelaySeconds)
//             );

//             // Keep state PREPARE_CPT while the prompt is up
//             GameManager.Instance?.UpdateGameState(GameManager.GameState.PrepareCPT);
//         }
//         // ------------------------------------------------------------

//         private void StartKoalaAfterGameDialogue()
//         {
//             Debug.Log("[AfterGame] StartKoalaAfterGameDialogue()");
//             StartCoroutine(CoStartKoalaDialogueNextFrame());
//         }

//         private IEnumerator CoStartKoalaDialogueNextFrame()
//         {
//             yield return null;

//             GameObject koala =
//                 SafeFindByTag("NPC_Koala") ??
//                 GameObject.Find("Koala");

//             KoalaDialogueHandler handler = null;

//             if (koala)
//             {
//                 handler = koala.GetComponentInChildren<KoalaDialogueHandler>(true);
//                 Debug.Log($"[AfterGame] Found Koala GO: {(koala ? koala.name : "null")}, handler={(handler ? "YES" : "NO")}, active={koala.activeInHierarchy}");
//             }

//             if (!handler)
//             {
//                 handler = FindObjectsOfType<KoalaDialogueHandler>(true).FirstOrDefault();
//             }

//             if (!handler)
//             {
//                 Debug.LogWarning("[AfterGame] ❌ No KoalaDialogueHandler found in scene.");
//                 yield break;
//             }

//             if (koala && !koala.activeInHierarchy)
//                 koala.SetActive(true);

//             try { handler.StartAfterMoxo(); }
//             catch (Exception e) { Debug.LogWarning($"[AfterGame] KoalaDialogueHandler.StartAfterMoxo threw: {e.Message}"); }
//         }

//         // ---------- internals ----------
//         private void SetCardsActiveSafe(bool on)
//         {
//             var cards = GetComponent("CardsActive");
//             if (cards == null)
//             {
//                 Debug.Log($"[MOXO] CardsActive {(on ? "enable" : "disable")} skipped (component missing).");
//                 return;
//             }

//             try
//             {
//                 var m = cards.GetType().GetMethod("SetCardsActive", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
//                 if (m != null)
//                 {
//                     m.Invoke(cards, new object[] { on });
//                 }
//                 else
//                 {
//                     var start = cards.GetType().GetMethod("StartCards", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
//                     var stop  = cards.GetType().GetMethod("StopCards",  BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
//                     if (on) start?.Invoke(cards, null); else stop?.Invoke(cards, null);
//                 }
//             }
//             catch (Exception e)
//             {
//                 Debug.LogWarning($"[MOXO] CardsActive toggle threw: {e.Message}");
//             }
//         }

//         private void NudgeActiveRunnerStart()
//         {
//             var runner = FindObjectsOfType<MonoBehaviour>(true)
//                          .FirstOrDefault(mb => mb && mb.isActiveAndEnabled && mb.gameObject.activeInHierarchy
//                                             && mb.GetType().Name == "ChangeShapes");

//             if (!runner)
//             {
//                 Debug.LogWarning("[MOXO] No active ChangeShapes found to start.");
//                 return;
//             }

//             var t = runner.GetType();

//             string[] starters = { "ForceStart", "BeginNow", "StartNow" };
//             foreach (var name in starters)
//             {
//                 var m = t.GetMethod(name, BindingFlags.Instance | BindingFlags.Public);
//                 if (m != null && m.GetParameters().Length == 0)
//                 {
//                     try { Debug.Log($"[MOXO] Runner.{name}() on {runner.gameObject.name}"); m.Invoke(runner, null); return; }
//                     catch (Exception e) { Debug.LogWarning($"[MOXO] Runner.{name} threw: {e.Message}"); }
//                 }
//             }

//             var co = t.GetMethod("CoStartAfterIntro", BindingFlags.Instance | BindingFlags.NonPublic);
//             if (co != null && typeof(IEnumerator).IsAssignableFrom(co.ReturnType))
//             {
//                 try
//                 {
//                     var enumerator = (IEnumerator)co.Invoke(runner, null);
//                     Debug.Log($"[MOXO] Runner.StartCoroutine(CoStartAfterIntro) on {runner.gameObject.name}");
//                     runner.StartCoroutine(enumerator);
//                     return;
//                 }
//                 catch (Exception e) { Debug.LogWarning($"[MOXO] Runner.CoStartAfterIntro threw: {e.Message}"); }
//             }

//             Debug.LogWarning("[MOXO] Could not start ChangeShapes: no known entry points found.");
//         }

//         private static GameObject SafeFindByTag(string tag)
//         {
//             try
//             {
//                 var arr = GameObject.FindGameObjectsWithTag(tag);
//                 return (arr != null && arr.Length > 0) ? arr[0] : null;
//             }
//             catch (UnityException) { return null; }
//         }
//     }
// }

//ADD CAMERA + KOALA READY BROADCAST (NPC_Flynn) - copy/paste
/// ADD CAMERA + BETTER DEBUG + MORE ROBUST RIG PICKING
// Drop-in replacement for your current MoxoCPTManager.cs
// using System;
// using System.Collections;
// using System.Linq;
// using System.Reflection;
// using UnityEngine;
// using UnityEngine.InputSystem;

// namespace MoxoCPT
// {
//     public class MoxoCPTManager : MonoBehaviour
//     {
//         public static MoxoCPTManager Instance;

//         [HideInInspector] public bool isGameOver;

//         [Header("Optional Systems")]
//         [SerializeField] private DistractorSystem distractors;
//         [SerializeField] private EyeTrackLogger eyeLogger;

//         [Header("Camera Acquire")]
//         [Tooltip("How long we retry to find the island rig after entering CPT.")]
//         [SerializeField] private float cameraAcquireTimeoutSeconds = 2.0f;
//         [SerializeField] private bool logCamera = true;
//         [SerializeField] private float maxAnchorDistanceFallback = 80f;

//         [Header("Safeguard (Replay If No Real Participation)")]
//         [SerializeField] private bool enableSafeguard = true;
//         [SerializeField] private string redoTitle = "Let's try that again";
//         [SerializeField, TextArea]
//         private string redoBody =
//             "It looks like you didn't respond or missed every target.\nPress Space to replay the test.";
//         [SerializeField] private float redoDelaySeconds = 1.5f;

//         // runtime
//         private bool _running = false;
//         private bool _eyeLogging = false;
//         private int _cptKeyPresses = 0;

//         private Coroutine _cameraCo;
//         private bool _moxoCameraActive = false;

//         // NEW: lock GameState to CPT while running / showing results / showing replay prompt
//         private bool _lockStateToCpt = false;
//         private Coroutine _stateLockCo;

//         // ==================================================
//         #region Unity Lifecycle

//         private void Awake()
//         {
//             if (Instance == null)
//             {
//                 Instance = this;
//                 DontDestroyOnLoad(gameObject);
//             }
//             else if (Instance != this)
//             {
//                 Destroy(gameObject);
//                 return;
//             }
//         }

//         private void OnEnable()
//         {
//             Instance = this;
//             GameManager.OnGameStateChanged += OnGameStateChanged;
//         }

//         private void OnDisable()
//         {
//             GameManager.OnGameStateChanged -= OnGameStateChanged;
//             if (Instance == this) Instance = null;
//         }

//         private void Update()
//         {
//             // Track Space/A presses only during real CPT run
//             if (!_running) return;
//             if (GameManager.Instance == null || GameManager.Instance.State != GameManager.GameState.CPT) return;

//             var kb = Keyboard.current;
//             var gp = Gamepad.current;

//             bool pressedSpace = kb != null && kb.spaceKey.wasPressedThisFrame;
//             bool pressedA = gp != null && gp.buttonSouth.wasPressedThisFrame;

//             if (pressedSpace || pressedA)
//                 _cptKeyPresses++;
//         }

//         #endregion

//         // ==================================================
//         #region State Lock (CPT)

//         private void SetCptLock(bool on)
//         {
//             _lockStateToCpt = on;

//             if (!on)
//             {
//                 if (_stateLockCo != null) StopCoroutine(_stateLockCo);
//                 _stateLockCo = null;
//             }
//         }

//         private void OnGameStateChanged(GameManager.GameState newState)
//         {
//             if (!_lockStateToCpt) return;

//             // Allow Teleport if your flow needs it, otherwise force CPT always
//             if (newState == GameManager.GameState.Teleport) return;

//             if (newState != GameManager.GameState.CPT)
//             {
//                 if (logCamera)
//                     Debug.LogWarning($"[MOXO] CPT LOCK: detected state change to {newState}. Forcing back to CPT.");

//                 if (_stateLockCo != null) StopCoroutine(_stateLockCo);
//                 _stateLockCo = StartCoroutine(CoForceBackToCptNextFrame());
//             }
//         }

//         private IEnumerator CoForceBackToCptNextFrame()
//         {
//             yield return null;

//             if (!_lockStateToCpt) yield break;
//             if (GameManager.Instance == null) yield break;

//             // Use ForceUpdateGameState if you have it; else fallback to UpdateGameState
//             try
//             {
//                 var gm = GameManager.Instance;

//                 // Prefer ForceUpdateGameState if present
//                 var mi = gm.GetType().GetMethod("ForceUpdateGameState",
//                     BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

//                 if (mi != null)
//                     mi.Invoke(gm, new object[] { GameManager.GameState.CPT });
//                 else
//                     gm.UpdateGameState(GameManager.GameState.CPT);
//             }
//             catch
//             {
//                 GameManager.Instance.UpdateGameState(GameManager.GameState.CPT);
//             }

//             // Re-acquire camera if someone caused an exit
//             if (!_moxoCameraActive)
//                 StartEnterMoxoCameraWhenReady();
//         }

//         #endregion

//         // ==================================================
//         #region Camera Handling

//         private static string Norm(string s)
//         {
//             return string.IsNullOrWhiteSpace(s) ? string.Empty : s.Trim().ToUpperInvariant();
//         }

//         private string CurrentIslandId()
//         {
//             var travel = IslandTravelManager.I;
//             if (travel == null || travel.CurrentIsland == null) return "";
//             return Norm(travel.CurrentIsland.islandId);
//         }

//         private IslandCameraRig FindRigForCurrentIslandOrFallback(out string reason)
//         {
//             reason = "";

//             string islandId = CurrentIslandId();
//             var rigs = FindObjectsOfType<IslandCameraRig>(true);

//             if (string.IsNullOrEmpty(islandId))
//             {
//                 reason = "CurrentIslandId empty -> fallback nearest rig.";
//                 return FindNearestActiveRig(rigs, out _);
//             }

//             var strict = rigs.FirstOrDefault(r =>
//                 r &&
//                 r.gameObject.activeInHierarchy &&
//                 Norm(r.islandId) == islandId &&
//                 r.moxoCameraAnchor != null);

//             if (strict != null)
//             {
//                 reason = $"STRICT match '{islandId}'.";
//                 return strict;
//             }

//             var idButInactive = rigs.FirstOrDefault(r =>
//                 r && Norm(r.islandId) == islandId && r.moxoCameraAnchor != null);

//             if (idButInactive != null && !idButInactive.gameObject.activeInHierarchy)
//             {
//                 reason = $"Rig exists for '{islandId}' but is inactive -> island moxoRoot disabled.";
//                 return null;
//             }

//             var nearest = FindNearestActiveRig(rigs, out float bestDist);
//             if (nearest != null && bestDist <= Mathf.Max(0.1f, maxAnchorDistanceFallback))
//             {
//                 reason = $"FALLBACK nearest active rig dist={bestDist:0.00}.";
//                 return nearest;
//             }

//             reason = $"No rig found for '{islandId}'.";
//             return null;
//         }

//         private IslandCameraRig FindNearestActiveRig(IslandCameraRig[] rigs, out float bestDist)
//         {
//             bestDist = float.PositiveInfinity;

//             var player = FindObjectOfType<DesktopArrowController>(true);
//             Vector3 p = player ? player.transform.position : Vector3.zero;

//             IslandCameraRig best = null;

//             foreach (var r in rigs)
//             {
//                 if (!r) continue;
//                 if (!r.gameObject.activeInHierarchy) continue;
//                 if (!r.moxoCameraAnchor) continue;

//                 float d = Vector3.Distance(p, r.moxoCameraAnchor.position);
//                 if (d < bestDist)
//                 {
//                     bestDist = d;
//                     best = r;
//                 }
//             }

//             return best;
//         }

//         private void StartEnterMoxoCameraWhenReady()
//         {
//             if (_cameraCo != null)
//                 StopCoroutine(_cameraCo);

//             _cameraCo = StartCoroutine(CoEnterMoxoCameraWhenReady());
//         }

//         private IEnumerator CoEnterMoxoCameraWhenReady()
//         {
//             _moxoCameraActive = false;
//             yield return null;

//             float elapsed = 0f;
//             float timeout = Mathf.Max(0.1f, cameraAcquireTimeoutSeconds);

//             while (elapsed < timeout)
//             {
//                 if (GameManager.Instance == null || GameManager.Instance.State != GameManager.GameState.CPT)
//                     yield break;

//                 var rig = FindRigForCurrentIslandOrFallback(out string reason);
//                 if (rig != null)
//                 {
//                     var director = FindObjectOfType<MoxoCameraDirector>(true);
//                     if (!director)
//                     {
//                         Debug.LogWarning("[MOXO-CAM] MoxoCameraDirector missing.");
//                         yield break;
//                     }

//                     director.EnterMoxoView(rig.moxoCameraAnchor);
//                     _moxoCameraActive = true;

//                     if (logCamera)
//                         Debug.Log($"[MOXO-CAM] ENTER OK -> rig='{rig.name}', id='{rig.islandId}', reason={reason}");

//                     yield break;
//                 }

//                 elapsed += 0.1f;
//                 yield return new WaitForSecondsRealtime(0.1f);
//             }

//             Debug.LogWarning("[MOXO-CAM] Camera acquire timed out.");
//         }

//         private void ExitMoxoCamera()
//         {
//             if (_cameraCo != null)
//             {
//                 StopCoroutine(_cameraCo);
//                 _cameraCo = null;
//             }

//             if (_moxoCameraActive)
//             {
//                 var director = FindObjectOfType<MoxoCameraDirector>(true);
//                 director?.ExitMoxoView();
//             }

//             _moxoCameraActive = false;
//         }

//         #endregion

//         // ==================================================
//         #region Game Flow

//         public void ForceEndAndCleanup()
//         {
//             SetCptLock(false);

//             ExitMoxoCamera();

//             try { StopAllCoroutines(); } catch { }
//             Time.timeScale = 1f;

//             distractors?.StopSystem();

//             if (_eyeLogging)
//             {
//                 (eyeLogger ?? EyeTrackLogger.I)?.EndSession();
//                 _eyeLogging = false;
//             }

//             SetCardsActiveSafe(false);

//             _running = false;
//             isGameOver = true;

//             GameManager.Instance?.UpdateGameState(GameManager.GameState.Narrative);
//         }

//         public void OnGameBegin() => OnGameBeginReal();

//         private void OnGameBeginReal()
//         {
//             if (_running)
//             {
//                 Debug.LogWarning("[MOXO] CPT already running.");
//                 return;
//             }

//             try { StopAllCoroutines(); } catch { }

//             _running = true;
//             isGameOver = false;
//             _cptKeyPresses = 0;

//             CPTScoreRuntime.I?.ResetScore();
//             SetCardsActiveSafe(true);

//             LoggingReport.CreateReportCSV();

//             // LOCK CPT immediately (prevents other scripts from pulling to Explore/Narrative)
//             SetCptLock(true);

//             if (GameManager.Instance != null)
//             {
//                 GameManager.Instance.UpdateGameState(GameManager.GameState.CPT);
//                 Debug.Log("[MOXO] GameState set to CPT (real run).");
//             }
//             else
//             {
//                 Debug.LogError("[MoxoCPTManager] Cannot set state: GameManager.Instance is null.");
//             }

//             StartEnterMoxoCameraWhenReady();

//             KoalaAnimBus.EnterCptAll();

//             string pid = GameManager.Instance?.ParticipantId;
//             if (string.IsNullOrWhiteSpace(pid))
//                 pid = "P_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

//             eyeLogger ??= EyeTrackLogger.I;
//             if (eyeLogger && !_eyeLogging)
//             {
//                 eyeLogger.BeginSession(pid);
//                 _eyeLogging = true;
//             }

//             NudgeActiveRunnerStart();
//         }

//         public void OnGameEnd()
//         {
            
            
            
            
//             KoalaAnimBus.BroadcastToActiveKoalas(d => d.OnGameEnd());

//             if (!_running && isGameOver)
//                 return;

//             _running = false;
//             isGameOver = true;

//             distractors?.StopSystem();

//             if (_eyeLogging)
//             {
//                 (eyeLogger ?? EyeTrackLogger.I)?.EndSession();
//                 _eyeLogging = false;
//             }

//             // Keep CPT + keep MOXO camera here.
//             // We only exit camera + unlock CPT when player presses Continue on results.
//             SetCardsActiveSafe(false);

//             int hit = CPTScoreRuntime.I?.CorrectTargetsHit ?? 0;
//             int total = CPTScoreRuntime.I?.TotalTargets ?? 0;

//             int falseAlarms = CPTScoreRuntime.I?.FalseAlarms ?? 0;
//             int totalDistractors = CPTScoreRuntime.I?.TotalDistractors ?? 0;

//             Debug.Log($"[MOXO] Targets {hit}/{total}, False {falseAlarms}/{totalDistractors}, Keys {_cptKeyPresses}");

//             if (enableSafeguard)
//             {
//                 bool noButtonPresses = (_cptKeyPresses <= 0);
//                 bool missedAllTargets = (hit <= 0 && total > 0);

//                 if (noButtonPresses || missedAllTargets)
//                 {
//                     ShowReplayPrompt_KEEP_CPT();
//                     return;
//                 }
//             }

//             var intro = IntroScreen.Instance ?? FindObjectOfType<IntroScreen>(true);
//             if (intro != null)
//             {
//                 intro.ShowResults(hit, total, falseAlarms, totalDistractors, () =>
//                 {
//                     // NOW we leave CPT.
//                     SetCptLock(false);
//                     ExitMoxoCamera();

//                     //Animation: Koala Talk
//                     KoalaAnimBus.BroadcastToActiveKoalas(d => d.OnResultsContinue());
//                     Debug.Log("[MOXO] Results Continue → Narrative.");

//                     GameManager.Instance?.UpdateGameState(GameManager.GameState.Narrative);
//                     StartKoalaAfterGameDialogue();
//                 });
//             }
//             else
//             {
//                 Debug.LogWarning("[MOXO] IntroScreen not found; switching state immediately.");

//                 SetCptLock(false);
//                 ExitMoxoCamera();

//                 GameManager.Instance?.UpdateGameState(GameManager.GameState.Narrative);
//                 StartKoalaAfterGameDialogue();
//             }
//         }

//         #endregion

//         // ==================================================
//         #region Helpers

//         private void ShowReplayPrompt_KEEP_CPT()
//         {
//             Debug.Log("[MOXO] Safeguard triggered → offering replay (stay in CPT, keep MOXO cam).");

//             var intro = IntroScreen.Instance ?? FindObjectOfType<IntroScreen>(true);
//             if (!intro)
//             {
//                 Debug.LogWarning("[MOXO] No IntroScreen found for replay prompt. Restarting immediately.");
//                 OnGameBeginReal();
//                 return;
//             }

//             // DO NOT switch to PrepareCPT (you want to remain CPT)
//             intro.ShowTextOnly(redoTitle, redoBody);
//             intro.ArmOnStart(
//                 onStart: () =>
//                 {
//                     // allow replay
//                     OnGameBeginReal();
//                 },
//                 delayButton: redoDelaySeconds > 0f,
//                 delaySeconds: Mathf.Max(0f, redoDelaySeconds)
//             );
//         }

//         private void StartKoalaAfterGameDialogue()
//         {
//             StartCoroutine(CoStartKoalaDialogueNextFrame());
//         }

//         private IEnumerator CoStartKoalaDialogueNextFrame()
//         {
//             yield return null;

//             var koala = SafeFindByTag("NPC_Koala") ?? GameObject.Find("Koala");

//             var handler = koala
//                 ? koala.GetComponentInChildren<KoalaDialogueHandler>(true)
//                 : FindObjectsOfType<KoalaDialogueHandler>(true).FirstOrDefault();

//             if (!handler) yield break;

//             if (koala && !koala.activeInHierarchy)
//                 koala.SetActive(true);

//             handler.StartAfterMoxo();
//         }

//         private void SetCardsActiveSafe(bool on)
//         {
//             var cards = GetComponent("CardsActive");
//             if (cards == null)
//             {
//                 Debug.Log($"[MOXO] CardsActive {(on ? "enable" : "disable")} skipped (component missing).");
//                 return;
//             }

//             try
//             {
//                 var m = cards.GetType().GetMethod("SetCardsActive", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
//                 if (m != null)
//                 {
//                     m.Invoke(cards, new object[] { on });
//                 }
//                 else
//                 {
//                     var start = cards.GetType().GetMethod("StartCards", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
//                     var stop = cards.GetType().GetMethod("StopCards", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
//                     if (on) start?.Invoke(cards, null); else stop?.Invoke(cards, null);
//                 }
//             }
//             catch (Exception e)
//             {
//                 Debug.LogWarning($"[MOXO] CardsActive toggle threw: {e.Message}");
//             }
//         }

//         private void NudgeActiveRunnerStart()
//         {
//             var runner = FindObjectsOfType<MonoBehaviour>(true)
//                          .FirstOrDefault(mb => mb && mb.isActiveAndEnabled && mb.gameObject.activeInHierarchy
//                                             && mb.GetType().Name == "ChangeShapes");

//             if (!runner)
//             {
//                 Debug.LogWarning("[MOXO] No active ChangeShapes found to start.");
//                 return;
//             }

//             var t = runner.GetType();

//             string[] starters = { "ForceStart", "BeginNow", "StartNow" };
//             foreach (var name in starters)
//             {
//                 var m = t.GetMethod(name, BindingFlags.Instance | BindingFlags.Public);
//                 if (m != null && m.GetParameters().Length == 0)
//                 {
//                     try { Debug.Log($"[MOXO] Runner.{name}() on {runner.gameObject.name}"); m.Invoke(runner, null); return; }
//                     catch (Exception e) { Debug.LogWarning($"[MOXO] Runner.{name} threw: {e.Message}"); }
//                 }
//             }

//             var co = t.GetMethod("CoStartAfterIntro", BindingFlags.Instance | BindingFlags.NonPublic);
//             if (co != null && typeof(IEnumerator).IsAssignableFrom(co.ReturnType))
//             {
//                 try
//                 {
//                     var enumerator = (IEnumerator)co.Invoke(runner, null);
//                     Debug.Log($"[MOXO] Runner.StartCoroutine(CoStartAfterIntro) on {runner.gameObject.name}");
//                     runner.StartCoroutine(enumerator);
//                     return;
//                 }
//                 catch (Exception e) { Debug.LogWarning($"[MOXO] Runner.CoStartAfterIntro threw: {e.Message}"); }
//             }

//             Debug.LogWarning("[MOXO] Could not start ChangeShapes: no known entry points found.");
//         }

//         private static GameObject SafeFindByTag(string tag)
//         {
//             try
//             {
//                 var arr = GameObject.FindGameObjectsWithTag(tag);
//                 return (arr != null && arr.Length > 0) ? arr[0] : null;
//             }
//             catch (UnityException) { return null; }
//         }

//         #endregion
//     }
// }

using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MoxoCPT
{
    public class MoxoCPTManager : MonoBehaviour
    {
        public static MoxoCPTManager Instance;

        [HideInInspector] public bool isGameOver;

        [Header("Optional Systems")]
        [SerializeField] private DistractorSystem distractors;
        [SerializeField] private EyeTrackLogger eyeLogger;

        [Header("Camera Acquire")]
        [Tooltip("How long we retry to find the island rig after entering CPT.")]
        [SerializeField] private float cameraAcquireTimeoutSeconds = 2.0f;
        [SerializeField] private bool logCamera = true;
        [SerializeField] private float maxAnchorDistanceFallback = 80f;

        [Header("Safeguard (Replay If No Real Participation)")]
        [SerializeField] private bool enableSafeguard = true;
        [SerializeField] private string redoTitle = "Let's try that again";
        [SerializeField, TextArea]
        private string redoBody =
            "It looks like you didn't respond or missed every target.\nPress Space to replay the test.";
        [SerializeField] private float redoDelaySeconds = 1.5f;

        // NEW: second button (replay training)
        [Header("Safeguard - Optional Replay Training Button (NEW)")]
        [SerializeField] private bool allowReplayTrainingButton = true;
        [SerializeField] private string replayTrainingTitle = "Want to practice again?";
        [SerializeField, TextArea]
        private string replayTrainingBody =
            "Use Left/Right to choose:\n- Start (replay the test)\n- Replay Training (practice again first)";
        [Tooltip("Optional voice over clip to play when the replay / replay-training prompt is shown.")]
        [SerializeField] private AudioClip replayPromptVoice;

        // runtime
        private bool _running = false;
        private bool _eyeLogging = false;
        private int _cptKeyPresses = 0;

        private Coroutine _cameraCo;
        private bool _moxoCameraActive = false;

        // NEW: lock GameState to CPT while running / showing results / showing replay prompt
        private bool _lockStateToCpt = false;
        private Coroutine _stateLockCo;

        // NEW: prevent double-start while training flow runs
        private Coroutine _replayTrainingCo;

        // ==================================================
        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void OnEnable()
        {
            Instance = this;
            GameManager.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameManager.OnGameStateChanged -= OnGameStateChanged;
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // Track Space/A presses only during real CPT run
            if (!_running) return;
            if (GameManager.Instance == null || GameManager.Instance.State != GameManager.GameState.CPT) return;

            var kb = Keyboard.current;
            var gp = Gamepad.current;

            bool pressedSpace = kb != null && kb.spaceKey.wasPressedThisFrame;
            bool pressedA = gp != null && gp.buttonSouth.wasPressedThisFrame;

            if (pressedSpace || pressedA)
                _cptKeyPresses++;
        }

        #endregion

        // ==================================================
        #region State Lock (CPT)

        private void SetCptLock(bool on)
        {
            _lockStateToCpt = on;

            if (!on)
            {
                if (_stateLockCo != null) StopCoroutine(_stateLockCo);
                _stateLockCo = null;
            }
        }

        private void OnGameStateChanged(GameManager.GameState newState)
        {
            if (!_lockStateToCpt) return;

            // Allow Teleport if your flow needs it, otherwise force CPT always
            if (newState == GameManager.GameState.Teleport) return;

            if (newState != GameManager.GameState.CPT)
            {
                if (logCamera)
                    Debug.LogWarning($"[MOXO] CPT LOCK: detected state change to {newState}. Forcing back to CPT.");

                if (_stateLockCo != null) StopCoroutine(_stateLockCo);
                _stateLockCo = StartCoroutine(CoForceBackToCptNextFrame());
            }
        }

        private IEnumerator CoForceBackToCptNextFrame()
        {
            yield return null;

            if (!_lockStateToCpt) yield break;
            if (GameManager.Instance == null) yield break;

            // Use ForceUpdateGameState if you have it; else fallback to UpdateGameState
            try
            {
                var gm = GameManager.Instance;

                // Prefer ForceUpdateGameState if present
                var mi = gm.GetType().GetMethod("ForceUpdateGameState",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (mi != null)
                    mi.Invoke(gm, new object[] { GameManager.GameState.CPT });
                else
                    gm.UpdateGameState(GameManager.GameState.CPT);
            }
            catch
            {
                GameManager.Instance.UpdateGameState(GameManager.GameState.CPT);
            }

            // Re-acquire camera if someone caused an exit
            if (!_moxoCameraActive)
                StartEnterMoxoCameraWhenReady();
        }

        #endregion

        // ==================================================
        #region Camera Handling

        private static string Norm(string s)
        {
            return string.IsNullOrWhiteSpace(s) ? string.Empty : s.Trim().ToUpperInvariant();
        }

        private string CurrentIslandId()
        {
            var travel = IslandTravelManager.I;
            if (travel == null || travel.CurrentIsland == null) return "";
            return Norm(travel.CurrentIsland.islandId);
        }

        private IslandCameraRig FindRigForCurrentIslandOrFallback(out string reason)
        {
            reason = "";

            string islandId = CurrentIslandId();
            var rigs = FindObjectsOfType<IslandCameraRig>(true);

            if (string.IsNullOrEmpty(islandId))
            {
                reason = "CurrentIslandId empty -> fallback nearest rig.";
                return FindNearestActiveRig(rigs, out _);
            }

            var strict = rigs.FirstOrDefault(r =>
                r &&
                r.gameObject.activeInHierarchy &&
                Norm(r.islandId) == islandId &&
                r.moxoCameraAnchor != null);

            if (strict != null)
            {
                reason = $"STRICT match '{islandId}'.";
                return strict;
            }

            var idButInactive = rigs.FirstOrDefault(r =>
                r && Norm(r.islandId) == islandId && r.moxoCameraAnchor != null);

            if (idButInactive != null && !idButInactive.gameObject.activeInHierarchy)
            {
                reason = $"Rig exists for '{islandId}' but is inactive -> island moxoRoot disabled.";
                return null;
            }

            var nearest = FindNearestActiveRig(rigs, out float bestDist);
            if (nearest != null && bestDist <= Mathf.Max(0.1f, maxAnchorDistanceFallback))
            {
                reason = $"FALLBACK nearest active rig dist={bestDist:0.00}.";
                return nearest;
            }

            reason = $"No rig found for '{islandId}'.";
            return null;
        }

        private IslandCameraRig FindNearestActiveRig(IslandCameraRig[] rigs, out float bestDist)
        {
            bestDist = float.PositiveInfinity;

            var player = FindObjectOfType<DesktopArrowController>(true);
            Vector3 p = player ? player.transform.position : Vector3.zero;

            IslandCameraRig best = null;

            foreach (var r in rigs)
            {
                if (!r) continue;
                if (!r.gameObject.activeInHierarchy) continue;
                if (!r.moxoCameraAnchor) continue;

                float d = Vector3.Distance(p, r.moxoCameraAnchor.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = r;
                }
            }

            return best;
        }

        private void StartEnterMoxoCameraWhenReady()
        {
            if (_cameraCo != null)
                StopCoroutine(_cameraCo);

            _cameraCo = StartCoroutine(CoEnterMoxoCameraWhenReady());
        }

        private IEnumerator CoEnterMoxoCameraWhenReady()
        {
            _moxoCameraActive = false;
            yield return null;

            float elapsed = 0f;
            float timeout = Mathf.Max(0.1f, cameraAcquireTimeoutSeconds);

            while (elapsed < timeout)
            {
                if (GameManager.Instance == null || GameManager.Instance.State != GameManager.GameState.CPT)
                    yield break;

                var rig = FindRigForCurrentIslandOrFallback(out string reason);
                if (rig != null)
                {
                    var director = FindObjectOfType<MoxoCameraDirector>(true);
                    if (!director)
                    {
                        Debug.LogWarning("[MOXO-CAM] MoxoCameraDirector missing.");
                        yield break;
                    }

                    director.EnterMoxoView(rig.moxoCameraAnchor);
                    _moxoCameraActive = true;

                    if (logCamera)
                        Debug.Log($"[MOXO-CAM] ENTER OK -> rig='{rig.name}', id='{rig.islandId}', reason={reason}");

                    yield break;
                }

                elapsed += 0.1f;
                yield return new WaitForSecondsRealtime(0.1f);
            }

            Debug.LogWarning("[MOXO-CAM] Camera acquire timed out.");
        }

        private void ExitMoxoCamera()
        {
            if (_cameraCo != null)
            {
                StopCoroutine(_cameraCo);
                _cameraCo = null;
            }

            if (_moxoCameraActive)
            {
                var director = FindObjectOfType<MoxoCameraDirector>(true);
                director?.ExitMoxoView();
            }

            _moxoCameraActive = false;
        }

        #endregion

        // ==================================================
        #region Game Flow

        public void ForceEndAndCleanup()
        {
            SetCptLock(false);

            ExitMoxoCamera();

            try { StopAllCoroutines(); } catch { }
            Time.timeScale = 1f;

            distractors?.StopSystem();

            if (_eyeLogging)
            {
                (eyeLogger ?? EyeTrackLogger.I)?.EndSession();
                _eyeLogging = false;
            }

            SetCardsActiveSafe(false);

            _running = false;
            isGameOver = true;

            GameManager.Instance?.UpdateGameState(GameManager.GameState.Narrative);
        }

        public void OnGameBegin() => OnGameBeginReal();

        private void OnGameBeginReal()
        {
            if (_running)
            {
                Debug.LogWarning("[MOXO] CPT already running.");
                return;
            }

            // NEW: cancel training flow if user starts CPT anyway
            if (_replayTrainingCo != null)
            {
                StopCoroutine(_replayTrainingCo);
                _replayTrainingCo = null;
            }

            try { StopAllCoroutines(); } catch { }

            _running = true;
            isGameOver = false;
            _cptKeyPresses = 0;

            CPTScoreRuntime.I?.ResetScore();
            SetCardsActiveSafe(true);

            LoggingReport.CreateReportCSV();

            // LOCK CPT immediately (prevents other scripts from pulling to Explore/Narrative)
            SetCptLock(true);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdateGameState(GameManager.GameState.CPT);
                Debug.Log("[MOXO] GameState set to CPT (real run).");
            }
            else
            {
                Debug.LogError("[MoxoCPTManager] Cannot set state: GameManager.Instance is null.");
            }

            StartEnterMoxoCameraWhenReady();

            KoalaAnimBus.EnterCptAll();

            string pid = GameManager.Instance?.ParticipantId;
            if (string.IsNullOrWhiteSpace(pid))
                pid = "P_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

            eyeLogger ??= EyeTrackLogger.I;
            if (eyeLogger && !_eyeLogging)
            {
                eyeLogger.BeginSession(pid);
                _eyeLogging = true;
            }

            NudgeActiveRunnerStart();
        }

        public void OnGameEnd()
        {
            KoalaAnimBus.BroadcastToActiveKoalas(d => d.OnGameEnd());

            if (!_running && isGameOver)
                return;

            _running = false;
            isGameOver = true;

            distractors?.StopSystem();

            if (_eyeLogging)
            {
                (eyeLogger ?? EyeTrackLogger.I)?.EndSession();
                _eyeLogging = false;
            }

            // Keep CPT + keep MOXO camera here.
            // We only exit camera + unlock CPT when player presses Continue on results.
            SetCardsActiveSafe(false);

            int hit = CPTScoreRuntime.I?.CorrectTargetsHit ?? 0;
            int total = CPTScoreRuntime.I?.TotalTargets ?? 0;

            int falseAlarms = CPTScoreRuntime.I?.FalseAlarms ?? 0;
            int totalDistractors = CPTScoreRuntime.I?.TotalDistractors ?? 0;

            Debug.Log($"[MOXO] Targets {hit}/{total}, False {falseAlarms}/{totalDistractors}, Keys {_cptKeyPresses}");

            if (enableSafeguard)
            {
                bool noButtonPresses = (_cptKeyPresses <= 0);
                bool missedAllTargets = (hit <= 0 && total > 0);

                if (noButtonPresses || missedAllTargets)
                {
                    ShowReplayPrompt_KEEP_CPT();
                    return;
                }
            }

            var intro = IntroScreen.Instance ?? FindObjectOfType<IntroScreen>(true);
            if (intro != null)
            {
                // IMPORTANT: hide training button on normal results
                intro.SetReplayTrainingVisible(false);

                // Show island reward (if configured) then the results screen.
                // Redo path (ShowReplayPrompt_KEEP_CPT) skips this entirely.
                StartCoroutine(CoShowRewardThenResults(intro, hit, total, falseAlarms, totalDistractors));
            }
            else
            {
                Debug.LogWarning("[MOXO] IntroScreen not found; switching state immediately.");

                SetCptLock(false);
                ExitMoxoCamera();

                GameManager.Instance?.UpdateGameState(GameManager.GameState.Narrative);
                StartKoalaAfterGameDialogue();
            }
        }

        #endregion

        // ==================================================
        #region Helpers

        // ---------- Reward helpers ----------

        /// <summary>Returns the IntroAnchor that matches the currently active island, or null.</summary>
        private IntroAnchor GetCurrentIntroAnchor()
        {
            string id = CurrentIslandId();
            if (string.IsNullOrWhiteSpace(id)) return null;

            foreach (var ia in FindObjectsOfType<IntroAnchor>(true))
            {
                if (ia && !string.IsNullOrWhiteSpace(ia.islandId) &&
                    string.Equals(ia.islandId.Trim(), id.Trim(), System.StringComparison.OrdinalIgnoreCase))
                    return ia;
            }
            return null;
        }

        /// <summary>
        /// Shows the island's reward object (if configured), waits for it to finish,
        /// then shows the results screen. Only called on the normal (non-redo) completion path.
        /// </summary>
        private IEnumerator CoShowRewardThenResults(
            IntroScreen intro, int hit, int total, int falseAlarms, int totalDistractors)
        {
            var anchor = GetCurrentIntroAnchor();

            if (anchor != null && anchor.rewardObject != null)
            {
                anchor.rewardObject.SetActive(true);

                // Play sound via the reward object's own AudioSource if present,
                // otherwise fall back to a one-shot at the anchor's world position.
                float duration = anchor.rewardDuration;

                if (anchor.rewardSound != null)
                {
                    var src = anchor.rewardObject.GetComponent<AudioSource>();
                    if (src != null)
                    {
                        src.clip = anchor.rewardSound;
                        src.Play();
                    }
                    else
                    {
                        AudioSource.PlayClipAtPoint(anchor.rewardSound, anchor.transform.position);
                    }

                    if (duration <= 0f)
                        duration = anchor.rewardSound.length;
                }

                if (duration <= 0f) duration = 2f; // silent reward: show for 2 s

                yield return new WaitForSecondsRealtime(duration);

                // Reward stays active — hide it yourself (e.g. from the results Continue callback)
                // if you need it gone after the player continues.
            }

            // Results screen (with character enabled via ShowInstant → localCharacter.SetActive)
            intro.ShowResults(hit, total, falseAlarms, totalDistractors, () =>
            {
                SetCptLock(false);
                ExitMoxoCamera();

                KoalaAnimBus.BroadcastToActiveKoalas(d => d.OnResultsContinue());
                Debug.Log("[MOXO] Results Continue → Narrative.");

                GameManager.Instance?.UpdateGameState(GameManager.GameState.Narrative);
                StartKoalaAfterGameDialogue();
            });
        }

        // NEW: run training again then show the "Ready" panel
        private void ReplayTrainingThenShowReady()
        {
            if (_replayTrainingCo != null)
            {
                StopCoroutine(_replayTrainingCo);
                _replayTrainingCo = null;
            }
            _replayTrainingCo = StartCoroutine(CoReplayTrainingThenReady());
        }

        private IEnumerator CoReplayTrainingThenReady()
        {
            // Training should NOT be locked to CPT or the lock will fight PrepareCPT.
            SetCptLock(false);
            ExitMoxoCamera();

            // Force PrepareCPT so your TrainingCPTRunner won't instantly abort.
            GameManager.Instance?.UpdateGameState(GameManager.GameState.PrepareCPT);
            yield return null;

            string islandId = CurrentIslandId();

            // Find a training runner for this island (prefer BelongsToIsland if available)
            TrainingCPTRunner runner = null;
            var all = FindObjectsOfType<TrainingCPTRunner>(true);

            foreach (var r in all)
            {
                if (!r) continue;
                try
                {
                    var m = r.GetType().GetMethod("BelongsToIsland", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (m != null)
                    {
                        bool ok = (bool)m.Invoke(r, new object[] { islandId });
                        if (ok) { runner = r; break; }
                    }
                }
                catch { }
            }
            if (!runner) runner = all.FirstOrDefault(r => r != null);

            if (!runner)
            {
                Debug.LogWarning("[MOXO] ReplayTraining: no TrainingCPTRunner found. Showing Ready anyway.");
            }
            else
            {
                // Allow rerun even if it was marked completed this session
                try { TrainingCPTRunner.ClearCompletedForIsland(islandId); } catch { }

                yield return StartCoroutine(runner.RunTrainingForActiveIsland(islandId));
            }

          
            // After training: show the ready-to-start panel (this arms the one-shot gate)
            var intro = IntroScreen.Instance ?? FindObjectOfType<IntroScreen>(true);
            if (intro)
            {
                // IMPORTANT: ensure replay training button is hidden on the Ready screen
                intro.SetReplayTrainingVisible(false);

                // Read island-specific ready text — same fallback logic as IslandTravelManager.
                var currentIsland = IslandTravelManager.I?.CurrentIsland;
                var readyTitle = IntroScreen.Fallback(currentIsland?.readyIntroTitle, "Ready to start?");
                var readyBody  = IntroScreen.Fallback(currentIsland?.readyIntroBody,  "Press Space to begin the real test.");
                intro.ShowReadyAfterTraining(readyTitle, readyBody);

                // ---- NEW: wait until the Ready panel closes, then ensure CPT starts ----
                float timeout = 45f;
                while (timeout > 0f)
                {
                    bool hidden =
                        !intro.isActiveAndEnabled ||
                        !intro.gameObject.activeInHierarchy ||
                        (intro.GetComponent<CanvasGroup>() && intro.GetComponent<CanvasGroup>().alpha <= 0.001f);

                    if (hidden) break;

                    timeout -= Time.unscaledDeltaTime;
                    yield return null;
                }

                // If the player dismissed Ready but CPT didn't start for any reason, start it now.
                if (GameManager.Instance && GameManager.Instance.State != GameManager.GameState.CPT)
                {
                    Debug.LogWarning("[MOXO] ReplayTraining: Ready panel finished but state is not CPT → forcing OnGameBegin().");
                    OnGameBeginReal();
                }
            }
            else
            {
                // No intro screen? Start immediately to avoid getting stuck in PrepareCPT.
                Debug.LogWarning("[MOXO] ReplayTraining: IntroScreen missing after training → forcing OnGameBegin().");
                OnGameBeginReal();
            }

            _replayTrainingCo = null;

        }

        private void ShowReplayPrompt_KEEP_CPT()
        {
            Debug.Log("[MOXO] Safeguard triggered → offering replay (stay in CPT, keep MOXO cam).");

            var intro = IntroScreen.Instance ?? FindObjectOfType<IntroScreen>(true);
            if (!intro)
            {
                Debug.LogWarning("[MOXO] No IntroScreen found for replay prompt. Restarting immediately.");
                OnGameBeginReal();
                return;
            }

            intro.SetReplayTrainingVisible(false);

            // Keep CPT lock ON here (we are still "in CPT context" until a choice is made).

            // Show text prompt (2-button body if enabled)
            if (allowReplayTrainingButton)
                intro.ShowTextOnly(replayTrainingTitle, replayTrainingBody);
            else
                intro.ShowTextOnly(redoTitle, redoBody);

            // Optional voice over for the replay / training choice prompt
            if (replayPromptVoice)
                intro.PlayVoice(replayPromptVoice);

            // Start button -> replay test
            intro.ArmOnStart(
                onStart: () =>
                {
                    OnGameBeginReal();
                },
                delayButton: redoDelaySeconds > 0f,
                delaySeconds: Mathf.Max(0f, redoDelaySeconds)
            );

            // NEW: show + wire replay-training button ONLY on this safeguard screen
            if (allowReplayTrainingButton)
            {
                intro.SetReplayTrainingVisible(true);
                intro.ArmReplayTraining(() =>
                {
                    ReplayTrainingThenShowReady();
                });
            }
            else
            {
                intro.SetReplayTrainingVisible(false);
            }
        }

        private void StartKoalaAfterGameDialogue()
        {
            StartCoroutine(CoStartKoalaDialogueNextFrame());
        }

        private IEnumerator CoStartKoalaDialogueNextFrame()
        {
            yield return null;

            var koala = SafeFindByTag("NPC_Koala") ?? GameObject.Find("Koala");

            var handler = koala
                ? koala.GetComponentInChildren<KoalaDialogueHandler>(true)
                : FindObjectsOfType<KoalaDialogueHandler>(true).FirstOrDefault();

            if (!handler) yield break;

            if (koala && !koala.activeInHierarchy)
                koala.SetActive(true);

            handler.StartAfterMoxo();
        }

        private void SetCardsActiveSafe(bool on)
        {
            var cards = GetComponent("CardsActive");
            if (cards == null)
            {
                Debug.Log($"[MOXO] CardsActive {(on ? "enable" : "disable")} skipped (component missing).");
                return;
            }

            try
            {
                var m = cards.GetType().GetMethod("SetCardsActive", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m != null)
                {
                    m.Invoke(cards, new object[] { on });
                }
                else
                {
                    var start = cards.GetType().GetMethod("StartCards", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var stop = cards.GetType().GetMethod("StopCards", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (on) start?.Invoke(cards, null); else stop?.Invoke(cards, null);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MOXO] CardsActive toggle threw: {e.Message}");
            }
        }

        private void NudgeActiveRunnerStart()
        {
            var runner = FindObjectsOfType<MonoBehaviour>(true)
                         .FirstOrDefault(mb => mb && mb.isActiveAndEnabled && mb.gameObject.activeInHierarchy
                                            && mb.GetType().Name == "ChangeShapes");

            if (!runner)
            {
                Debug.LogWarning("[MOXO] No active ChangeShapes found to start.");
                return;
            }

            var t = runner.GetType();

            string[] starters = { "ForceStart", "BeginNow", "StartNow" };
            foreach (var name in starters)
            {
                var m = t.GetMethod(name, BindingFlags.Instance | BindingFlags.Public);
                if (m != null && m.GetParameters().Length == 0)
                {
                    try { Debug.Log($"[MOXO] Runner.{name}() on {runner.gameObject.name}"); m.Invoke(runner, null); return; }
                    catch (Exception e) { Debug.LogWarning($"[MOXO] Runner.{name} threw: {e.Message}"); }
                }
            }

            var co = t.GetMethod("CoStartAfterIntro", BindingFlags.Instance | BindingFlags.NonPublic);
            if (co != null && typeof(IEnumerator).IsAssignableFrom(co.ReturnType))
            {
                try
                {
                    var enumerator = (IEnumerator)co.Invoke(runner, null);
                    Debug.Log($"[MOXO] Runner.StartCoroutine(CoStartAfterIntro) on {runner.gameObject.name}");
                    runner.StartCoroutine(enumerator);
                    return;
                }
                catch (Exception e) { Debug.LogWarning($"[MOXO] Runner.CoStartAfterIntro threw: {e.Message}"); }
            }

            Debug.LogWarning("[MOXO] Could not start ChangeShapes: no known entry points found.");
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

        #endregion
    }
}

