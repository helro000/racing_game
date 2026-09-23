#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RelayExpansionTest : MonoBehaviour
{
    bool done;
    int car,track,difficulty;bool trial;
    float started;
    readonly Dictionary<string,float> bests=new Dictionary<string,float>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() {
        if(!SessionState.GetBool("RelayExpansion.Test",false)) return;
        SessionState.SetBool("RelayExpansion.Test",false);
        var go=new GameObject("Expansion verification");DontDestroyOnLoad(go);go.AddComponent<RelayExpansionTest>();
    }
    void Awake() {
        car=PlayerPrefs.GetInt("pointer");track=RelaySettings.Track;difficulty=RelaySettings.Difficulty;trial=RelaySettings.TimeTrial;
        started=Time.realtimeSinceStartup;Application.logMessageReceived+=OnLog;
        File.WriteAllText("ExpansionReport.txt","ROAD RELAY EXPANSION CHECKS\n");
    }
    void OnLog(string message,string stack,LogType type) { if(type==LogType.Exception || type==LogType.Error) End(false,message); }
    void Update() { if(!done && Time.realtimeSinceStartup-started>420) End(false,"Test watchdog expired"); }
    void Check(bool ok,string message) { if(!ok) { End(false,message);throw new Exception(message); } File.AppendAllText("ExpansionReport.txt","PASS "+message+"\n"); }
    IEnumerator Start() {
        yield return null;yield return null;
        Check(FindFirstObjectByType<RelayGarage>()!=null,"Garage and race options load");
        for(int mode=0;mode<2;mode++) for(int map=0;map<4;map++) {
            RelaySettings.Track=map;RelaySettings.TimeTrial=mode==1;RelaySettings.Difficulty=map%3;
            PlayerPrefs.SetInt("pointer",(map+mode)%5);
            SceneManager.LoadScene(RelaySettings.Scenes[map]);yield return null;yield return null;
            var race=RelayRace.Instance;string label=RelaySettings.Tracks[map]+" / "+(mode==0?"RACE":"TIME TRIAL");
            Check(race!=null && race.Racers.Count==(mode==0?3:1),label+" has correct racer count");
            var minimap=FindFirstObjectByType<RelayMinimap>();Canvas.ForceUpdateCanvases();
            var mesh=minimap.canvasRenderer.GetMesh();
            Check(mesh!=null && mesh.vertexCount>=race.Route.Count*4,label+" renders minimap geometry");
            Check(!FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains("[B]") || t.text.Contains("WASD") || t.text.Contains("Esc  Pause")),label+" has no driving instructions");
            foreach(var node in race.Route) CheckSurface(node);
            File.AppendAllText("ExpansionReport.txt","PASS "+label+" continuous road surface\n");
            var player=race.Player;string key=RelaySettings.BestKey(player.GetComponent<controller>().carName);bests[key]=PlayerPrefs.GetFloat(key,-1);
            yield return new WaitForSeconds(3.2f);Check(race.Running,label+" countdown works");
            race.TogglePause();Check(race.State==RelayRace.Phase.Paused,label+" pauses");race.TogglePause();
            player.Autopilot=true;Time.timeScale=5;float reportAt=30;
            while(race.State!=RelayRace.Phase.Results && race.Elapsed<240) {
                if(race.State==RelayRace.Phase.Paused) race.TogglePause();
                if(race.Elapsed>reportAt) { File.AppendAllText("ExpansionReport.txt","DRIVE "+label+" t="+race.Elapsed.ToString("0")+" "+string.Join(" | ",race.Racers.Select(r=>r.name+" "+r.Next+"/"+race.Route.Count+" recoveries="+r.Recoveries))+"\n");reportAt+=30; }
                yield return null;
            }
            Check(race.State==RelayRace.Phase.Results,label+" finishes");
            Check(player.Recoveries<=1,label+" player stays on course");
            Check(race.Racers.Where(r=>r!=player).All(r=>r.Recoveries<=1 && r.Fraction>.7f),label+" rivals remain on course and competitive");
            Check(PlayerPrefs.GetFloat(key)>0,label+" saves separate best time");
            race.Restart();yield return null;yield return null;
            Check(SceneManager.GetActiveScene().name==RelaySettings.Scenes[map] && RelayRace.Instance.State==RelayRace.Phase.Countdown,label+" restarts correct track");
        }
        RelayRace.Instance.Garage();yield return null;yield return null;
        Check(FindFirstObjectByType<RelayGarage>()!=null,"Return to garage works");
        End(true,"All tracks, modes, five cars, three difficulties and clean HUD verified");
    }
    void CheckSurface(Transform node) {
        if(!Physics.RaycastAll(node.position+Vector3.up*3,Vector3.down,8).Any(h=>h.collider.name=="Track")) Check(false,"Missing road beneath "+node.name);
    }
    void End(bool success,string message) {
        if(done)return;done=true;Application.logMessageReceived-=OnLog;
        PlayerPrefs.SetInt("pointer",car);RelaySettings.Track=track;RelaySettings.Difficulty=difficulty;RelaySettings.TimeTrial=trial;
        foreach(var pair in bests) { if(pair.Value<0) PlayerPrefs.DeleteKey(pair.Key);else PlayerPrefs.SetFloat(pair.Key,pair.Value); }PlayerPrefs.Save();
        File.AppendAllText("ExpansionReport.txt",(success?"SUCCESS ":"FAIL ")+message+"\n");
        Time.timeScale=1;SessionState.SetInt("RelayExpansion.Result",success?1:-1);EditorApplication.isPlaying=false;
    }
}
#endif
