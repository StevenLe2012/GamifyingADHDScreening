using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IslandTravelManager : MonoBehaviour
{
    public static IslandTravelManager I { get; private set; }
    void Awake()
    {
        I = this;
        if (!fader) fader = GameObject.FindGameObjectWithTag("Fader")?.GetComponent<FadeScreen>();

        // Build lookup from IslandAnchor components found in the scene
        _anchorById.Clear();
        foreach (var a in FindObjectsOfType<IslandAnchor>(true))
            if (!string.IsNullOrWhiteSpace(a.islandId))
                _anchorById[a.islandId.Trim().ToUpperInvariant()] = a.transform;
    }

    [SerializeField] private FadeScreen fader;
    [SerializeField] private Transform playerRoot; // XR Origin or Desktop player root

    private readonly Dictionary<string, Transform> _anchorById = new();
    public IslandData CurrentIsland { get; private set; }

    public void TravelTo(IslandData island)
    {
        CurrentIsland = island;
        StartCoroutine(CoTravel(island));
    }

    IEnumerator CoTravel(IslandData island)
    {
        if (!_anchorById.TryGetValue(island.islandId.Trim().ToUpperInvariant(), out var dest))
        {
            Debug.LogError($"[IslandTravel] No anchor found for id '{island.islandId}'.");
            yield break;
        }

        if (fader) fader.TeleportFade();
        yield return new WaitForSeconds(0.2f);

        var cc = playerRoot.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;

        playerRoot.position = dest.position;
        playerRoot.rotation = dest.rotation;

        if (cc) cc.enabled = true;

        GameManager.Instance.UpdateGameState(GameManager.GameState.PrepareCPT);

        if (island.startsMoxoOnArrival)
        {
            var intro = FindObjectOfType<IntroScreen>(true);
            if (intro) intro.ShowMoxo();
            else MoxoCPT.MoxoCPTManager.Instance?.OnGameBegin();
        }
    }
}
