using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// CameraNearPlane video often renders black (especially with CutsceneWorldHide cullingMask=0).
/// Uses a fullscreen Screen Space Overlay RawImage + RenderTexture instead when needed.
/// </summary>
public static class CutsceneWebGLVideoOutput
{
    static GameObject _root;
    static RawImage _overlayImage;
    static RenderTexture _overlayTexture;
    static bool _usingRuntimeOverlay;

    public static bool UsesOverlayUi => _usingRuntimeOverlay;

    /// <summary>True when Configure switched to UI+RenderTexture output (not CameraNearPlane).</summary>
    public static bool UsesOverlayPath { get; private set; }

    /// <summary>
    /// Call after VideoPlayer is created. Ensures UI+RT output when CameraNearPlane would be used.
    /// </summary>
    public static bool Configure(
        MonoBehaviour host,
        VideoPlayer vp,
        ref bool renderToCamera,
        ref RawImage rawImage,
        ref RenderTexture tempRenderTexture)
    {
        UsesOverlayPath = false;
        _usingRuntimeOverlay = false;

        if (rawImage != null && tempRenderTexture != null)
        {
            renderToCamera = false;
            ApplyVideoPlayer(vp, false, null, rawImage, tempRenderTexture, 1f);
            UsesOverlayPath = true;
            return true;
        }

        if (!renderToCamera)
            return false;

        EnsureRuntimeOverlay(host != null ? host.transform : null);
        rawImage = _overlayImage;
        tempRenderTexture = _overlayTexture;
        renderToCamera = false;
        _usingRuntimeOverlay = true;
        UsesOverlayPath = true;

        ApplyVideoPlayer(vp, false, null, rawImage, tempRenderTexture, 1f);
        if (host != null)
            Debug.Log("[CutsceneWebGLVideoOutput] Using runtime fullscreen overlay for cutscene video.");
        return true;
    }

    public static void Show(VideoPlayer vp = null)
    {
        if (_overlayImage != null && _overlayTexture != null)
        {
            _overlayImage.texture = _overlayTexture;
            if (vp != null)
            {
                vp.renderMode = VideoRenderMode.RenderTexture;
                vp.targetCamera = null;
                vp.targetTexture = _overlayTexture;
            }
        }

        if (_root != null)
            _root.SetActive(true);
        if (_overlayImage != null)
            _overlayImage.gameObject.SetActive(true);
    }

    public static void Hide()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    public static void ApplyVideoPlayer(
        VideoPlayer vp,
        bool renderToCamera,
        Camera targetCamera,
        RawImage rawImage,
        RenderTexture tempRenderTexture,
        float cameraAlpha)
    {
        if (vp == null) return;

        if (renderToCamera)
        {
            vp.renderMode = VideoRenderMode.CameraNearPlane;
            vp.targetCamera = targetCamera;
            vp.targetCameraAlpha = cameraAlpha;
            vp.targetTexture = null;
            return;
        }

        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.targetCamera = null;
        vp.targetTexture = tempRenderTexture;
        if (rawImage != null)
        {
            rawImage.texture = tempRenderTexture;
            CutsceneVideoLayout.ApplyFullscreen(rawImage);
        }
    }

    static void EnsureRuntimeOverlay(Transform host)
    {
        if (_root != null && _overlayImage != null && _overlayTexture != null)
            return;

        _root = new GameObject("[CutsceneVideoOverlay]");
        Object.DontDestroyOnLoad(_root);

        var canvas = _root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;

        var scaler = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        _root.AddComponent<GraphicRaycaster>();

        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(_root.transform, false);
        var bg = bgGo.AddComponent<Image>();
        bg.color = Color.black;
        bg.raycastTarget = false;
        var bgRt = bg.rectTransform;
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        var imgGo = new GameObject("Video");
        imgGo.transform.SetParent(_root.transform, false);
        _overlayImage = imgGo.AddComponent<RawImage>();
        _overlayImage.color = Color.white;
        _overlayImage.raycastTarget = false;
        CutsceneVideoLayout.ApplyFullscreen(_overlayImage);

        _overlayTexture = new RenderTexture(1280, 720, 0);
        _overlayTexture.Create();
        _overlayImage.texture = _overlayTexture;

        _root.SetActive(false);
    }
}
