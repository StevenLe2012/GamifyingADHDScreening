using System;
using System.Collections;
using System.Collections.Generic;
using MoxoCPT;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameState State;

    public static event Action<GameState> OnGameStateChanged;

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
        UpdateGameState(GameState.Narrative);
    }

    public void UpdateGameState(GameState newState)
    {
        State = newState;
        
        switch (newState)
        {
            case GameState.Narrative:
                HandleNarrative();
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
        
    }
    private void HandleCPT()
    {
        if (Cards.Instance.numSeen == Cards.Instance.numCards)
        {
            UpdateGameState(GameState.Narrative);
        }
    }
}

public enum GameState {
    Narrative,
    CPT
}
