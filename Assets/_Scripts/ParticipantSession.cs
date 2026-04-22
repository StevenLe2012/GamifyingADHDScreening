using UnityEngine;

/// <summary>
/// Lightweight static holder for participant data collected on the login screen.
/// Static fields survive Unity scene loads, so data entered in the IntroBoot scene
/// is still available when GameManager initialises in the Main scene.
/// </summary>
public static class ParticipantSession
{
    /// <summary>Participant number exactly as typed (e.g. "1", "042").</summary>
    public static string Number       { get; set; } = "";

    /// <summary>Participant's last name exactly as typed.</summary>
    public static string LastName     { get; set; } = "";

    /// <summary>Session date in ISO format (yyyy-MM-dd).</summary>
    public static string SessionDate  { get; set; } = "";

    /// <summary>True once the login form has been successfully submitted.</summary>
    public static bool   WasSubmitted { get; set; } = false;

    /// <summary>Clears all stored data (call if you want a fresh session).</summary>
    public static void Reset()
    {
        Number       = "";
        LastName     = "";
        SessionDate  = "";
        WasSubmitted = false;
    }

    /// <summary>
    /// Automatically resets static state at the start of every Play session.
    /// Prevents stale WasSubmitted = true from a previous Editor Play run
    /// causing the login screen to be skipped on the next run.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnLoad() => Reset();
}
