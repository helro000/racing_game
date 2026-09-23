using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class EasyRoadsRaceSetup : MonoBehaviour
{
    private void Awake()
    {
        var pathObject = GameObject.FindGameObjectWithTag("path");
        if (pathObject == null)
        {
            return;
        }

        var waypoints = pathObject.GetComponent<trackWaypoints>();
        if (waypoints == null)
        {
            return;
        }

        waypoints.RefreshNodes();
        foreach (var aiObject in GameObject.FindGameObjectsWithTag("AI"))
        {
            ConfigureAi(aiObject, waypoints);
        }
    }

    private static void ConfigureAi(GameObject aiObject, trackWaypoints waypoints)
    {
        foreach (var ai in aiObject.GetComponents<AIcontroller>())
        {
            ai.waypoints = waypoints;
            ai.nodes = waypoints.nodes;
            ai.currentWaypoint = waypoints.nodes.Count > 0 ? waypoints.nodes[0] : aiObject.transform;
        }
    }
}
