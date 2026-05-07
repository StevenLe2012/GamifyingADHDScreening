using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    public string readyText = "Press Enter to start";

    [Tooltip("How long to wait for the video to buffer before giving up (seconds).")]
    public float prepareTimeout = 20f;

    [Header("Diagnostics")]
    public bool log = true;

    VideoPlayer vp;
    AsyncOperation preload;
    bool isVideoActive;
    bool videoEnded;
    bool appSuspended;
    bool resumeAfterAppReturn;
    float ignoreSkipInputUntil;

    void Awake()
    {
        // Hide both screens at startup — CoRun activates them at the right moment.
        if (loginScreen)   loginScreen.gameObject.SetActive(false);
        if (thumbnailImage) thumbnailImage.gameObject.SetActive(false);

        if (renderToCamera && !targetCamera)
            targetCamera = Camera.main;

        // Scene is preloaded later (after login) so it doesn't block the Editor on startup.

        // Build VideoPlayer
        vp = gameObject.AddComponent<VideoPlayer>();
        vp.playOnAwake = false;
        vp.isLooping = false;
        vp.audioOutputMode = audioEnabled ? VideoAudioOutputMode.AudioSource
                                        : VideoAudioOutputMode.None;
        if (audioEnabled)
        {
            var audio = gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.loop = false;
            vp.SetTargetAudioSource(0, audio);
        }

        // Source — use string concat on WebGL to guarantee forward-slash URLs
        // (Path.Combine on Windows IL2CPP can produce backslash separators which break HTTP URLs)
        vp.source = VideoSource.Url;
#if UNITY_WEBGL && !UNITY_EDITOR
        vp.url = Application.streamingAssetsPath + "/" + videoFileName;
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

        vp.loopPointReached += OnVideoFinished;

#if UNITY_WEBGL && !UNITY_EDITOR
        // Start buffering the video immediately in the background so it is ready
        // (or close to ready) by the time the user presses Enter on the thumbnail.
        if (log) Debug.Log("[IntroBoot] WebGL: starting early video prepare…");
        vp.Prepare();
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
            // ── Fast-start: straight to Main (black screen while loading) ─────────────
            if (log) Debug.Log("[IntroBoot] Fast-start: ?fast=1 or ?skipintro=1 — skipping thumbnail + intro video.");
            if (fadeToBlack && fadeGroup) fadeGroup.alpha = 1f;  // stay black during load
            preload = SceneManager.LoadSceneAsync(mainSceneName);
            if (preload != null) preload.allowSceneActivation = false;

            if (preload != null)
            {
                while (preload.progress < 0.9f) yield return null;
                preload.allowSceneActivation = true;
            }
            else
            {
                SceneManager.LoadScene(mainSceneName);
            }
            yield break; // done — skip the rest of CoRun
        }

        // ── Normal WebGL flow ────────────────────────────────────────────────────────
        //   1. Thumbnail → player presses Enter → audio unlocked
        //   2. Login screen pops up over thumbnail → player fills in + clicks Start
        //   3. Fade to black → intro video plays
        if (fadeToBlack && fadeGroup) fadeGroup.alpha = 0f;   // clear so thumbnail is visible
        if (thumbnailImage) thumbnailImage.gameObject.SetActive(true);

        if (needLogin)
        {
            // Step 1: thumbnail only — wait for Enter (also unlocks browser audio).
            SetLabel(readyText);    // e.g. "Press Enter to start"
            if (log) Debug.Log("[IntroBoot] WebGL: thumbnail shown — waiting for Enter.");
            yield return CoWaitForEnter();
            if (log) Debug.Log("[IntroBoot] WebGL: Enter pressed — showing login screen.");

            // Step 2: login screen pops up; start preloading Main while player fills the form.
            SetLabel(null);
            loginScreen.gameObject.SetActive(true);
            preload = SceneManager.LoadSceneAsync(mainSceneName);
            if (preload != null) preload.allowSceneActivation = false;
            bool submitted = false;
            loginScreen.OnSubmitted += () => submitted = true;
            while (!submitted) yield return null;
            if (log) Debug.Log($"[IntroBoot] Login: #{ParticipantSession.Number} {ParticipantSession.LastName} {ParticipantSession.SessionDate}");
        }
        else
        {
            // No login needed (URL supplied num/pid) — thumbnail + buffer wait only.
            SetLabel(loadingText);
            if (log) Debug.Log("[IntroBoot] WebGL: waiting for video to buffer…");
            preload = SceneManager.LoadSceneAsync(mainSceneName);
            if (preload != null) preload.allowSceneActivation = false;
            float elapsed = 0f;
            while (!vp.isPrepared && elapsed < prepareTimeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            if (log) Debug.Log(vp.isPrepared
                ? $"[IntroBoot] WebGL: video ready after {elapsed:0.0}s."
                : $"[IntroBoot] WebGL: buffer timeout — proceeding anyway.");
            SetLabel(readyText);
            yield return CoWaitForEnter();
            if (log) Debug.Log("[IntroBoot] WebGL: Enter pressed.");
        }

        // Step 3: hide thumbnail + login, fade to black, play video.
        if (thumbnailImage) thumbnailImage.gameObject.SetActive(false);
        SetLabel(null);
        if (fadeToBlack && fadeGroup) fadeGroup.alpha = 1f;
        // ────────────────────────────────────────────────────────────────────────────
#else
        // ── Editor / standalone: login on black screen then play immediately ─────────
        if (fadeToBlack && fadeGroup) fadeGroup.alpha = 0f;

        if (needLogin)
        {
            loginScreen.gameObject.SetActive(true);
            bool submitted = false;
            loginScreen.OnSubmitted += () => submitted = true;
            if (log) Debug.Log("[IntroBoot] Editor: showing login screen.");
            while (!submitted) yield return null;
            if (log) Debug.Log($"[IntroBoot] Login: #{ParticipantSession.Number} {ParticipantSession.LastName} {ParticipantSession.SessionDate}");

            loginScreen.gameObject.SetActive(false);
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
        while (!videoEnded)
        {
            if (appSuspended)
            {
                yield return null;
                continue;
            }

            if (vp != null && vp.isPlaying)
                t += Time.unscaledDeltaTime;

            if (allowSkip && t > minUnskippableSeconds && Time.unscaledTime >= ignoreSkipInputUntil && AnySkipPressed())
            {
                if (log) Debug.Log("[IntroBoot] Skipped.");
                break;
            }

            yield return null;
        }

AfterPlayback:
        // Fade to black — screen is now fully black before any scene loading starts.
        if (fadeToBlack && fadeGroup)
            yield return StartCoroutine(CoFade(fadeGroup, fadeGroup.alpha, 1f, fadeDuration));

        CleanUpVideo();

        // Start async load now that the screen is black — any hitch is invisible.
        // On WebGL preload was already started during login so this is skipped.
        if (preload == null)
        {
            preload = SceneManager.LoadSceneAsync(mainSceneName);
            if (preload != null) preload.allowSceneActivation = false;
        }

        // Wait for load then activate.
        if (preload != null)
        {
            while (preload.progress < 0.9f)
            {
                if (log) Debug.Log($"[IntroBoot] Loading… {preload.progress * 100:0}%");
                yield return null;
            }
            preload.allowSceneActivation = true;
        }
        else
        {
            SceneManager.LoadScene(mainSceneName);
        }
    }

    IEnumerator PrepareAndPlay()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // On WebGL, Prepare() was already called in Awake and we waited in CoRun.
        // If somehow it still isn't ready, give it one more short chance.
        if (!vp.isPrepared)
        {
            if (log) Debug.Log("[IntroBoot] WebGL: video not yet prepared — brief extra wait.");
            float extra = 5f;
            while (!vp.isPrepared && extra > 0f)
            {
                extra -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (!vp.isPrepared)
        {
            if (log) Debug.LogWarning("[IntroBoot] WebGL: skipping video (never became prepared).");
            isVideoActive = false;
            yield break;
        }
#else
        if (log) Debug.Log($"[IntroBoot] Preparing video: {vp.url}");
        vp.Prepare();
        float prepareTimer = 0f;
        while (!vp.isPrepared && prepareTimer < prepareTimeout)
        {
            prepareTimer += Time.unscaledDeltaTime;
            yield return null;
        }
        if (!vp.isPrepared)
        {
            if (log) Debug.LogWarning("[IntroBoot] Video did not prepare in time — skipping.");
            isVideoActive = false;
            yield break;
        }
#endif

        if (log) Debug.Log("[IntroBoot] Playing.");
        videoEnded = false;
        appSuspended = false;
        resumeAfterAppReturn = false;
        isVideoActive = true;
        ignoreSkipInputUntil = Time.unscaledTime + 0.25f;
        vp.Play();
    }

    void OnVideoFinished(VideoPlayer player)
    {
        if (!isVideoActive) return;
        if (appSuspended) return;
        videoEnded = true;
        if (log) Debug.Log("[IntroBoot] Video finished.");
        // Let CoRun handle transition; nothing else here.
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
        if (vp == null || !isVideoActive) return;

        if (!isVisibleAndFocused)
        {
            appSuspended = true;
            resumeAfterAppReturn = vp.isPlaying;
            if (resumeAfterAppReturn)
                vp.Pause();
            return;
        }

        appSuspended = false;
        ignoreSkipInputUntil = Time.unscaledTime + 0.35f;
        if (resumeAfterAppReturn && !vp.isPlaying)
            vp.Play();

        resumeAfterAppReturn = false;
    }

    bool AnySkipPressed()
    {
        var kb = Keyboard.current;
        var gp = Gamepad.current;

        // Do not treat Space as skip during intro playback.
        if ((kb != null && (kb.escapeKey.wasPressedThisFrame ||
                            kb.enterKey.wasPressedThisFrame ||
                            kb.numpadEnterKey.wasPressedThisFrame)) ||
            (gp != null && gp.startButton.wasPressedThisFrame))
            return true;

#if !UNITY_WEBGL
        // Keep legacy input support without using Space as a skip key.
        if (Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
            return true;
#endif
        return false;
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    // Waits until the player presses Enter or Space (satisfies the browser user-gesture
    // requirement for audio autoplay), then yields one extra frame so that key-press is
    // fully consumed and doesn't register as a skip in the video loop that follows.
    IEnumerator CoWaitForEnter()
    {
        var kb = Keyboard.current;
        while (true)
        {
            if (kb != null && (kb.enterKey.wasPressedThisFrame ||
                               kb.numpadEnterKey.wasPressedThisFrame ||
                               kb.spaceKey.wasPressedThisFrame))
                break;
            if (Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter) ||
                Input.GetKeyDown(KeyCode.Space))
                break;
            yield return null;
        }
        yield return null; // flush — prevent the key from being seen by the skip loop
    }
#endif

    void SetLabel(string text)
    {
        if (loadingLabel == null) return;
        if (string.IsNullOrEmpty(text))
            loadingLabel.gameObject.SetActive(false);
        else
        {
            loadingLabel.gameObject.SetActive(true);
            loadingLabel.text = text;
        }
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
        appSuspended = false;
        resumeAfterAppReturn = false;
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
        return TryGetQueryValue(q, "num", out _) || TryGetQueryValue(q, "pid", out _);
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
}
