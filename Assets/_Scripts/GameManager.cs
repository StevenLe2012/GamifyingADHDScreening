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
       Teleport,
      
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
           case GameState.Teleport:
               HandleTeleport();
               break;

           default:
               throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
       }

       OnGameStateChanged?.Invoke(newState);


   }

   private void HandleNarrative()
   {
       print("GM: Narrative");
   }

   private void HandleExplore()
   {
      print("GM: Explore");
   }
   private void HandlePrepareCPT()
   {
       print("GM: PrepareCPT");
      
       var companion = GameObject.FindGameObjectWithTag("Companion");
      
       if (companion == null)
       {
           print("Could not find a Companion with tag: 'Companion'");
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
       print("GM: CPT");
   }
  
   private void HandleTeleport()
   {
       print("GM: Teleport");
      
       var fadeScreen = GameObject.FindGameObjectWithTag("Fader");
      
       if (fadeScreen == null)
       {
           print("Could not find a FadeScreen with tag: 'Fader'");
           return;
       }

       var fader = fadeScreen.GetComponent<FadeScreen>();
       fader.TeleportFade();
   }
  
}
