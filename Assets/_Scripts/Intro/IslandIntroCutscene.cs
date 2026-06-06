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

    [Header("WebGL Buffering")]
    [Tooltip("How long to wait for the video to buffer before skipping (seconds). Increase for slow connections.")]
    public float prepareTimeout = 30f;

    [Header("Loading screen (before cutscene)")]
    [Tooltip("Optional override. Default: Resources/UI/LoadingPage.png")]
    public Sprite loadingPageSprite;
    [Tooltip("Minimum time on loading page before cutscene (even if video is already buffered).")]
    public float loadingScreenMinSeconds = 3f;
    [Tooltip("Fallback: stop waiting and continue after this many seconds if the cutscene is still not ready.")]
    public float loadingScreenMaxSeconds = 30f;

    [Header("Diagnostics")]
    public bool log = true;

    VideoPlayer _vp;
    AudioSource _videoAudio;
    IslandData _pendingIsland;
    CutsceneEntry _currentCutscene;
    bool _isPlaying;
    bool _videoEnded;
    float _ignoreSkipInputUntil;
    CutsceneVideoSuspend.State _suspend;

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
        _vp.waitForFirstFrame = true;
        _vp.skipOnDrop = true; // Prefer frame drops over A/V drift on WebGL.
        _vp.audioOutputMode = audioEnabled ? VideoAudioOutputMode.AudioSource
                                           : VideoAudioOutputMode.None;
        if (audioEnabled)
        {
            _videoAudio = gameObject.AddComponent<AudioSource>();
            _videoAudio.playOnAwake = false;
            _videoAudio.loop = false;
            _videoAudio.spatialBlend = 0f;
            _videoAudio.dopplerLevel = 0f;
            _vp.SetTargetAudioSource(0, _videoAudio);
        }

        // Source (we assign url right before play in case filename changes).
        _vp.source = VideoSource.Url;

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

        CutsceneWebGLVideoOutput.Configure(this, _vp, ref renderToCamera, ref rawImage, ref tempRenderTexture);

        _vp.loopPointReached += OnVideoFinished;

        WebGLPageVisibility.Register(this);
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

#if UNITY_WEBGL && !UNITY_EDITOR
        // Prefetch + prepare handled in PrepareAndPlay (full-file wait before playback).
        WebGLVideoPrefetch.StartFile(_currentCutscene.videoFileName);
        if (log) Debug.Log($"[IslandIntroCutscene] WebGL: prefetch started for {_currentCutscene.videoFileName}");
