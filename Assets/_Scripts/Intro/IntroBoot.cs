using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem; // if you use the new Input System

public class IntroBoot : MonoBehaviour
{
    [Header("Video")]
    [Tooltip("Relative to StreamingAssets, e.g. intro.mp4")]
    public string videoFileName = "intro.mp4";
    public bool audioEnabled = true;
    public bool allowSkip = true;
    public float minUnskippableSeconds = 1.5f;   // prevent accidental skip at first frame
    public bool showSkipAfterDelay = true;
    public float showSkipDelay = 2f;

    [Header("Rendering")]
    public bool renderToCamera = true;           // easiest: draws on camera near/far plane
    public Camera targetCamera;                  // assign your main camera in this scene
    public UnityEngine.UI.RawImage rawImage;     // only used if renderToCamera = false
    public RenderTexture tempRenderTexture;      // assign an RT that matches display

    [Header("Next Scene")]
    public string mainSceneName = "Main";
    public bool fadeToBlack = true;
    public CanvasGroup fadeGroup;                // optional: black Image with CanvasGroup alpha
    public float fadeDuration = 0.35f;

    [Header("WebGL – Participant Login")]
    [Tooltip("Assign the ParticipantLoginScreen component here. " +
             "It is shown first (before the thumbnail) so the player can enter their info. " +
             "Leave empty to skip (e.g. when URL params supply the ID already).")]
    public ParticipantLoginScreen loginScreen;

    [Header("WebGL – Thumbnail Gate")]
    [Tooltip("Full-screen image shown on WebGL before the intro video starts. " +
             "Player presses Enter (or Space) to dismiss it and begin playback.")]
    public UnityEngine.UI.Image thumbnailImage;

    [Tooltip("Optional text shown while the video is buffering ('Loading…') " +
             "and then updated to 'Press Enter to start' once ready.")]
    public TMPro.TMP_Text loadingLabel;

    [Tooltip("Text shown while the video is buffering.")]
    public string loadingText = "Loading…";

    [Tooltip("Text shown once the video is buffered and ready to play.")]
    public string readyText = "Click anywhere or press Enter to start";

    [Tooltip("How long to wait for the video to buffer before giving up (seconds).")]
    public float prepareTimeout = 20f;

    [Header("Loading screen — before intro video")]
    [Tooltip("Optional override. Default: Resources/UI/LoadingPage.png")]
    public Sprite loadingPageSprite;
    [Tooltip("Minimum time on loading page before intro (even if video is already buffered).")]
    public float introVideoLoadingMinSeconds = 3f;
    [Tooltip("Fallback: stop waiting and continue after this many seconds if intro.mp4 is still not ready.")]
    public float introVideoLoadingMaxSeconds = 60f;

    [Header("Loading screen — after intro (Main scene)")]
    [Tooltip("Minimum time on loading page after intro while Main finishes loading.")]
    public float mainSceneLoadingMinSeconds = 0f;
    [Tooltip("Give up waiting and activate Main after this many seconds (fallback).")]
    public float mainSceneLoadingMaxSeconds = 120f;

    [Header("Diagnostics")]
    public bool log = true;

    VideoPlayer vp;
    AudioSource _videoAudio;
    AsyncOperation preload;
    bool isVideoActive;
    bool videoEnded;
    float ignoreSkipInputUntil;
    CutsceneVideoSuspend.State _suspend;
    TMP_Text _runtimeStartPrompt;
    bool _pageGestureReceived;
    bool _awaitingUserGesture;
    Button _thumbnailContinueButton;

    const float ThumbnailMinDisplaySeconds = 1.25f;

#if UNITY_WEBGL && !UNITY_EDITOR
    /// <summary>Called from IntroBridge.jslib when the browser receives a click/key outside Unity input.</summary>
    public void OnUserGestureFromPage()
    {
        if (_awaitingUserGesture)
            _pageGestureReceived = true;
    }

    /// <summary>Unity UI click target on the thumbnail (SendMessage fallback).</summary>
    public void OnThumbnailContinueClicked()
    {
        if (_awaitingUserGesture)
            _pageGestureReceived = true;
    }
#endif

