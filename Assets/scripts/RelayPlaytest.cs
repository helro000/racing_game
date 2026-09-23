#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Opt-in integration test: real scenes, vehicle physics and UI callbacks, never included in the player.
public class RelayPlaytest : MonoBehaviour
{
    private int originalCar;
    private float started;
    private bool done;
    private int exitCode;
    private readonly System.Collections.Generic.Dictionary<string,float> bestTimes = new System.Collections.Generic.Dictionary<string,float>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if(!Environment.GetCommandLineArgs().Contains("-relay-test")) return;
        var go=new GameObject("Relay integration test");
        DontDestroyOnLoad(go);
        go.AddComponent<RelayPlaytest>();
    }
    void Awake()
    {
        started=Time.realtimeSinceStartup;
        originalCar=PlayerPrefs.GetInt("pointer");
        File.WriteAllText("RelayPlaytestReport.txt", "ROAD RELAY PLAYMODE INTEGRATION\n");
        Application.logMessageReceived += OnLog;
        Application.runInBackground=true;
    }
    void OnLog(string message,string stack,LogType type)
    {
        if(type==LogType.Exception || type==LogType.Error) End(false,message+"\n"+stack);
    }
    void Update() { if(done) { UnityEditor.EditorApplication.Exit(exitCode); return; } if(Time.realtimeSinceStartup-started>700) End(false,"Wall-clock watchdog expired"); }
    void Check(bool condition,string message)
    {
        if(!condition) { End(false,message); throw new Exception(message); }
        File.AppendAllText("RelayPlaytestReport.txt","PASS "+message+"\n");
    }
    IEnumerator Start()
    {
        yield return null;
        yield return null;
        var garage=FindFirstObjectByType<awakeManager>();
        Check(garage!=null && FindFirstObjectByType<RelayGarage>()!=null,"Garage opens");
        int count=garage.listOfVehicles.vehicles.Length;
        foreach(var prefab in garage.listOfVehicles.vehicles) { var key="RoadRelay.Best."+prefab.GetComponent<controller>().carName; bestTimes[key]=PlayerPrefs.GetFloat(key,-1); }
        for(int i=0;i<count;i++)
        {
            while(garage.vehiclePointer>i) FindFirstObjectByType<RelayGarage>().Select(-1);
            while(garage.vehiclePointer<i) FindFirstObjectByType<RelayGarage>().Select(1);
            yield return null;
            yield return null;
            Check(GameObject.FindGameObjectWithTag("Player")!=null,"Free car preview "+i);
        }
        for(int car=0;car<count;car++)
        {
            PlayerPrefs.SetInt("pointer",car);
            SceneManager.LoadScene("EasyRoadsMap");
            yield return null;
            yield return null;
            var race=RelayRace.Instance;
            Check(race!=null && race.Racers.Count==3,"Three racers for car "+car);
            Check(race.Route.Count>30,"Actual road route loaded");
            var player=race.Player;
            foreach(var opponent in race.Racers.Where(r=>r!=player)) {
                Check(Vector3.Dot(opponent.transform.position-player.transform.position,player.transform.forward)>5,"Rival starts visibly ahead: "+opponent.DisplayName);
                Check(opponent.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),"Rival has visible body: "+opponent.DisplayName);
            }
            Check(!player.GetComponent<RoadBuilderPower>().TryBuild(),"Build blocked during countdown");
            yield return new WaitForSeconds(3.2f);
            Check(race.Running,"Countdown releases race");
            var initialPosition=player.Body.position;
            var initialRotation=player.Body.rotation;
            var controls=player.GetComponent<inputManager>();
            player.enabled=false;
            controls.vertical=1;
            yield return new WaitForSeconds(2.5f);
            Check(player.Body.linearVelocity.magnitude>4,"Car "+car+" accelerates from keyboard-equivalent throttle");
            controls.horizontal=.4f;
            yield return new WaitForSeconds(.6f);
            Check(Quaternion.Angle(initialRotation,player.Body.rotation)>2,"Car "+car+" responds to steering");
            controls.horizontal=0;controls.vertical=0;controls.handbrake=true;
            yield return new WaitForSeconds(1.5f);
            Check(player.Body.linearVelocity.magnitude<1.5f,"Car "+car+" brakes to a stop (speed="+player.Body.linearVelocity.magnitude.ToString("0.00")+" m/s)");
            controls.handbrake=false;controls.vertical=-1;
            yield return new WaitForSeconds(1.5f);
            Check(Vector3.Dot(player.Body.linearVelocity,player.transform.forward)<-.5f,"Car "+car+" reverses from standstill");
            controls.vertical=0;
            player.Body.position=initialPosition;player.Body.rotation=initialRotation;
            player.Body.linearVelocity=Vector3.zero;player.Body.angularVelocity=Vector3.zero;
            player.enabled=true;
            if(car==0)
            {
                race.TogglePause();
                float elapsed=race.Elapsed;
                yield return new WaitForSecondsRealtime(.2f);
                Check(race.State==RelayRace.Phase.Paused && race.Elapsed==elapsed,"Pause freezes race time");
                Check(!player.GetComponent<RoadBuilderPower>().TryBuild(),"Build blocked while paused");
                FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name=="RESUME").onClick.Invoke();
                Check(race.Running && Time.timeScale==1,"Resume button works");
                player.Body.position=race.Route.Last().position+Vector3.up;
                yield return new WaitForFixedUpdate();
                Check(!player.Finished && player.Next<5,"Finish cannot be reached by skipping the road");
                player.Recover();
                yield return new WaitForSeconds(.5f);
                Check(player.Recoveries==1,"Recovery returns player to route");
                var power=player.GetComponent<RoadBuilderPower>();
                Check(power.TryBuild(),"Player builds a road-aligned boost strip");
                var pad=FindFirstObjectByType<RoadBoostPad>();
                Check(pad!=null && power.Charges==1,"Build consumes one charge");
                player.Body.rotation=pad.transform.rotation;
                Check(pad.TryBoost(player.Body),"Boost accepts player car");
                Check(!pad.TryBoost(player.Body),"Multiple colliders cannot duplicate the boost");
                var rival=race.Racers.First(r=>r!=player);
                rival.Body.rotation=pad.transform.rotation;
                Check(pad.TryBoost(rival.Body),"Rival can use player strip");
                yield return new WaitForSeconds(8.1f);
                Check(power.Charges==2,"Boost charges regenerate");
                yield return new WaitForSeconds(4.1f);
                Check(pad==null,"Boost strip expires");
                Time.timeScale=5;
                while(race.Elapsed<150 && race.Racers.Any(r=>r!=player && !r.Finished)) yield return null;
                Check(race.Racers.Where(r=>r!=player).All(r=>r.Finished),"Both rivals independently finish the whole course");
            }
            if(car==0) {
                // Crossing off-center must advance progress without requiring the centerline radius.
                int before=player.Next;
                player.Body.position=race.Route[before].position+race.Direction(before)*.5f+Vector3.Cross(Vector3.up,race.Direction(before))*5.5f+Vector3.up*.6f;
                yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                Check(player.Next>before,"Full-width road crossing advances ordered progress");
                player.Recover();
            }
            player.Autopilot=true;
            Time.timeScale=5;
            float nextReport=race.Elapsed+20;
            while(race.Running && race.Elapsed<260)
            {
                if(race.Elapsed>nextReport)
                {
                    File.AppendAllText("RelayPlaytestReport.txt","DRIVE car="+car+" t="+race.Elapsed.ToString("0")+" "+string.Join(" | ",race.Racers.Select(r=>r.name+" "+r.Next+"/"+race.Route.Count+" speed="+(r.Body.linearVelocity.magnitude*3.6f).ToString("0")+" recoveries="+r.Recoveries))+"\n");
                    nextReport+=20;
                }
                yield return null;
            }
            Check(race.State==RelayRace.Phase.Results,"Car "+car+" drives entire sprint and reaches results");
            Check(player.Recoveries<=3,"Car "+car+" completes without repeated recovery");
            foreach(var opponent in race.Racers.Where(r=>r!=player)) {
                Check(opponent.Recoveries==0,"Rival stays on track without recovery: "+opponent.DisplayName+" car="+car);
                Check(opponent.Fraction>.8f,"Rival remains competitive: "+opponent.DisplayName+" car="+car);
            }
            Check(!player.GetComponent<RoadBuilderPower>().TryBuild(),"Build blocked after finish");
            Check(PlayerPrefs.GetFloat("RoadRelay.Best."+player.GetComponent<controller>().carName)>0,"Best time saved");
            if(car==0)
            {
                FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name=="RACE AGAIN").onClick.Invoke();
                yield return null; yield return null;
                Check(RelayRace.Instance.State==RelayRace.Phase.Countdown && RelayRace.Instance.Elapsed==0,"Race again resets race");
                RelayRace.Instance.Garage();
                yield return null; yield return null;
                Check(FindFirstObjectByType<RelayGarage>()!=null && Time.timeScale==1,"Change car returns to garage");
            }
        }
        End(true,"All five cars and race lifecycle verified");
    }
    void End(bool passed,string text)
    {
        if(done) return;
        done=true;
        Application.logMessageReceived-=OnLog;
        PlayerPrefs.SetInt("pointer",originalCar);
        foreach(var pair in bestTimes) { if(pair.Value<0) PlayerPrefs.DeleteKey(pair.Key); else PlayerPrefs.SetFloat(pair.Key,pair.Value); }
        PlayerPrefs.Save();
        File.AppendAllText("RelayPlaytestReport.txt",(passed?"SUCCESS ":"FAIL ")+text+"\n");
        exitCode = passed?0:1;
    }
}
#endif
