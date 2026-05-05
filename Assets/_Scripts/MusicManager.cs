using UnityEngine;
using UnityEngine.Audio; // NEW: optional routing to a mixer group

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Crossfade")]
    [SerializeField] private float defaultFadeSeconds = 1.5f;

    [Header("Defaults (Inspector controlled)")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField] private bool defaultLoop = true;

    [Header("Auto Start")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private AudioClip startClip;
    [SerializeField, Range(0f, 1f)] private float startVolume = 1f;

    // ✅ Debug + optional mixer routing
    [Header("Routing / Debug")]
    [SerializeField] private AudioMixerGroup outputGroup = null; // optional
    [SerializeField] private bool debugLog = false;

    private AudioSource _a;
    private AudioSource _b;
    private AudioSource _active;
    private AudioSource _idle;
    private Coroutine _fadeCo;
    private float _activeTargetVol;
    private float _idleTargetVol;

    private float _dbg;
    private void Update() 
    {
        _dbg += Time.deltaTime;
        if (debugLog && _dbg > 1f) {
            _dbg = 0f;
            Debug.Log($"[Music] active={_active.clip?.name} srcVol={_active?.volume:0.00} target={_activeTargetVol:0.00} master={masterVolume:0.00}");
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _a = gameObject.AddComponent<AudioSource>();
        _b = gameObject.AddComponent<AudioSource>();
        ConfigureSource(_a);
        ConfigureSource(_b);

        _a.volume = 0f; _b.volume = 0f;
        _active = _a; _idle = _b;
        _activeTargetVol = 0f; _idleTargetVol = 0f;
    }

    private void Start()
    {
        if (playOnStart && startClip != null)
        {
            if (debugLog) Debug.Log($"[Music] PlayOnStart '{startClip.name}' vol={startVolume}");
            PlayImmediate(startClip, startVolume, defaultLoop);
        }
    }

    private void ConfigureSource(AudioSource src)
    {
        src.loop = defaultLoop;
        src.playOnAwake = false;
        src.spatialBlend = 0f;                     // 2D
        src.rolloffMode = AudioRolloffMode.Linear;
        src.outputAudioMixerGroup = outputGroup;   // optional

        // 🔒 Make sure nothing is silently muting it
        src.mute = false;
        src.bypassEffects = true;
        src.bypassListenerEffects = true;
        src.bypassReverbZones = true;
    }

    // Ensure source is hearable right before Play()
    private void EnsureAudible(AudioSource src)
    {
        if (!src) return;
        src.mute = false;
        src.spatialBlend = 0f; // 2D
        // if you use an AudioMixer, keep routing consistent
        src.outputAudioMixerGroup = outputGroup;
        AudioListener.pause = false; // in case something paused global audio
    }

    private void LateUpdate()
    {
        if (_active != null) _active.volume = Mathf.Clamp01(_activeTargetVol * masterVolume);
        if (_idle   != null) _idle.volume   = Mathf.Clamp01(_idleTargetVol   * masterVolume);
    }

    public void PlayImmediate(AudioClip clip, float volume = 1f, bool loop = true)
    {
        if (clip == null) return;
        if (debugLog) Debug.Log($"[Music] PlayImmediate '{clip.name}' vol={volume} loop={loop}");

        if (_fadeCo != null) { StopCoroutine(_fadeCo); _fadeCo = null; }

        _a.Stop(); _b.Stop();
        _active = _a; _idle = _b;

        _active.clip = clip;
        _active.loop = loop;
        _activeTargetVol = Mathf.Clamp01(volume);
        _active.volume = _activeTargetVol * masterVolume;
        _active.time = 0f;

        EnsureAudible(_active); // NEW
        _active.Play();

        _idle.clip = null;
        _idleTargetVol = 0f;
        _idle.volume = 0f;
    }

    public void Switch(AudioClip clip, float fadeSeconds = -1f, float targetVolume = 1f, bool? loopOverride = null)
    {
        if (clip == null)
        {
            if (debugLog) Debug.LogWarning("[Music] Switch called with null clip.");
            return;
        }
        if (fadeSeconds < 0f) fadeSeconds = defaultFadeSeconds;

        // Same-clip: ensure it’s actually playing, then just set target volume/loop
        if (_active.clip == clip)
        {
            if (debugLog) Debug.Log($"[Music] Switch same-clip '{clip.name}' (adjust volume/loop).");
            _active.loop = loopOverride ?? defaultLoop;
            _activeTargetVol = Mathf.Clamp01(targetVolume);

            EnsureAudible(_active);           // NEW
            if (!_active.isPlaying)           // NEW
            {
                _active.time = 0f;
                _active.Play();
            }
            return;
        }

        // Prepare idle with new clip
        _idle.clip = clip;
        _idle.loop = loopOverride ?? defaultLoop;
        _idleTargetVol = Mathf.Clamp01(targetVolume);
        _idle.volume = 0f;
        _idle.time = 0f;

        EnsureAudible(_idle);                 // NEW
        _idle.Play();

        if (debugLog) Debug.Log($"[Music] Crossfade → '{clip.name}' fade={fadeSeconds}s targetVol={_idleTargetVol}");

        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(CoCrossfade(fadeSeconds));
    }

        private System.Collections.IEnumerator CoCrossfade(float dur)
    {
        float t = 0f;
        float startActive = _activeTargetVol;
        float endActive   = 0f;
        float endIdle     = _idleTargetVol;

        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);

            _activeTargetVol = Mathf.Lerp(startActive, endActive, k);
            _idleTargetVol   = Mathf.Lerp(0f,           endIdle,   k);
            yield return null;
        }

        // finalize mix for current roles
        _activeTargetVol = 0f;
        _idleTargetVol   = endIdle;

        _active.Stop();
        _active.volume = 0f;

        // swap roles
        var oldActive = _active;
        _active = _idle;    // this was fading up to endIdle
        _idle   = oldActive;

        // ✅ IMPORTANT: give the new active its target back
        _activeTargetVol = endIdle;
        _active.volume   = Mathf.Clamp01(_activeTargetVol * masterVolume);
        if (!_active.isPlaying) _active.Play();

        // reset the new idle
        _idle.clip = null;
        _idleTargetVol = 0f;
        _idle.volume = 0f;

        if (debugLog) Debug.Log($"[Music] Crossfade complete. Active='{_active.clip?.name}'");
        _fadeCo = null;
    }


    // -------- Inspector-friendly setters --------
    public void SetMasterVolume(float vol)  { masterVolume = Mathf.Clamp01(vol); }
    public void SetDefaultLoop(bool loop)   { defaultLoop = loop; if (_active != null) _active.loop = loop; }
}
