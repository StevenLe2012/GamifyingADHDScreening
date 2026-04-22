#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace _Scripts.CustomTools
{
    /// <summary>
    /// Bulk-applies WebGL-optimised texture import settings to every texture in the project.
    /// Run once via  CustomTools → Optimize Textures for WebGL.
    /// </summary>
    public static class WebGLTextureOptimizer
    {
        // ── Tuneable constants ────────────────────────────────────────────────────
        private const int    MAX_SIZE_DEFAULT    = 1024;  // safe quality/size balance
        private const int    MAX_SIZE_SMALL      = 512;   // applied when source ≤ 512 px
        private const int    MAX_SIZE_LARGE      = 2048;  // kept for lightmaps / render targets
        private const string WEBGL_PLATFORM      = "WebGL";

        // Folders whose textures should be kept at higher quality (relative to Assets/).
        private static readonly string[] HighQualityFolders = {
            "Assets/Resources/UI",
        };

        // Folders to skip entirely (e.g. third-party read-only packages).
        private static readonly string[] SkipFolders = {
            "Assets/Packages",
            "Assets/TextMesh Pro",
        };
        // ─────────────────────────────────────────────────────────────────────────

        [MenuItem("CustomTools/Optimize Textures for WebGL")]
        public static void OptimizeAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });

            int total    = guids.Length;
            int changed  = 0;
            int skipped  = 0;

            try
            {
                for (int i = 0; i < total; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                    // Progress bar — cancel supported.
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "Optimizing textures for WebGL",
                            path,
                            (float)i / total))
                    {
                        Debug.LogWarning("[WebGLTextureOptimizer] Cancelled by user.");
                        break;
                    }

                    if (ShouldSkip(path)) { skipped++; continue; }

                    if (ProcessTexture(path)) changed++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[WebGLTextureOptimizer] Done. " +
                      $"Modified: {changed} | Skipped: {skipped} | Total: {total}");

            EditorUtility.DisplayDialog(
                "WebGL Texture Optimisation Complete",
                $"Processed {total} textures.\n" +
                $"  Modified : {changed}\n" +
                $"  Skipped  : {skipped}\n\n" +
                "Rebuild the WebGL player to see the size reduction.",
                "OK");
        }

        // ── Per-texture logic ─────────────────────────────────────────────────────

        private static bool ProcessTexture(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return false;

            // Determine target format based on texture type.
            TextureImporterFormat fmt = ChooseFormat(importer);
            int maxSize = ChooseMaxSize(importer, path);

            // Read existing WebGL override (or create one).
            importer.GetPlatformTextureSettings(WEBGL_PLATFORM, out int curMax, out TextureImporterFormat curFmt);
            bool overrideExists = importer.GetPlatformTextureSettings(WEBGL_PLATFORM) is { overridden: true };

            var settings = importer.GetPlatformTextureSettings(WEBGL_PLATFORM);
            bool needsChange = !settings.overridden
                            || settings.maxTextureSize > maxSize
                            || settings.format != fmt;

            if (!needsChange) return false;

            settings.overridden      = true;
            settings.maxTextureSize  = maxSize;
            settings.format          = fmt;
            settings.compressionQuality = (int)TextureCompressionQuality.Normal;

            importer.SetPlatformTextureSettings(settings);
            importer.SaveAndReimport();
            return true;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static TextureImporterFormat ChooseFormat(TextureImporter importer)
        {
            switch (importer.textureType)
            {
                case TextureImporterType.NormalMap:
                    return TextureImporterFormat.ASTC_6x6;

                case TextureImporterType.Lightmap:
                    return TextureImporterFormat.ASTC_4x4;   // higher quality for baked light

                case TextureImporterType.SingleChannel:
                    return TextureImporterFormat.ASTC_6x6;

                default:
                    // Use ASTC 6×6 (good compression, wide WebGL support).
                    // For textures that have NO alpha channel DXT1 would be slightly smaller,
                    // but ASTC is universally better on modern browsers.
                    return TextureImporterFormat.ASTC_6x6;
            }
        }

        private static int ChooseMaxSize(TextureImporter importer, string path)
        {
            // Keep lightmaps and HDR envmaps at higher res.
            if (importer.textureType == TextureImporterType.Lightmap)
                return MAX_SIZE_LARGE;

            // High-quality folders stay at 1024 regardless of source size.
            foreach (string folder in HighQualityFolders)
                if (path.StartsWith(folder)) return MAX_SIZE_DEFAULT;

            // If the actual source texture is small, no point upscaling the cap.
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null && tex.width <= MAX_SIZE_SMALL && tex.height <= MAX_SIZE_SMALL)
                return MAX_SIZE_SMALL;

            return MAX_SIZE_DEFAULT;
        }

        private static bool ShouldSkip(string path)
        {
            foreach (string skip in SkipFolders)
                if (path.StartsWith(skip)) return true;
            return false;
        }
    }
}
#endif
