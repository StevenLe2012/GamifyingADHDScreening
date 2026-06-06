using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ensures UI-based cutscene video covers the full screen (RenderTexture + RawImage path).
/// </summary>
public static class CutsceneVideoLayout
{
    struct SavedRect
    {
        public RectTransform Rect;
        public Vector2 AnchorMin;
        public Vector2 AnchorMax;
        public Vector2 OffsetMin;
        public Vector2 OffsetMax;
        public Vector2 AnchoredPosition;
        public Vector3 LocalScale;
    }

    static SavedRect? _savedRawImage;

    public static void ApplyFullscreen(RawImage rawImage)
    {
        if (rawImage == null) return;

        var rt = rawImage.rectTransform;
        _savedRawImage = new SavedRect
        {
            Rect = rt,
            AnchorMin = rt.anchorMin,
            AnchorMax = rt.anchorMax,
            OffsetMin = rt.offsetMin,
            OffsetMax = rt.offsetMax,
            AnchoredPosition = rt.anchoredPosition,
            LocalScale = rt.localScale
        };

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    public static void Restore(RawImage rawImage)
    {
        if (rawImage == null || _savedRawImage == null) return;
        if (_savedRawImage.Value.Rect != rawImage.rectTransform) return;

        var s = _savedRawImage.Value;
        var rt = s.Rect;
        if (rt == null) return;

        rt.anchorMin = s.AnchorMin;
        rt.anchorMax = s.AnchorMax;
        rt.offsetMin = s.OffsetMin;
        rt.offsetMax = s.OffsetMax;
        rt.anchoredPosition = s.AnchoredPosition;
        rt.localScale = s.LocalScale;
        _savedRawImage = null;
    }
}
