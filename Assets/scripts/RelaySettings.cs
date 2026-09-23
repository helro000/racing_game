using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RelaySettings
{
    public static readonly string[] Scenes={"EasyRoadsMap","CitySprint","MountainPass","DesertRun"};
    public static readonly string[] Tracks={"VALLEY SPRINT","CITY SPRINT","MOUNTAIN PASS","DESERT RUN"};
    public static readonly string[] Difficulties={"EASY","NORMAL","HARD"};
    public static int Track { get=>Mathf.Clamp(PlayerPrefs.GetInt("Relay.Track"),0,3); set=>PlayerPrefs.SetInt("Relay.Track",Mathf.Clamp(value,0,3)); }
    public static int Difficulty { get=>Mathf.Clamp(PlayerPrefs.GetInt("Relay.Difficulty",1),0,2); set=>PlayerPrefs.SetInt("Relay.Difficulty",Mathf.Clamp(value,0,2)); }
    public static bool TimeTrial { get=>PlayerPrefs.GetInt("Relay.TimeTrial")==1; set=>PlayerPrefs.SetInt("Relay.TimeTrial",value?1:0); }
    public static bool IsRaceScene => Array.IndexOf(Scenes,SceneManager.GetActiveScene().name)>=0;
    public static string BestKey(string car) => "RoadRelay.Best.v2."+SceneManager.GetActiveScene().name+"."+(TimeTrial?"TimeTrial":Difficulties[Difficulty])+"."+car;
    public static string[] BuildScenes => new[]{"Assets/Scenes/awakeScene.unity","Assets/Scenes/EasyRoadsMap.unity","Assets/Scenes/CitySprint.unity","Assets/Scenes/MountainPass.unity","Assets/Scenes/DesertRun.unity"};
}