    void Awake()
    {
        // Hide both screens at startup — CoRun activates them at the right moment.
        if (loginScreen)   loginScreen.gameObject.SetActive(false);
        if (thumbnailImage) thumbnailImage.gameObject.SetActive(false);

        if (renderToCamera && !targetCamera)
            targetCamera = Camera.main;

        if (loadingPageSprite == null)
            loadingPageSprite = VideoLoadingScreen.LoadDefaultSprite();

        // Main preloads during login / intro playback; loading page shown again after intro if still finishing.

        // Build VideoPlayer
        vp = gameObject.AddComponent<VideoPlayer>();
        vp.playOnAwake = false;
        vp.isLooping = false;
        vp.waitForFirstFrame = true;
        vp.skipOnDrop = true; // Keep audio and video aligned under transient decode/network pressure.
        vp.audioOutputMode = audioEnabled ? VideoAudioOutputMode.AudioSource
                                        : VideoAudioOutputMode.None;
        if (audioEnabled)
        {
            _videoAudio = gameObject.AddComponent<AudioSource>();
            _videoAudio.playOnAwake = false;
            _videoAudio.loop = false;
            _videoAudio.spatialBlend = 0f;
            _videoAudio.dopplerLevel = 0f;
            vp.SetTargetAudioSource(0, _videoAudio);
        }

        // Source — use string concat on WebGL to guarantee forward-slash URLs
        // (Path.Combine on Windows IL2CPP can produce backslash separators which break HTTP URLs)
        vp.source = VideoSource.Url;
#if UNITY_WEBGL && !UNITY_EDITOR
        // URL set after full-file prefetch in PrepareAndPlay (blob playback avoids stream stutter).
#else
        vp.url = System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName);
#endif

        // Output
        if (renderToCamera)
        {
            vp.renderMode = VideoRenderMode.CameraNearPlane;
            vp.targetCamera = targetCamera;
            vp.targetCameraAlpha = 1f;
        }
        else
        {
            vp.renderMode = VideoRenderMode.RenderTexture;
            vp.targetTexture = tempRenderTexture;
            if (rawImage) rawImage.texture = tempRenderTexture;
        }

        CutsceneWebGLVideoOutput.Configure(this, vp, ref renderToCamera, ref rawImage, ref tempRenderTexture);

        vp.loopPointReached += OnVideoFinished;

        WebGLPageVisibility.Register(this);

#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLVideoPrefetch.StartFile(videoFileName);
        if (log) Debug.Log("[IntroBoot] WebGL: started full-file intro prefetch.");
#endif

