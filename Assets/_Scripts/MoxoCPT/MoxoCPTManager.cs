// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// namespace MoxoCPT
// {
//     public class MoxoCPTManager : MonoBehaviour
//     {
//         public static MoxoCPTManager Instance;

//         [HideInInspector] public bool isGameOver;

//         [Header("Optional Systems")]
//         [SerializeField] private DistractorSystem distractors;  // drag your DistractorSystem here
//         // Hami:Eyetracking - start gaze logging
//         [SerializeField] private VarjoEyeLogger eyeLogger;
//         //End


//         private void Awake()
//         {
//             if (Instance == null)
//             {
//                 Instance = this;
//                 DontDestroyOnLoad(gameObject);
//             }
//             else Destroy(gameObject);
//         }

//         public void OnGameBegin()
//         {
            
//             isGameOver = false;
//             TurnCardsOn();
//             LoggingReport.CreateReportCSV();
//             GameManager.Instance.UpdateGameState(GameManager.GameState.CPT);

//             // Hami:Eyetracking - start gaze logging
//             EyeTrackLogger.I?.BeginSession(GameManager.Instance?.ParticipantId);

//         }
        
//         public void OnGameEnd()
//         {
//             if (distractors != null) distractors.StopSystem();
//             // Hami:Eyetracking - end logging
//             EyeTrackLogger.I?.EndSession();
//             GameManager.Instance.UpdateGameState(GameManager.GameState.Explore);
//         }

//         private void TurnCardsOn()
//         {
//             CardsActive tmp = gameObject.GetComponent(typeof(CardsActive)) as CardsActive;
//             tmp?.SetCardsActive(true);
//         }
//     }
// }


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoxoCPT
{
    public class MoxoCPTManager : MonoBehaviour
    {
        public static MoxoCPTManager Instance;

        [HideInInspector] public bool isGameOver;

        [Header("Optional Systems")]
        [SerializeField] private DistractorSystem distractors;  // drag your DistractorSystem here
        // Hami:Eyetracking - start gaze logging
        [SerializeField] private EyeTrackLogger eyeLogger;      // ← use EyeTrackLogger (not VarjoEyeLogger)
        //End

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else Destroy(gameObject);
        }

        public void OnGameBegin()
        {
            Debug.Log("[MoxoCPTManager] OnGameBegin() called.");
            Debug.Log($"[MoxoCPTManager] GameManager.Instance = {(GameManager.Instance ? "OK" : "NULL")}");
            
            isGameOver = false;

            var cards = GetComponent<CardsActive>();
            Debug.Log($"[MoxoCPTManager] CardsActive on same object = {(cards ? "OK" : "NULL")}");

            TurnCardsOn();
            LoggingReport.CreateReportCSV();
            if (GameManager.Instance != null)
                GameManager.Instance.UpdateGameState(GameManager.GameState.CPT);
            else
                Debug.LogError("[MoxoCPTManager] Cannot set state: GameManager.Instance is null.");

            // Hami:Eyetracking - start gaze logging with ParticipantId from GameManager
            var pid = GameManager.Instance != null ? GameManager.Instance.ParticipantId : "";
            if (string.IsNullOrWhiteSpace(pid))
            {
                Debug.LogWarning("[MoxoCPTManager] ParticipantId is empty; using timestamp fallback.");
                pid = "P_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            }

            // If you didn’t drag it, try to find it
            if (eyeLogger == null) eyeLogger = EyeTrackLogger.I ?? FindObjectOfType<EyeTrackLogger>(true);
            eyeLogger?.BeginSession(pid);
        }

        public void OnGameEnd()
        {
            if (distractors != null) distractors.StopSystem();

            // Hami:Eyetracking - end logging
            (eyeLogger ?? EyeTrackLogger.I)?.EndSession();

            GameManager.Instance.UpdateGameState(GameManager.GameState.Explore);
        }

        private void TurnCardsOn()
        {
            var tmp = GetComponent<CardsActive>();
            tmp?.SetCardsActive(true);
        }
    }
}
