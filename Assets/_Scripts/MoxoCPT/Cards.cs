// using System.Linq;
// using UnityEngine;

// namespace MoxoCPT
// {
//     /// <summary>
//     /// Deck of CPT cards that lives under a single MOXO rig.
//     /// Designed to be enabled/disabled as islands swap.
//     /// </summary>
//     public class Cards : MonoBehaviour
//     {
//         // Legacy convenience: points to the most recently enabled Cards.
//         // ChangeShapes now binds locally, but other code might still read Instance.
//         public static Cards Instance;

//         // 60% 0.5s, 30% 1s, 10% 3s
//         [Tooltip("Per-trial durations; randomized each trial.")]
//         public float[] cardDuration = { 0.5f,0.5f,0.5f,0.5f,0.5f,0.5f, 1f,1f,1f, 3f };

//         [HideInInspector] public Transform   curCard;
//         [HideInInspector] public Transform[] cardArr;

//         [HideInInspector] public int  numCards;
//         [HideInInspector] public int  numDurations;
//         [HideInInspector] public int  numSeen = 0;
//         [HideInInspector] public bool isActive;

//         // ---------- Lifecycle ----------

//         private void Awake()
//         {
//             // Do NOT destroy duplicates; each island has its own deck.
//             // We rebuild on enable so the deck is always ready when the rig is toggled on.
//         }

//         private void OnEnable()
//         {
//             Instance = this;             // soft/last-active pointer for legacy reads
//             EnsureBuilt();
//             TurnAllCardsOff();           // decks start hidden until ChangeShapes begins
//         }

//         private void OnDisable()
//         {
//             if (Instance == this) Instance = null;
//             // Keep cards off when this rig turns off
//             TurnAllCardsOff();
//         }

//         private void Start()
//         {
//             // If this rig starts active in the scene, also build here.
//             EnsureBuilt();
//             TurnAllCardsOff();
//         }

//         // ---------- Public API (used by manager via reflection) ----------

//         /// <summary>Rebuild internal arrays if missing/out of date.</summary>
//         public void EnsureBuilt()
//         {
//             // Grab only children tagged as Target/NonTarget (include inactive)
//             cardArr = GetComponentsInChildren<Transform>(true)
//                      .Where(t => t && (t.CompareTag("Target") || t.CompareTag("NonTarget")))
//                      .ToArray();

//             numCards     = cardArr.Length;
//             numDurations = cardDuration?.Length ?? 0;
//         }

//         /// <summary>Clear state and hide all cards.</summary>
//         public void ResetDeck()
//         {
//             EnsureBuilt();
//             numSeen   = 0;
//             curCard   = null;
//             TurnAllCardsOff();
//         }

//         /// <summary>Alias for ResetDeck (some callers search this name).</summary>
//         public void ResetCards() => ResetDeck();

//         /// <summary>Alias for ResetDeck (searched by manager reflection).</summary>
//         public void ClearAndLoadTargets() => ResetDeck();

//         /// <summary>Optional island-specific prepare (currently just resets).</summary>
//         public void PrepareForIsland(string islandId) => ResetDeck();

//         /// <summary>
//         /// Convenience used by reflection: toggle all card objects on/off.
//         /// Usually you keep them off until trials start.
//         /// </summary>
//         public void SetCardsActive(bool on)
//         {
//             EnsureBuilt();
//             foreach (var card in cardArr)
//             {
//                 if (card) card.gameObject.SetActive(on);
//             }
//             isActive = on;

//             // If turning off, also clear current pointer.
//             if (!on) curCard = null;
//         }

//         // ---------- Gameplay helpers ----------

//         public void UpdateCurCard(Transform newCard)
//         {
//             curCard = newCard;
//             numSeen++;
//         }

//         public bool IsCurrentCardTarget()
//         {
//             return curCard != null && curCard.CompareTag("Target");
//         }

//         private void TurnAllCardsOff()
//         {
//             if (cardArr == null) return;
//             foreach (var card in cardArr)
//                 if (card) card.gameObject.SetActive(false);

//             isActive = false;
//         }
//     }
// }

using System.Linq;
using UnityEngine;

namespace MoxoCPT
{
    /// <summary>
    /// Deck of CPT cards that lives under a single MOXO rig.
    /// Designed to be enabled/disabled as islands swap.
    /// </summary>
    public class Cards : MonoBehaviour
    {
        // Legacy convenience: points to the most recently enabled Cards.
        // Try to avoid using this in new code; prefer rig-local references.
        public static Cards Instance;

        // 60% 0.5s, 30% 1s, 10% 3s (legacy pool; your balanced-schedule code may ignore this)
        [Tooltip("Per-trial durations; randomized each trial (legacy).")]
        public float[] cardDuration = { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 1f, 1f, 1f, 3f };

        [HideInInspector] public Transform curCard;
        [HideInInspector] public Transform[] cardArr;

        [HideInInspector] public int numCards;
        [HideInInspector] public int numDurations;
        [HideInInspector] public int numSeen = 0;
        [HideInInspector] public bool isActive;

        // ---------- Lifecycle ----------

        private void OnEnable()
        {
            // Safer: warn if multiple decks fight over the global Instance pointer.
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    $"[Cards] Instance changed from '{Instance.name}' -> '{name}'. " +
                    "If any code still uses Cards.Instance, it may read the wrong rig."
                );
            }

            Instance = this;

            EnsureBuilt();
            TurnAllCardsOff(); // decks start hidden until ChangeShapes begins
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
            TurnAllCardsOff();
        }

        private void Start()
        {
            // If this rig starts active in the scene, also build here.
            EnsureBuilt();
            TurnAllCardsOff();
        }

        // ---------- Public API ----------

        /// <summary>Rebuild internal arrays if missing/out of date.</summary>
        public void EnsureBuilt()
        {
            // Grab only children tagged as Target/NonTarget (include inactive)
            cardArr = GetComponentsInChildren<Transform>(true)
                .Where(t => t && (t.CompareTag("Target") || t.CompareTag("NonTarget")))
                .ToArray();

            numCards = cardArr.Length;
            numDurations = cardDuration?.Length ?? 0;
        }

        /// <summary>Clear state and hide all cards.</summary>
        public void ResetDeck()
        {
            EnsureBuilt();
            numSeen = 0;
            curCard = null;
            TurnAllCardsOff();
        }

        public void ResetCards() => ResetDeck();
        public void ClearAndLoadTargets() => ResetDeck();
        public void PrepareForIsland(string islandId) => ResetDeck();

        /// <summary>
        /// Convenience used by reflection: toggle all card objects on/off.
        /// Usually you keep them off until trials start.
        /// </summary>
        public void SetCardsActive(bool on)
        {
            EnsureBuilt();
            foreach (var card in cardArr)
                if (card) card.gameObject.SetActive(on);

            isActive = on;

            // If turning off, also clear current pointer.
            if (!on) curCard = null;
        }

        // ---------- Gameplay helpers ----------

        public void UpdateCurCard(Transform newCard)
        {
            curCard = newCard;
            numSeen++;
        }

        public bool IsCurrentCardTarget()
        {
            return curCard != null && curCard.CompareTag("Target");
        }

        private void TurnAllCardsOff()
        {
            if (cardArr == null) return;

            foreach (var card in cardArr)
                if (card) card.gameObject.SetActive(false);

            isActive = false;
        }
    }
}
