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

    [Header("Diagnostics")]
    public bool log = true;

    VideoPlayer vp;
    AsyncOperation preload;

    void Awake()
    {
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

        // Source
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName);
        vp.source = VideoSource.Url;
        vp.url = path;

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
        StartCoroutine(CoRun());
    }

    IEnumerator CoRun()
    {
        // Optional fade in from black
        if (fadeToBlack && fadeGroup) { fadeGroup.alpha = 1f; }
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
        if (log) Debug.Log($"[IntroBoot] Preparing video: {vp.url}");
        vp.Prepare();
        while (!vp.isPrepared) yield return null;

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
        // New Input System
        var kb = Keyboard.current;
        var gp = Gamepad.current;
        if ((kb != null && (kb.anyKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) ||
            (gp != null && (gp.startButton.wasPressedThisFrame || gp.aButton.wasPressedThisFrame)))
            return true;

        // Also support old Input Manager if enabled
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

    void CleanUpVideo()
    {
        if (vp == null) return;
        vp.loopPointReached -= OnVideoFinished;
        if (vp.isPlaying) vp.Stop();
        if (vp.targetTexture) vp.targetTexture.Release();
    }
}
