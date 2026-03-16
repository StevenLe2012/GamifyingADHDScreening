using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExploreHintUI : MonoBehaviour
{
    public static ExploreHintUI I { get; private set; }

    [Header("UI References")]
    [SerializeField] private CanvasGroup group;          // root with CanvasGroup
    [SerializeField] private TextMeshProUGUI mainText;   // "Get ready…"
    [SerializeField] private TextMeshProUGUI timerText;  // "59s"
    [SerializeField] private TextMeshProUGUI endText;    // "Go!"

    [Header("Optional Progress Bar")]
    [SerializeField] private bool useProgressBar = true; 
    [SerializeField] private Image progressFill;         
    [SerializeField] private bool invertFill = false;    

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.3f;     
    [SerializeField] private float endMessageSeconds = 4f;  

    private Coroutine countdownCo;
    private Coroutine _dialogueHintCo;

    private void Awake()
    {
        if (I == null) I = this;
    }

    private void OnDestroy()
    {
        if (I == this) I = null;
    }

    /// <summary>
    /// Show a static hint (no countdown) that stays visible until HideDialogueOptionsHint is called.
    /// Intended to appear when dialogue presents multiple options.
    /// </summary>
    public void ShowDialogueOptionsHint(string message)
    {
        if (_dialogueHintCo != null) { StopCoroutine(_dialogueHintCo); _dialogueHintCo = null; }

        // Suspend any active countdown so they don't fight over the same UI.
        if (countdownCo != null) { StopCoroutine(countdownCo); countdownCo = null; }

        // Activate BEFORE StartCoroutine — Unity throws if the GameObject is inactive.
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (group == null) group = GetComponent<CanvasGroup>();
        if (group != null && !group.gameObject.activeSelf) group.gameObject.SetActive(true);

        _dialogueHintCo = StartCoroutine(CoShowDialogueHint(message));
    }

    /// <summary>Fade out and deactivate the dialogue options hint.</summary>
    public void HideDialogueOptionsHint()
    {
        if (_dialogueHintCo != null) { StopCoroutine(_dialogueHintCo); _dialogueHintCo = null; }

        if (group != null && group.gameObject.activeSelf && group.alpha > 0f && isActiveAndEnabled)
            StartCoroutine(CoFadeToInactive());
        else if (group != null)
            group.gameObject.SetActive(false);
    }

    private IEnumerator CoShowDialogueHint(string message)
    {
        // Activation is guaranteed by ShowDialogueOptionsHint before StartCoroutine.
        SetInteractable(true);

        if (mainText)  mainText.text  = message ?? "";
        if (timerText) timerText.text = "";
        if (endText)   endText.text   = "";

        if (useProgressBar && progressFill)
            progressFill.fillAmount = 0f;

        yield return StartCoroutine(CoFadeUnscaled(1f));

        // Hold until explicitly hidden — HideDialogueOptionsHint will stop this coroutine.
        _dialogueHintCo = null;
    }

    /// <summary>Show countdown hint.</summary>
    public void ShowExploreHint(float durationSeconds, string startMessage, string endMessage)
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (!isActiveAndEnabled) enabled = true;

        if (group == null) group = GetComponent<CanvasGroup>();
        if (group != null && !group.gameObject.activeSelf) group.gameObject.SetActive(true);

        GetComponent<XRCountdownSnapToWorldAnchor>()?.SnapToAnchor();

        if (countdownCo != null) StopCoroutine(countdownCo);
        countdownCo = StartCoroutine(CoShowHint(durationSeconds, startMessage, endMessage));
    }

    /// <summary>Hide immediately (no end message).</summary>
    public void Hide()
    {
        if (countdownCo != null)
        {
            StopCoroutine(countdownCo);
            countdownCo = null;
        }

        ResetTextsAndBar();
        SetInteractable(false);

        if (group) group.alpha = 0f;
        if (group) group.gameObject.SetActive(false);
    }

    /// <summary>Cancel current countdown and fade out.</summary>
    public void Cancel()
    {
        if (countdownCo != null)
        {
            StopCoroutine(countdownCo);
            countdownCo = null;
        }
        StartCoroutine(CoFadeToInactive());
    }

    private IEnumerator CoShowHint(float seconds, string startMessage, string endMessage)
    {
        if (group) group.gameObject.SetActive(true);
        SetInteractable(true);

        yield return StartCoroutine(CoFadeUnscaled(1f));

        float duration = Mathf.Max(0f, seconds);
        float t = duration;

        if (mainText) mainText.text = startMessage ?? "";
        if (endText) endText.text = "";
        if (timerText) timerText.text = Mathf.CeilToInt(t) + "s";

        if (useProgressBar && progressFill)
            progressFill.fillAmount = invertFill ? 0f : 1f;

        while (t > 0f)
        {
            t -= Time.unscaledDeltaTime;

            if (timerText)
                timerText.text = Mathf.Max(0, Mathf.CeilToInt(t)) + "s";

            if (useProgressBar && progressFill && duration > 0f)
            {
                float pct = Mathf.Clamp01(t / duration);
                progressFill.fillAmount = invertFill ? (1f - pct) : pct;
            }

            yield return null;
        }

        if (string.IsNullOrEmpty(endMessage))
        {
            ResetTextsAndBar();
            yield return StartCoroutine(CoFadeUnscaled(0f));

            if (group) group.gameObject.SetActive(false);
            countdownCo = null;
            yield break;
        }

        if (timerText) timerText.text = "";
        if (mainText) mainText.text = "";
        if (endText) endText.text = endMessage;

        if (useProgressBar && progressFill)
            progressFill.fillAmount = invertFill ? 1f : 0f;

        float e = endMessageSeconds;
        while (e > 0f)
        {
            e -= Time.unscaledDeltaTime;
            yield return null;
        }

        yield return StartCoroutine(CoFadeUnscaled(0f));

        if (group) group.gameObject.SetActive(false);
        countdownCo = null;
    }

    private IEnumerator CoFadeToInactive()
    {
        SetInteractable(false);
        yield return StartCoroutine(CoFadeUnscaled(0f));
        if (group) group.gameObject.SetActive(false);
    }

    private IEnumerator CoFadeUnscaled(float targetAlpha)
    {
        float start = group ? group.alpha : 0f;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            if (group) group.alpha = Mathf.Lerp(start, targetAlpha, t / fadeDuration);
            yield return null;
        }

        if (group) group.alpha = targetAlpha;
    }

    private void SetInteractable(bool on)
    {
        if (!group) return;
        group.blocksRaycasts = on;
        group.interactable = on;
    }

    private void ResetTextsAndBar()
    {
        if (mainText) mainText.text = "";
        if (timerText) timerText.text = "";
        if (endText) endText.text = "";

        if (useProgressBar && progressFill)
            progressFill.fillAmount = invertFill ? 1f : 0f;
    }
}
