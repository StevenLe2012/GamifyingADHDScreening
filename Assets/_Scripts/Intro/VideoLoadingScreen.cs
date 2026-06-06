using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Full-screen loading page (Resources/UI/LoadingPage.png) shown while content loads.
/// </summary>
public static class VideoLoadingScreen
{
    const int SortOrder = 32759;
    const int SortOrderSceneLoad = 32762;

    static GameObject _root;
    static Image _image;
    static Sprite _defaultSprite;
    static bool _sceneLoadFinished;

    public struct Settings
    {
        public Sprite PageSprite;
        /// <summary>Minimum time the page stays visible.</summary>
        public float MinSeconds;
        /// <summary>Maximum wait when waiting on a readiness check (video prepare, etc.).</summary>
        public float MaxSeconds;
        public bool Log;
    }

    public static Settings DefaultSettings(Sprite overrideSprite = null, float minSeconds = 3f, float maxSeconds = 5f, bool log = false)
    {
        return new Settings
        {
            PageSprite = overrideSprite != null ? overrideSprite : LoadDefaultSprite(),
            MinSeconds = Mathf.Max(0f, minSeconds),
            MaxSeconds = Mathf.Max(minSeconds, maxSeconds),
            Log = log
        };
    }

    /// <summary>
    /// Loading page stays visible until the video is prepared (and optional extra checks pass),
    /// for at least <see cref="Settings.MinSeconds"/>, up to <see cref="Settings.MaxSeconds"/> fallback.
    /// </summary>
    public static IEnumerator CoShowWhilePreparing(
        VideoPlayer vp,
        Settings settings,
        MonoBehaviour host = null,
        Action beginPrepare = null,
        Func<bool> additionalReadyCheck = null,
        bool hideWhenDone = true)
    {
        EnsureOverlay(settings.PageSprite, host != null ? host.transform : null, persistAcrossScenes: false);
        _root.SetActive(true);
        yield return null;

        beginPrepare?.Invoke();
        if (vp != null && !string.IsNullOrEmpty(vp.url) && !vp.isPrepared)
            vp.Prepare();

        float elapsed = 0f;
        while (true)
        {
            elapsed += Time.unscaledDeltaTime;

            if (IsVideoPrepareReady(vp, additionalReadyCheck) && elapsed >= settings.MinSeconds)
                break;

            if (elapsed >= settings.MaxSeconds)
                break;

            yield return null;
        }

        if (settings.Log)
        {
            var ready = IsVideoPrepareReady(vp, additionalReadyCheck);
            Debug.Log($"[VideoLoadingScreen] Video prepare overlay done after {elapsed:0.0}s (ready={ready}).");
        }

        if (hideWhenDone)
            Hide();
    }

    static bool IsVideoPrepareReady(VideoPlayer vp, Func<bool> additionalReadyCheck)
    {
        if (vp != null && !vp.isPrepared)
            return false;
        if (additionalReadyCheck != null && !additionalReadyCheck())
            return false;
        return true;
    }

    /// <summary>
    /// Loading page until <paramref name="isReady"/> is true (after MinSeconds), up to MaxSeconds.
    /// </summary>
    public static IEnumerator CoShowWhileWaiting(
        Settings settings,
        MonoBehaviour host,
        Func<bool> isReady,
        Action onBegin = null)
    {
        EnsureOverlay(settings.PageSprite, host != null ? host.transform : null, persistAcrossScenes: false);
        _root.SetActive(true);
        yield return null;

        onBegin?.Invoke();

        float elapsed = 0f;
        while (true)
        {
            elapsed += Time.unscaledDeltaTime;

            if (isReady != null && elapsed >= settings.MinSeconds && isReady())
                break;

            if (elapsed >= settings.MaxSeconds)
                break;

            yield return null;
        }

        if (settings.Log)
        {
            var ready = isReady != null && isReady();
            Debug.Log($"[VideoLoadingScreen] Done after {elapsed:0.0}s (ready={ready}).");
        }

        Hide();
    }

    public static void Hide()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    /// <summary>
    /// Destroys the overlay (e.g. after persisting across a scene change).
    /// </summary>
    public static void HideAndDestroy()
    {
        if (_root == null) return;
        UnityEngine.Object.Destroy(_root);
        _root = null;
        _image = null;
    }

    /// <summary>
    /// Loading page stays up until async scene load finishes activating (not just progress 0.9).
    /// Uses DontDestroyOnLoad so it remains visible through the scene switch.
    /// </summary>
    /// <summary>
    /// Show loading page immediately and keep it through async load + Main scene startup.
    /// </summary>
    public static void ShowForSceneLoad(Settings settings, MonoBehaviour host = null)
    {
        MainSceneLoadBridge.Reset();
        _sceneLoadFinished = false;
        EnsureOverlay(settings.PageSprite, host != null ? host.transform : null, persistAcrossScenes: true);
        ApplyPersistAcrossScenes();
        _root.SetActive(true);
    }

