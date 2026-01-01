// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;
// using Random = UnityEngine.Random;

// namespace MoxoCPT
// {
//     public class ChangeShapes : MonoBehaviour
//     {
//         [SerializeField] private float _secondsTillGameStarts = 5.0f;

//         // Hami: plug ExploreHintUI
//         [Header("Countdown UI")]
//         [SerializeField] private ExploreHintUI countdownUI;
//         [SerializeField] private string countdownStartText = "Get ready…";
//         [SerializeField] private string countdownEndText   = "Go!";
//         //Hami: End change


//         private Report _report;
//         private Dictionary<int, int> _counts;

//         // change this number for how many total stimuli trials are needed.
//         private const int NUM_TRIALS = 59;
//         private const int MAX_NUM_NONTARGET = 5;

//         private void Start()
//         {
//             _counts = new Dictionary<int, int>();
//             _report = new Report();

//             StartCoroutine("Change");
//         }

//         IEnumerator Change()
//         {
//             // Hami: Show “starting in …” countdown on the same UI widget
//             if (countdownUI != null)
//             {
//                 countdownUI.ShowExploreHint(_secondsTillGameStarts, "Get ready…", ""); // <- no end text
//             }
//             yield return new WaitForSeconds(_secondsTillGameStarts);
//             //Hami: End change
            
            
//             // for the first card
//             yield return new WaitForSeconds(_secondsTillGameStarts);
//             var newCard = GetNextObj();
//             TurnCardOn(newCard);
//             Cards.Instance.UpdateCurCard(newCard);
            
//             for (var i = 1; i < NUM_TRIALS; i++)
//             {
//                 var duration = GetCardDuration();
                
//                 _report.ResetReport();
//                 StartCoroutine(Interact.StartReport(_report, duration * 2));
//                 yield return new WaitForSeconds(duration);
//                 newCard = GetNextObj();
//                 TurnCardOff(Cards.Instance.curCard);
                
//                 yield return new WaitForSeconds(duration);
//                 TurnCardOn(newCard);
//                 Cards.Instance.UpdateCurCard(newCard);
//             }
            
//             TurnCardOff(Cards.Instance.curCard);


//             foreach (var num in _counts) Debug.Log(num);
//         }

//         private Transform GetNextObj()
//         {
//             var index = Random.Range(0, Cards.Instance.numCards);
            
//             AddToDict(index);
            
//             return Cards.Instance.cardArr[index];
//         }

//         private void AddToDict(int index)
//         {
//             // a lot of edge case checking to make sure not to access invalid dictionary index
//             if (!_counts.ContainsKey(index)) _counts.Add(index, 1);
//             else
//             {
//                 while (index != 0 && _counts[index] >= MAX_NUM_NONTARGET)
//                 {
//                     index = Random.Range(0, Cards.Instance.numCards);
//                     if (!_counts.ContainsKey(index)) _counts.Add(index, 0);
//                 }

//                 _counts[index] += 1;
//             }
//         }

//         private float GetCardDuration()
//         {
//             return Cards.Instance.cardDuration[Random.Range(0, Cards.Instance.numDurations)];
//         }

//         private void TurnCardOn(Transform card)
//         {
//             card.gameObject.SetActive(true);
//             Cards.Instance.isActive = true;
//         }

//         private void TurnCardOff(Transform card)
//         {
//             card.gameObject.SetActive(false);
//             Cards.Instance.isActive = false;
//         }
//     }
// }

// Hami: Wait for the intro screen + reference distractor
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI; // Canvas
using Random = UnityEngine.Random;

namespace MoxoCPT
{
    public class ChangeShapes : MonoBehaviour
    {
        [Header("Pre-start")]
        [SerializeField] private float _secondsTillGameStarts = 5.0f;

        [Header("Countdown UI")]
        [SerializeField] private ExploreHintUI countdownUI;
        [SerializeField] private string countdownStartText = "Get ready…";
        [SerializeField] private string countdownEndText   = "Go!";

