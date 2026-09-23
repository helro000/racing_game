using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PlayModeLauncher
{
    public static void OpenGarageAndPlay()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/awakeScene.unity");
        EditorApplication.delayCall += () =>
        {
            Debug.Log("Opening Racing Game in Play mode.");
            EditorApplication.isPlaying = true;
        };
    }
}
