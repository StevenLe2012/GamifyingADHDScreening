using UnityEngine;

public static class EkonnAnimBus
{
    // Iterate over all active Ekonn drivers
    private static void ForEachActive(System.Action<EkonnAnimDriver> action)
    {
        if (action == null) return;

        var drivers = UnityEngine.Object.FindObjectsOfType<EkonnAnimDriver>(true);
        foreach (var d in drivers)
        {
            if (!d) continue;
            if (!d.gameObject.activeInHierarchy) continue;
            if (!d.isActiveAndEnabled) continue;
            action(d);
        }
    }

    // Convenience API
    public static void StandUpAndTalk()      => ForEachActive(e => e.StandUpAndTalk());
    public static void EnsureStandingIdle()   => ForEachActive(e => e.EnsureStandingIdle());
    public static void TalkOff()              => ForEachActive(e => e.SetTalking(false));
    public static void SitDown()              => ForEachActive(e => e.SitDown());

    // Aliases (if you prefer the “All” naming)
    public static void StandUpAndTalkAll()     => StandUpAndTalk();
    public static void EnsureStandingIdleAll() => EnsureStandingIdle();
    public static void TalkOffAll()            => TalkOff();
    public static void SitDownAll()            => SitDown();
}