        [Header("Countdown UI Placement")]
        [Tooltip("Sorting order for the world-space countdown canvas.")]
        [SerializeField] private int countdownSortingOrder = 90;
        [Tooltip("If true, rotate countdown to face the player; otherwise use anchor rotation.")]
        [SerializeField] private bool countdownFacePlayer = false;

        [Header("UI Timing")]
        [Tooltip("Extra delay after the countdown UI ends, before the first card appears.")]
        [SerializeField] private float postCountdownDelay = 0.35f;

        [Header("Exact counts")]
        [SerializeField] private int totalTrials  = 59;
        [SerializeField] private int totalTargets = 36;
        [SerializeField] private int totalNonTargets = 23;

        [Header("Distractors")]
        [SerializeField] private DistractorSystem distractors;

        private Report _report;
        private List<Transform> _schedule;
        private bool _hasStarted = false;

        private void Start() { /* wait for CPT state */ }

        // Eyetracking
        private static EyeTrackLogger GetLogger()
            => EyeTrackLogger.I ?? FindObjectOfType<EyeTrackLogger>(true);

        private void Update()
        {
            if (_hasStarted) return;

            if (GameManager.Instance != null &&
                GameManager.Instance.State == GameManager.GameState.CPT)
            {
                _hasStarted = true;
                StartCoroutine(CoStart());
            }
        }

        private IEnumerator CoStart()
        {
            // Adopt island-specific countdown (IslandData.countdownSeconds)
            var island = IslandTravelManager.I ? IslandTravelManager.I.CurrentIsland : null;
            if (island != null && island.countdownSeconds > 0f)
                _secondsTillGameStarts = island.countdownSeconds;

            if (countdownUI == null)
            {
                countdownUI = FindObjectOfType<ExploreHintUI>(true);
                Debug.Log($"[ChangeShapes] Auto-found countdownUI = {(countdownUI ? countdownUI.name : "NULL")}");
            }

            // Place countdown UI using anchors
            ApplyCountdownAnchorAndPlace(island);

            if (totalTargets + totalNonTargets != totalTrials)
            {
                Debug.LogError($"ChangeShapes: totalTargets({totalTargets}) + totalNonTargets({totalNonTargets}) != totalTrials({totalTrials}).");
                yield break;
            }

            yield return new WaitUntil(() =>
                Cards.Instance != null &&
                Cards.Instance.cardArr != null &&
                Cards.Instance.cardArr.Length > 0
            );

            if (!BuildSchedule()) yield break;
            _report = new Report();

            yield return StartCoroutine(Change());
        }

        private bool BuildSchedule()
        {
            var allCards = Cards.Instance.cardArr;
            if (allCards == null || allCards.Length == 0)
            {
                Debug.LogError("ChangeShapes: Cards.Instance.cardArr is empty or null.");
                return false;
            }

            var targets    = allCards.Where(t => t.CompareTag("Target")).ToArray();
            var nontargets = allCards.Where(t => t.CompareTag("NonTarget")).ToArray();

            if (targets.Length == 0 || nontargets.Length == 0)
            {
                Debug.LogError("ChangeShapes: Need at least one Target and one NonTarget card in children.");
                return false;
            }

            _schedule = new List<Transform>(totalTrials);

            for (int i = 0; i < totalTargets; i++)
                _schedule.Add(targets[Random.Range(0, targets.Length)]);
            for (int i = 0; i < totalNonTargets; i++)
                _schedule.Add(nontargets[Random.Range(0, nontargets.Length)]);

            // Fisher–Yates
            for (int i = 0; i < _schedule.Count; i++)
            {
                int j = Random.Range(i, _schedule.Count);
                (_schedule[i], _schedule[j]) = (_schedule[j], _schedule[i]);
            }

            return true;
        }

        // Keep for external control if needed
        public void SetCountdown(float seconds)
        {
            if (seconds > 0f) _secondsTillGameStarts = seconds;
        }

