using System;
using System.Collections;
using System.Collections.Generic;
using Companion;
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
        PrepareCPT,
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
        UpdateGameState(GameState.Explore);
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
            case GameState.PrepareCPT:
                HandlePrepareCPT();
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
    private void HandlePrepareCPT()
    {
        Debug.Log("GM: PrepareCPT");
        
        var companion = GameObject.FindGameObjectWithTag("Companion");
        if (companion == null)
        {
            Debug.Log("Could not find a Companion with tag: 'Companion'");
            return;
        }

        var companionFollowScript = companion.GetComponent<NPCFollow>();
        companionFollowScript.enabled = false;
                
        var companionDestinationScript = companion.GetComponent<GoToDestination>();
        companionDestinationScript.enabled = true;

        var companionAnimator = companion.GetComponent<Animator>();
        companionAnimator.enabled = true;


    }
    private void HandleCPT()
    {
        Debug.Log("GM: CPT"); 
    }
}


