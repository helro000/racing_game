using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomRoadHazardSpawner : MonoBehaviour
{
    [Header("Spawn Timing")]
    public float firstSpawnDelay = 5f;
    public float minimumSpawnTime = 8f;
    public float maximumSpawnTime = 12f;

    [Header("Spawn Limits")]
    public int maximumHazards = 4;
    public float hazardLifetime = 60f;
    public float laneWidth = 4f;

    [Header("Random Hazard Size")]
    public float minimumScale = 0.8f;
    public float maximumScale = 1.35f;

    private RelayRace race;

    private GameObject oilPrefab;
    private GameObject mudPrefab;
    private GameObject roughPrefab;

    private readonly List<GameObject> hazards =
        new List<GameObject>();


    private void Awake()
    {
        oilPrefab =
            Resources.Load<GameObject>(
                "Hazards/OilSlick"
            );

        mudPrefab =
            Resources.Load<GameObject>(
                "Hazards/MudPatch"
            );

        roughPrefab =
            Resources.Load<GameObject>(
                "Hazards/RoughRoad"
            );


        Debug.Log(
            "HAZARD LOAD: Oil=" +
            (oilPrefab != null) +
            " Mud=" +
            (mudPrefab != null) +
            " Rough=" +
            (roughPrefab != null)
        );
    }


    private IEnumerator Start()
    {
        // Wait for race
        while (RelayRace.Instance == null)
        {
            yield return null;
        }

        race =
            RelayRace.Instance;


        // Wait for route
        while (
            race.Route == null ||
            race.Route.Count < 2
        )
        {
            yield return null;
        }


        Debug.Log(
            "HAZARD SYSTEM READY. Route nodes: "
            + race.Route.Count
        );


        // =============================
        // DIFFICULTY
        // =============================

        if (RelaySettings.Difficulty == 0)
        {
            // EASY
            minimumSpawnTime = 12f;
            maximumSpawnTime = 18f;
            maximumHazards = 2;
        }
        else if (RelaySettings.Difficulty == 2)
        {
            // HARD
            minimumSpawnTime = 6f;
            maximumSpawnTime = 10f;
            maximumHazards = 4;
        }
        else
        {
            // NORMAL
            minimumSpawnTime = 9f;
            maximumSpawnTime = 14f;
            maximumHazards = 3;
        }


        // Wait until countdown finishes
        while (!race.Running)
        {
            yield return null;
        }


        Debug.Log(
            "HAZARD SYSTEM: Race started"
        );


        // =============================
        // FIRST HAZARD AFTER 5 SECONDS
        // =============================

        yield return new WaitForSeconds(
            firstSpawnDelay
        );


        if (
            race != null &&
            race.Running
        )
        {
            SpawnHazard();
        }


        // =============================
        // CONTINUE RANDOM SPAWNING
        // =============================

        while (true)
        {
            float waitTime =
                Random.Range(
                    minimumSpawnTime,
                    maximumSpawnTime
                );


            yield return new WaitForSeconds(
                waitTime
            );


            if (
                race != null &&
                race.Running
            )
            {
                SpawnHazard();
            }
        }
    }


    private void SpawnHazard()
    {
        // Remove destroyed hazards
        hazards.RemoveAll(
            x => x == null
        );


        if (
            hazards.Count >=
            maximumHazards
        )
        {
            return;
        }


        if (
            race == null ||
            race.Player == null ||
            race.Route == null ||
            race.Route.Count < 2
        )
        {
            Debug.LogWarning(
                "HAZARD: Race data missing"
            );

            return;
        }


        // =============================
        // RANDOM HAZARD TYPE
        // 0 = Oil
        // 1 = Mud
        // 2 = Rough
        // =============================

        int randomType =
            Random.Range(0, 3);


        GameObject prefab;


        if (randomType == 0)
        {
            prefab =
                oilPrefab;
        }
        else if (randomType == 1)
        {
            prefab =
                mudPrefab;
        }
        else
        {
            prefab =
                roughPrefab;
        }


        if (prefab == null)
        {
            Debug.LogWarning(
                "HAZARD PREFAB NULL. Type = "
                + randomType
            );

            return;
        }


        // =============================
        // POSITION AHEAD OF PLAYER
        // =============================

        int playerNode =
            race.Player.Next;


        int minIndex =
            Mathf.Clamp(
                playerNode + 8,
                0,
                race.Route.Count - 1
            );


        int maxIndex =
            Mathf.Clamp(
                playerNode + 30,
                0,
                race.Route.Count - 1
            );


        if (maxIndex <= minIndex)
        {
            minIndex =
                Mathf.Clamp(
                    playerNode + 1,
                    0,
                    race.Route.Count - 1
                );

            maxIndex =
                race.Route.Count - 1;
        }


        if (maxIndex <= minIndex)
        {
            return;
        }


        int index =
            Random.Range(
                minIndex,
                maxIndex
            );


        int directionIndex =
            Mathf.Min(
                index,
                race.Route.Count - 2
            );


        Vector3 direction =
            race.Direction(
                directionIndex
            );


        Vector3 side =
            Vector3.Cross(
                Vector3.up,
                direction
            ).normalized;


        float offset =
            Random.Range(
                -laneWidth,
                laneWidth
            );


        Vector3 position =
            race.Route[index].position +
            side * offset;


        // =============================
        // FIND ROAD SURFACE
        // =============================

        RaycastHit hit;

        Vector3 normal =
            Vector3.up;


        Vector3 rayStart =
            position +
            Vector3.up * 20f;


        if (
            Physics.Raycast(
                rayStart,
                Vector3.down,
                out hit,
                50f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
            )
        )
        {
            normal =
                hit.normal;


            position =
                hit.point +
                normal * 0.02f;
        }


        // =============================
        // CREATE HAZARD
        // =============================

        GameObject hazard =
            Instantiate(
                prefab,
                position,
                Quaternion.identity
            );


        // =============================
        // RANDOM SIZE
        // =============================

        float randomScale =
            Random.Range(
                minimumScale,
                maximumScale
            );


        Vector3 originalScale =
            hazard.transform.localScale;


        hazard.transform.localScale =
            originalScale *
            randomScale;


        // =============================
        // ALIGN VISUAL TO ROAD
        // =============================

        MeshRenderer renderer =
            hazard.GetComponentInChildren<
                MeshRenderer
            >();


        if (renderer != null)
        {
            Transform visual =
                renderer.transform;


            Vector3 forward =
                Vector3.ProjectOnPlane(
                    direction,
                    normal
                );


            if (
                forward.sqrMagnitude <
                0.01f
            )
            {
                forward =
                    Vector3.forward;
            }


            forward.Normalize();


            // Makes visible side of Quad face upward
            visual.rotation =
                Quaternion.LookRotation(
                    -normal,
                    forward
                );


            visual.position =
                position;
        }


        // =============================
        // KEEP COLLIDER WITH HAZARD
        // =============================

        Collider hazardCollider =
            hazard.GetComponentInChildren<
                Collider
            >();


        if (hazardCollider != null)
        {
            hazardCollider.transform.position =
                position;
        }


        hazards.Add(
            hazard
        );


        Debug.Log(
            "HAZARD SPAWNED: "
            + hazard.name
            + " node="
            + index
            + " scale="
            + randomScale.ToString("0.00")
        );


        // Remove later
        Destroy(
            hazard,
            hazardLifetime
        );
    }
}