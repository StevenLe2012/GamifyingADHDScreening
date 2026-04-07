using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using System.IO;
#endif

namespace Biometrics
{
    public class LoggingBiometrics : MonoBehaviour
    {
        [Header("Left Hand")]
        [SerializeField] private InputActionReference controllerLeftTrigger;
        [SerializeField] private InputActionReference controllerLeftGrip;
        
        [Header("Right Hand")]
        [SerializeField] private InputActionReference controllerRightTrigger;
        [SerializeField] private InputActionReference controllerRightGrip;
        
        
        private const string CSVSeperator = ",";
        private static int framesPassed;
        private static float startingTime;
        private static BiometricInfo biometricData;
        private static int _buttonPress;


        private void Awake()
        {
            startingTime = Time.time;
            biometricData = new BiometricInfo();
            _buttonPress = 0;
        }

        void Start()
        {
            print("creating biometrics");
#if UNITY_EDITOR
            // CSV biometric logging only works in the Unity Editor (requires filesystem access).
            CreateBiometricCSV();
            StartCoroutine(UpdateAndAppendBiometrics());
#endif
        }

        void Update()
        {
            // updates the button press counter for each button they press on controller.
            if (controllerLeftTrigger.action.triggered)
            {
                _buttonPress++;
            }
        }

        private static string[] CSVHeaders = new string[6]
        {
            "PlayerPos",
            "HeadsetRot",
            "EyeMov",
            "ControllerMov",
            "ControllerRot",
            "ButtonPress"
        };

        public static void CreateBiometricCSV()
        {
#if UNITY_EDITOR
            using (StreamWriter sw = File.CreateText(GetCSVPath()))
            {
                string finalString = "";
                for (int i = 0; i < CSVHeaders.Length; i++)
                {
                    if (finalString != "")
                        finalString += CSVSeperator;
                    finalString += CSVHeaders[i];
                }
                finalString += CSVSeperator + "TimePassed";
                sw.WriteLine(finalString);
            }
#endif
        }

        public static void AppendToBiometricCSV(BiometricInfo Biometrics)
        {
#if UNITY_EDITOR
            using (StreamWriter sw = File.AppendText(GetCSVPath()))
            {
                string playerPos     = Biometrics.PlayerPos.ToString().Replace(",", "");
                string headsetRot    = Biometrics.HeadsetRot.ToString().Replace(",", "");
                string eyeMov        = Biometrics.EyeMov.ToString().Replace(",", "");
                string controllerMov = Biometrics.ControllerMov.ToString().Replace(",", "");
                string controllerRot = Biometrics.ControllerRot.ToString().Replace(",", "");
                string buttonPress   = Biometrics.ButtonPress.ToString();

                string finalString = playerPos     + CSVSeperator
                                   + headsetRot    + CSVSeperator
                                   + eyeMov        + CSVSeperator
                                   + controllerMov + CSVSeperator
                                   + controllerRot + CSVSeperator
                                   + buttonPress   + CSVSeperator
                                   + (Time.time - startingTime).ToString();
                sw.WriteLine(finalString);
            }
#endif
        }

        public BiometricInfo UpdateBiometricData()
        {
            biometricData.ButtonPress = _buttonPress;
            biometricData.ControllerMov = transform.position;
            biometricData.ControllerRot = transform.rotation;
            biometricData.HeadsetRot = transform.rotation;
            biometricData.PlayerPos = transform.position;
            biometricData.EyeMov = transform.position;
            return biometricData;
        }

#if UNITY_EDITOR
        private static string GetCSVPath()
        {
            return System.IO.Path.Combine(Environment.CurrentDirectory, "Assets", "Resources",
                "ParticipantData", "BiometricData", "P__Biometrics.csv");
        }
#endif

        IEnumerator UpdateAndAppendBiometrics()
        {
#if UNITY_EDITOR
            while (true)
            {
                UpdateBiometricData();
                AppendToBiometricCSV(biometricData);
                yield return new WaitForSeconds(0.04f);
            }
#else
            yield break;
#endif
        }
    }
}

