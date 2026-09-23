using UnityEngine;
using UnityEngine.UI;

// Explicitly supply the renderer: Graphic's lazy null check behaves differently
// for a missing Unity component in the Editor and in a standalone player.
[RequireComponent(typeof(CanvasRenderer))]
public class RelayMinimap : MaskableGraphic
{
    public RelayRace Race;
    Vector2 center;
    float mapScale;
    void Measure()
    {
        var min=new Vector2(float.MaxValue,float.MaxValue);
        var max=new Vector2(float.MinValue,float.MinValue);
        foreach(var node in Race.Route) { var p=new Vector2(node.position.x,node.position.z); min=Vector2.Min(min,p);max=Vector2.Max(max,p); }
        Vector2 size=max-min;
        center=(min+max)*.5f;
        mapScale=Mathf.Min((rectTransform.rect.width-22)/Mathf.Max(1,size.x),(rectTransform.rect.height-22)/Mathf.Max(1,size.y));
    }
    Vector2 Project(Vector3 world)
    {
        return (new Vector2(world.x,world.z)-center)*mapScale;
    }
    void LateUpdate() { SetVerticesDirty(); }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if(Race==null || Race.Route==null) return;
        Measure();
        for(int i=1;i<Race.Route.Count;i++) {
            Vector2 a=Project(Race.Route[i-1].position), b=Project(Race.Route[i].position);
            var perpendicular=new Vector2(-(b-a).y,(b-a).x).normalized*2;
            Quad(mesh,a-perpendicular,a+perpendicular,b+perpendicular,b-perpendicular,new Color(.6f,.7f,.72f));
        }
        foreach(var racer in Race.Racers) {
            var p=Project(racer.transform.position);
            var c=racer==Race.Player?RelayUI.Cyan:(racer.name.Contains("1")?new Color(1,.65f,.2f):new Color(1,.35f,.5f));
            Quad(mesh,p+new Vector2(-4,-4),p+new Vector2(-4,4),p+new Vector2(4,4),p+new Vector2(4,-4),c);
        }
    }
    static void Quad(VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color color)
    {
        int start=mesh.currentVertCount;
        mesh.AddVert(a,color,Vector2.zero);mesh.AddVert(b,color,Vector2.zero);mesh.AddVert(c,color,Vector2.zero);mesh.AddVert(d,color,Vector2.zero);
        mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
    }
}
