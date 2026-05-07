using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class BootVideoLoader : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "Core";
    [SerializeField] private bool allowSkipWithSpace = false;

    private VideoPlayer vp;
    private bool wasPlayingBeforeBackground;
    private bool appSuspended;
    private bool loadTriggered;
    private float ignoreInputUntil;

    private void Awake()
    {
        vp = GetComponent<VideoPlayer>();
        vp.loopPointReached += OnVideoFinished;
    }

    private void Start()
    {
        vp.Play();
        ignoreInputUntil = Time.unscaledTime + 0.2f;
    }

    private void Update()
    {
        if (!allowSkipWithSpace || appSuspended || Time.unscaledTime < ignoreInputUntil) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            LoadNext();
        }
    }

    private void OnVideoFinished(VideoPlayer _)
    {
        if (appSuspended) return;
        LoadNext();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        HandleAppVisibilityChanged(!pauseStatus);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        HandleAppVisibilityChanged(hasFocus);
    }

    private void HandleAppVisibilityChanged(bool isVisibleAndFocused)
    {
        if (vp == null) return;

        if (!isVisibleAndFocused)
        {
            appSuspended = true;
            wasPlayingBeforeBackground = vp.isPlaying;
            if (wasPlayingBeforeBackground)
                vp.Pause();
            return;
        }

        appSuspended = false;
        ignoreInputUntil = Time.unscaledTime + 0.35f;
        if (wasPlayingBeforeBackground && !vp.isPlaying)
            vp.Play();

        wasPlayingBeforeBackground = false;
    }

    private void LoadNext()
    {
        if (loadTriggered) return;
        loadTriggered = true;
        vp.loopPointReached -= OnVideoFinished;
        SceneManager.LoadScene(nextSceneName);
    }
}