        private IEnumerator Change()
        {
            // Show countdown (with end text)
            if (countdownUI != null)
            {
                if (!countdownUI.gameObject.activeSelf) countdownUI.gameObject.SetActive(true);
                Debug.Log($"[ChangeShapes] countdownUI={(countdownUI ? countdownUI.name : "NULL")}");
                countdownUI.ShowExploreHint(_secondsTillGameStarts, countdownStartText, countdownEndText);
            }

            // Wait main countdown…
            yield return new WaitForSeconds(_secondsTillGameStarts);

            // Hide countdown BEFORE Trial 0 begins
            HideCountdownUI();

            // Small grace so the hide fully applies / fades
            if (postCountdownDelay > 0f)
                yield return new WaitForSeconds(postCountdownDelay);

            Debug.Log($"[ChangeShapes] Starting trials. countdownUI={(countdownUI ? "OK" : "NULL")}");

            // Start distractors aligned with Trial 0 (optional)
            if (distractors != null)
                distractors.StartSystem();

            // Trial 0
            var firstCard = _schedule[0];
            TurnCardOn(firstCard);
            Cards.Instance.UpdateCurCard(firstCard);

            // Eyetracking – mark trial 0
            var logger = GetLogger();
            if (logger != null && logger.IsLogging) logger.MarkNewTrial(0);

            for (int trial = 1; trial < totalTrials; trial++)
            {
                float duration = GetCardDuration();

                _report.ResetReport();
                StartCoroutine(Interact.StartReport(_report, duration * 2f));

                yield return new WaitForSeconds(duration);
                TurnCardOff(Cards.Instance.curCard);

                yield return new WaitForSeconds(duration);
                var nextCard = _schedule[trial];
                TurnCardOn(nextCard);
                Cards.Instance.UpdateCurCard(nextCard);

                logger = GetLogger();
                if (logger != null && logger.IsLogging) logger.MarkNewTrial(trial);
            }

            TurnCardOff(Cards.Instance.curCard);

            if (distractors != null)
                distractors.StopSystem();

            // Results
            var tracker = CPTScoreRuntime.I;
            if (tracker != null)
            {
                var intro = FindObjectOfType<IntroScreen>(true);
                if (intro != null)
                {
                    intro.ShowResults(
                        tracker.CorrectTargetsHit,
                        tracker.TotalTargets
                    );
                }
            }
        }

        private float GetCardDuration()
            => Cards.Instance.cardDuration[Random.Range(0, Cards.Instance.numDurations)];

        private void TurnCardOn(Transform card)
        {
            card.gameObject.SetActive(true);
            Cards.Instance.isActive = true;
        }

        private void TurnCardOff(Transform card)
        {
            if (card == null) return;
            card.gameObject.SetActive(false);
            Cards.Instance.isActive = false;
        }

        // ---------- Countdown Anchor placement ----------
        private void ApplyCountdownAnchorAndPlace(IslandData island)
        {
            if (!countdownUI) return;

            // Try to find matching CountdownAnchor by islandId
            CountdownAnchor anchor = null;
            var islandId = island != null ? island.islandId?.Trim().ToUpperInvariant() : null;
            if (!string.IsNullOrEmpty(islandId))
            {
                anchor = FindObjectsOfType<CountdownAnchor>(true)
                    .FirstOrDefault(a => a.islandId == islandId);

                // Per-island override for seconds
                if (anchor && anchor.overrideCountdownSeconds > 0f)
                    _secondsTillGameStarts = anchor.overrideCountdownSeconds;
            }

            // Select placement target
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

            // Place/rotate the countdown UI (world-space canvas assumed)
            var t = countdownUI.transform;
            t.position = target.position;
            t.rotation = target.rotation;

            if (countdownFacePlayer)
            {
                var pm = FindObjectOfType<PlayerModeManager>(true);
                var player = pm && pm.CurrentPlayerRoot
                    ? pm.CurrentPlayerRoot
                    : (Camera.main ? Camera.main.transform : target);

                t.LookAt(player.position, Vector3.up);
                t.Rotate(0f, 180f, 0f, Space.Self); // face player
            }

            // Ensure canvas uses active rig camera + proper sorting order
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

        // ---------- Hide helper ----------
        private void HideCountdownUI()
        {
            if (!countdownUI) return;

            var cg = countdownUI.GetComponent<CanvasGroup>();
            if (cg)
            {
                cg.alpha = 0f;
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }
            countdownUI.gameObject.SetActive(false);
        }
    }
}
