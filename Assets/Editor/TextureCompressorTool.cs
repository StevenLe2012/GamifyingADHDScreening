using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor tool to batch-compress all textures for WebGL.
/// Run via:  Tools → WebGL Texture Compressor
/// </summary>
public class TextureCompressorTool : EditorWindow
{
    // ── Settings exposed in the window ────────────────────────────────────
    private int     maxSize           = 1024;
    private int     crunchQuality     = 50;
    private bool    skipUITextures    = true;
    private bool    skipNormalMaps    = true;
    private bool    dryRun            = true;   // safe default: preview first
    private Vector2 scroll;

    private List<string> _preview = new List<string>();

    // ── Menu entry ─────────────────────────────────────────────────────────
    [MenuItem("Tools/WebGL Texture Compressor")]
    public static void ShowWindow()
    {
        var win = GetWindow<TextureCompressorTool>("WebGL Texture Compressor");
        win.minSize = new Vector2(480, 520);
    }

    // ── GUI ────────────────────────────────────────────────────────────────
    private void OnGUI()
    {
        GUILayout.Label("WebGL Texture Compression Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        maxSize       = EditorGUILayout.IntPopup("Max Texture Size",
                            maxSize,
                            new[] { "256", "512", "1024", "2048" },
                            new[] { 256, 512, 1024, 2048 });

        crunchQuality = EditorGUILayout.IntSlider("Crunch Quality (0=smallest, 100=best)", crunchQuality, 0, 100);
        skipUITextures = EditorGUILayout.Toggle("Skip UI textures (path contains /UI/)", skipUITextures);
        skipNormalMaps = EditorGUILayout.Toggle("Skip normal maps", skipNormalMaps);

        EditorGUILayout.Space(8);
        dryRun = EditorGUILayout.Toggle("Dry Run (preview only, no changes)", dryRun);

        EditorGUILayout.Space(8);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Preview affected textures", GUILayout.Height(30)))
                RunScan(applyChanges: false);

            GUI.backgroundColor = dryRun ? Color.white : new Color(1f, 0.6f, 0.3f);
            if (GUILayout.Button(dryRun ? "Preview (dry run ON)" : "Apply to ALL textures ⚠", GUILayout.Height(30)))
                RunScan(applyChanges: !dryRun);
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.Space(6);

        if (_preview.Count > 0)
        {
            GUILayout.Label($"Textures that will be changed: {_preview.Count}", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(300));
            foreach (var p in _preview)
                EditorGUILayout.LabelField(p, EditorStyles.miniLabel);
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "Recommended workflow:\n" +
            "1. Run 'Preview' first to review which textures will change.\n" +
            "2. Uncheck 'Dry Run' and click 'Apply' to commit.\n" +
            "3. Rebuild your WebGL project.\n\n" +
            "The .data file should drop from ~700 MB to under 150 MB.",
            MessageType.Info);
    }

    // ── Core logic ─────────────────────────────────────────────────────────
    private void RunScan(bool applyChanges)
    {
        _preview.Clear();

        var guids = AssetDatabase.FindAssets("t:Texture2D");
        int changed = 0;

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path     = AssetDatabase.GUIDToAssetPath(guids[i]);
                var    importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                // ── Skip filters ──────────────────────────────────────────
                if (skipNormalMaps && importer.textureType == TextureImporterType.NormalMap)
                    continue;

                if (skipUITextures && path.Replace('\\', '/').ToLowerInvariant().Contains("/ui/"))
                    continue;

                // ── Check current WebGL override ──────────────────────────
                var current = importer.GetPlatformTextureSettings("WebGL");
                bool alreadySet = current.overridden
                               && current.maxTextureSize   <= maxSize
                               && current.compressionQuality == crunchQuality
                               && (current.format == TextureImporterFormat.DXT5Crunched
                                || current.format == TextureImporterFormat.DXT1Crunched);

                if (alreadySet) continue;

                // ── Choose format based on alpha ───────────────────────────
                // Textures with alpha channel → DXT5 Crunched (RGBA)
                // Textures without           → DXT1 Crunched (RGB, half the size)
                bool hasAlpha = importer.DoesSourceTextureHaveAlpha();
                var fmt = hasAlpha
                    ? TextureImporterFormat.DXT5Crunched
                    : TextureImporterFormat.DXT1Crunched;

                _preview.Add($"{(hasAlpha ? "RGBA" : "RGB ")}  {path}");

                if (applyChanges)
                {
                    var settings = new TextureImporterPlatformSettings
                    {
                        name               = "WebGL",
                        overridden         = true,
                        maxTextureSize     = maxSize,
                        format             = fmt,
                        compressionQuality = crunchQuality
                    };

                    importer.SetPlatformTextureSettings(settings);
                    importer.SaveAndReimport();
                    changed++;

                    // Progress bar
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "Compressing textures…",
                            path,
                            (float)i / guids.Length))
                    {
                        Debug.LogWarning("[TextureCompressor] Cancelled by user.");
                        break;
                    }
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (applyChanges)
            Debug.Log($"[TextureCompressor] Done — applied WebGL compression to {changed} textures.");
        else
            Debug.Log($"[TextureCompressor] Preview — {_preview.Count} textures would be changed.");

        Repaint();
    }
}
