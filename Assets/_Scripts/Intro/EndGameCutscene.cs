using System.Collections;
using MoxoCPT;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Plays one of two end-game cutscenes (Ending1 / Ending2) on demand.
/// Intended to be called from Dragon dialogue option UnityEvents.
/// </summary>
public class EndGameCutscene : MonoBehaviour
{
    [Header("Video Files")]
    [Tooltip("Relative to StreamingAssets, e.g. Ending1.mp4")]
    public string ending1FileName = "Ending1.mp4";

    [Tooltip("Relative to StreamingAssets, e.g. Ending2.mp4")]
    public string ending2FileName = "Ending2.mp4";

    [Header("Playback Options")]
    public bool audioEnabled = true;
    public bool allowSkip = true;
    public float minUnskippableSeconds = 1.5f;

    [Header("Rendering")]
    [Tooltip("If true, renders on camera near plane. If false, uses RenderTexture + RawImage.")]
    public bool renderToCamera = false;
    public Camera targetCamera;
    public RawImage rawImage;
    public RenderTexture tempRenderTexture;

    [Header("Fade (optional)")]
    public bool fadeToBlack = false;
    public CanvasGroup fadeGroup;
    public float fadeDuration = 0.35f;

    [Header("Ending Screen")]
    [Tooltip("Image revealed on WebGL after the video ends. Sits on screen indefinitely " +
             "(Application.Quit does nothing in a browser, so this replaces it).")]
    public Image endingImage;

    [Tooltip("Optional UI Text: digits only. Sum of correct target hits across CARDS, BREAD, POISON, SKULL for this session.")]
    public Text fourIslandTotalScoreDigits;

    [Tooltip("Optional TextMeshPro: digits only (same total). Use this OR fourIslandTotalScoreDigits.")]
    public TextMeshProUGUI fourIslandTotalScoreDigitsTMP;

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

    [Header("Editor preview")]
    [Tooltip("After the ending image is shown in the Editor, wait this many realtime seconds before stopping Play Mode (so you can verify layout and score).")]
    [SerializeField] private float editorEndingPreviewHoldSeconds = 10f;

    [Header("Diagnostics")]
    public bool log = true;

    VideoPlayer _vp;
    AudioSource _videoAudio;
    bool _isPlaying;
    bool _finished;
    string _currentFile;
    bool _videoEnded;
    float _ignoreSkipInputUntil;
    CutsceneVideoSuspend.State _suspend;

    void Awake()
    {
        // Keep ending image hidden until the video finishes.
        if (endingImage) endingImage.gameObject.SetActive(false);

        // Auto-fallback for Editor/standalone only — WebGL uses CutsceneWebGLVideoOutput overlay.
#if !UNITY_WEBGL || UNITY_EDITOR
        if (!renderToCamera && rawImage == null && tempRenderTexture == null)
        {
            renderToCamera = true;
            if (log) Debug.Log("[EndGameCutscene] No RawImage/RenderTexture assigned — auto-switching to CameraNearPlane rendering.");
        }
#endif

        if (renderToCamera && !targetCamera)
            targetCamera = Camera.main;

        if (!targetCamera && renderToCamera)
            Debug.LogWarning("[EndGameCutscene] renderToCamera=true but no camera found! Video may not be visible.");

        // Build VideoPlayer once.
        _vp = gameObject.AddComponent<VideoPlayer>();
        _vp.playOnAwake = false;
        _vp.isLooping = false;
        _vp.waitForFirstFrame = true;
        _vp.skipOnDrop = true; // Helps prevent A/V desync on constrained WebGL clients.
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

        _vp.source = VideoSource.Url;

        if (renderToCamera)
        {
            _vp.renderMode = VideoRenderMode.CameraNearPlane;
            _vp.targetCamera = targetCamera;
            _vp.targetCameraAlpha = 0f; // start hidden
        }
        else
        {
            _vp.renderMode = VideoRenderMode.RenderTexture;
            _vp.targetTexture = tempRenderTexture;
            if (rawImage) rawImage.texture = tempRenderTexture;
            if (rawImage) rawImage.gameObject.SetActive(false);
        }

        CutsceneWebGLVideoOutput.Configure(this, _vp, ref renderToCamera, ref rawImage, ref tempRenderTexture);

        _vp.loopPointReached += OnVideoFinished;

        WebGLPageVisibility.Register(this);

        if (log) Debug.Log($"[EndGameCutscene] Awake complete. renderToCamera={renderToCamera}, camera={targetCamera?.name ?? "none"}, rawImage={rawImage?.name ?? "none"}, rt={tempRenderTexture?.name ?? "none"}");
    }

    // These two methods are what you will hook from Dialogue option UnityEvents.
    public void PlayEnding1()
    {
        if (log) Debug.Log("[EndGameCutscene] PlayEnding1() called.");
        PlayFile(ending1FileName);
    }

    public void PlayEnding2()
    {
        if (log) Debug.Log("[EndGameCutscene] PlayEnding2() called.");
        PlayFile(ending2FileName);
    }

    void PlayFile(string fileName)
    {
        if (_isPlaying || _finished)
        {
            if (log) Debug.LogWarning("[EndGameCutscene] Already playing or finished; ignoring new request.");
            return;
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            if (log) Debug.LogWarning("[EndGameCutscene] No file name configured.");
            return;
        }

        _currentFile = fileName.Trim();

#if UNITY_WEBGL && !UNITY_EDITOR
        // Start buffering immediately so the video is ready by the time CoRun reaches PrepareAndPlay.
        _vp.url = Application.streamingAssetsPath + "/" + _currentFile;
        _vp.Prepare();
        if (log) Debug.Log($"[EndGameCutscene] WebGL: early prepare started for {_currentFile}");
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

        if (log) Debug.Log("[EndGameCutscene] Playing.");
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
                if (log) Debug.Log("[EndGameCutscene] Skipped.");
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
                if (log) Debug.LogWarning("[EndGameCutscene] Playback stalled after tab/window change — continuing.");
                break;
            }

