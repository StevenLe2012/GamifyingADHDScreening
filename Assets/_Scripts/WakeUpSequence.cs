using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class WakeUpSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScreenFader fader;   // assign the Fader (Panel with ScreenFader)

    [Header("Blink Settings")]
    [SerializeField] private int blinkCount = 3;
    [SerializeField] private float blinkDuration = 0.25f; // fade out/in speed
    [SerializeField] private float pauseBetween = 0.2f;   // pause between blinks
    [SerializeField] private float finalOpenDuration = 1.0f; // slower final open
    [SerializeField] private float endDelay = 0.3f;       // small delay before firing event

    [Header("Flow")]
    [SerializeField] private bool autoRunOnStart = true;
    [SerializeField] private UnityEvent onFinished; // hook your intro start here if you want

    private void Start()
    {
        if (!fader)
        {
            // Try auto-find by name
            var go = GameObject.Find("Fader");
            if (go) fader = go.GetComponent<ScreenFader>();
        }

        if (!fader)
        {
            Debug.LogWarning("[WakeUpSequence] No ScreenFader assigned/found.");
            return;
        }

        if (autoRunOnStart) StartCoroutine(Run());
    }

    public void RunNow() => StartCoroutine(Run());

    private IEnumerator Run()
    {
        // Start with eyes closed (black)
        fader.SetBlack();

        // Optional tiny settle
        yield return new WaitForSeconds(0.3f);

        // Blinks
        for (int i = 0; i < blinkCount; i++)
        {
            // open (black -> clear)
            yield return fader.FadeIn(blinkDuration);
            yield return new WaitForSeconds(pauseBetween);

            // close (clear -> black), except skip if it's the last before final open
            yield return fader.FadeOut(blinkDuration);
            yield return new WaitForSeconds(pauseBetween);
        }

        // Final open (longer, to “wake up”)
        yield return fader.FadeIn(finalOpenDuration);

        // Small delay, then fire event (start dialogue, etc.)
        yield return new WaitForSeconds(endDelay);
        onFinished?.Invoke();
    }
}
