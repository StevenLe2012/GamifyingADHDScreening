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
        group.gameObject.SetActive(true);
        yield return StartCoroutine(CoFade(1f));

        float duration = Mathf.Max(0f, seconds);
        float t = duration;

        // initial text
       
        mainText.text = startMessage;
        endText.text  = "";
        timerText.text = Mathf.CeilToInt(t) + "s";

        // init progress
        if (useProgressBar && progressFill != null)
        {
            float startFill = invertFill ? 0f : 1f;
            progressFill.fillAmount = startFill;
        }

        while (t > 0f)
        {
            t -= Time.deltaTime;
            // countdown text
            timerText.text = Mathf.Max(0, Mathf.CeilToInt(t)) + "s";

            // progress update
            if (useProgressBar && progressFill != null && duration > 0f)
            {
                float pct = Mathf.Clamp01(t / duration);        // 1 -> 0 as time passes
                progressFill.fillAmount = invertFill ? (1f - pct) : pct;
            }

            yield return null;
        }

        // time's up
        timerText.text = "";
        mainText.text  = "";
        endText.text   = endMessage;

        // snap progress to end state
        if (useProgressBar && progressFill != null)
        {
            progressFill.fillAmount = invertFill ? 1f : 0f;
        }

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
        Debug.Log("here");
    }
}
