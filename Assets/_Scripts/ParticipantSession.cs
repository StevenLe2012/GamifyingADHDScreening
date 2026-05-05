using UnityEngine;

/// <summary>
/// Lightweight static holder for participant data collected on the login screen.
/// Static fields survive Unity scene loads, so data entered in the IntroBoot scene
/// is still available when GameManager initialises in the Main scene.
/// </summary>
public static class ParticipantSession
{
    /// <summary>5-digit participant code exactly as typed (e.g. "10042").</summary>
    public static string Number       { get; set; } = "";

    /// <summary>Not collected — kept for backward compatibility only.</summary>
    public static string LastName     { get; set; } = "";

    /// <summary>Not included in participant ID — kept for backward compatibility only.</summary>
    public static string SessionDate  { get; set; } = "";

    /// <summary>True once the login form has been successfully submitted.</summary>
    public static bool   WasSubmitted { get; set; } = false;

    /// <summary>Clears all stored data.</summary>
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
