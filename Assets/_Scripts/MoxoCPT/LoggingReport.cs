using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MoxoCPT
{
    public static class LoggingReport
    {
        private const string CSVSeperator = ",";
        
        public static void AppendToReportCSV(Report report)
        {
            using (StreamWriter sw = File.AppendText(GetCSVPath()))
            {
                // Casts all Biometrics to 
                var isTarget = report.IsTarget ? "Target" : "Distractor";

                var timeShown = report.TimeShown.ToString();
                
                var attentiveness = report.Attentiveness.ToString();

                var timeliness =  report.Timeliness.ToString();

                var hyperReactiveness = report.HyperReactiveness.ToString();

                var impulsiveness = report.Impulsiveness.ToString();

                var reactionTime = report.ReactionTime.ToString();
                
                var hyperReactiveCount = report.HyperReactiveCount.ToString();

                // adds biometric data to the finalString
                var finalString = "";
                finalString += isTarget + CSVSeperator;
                finalString += timeShown + CSVSeperator;
                finalString += attentiveness + CSVSeperator;
                finalString += timeliness + CSVSeperator;
                finalString += hyperReactiveness + CSVSeperator;
                finalString += impulsiveness + CSVSeperator;
                finalString += reactionTime + CSVSeperator;
                finalString += hyperReactiveCount;

                // appends the biometric to the CSV
                sw.WriteLine(finalString);
            }
        }

        private static string[] CSVHeaders = new string[8]
        {
            "Type",
            "Time Shown",
            "Attentiveness",
            "Timelineess",
            "HyperReactiveness",
            "Impulsiveness",
            "ReactionTime",
            "HyperReactiveCount"
        };

        public static void CreateReportCSV()
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
        
        private static string GetCSVPath()
        {
            return Path.Combine(Environment.CurrentDirectory, "Assets", "Resources", "ParticipantData", "ReportData", $"P__MoxoCPT.csv");
            //return Path.Combine(Environment.CurrentDirectory, "Assets", "Resources", "ParticipantData", "ReportData", $"MoxoCPT-{DateTime.Now.ToFileTime()}.csv");
        }
    }
}
