using UnityEngine;

namespace MoxoCPT
{
    /// <summary>
    /// Lightweight per-session data-quality monitor for the CPT.
    ///
    /// Frame cadence is the hard limit on reaction-time precision in a WebGL build
    /// (input is delivered and stimuli are drawn once per frame). This sampler records
    /// the unscaled frame time of every frame that elapses WHILE a real CPT run is in
    /// progress, then exposes robust summary statistics (median FPS, slow-frame FPS,
    /// frame-time jitter, hitch count) that <see cref="SessionPlayTimeTracker"/> writes
    /// into the session_summary row.
    ///
    /// Use these to flag or exclude sessions whose timing is untrustworthy (e.g. a
    /// participant on an underpowered or throttled device), which matters more for
    /// research validity than chasing sub-frame RT precision.
    ///
    /// Memory is bounded: frame times are accumulated into a fixed histogram, so a
    /// session of any length costs the same ~1 KB regardless of frame count.
    /// </summary>
    public class CptPerformanceMonitor : MonoBehaviour
    {
        // Histogram: bins of BinWidthMs spanning [0, BinCount*BinWidthMs) ms, plus a
        // final overflow bin for anything slower. 0.5 ms × 200 bins = 0..100 ms.
        private const float BinWidthMs = 0.5f;
        private const int BinCount = 200;          // 0..100 ms
        private const float LongFrameThresholdMs = 50f; // < 20 FPS hitch

        private static int[] _hist;                // length BinCount + 1 (last = overflow)
        private static long _frameCount;
        private static double _sumMs;
        private static float _maxMs;
        private static long _longFrameCount;

        private static CptPerformanceMonitor _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            _hist = null;
            _frameCount = 0;
            _sumMs = 0;
            _maxMs = 0f;
            _longFrameCount = 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;
            var go = new GameObject("[CptPerformanceMonitor]");
            _instance = go.AddComponent<CptPerformanceMonitor>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _hist = new int[BinCount + 1];
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (!IsCptRunActive()) return;

            // Unscaled: frame cadence is independent of any Time.timeScale changes.
            float ms = Time.unscaledDeltaTime * 1000f;
            if (ms <= 0f) return;

            int bin = Mathf.Clamp((int)(ms / BinWidthMs), 0, BinCount);
            _hist[bin]++;
            _frameCount++;
            _sumMs += ms;
            if (ms > _maxMs) _maxMs = ms;
            if (ms > LongFrameThresholdMs) _longFrameCount++;
        }

        // Mirror ButtonPress's gating: only sample while a real run is collecting
        // responses (not menus, cutscenes, or the post-game results screen).
        private static bool IsCptRunActive()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameManager.GameState.CPT) return false;

            var moxo = MoxoCPTManager.Instance;
            return moxo != null && !moxo.isGameOver;
        }

        // ---------- Public stats (read at session end) ----------

        public static long FrameSamples => _frameCount;

        /// <summary>Median frames-per-second across the sampled CPT frames. -1 if no data.</summary>
        public static double MedianFps
        {
            get
            {
                double ms = FrameMsPercentile(0.50);
                return ms > 0 ? 1000.0 / ms : -1.0;
            }
        }

        /// <summary>
        /// 5th-percentile FPS: the frame rate the slowest 5% of frames dipped below.
        /// Derived from the 95th-percentile frame time. -1 if no data.
        /// </summary>
        public static double P05Fps
        {
            get
            {
                double ms = FrameMsPercentile(0.95);
                return ms > 0 ? 1000.0 / ms : -1.0;
            }
        }

        /// <summary>Median frame time in ms. -1 if no data.</summary>
        public static double FrameMsMedian => FrameMsPercentile(0.50);

        /// <summary>
        /// Frame-time jitter as the inter-quartile range (p75 − p25) in ms — a robust
        /// spread measure that is not skewed by occasional hitches. -1 if no data.
        /// </summary>
        public static double FrameMsIqr
        {
            get
            {
                if (_frameCount <= 0) return -1.0;
                return FrameMsPercentile(0.75) - FrameMsPercentile(0.25);
            }
        }

        public static long LongFrameCount => _longFrameCount;

        public static double MeanFps
        {
            get
            {
                if (_frameCount <= 0) return -1.0;
                double meanMs = _sumMs / _frameCount;
                return meanMs > 0 ? 1000.0 / meanMs : -1.0;
            }
        }

        // Linear-interpolated percentile of frame time (ms) from the histogram.
        private static double FrameMsPercentile(double frac)
        {
            if (_hist == null || _frameCount <= 0) return -1.0;

            double target = frac * _frameCount;
            long cumulative = 0;
            for (int i = 0; i < _hist.Length; i++)
            {
                cumulative += _hist[i];
                if (cumulative >= target)
                {
                    if (i >= BinCount) return _maxMs; // overflow bin: report the worst seen
                    // Bin center as the representative value for this bucket.
                    return (i + 0.5) * BinWidthMs;
                }
            }
            return _maxMs;
        }
    }
}
