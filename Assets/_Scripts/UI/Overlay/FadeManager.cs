using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance;
    [SerializeField] private GameObject _faderScreen;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }
    
    public void DeactivateFaderScreen()
    {
        _faderScreen.SetActive(false);
    }

    public void ActivateFaderScreen()
    {
        _faderScreen.SetActive(true);
    }
}
