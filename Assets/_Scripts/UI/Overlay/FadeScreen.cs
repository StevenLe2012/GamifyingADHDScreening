using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeScreen : MonoBehaviour
{
    [SerializeField] private bool _fadeOnStart = true;
    [SerializeField] private Color _fadeColor;
    [SerializeField] private float  _initialFadeDuration = 2f;
    [SerializeField] private float _teleportFadeDuration = 0.25f;
  

    private Renderer _rend;
    private readonly int _baseColorID = Shader.PropertyToID("_BaseColor");
    private float _fadeDuration;
  
    private void Awake()
    {
        _rend = GetComponent<Renderer>();
    }

    private void Start()
    {
        _fadeDuration = _initialFadeDuration;
        if (_fadeOnStart) FadeIn();
    }

    private void Fade(float startAlpha, float endAlpha)
    {
        StartCoroutine(FadeRoutine(startAlpha, endAlpha));
    }

    public void TeleportFade()
    {
        _fadeDuration = _teleportFadeDuration / 2;
        StartCoroutine(TeleportFadeRoutine());
    }

    public void FadeIn() => Fade(1, 0);

    public void FadeOut() => Fade(0, 1);

    private IEnumerator FadeRoutine(float startAlpha, float endAlpha)
    {
        var timer = 0f;
      
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

    private IEnumerator TeleportFadeRoutine()
    {
        FadeOut();
        yield return new WaitForSeconds(_teleportFadeDuration / 2);
        FadeIn();
    }
}