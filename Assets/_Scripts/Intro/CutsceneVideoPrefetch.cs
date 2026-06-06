using UnityEngine;

/// <summary>
/// Prefetches island intro cutscene MP4s on demand (island-picker highlight, before play).
/// </summary>
public static class CutsceneVideoPrefetch
{
    public static void PrefetchAllIslandIntros(bool log = true)
    {
        var cutscene = IslandIntroCutscene.I;
        if (cutscene == null || cutscene.cutscenes == null) return;

        int count = 0;
        foreach (var entry in cutscene.cutscenes)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.videoFileName)) continue;
            WebGLVideoPrefetch.StartFile(entry.videoFileName);
            count++;
        }

        if (log && count > 0)
            Debug.Log($"[CutsceneVideoPrefetch] Started prefetch for {count} island intro video(s).");
    }

    public static void PrefetchForIsland(string islandId, bool log = false)
    {
        var fileName = GetVideoFileNameForIsland(islandId);
        if (string.IsNullOrWhiteSpace(fileName)) return;

        WebGLVideoPrefetch.StartFile(fileName);
        if (log)
            Debug.Log($"[CutsceneVideoPrefetch] Prefetch priority: {fileName} (island {islandId}).");
    }

    public static void PrefetchRemainingIslands(System.Collections.Generic.IEnumerable<IslandData> islands, bool log = true)
    {
        if (islands == null) return;

        int count = 0;
        foreach (var island in islands)
        {
            if (island == null) continue;
            var fileName = GetVideoFileNameForIsland(island.islandId);
            if (string.IsNullOrWhiteSpace(fileName)) continue;
            WebGLVideoPrefetch.StartFile(fileName);
            count++;
        }

        if (log && count > 0)
            Debug.Log($"[CutsceneVideoPrefetch] Prefetching {count} remaining island intro(s) for picker.");
    }

    public static string ResolvePlaybackFile(string streamingAssetsFileName)
    {
        return WebGLVideoPrefetch.ResolvePlaybackFile(streamingAssetsFileName);
    }

    public static bool WasPrefetchStarted(string streamingAssetsFileName)
    {
        var url = WebGLVideoPrefetch.BuildStreamingAssetsUrl(streamingAssetsFileName);
        return WebGLVideoPrefetch.WasStarted(url);
    }

    public static bool IsPrefetchReady(string streamingAssetsFileName)
    {
        var url = WebGLVideoPrefetch.BuildStreamingAssetsUrl(streamingAssetsFileName);
        return WebGLVideoPrefetch.IsReady(url);
    }

    static string GetVideoFileNameForIsland(string islandId)
    {
        var cutscene = IslandIntroCutscene.I;
        if (cutscene == null || cutscene.cutscenes == null) return null;

        var id = (islandId ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(id)) return null;

        foreach (var entry in cutscene.cutscenes)
        {
            if (entry == null) continue;
            var cid = (entry.islandId ?? "").Trim().ToUpperInvariant();
            if (cid == id)
                return entry.videoFileName;
        }

        return null;
    }
}
