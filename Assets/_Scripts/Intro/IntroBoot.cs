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

    void Awake()
    {
        // Ensure thumbnail is hidden at startup; it is shown only in the WebGL gate below.
        if (thumbnailImage) thumbnailImage.gameObject.SetActive(false);

        if (renderToCamera && !targetCamera)
            targetCamera = Camera.main;

        // Preload next scene while video plays (DISABLED for now)
        // preload = SceneManager.LoadSceneAsync(mainSceneName);
        // if (preload != null) preload.allowSceneActivation = false;

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
#if UNITY_WEBGL && !UNITY_EDITOR
        // ── Thumbnail gate ──────────────────────────────────────────────────────────
        // Show the thumbnail immediately while the video buffers in the background.
        // The label cycles through loading → ready states so the user always knows
        // what is happening. Enter/Space is only accepted once the video is prepared
        // (or the timeout expires), guaranteeing the video plays instantly on press.
        if (fadeToBlack && fadeGroup) fadeGroup.alpha = 0f;
        if (thumbnailImage) thumbnailImage.gameObject.SetActive(true);
        SetLabel(loadingText);

        // Wait for the video to finish buffering (started in Awake).
        if (log) Debug.Log("[IntroBoot] WebGL: waiting for video to buffer…");
        float elapsed = 0f;
        while (!vp.isPrepared && elapsed < prepareTimeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (vp.isPrepared)
        {
            if (log) Debug.Log($"[IntroBoot] WebGL: video ready after {elapsed:0.0}s — waiting for Enter.");
            SetLabel(readyText);
        }
        else
        {
            // Timed out — still let the user in; the video will try to play anyway.
            if (log) Debug.LogWarning($"[IntroBoot] WebGL: video not ready after {prepareTimeout}s — proceeding anyway.");
            SetLabel(readyText);
        }

        // Now wait for user gesture (unlocks browser audio AND confirms they are ready).
        yield return CoWaitForEnter();
        if (log) Debug.Log("[IntroBoot] WebGL: Enter pressed — starting video.");

        if (thumbnailImage) thumbnailImage.gameObject.SetActive(false);
        SetLabel(null);
        if (fadeToBlack && fadeGroup) fadeGroup.alpha = 1f;
        // ───────────────────────────────────────────────────────────────────────────
#else
        // Non-WebGL: start with black screen as before.
        if (fadeToBlack && fadeGroup) fadeGroup.alpha = 1f;
#endif

        yield return PrepareAndPlay();

        // Fade in underlying frame quickly
        if (fadeToBlack && fadeGroup)
            yield return StartCoroutine(CoFade(fadeGroup, 1f, 0f, 0.25f));

        // Show "skip" hint after delay
        float t = 0f;
        while (vp.isPlaying)
        {
            t += Time.unscaledDeltaTime;

            if (allowSkip && t > minUnskippableSeconds && AnySkipPressed())
            {
                if (log) Debug.Log("[IntroBoot] Skipped.");
                break;
            }

            yield return null;
        }

        // Fade out
        if (fadeToBlack && fadeGroup)
            yield return StartCoroutine(CoFade(fadeGroup, fadeGroup.alpha, 1f, fadeDuration));

        // Finish
        CleanUpVideo();
        if (preload != null) preload.allowSceneActivation = true;
        else SceneManager.LoadScene(mainSceneName);
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
            yield break;
        }
#else
        if (log) Debug.Log($"[IntroBoot] Preparing video: {vp.url}");
        vp.Prepare();
        while (!vp.isPrepared) yield return null;
#endif

        if (log) Debug.Log("[IntroBoot] Playing.");
        vp.Play();
    }

    void OnVideoFinished(VideoPlayer player)
    {
        if (log) Debug.Log("[IntroBoot] Video finished.");
        // Let CoRun handle transition; nothing else here.
    }

    bool AnySkipPressed()
    {
        var kb = Keyboard.current;
        var gp = Gamepad.current;
        if ((kb != null && (kb.anyKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) ||
            (gp != null && (gp.startButton.wasPressedThisFrame || gp.aButton.wasPressedThisFrame)))
            return true;

#if !UNITY_WEBGL
        // On WebGL, Input.anyKeyDown includes mouse button presses which would cause an
        // accidental skip when the player clicks anywhere on the video. Use keyboard only.
        if (Input.anyKeyDown) return true;
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
        vp.loopPointReached -= OnVideoFinished;
        if (vp.isPlaying) vp.Stop();
        // Do not Release() the Inspector-assigned RenderTexture.
        vp.url = "";
    }
}
