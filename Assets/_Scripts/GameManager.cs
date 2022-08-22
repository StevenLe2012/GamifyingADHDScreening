using System;
using System.Collections;
using System.Collections.Generic;
using MoxoCPT;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [HideInInspector] public GameState State;
    public static event Action<GameState> OnGameStateChanged;
    
    public enum GameState {
        Narrative,
        Explore,
        CPT,
        
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    private void Start()
    {
        UpdateGameState(GameState.CPT);
    }

    public void UpdateGameState(GameState newState)
    {
        if (newState == State) return;
        
        State = newState;
        
        switch (newState)
        {
            case GameState.Narrative:
                HandleNarrative();
                break;
            case GameState.Explore:
                HandleExplore();
                break;
            case GameState.CPT:
                HandleCPT();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }

        OnGameStateChanged?.Invoke(newState);


    }

    private void HandleNarrative()
    {
        Debug.Log("GM: Narrative"); 
    }

    private void HandleExplore()
    {
       Debug.Log("GM: Explore"); 
    }
    private void HandleCPT()
    {
        Debug.Log("GM: CPT"); 
    }
}


