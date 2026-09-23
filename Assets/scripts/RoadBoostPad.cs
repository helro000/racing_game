using System.Collections.Generic;
using UnityEngine;

public class RoadBoostPad : MonoBehaviour
{
    public RelayRacer Owner;
    private readonly HashSet<Rigidbody> used = new HashSet<Rigidbody>();
    void Start() { Destroy(gameObject,12); }
    void OnTriggerEnter(Collider other) { TryBoost(other.attachedRigidbody); }
    public bool TryBoost(Rigidbody body)
    {
        if(body == null || body.isKinematic || RelayRace.Instance == null || !RelayRace.Instance.Running) return false;
        var racer = body.GetComponent<RelayRacer>();
        if(racer == null || racer.Finished || used.Contains(body)) return false;
        float forwardSpeed = Vector3.Dot(body.linearVelocity,transform.forward);
        if(forwardSpeed < -.5f || Vector3.Dot(body.rotation*Vector3.forward,transform.forward) < .4f) return false;
        used.Add(body);
        body.AddForce(transform.forward*Mathf.Clamp(27-forwardSpeed,0,7),ForceMode.VelocityChange);
        racer.BoostsUsed++;
        if(racer.CompareTag("Player")) RelayRace.Instance.Notify(Owner == racer ? "YOUR BOOST +" : "RIVAL BOOST STOLEN +",1.5f);
        return true;
    }
}
