using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CsvHelper;
using System.IO;
using System.Globalization;
using System;
public class LoggingBiometrics : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}


namespace CsvWriter
{
    class Logging
    {
        static void Main(string[] args)
        {
            string user = "0";  // TODO: Change to make it fit global number
            var csvPath = Path.Combine(Environment.CurrentDirectory, $"P{user}_BiometricData.csv");
        }
    }
}