    /// <summary>
    /// Runs on a DontDestroyOnLoad runner so BootIntro unload does not cancel the hide.
    /// </summary>
    public static void BeginSceneLoadActivates(
        AsyncOperation loadOp,
        Settings settings,
        Action ensureLoadStarted = null)
    {
        _sceneLoadFinished = false;
        VideoLoadingScreenRunner.Ensure().Run(CoSceneLoadActivates(loadOp, settings, ensureLoadStarted));
    }

    public static IEnumerator CoWaitUntilSceneLoadFinished()
    {
        while (!_sceneLoadFinished)
            yield return null;
    }

    /// <summary>Called when Main has rendered — dismisses the overlay once.</summary>
    public static void OnMainSceneReady()
    {
        if (_sceneLoadFinished) return;
        _sceneLoadFinished = true;
        HideAndDestroy();
    }

    static IEnumerator CoSceneLoadActivates(
        AsyncOperation loadOp,
        Settings settings,
        Action ensureLoadStarted)
    {
        yield return null;

        ensureLoadStarted?.Invoke();

        float elapsed = 0f;
        while (elapsed < settings.MaxSeconds)
        {
            elapsed += Time.unscaledDeltaTime;

            if (loadOp == null)
                break;

            if (loadOp.isDone)
            {
                if (elapsed >= settings.MinSeconds)
                    break;
            }
            else if (elapsed >= settings.MinSeconds && loadOp.progress >= 0.9f)
            {
                break;
            }

            yield return null;
        }

        if (loadOp != null && !loadOp.isDone)
        {
            loadOp.allowSceneActivation = true;
            while (!loadOp.isDone)
                yield return null;
        }

        // Stay visible until Main signals it has rendered (GameManager.Start), with a safety timeout.
        float postActivate = 0f;
        const float postActivateMax = 45f;
        while (!MainSceneLoadBridge.IsReady && postActivate < postActivateMax)
        {
            postActivate += Time.unscaledDeltaTime;
            yield return null;
        }

        if (settings.Log)
        {
            Debug.Log(
                $"[VideoLoadingScreen] Scene load overlay done after {elapsed:0.0}s load + {postActivate:0.0}s Main startup (ready={MainSceneLoadBridge.IsReady}).");
        }

        if (!MainSceneLoadBridge.IsReady)
            OnMainSceneReady();
    }

    public static void Show(Settings settings, MonoBehaviour host, bool persistAcrossScenes = false)
    {
        EnsureOverlay(settings.PageSprite, host != null ? host.transform : null, persistAcrossScenes);
        _root.SetActive(true);
    }

    public static Sprite LoadDefaultSprite()
    {
        if (_defaultSprite != null)
            return _defaultSprite;

        _defaultSprite = Resources.Load<Sprite>("UI/LoadingPage");
        if (_defaultSprite == null)
            Debug.LogWarning("[VideoLoadingScreen] Missing Resources/UI/LoadingPage.png — assign a sprite on IntroBoot / cutscene components.");

        return _defaultSprite;
    }

    static void ApplyPersistAcrossScenes()
    {
        if (_root == null) return;
        UnityEngine.Object.DontDestroyOnLoad(_root);
        _root.transform.SetParent(null);
        var canvas = _root.GetComponent<Canvas>();
        if (canvas != null)
            canvas.sortingOrder = SortOrderSceneLoad;
    }

    static void EnsureOverlay(Sprite sprite, Transform parent, bool persistAcrossScenes)
    {
        if (_root != null && _image != null)
        {
            if (sprite != null)
                _image.sprite = sprite;
            if (persistAcrossScenes)
                ApplyPersistAcrossScenes();
            else if (parent != null)
                _root.transform.SetParent(parent, false);
            return;
        }

        _root = new GameObject("[VideoLoadingScreen]");
        if (!persistAcrossScenes && parent != null)
            _root.transform.SetParent(parent, false);

        var canvas = _root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = persistAcrossScenes ? SortOrderSceneLoad : SortOrder;

        if (persistAcrossScenes)
            ApplyPersistAcrossScenes();

        var scaler = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(_root.transform, false);
        var bg = bgGo.AddComponent<Image>();
        bg.color = Color.black;
        bg.raycastTarget = false;
        StretchFullScreen(bg.rectTransform);

        var imgGo = new GameObject("LoadingPage");
        imgGo.transform.SetParent(_root.transform, false);
        _image = imgGo.AddComponent<Image>();
        _image.preserveAspect = true;
        _image.raycastTarget = false;
        _image.color = Color.white;
        StretchFullScreen(_image.rectTransform);

        if (sprite != null)
            _image.sprite = sprite;

        _root.SetActive(false);
    }

    static void StretchFullScreen(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
    }
}
