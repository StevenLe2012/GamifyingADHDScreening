using UnityEngine;
using System.IO;
using System;
using Assets.Scripts.Biometrics;

public class LoggingBiometrics : MonoBehaviour
{
    private const string CSVSeperator = ",";
    private static int framesPassed;
    private static float startingTime;
    private static BiometricInfo biometricData;
    private static int buttonPress;
    
    private void Awake()
    {
        startingTime = Time.time; 
        buttonPress = 0;
    }

    // Start is called before the first frame update
    void Start()
    {
        // Creates to the CSV file with only heading
        CreateBiometricCSV();
    }

    // Update is called once per frame
    void Update()
    {
        // TODO: Rather than every 10 frames, make it so that it is consistent with time passed.
        if (framesPassed % 10 == 0)
        {
            UpdateBiometricData();
            AppendToBiometricCSV(biometricData);
        }

        //TODO: If player hits any buttons, buttonPress++;

        framesPassed += 1;
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
        Debug.Log("Created!");
        using (StreamWriter sw = File.CreateText(getCSVPath()))
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
        using (StreamWriter sw = File.AppendText(getCSVPath()))
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

            string ButtonPress = Biometrics.ButtonPress.ToString();

            // adds biometric data to the finalString
            string finalString = "";
            finalString += playerPos + CSVSeperator;
            finalString += headsetRot + CSVSeperator;
            finalString += eyeMov + CSVSeperator;
            finalString += controllerMov + CSVSeperator;
            finalString += controllerRot + CSVSeperator;
            finalString += ButtonPress + CSVSeperator;

            var secondsPassed = Time.time - startingTime;
            finalString += secondsPassed.ToString();

            // appends the biometric to the CSV
            sw.WriteLine(finalString);
        }
    }

    private BiometricInfo UpdateBiometricData()
    {
        biometricData.ButtonPress = buttonPress;
        biometricData.ControllerMov = transform.position;
        biometricData.ControllerRot = transform.rotation;
        biometricData.HeadsetRot = transform.rotation;
        biometricData.PlayerPos = transform.position;
        biometricData.EyeMov = transform.position;
        return biometricData;
    }

    private static string getCSVPath()
    {
        return Path.Combine(Environment.CurrentDirectory, "Assets", "ParticipantBiometricData", $"P__Biometrics.csv");
        //return Path.Combine(Environment.CurrentDirectory, "Assets", "ParticipantBiometricData", $"Biometrics-{DateTime.Now.ToFileTime()}.csv");
    }
}