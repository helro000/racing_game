using System.Collections.Generic;
using UnityEngine;

public class RoadHazard : MonoBehaviour
{
    public enum HazardType
    {
        Oil,
        Mud,
        Rough
    }

    public HazardType type;

    private HashSet<RelayRacer> affectedCars =
        new HashSet<RelayRacer>();


    private void Awake()
    {
        // Remove MeshCollider if one exists
        MeshCollider meshCollider =
            GetComponent<MeshCollider>();

        if (meshCollider != null)
        {
            Destroy(meshCollider);
        }


        // Get or create BoxCollider
        BoxCollider box =
            GetComponent<BoxCollider>();

        if (box == null)
        {
            box =
                gameObject.AddComponent<BoxCollider>();
        }


        box.isTrigger = true;


        // Quad mesh is normally 1 x 1.
        // Since the Quad itself is scaled,
        // this collider automatically scales
        // exactly with the visible image.
        box.center =
            Vector3.zero;


        box.size =
            new Vector3(
                1f,
                1f,
                0.35f
            );
    }


    private void OnTriggerEnter(
        Collider other
    )
    {
        RelayRacer racer =
            other.GetComponent<RelayRacer>();


        if (racer == null)
        {
            racer =
                other.GetComponentInParent<RelayRacer>();
        }


        if (racer == null)
        {
            return;
        }


        // Same car cannot trigger
        // the same hazard twice
        if (
            affectedCars.Contains(racer)
        )
        {
            return;
        }


        affectedCars.Add(racer);


        CarHazardEffect effect =
            racer.GetComponent<CarHazardEffect>();


        if (effect == null)
        {
            effect =
                racer.gameObject.AddComponent<
                    CarHazardEffect
                >();
        }


        Debug.Log(
            "HAZARD HIT: "
            + type
            + " by "
            + racer.name
        );


        effect.Apply(type);


        // Do NOT disable collider.
        // Other cars can still hit this hazard.
    }
}