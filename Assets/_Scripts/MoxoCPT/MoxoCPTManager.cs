using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoxoCPT
{
    public class MoxoCPTManager : MonoBehaviour
    {
        public static MoxoCPTManager instance;

        [HideInInspector] public bool isGameOver = false;

        private Cards _cards;
        private LoggingReport _loggingReport;
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else Destroy(instance);

            _cards = GetComponent<Cards>();
            _loggingReport = GetComponent<LoggingReport>();
        }

        private void Update()
        {
            if (isGameOver)
            {
                isGameOver = false;  // only plays once
                
            }
        }

        private void GameOver()
        {
            _loggingReport.LogReport();
        }
    }
}


