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

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else Destroy(gameObject);
        }

        /*
        public void StartGame()
        {
            LoggingReport.CreateReportCSV();
        }
        */
    }
}