        StartCoroutine(CoRun());
    }

    IEnumerator CoRun()
    {
        // Determine whether participant info was already supplied via URL params.
        // If so the login screen is skipped (researcher mode).
#if UNITY_WEBGL && !UNITY_EDITOR
        string absUrl = Application.absoluteURL ?? "";
        bool urlHasId = UrlHasParticipantIdQueryKey(absUrl);
#else
        bool urlHasId = false;
#endif

        bool needLogin = loginScreen != null && !urlHasId && !ParticipantSession.WasSubmitted;

        // Skip thumbnail + video ONLY when the URL explicitly sets fast=1 or skipintro=1
        // (parsed safely — no substring false positives from hashes or nested URLs).
#if UNITY_WEBGL && !UNITY_EDITOR
        bool skipAllIntro = urlHasId && UrlRequestsFastIntroSkip(absUrl);
        if (log) Debug.Log($"[IntroBoot] WebGL url='{absUrl}' urlHasId={urlHasId} skipAllIntro={skipAllIntro} needLogin={needLogin}");
#else
        bool skipAllIntro = false;
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
        if (skipAllIntro)
        {
            if (log) Debug.Log("[IntroBoot] Fast-start: ?fast=1 or ?skipintro=1 — skipping thumbnail + intro video.");
            EnsureMainPreloadStarted(lowPriority: false);
            var loadSettings = VideoLoadingScreen.DefaultSettings(
                loadingPageSprite, mainSceneLoadingMinSeconds, mainSceneLoadingMaxSeconds, log);

            VideoLoadingScreen.ShowForSceneLoad(loadSettings, this);
            VideoLoadingScreen.BeginSceneLoadActivates(
                preload,
                loadSettings,
                () =>
                {
                    if (fadeToBlack && fadeGroup) fadeGroup.alpha = 1f;
                    EnsureMainPreloadStarted(lowPriority: false);
                });

            yield return VideoLoadingScreen.CoWaitUntilSceneLoadFinished();

            if (preload == null)
                SceneManager.LoadScene(mainSceneName);
            yield break;
        }

        // ── Normal WebGL flow ────────────────────────────────────────────────────────
        // 1) Thumbnail (includes "press space to continue" artwork)
        // 2) Login if no ?num= in URL
        // 3) Loading page while intro.mp4 prepares
        // 4) Intro video → Main
        yield return CoEnsureBootUIReady();
        EnsureThumbnailContinueButton();

        if (fadeToBlack && fadeGroup) fadeGroup.alpha = 0f;
        if (thumbnailImage) thumbnailImage.gameObject.SetActive(true);
        if (_thumbnailContinueButton) _thumbnailContinueButton.gameObject.SetActive(true);
        WebGLIntroBridge.HidePageStartHint();

        if (needLogin)
        {
            if (log) Debug.Log("[IntroBoot] WebGL: thumbnail — waiting for click, Space, or Enter.");
            yield return CoWaitForUserGesture();
            if (log) Debug.Log("[IntroBoot] WebGL: gesture received — showing login screen.");

            if (thumbnailImage) thumbnailImage.gameObject.SetActive(false);
            if (_thumbnailContinueButton) _thumbnailContinueButton.gameObject.SetActive(false);

            loginScreen.gameObject.SetActive(true);
            EnsureMainPreloadStarted(lowPriority: true);
            bool submitted = false;
            loginScreen.OnSubmitted += () => submitted = true;
            while (!submitted) yield return null;
            if (log) Debug.Log($"[IntroBoot] Login: #{ParticipantSession.Number} {ParticipantSession.LastName} {ParticipantSession.SessionDate}");
        }
        else
        {
            if (log) Debug.Log("[IntroBoot] WebGL: thumbnail — waiting for click, Space, or Enter.");
            yield return CoWaitForUserGesture();
            if (log) Debug.Log("[IntroBoot] WebGL: gesture received.");
            EnsureMainPreloadStarted(lowPriority: true);
        }

        if (thumbnailImage) thumbnailImage.gameObject.SetActive(false);
        if (_thumbnailContinueButton) _thumbnailContinueButton.gameObject.SetActive(false);
        if (loginScreen) loginScreen.gameObject.SetActive(false);
        // ────────────────────────────────────────────────────────────────────────────
#else
        // ── Editor / standalone: login on black screen then play immediately ─────────
        if (fadeToBlack && fadeGroup) fadeGroup.alpha = 0f;

        if (needLogin)
        {
            loginScreen.gameObject.SetActive(true);
            EnsureMainPreloadStarted(lowPriority: true);
            bool submitted = false;
            loginScreen.OnSubmitted += () => submitted = true;
            if (log) Debug.Log("[IntroBoot] Editor: showing login screen.");
            while (!submitted) yield return null;
            if (log) Debug.Log($"[IntroBoot] Login: #{ParticipantSession.Number} {ParticipantSession.LastName} {ParticipantSession.SessionDate}");

            loginScreen.gameObject.SetActive(false);
        }
        else
        {
            EnsureMainPreloadStarted(lowPriority: true);
        }

        if (fadeToBlack && fadeGroup) fadeGroup.alpha = 1f;
        // ────────────────────────────────────────────────────────────────────────────
#endif

        yield return PrepareAndPlay();
        if (!isVideoActive)
        {
            // Prepare failed / skipped by timeout path; continue without entering playback loop.
            goto AfterPlayback;
        }

        // Fade in underlying frame quickly
        if (fadeToBlack && fadeGroup)
            yield return StartCoroutine(CoFade(fadeGroup, 1f, 0f, 0.25f));

        // Wait for video to finish (or skip if allowed).
        float t = 0f;
        float stallTimer = 0f;
        bool playbackStarted = false;
        while (!videoEnded)
        {
            if (CutsceneVideoPlayback.ShouldSkip(allowSkip, minUnskippableSeconds, t, ignoreSkipInputUntil))
            {
                if (log) Debug.Log("[IntroBoot] Skipped.");
                break;
            }

            if (_suspend.AppSuspended)
            {
                yield return null;
                continue;
            }

            if (vp != null && vp.isPlaying)
            {
                playbackStarted = true;
                t += Time.unscaledDeltaTime;
            }

            if (CutsceneVideoPlayback.UpdateStallTimer(vp, playbackStarted, ref stallTimer))
            {
                if (log) Debug.LogWarning("[IntroBoot] Playback stalled after tab/window change — continuing.");
                break;
            }

            yield return null;
        }

AfterPlayback:
        var mainLoadSettings = VideoLoadingScreen.DefaultSettings(
            loadingPageSprite, mainSceneLoadingMinSeconds, mainSceneLoadingMaxSeconds, log);

        // Cover the last intro frame before tearing down video output.
        VideoLoadingScreen.ShowForSceneLoad(mainLoadSettings, this);
        if (fadeToBlack && fadeGroup) fadeGroup.alpha = 1f;

        CleanUpVideo();

        if (log && preload != null)
            Debug.Log($"[IntroBoot] Post-intro Main preload at {preload.progress * 100f:0}%.");

        VideoLoadingScreen.BeginSceneLoadActivates(
            preload,
            mainLoadSettings,
            () => EnsureMainPreloadStarted(lowPriority: false));

        yield return VideoLoadingScreen.CoWaitUntilSceneLoadFinished();

        if (preload == null)
            SceneManager.LoadScene(mainSceneName);
    }

    IEnumerator PrepareAndPlay()
    {
        var loadingMax = Mathf.Max(introVideoLoadingMaxSeconds, prepareTimeout);
        var loadingSettings = VideoLoadingScreen.DefaultSettings(
            loadingPageSprite, introVideoLoadingMinSeconds, loadingMax, log);

#if UNITY_WEBGL && !UNITY_EDITOR
        yield return CoPrepareIntroWebGL(loadingSettings);
        VideoLoadingScreen.SetCaption(null);
#else
        yield return VideoLoadingScreen.CoShowWhilePreparing(
            vp,
            loadingSettings,
            this,
            () =>
            {
                if (vp != null && string.IsNullOrEmpty(vp.url))
                    vp.url = System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName);
            },
            hideWhenDone: false);
