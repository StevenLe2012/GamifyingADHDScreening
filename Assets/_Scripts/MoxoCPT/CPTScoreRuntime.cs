using UnityEngine;

namespace MoxoCPT
{
    public class CPTScoreRuntime : MonoBehaviour
    {
        public static CPTScoreRuntime I { get; private set; }

        public int TotalTargets { get; private set; }
        public int CorrectTargetsHit { get; private set; }

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        public void ResetScore()
        {
            TotalTargets = 0;
            CorrectTargetsHit = 0;
        }

        public void RegisterTrial(bool isTarget, bool correctTargetHit)
        {
            if (isTarget)
            {
                TotalTargets++;
                if (correctTargetHit) CorrectTargetsHit++;
            }
        }

        public string ScoreString() => $"{CorrectTargetsHit}/{TotalTargets}";
    }
}
