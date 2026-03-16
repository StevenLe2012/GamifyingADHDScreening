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

    [Header("Diagnostics")]
    public bool log = true;

    VideoPlayer _vp;
    bool _isPlaying;
    string _currentFile;

    void Awake()
    {
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
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, _currentFile);
        _vp.url = path;

        if (log) Debug.Log($"[EndGameCutscene] Preparing video: {_vp.url}");
        _vp.Prepare();
        while (!_vp.isPrepared) yield return null;

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

        if (Input.anyKeyDown) return true;
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
        if (fadeGroup) fadeGroup.alpha = 0f;

        _isPlaying = false;

        // End the game after the final cutscene.
#if UNITY_EDITOR
        // Stop play mode in the editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Quit the built application
        Application.Quit();
#endif
    }

    void CleanUpVideo()
    {
        if (_vp == null) return;
        if (_vp.isPlaying) _vp.Stop();
        if (_vp.targetTexture) _vp.targetTexture.Release();
    }
}