            yield return null;
        }

        if (fadeToBlack && fadeGroup)
            yield return StartCoroutine(CoFade(fadeGroup, fadeGroup.alpha, 1f, fadeDuration));

        Finish();
    }

    IEnumerator PrepareAndPlay()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // URL and Prepare() were already called in PlayFile for early buffering.
        // Only call again if not already started (safety fallback).
        if (!_vp.isPrepared && string.IsNullOrEmpty(_vp.url))
        {
            _vp.url = Application.streamingAssetsPath + "/" + _currentFile;
            _vp.Prepare();
        }
#else
        _vp.url = System.IO.Path.Combine(Application.streamingAssetsPath, _currentFile);
        _vp.Prepare();
#endif

        if (log) Debug.Log($"[EndGameCutscene] Waiting for video: {_currentFile}");

        var loadingMax = Mathf.Max(loadingScreenMaxSeconds, prepareTimeout);
        var loadingSettings = VideoLoadingScreen.DefaultSettings(
            loadingPageSprite, loadingScreenMinSeconds, loadingMax, log);

        yield return VideoLoadingScreen.CoShowWhilePreparing(_vp, loadingSettings, this, hideWhenDone: false);

        if (!_vp.isPrepared)
        {
            if (log) Debug.LogWarning("[EndGameCutscene] Video did not prepare within loading screen; skipping.");
            VideoLoadingScreen.Hide();
            Finish();
            yield break;
        }

        if (log) Debug.Log("[EndGameCutscene] Prepared.");
    }

    void OnVideoFinished(VideoPlayer player)
    {
        if (!_isPlaying) return;
        _videoEnded = true;
        if (log) Debug.Log("[EndGameCutscene] Video finished.");
    }

    public void OnBrowserVisibilityChanged(int hidden)
    {
        HandleAppVisibilityChanged(hidden == 0);
    }

    bool AnySkipPressed() => CutsceneVideoPlayback.AnySkipPressed();

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

    void OnApplicationPause(bool pauseStatus)
    {
        HandleAppVisibilityChanged(!pauseStatus);
    }

    void OnApplicationFocus(bool hasFocus)
    {
        HandleAppVisibilityChanged(hasFocus);
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
                    if (log) Debug.LogWarning("[EndGameCutscene] Could not resume video after focus change — continuing.");
                    _videoEnded = true;
                });
    }

    void Finish()
    {
        if (_finished) return;
        _finished = true;

        SessionPlayTimeTracker.FinishSession();

        CleanUpVideo();

        if (renderToCamera && _vp != null && _vp.targetCamera != null)
            _vp.targetCameraAlpha = 0f;
        if (!renderToCamera && rawImage)
            rawImage.gameObject.SetActive(false);

        _isPlaying = false;

#if UNITY_WEBGL && !UNITY_EDITOR
        // Application.Quit() is a no-op in WebGL.
        // Reveal the ending image over the existing black screen and stay there.
        StartCoroutine(CoShowEndingScreen());
#elif UNITY_EDITOR
        // Same ending UI as WebGL, then exit Play Mode after a hold so you can verify score/layout.
        StartCoroutine(CoShowEndingScreenThenExitEditor());
#else
        if (fadeGroup) fadeGroup.alpha = 0f;
        Application.Quit();
#endif
    }

    /// <summary>
    /// The screen is already faded to black when this runs (after the video).
    /// Show the ending image behind the black overlay, then fade the overlay out.
    /// </summary>
    IEnumerator CoShowEndingScreen()
    {
        ApplyFourIslandTotalToEndingUI();
        EndingScreenLayout.Apply(endingImage, fourIslandTotalScoreDigits, fourIslandTotalScoreDigitsTMP);

        if (endingImage) endingImage.gameObject.SetActive(true);
        if (fadeGroup)
            yield return StartCoroutine(CoFade(fadeGroup, 1f, 0f, fadeDuration));
        else
            yield return null;

        if (log) Debug.Log("[EndGameCutscene] Ending screen displayed.");
    }

#if UNITY_EDITOR
    IEnumerator CoShowEndingScreenThenExitEditor()
    {
        yield return StartCoroutine(CoShowEndingScreen());

        float hold = Mathf.Max(0f, editorEndingPreviewHoldSeconds);
        if (log) Debug.Log($"[EndGameCutscene] Editor: holding ending screen for {hold}s then exiting Play Mode.");
        if (hold > 0f)
            yield return new WaitForSecondsRealtime(hold);

        UnityEditor.EditorApplication.isPlaying = false;
    }
#endif

    void ApplyFourIslandTotalToEndingUI()
    {
        int n = CPTScoreRuntime.CumulativeCorrectHitsFourBaseIslands;
        if (fourIslandTotalScoreDigits)
            fourIslandTotalScoreDigits.text = n.ToString();
        if (fourIslandTotalScoreDigitsTMP)
            fourIslandTotalScoreDigitsTMP.text = n.ToString();
    }

    void CleanUpVideo()
    {
        if (_vp == null) return;
        if (_vp.isPlaying) _vp.Stop();
        CutsceneVideoSuspend.Reset(ref _suspend);
        CutsceneWorldHide.End();
        CutsceneWebGLVideoOutput.Hide();
        _videoEnded = false;
        // Do NOT Release() the RenderTexture — it is an Inspector-assigned asset.
        // Release() permanently frees GPU memory and would break any subsequent play.
        _vp.url = "";
    }

    void OnDestroy()
    {
        CutsceneWorldHide.End();
    }
}

