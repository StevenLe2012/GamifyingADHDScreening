using UnityEngine;
using UnityEngine.InputSystem;
using MoxoCPT;

public class CardIslandSoloStartCPT : MonoBehaviour
{
    [Header("Enable for solo testing only")]
    [SerializeField] private bool enableSoloStart = true;

    [Header("Keys")]
    [SerializeField] private bool startWithSpace = true;

    [Header("Optional: hide intro UI")]
    [SerializeField] private bool hideIntroOnStart = true;

    private bool started;

    private void Update()
    {
        if (!enableSoloStart || started) return;

        bool pressedSpace =
            startWithSpace &&
            Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame;

        if (pressedSpace)
            Begin();
    }

    public void Begin()
    {
        if (started) return;
        started = true;

        if (hideIntroOnStart)
        {
            var intro = FindObjectOfType<IntroScreen>(true);
            if (intro != null) intro.Hide();
        }

        // Prefer the singleton, fall back to search
        var moxo = MoxoCPTManager.Instance != null
            ? MoxoCPTManager.Instance
            : FindObjectOfType<MoxoCPTManager>(true);

        if (moxo == null)
        {
            Debug.LogError("[SoloStartCPT] MoxoCPTManager not found.");
            return;
        }

        Debug.Log($"[SoloStartCPT] Calling OnGameBegin on: {moxo.gameObject.name}");
        moxo.OnGameBegin();
    }
}
