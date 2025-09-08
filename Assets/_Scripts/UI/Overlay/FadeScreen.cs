using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class FadeScreen : MonoBehaviour
{
    [SerializeField] private Color _fadeColor = Color.black;     // color of fade (RGB matters, alpha controlled in code)
    [SerializeField] private float _teleportFadeDuration = 0.25f;

    private Renderer _rend;
    private readonly int _baseColorID = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        _rend = GetComponent<Renderer>();

        // Important: duplicate material so we don’t overwrite the shared one
        _rend.material = Instantiate(_rend.material);

        // Start transparent (alpha = 0), so scene is visible by default
        SetAlpha(0f);
    }

    private void OnEnable()
    {
        // Safety: if object is toggled back on, keep it clear unless told otherwise
        SetAlpha(0f);
    }

    // --- Public helpers ---
    public void SetBlack() => SetAlpha(1f);
    public void SetClear() => SetAlpha(0f);
    public IEnumerator FadeIn(float duration) => Fade(1f, 0f, duration);  // black -> clear
    public IEnumerator FadeOut(float duration) => Fade(0f, 1f, duration); // clear -> black
    public void TeleportFade() => StartCoroutine(TeleportFadeRoutine());

    // --- Internals ---
    private IEnumerator Fade(float startA, float endA, float duration)
    {
        if (duration <= 0f) { SetAlpha(endA); yield break; }

        float t = 0f;
        while (t < duration)
        {
            SetAlpha(Mathf.Lerp(startA, endA, t / duration));
            t += Time.deltaTime;
            yield return null;
        }
        SetAlpha(endA);
    }

    private void SetAlpha(float a)
    {
        var c = _fadeColor;
        c.a = Mathf.Clamp01(a);
        _rend.material.SetColor(_baseColorID, c);
    }

    private IEnumerator TeleportFadeRoutine()
    {
        float half = _teleportFadeDuration * 0.5f;
        yield return FadeOut(half);
        yield return FadeIn(half);
        GameManager.Instance.UpdateGameState(GameManager.GameState.Explore);
    }
}
