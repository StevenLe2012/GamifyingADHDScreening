using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoxoCPT
{
    public class DistractorSystem : MonoBehaviour
    {
        [System.Serializable]
        public class Entry
        {
            public GameObject obj;
            public AudioClip clip;
            public AudioSource audioSource;
            [HideInInspector] public bool running;
        }

        [Header("Population")]
        [SerializeField] private bool autoFindByTag = true;
        [SerializeField] private string distractorTag = "Distractor";
        [SerializeField] private List<Entry> entries = new List<Entry>();

        [Header("Timing (Active ON)")]
        [SerializeField] private float minOnSeconds = 3.5f;
        [SerializeField] private float maxOnSeconds = 15.0f;
        [SerializeField] private float onStepSeconds = 0.5f;

        [Header("Timing (Idle/OFF between activations)")]
        [SerializeField] private float minOffSeconds = 0.5f;
        [SerializeField] private float maxOffSeconds = 0.5f;

        [Header("Concurrency")]
        [SerializeField] private bool allowOverlap = true;
        [SerializeField] private int maxSimultaneous = 0; // 0 = unlimited

        [Header("Global audio settings")]
        [SerializeField] private bool loopAudioWhileVisible = true;
        [SerializeField] private float audioVolume = 1.0f;

        [Header("Debug / Safety")]
        [SerializeField] private bool debugLogs = true;
        [SerializeField] private bool autoStartWhenCPT = true;     // NEW: self-start when GameState == CPT
        [SerializeField] private bool startImmediatelyForDebug = false;

        private readonly List<Coroutine> _coRoutines = new List<Coroutine>();
        private int _activeCount = 0;
        private bool _enabled;

        private void Log(string msg)
        {
            if (debugLogs) Debug.Log($"[Distractors] {msg}", this);
        }

        private void Awake()
        {
            if (autoFindByTag)
            {
                entries.Clear();
                var gos = GameObject.FindGameObjectsWithTag(distractorTag);
                foreach (var go in gos) entries.Add(new Entry { obj = go });
                Log($"Auto-found {entries.Count} distractors with tag '{distractorTag}'.");
            }

            foreach (var e in entries)
                if (e?.obj) e.obj.SetActive(false);

            if (startImmediatelyForDebug) StartSystem();
        }

        private void Update()
        {
            // Auto start/stop following GameManager state (hands-free)
            if (!autoStartWhenCPT || GameManager.Instance == null) return;

            var state = GameManager.Instance.State;

            if (!_enabled && state == GameManager.GameState.CPT)
            {
                Log("Auto-starting (GameState == CPT).");
                StartSystem();
            }
            else if (_enabled && state != GameManager.GameState.CPT)
            {
                Log("Auto-stopping (GameState != CPT).");
                StopSystem();
            }
        }

        private float NextOnDuration()
        {
            float steps = Mathf.Max(0, Mathf.Round((maxOnSeconds - minOnSeconds) / onStepSeconds));
            float k = Random.Range(0, (int)steps + 1);
            return minOnSeconds + k * onStepSeconds;
        }

        private float NextOffDelay() => Random.Range(minOffSeconds, maxOffSeconds);

        public void StartSystem()
        {
            if (_enabled) return;

            // Sanity check: do we actually have assigned entries?
            int assigned = 0;
            foreach (var e in entries) if (e != null && e.obj != null) assigned++;
            if (assigned == 0)
            {
                Debug.LogWarning("[Distractors] No valid entries. Turn on Auto Find By Tag or assign objects in Entries.", this);
                return;
            }

            _enabled = true;
            _activeCount = 0;

            foreach (var e in entries)
            {
                if (e == null || e.obj == null) continue;

                if (e.audioSource == null)
                {
                    e.audioSource = e.obj.GetComponent<AudioSource>();
                    if (e.audioSource == null) e.audioSource = e.obj.AddComponent<AudioSource>();
                }
                e.audioSource.playOnAwake = false;
                e.audioSource.loop = loopAudioWhileVisible;
                e.audioSource.volume = audioVolume;

                var co = StartCoroutine(CoRunEntry(e));
                _coRoutines.Add(co);
            }

            Log($"Started with {assigned} valid entries.");
        }

        public void StopSystem()
        {
            if (!_enabled) return;
            _enabled = false;

            foreach (var co in _coRoutines)
                if (co != null) StopCoroutine(co);
            _coRoutines.Clear();

            foreach (var e in entries)
            {
                if (e == null || e.obj == null) continue;
                e.running = false;
                e.obj.SetActive(false);
                if (e.audioSource && e.audioSource.isPlaying) e.audioSource.Stop();
            }

            _activeCount = 0;
            Log("Stopped.");
        }

        private IEnumerator CoRunEntry(Entry e)
        {
            // Stagger start
            yield return new WaitForSeconds(Random.Range(0f, 1.0f));

            while (_enabled)
            {
                float off = NextOffDelay();
                yield return new WaitForSeconds(off);
                if (!_enabled) yield break;

                // Overlap gating
                if (!allowOverlap)
                {
                    if (_activeCount > 0)
                        yield return new WaitUntil(() => !_enabled || _activeCount == 0);
                    if (!_enabled) yield break;
                }
                else if (maxSimultaneous > 0)
                {
                    if (_activeCount >= maxSimultaneous)
                        yield return new WaitUntil(() => !_enabled || _activeCount < maxSimultaneous);
                    if (!_enabled) yield break;
                }

                // Activate
                float on = NextOnDuration();
                e.running = true;
                _activeCount++;
                e.obj.SetActive(true);
                Log($"ON: {e.obj.name} for {on:0.0}s (active={_activeCount})");

                if (e.clip != null && e.audioSource != null)
                {
                    e.audioSource.clip = e.clip;
                    e.audioSource.loop = loopAudioWhileVisible;
                    e.audioSource.volume = audioVolume;
                    e.audioSource.Play();
                }

                yield return new WaitForSeconds(on);

                // Deactivate
                if (e.audioSource && e.audioSource.isPlaying) e.audioSource.Stop();
                e.obj.SetActive(false);
                e.running = false;
                _activeCount = Mathf.Max(0, _activeCount - 1);
                Log($"OFF: {e.obj.name} (active={_activeCount})");
            }
        }
    }
}
