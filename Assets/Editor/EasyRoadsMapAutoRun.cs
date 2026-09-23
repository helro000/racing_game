using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class EasyRoadsMapAutoRun
{
    private const string MarkerPath = "Assets/Editor/.build-easyroads-map";

    static EasyRoadsMapAutoRun()
    {
        EditorApplication.delayCall += RunIfRequested;
    }

    private static void RunIfRequested()
    {
        if (!File.Exists(MarkerPath))
        {
            return;
        }

        File.Delete(MarkerPath);
        AssetDatabase.Refresh();

        try
        {
            EasyRoadsMapBuilder.Build();
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}
