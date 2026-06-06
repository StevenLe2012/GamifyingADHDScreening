using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MoxoCPT
{
    /// <summary>
    /// Hard reset between islands + safe preparation for the next island.
    /// Call CleanupPreviousIsland() BEFORE placing the next island UI,
    /// then call PrepareForNewIsland(islandId) AFTER you place Intro/Countdown.
    /// </summary>
    public static class MoxoSessionCoordinator
    {
        // ---------------- CLEANUP (previous island) ----------------
        public static void CleanupPreviousIsland()
        {
            // 0) Time back to normal
            if (Time.timeScale != 1f) Time.timeScale = 1f;

            // 1) Kill any countdown UIs named "ExploreHintUI" (no compile-time dep)
            foreach (var mb in Object.FindObjectsOfType<MonoBehaviour>(true))
            {
                if (!mb) continue;
                var t = mb.GetType();
                if (t.Name != "ExploreHintUI") continue;

                try
                {
                    mb.CancelInvoke();
                    mb.StopAllCoroutines();

                    t.GetMethod("Cancel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?.Invoke(mb, null);
                    t.GetMethod("Hide", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?.Invoke(mb, null);

                    if (mb.gameObject.activeSelf) mb.gameObject.SetActive(false);
                }
                catch { /* best effort */ }
            }

            // 2) Hide IntroScreen immediately (if present)
            var intro = IntroScreen.Instance ?? Object.FindObjectOfType<IntroScreen>(true);
            if (intro)
            {
                try
                {
                    intro.CancelInvoke();
                    intro.StopAllCoroutines();
                } catch { }

                var cg = intro.GetComponent<CanvasGroup>();
                if (cg) { cg.alpha = 0f; cg.blocksRaycasts = false; cg.interactable = false; }
                intro.gameObject.SetActive(false);
            }

            // 3) Stop any running MOXO manager (defensive)
            var mgr = MoxoCPTManager.Instance ?? Object.FindObjectOfType<MoxoCPTManager>(true);
            if (mgr)
            {
                try { mgr.CancelInvoke(); mgr.StopAllCoroutines(); } catch { }

                // If a game was still running, end it safely
                SafeCall(mgr, "OnGameEnd");

                // Ensure cards are OFF
                var cards = mgr.GetComponent("CardsActive");
                if (cards)
                    cards.GetType().GetMethod("SetCardsActive", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                         ?.Invoke(cards, new object[] { false });
            }

            // 4) Reset score/runtime
            var score = Object.FindObjectOfType<CPTScoreRuntime>(true);
            if (score) score.ResetScore();

            // 5) Disable ALL island MOXO roots; the next island will be enabled explicitly
            foreach (var group in Object.FindObjectsOfType<IslandMoxoGroup>(true))
                if (group && group.moxoRoot) group.moxoRoot.SetActive(false);

            // 6) Return to Explore so the pipeline can enter PrepareCPT cleanly
            GameManager.Instance?.UpdateGameState(GameManager.GameState.Explore);
        }

        // ---------------- PREP (next island) ----------------
        /// <summary>
        /// Enable only the requested island’s MOXO rig, re-prime cards/score,
        /// and leave the rig ready for Intro → Start click → OnGameBegin().
        /// Call this AFTER you have placed Intro/Countdown for the new island.
        /// </summary>
        public static void PrepareForNewIsland(string islandId)
        {
            islandId = (islandId ?? "").Trim().ToUpperInvariant();

            // 1) Turn ON just this island's MOXO root
            foreach (var group in Object.FindObjectsOfType<IslandMoxoGroup>(true))
            {
                bool on = group && !string.IsNullOrWhiteSpace(group.islandId) &&
                          string.Equals(group.islandId.Trim(), islandId, System.StringComparison.OrdinalIgnoreCase);
                if (group && group.moxoRoot) group.moxoRoot.SetActive(on);
            }

            // 2) Ensure Score runtime exists & is reset
            if (CPTScoreRuntime.I == null) Object.FindObjectOfType<CPTScoreRuntime>(true);
            CPTScoreRuntime.I?.ResetScore();

            // 3) Toggle cards OFF→ON to clear any stale sequence/counters
            var mgr = MoxoCPTManager.Instance ?? Object.FindObjectOfType<MoxoCPTManager>(true);
            if (mgr)
            {
                var cards = mgr.GetComponent("CardsActive");
                if (cards)
                {
                    var set = cards.GetType().GetMethod("SetCardsActive", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    set?.Invoke(cards, new object[] { false });
                    set?.Invoke(cards, new object[] { true });
                }
            }
        }

        // ---------------- Utils ----------------
        private static void SafeCall(object target, string method)
        {
            if (target == null) return;
            var mi = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            try { mi?.Invoke(target, null); } catch { }
        }
    }
}
