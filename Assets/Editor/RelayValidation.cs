using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor.Build.Reporting;
using System.IO;

public static class RelayValidation
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/awakeScene.unity");
        EditorApplication.isPlaying = true;
    }
    public static void BuildPlayer()
    {
        PlayerSettings.productName = "Road Relay";
        PlayerSettings.companyName = "Road Relay";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.runInBackground = true;
        Directory.CreateDirectory("Builds/RoadRelay");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = RelaySettings.BuildScenes,
            locationPathName = "Builds/RoadRelay/RoadRelay.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        if(report.summary.result != BuildResult.Succeeded) throw new System.Exception("Player build failed: " + report.summary.result);
        Debug.Log("ROAD_RELAY_PLAYER_BUILD_PASSED " + report.summary.totalSize);
    }
}
