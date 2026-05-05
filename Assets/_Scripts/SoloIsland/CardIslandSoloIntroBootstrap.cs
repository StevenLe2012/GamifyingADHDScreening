using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CardIslandSoloIntroBootstrap : MonoBehaviour
{
    [Header("Solo testing toggle")]
    [SerializeField] private bool enableSoloFlow = true;

    [Header("Island Scene")]
    [SerializeField] private string islandSceneName = "CardIsland_Solo";

    [Header("MOXO Intro Text")]
    [SerializeField] private string title = "MOXO CPT";
    [TextArea] [SerializeField] private string body =
        "Press Start (or A/Space) to begin.";

    [Header("Timing")]
    [SerializeField] private float delaySeconds = 0.25f;

    private IEnumerator Start()
    {
        if (!enableSoloFlow) yield break;

        // wait until the island scene is loaded additively
        while (!SceneManager.GetSceneByName(islandSceneName).isLoaded)
            yield return null;

        if (delaySeconds > 0f)
            yield return new WaitForSeconds(delaySeconds);

        // find IntroScreen and show MOXO intro
        var intro = FindObjectOfType<IntroScreen>(true);
        if (intro == null)
        {
            Debug.LogError("[SoloIntroBootstrap] IntroScreen not found.");
            yield break;
        }

        intro.ShowMoxo(title, body);
        Debug.Log("[SoloIntroBootstrap] Showing MOXO intro for Card Island solo.");
    }
}
