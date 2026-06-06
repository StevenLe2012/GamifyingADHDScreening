using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Normalises the end-game score screen for any resolution / WebGL letterbox.
/// </summary>
public static class EndingScreenLayout
{
    // Fraction of EndingImage rect where the score digits sit on the artwork.
    const float ScoreAnchorX = 0.5f;
    const float ScoreAnchorY = 0.42f;

    public static void Apply(Image endingImage, Text scoreText, TextMeshProUGUI scoreTmp)
    {
        if (endingImage == null) return;

        var cutscene = endingImage.GetComponentInParent<EndGameCutscene>();
        if (cutscene != null)
            cutscene.transform.localScale = Vector3.one;

        var canvas = endingImage.canvas;
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;

            var canvasRt = canvas.GetComponent<RectTransform>();
            if (canvasRt != null)
            {
                canvasRt.localScale = Vector3.one;
                canvasRt.anchorMin = Vector2.zero;
                canvasRt.anchorMax = Vector2.one;
                canvasRt.offsetMin = Vector2.zero;
                canvasRt.offsetMax = Vector2.zero;
                canvasRt.anchoredPosition = Vector2.zero;
            }

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        var imageRt = endingImage.rectTransform;
        imageRt.anchorMin = Vector2.zero;
        imageRt.anchorMax = Vector2.one;
        imageRt.offsetMin = Vector2.zero;
        imageRt.offsetMax = Vector2.zero;
        imageRt.anchoredPosition = Vector2.zero;
        imageRt.localScale = Vector3.one;
        endingImage.preserveAspect = true;

        LayoutScoreLabel(scoreText != null ? scoreText.rectTransform : null);
        LayoutScoreLabel(scoreTmp != null ? scoreTmp.rectTransform : null);

        if (scoreTmp != null)
        {
            scoreTmp.enableAutoSizing = true;
            scoreTmp.fontSizeMin = 24;
            scoreTmp.fontSizeMax = 96;
            scoreTmp.fontSize = 48;
            scoreTmp.alignment = TextAlignmentOptions.Center;
        }

        if (scoreText != null)
        {
            scoreText.alignment = TextAnchor.MiddleCenter;
            scoreText.resizeTextForBestFit = true;
            scoreText.resizeTextMinSize = 24;
            scoreText.resizeTextMaxSize = 96;
        }
    }

    static void LayoutScoreLabel(RectTransform rt)
    {
        if (rt == null) return;

        var anchor = new Vector2(ScoreAnchorX, ScoreAnchorY);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(500, 120);
        rt.localScale = Vector3.one;
    }
}
