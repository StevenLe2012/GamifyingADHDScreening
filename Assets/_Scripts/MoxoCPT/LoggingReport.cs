using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MoxoCPT;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MoxoCPT
{
    public class LoggingReport : MonoBehaviour
    {
        [SerializeField] private Interact _interact;
        
        private const string CSVSeperator = ",";

        public void LogReport()
        {
            CreateReportCSV();
            AppendToReportCSV();
        }


        private string[] CSVHeaders = new string[6]
        {
            "Attentiveness",
            "Timelineess",
            "HyperReactiveness",
            "Impulsiveness",
            "ReactionTime",
            "HyperReactiveCount"
        };

        private void CreateReportCSV()
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
                sw.WriteLine(finalString);
            }
        }
        
        private void AppendToReportCSV()
        {
            using (StreamWriter sw = File.AppendText(GetCSVPath()))
            {
                string hyperReactiveCount = default;
                for (var i = 0; i < 59; i++)
                {
                    // Casts all Biometrics to string
                    var attentiveness = _interact._report.Attentiveness[i].ToString();

                    var timeliness =  _interact._report.Timelineess[i].ToString();

                    var hyperReactiveness = _interact._report.HyperReactiveness[i].ToString();

                    var impulsiveness = _interact._report.Impulsiveness.ToString();

                    var reactionTime = _interact._report.ReactionTime.ToString();

                    if (i == 0)
                    {
                        hyperReactiveCount = _interact._report.HyperReactiveCount.ToString();
                    }

                    // adds biometric data to the finalString
                    var finalString = "";
                    finalString += attentiveness + CSVSeperator;
                    finalString += timeliness + CSVSeperator;
                    finalString += hyperReactiveness + CSVSeperator;
                    finalString += impulsiveness + CSVSeperator;
                    finalString += reactionTime + CSVSeperator;
                    if (i == 0) finalString += hyperReactiveCount;

                    // appends the biometric to the CSV
                    sw.WriteLine(finalString);
                }
            }
        }
        
        private static string GetCSVPath()
        {
            return Path.Combine(Environment.CurrentDirectory, "Assets", "ParticipantData", "ReportData", $"P__MoxoCPT.csv");
            //return Path.Combine(Environment.CurrentDirectory, "Assets", "ParticipantData", "ReportData", $"MoxoCPT-{DateTime.Now.ToFileTime()}.csv");
        }
    }
}