#endif

        if (vp == null || !vp.isPrepared)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (log) Debug.LogWarning("[IntroBoot] Prefetch prepare failed — trying direct stream URL.");
            var streamUrl = WebGLVideoPrefetch.BuildStreamingAssetsUrl(videoFileName);
            if (vp != null && !string.IsNullOrEmpty(streamUrl))
            {
                if (vp.isPlaying) vp.Stop();
                vp.url = streamUrl;
                vp.Prepare();
                float streamWait = 0f;
                var streamMax = Mathf.Max(prepareTimeout * 2f, 45f);
                while (!vp.isPrepared && streamWait < streamMax)
                {
                    streamWait += Time.unscaledDeltaTime;
                    yield return null;
                }
                if (log) Debug.Log($"[IntroBoot] Stream prepare: prepared={vp.isPrepared}, wait={streamWait:0.0}s.");
            }
#endif
        }

        if (vp == null || !vp.isPrepared)
        {
            if (log) Debug.LogWarning("[IntroBoot] Video not prepared after loading screen — skipping intro, loading Main.");
            VideoLoadingScreen.SetCaption("Loading game…");
            isVideoActive = false;
            yield break;
        }

        if (log) Debug.Log("[IntroBoot] Playing.");
        videoEnded = false;
        CutsceneVideoSuspend.Reset(ref _suspend);
        isVideoActive = true;
        ignoreSkipInputUntil = Time.unscaledTime + 0.25f;

        SessionPlayTimeTracker.StartSession();

        CutsceneWebGLVideoOutput.Show(vp);

        if (!CutsceneWebGLVideoOutput.UsesOverlayPath)
            CutsceneWorldHide.Begin(renderToCamera ? targetCamera : null);
        if (!renderToCamera && rawImage)
        {
            rawImage.gameObject.SetActive(true);
            CutsceneVideoLayout.ApplyFullscreen(rawImage);
        }

        vp.Play();
        VideoLoadingScreen.Hide();

        // Preload Main in the background while intro plays (low priority — most load finishes before intro ends).
        EnsureMainPreloadStarted(lowPriority: true);
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    /// <summary>
    /// Wait for full-file prefetch, then prepare on blob URL. isPrepared alone is not enough on WebGL
    /// (first frame can be ready while the rest still streams and stutters during Play).
    /// </summary>
    IEnumerator CoPrepareIntroWebGL(VideoLoadingScreen.Settings settings)
    {
        yield return CutsceneWebGLPrepare.CoPrepare(
            vp, videoFileName, settings, this, prepareTimeout, log, "IntroBoot");
    }
