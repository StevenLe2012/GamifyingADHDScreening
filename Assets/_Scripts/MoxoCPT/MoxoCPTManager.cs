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
            isGameOver = false;
            TurnCardsOn();
            LoggingReport.CreateReportCSV();
            GameManager.Instance.UpdateGameState(GameManager.GameState.CPT);
        }
        
        public void OnGameEnd()
        {
            GameManager.Instance.UpdateGameState(GameManager.GameState.Explore);
        }

        private void TurnCardsOn()
        {
            CardsActive tmp = gameObject.GetComponent(typeof(CardsActive)) as CardsActive;
            tmp?.SetCardsActive(true);
        }

        public IEnumerator PrepareCPT(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            OnGameBegin();
        }
        
    }
}


