using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ReconstructionSetup
{
    [MenuItem("Racing/Reconstruct Settings")]
    public static void Run()
    {
        PlayerSettings.companyName = "Reconstructed";
        PlayerSettings.productName = "Racing Game";
        var paths = new[] { "Assets/Scenes/awakeScene.unity", "Assets/Scenes/superMarioMap.unity", "Assets/Scenes/ComunityMap.unity", "Assets/Scenes/EasyRoadsMap.unity" };
        EditorBuildSettings.scenes = paths.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        var report = new System.Text.StringBuilder();
        foreach (var path in paths)
        {
            var scene = EditorSceneManager.OpenScene(path);
            int missing = 0;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            report.AppendLine(path + ": missing scripts = " + missing);
        }
        EditorSceneManager.OpenScene(paths[0]);
        AssetDatabase.SaveAssets();
        File.WriteAllText("ReconstructionReport.txt", report.ToString());
        Debug.Log("RECONSTRUCTION_SETUP_COMPLETE\n" + report);
    }
}
