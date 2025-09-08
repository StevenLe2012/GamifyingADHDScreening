using UnityEngine;
using System.Collections;
using Dialogue;

public class WakeUpController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FadeScreen fadeScreen;              // drag your Fader (tagged "Fader")
    [SerializeField] private DialogueHandler introDialogue;      // drag PlayerIntroDialogue's DialogueHandler
    [SerializeField] private DialogueState sharedState;          // drag your shared DialogueState asset

    [Header("Blink Settings")]
    [SerializeField] private int blinkCount = 2;
    [SerializeField] private float blinkDuration = 0.2f;         // speed of eyelid open/close
    [SerializeField] private float pauseBetweenBlinks = 0.5f;    // pause while eyes are closed
    [SerializeField] private float finalFadeIn = 2f;             // final slow open

    // Prevents the wake-up from replaying if you reload the scene in same session
    private static bool hasPlayedThisSession = false;

    private IEnumerator Start()
    {
        Debug.Log("[WakeUp] Start()");
        Debug.Log($"[WakeUp] fadeScreen ref set? {fadeScreen != null}");
        if (fadeScreen == null)
        {
            var go = GameObject.FindGameObjectWithTag("Fader");
            Debug.Log($"[WakeUp] Fader tagged object found? {go != null}");
            if (go != null) fadeScreen = go.GetComponent<FadeScreen>();
        }
        Debug.Log($"[WakeUp] fadeScreen component? {fadeScreen != null}");

        Debug.Log($"Reached here. Has played before: {hasPlayedThisSession}");

        // Only run in the very first scene, only once
        // if (hasPlayedThisSession) { Destroy(gameObject); yield break; }
        // hasPlayedThisSession = true;

        Debug.Log("Reached here 1");

        if (fadeScreen == null)
        {
            var go = GameObject.FindGameObjectWithTag("Fader");
            if (go != null) fadeScreen = go.GetComponent<FadeScreen>();
        }
        if (fadeScreen == null)
        {
            Debug.LogWarning("WakeUpController: No FadeScreen assigned.");
            yield break;
        }

        Debug.Log("Reached here 2");

        // Start with eyes closed
        fadeScreen.SetBlack();
        yield return new WaitForSeconds(0.6f);

        // Blinks
        for (int i = 0; i < blinkCount; i++)
        {
            yield return fadeScreen.FadeIn(blinkDuration);   // open
            yield return new WaitForSeconds(pauseBetweenBlinks);
            yield return fadeScreen.FadeOut(blinkDuration);  // close
            yield return new WaitForSeconds(pauseBetweenBlinks);
        }

        // Final slow open
        yield return fadeScreen.FadeIn(finalFadeIn);



        // Start the player's intro dialogue
        if (introDialogue != null && sharedState != null)
        {
            Debug.Log("Starting Dialogue for ", sharedState);
            introDialogue.StartDialogueFor(sharedState);
        }

        // Done, no need to keep this alive
        Destroy(gameObject);


    }
}
