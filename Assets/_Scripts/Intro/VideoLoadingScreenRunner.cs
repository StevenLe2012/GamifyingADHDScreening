using System.Collections;
using UnityEngine;

/// <summary>
/// Survives BootIntro → Main scene switch so scene-load coroutines are not cut off.
/// </summary>
sealed class VideoLoadingScreenRunner : MonoBehaviour
{
    static VideoLoadingScreenRunner _instance;

    public static VideoLoadingScreenRunner Ensure()
    {
        if (_instance != null) return _instance;

        var go = new GameObject("[VideoLoadingScreenRunner]");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<VideoLoadingScreenRunner>();
        return _instance;
    }

    public void Run(IEnumerator routine)
    {
        StopAllCoroutines();
        StartCoroutine(routine);
    }
}