#endif

    void EnsureMainPreloadStarted(bool lowPriority)
    {
        Application.backgroundLoadingPriority = lowPriority
            ? ThreadPriority.Low
            : ThreadPriority.High;

        if (preload != null)
        {
            if (log && !lowPriority)
                Debug.Log($"[IntroBoot] Main preload boosted (progress={preload.progress * 100f:0}%).");
            return;
        }

        preload = SceneManager.LoadSceneAsync(mainSceneName);
        if (preload == null) return;

        preload.allowSceneActivation = false;

        if (log) Debug.Log($"[IntroBoot] Main preload started (priority={(lowPriority ? "low" : "high")}).");
    }

    void OnVideoFinished(VideoPlayer player)
    {
        if (!isVideoActive) return;
        videoEnded = true;
        if (log) Debug.Log("[IntroBoot] Video finished.");
    }

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

    void HandleAppVisibilityChanged(bool isVisibleAndFocused)
    {
        if (vp == null || !isVideoActive || videoEnded) return;

        if (!isVisibleAndFocused)
            CutsceneVideoSuspend.OnVisibilityLost(vp, ref _suspend, _videoAudio);
        else
            CutsceneVideoSuspend.OnVisibilityGained(
                vp, ref _suspend, this, isVideoActive, videoEnded, _videoAudio,
                () => ignoreSkipInputUntil = Time.unscaledTime + 0.35f,
                () =>
                {
                    if (log) Debug.LogWarning("[IntroBoot] Could not resume video after focus change — continuing.");
                    videoEnded = true;
                });
    }

    bool AnySkipPressed() => CutsceneVideoPlayback.AnySkipPressed();

#if UNITY_WEBGL && !UNITY_EDITOR
    IEnumerator CoEnsureBootUIReady()
    {
        FixBootCanvas(thumbnailImage != null ? thumbnailImage.GetComponentInParent<Canvas>() : null);
        if (loginScreen != null)
            FixBootCanvas(loginScreen.GetComponentInParent<Canvas>());

        Canvas.ForceUpdateCanvases();
        for (int i = 0; i < 3; i++)
            yield return null;
        Canvas.ForceUpdateCanvases();
    }

    static void FixBootCanvas(Canvas canvas)
    {
        if (canvas == null) return;

        var root = canvas.GetComponent<RectTransform>();
        if (root != null && root.localScale.sqrMagnitude < 0.001f)
            root.localScale = Vector3.one;

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null && scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (scaler != null)
        {
            scaler.enabled = false;
            scaler.enabled = true;
        }
    }

    void EnsureThumbnailContinueButton()
    {
        if (_thumbnailContinueButton != null || thumbnailImage == null) return;

        var parent = thumbnailImage.transform.parent;
        if (parent == null) return;

        var go = new GameObject("ThumbnailContinue");
        go.transform.SetParent(parent, false);
        go.transform.SetAsLastSibling();

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.004f);
        img.raycastTarget = true;

        _thumbnailContinueButton = go.AddComponent<Button>();
        _thumbnailContinueButton.transition = Selectable.Transition.None;
        _thumbnailContinueButton.onClick.AddListener(OnThumbnailContinueClicked);
        go.SetActive(false);
    }

    // Waits for click, touch, or Enter/Space (browser user-gesture for audio + fullscreen).
    IEnumerator CoWaitForUserGesture()
    {
        _awaitingUserGesture = true;
        _pageGestureReceived = false;
        WebGLIntroBridge.Setup(gameObject.name);
        WebGLIntroBridge.Reset();
        WebGLIntroBridge.Disarm();

        float shownAt = Time.unscaledTime;
        while (Time.unscaledTime < shownAt + ThumbnailMinDisplaySeconds)
            yield return null;

        for (int i = 0; i < 10; i++)
            yield return null;

        WebGLIntroBridge.Arm();

        while (!_pageGestureReceived && !AnyStartGesturePressedWebGL())
            yield return null;

        _awaitingUserGesture = false;
        WebGLIntroBridge.Disarm();
        WebGLIntroBridge.HidePageStartHint();
        WebGLFullscreen.Request();
        yield return null;
    }

    static bool AnyStartGesturePressedWebGL()
    {
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            return true;

        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            return true;

        var kb = Keyboard.current;
        if (kb != null && (kb.enterKey.wasPressedThisFrame ||
                           kb.numpadEnterKey.wasPressedThisFrame ||
                           kb.spaceKey.wasPressedThisFrame))
            return true;

        return false;
    }
