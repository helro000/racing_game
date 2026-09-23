using UnityEngine;

public class RoadBuilderPower : MonoBehaviour
{
    public int maxCharges = 2;
    public int Charges { get; private set; }
    public float RechargeRemaining { get; private set; }
    private float nextBuild;
    private Material material;
    void Start()
    {
        Charges = maxCharges;
        material = new Material(Shader.Find("Standard"));
        material.color = CompareTag("Player") ? RelayUI.Cyan : new Color(1,.55f,.12f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor",material.color*.6f);
    }
    void Update()
    {
        var race = RelayRace.Instance;
        if(race == null || !race.Running) return;
        if(Charges < maxCharges)
        {
            RechargeRemaining -= Time.deltaTime;
            if(RechargeRemaining <= 0) { Charges++; RechargeRemaining = Charges < maxCharges ? 8 : 0; }
        }
        if(CompareTag("Player") && Input.GetKeyDown(KeyCode.B)) TryBuild();
    }
    public bool TryBuild()
    {
        var race = RelayRace.Instance;
        var racer = GetComponent<RelayRacer>();
        if(race == null || !race.Running || racer == null || racer.Finished || Charges <= 0 || Time.time < nextBuild) return false;
        if(RelayRacer.HorizontalDistance(transform.position,race.Route[racer.Next].position) > 12)
        {
            if(CompareTag("Player")) race.Notify("Return to the road to build");
            return false;
        }
        int index = Mathf.Min(racer.Next+4,race.Route.Count-2);
        Vector3 position = race.Route[index].position;
        var direction = race.Direction(index);
        if(!Physics.Raycast(position+Vector3.up*5,Vector3.down,out var hit,15,~0,QueryTriggerInteraction.Ignore)) return false;
        if(hit.collider.GetComponentInParent<controller>() != null) return false;
        var pad = new GameObject("Shared boost strip");
        pad.transform.position = hit.point + hit.normal*.06f;
        pad.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(direction,hit.normal),hit.normal);
        var trigger = pad.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0,1,0);
        trigger.size = new Vector3(5,2.5f,8);
        var boost = pad.AddComponent<RoadBoostPad>();
        boost.Owner = racer;
        for(int i=0;i<5;i++)
        {
            var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.transform.SetParent(pad.transform,false);
            stripe.transform.localPosition = new Vector3(0,0,-3+i*1.5f);
            stripe.transform.localScale = new Vector3(4.8f,.08f,.55f);
            Destroy(stripe.GetComponent<Collider>());
            stripe.GetComponent<Renderer>().sharedMaterial = material;
        }
        Charges--;
        if(RechargeRemaining <= 0) RechargeRemaining = 8;
        nextBuild = Time.time+1;
        if(CompareTag("Player")) race.Notify("BOOST READY",1);
        return true;
    }
    void OnDestroy() { if(material != null) Destroy(material); }
}