#endif

        StartCoroutine(CoRun());
    }

    IEnumerator CoRun()
    {
        _isPlaying = true;
        _videoEnded = false;
        CutsceneVideoSuspend.Reset(ref _suspend);
        _ignoreSkipInputUntil = Time.unscaledTime + 0.2f;

        if (fadeToBlack && fadeGroup)
            fadeGroup.alpha = 1f;

        yield return PrepareAndPlay();
        if (!_isPlaying)
            yield break;

        CutsceneWebGLVideoOutput.Show(_vp);

        if (!CutsceneWebGLVideoOutput.UsesOverlayPath)
            CutsceneWorldHide.Begin(renderToCamera ? targetCamera : null);
        if (!renderToCamera && rawImage)
            CutsceneVideoLayout.ApplyFullscreen(rawImage);

        if (renderToCamera && _vp != null && _vp.targetCamera != null)
            _vp.targetCameraAlpha = 1f;
        if (!renderToCamera && rawImage)
            rawImage.gameObject.SetActive(true);

        if (log) Debug.Log("[IslandIntroCutscene] Playing.");
        _vp.Play();
        VideoLoadingScreen.Hide();

        if (fadeToBlack && fadeGroup)
            yield return StartCoroutine(CoFade(fadeGroup, 1f, 0f, 0.25f));

        float t = 0f;
        float stallTimer = 0f;
        bool playbackStarted = false;

        while (!_videoEnded)
        {
            if (CutsceneVideoPlayback.ShouldSkip(allowSkip, minUnskippableSeconds, t, _ignoreSkipInputUntil))
            {
                if (log) Debug.Log("[IslandIntroCutscene] Skipped.");
                break;
            }

            if (_suspend.AppSuspended)
            {
                yield return null;
                continue;
            }

            if (_vp.isPlaying)
            {
                playbackStarted = true;
                t += Time.unscaledDeltaTime;
            }

            if (CutsceneVideoPlayback.UpdateStallTimer(_vp, playbackStarted, ref stallTimer))
            {
                if (log) Debug.LogWarning("[IslandIntroCutscene] Playback stalled after tab/window change — continuing.");
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
        var fileName = _currentCutscene.videoFileName;
        WebGLVideoPrefetch.StartFile(fileName);

        var loadingMax = Mathf.Max(loadingScreenMaxSeconds, prepareTimeout);
        var loadingSettings = VideoLoadingScreen.DefaultSettings(
            loadingPageSprite, loadingScreenMinSeconds, loadingMax, log);

        yield return VideoLoadingScreen.CoShowWhilePreparing(
            _vp,
            loadingSettings,
            this,
            beginPrepare: () =>
            {
                var playbackUrl = CutsceneVideoPrefetch.ResolvePlaybackFile(fileName);
                if (_vp.url != playbackUrl)
                {
                    if (_vp.isPlaying) _vp.Stop();
                    _vp.url = playbackUrl;
                }
                _vp.skipOnDrop = CutsceneVideoPrefetch.IsPrefetchReady(fileName) ? false : true;
            },
            additionalReadyCheck: () => CutsceneVideoPrefetch.IsPrefetchReady(fileName),
            hideWhenDone: false);
#else
        _vp.url = System.IO.Path.Combine(Application.streamingAssetsPath, _currentCutscene.videoFileName);
        _vp.Prepare();

        if (log) Debug.Log($"[IslandIntroCutscene] Waiting for video: {_currentCutscene.videoFileName}");

        var loadingMax = Mathf.Max(loadingScreenMaxSeconds, prepareTimeout);
        var loadingSettings = VideoLoadingScreen.DefaultSettings(
            loadingPageSprite, loadingScreenMinSeconds, loadingMax, log);

        yield return VideoLoadingScreen.CoShowWhilePreparing(_vp, loadingSettings, this, hideWhenDone: false);
#endif

        if (!_vp.isPrepared)
        {
            if (log) Debug.LogWarning($"[IslandIntroCutscene] Video did not prepare within loading screen — skipping.");
            VideoLoadingScreen.Hide();
            FinishAndTravel();
            yield break;
        }

        if (log) Debug.Log("[IslandIntroCutscene] Prepared.");
    }

    void OnVideoFinished(VideoPlayer player)
    {
        if (!_isPlaying) return;
        _videoEnded = true;
        if (log) Debug.Log("[IslandIntroCutscene] Video finished.");
    }

    // WebGL: called from PageVisibility.jslib when the browser tab is hidden/shown.
    public void OnBrowserVisibilityChanged(int hidden)
    {
        HandleAppVisibilityChanged(hidden == 0);
    }

    void OnApplicationPause(bool pauseStatus)
    {
        HandleAppVisibilityChanged(!pauseStatus);
    }

    void OnApplicationFocus(bool hasFocus)
    {
        HandleAppVisibilityChanged(hasFocus);
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

    void HandleAppVisibilityChanged(bool isVisibleAndFocused)
    {
        if (_vp == null || !_isPlaying || _videoEnded) return;

        if (!isVisibleAndFocused)
            CutsceneVideoSuspend.OnVisibilityLost(_vp, ref _suspend, _videoAudio);
        else
            CutsceneVideoSuspend.OnVisibilityGained(
                _vp, ref _suspend, this, _isPlaying, _videoEnded, _videoAudio,
                () => _ignoreSkipInputUntil = Time.unscaledTime + 0.35f,
                () =>
                {
                    if (log) Debug.LogWarning("[IslandIntroCutscene] Could not resume video after focus change — continuing.");
                    _videoEnded = true;
                });
    }

    void FinishAndTravel()
    {
        CleanUpVideo();

        CutsceneWorldHide.End();
        CutsceneWebGLVideoOutput.Hide();
        if (rawImage)
            CutsceneVideoLayout.Restore(rawImage);

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
        CutsceneVideoSuspend.Reset(ref _suspend);
        _videoEnded = false;
        // Do NOT Release() the RenderTexture — it is an Inspector-assigned asset shared
        // across multiple cutscene plays. Releasing it permanently frees GPU memory and
        // the next cutscene would render to a dead texture (frozen first frame, no visuals).
        _vp.url = "";
    }

    void OnDestroy()
    {
        CutsceneWorldHide.End();
    }
}

