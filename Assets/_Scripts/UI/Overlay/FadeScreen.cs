using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeScreen : MonoBehaviour
{
    [SerializeField] private bool _fadeOnStart = true;
    [SerializeField] private float  _fadeDuration = 2;
    [SerializeField] private Color _fadeColor;

    private Renderer _rend;
    private readonly int _baseColorID = Shader.PropertyToID("_BaseColor");
    
    private void Awake()
    {
        _rend = GetComponent<Renderer>();
    }

    private void Start()
    {
        if (_fadeOnStart) FadeIn();
    }

    private void Fade(float startAlpha, float endAlpha)
    {
        StartCoroutine(FadeRoutine(startAlpha, endAlpha));
    }
    
    public void FadeIn() => Fade(1, 0);

    public void FadeOut() => Fade(0, 1);

    private IEnumerator FadeRoutine(float startAlpha, float endAlpha)
    {
        float timer = 0f;
        
        while (timer <= _fadeDuration)
        {
            var amount = timer / _fadeDuration;
            ChangeFade(startAlpha, endAlpha, amount);
            
            timer += Time.deltaTime;
            yield return null;
        }

        var amount2 = timer / _fadeDuration;
        ChangeFade(startAlpha, endAlpha, amount2);
    }

    private void ChangeFade(float startAlpha, float endAlpha, float amount)
    {
        var newColor = _fadeColor;
        newColor.a = Mathf.Lerp(startAlpha, endAlpha, amount);
        _rend.material.SetColor(_baseColorID, newColor);
    }
}
