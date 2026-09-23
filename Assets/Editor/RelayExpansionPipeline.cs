using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class RelayExpansionPipeline
{
    static bool busy;
    static RelayExpansionPipeline() { EditorApplication.update+=Update; }
    static void Update()
    {
        if(busy || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if(File.Exists("Library/RelayTracks.request") && !EditorApplication.isPlayingOrWillChangePlaymode) {
            File.Delete("Library/RelayTracks.request");busy=true;
            try {
                EditorSceneManager.SaveOpenScenes();EasyRoadsMapBuilder.BuildAll();
                foreach(string scenePath in RelaySettings.BuildScenes) {
                    if(scenePath.EndsWith("awakeScene.unity")) continue;
                    EditorSceneManager.OpenScene(scenePath);
                    var arch=GameObject.Find("Finish arch");
                    var route=Object.FindFirstObjectByType<trackWaypoints>();route.RefreshNodes();
                    if(Mathf.Abs(arch.transform.position.y-route.nodes[route.nodes.Count-1].position.y)>.8f) throw new System.Exception("Finish arch is not grounded: "+scenePath);
                }
                RelayValidation.BuildPlayer();File.AppendAllText("ExpansionReport.txt","PASS Finish arches grounded on all tracks\nBUILD SUCCESS\n");
            } catch(System.Exception e) { File.AppendAllText("ExpansionReport.txt","BUILD FAIL "+e);Debug.LogException(e); }
            finally { EditorSceneManager.OpenScene("Assets/Scenes/awakeScene.unity");busy=false; }
        }
        if(File.Exists("Library/RelayExpansion.request")) {
            if(EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.isPlaying=false; return; }
            File.Delete("Library/RelayExpansion.request");
            busy=true;
            try {
                EditorSceneManager.SaveOpenScenes();
                EasyRoadsMapBuilder.BuildAll();
                SessionState.SetBool("RelayExpansion.Test",true);
                EditorApplication.isPlaying=true;
            } catch(System.Exception e) { File.WriteAllText("ExpansionReport.txt","FAIL "+e); Debug.LogException(e); }
            finally { busy=false; }
        }
        if(SessionState.GetInt("RelayExpansion.Result",0)!=0 && !EditorApplication.isPlayingOrWillChangePlaymode) {
            int result=SessionState.GetInt("RelayExpansion.Result",0);SessionState.SetInt("RelayExpansion.Result",0);
            if(result!=1) return;
            busy=true;
            try { RelayValidation.BuildPlayer();File.AppendAllText("ExpansionReport.txt","BUILD SUCCESS\n"); }
            catch(System.Exception e) { File.AppendAllText("ExpansionReport.txt","BUILD FAIL "+e);Debug.LogException(e); }
            finally { EditorSceneManager.OpenScene("Assets/Scenes/awakeScene.unity");busy=false; }
        }
    }
}
