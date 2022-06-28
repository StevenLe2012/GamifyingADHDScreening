using UnityEngine;
using TMPro;

public class FPSTracker : MonoBehaviour
{
    [SerializeField] private float fpsUpdateInterval = 0.5f;
    [SerializeField] private float warningFPS = 70.0f;
    [SerializeField] private float criticalFPS = 60.0f;

    private TextMeshProUGUI _fpsUI;
    private float _timeLeft;
    private float _accumulatedTime;
    private int _frames;

    private void Awake()
    {
        _fpsUI = GetComponentInChildren<TextMeshProUGUI>();
        _timeLeft = fpsUpdateInterval;
        _accumulatedTime = 0.0f;
        _frames = 0;
    }

    private void Update()
    {
        UpdateFPS();
    }

    private void SetText(string text)
    {
        _fpsUI.SetText(text);
    }

    private void SetColor(Color color)
    {
        _fpsUI.color = color;
    }

    private void UpdateFPS()
    {
        _timeLeft -= Time.deltaTime;
        _accumulatedTime += Time.deltaTime;
        _frames++;

        if (_timeLeft < 0f) return;

        // Interval ended - update GUI text and start new interval
        float fps = _frames/_accumulatedTime;
        string fpsText = $"{fps:F1} FPS";
        SetText(fpsText);

        if (fps < criticalFPS)
        {
            SetColor(Color.red);
        }

        else if (fps < warningFPS)
        {
            SetColor(Color.yellow);
        }

        else
        {
            SetColor(Color.green);
        }

        // resets all variables
        _timeLeft = fpsUpdateInterval;
        _accumulatedTime = 0.0f;
        _frames = 0;
    }

}
