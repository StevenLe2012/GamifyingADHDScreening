using UnityEngine;

public class UIInputFocusDriver : MonoBehaviour
{
    private static UIInputFocusDriver _instance;

    private void Awake()
    {
        if (_instance != null) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        UIInputFocus.Tick();
    }
}
