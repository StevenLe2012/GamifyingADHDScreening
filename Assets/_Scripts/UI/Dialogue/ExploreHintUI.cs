using UnityEngine;
using TMPro;

public class ExploreHintUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private float fadeInSpeed = 10f;
    [SerializeField] private float visibleSeconds = 3f;
    [SerializeField] private float fadeOutSpeed = 6f;

    private Coroutine _co;

    public void ShowExploreHint()    => Show("Explore for about 2 minutes.");
    public void ShowReturnToNPCHint()=> Show("Return to the NPC and press A");

    public void Show(string message)
    {
        if (label) label.text = message;
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoShow());
    }

    private System.Collections.IEnumerator CoShow()
    {
        gameObject.SetActive(true);
        while (canvasGroup.alpha < 1f)
        { canvasGroup.alpha += Time.unscaledDeltaTime * fadeInSpeed; yield return null; }
        canvasGroup.alpha = 1f;

        float t = visibleSeconds;
        while (t > 0f) { t -= Time.unscaledDeltaTime; yield return null; }

        while (canvasGroup.alpha > 0f)
        { canvasGroup.alpha -= Time.unscaledDeltaTime * fadeOutSpeed; yield return null; }
        canvasGroup.alpha = 0f;

        gameObject.SetActive(false);
        _co = null;
    }
}
