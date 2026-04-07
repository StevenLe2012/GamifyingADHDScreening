using System.Collections;
using UnityEngine;
using UnityEngine.Video;
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
    public UnityEngine.UI.RawImage rawImage;
    public RenderTexture tempRenderTexture;

    [Header("Fade (optional)")]
    public bool fadeToBlack = false;
    public CanvasGroup fadeGroup;
    public float fadeDuration = 0.35f;

    [Header("Ending Screen")]
    [Tooltip("Image revealed on WebGL after the video ends. Sits on screen indefinitely " +
             "(Application.Quit does nothing in a browser, so this replaces it).")]
    public UnityEngine.UI.Image endingImage;

    [Header("Diagnostics")]
    public bool log = true;

    VideoPlayer _vp;
    bool _isPlaying;
    string _currentFile;

    void Awake()
    {
        // Keep ending image hidden until the video finishes.
        if (endingImage) endingImage.gameObject.SetActive(false);

        if (renderToCamera && !targetCamera)
            targetCamera = Camera.main;

        // Build VideoPlayer once.
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

        _vp.source = VideoSource.Url;

        // Output routing
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

        _vp.loopPointReached += OnVideoFinished;
    }

    // These two methods are what you will hook from Dialogue option UnityEvents.
    public void PlayEnding1()
    {
        PlayFile(ending1FileName);
    }

    public void PlayEnding2()
    {
        PlayFile(ending2FileName);
    }

    void PlayFile(string fileName)
    {
        if (_isPlaying)
        {
            if (log) Debug.LogWarning("[EndGameCutscene] Already playing; ignoring new request.");
            return;
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            if (log) Debug.LogWarning("[EndGameCutscene] No file name configured.");
            return;
        }

        _currentFile = fileName.Trim();
        StartCoroutine(CoRun());
    }

    IEnumerator CoRun()
    {
        _isPlaying = true;

        if (renderToCamera && _vp != null && _vp.targetCamera != null)
            _vp.targetCameraAlpha = 1f;
        if (!renderToCamera && rawImage)
            rawImage.gameObject.SetActive(true);

        if (fadeToBlack && fadeGroup)
        {
            fadeGroup.alpha = 1f;
        }

        yield return PrepareAndPlay();

        if (fadeToBlack && fadeGroup)
            yield return StartCoroutine(CoFade(fadeGroup, 1f, 0f, 0.25f));

        float t = 0f;
        while (_vp.isPlaying)
        {
            t += Time.unscaledDeltaTime;

            if (allowSkip && t > minUnskippableSeconds && AnySkipPressed())
            {
                if (log) Debug.Log("[EndGameCutscene] Skipped.");
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
        // Application.streamingAssetsPath is a URL on WebGL — use string concat, not Path.Combine.
        _vp.url = Application.streamingAssetsPath + "/" + _currentFile;
#else
        _vp.url = System.IO.Path.Combine(Application.streamingAssetsPath, _currentFile);
#endif

        if (log) Debug.Log($"[EndGameCutscene] Preparing video: {_vp.url}");
        _vp.Prepare();

#if UNITY_WEBGL && !UNITY_EDITOR
        float timeout = 8f;
        while (!_vp.isPrepared && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }
        if (!_vp.isPrepared)
        {
            if (log) Debug.LogWarning("[EndGameCutscene] WebGL: video did not prepare in time; skipping.");
            Finish();
            yield break;
        }
#else
        while (!_vp.isPrepared) yield return null;
#endif

        if (log) Debug.Log("[EndGameCutscene] Playing.");
        _vp.Play();
    }

    void OnVideoFinished(VideoPlayer player)
    {
        if (!_isPlaying) return;
        if (log) Debug.Log("[EndGameCutscene] Video finished.");
        // CoRun handles the rest.
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

    void Finish()
    {
        CleanUpVideo();

        if (renderToCamera && _vp != null && _vp.targetCamera != null)
            _vp.targetCameraAlpha = 0f;
        if (!renderToCamera && rawImage)
            rawImage.gameObject.SetActive(false);

        _isPlaying = false;

#if UNITY_WEBGL && !UNITY_EDITOR
        // Application.Quit() is a no-op in WebGL.
        // Instead, reveal the ending image over the existing black screen and stay there.
        StartCoroutine(CoShowEndingScreen());
#elif UNITY_EDITOR
        if (fadeGroup) fadeGroup.alpha = 0f;
        UnityEditor.EditorApplication.isPlaying = false;
#else
        if (fadeGroup) fadeGroup.alpha = 0f;
        Application.Quit();
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    // The screen is already faded to black when this runs.
    // Show the ending image behind the black overlay, then fade the overlay out
    // so the image is revealed. The game then sits on that image indefinitely.
    IEnumerator CoShowEndingScreen()
    {
        if (endingImage) endingImage.gameObject.SetActive(true);
        if (fadeGroup)
            yield return StartCoroutine(CoFade(fadeGroup, 1f, 0f, fadeDuration));
        else
            yield return null;

        if (log) Debug.Log("[EndGameCutscene] Ending screen displayed.");
    }
#endif

    void CleanUpVideo()
    {
        if (_vp == null) return;
        if (_vp.isPlaying) _vp.Stop();
        // Do NOT Release() the RenderTexture — it is an Inspector-assigned asset.
        // Release() permanently frees GPU memory and would break any subsequent play.
        _vp.url = "";
    }
}

