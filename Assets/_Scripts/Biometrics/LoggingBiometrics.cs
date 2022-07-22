using UnityEngine;
using System.IO;
using System;
using Assets.Scripts.Biometrics;
using System.Collections;
using System.Collections.Generic;

public class LoggingBiometrics : MonoBehaviour
{
    private const string CSVSeperator = ",";
    private static int framesPassed;
    private static float startingTime;
    private static BiometricInfo  biometricData;
    private static int _buttonPress;

    
    private void Awake()
    {
        startingTime = Time.time;
        biometricData = new BiometricInfo();
        _buttonPress = 0;
    }

    void Start()
    {
        // Creates to the CSV file with only heading
        CreateBiometricCSV();
        // Updates and Appends the Biometric data to teh CSV
        StartCoroutine(UpdateAndAppendBiometrics());
    }

    void Update()
    {
        // updates the button press counter for each button they press on controller.
        if (Input.anyKeyDown)
        {
            _buttonPress++;
        }
    }

    private static string[] CSVHeaders = new string[6] {
        "PlayerPos",
        "HeadsetRot",
        "EyeMov",
        "ControllerMov",
        "ControllerRot",
        "ButtonPress"
    };
    public static void CreateBiometricCSV()
    {
        using (StreamWriter sw = File.CreateText(GetCSVPath()))
        {
            string finalString = "";
            for (int i = 0; i < CSVHeaders.Length; i++)
            {
                if (finalString != "")
                {
                    finalString += CSVSeperator;
                }
                finalString += CSVHeaders[i];
            }
            finalString += CSVSeperator + "TimePassed";
            sw.WriteLine(finalString);
        }
    }

    public static void AppendToBiometricCSV(BiometricInfo Biometrics)
    {
        using (StreamWriter sw = File.AppendText(GetCSVPath()))
        {
            // Casts all Biometrics to string
            string playerPos = Biometrics.PlayerPos.ToString();
            playerPos = playerPos.Replace(",", "");

            string headsetRot = Biometrics.HeadsetRot.ToString();
            headsetRot = headsetRot.Replace(",", "");

            string eyeMov = Biometrics.EyeMov.ToString();
            eyeMov = eyeMov.Replace(",", "");

            string controllerMov = Biometrics.ControllerMov.ToString();
            controllerMov = controllerMov.Replace(",", "");

            string controllerRot = Biometrics.ControllerRot.ToString();
            controllerRot = controllerRot.Replace(",", "");

            string buttonPress = Biometrics.ButtonPress.ToString();

            // adds biometric data to the finalString
            string finalString = "";
            finalString += playerPos + CSVSeperator;
            finalString += headsetRot + CSVSeperator;
            finalString += eyeMov + CSVSeperator;
            finalString += controllerMov + CSVSeperator;
            finalString += controllerRot + CSVSeperator;
            finalString += buttonPress + CSVSeperator;

            var secondsPassed = Time.time - startingTime;
            finalString += secondsPassed.ToString();

            // appends the biometric to the CSV
            sw.WriteLine(finalString);
        }
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

    private static string GetCSVPath()
    {
        return Path.Combine(Environment.CurrentDirectory, "Assets", "ParticipantBiometricData", $"P__Biometrics.csv");
        //return Path.Combine(Environment.CurrentDirectory, "Assets", "ParticipantBiometricData", $"Biometrics-{DateTime.Now.ToFileTime()}.csv");
    }

    IEnumerator UpdateAndAppendBiometrics()
    {
        while (true)  //TODO: TURN OFF WHEN GAME IS FINISHED
        {
            UpdateBiometricData();
            AppendToBiometricCSV(biometricData);
            yield return new WaitForSeconds(0.04f);
        }
    }

}