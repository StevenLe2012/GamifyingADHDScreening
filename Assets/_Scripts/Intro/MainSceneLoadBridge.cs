/// <summary>
/// Signals when the Main scene has finished its first-frame startup (IntroBoot hides loading overlay).
/// </summary>
public static class MainSceneLoadBridge
{
    public static bool IsReady { get; private set; }

    public static void Reset() => IsReady = false;

    public static void MarkReady()
    {
        if (IsReady) return;
        IsReady = true;
        VideoLoadingScreen.OnMainSceneReady();
    }
}
