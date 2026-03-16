using System;
using UnityEngine;

public static class KoalaAnimBus
{
    // ------------------------------------------------------------
    // Core enumeration
    // ------------------------------------------------------------
    private static void ForEachKoala(Action<KoalaAnimatorDriver> action, bool requireActiveInHierarchy)
    {
        if (action == null) return;

        // includeInactive = true so we can find koalas even if currently disabled
        var drivers = UnityEngine.Object.FindObjectsOfType<KoalaAnimatorDriver>(true);

        foreach (var d in drivers)
        {
            if (!d) continue;

            if (requireActiveInHierarchy)
            {
                if (!d.gameObject.activeInHierarchy) continue;
                if (!d.isActiveAndEnabled) continue;
            }

            action(d);
        }
    }

    private static void ActivateWithParents(GameObject go)
    {
        if (!go) return;

        // Activate parents first so child activation "sticks"
        var t = go.transform;
        while (t != null)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            t = t.parent;
        }
    }

    // ------------------------------------------------------------
    // Public broadcast methods
    // ------------------------------------------------------------

    // Back-compat: only active-in-hierarchy koalas
    public static void BroadcastToActiveKoalas(Action<KoalaAnimatorDriver> action)
        => ForEachKoala(action, requireActiveInHierarchy: true);

    // Includes inactive koalas (NOTE: if a koala is inactive, calling methods might do nothing
    // if its Animator isn't enabled. Use BroadcastEnsuringActive(...) if you need reliability.)
    public static void BroadcastToAllKoalas(Action<KoalaAnimatorDriver> action)
        => ForEachKoala(action, requireActiveInHierarchy: false);

    // NEW: Ensures koalas (and parents) are active BEFORE firing action.
    // Best for "phase toggles" where koala may be disabled and you need a trigger to land.
    public static void BroadcastEnsuringActive(Action<KoalaAnimatorDriver> action, bool includeDisabledDrivers = true)
    {
        if (action == null) return;

        var drivers = UnityEngine.Object.FindObjectsOfType<KoalaAnimatorDriver>(includeDisabledDrivers);
        foreach (var d in drivers)
        {
            if (!d) continue;

            ActivateWithParents(d.gameObject);

            // If component disabled, we still allow the call (your driver can ignore if needed)
            action(d);
        }
    }

    // ------------------------------------------------------------
    // Convenience API (keep these so old scripts compile)
    // ------------------------------------------------------------

    // Your existing names (kept)
    public static void StandUpAndTalkAll() => BroadcastToActiveKoalas(k => k.StandUpAndTalk());
    public static void TalkOffAll()        => BroadcastToActiveKoalas(k => k.SetTalking(false));
    public static void SitDownAll()        => BroadcastToActiveKoalas(k => k.SitDown());

    // These were switched to BroadcastToAllKoalas in your version.
    // Keeping them, but ALSO offering "EnsuringActive" variants below.
    public static void EnsureStandingIdleAll() => BroadcastToAllKoalas(k => k.EnsureStandingIdle());
    public static void CheerAll()              => BroadcastToAllKoalas(k => k.PlayCheer());
    public static void TalkAll()               => BroadcastToAllKoalas(k => k.StandAndTalk());
    public static void CheerOnAll(bool on) => BroadcastToAllKoalas(k => k.SetCheer(on));

    // New: single call for CPT start (kept)
    public static void EnterCptAll() => BroadcastToActiveKoalas(k => k.OnEnterCPT());

    // ------------------------------------------------------------
    // NEW: Reliable variants for phase transitions
    // ------------------------------------------------------------

    // Use these if koala might have been inactive a frame ago.
    public static void EnsureStandingIdleEnsuringActive() => BroadcastEnsuringActive(k => k.EnsureStandingIdle());
    public static void CheerEnsuringActive()              => BroadcastEnsuringActive(k => k.PlayCheer());
    public static void TalkEnsuringActive()               => BroadcastEnsuringActive(k => k.StandAndTalk());

    // Handy for "conversation begins/ends"
    public static void TalkOffEnsuringActive()            => BroadcastEnsuringActive(k => k.SetTalking(false));
}