// Assets/_Scripts/MoxoCPT/MoxoStartGate.cs
namespace MoxoCPT
{
    /// <summary>
    /// One-shot gate for starting the MOXO run.
    /// Arm() exactly when you show the post-training "Ready" panel.
    /// Any code that wants to begin the run must TryConsume().
    /// </summary>
    public static class MoxoStartGate
    {
        public static bool Armed { get; private set; }

        public static void Arm() => Armed = true;

        /// <summary>Returns true exactly once after Arm(); resets Armed to false.</summary>
        public static bool TryConsume()
        {
            if (!Armed) return false;
            Armed = false;
            return true;
        }

        public static void Disarm() => Armed = false;
    }
}