#endif

    void ShowStartPrompt(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            HideStartPrompt();
            return;
        }

        if (loadingLabel != null)
        {
            loadingLabel.gameObject.SetActive(true);
            loadingLabel.text = text;
            return;
        }

        EnsureRuntimeStartPrompt();
        if (_runtimeStartPrompt == null) return;
        _runtimeStartPrompt.text = text;
        _runtimeStartPrompt.gameObject.SetActive(true);
    }

    void HideStartPrompt()
    {
        if (loadingLabel != null)
            loadingLabel.gameObject.SetActive(false);

        if (_runtimeStartPrompt != null)
            _runtimeStartPrompt.gameObject.SetActive(false);
    }

    void EnsureRuntimeStartPrompt()
    {
        if (_runtimeStartPrompt != null) return;

        Transform parent = null;
        if (thumbnailImage != null)
            parent = thumbnailImage.transform.parent;
        if (parent == null && loginScreen != null)
            parent = loginScreen.transform;
        if (parent == null) return;

        var go = new GameObject("StartPrompt");
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 48f);
        rt.sizeDelta = new Vector2(900f, 80f);

        _runtimeStartPrompt = go.AddComponent<TextMeshProUGUI>();
        _runtimeStartPrompt.alignment = TextAlignmentOptions.Center;
        _runtimeStartPrompt.fontSize = 28f;
        _runtimeStartPrompt.color = Color.white;
        _runtimeStartPrompt.raycastTarget = false;
        _runtimeStartPrompt.enableWordWrapping = true;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);

        go.SetActive(false);
    }

    void SetLabel(string text)
    {
        ShowStartPrompt(text);
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

    void CleanUpVideo()
    {
        if (vp == null) return;
        isVideoActive = false;
        CutsceneWorldHide.End();
        CutsceneWebGLVideoOutput.Hide();
        if (rawImage)
            CutsceneVideoLayout.Restore(rawImage);
        CutsceneVideoSuspend.Reset(ref _suspend);
        videoEnded = false;
        vp.loopPointReached -= OnVideoFinished;
        if (vp.isPlaying) vp.Stop();
        // Do not Release() the Inspector-assigned RenderTexture.
        vp.url = "";
    }

    /// <summary>Query string only (?foo=bar&…), no leading ?, fragment stripped.</summary>
    static string WebGlRawQueryString(string absoluteUrl)
    {
        if (string.IsNullOrEmpty(absoluteUrl)) return "";
        int q = absoluteUrl.IndexOf('?');
        if (q < 0) return "";
        string rest = absoluteUrl.Substring(q + 1);
        int hash = rest.IndexOf('#');
        if (hash >= 0) rest = rest.Substring(0, hash);
        return rest;
    }

    static bool TryGetQueryValue(string query, string key, out string value)
    {
        value = null;
        if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(key)) return false;
        foreach (var part in query.Split('&'))
        {
            if (string.IsNullOrEmpty(part)) continue;
            int eq = part.IndexOf('=');
            string k = eq >= 0 ? part.Substring(0, eq) : part;
            string v = eq >= 0 ? part.Substring(eq + 1) : "";
            if (!string.Equals(k, key, StringComparison.OrdinalIgnoreCase)) continue;
            try { value = Uri.UnescapeDataString(v); }
            catch { value = v; }
            return true;
        }
        return false;
    }

    static bool UrlHasParticipantIdQueryKey(string absoluteUrl)
    {
        string q = WebGlRawQueryString(absoluteUrl);
        if (TryGetQueryValue(q, "num", out var num) && !string.IsNullOrWhiteSpace(num))
            return true;
        if (TryGetQueryValue(q, "pid", out var pid) && !string.IsNullOrWhiteSpace(pid))
            return true;
        return false;
    }

    /// <summary>
    /// Explicit opt-in only: <c>?num=…&amp;fast=1</c> or <c>skipintro=1</c> (value may be "true").
    /// </summary>
    static bool UrlRequestsFastIntroSkip(string absoluteUrl)
    {
        string query = WebGlRawQueryString(absoluteUrl);
        if (TryGetQueryValue(query, "fast", out var v) &&
            (v == "1" || string.Equals(v, "true", StringComparison.OrdinalIgnoreCase)))
            return true;
        if (TryGetQueryValue(query, "skipintro", out v) &&
            (v == "1" || string.Equals(v, "true", StringComparison.OrdinalIgnoreCase)))
            return true;
        return false;
    }

    void OnDestroy()
    {
        CutsceneWorldHide.End();
    }
}
