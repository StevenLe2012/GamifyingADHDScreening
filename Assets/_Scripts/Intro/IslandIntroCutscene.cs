using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;

/// <summary>
/// Plays a one-shot video cutscene before travelling to a specific island (e.g. DRAGON).
/// For all other islands it immediately forwards to IslandTravelManager.
/// </summary>
public class IslandIntroCutscene : MonoBehaviour
{
    public static IslandIntroCutscene I { get; private set; }

    [System.Serializable]
    public class CutsceneEntry
    {
        [Tooltip("Island id (uppercased), e.g. CARDS, BREAD, POISON, SKULL, DRAGON")]
        public string islandId = "DRAGON";

        [Tooltip("Relative to StreamingAssets, e.g. dragon_intro.mp4")]
        public string videoFileName = "dragon_intro.mp4";
    }

    [Header("Per-island cutscenes")]
    [Tooltip("Configure one row per island that should play a video before travel.")]
    public CutsceneEntry[] cutscenes;

    [Header("Video Options (shared)")]
    public bool audioEnabled = true;
    public bool allowSkip = true;
    public float minUnskippableSeconds = 1.5f;

    [Header("Rendering")]
    [Tooltip("If true, renders on camera near plane. If false, uses RenderTexture + RawImage.")]
    public bool renderToCamera = false;
    public Camera targetCamera;
    public UnityEngine.UI.RawImage rawImage;
    public RenderTexture tempRenderTexture;

    [Header("Fade (optional)")]
    public bool fadeToBlack = false;
    public CanvasGroup fadeGroup;
    public float fadeDuration = 0.35f;

    [Header("Diagnostics")]
    public bool log = true;

    VideoPlayer _vp;
    IslandData _pendingIsland;
    CutsceneEntry _currentCutscene;
    bool _isPlaying;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;

        if (renderToCamera && !targetCamera)
            targetCamera = Camera.main;

        // Build VideoPlayer once; we only call it when needed.
        _vp = gameObject.AddComponent<VideoPlayer>();
        _vp.playOnAwake = false;
        _vp.isLooping = false;
        _vp.audioOutputMode = audioEnabled ? VideoAudioOutputMode.AudioSource
                                           : VideoAudioOutputMode.None;
        if (audioEnabled)
        {
            var audio = gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.loop = false;
            _vp.SetTargetAudioSource(0, audio);
        }

        // Source (we assign url right before play in case filename changes).
        _vp.source = VideoSource.Url;

        // Output
        if (renderToCamera)
        {
            _vp.renderMode = VideoRenderMode.CameraNearPlane;
            _vp.targetCamera = targetCamera;
            _vp.targetCameraAlpha = 1f;
        }
        else
        {
            _vp.renderMode = VideoRenderMode.RenderTexture;
            _vp.targetTexture = tempRenderTexture;
            if (rawImage) rawImage.texture = tempRenderTexture;
        }

