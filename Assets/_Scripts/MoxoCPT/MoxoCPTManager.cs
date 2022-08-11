using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoxoCPT
{
    public class MoxoCPTManager : MonoBehaviour
    {
        public static MoxoCPTManager Instance;

        [HideInInspector] public bool isGameOver = false;
        
        private LoggingReport _loggingReport;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                //DontDestroyOnLoad(gameObject);
            }
            else Destroy(Instance);
            
            _loggingReport = GetComponent<LoggingReport>();
        }

        private void Update()
        {
            if (isGameOver)
            {
                isGameOver = false;  // only plays once
                GameOver();
            }
        }

        private void GameOver()
        {
        }
    }
}


