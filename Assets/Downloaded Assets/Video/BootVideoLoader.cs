using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class BootVideoLoader : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "Core";
    [SerializeField] private bool allowSkipWithSpace = true;

    private VideoPlayer vp;

    private void Awake()
    {
        vp = GetComponent<VideoPlayer>();
        vp.loopPointReached += OnVideoFinished;
    }

    private void Start()
    {
        vp.Play();
    }

    private void Update()
    {
        if (!allowSkipWithSpace) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            LoadNext();
        }
    }

    private void OnVideoFinished(VideoPlayer _)
    {
        LoadNext();
    }

    private void LoadNext()
    {
        vp.loopPointReached -= OnVideoFinished;
        SceneManager.LoadScene(nextSceneName);
    }
}
