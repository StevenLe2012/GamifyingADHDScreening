using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;   // <- for Image progress bar

public class ExploreHintUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup group;              // root with CanvasGroup
    [SerializeField] private TextMeshProUGUI mainText;       // "Explore the cabin on your left"
    [SerializeField] private TextMeshProUGUI timerText;      // "59s"
    [SerializeField] private TextMeshProUGUI endText;        // "Return to the wizard..."
    
    [Header("Optional Progress Bar")]
    [SerializeField] private bool useProgressBar = true;     // toggle in Inspector
    [SerializeField] private Image progressFill;             // Image.type = Filled (Radial360 or Horizontal)
    [SerializeField] private bool invertFill = false;        // if your bar fills up instead of down

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.3f;      // fade in/out seconds
    [SerializeField] private float endMessageSeconds = 4f;   // how long to show end text

    

    private Coroutine countdownCo;

    /// <summary>
    /// Show countdown hint with fully inspector-controlled texts.
    /// </summary>
    public void ShowExploreHint(float durationSeconds, string startMessage, string endMessage)
    {
        if (countdownCo != null) StopCoroutine(countdownCo);
        countdownCo = StartCoroutine(CoShowHint(durationSeconds, startMessage, endMessage));
    }

    private IEnumerator CoShowHint(float seconds, string startMessage, string endMessage)
    {
        GetComponent<XRCountdownSnapToWorldAnchor>()?.SnapToAnchor();
        
        group.gameObject.SetActive(true);
        yield return StartCoroutine(CoFade(1f));

        float duration = Mathf.Max(0f, seconds);
        float t = duration;

        mainText.text  = startMessage;   // e.g., "Get ready…"
        endText.text   = "";             // ensure hidden
        timerText.text = Mathf.CeilToInt(t) + "s";

        if (useProgressBar && progressFill != null)
        {
            float startFill = invertFill ? 0f : 1f;
            progressFill.fillAmount = startFill;
        }

        while (t > 0f)
        {
            t -= Time.deltaTime;
            timerText.text = Mathf.Max(0, Mathf.CeilToInt(t)) + "s";

            if (useProgressBar && progressFill != null && duration > 0f)
            {
                float pct = Mathf.Clamp01(t / duration);        // 1 -> 0
                progressFill.fillAmount = invertFill ? (1f - pct) : pct;
            }
            yield return null;
        }

        // --- changed block ---
        // If endMessage is empty/null, just hide immediately.
        if (string.IsNullOrEmpty(endMessage))
        {
            timerText.text = "";
            mainText.text  = "";
            if (useProgressBar && progressFill != null)
                progressFill.fillAmount = invertFill ? 1f : 0f;

            yield return StartCoroutine(CoFade(0f));
            group.gameObject.SetActive(false);
            yield break;
        }
        // ---------------------

        // original end-message path
        timerText.text = "";
        mainText.text  = "";
        endText.text   = endMessage;

        if (useProgressBar && progressFill != null)
            progressFill.fillAmount = invertFill ? 1f : 0f;

        yield return new WaitForSeconds(endMessageSeconds);
        yield return StartCoroutine(CoFade(0f));
        group.gameObject.SetActive(false);
    }

    private IEnumerator CoFade(float targetAlpha)
    {
        float start = group.alpha;
        float time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            group.alpha = Mathf.Lerp(start, targetAlpha, time / fadeDuration);
            yield return null;
        }
        group.alpha = targetAlpha;
    }


    
}
