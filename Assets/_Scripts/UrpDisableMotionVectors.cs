#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Reflection;

public static class UrpDisableMotionVectors
{
    [MenuItem("Tools/URP/Disable Camera Motion Vectors (force)")]
    public static void ForceDisable()
    {
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp == null) { Debug.LogWarning("No active URP asset."); return; }

        // 1) Try the public API (URP 14/15+)
        var prop = urp.GetType().GetProperty("supportsCameraMotionVectors",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(urp, false);
            EditorUtility.SetDirty(urp);
            AssetDatabase.SaveAssets();
            Debug.Log("Disabled motion vectors via public property.");
            return;
        }

        // 2) Fall back to serialized private field (older URP)
        var so = new SerializedObject(urp);
        var sp = so.FindProperty("m_SupportsCameraMotionVectors"); // internal name
        if (sp != null)
        {
            sp.boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(urp);
            AssetDatabase.SaveAssets();
            Debug.Log("Disabled motion vectors by writing m_SupportsCameraMotionVectors.");
            return;
        }

        // 3) Last resort: reflection set private field
        var field = urp.GetType().GetField("m_SupportsCameraMotionVectors",
                    BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(urp, false);
            EditorUtility.SetDirty(urp);
            AssetDatabase.SaveAssets();
            Debug.Log("Disabled motion vectors via reflection.");
            return;
        }

        Debug.LogWarning("Your URP build doesn’t seem to have camera motion vectors at all. " +
                         "If the MotionVectorsPersistentData error persists, upgrade URP.");
    }
}
#endif
