using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RelayGarage : MonoBehaviour
{
    private awakeManager garage;
    private Text carLabel;
    private Text trackLabel,modeLabel,difficultyLabel;
    private bool loading;
    private Button previous, next;
    void Start()
    {
        Time.timeScale = 1;
        AudioListener.pause = false;
        garage = GetComponent<awakeManager>();
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)) c.gameObject.SetActive(false);
        var canvas = RelayUI.Canvas("Road Relay Garage");
        var panel = RelayUI.Panel(canvas.transform, "Choose your ride", new Vector2(0,.5f), new Vector2(230,0), new Vector2(420,660));
        RelayUI.Label(panel.transform,"ROAD RELAY", new Vector2(.5f,.5f), new Vector2(0,245), new Vector2(365,75), 43).color = RelayUI.Cyan;
        RelayUI.Label(panel.transform,"BUILD YOUR ADVANTAGE", new Vector2(.5f,.5f), new Vector2(0,145), new Vector2(350,40), 19);
        RelayUI.Label(panel.transform,"Four tracks. Five cars.\nRace rivals or beat your best time.", new Vector2(.5f,.5f), new Vector2(0,70), new Vector2(350,95), 22);
        carLabel = RelayUI.Label(panel.transform,"", new Vector2(.5f,.5f), new Vector2(0,-15), new Vector2(350,45), 24, TextAnchor.MiddleCenter);
        previous = RelayUI.Button(panel.transform,"< CAR",new Vector2(-90,-75),new Vector2(165,48),()=>Select(-1));
        next = RelayUI.Button(panel.transform,"CAR >",new Vector2(90,-75),new Vector2(165,48),()=>Select(1));
        RelayUI.Button(panel.transform,"RACE",new Vector2(0,-145),new Vector2(350,58),Play);
        RelayUI.Label(panel.transform,"WASD / arrows  Drive\nB  Build boost    R  Recover\nSpace  Brake    Esc  Pause", new Vector2(.5f,.5f), new Vector2(0,-235), new Vector2(350,100), 19);
        var options=RelayUI.Panel(canvas.transform,"Race setup",new Vector2(1,.5f),new Vector2(-225,0),new Vector2(400,440));
        RelayUI.Label(options.transform,"RACE SETUP",new Vector2(.5f,.5f),new Vector2(0,165),new Vector2(350,45),30,TextAnchor.MiddleCenter);
        trackLabel=RelayUI.Button(options.transform,"",new Vector2(0,85),new Vector2(350,65),()=>{RelaySettings.Track=(RelaySettings.Track+1)%4;RefreshOptions();}).GetComponentInChildren<Text>();
        modeLabel=RelayUI.Button(options.transform,"",new Vector2(0,0),new Vector2(350,65),()=>{RelaySettings.TimeTrial=!RelaySettings.TimeTrial;RefreshOptions();}).GetComponentInChildren<Text>();
        difficultyLabel=RelayUI.Button(options.transform,"",new Vector2(0,-85),new Vector2(350,65),()=>{RelaySettings.Difficulty=(RelaySettings.Difficulty+1)%3;RefreshOptions();}).GetComponentInChildren<Text>();
        RelayUI.Label(options.transform,"Click an option to change it",new Vector2(.5f,.5f),new Vector2(0,-170),new Vector2(350,35),18,TextAnchor.MiddleCenter);
        RefreshOptions();
        Refresh();
    }
    void RefreshOptions() {
        trackLabel.text=RelaySettings.Tracks[RelaySettings.Track]+"  >";
        modeLabel.text=(RelaySettings.TimeTrial?"TIME TRIAL":"RACE")+"  >";
        difficultyLabel.text=RelaySettings.TimeTrial?"SOLO / PERSONAL BEST":RelaySettings.Difficulties[RelaySettings.Difficulty]+"  >";
        difficultyLabel.GetComponentInParent<Button>().interactable=!RelaySettings.TimeTrial;
    }
    public void Select(int delta)
    {
        if (loading) return;
        if(delta > 0) garage.rightButton(); else garage.leftButton();
        Refresh();
    }
    void Refresh() {
        carLabel.text = garage.listOfVehicles.vehicles[garage.vehiclePointer].GetComponent<controller>().carName + "  /  FREE";
        previous.interactable = garage.vehiclePointer > 0;
        next.interactable = garage.vehiclePointer < garage.listOfVehicles.vehicles.Length-1;
    }
    public void Play()
    {
        if(loading) return;
        loading = true;
        PlayerPrefs.Save();
        SceneManager.LoadScene(RelaySettings.Scenes[RelaySettings.Track]);
    }
}
