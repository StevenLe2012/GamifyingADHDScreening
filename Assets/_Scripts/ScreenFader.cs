using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    [SerializeField] private float defaultDuration = 0.3f;

    private CanvasGroup cg;

    private void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
    }

    // Immediate states
    public void SetBlack() { SetAlpha(1f); }
    public void SetClear() { SetAlpha(0f); }

    // Timed fades (use defaultDuration if none passed)
    public Coroutine FadeOut(float duration = -1f) // clear -> black
    {
        return StartCoroutine(Fade(0f, 1f, duration < 0f ? defaultDuration : duration));
    }

    public Coroutine FadeIn(float duration = -1f)  // black -> clear
    {
        return StartCoroutine(Fade(1f, 0f, duration < 0f ? defaultDuration : duration));
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f) { SetAlpha(to); yield break; }

        float t = 0f;
        SetAlpha(from);
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / duration);
            SetAlpha(a);
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float a)
    {
        if (cg == null) cg = GetComponent<CanvasGroup>();
        cg.alpha = Mathf.Clamp01(a);
        cg.blocksRaycasts = a > 0.001f;  // block clicks while black
        cg.interactable = a > 0.001f;
    }
}
