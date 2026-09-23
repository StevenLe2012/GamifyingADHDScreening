using System.Collections;
using UnityEngine;

/// <summary>
/// Per-trial correct/incorrect feedback (green/red background + sound) for the REAL MOXO run,
/// Magic Academy only. Mirrors the old TrainingCPTRunner feedback setup.
/// </summary>
public class MagicAcademyFeedback : MonoBehaviour
{
    public static MagicAcademyFeedback Instance { get; private set; }

    [Tooltip("Green background for correct; keep disabled in scene.")]
    [SerializeField] private GameObject correctCanvas;
    [Tooltip("Red background for incorrect; keep disabled in scene.")]
    [SerializeField] private GameObject incorrectCanvas;

    [Tooltip("If null, the script will create a hidden 2D AudioSource at runtime.")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip correctClip;
    [SerializeField] private AudioClip incorrectClip;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    [Tooltip("How long to keep the correct/incorrect background visible before hiding it.")]
    [SerializeField] private float feedbackHoldSeconds = 0.5f;

    private Coroutine _hideCo;

    private void Awake()
    {
        Instance = this;
        EnsureAudioSource();
        HideAllFeedback();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void EnsureAudioSource()
    {
        if (audioSource)
        {
            audioSource.spatialBlend = 0f;
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            return;
        }

        var go = new GameObject("MagicAcademyFeedbackAudio");
        go.transform.SetParent(transform, false);
        audioSource = go.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = 1f;
    }

    public void ShowFeedback(bool correct)
    {
        if (_hideCo != null)
        {
            StopCoroutine(_hideCo);
            _hideCo = null;
        }

        if (correctCanvas) correctCanvas.SetActive(correct);
        if (incorrectCanvas) incorrectCanvas.SetActive(!correct);

        var clip = correct ? correctClip : incorrectClip;
        if (clip && audioSource) audioSource.PlayOneShot(clip, Mathf.Clamp01(sfxVolume));

        if (feedbackHoldSeconds > 0f)
            _hideCo = StartCoroutine(CoHideAfterDelay());
    }

    private IEnumerator CoHideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(feedbackHoldSeconds);
        HideAllFeedback();
        _hideCo = null;
    }

    public void HideAllFeedback()
    {
        if (correctCanvas) correctCanvas.SetActive(false);
        if (incorrectCanvas) incorrectCanvas.SetActive(false);
    }
}
