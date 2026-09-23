using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RelayRace : MonoBehaviour
{
    public enum Phase { Countdown, Racing, Paused, Results }
    public static RelayRace Instance { get; private set; }
    public Phase State { get; private set; }
    public float Elapsed { get; private set; }
    public List<RelayRacer> Racers { get; private set; } = new List<RelayRacer>();
    public List<Transform> Route { get; private set; }
    public RelayRacer Player { get; private set; }
    public bool Running => State == Phase.Racing;
    private float countdown = 3;
    private Text speed, progress, clock, power, message, result;
    private GameObject pausePanel, resultPanel;
    private Transform gate;
    private int gateIndex;
    private float messageUntil;
    private readonly List<RelayRacer> finishOrder = new List<RelayRacer>();
    private Phase beforePause;
    private Text rivals;
    private readonly Dictionary<RelayRacer,Text> rivalLabels = new Dictionary<RelayRacer,Text>();
    public int Place => Ordered().IndexOf(Player) + 1;

    void Awake() { Instance = this; Time.timeScale = 1; AudioListener.pause = false; }
    void Start()
    {
        Route = FindFirstObjectByType<trackWaypoints>().nodes;
        foreach(var car in FindObjectsByType<controller>(FindObjectsSortMode.None))
        {
            if(RelaySettings.TimeTrial && car.CompareTag("AI")) { car.gameObject.SetActive(false); continue; }
            var racer = car.gameObject.AddComponent<RelayRacer>();
            racer.Setup(this);
            Racers.Add(racer);
            if(car.CompareTag("Player")) Player = racer;
            car.nitrusValue = 0;
            foreach(var legacyFinish in car.GetComponentsInChildren<finishTrigger>()) legacyFinish.enabled = false;
            foreach(var legacyAi in car.GetComponents<AIcontroller>()) legacyAi.enabled = false;
            racer.Body.isKinematic = true;
            var builder = car.GetComponent<RoadBuilderPower>() ?? car.gameObject.AddComponent<RoadBuilderPower>();
            builder.maxCharges = 2;
        }
        foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.gameObject.SetActive(false);
        BuildUI();
        CreateGate();
        State = Phase.Countdown;
    }
    void Update()
    {
        if(Player == null) return;
        if(Input.GetKeyDown(KeyCode.Escape) && State != Phase.Results) TogglePause();
        if(State == Phase.Countdown)
        {
            countdown -= Time.deltaTime;
            message.text = "GET READY\n" + Mathf.CeilToInt(Mathf.Max(0,countdown));
            if(countdown <= 0)
            {
                State = Phase.Racing;
                foreach(var racer in Racers) racer.Body.isKinematic = false;
                Notify("GO!", 1);
            }
        }
        if(Running)
        {
            Elapsed += Time.deltaTime;
            if(Input.GetKeyDown(KeyCode.R)) Player.Recover();
            if(Time.time > messageUntil) message.text = "";
        }
        speed.text = Mathf.RoundToInt(Player.Body.linearVelocity.magnitude * 3.6f) + " <size=20>KM/H</size>";
        progress.text = (RelaySettings.TimeTrial?"TIME TRIAL":"POSITION  " + Place + " / " + Racers.Count) + "\nROUTE  " + Mathf.FloorToInt(Player.Fraction * 100) + "%";
        clock.text = Elapsed.ToString("0.0") + " s";
        var builderPower = Player.GetComponent<RoadBuilderPower>();
        power.text = "BOOST  " + builderPower.Charges + "/2" + (builderPower.Charges < 2 ? "   " + builderPower.RechargeRemaining.ToString("0.0") + "s" : "");
        rivals.text = string.Join("\n",Racers.Where(r=>r!=Player).Select(r=>r.DisplayName+"  "+(r.Finished?"FINISHED":((r.Fraction>Player.Fraction?"AHEAD  ":"BEHIND  ")+Mathf.RoundToInt(Vector3.Distance(r.transform.position,Player.transform.position))+" m"))));
        foreach(var pair in rivalLabels) {
            var point=Camera.main.WorldToViewportPoint(pair.Key.transform.position+Vector3.up*2.8f);
            pair.Value.gameObject.SetActive(!pair.Key.Finished && point.z>0 && point.x>.02f && point.x<.98f && point.y>.12f && point.y<.85f);
            pair.Value.rectTransform.anchorMin=pair.Value.rectTransform.anchorMax=new Vector2(point.x,point.y);
        }
        if(gate != null)
        {
            gateIndex = Mathf.Min(((Player.Next / 30) + 1) * 30, Route.Count - 1);
            gate.position = Route[gateIndex].position;
            gate.rotation = Quaternion.LookRotation(Direction(gateIndex),Vector3.up);
        }
    }
    public Vector3 Direction(int i)
    {
        i = Mathf.Clamp(i,0,Route.Count-2);
        var dir = Route[i+1].position-Route[i].position;
        dir.y = 0;
        return dir.normalized;
    }
    public List<RelayRacer> Ordered()
    {
        return Racers.OrderBy(r => r.Finished ? finishOrder.IndexOf(r) : 1000).ThenByDescending(r => r.Fraction).ToList();
    }
    public void Finish(RelayRacer racer)
    {
        if(!Running || racer.Finished) return;
        racer.Finished = true;
        racer.FinishTime = Elapsed;
        finishOrder.Add(racer);
        racer.Body.linearVelocity = Vector3.zero;
        racer.Body.angularVelocity = Vector3.zero;
        racer.Body.isKinematic = true;
        racer.GetComponent<controller>().hasFinished = true;
        // Finished rivals leave the racing surface so they cannot block the player.
        if(racer != Player) { racer.gameObject.SetActive(false); return; }
        State = Phase.Results;
        foreach(var r in Racers) r.Body.isKinematic = true;
        var key = RelaySettings.BestKey(Player.GetComponent<controller>().carName);
        float best = PlayerPrefs.GetFloat(key,0);
        bool newBest = best <= 0 || Elapsed < best;
        if(newBest) { best = Elapsed; PlayerPrefs.SetFloat(key,best); PlayerPrefs.Save(); }
        var lines = Ordered().Select((r,i) => (i+1) + ". " + (r == Player ? "YOU" : r.DisplayName) + "   " + (r.Finished ? r.FinishTime.ToString("0.0") + "s" : "Racing - " + Mathf.FloorToInt(r.Fraction*100) + "%"));
        result.text = (RelaySettings.TimeTrial?"TIME TRIAL COMPLETE":(Place == 1 ? "YOU WIN" : "SPRINT COMPLETE")) + "\n<size=23>" + string.Join("\n",lines) + "\n\n" + (newBest ? "NEW BEST  " : "CAR BEST  ") + best.ToString("0.0") + "s\nBoost strips used: " + Player.BoostsUsed + "</size>";
        message.text = "";
        resultPanel.SetActive(true);
        AudioListener.pause = true;
    }
    public void TogglePause()
    {
        if(State == Phase.Results) return;
        if(State == Phase.Paused) { State = beforePause; Time.timeScale = 1; AudioListener.pause = false; }
        else { beforePause = State; State = Phase.Paused; Time.timeScale = 0; AudioListener.pause = true; }
        pausePanel.SetActive(State == Phase.Paused);
    }
    public void Restart() { Time.timeScale = 1; AudioListener.pause = false; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    public void Garage() { Time.timeScale = 1; AudioListener.pause = false; SceneManager.LoadScene("awakeScene"); }
    public void Notify(string text, float seconds = 2) { if(message == null) return; message.text = text; messageUntil = Time.time + seconds; }
    void OnDestroy() { if(Instance == this) Instance = null; Time.timeScale = 1; AudioListener.pause = false; }
    void OnApplicationFocus(bool focus) { if(!focus && Running) TogglePause(); }
    void BuildUI()
    {
        var canvas = RelayUI.Canvas("Road Relay HUD");
        var top = RelayUI.Panel(canvas.transform,"Race status",new Vector2(.5f,1),new Vector2(0,-55),new Vector2(1230,85));
        RelayUI.Label(top.transform,"ROAD RELAY",new Vector2(0,.5f),new Vector2(160,0),new Vector2(290,55),30).color = RelayUI.Cyan;
        progress = RelayUI.Label(top.transform,"",new Vector2(.5f,.5f),Vector2.zero,new Vector2(240,75),22);
        clock = RelayUI.Label(top.transform,"",new Vector2(1,.5f),new Vector2(-130,0),new Vector2(220,55),30,TextAnchor.MiddleRight);
        var rivalPanel=RelayUI.Panel(canvas.transform,"Rival tracker",new Vector2(0,1),new Vector2(155,-153),new Vector2(260,80));
        rivals=RelayUI.Label(rivalPanel.transform,"",new Vector2(.5f,.5f),Vector2.zero,new Vector2(230,70),19);
        rivalPanel.SetActive(!RelaySettings.TimeTrial);
        var mapPanel=RelayUI.Panel(canvas.transform,"Course map",new Vector2(1,1),new Vector2(-150,-195),new Vector2(250,150));
        var map=RelayUI.Rect(mapPanel.transform,"Live course",new Vector2(.5f,.5f),Vector2.zero,new Vector2(235,135)).gameObject.AddComponent<RelayMinimap>();
        map.Race=this;map.raycastTarget=false;
        foreach(var racer in Racers.Where(r=>r!=Player)) {
            var label=RelayUI.Label(canvas.transform,racer.DisplayName+"  ▼",new Vector2(.5f,.5f),Vector2.zero,new Vector2(170,35),20,TextAnchor.MiddleCenter);
            label.color=racer.name.Contains("1")?new Color(1,.65f,.2f):new Color(1,.35f,.5f);
            label.gameObject.AddComponent<Outline>().effectDistance=new Vector2(1,-1);
            rivalLabels[racer]=label;
        }
        var bottom = RelayUI.Panel(canvas.transform,"Driving",new Vector2(0,0),new Vector2(245,48),new Vector2(440,65));
        speed = RelayUI.Label(bottom.transform,"",new Vector2(0,.5f),new Vector2(110,0),new Vector2(195,60),35);
        power = RelayUI.Label(bottom.transform,"",new Vector2(1,.5f),new Vector2(-115,0),new Vector2(210,60),21,TextAnchor.MiddleRight);
        message = RelayUI.Label(canvas.transform,"",new Vector2(.5f,.5f),new Vector2(0,120),new Vector2(900,120),34,TextAnchor.MiddleCenter);
        pausePanel = RelayUI.Panel(canvas.transform,"Paused",new Vector2(.5f,.5f),Vector2.zero,new Vector2(540,430));
        RelayUI.Label(pausePanel.transform,"PAUSED",new Vector2(.5f,.5f),new Vector2(0,150),new Vector2(460,65),40,TextAnchor.MiddleCenter);
        RelayUI.Button(pausePanel.transform,"RESUME",new Vector2(0,65),new Vector2(420,55),TogglePause);
        RelayUI.Button(pausePanel.transform,"RESTART SPRINT",new Vector2(0,-10),new Vector2(420,55),Restart);
        RelayUI.Button(pausePanel.transform,"CHANGE CAR",new Vector2(0,-85),new Vector2(420,55),Garage);
        RelayUI.Label(pausePanel.transform,"WASD / arrows: drive    Space: brake\nB: build shared boost strip    R: recover",new Vector2(.5f,.5f),new Vector2(0,-160),new Vector2(500,60),18,TextAnchor.MiddleCenter);
        pausePanel.SetActive(false);
        resultPanel = RelayUI.Panel(canvas.transform,"Results",new Vector2(.5f,.5f),Vector2.zero,new Vector2(640,490));
        result = RelayUI.Label(resultPanel.transform,"",new Vector2(.5f,.5f),new Vector2(0,55),new Vector2(570,325),38,TextAnchor.MiddleCenter);
        RelayUI.Button(resultPanel.transform,"RACE AGAIN",new Vector2(-145,-175),new Vector2(260,58),Restart);
        RelayUI.Button(resultPanel.transform,"CHANGE CAR",new Vector2(145,-175),new Vector2(260,58),Garage);
        resultPanel.SetActive(false);
    }
    void CreateGate()
    {
        gate = new GameObject("Next route gate").transform;
        var material = new Material(Shader.Find("Standard"));
        material.color = RelayUI.Cyan;
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor",RelayUI.Cyan);
        for(int i = 0; i < 3; i++)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.transform.SetParent(gate,false);
            part.transform.localPosition = i < 2 ? new Vector3(i == 0 ? -6.5f : 6.5f,3,0) : new Vector3(0,6,0);
            part.transform.localScale = i < 2 ? new Vector3(.18f,6,.18f) : new Vector3(13,.18f,.18f);
            Destroy(part.GetComponent<Collider>());
            part.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