        _vp.loopPointReached += OnVideoFinished;
    }

    /// <summary>
    /// Entry point from IslandSelectionUI. Plays cutscene if islandId matches; otherwise travels immediately.
    /// </summary>
    public void PlayIfNeeded(IslandData island)
    {
        if (island == null)
        {
            if (log) Debug.LogWarning("[IslandIntroCutscene] PlayIfNeeded(null) – doing nothing.");
            return;
        }

        var id = (island.islandId ?? "").Trim().ToUpperInvariant();

        // Find a matching cutscene entry for this island.
        _currentCutscene = null;
        if (!string.IsNullOrEmpty(id) && cutscenes != null)
        {
            foreach (var c in cutscenes)
            {
                if (c == null) continue;
                var cid = (c.islandId ?? "").Trim().ToUpperInvariant();
                if (!string.IsNullOrEmpty(cid) && cid == id)
                {
                    _currentCutscene = c;
                    break;
                }
            }
        }

        if (_currentCutscene == null)
        {
            // No configured cutscene for this island → normal travel.
            IslandTravelManager.I?.TravelTo(island);
            return;
        }

        if (_isPlaying)
        {
            if (log) Debug.LogWarning("[IslandIntroCutscene] Already playing – ignoring new request.");
            return;
        }

        _pendingIsland = island;
        StartCoroutine(CoRun());
    }

    IEnumerator CoRun()
    {
        _isPlaying = true;

        // Ensure visuals are enabled every time we start a cutscene
        if (renderToCamera && _vp != null && _vp.targetCamera != null)
            _vp.targetCameraAlpha = 1f;
        if (!renderToCamera && rawImage)
            rawImage.gameObject.SetActive(true);

        if (fadeToBlack && fadeGroup)
            fadeGroup.alpha = 1f;

        yield return PrepareAndPlay();

        if (fadeToBlack && fadeGroup)
            yield return StartCoroutine(CoFade(fadeGroup, 1f, 0f, 0.25f));

        float t = 0f;
        while (_vp.isPlaying)
        {
            t += Time.unscaledDeltaTime;

            if (allowSkip && t > minUnskippableSeconds && AnySkipPressed())
            {
                if (log) Debug.Log("[IslandIntroCutscene] Skipped.");
                break;
            }

            yield return null;
        }

        if (fadeToBlack && fadeGroup)
            yield return StartCoroutine(CoFade(fadeGroup, fadeGroup.alpha, 1f, fadeDuration));

        FinishAndTravel();
    }

    IEnumerator PrepareAndPlay()
    {
        if (_currentCutscene == null)
        {
            // Safety: if something went wrong, just bail to normal travel.
            if (log) Debug.LogWarning("[IslandIntroCutscene] PrepareAndPlay with no current cutscene; aborting.");
            FinishAndTravel();
            yield break;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        _vp.url = Application.streamingAssetsPath + "/" + _currentCutscene.videoFileName;
#else
        _vp.url = System.IO.Path.Combine(Application.streamingAssetsPath, _currentCutscene.videoFileName);
#endif

        if (log) Debug.Log($"[IslandIntroCutscene] Preparing video: {_vp.url}");
        _vp.Prepare();

#if UNITY_WEBGL && !UNITY_EDITOR
        const float prepareTimeout = 8f;
        float elapsed = 0f;
        while (!_vp.isPrepared && elapsed < prepareTimeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (!_vp.isPrepared)
        {
            if (log) Debug.LogWarning($"[IslandIntroCutscene] WebGL: video did not prepare within {prepareTimeout}s, skipping.");
            FinishAndTravel();
            yield break;
        }
#else
        while (!_vp.isPrepared) yield return null;
#endif

        if (log) Debug.Log("[IslandIntroCutscene] Playing.");
        _vp.Play();
    }

    void OnVideoFinished(VideoPlayer player)
    {
        if (!_isPlaying) return;
        if (log) Debug.Log("[IslandIntroCutscene] Video finished.");
        // CoRun handles the transition; nothing else here.
    }

    bool AnySkipPressed()
    {
        var kb = Keyboard.current;
        var gp = Gamepad.current;

        if ((kb != null && (kb.anyKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) ||
            (gp != null && (gp.startButton.wasPressedThisFrame || gp.aButton.wasPressedThisFrame)))
            return true;

#if !UNITY_WEBGL
        // On WebGL, Input.anyKeyDown includes mouse buttons — clicking anywhere on the video
        // would trigger an accidental skip. Keyboard/gamepad only on WebGL.
        if (Input.anyKeyDown) return true;
#endif
        return false;
    }

    IEnumerator CoFade(CanvasGroup g, float from, float to, float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            g.alpha = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }
        g.alpha = to;
    }

    void FinishAndTravel()
    {
        CleanUpVideo();

        var island = _pendingIsland;
        _pendingIsland = null;
        _isPlaying = false;

        // Hide overlay visuals so we return to normal view.
        if (rawImage && rawImage.gameObject.activeSelf)
            rawImage.gameObject.SetActive(false);
        if (renderToCamera && _vp != null && _vp.targetCamera != null)
            _vp.targetCameraAlpha = 0f;
        if (fadeGroup) fadeGroup.alpha = 0f;

        if (island != null)
            IslandTravelManager.I?.TravelTo(island);
    }

    void CleanUpVideo()
    {
        if (_vp == null) return;
        if (_vp.isPlaying) _vp.Stop();
        // Do NOT Release() the RenderTexture — it is an Inspector-assigned asset shared
        // across multiple cutscene plays. Releasing it permanently frees GPU memory and
        // the next cutscene would render to a dead texture (frozen first frame, no visuals).
        _vp.url = "";
    }
}

