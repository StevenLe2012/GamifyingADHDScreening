using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CardIslandSoloAutoStart : MonoBehaviour
{
    [Header("Solo Testing Toggle")]
    [SerializeField] private bool enableSoloIntro = true;

    [Header("Island Scene")]
    [SerializeField] private string islandSceneName = "CardIsland_Solo";

    [Header("Intro Text")]
    [SerializeField] private string title = "MOXO CPT";
    [TextArea] [SerializeField] private string body = "Press Start (or A/Space) to begin.";

    [Header("Optional: delay for XR to settle")]
    [SerializeField] private float delaySeconds = 0.25f;

    private bool shown;

    private IEnumerator Start()
    {
        if (!enableSoloIntro) yield break;

        // Wait until island scene is loaded
        while (!SceneManager.GetSceneByName(islandSceneName).isLoaded)
            yield return null;

        if (delaySeconds > 0f)
            yield return new WaitForSeconds(delaySeconds);

        // Find IntroScreen (even if inactive)
        var intro = FindObjectOfType<IntroScreen>(true);
        if (intro == null)
        {
            Debug.LogError("[CardIslandSoloAutoStart] IntroScreen not found. Is your UI/IntroScreen in Core or the island scene?");
            yield break;
        }

        if (shown) yield break;
        shown = true;

        // Show only the MOXO intro. Player starts when Start/A/Space is pressed.
        intro.ShowMoxo(title, body);
        Debug.Log("[CardIslandSoloAutoStart] Showing MOXO intro for solo.");
    }
}

