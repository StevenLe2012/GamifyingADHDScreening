using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string islandSceneName = "CardIsland_Solo";
    [SerializeField] private string spawnObjectName = "CardIslandSpawn";
    [SerializeField] private Transform playerRoot; // drag your Player (or XR Origin) here in Inspector

    private void Start()
    {
        StartCoroutine(LoadThenPlacePlayer());
    }

    private IEnumerator LoadThenPlacePlayer()
    {
        // Load island additively
        if (!SceneManager.GetSceneByName(islandSceneName).isLoaded)
        {
            var op = SceneManager.LoadSceneAsync(islandSceneName, LoadSceneMode.Additive);
            while (!op.isDone) yield return null;
        }

        // Wait 1 frame so objects are available
        yield return null;

        // Find spawn
        var spawn = GameObject.Find(spawnObjectName);
        if (spawn == null)
        {
            Debug.LogError($"Spawn '{spawnObjectName}' not found in loaded scenes.");
            yield break;
        }

        if (playerRoot == null)
        {
            Debug.LogError("playerRoot not assigned on SceneLoader.");
            yield break;
        }

        // Move player
        playerRoot.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);

        Debug.Log($"Placed player at '{spawnObjectName}' in '{islandSceneName}'.");
    }
}
