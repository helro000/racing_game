using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// The road mesh and navigation samples share a single centerline.
public static class RelayTrackGeometry
{
    static string Folder = "Assets/RoadRelay";
    static Material Material(string name, Color color)
    {
        string path = Folder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material, path); }
        material.color = color;
        material.SetFloat("_Glossiness", .12f);
        return material;
    }
    static GameObject Block(string name, Vector3 position, Vector3 scale, Quaternion rotation, Material material, bool solid = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetPositionAndRotation(position, rotation);
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        if (!solid) Object.DestroyImmediate(go.GetComponent<Collider>());
        go.isStatic = true;
        return go;
    }
    public static List<Vector3> Build(int theme=0)
    {
        if (!AssetDatabase.IsValidFolder("Assets/RoadRelay")) AssetDatabase.CreateFolder("Assets", "RoadRelay");
        Folder=theme==0?"Assets/RoadRelay":"Assets/RoadRelay/"+RelaySettings.Scenes[theme];
        if(!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/RoadRelay",RelaySettings.Scenes[theme]);
        var asphalt = Material("SprintAsphalt", new Color(.105f,.13f,.16f));
        var grass = Material("SprintGrass", theme==3?new Color(.72f,.52f,.28f):theme==1?new Color(.29f,.32f,.35f):new Color(.24f,.39f,.22f));
        var white = Material("SprintWhite", new Color(.9f,.94f,.91f));
        var red = Material("SprintCoral", new Color(.95f,.23f,.13f));
        var gravel = Material("SprintShoulder", new Color(.42f,.43f,.35f));
        var foliage = Material("SprintPine", new Color(.075f,.23f,.17f));
        var stone = Material("SprintRock", new Color(.36f,.44f,.43f));
        var trunk = Material("SprintTrunk", new Color(.25f,.19f,.13f));
        Vector3[] controls = {
            new Vector3(0,0,-70), new Vector3(0,0,0), new Vector3(0,0,110),
            new Vector3(50,0,215), new Vector3(155,0,260), new Vector3(255,0,230),
            new Vector3(315,0,130), new Vector3(410,0,85), new Vector3(525,0,120),
            new Vector3(575,0,230), new Vector3(560,0,360), new Vector3(570,0,435)
        };
        if(theme==1) controls=new[]{new Vector3(0,0,-70),new Vector3(0,0,0),new Vector3(0,0,140),new Vector3(60,0,220),new Vector3(200,0,220),new Vector3(290,0,300),new Vector3(290,0,440),new Vector3(210,0,520),new Vector3(70,0,520),new Vector3(0,0,610),new Vector3(0,0,710)};
        if(theme==2) controls=new[]{new Vector3(0,0,-70),new Vector3(0,0,0),new Vector3(0,2,110),new Vector3(-60,7,210),new Vector3(-170,12,250),new Vector3(-270,16,320),new Vector3(-260,13,440),new Vector3(-140,7,510),new Vector3(0,3,530),new Vector3(90,0,610),new Vector3(140,0,700)};
        if(theme==3) controls=new[]{new Vector3(0,0,-70),new Vector3(0,0,0),new Vector3(0,0,190),new Vector3(-80,0,350),new Vector3(-240,0,410),new Vector3(-400,0,350),new Vector3(-470,0,190),new Vector3(-600,0,90),new Vector3(-760,0,140),new Vector3(-800,0,280),new Vector3(-800,0,400)};
        var points = new List<Vector3>();
        for (int s=1;s<controls.Length-2;s++)
        for (int j=0;j<150;j++)
        {
            float t=j/150f;
            var a=controls[s-1]; var b=controls[s]; var c=controls[s+1]; var d=controls[s+2];
            Vector3 p=.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);
            if(points.Count==0 || Vector3.Distance(points[points.Count-1],p)>=3.5f) points.Add(p);
        }
        points.Add(controls[controls.Length-2]);
        Block("Valley floor",new Vector3(0,-.45f,250),new Vector3(2400,.6f,2000),Quaternion.identity,grass,true);
        // Extend pavement behind the grid and beyond the finish.
        var pavement = new List<Vector3> { points[0]-Vector3.forward*45 };
        pavement.AddRange(points);
        pavement.Add(points[points.Count-1]+(points[points.Count-1]-points[points.Count-2]).normalized*55);
        if(theme==2) Ribbon("Mountain embankment",pavement,40,-.4f,grass,true);
        Ribbon("Shoulders",pavement,8,.005f,gravel,false);
        Ribbon("Track",pavement,6,.035f,asphalt,true);
        for(int i=0;i<pavement.Count-1;i++)
        {
            Vector3 delta=pavement[i+1]-pavement[i], direction=delta.normalized;
            Vector3 center=(pavement[i]+pavement[i+1])*.5f, right=Vector3.Cross(Vector3.up,direction);
            var rotation=Quaternion.LookRotation(direction);
            if(i%3==0) Block("Center dash",center+Vector3.up*.055f,new Vector3(.14f,.02f,2.4f),rotation,white);
            foreach(int side in new[]{-1,1})
            {
                Block("Edge line",center+right*5.65f*side+Vector3.up*.06f,new Vector3(.15f,.02f,delta.magnitude+.05f),rotation,white);
                Block("Curb",center+right*6.25f*side+Vector3.up*.07f,new Vector3(.5f,.12f,delta.magnitude+.05f),rotation,i%2==0?white:red);
                Block("Safety barrier",center+right*7.7f*side+Vector3.up*.55f,new Vector3(.4f,1.1f,delta.magnitude+.1f),rotation,i%5==0?red:white,true);
            }
        }
        var random = new System.Random(73);
        for(int i=4;i<points.Count-3;i+=5)
        {
            var forward=(points[i+1]-points[i]).normalized;
            var right=Vector3.Cross(Vector3.up,forward);
            foreach(int side in new[]{-1,1})
            {
                Vector3 p=points[i]+right*side*(15+(float)random.NextDouble()*19);
                float height=5+(float)random.NextDouble()*5;
                if(theme==1) {
                    height=12+(float)random.NextDouble()*26;
                    var building=Block("City building",p+Vector3.up*height*.5f,new Vector3(10,height,12),Quaternion.LookRotation(forward),i%2==0?stone:gravel);
                    for(float level=4;level<height;level+=5) Block("Window band",p+Vector3.up*level,new Vector3(10.2f,1.2f,12.2f),building.transform.rotation,white);
                    continue;
                }
                if(theme==3) {
                    var formation=GameObject.CreatePrimitive(PrimitiveType.Cube);
                    formation.name="Desert sandstone";formation.transform.position=p+Vector3.up*height*.25f;
                    formation.transform.localScale=new Vector3(8,height*.6f,6);formation.transform.rotation=Quaternion.Euler(0,i*17,12);
                    formation.GetComponent<Renderer>().sharedMaterial=gravel;Object.DestroyImmediate(formation.GetComponent<Collider>());
                    continue;
                }
                Block("Pine trunk",p+Vector3.up*height*.25f,new Vector3(.55f,height*.5f,.55f),Quaternion.identity,trunk);
                var tree=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tree.name="Valley pine"; tree.transform.position=p+Vector3.up*height*.65f;
                tree.transform.localScale=new Vector3(4,height,4);
                tree.GetComponent<Renderer>().sharedMaterial=foliage;
                Object.DestroyImmediate(tree.GetComponent<Collider>());
                if(i%10==4) {
                    var rock=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    rock.name="Rock outcrop"; rock.transform.position=p+right*side*7;
                    rock.transform.localScale=new Vector3(9,5,7);
                    rock.GetComponent<Renderer>().sharedMaterial=stone;
                    Object.DestroyImmediate(rock.GetComponent<Collider>());
                }
            }
        }
        for(int i=0;i<16;i++) {
            float angle=i*Mathf.PI*2/16;
            var hill=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hill.name="Distant ridge";
            hill.transform.position=new Vector3(270+Mathf.Cos(angle)*850,-35,180+Mathf.Sin(angle)*720);
            hill.transform.localScale=new Vector3(350,180+(i%4)*40,300);
            hill.GetComponent<Renderer>().sharedMaterial=i%2==0?grass:stone;
            Object.DestroyImmediate(hill.GetComponent<Collider>());
        }
        for(int row=0;row<2;row++) for(int col=0;col<12;col++)
        {
            var finishDirection=(points[points.Count-1]-points[points.Count-2]).normalized;
            Block("Finish checker",points[points.Count-1]+Vector3.Cross(Vector3.up,finishDirection)*(col-5.5f)+finishDirection*row+Vector3.up*.07f, new Vector3(1,.02f,1),Quaternion.LookRotation(finishDirection),(col+row)%2==0?white:asphalt);
        }
        var sun=new GameObject("Afternoon sun").AddComponent<Light>();
        sun.type=LightType.Directional; sun.intensity=1.15f; sun.color=new Color(1,.94f,.82f);
        sun.transform.rotation=Quaternion.Euler(48,-35,0); sun.shadows=LightShadows.Soft;
        RenderSettings.ambientLight=new Color(.58f,.66f,.73f);
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.fog=true; RenderSettings.fogColor=new Color(.62f,.77f,.83f);
        RenderSettings.fogMode=FogMode.Linear; RenderSettings.fogStartDistance=350; RenderSettings.fogEndDistance=1400;
        Physics.SyncTransforms();
        return points;
    }
    static void Ribbon(string name,List<Vector3> points,float halfWidth,float y,Material material,bool collision)
    {
        var vertices=new Vector3[points.Count*2]; var uv=new Vector2[vertices.Length]; var tris=new int[(points.Count-1)*6];
        for(int i=0;i<points.Count;i++) {
            var direction=(points[Mathf.Min(i+1,points.Count-1)]-points[Mathf.Max(0,i-1)]).normalized;
            var right=Vector3.Cross(Vector3.up,direction);
            vertices[i*2]=points[i]-right*halfWidth+Vector3.up*y;
            vertices[i*2+1]=points[i]+right*halfWidth+Vector3.up*y;
            uv[i*2]=new Vector2(0,i); uv[i*2+1]=new Vector2(1,i);
            if(i==points.Count-1) continue;
            int n=i*6,v=i*2; tris[n]=v;tris[n+1]=v+2;tris[n+2]=v+1;tris[n+3]=v+1;tris[n+4]=v+2;tris[n+5]=v+3;
        }
        string path=Folder+"/"+name+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null) { mesh=new Mesh(); AssetDatabase.CreateAsset(mesh,path); } else mesh.Clear();
        mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=tris;mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
        go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=material;
        if(collision) go.AddComponent<MeshCollider>().sharedMesh=mesh;
        go.isStatic=true;
    }
}
