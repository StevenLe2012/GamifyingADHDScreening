using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Batch WebGL build + exported size report.
/// Unity menu: Build → WebGL (with size report)
/// CLI: Unity -batchmode -executeMethod WebGLBuildWithReport.BuildWebGL ...
/// </summary>
public static class WebGLBuildWithReport
{
    const string OutputRelative = "../Umaki_WebGL";
    const string ReportRelative = "../Umaki_WebGL/build-size-report.txt";

    [MenuItem("Build/WebGL (with size report)")]
    public static void BuildWebGLMenu() => BuildWebGL();

    public static void BuildWebGL()
    {
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;
        var outputPath = Path.GetFullPath(Path.Combine(projectRoot, OutputRelative));
        var reportPath = Path.GetFullPath(Path.Combine(projectRoot, ReportRelative));

        Directory.CreateDirectory(outputPath);

        // Force Unity to regenerate loader.js (stale loader + new wasm causes WebGL "null function" crashes).
        var buildDir = Path.Combine(outputPath, "Build");
        if (Directory.Exists(buildDir))
        {
            foreach (var loader in Directory.GetFiles(buildDir, "*.loader.js"))
            {
                File.Delete(loader);
                Debug.Log($"[WebGLBuild] Removed stale loader: {loader}");
            }
        }

        // Keep gzip — do not switch to Brotli here.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.analyzeBuildSize = true;

        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("[WebGLBuild] No scenes enabled in Build Settings.");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log($"[WebGLBuild] Scenes: {string.Join(", ", scenes)}");
        Debug.Log($"[WebGLBuild] Output: {outputPath}");

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        WriteReport(report, reportPath);

        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"[WebGLBuild] FAILED: {report.summary.result}");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log($"[WebGLBuild] SUCCEEDED. Report: {reportPath}");
        EditorApplication.Exit(0);
    }

    static void WriteReport(BuildReport report, string reportPath)
    {
        var sb = new StringBuilder(256 * 1024);
        var summary = report.summary;

        sb.AppendLine("=== WebGL Build Size Report ===");
        sb.AppendLine($"Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Result: {summary.result}");
        sb.AppendLine($"Total size: {BytesToMb(summary.totalSize):F2} MB ({summary.totalSize:N0} bytes)");
        sb.AppendLine($"Build time: {summary.totalTime}");
        sb.AppendLine($"Output path: {summary.outputPath}");
        sb.AppendLine();

        sb.AppendLine("=== Build output files (largest first) ===");
        foreach (var file in report.GetFiles().OrderByDescending(f => f.size))
            sb.AppendLine($"{BytesToMb(file.size),8:F2} MB  [{file.role}]  {file.path}");

        sb.AppendLine();
        sb.AppendLine("=== Packed source assets (largest first, top 150) ===");

        var assetRows = new List<(ulong size, string path, string type)>();
        if (report.packedAssets != null)
        {
            foreach (var pack in report.packedAssets)
            {
                if (pack.contents == null) continue;
                foreach (var asset in pack.contents)
                {
                    var path = asset.sourceAssetPath;
                    if (string.IsNullOrEmpty(path))
                    {
                        path = AssetDatabase.GUIDToAssetPath(asset.sourceAssetGUID.ToString());
                        if (string.IsNullOrEmpty(path))
                            path = "Internal";
                    }

                    var typeName = asset.type != null ? asset.type.Name : "Unknown";
                    assetRows.Add((asset.packedSize, path, typeName));
                }
            }
        }

        foreach (var row in assetRows.OrderByDescending(r => r.size).Take(150))
            sb.AppendLine($"{BytesToMb(row.size),8:F3} MB  [{row.type}]  {row.path}");

        sb.AppendLine();
        sb.AppendLine($"Total packed asset entries: {assetRows.Count}");

        File.WriteAllText(reportPath, sb.ToString());
        Debug.Log($"[WebGLBuild] Wrote report ({assetRows.Count} assets) → {reportPath}");
    }

    static double BytesToMb(ulong bytes) => bytes / (1024.0 * 1024.0);
}
