using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class EasyRoadsMapBuilder
{
    private const string SourceScenePath = "Assets/EasyRoads3D scenes/test scene.unity";
    private const string BuiltScenePath = "Assets/Scenes/EasyRoadsMap.unity";
    private const string AwakeScenePath = "Assets/Scenes/awakeScene.unity";
    private const string VehicleListPath = "Assets/vehicles/vehicleList.prefab";

    [MenuItem("Racing/Build EasyRoads Map")]
    public static void Build()
    {
        BuildTrack(0);
        AddSelectorButton();
        EditorSceneManager.OpenScene(AwakeScenePath, OpenSceneMode.Single);
    }

    [MenuItem("Racing/Build All Tracks")]
    public static void BuildAll()
    {
        for(int i=0;i<4;i++) BuildTrack(i);
        EditorBuildSettings.scenes=RelaySettings.BuildScenes.Select(p=>new EditorBuildSettingsScene(p,true)).ToArray();
        EditorSceneManager.OpenScene(AwakeScenePath,OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
    }

    static void BuildTrack(int theme)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/"+RelaySettings.Scenes[theme]+".unity");
        var markers = RelayTrackGeometry.Build(theme);
        if (markers.Count < 4)
        {
            throw new System.InvalidOperationException("No sampled EasyRoads spline found. Refusing to create a route outside the road.");
        }

        var waypointRoot = BuildWaypoints(markers);
        var start = BuildStartPosition(markers);
        BuildFinishLine(markers);
        BuildCamera(markers);
        BuildHud(start);
        BuildAiCars(markers, waypointRoot);
        BuildRoadFurniture(markers);
        EnsureEventSystem();
        EnsureBuildSettings();

        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Built playable EasyRoadsMap scene and added it to the map selector.");
    }

    [MenuItem("Racing/Add EasyRoads Selector Button")]
    public static void AddSelectorButtonMenu()
    {
        AddSelectorButton();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Added EasyRoadsMap button to awakeScene.");
    }

    private static void RemoveGeneratedObjects()
    {
        foreach (var name in new[]
        {
            "EasyRoads Race Waypoints",
            "startPosition",
            "finishLineTrigger",
            "EasyRoads Race Canvas",
            "MANAGER",
            "EasyRoads AI 1",
            "EasyRoads AI 2"
        })
        {
            var go = GameObject.Find(name);
            if (go != null)
            {
                Object.DestroyImmediate(go);
            }
        }
    }

    private static List<Vector3> FindBestRoadMarkers()
    {
        var best = new List<Vector3>();
        foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (behaviour == null)
            {
                continue;
            }

            var serialized = new SerializedObject(behaviour);
            var markerArray = serialized.FindProperty("markersExt");
            if (markerArray == null || !markerArray.isArray || markerArray.arraySize <= best.Count)
            {
                continue;
            }

            var positions = new List<Vector3>();
            for (var i = 0; i < markerArray.arraySize; i++)
            {
                var marker = markerArray.GetArrayElementAtIndex(i).objectReferenceValue;
                if (marker == null)
                {
                    continue;
                }

                var markerSerialized = new SerializedObject(marker);
                var position = markerSerialized.FindProperty("position");
                if (position != null && position.propertyType == SerializedPropertyType.Vector3)
                {
                    positions.Add(position.vector3Value);
                }
            }

            if (positions.Count > best.Count)
            {
                best = positions;
            }
        }

        return NormalizePath(best);
    }

    private static List<Vector3> FindRoadSamples()
    {
        var best = new List<Vector3>();
        foreach(var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if(behaviour == null) continue;
            var samples = new SerializedObject(behaviour).FindProperty("splinePoints");
            if(samples == null || !samples.isArray || samples.arraySize < 20) continue;
            var points = new List<Vector3>();
            float length = 0;
            for(int i=0;i<samples.arraySize;i++)
            {
                var sample = samples.GetArrayElementAtIndex(i);
                if(sample.propertyType != SerializedPropertyType.Vector3) continue;
                var point = sample.vector3Value;
                if(points.Count > 0 && Vector3.Distance(points[points.Count-1],point)<3.5f) continue;
                if(points.Count>0) length += Vector3.Distance(points[points.Count-1],point);
                points.Add(point);
                if(length > 1000) break;
            }
            if(points.Count>best.Count) best = points;
        }
        // Leave an approach behind the grid and a run-off beyond the finish.
        if(best.Count > 20) best = best.Skip(6).Take(best.Count-12).ToList();
        Debug.Log("ROAD_RELAY_ROUTE samples=" + best.Count + " start=" + best.FirstOrDefault() + " end=" + best.LastOrDefault());
        return best;
    }

    private static void BuildRoadFurniture(List<Vector3> markers)
    {
        var root = new GameObject("Road Relay Course");
        var cyan = new Material(Shader.Find("Standard")) { color = new Color(.08f,.75f,.75f) };
        var dark = new Material(Shader.Find("Standard")) { color = new Color(.05f,.08f,.12f) };
        Directory.CreateDirectory("Assets/RoadRelay");
        var savedCyan = AssetDatabase.LoadAssetAtPath<Material>("Assets/RoadRelay/CourseCyan.mat");
        var savedDark = AssetDatabase.LoadAssetAtPath<Material>("Assets/RoadRelay/CourseDark.mat");
        if(savedCyan == null) AssetDatabase.CreateAsset(cyan,"Assets/RoadRelay/CourseCyan.mat"); else { Object.DestroyImmediate(cyan); cyan=savedCyan; }
        if(savedDark == null) AssetDatabase.CreateAsset(dark,"Assets/RoadRelay/CourseDark.mat"); else { Object.DestroyImmediate(dark); dark=savedDark; }
        for(int i=0;i<markers.Count;i+=4)
        {
            var direction = FlatDirection(markers[Mathf.Max(0,i-1)],markers[Mathf.Min(i+1,markers.Count-1)]);
            var right = Vector3.Cross(Vector3.up,direction);
            foreach(int side in new[]{-1,1})
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = "Route reflector";
                post.transform.SetParent(root.transform);
                post.transform.position = markers[i]+right*7.9f*side+Vector3.up*1.3f;
                post.transform.rotation = Quaternion.LookRotation(direction);
                post.transform.localScale = new Vector3(.16f,1.4f,.3f);
                Object.DestroyImmediate(post.GetComponent<Collider>());
                post.GetComponent<Renderer>().sharedMaterial = cyan;
            }
        }
        for(int end=0;end<2;end++)
        {
            int i=end==0?0:markers.Count-1;
            var frame = new GameObject(end==0?"Start arch":"Finish arch");
            frame.transform.SetParent(root.transform);
            frame.transform.position=LiftToRoad(markers[i]);
            frame.transform.rotation=Quaternion.LookRotation(FlatDirection(markers[Mathf.Max(0,i-1)],markers[Mathf.Min(i+1,markers.Count-1)]));
            for(int part=0;part<3;part++)
            {
                var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(frame.transform,false);
                cube.transform.localPosition=part<2?new Vector3(part==0?-7:7,3.5f,0):new Vector3(0,7,0);
                cube.transform.localScale=part<2?new Vector3(.35f,7,.35f):new Vector3(14.5f,1,.35f);
                cube.GetComponent<Renderer>().sharedMaterial=dark;
            }
            var label=new GameObject("Banner").AddComponent<TextMesh>();
            label.transform.SetParent(frame.transform,false);
            label.transform.localPosition=new Vector3(0,7,-.25f);
            label.transform.localRotation=Quaternion.identity;
            label.text=end==0?"ROAD RELAY":"FINISH";
            label.anchor=TextAnchor.MiddleCenter;
            label.characterSize=.22f;
            label.fontSize=60;
            label.color=Color.white;
        }
    }

    private static List<Vector3> FindRoadMeshFallbackPath()
    {
        var road = GameObject.Find("road_0001") ?? GameObject.Find("Default Road 1 002");
        var renderer = road != null ? road.GetComponentInChildren<MeshRenderer>() : Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).FirstOrDefault();
        var bounds = renderer != null ? renderer.bounds : new Bounds(new Vector3(512f, 5f, 512f), new Vector3(300f, 1f, 300f));
        var center = bounds.center;
        var radiusX = Mathf.Max(80f, bounds.extents.x * 0.35f);
        var radiusZ = Mathf.Max(80f, bounds.extents.z * 0.35f);
        var path = new List<Vector3>();
        for (var i = 0; i < 28; i++)
        {
            var angle = (Mathf.PI * 2f * i) / 28f;
            path.Add(new Vector3(center.x + Mathf.Cos(angle) * radiusX, center.y + 2f, center.z + Mathf.Sin(angle) * radiusZ));
        }

        return path;
    }

    private static List<Vector3> NormalizePath(List<Vector3> markers)
    {
        return markers
            .Where(p => p.sqrMagnitude > 1f)
            .GroupBy(p => new Vector2(Mathf.Round(p.x * 10f), Mathf.Round(p.z * 10f)))
            .Select(g => g.First())
            .ToList();
    }

    private static trackWaypoints BuildWaypoints(List<Vector3> markers)
    {
        var root = new GameObject("EasyRoads Race Waypoints");
        root.tag = "path";
        var path = root.AddComponent<trackWaypoints>();
        path.linecolor = Color.yellow;
        path.SphereRadius = 1f;

        for (var i = 0; i < markers.Count; i++)
        {
            var point = new GameObject("Waypoint " + (i + 1).ToString("00"));
            point.transform.SetParent(root.transform);
            point.transform.position = LiftToRoad(markers[i]) + Vector3.up * 0.5f;
            path.nodes.Add(point.transform);
        }

        return path;
    }

    private static GameObject BuildStartPosition(List<Vector3> markers)
    {
        var start = new GameObject("startPosition");
        var startPoint = LiftToRoad(markers[0]) + Vector3.up * 1.1f;
        var nextPoint = LiftToRoad(markers[1]);
        start.transform.position = startPoint;
        start.transform.rotation = Quaternion.LookRotation(FlatDirection(startPoint, nextPoint), Vector3.up);
        return start;
    }

    private static void BuildFinishLine(List<Vector3> markers)
    {
        var finish = new GameObject("finishLineTrigger");
        finish.tag = "Finish";
        var finishIndex = Mathf.Max(1, markers.Count - 2);
        var position = LiftToRoad(markers[finishIndex]) + Vector3.up * 2f;
        var direction = FlatDirection(markers[finishIndex - 1], markers[finishIndex]);
        finish.transform.position = position;
        finish.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        var box = finish.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(18f, 8f, 2f);
    }

    private static void BuildCamera(List<Vector3> markers)
    {
        var existing = Camera.main;
        var cameraObject = existing != null ? existing.gameObject : new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.GetComponent<Camera>();
        if(camera == null) camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 65f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.62f,.77f,.83f);
        camera.farClipPlane = 1800;
        if (cameraObject.GetComponent<cameraController>() == null)
        {
            cameraObject.AddComponent<cameraController>();
        }
        var start = LiftToRoad(markers[0]);
        var direction = FlatDirection(markers[0], markers[1]);
        cameraObject.transform.position = start - direction * 18f + Vector3.up * 7f;
        cameraObject.transform.LookAt(start + Vector3.up * 2f);
        if (cameraObject.GetComponent<AudioListener>() == null)
        {
            cameraObject.AddComponent<AudioListener>();
        }
    }

    private static void BuildHud(GameObject startPosition)
    {
        var canvas = new GameObject("EasyRoads Race Canvas");
        var canvasComponent = canvas.AddComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvas.AddComponent<GraphicRaycaster>();

        var speed = MakeText(canvas.transform, "SpeedText", "0", new Vector2(150f, -75f), 42, TextAnchor.MiddleLeft);
        var gear = MakeText(canvas.transform, "GearText", "1", new Vector2(150f, -135f), 34, TextAnchor.MiddleLeft);
        var position = MakeText(canvas.transform, "PositionText", "1/1", new Vector2(-150f, -75f), 34, TextAnchor.MiddleRight);
        MakeText(canvas.transform, "RoadPowerText", "ROAD BUILD B  3/3", new Vector2(150f, -200f), 28, TextAnchor.MiddleLeft);
        var countdown = MakeText(canvas.transform, "CountdownText", "3", Vector2.zero, 86, TextAnchor.MiddleCenter);
        var needle = new GameObject("Needle");
        needle.transform.SetParent(canvas.transform, false);

        var nitro = MakeSlider(canvas.transform, "NitroSlider", new Vector2(0f, 65f));
        var finishPanel = MakePanel(canvas.transform, "FinishPanel", new Vector2(0f, -150f), new Vector2(420f, 360f));
        finishPanel.SetActive(false);
        var listFolder = new GameObject("FinishList");
        listFolder.transform.SetParent(finishPanel.transform, false);
        var listRect = listFolder.AddComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0f, 0f);
        listRect.anchorMax = new Vector2(1f, 1f);
        listRect.offsetMin = new Vector2(0f, 0f);
        listRect.offsetMax = new Vector2(0f, 0f);
        var row = MakeFinishRow(canvas.transform);
        row.SetActive(false);
        var backImage = MakePanel(canvas.transform, "FinishBackdrop", Vector2.zero, new Vector2(1920f, 1080f));
        backImage.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);
        backImage.transform.SetAsFirstSibling();
        backImage.SetActive(false);

        var managerObject = new GameObject("MANAGER");
        managerObject.tag = "gameManager";
        managerObject.AddComponent<EasyRoadsRaceSetup>();
        var manager = managerObject.AddComponent<GameManager>();
        manager.list = AssetDatabase.LoadAssetAtPath<GameObject>(VehicleListPath).GetComponent<vehicleList>();
        manager.startPosition = startPosition;
        manager.kph = speed;
        manager.currentPosition = position;
        manager.gearNum = gear;
        manager.timeLeftText = countdown;
        manager.neeedle = needle;
        manager.nitrusSlider = nitro;
        manager.uiList = row;
        manager.uiListFolder = listFolder;
        manager.backImage = backImage;
    }

    private static void BuildAiCars(List<Vector3> markers, trackWaypoints waypoints)
    {
        var listObject = AssetDatabase.LoadAssetAtPath<GameObject>(VehicleListPath);
        var list = listObject != null ? listObject.GetComponent<vehicleList>() : null;
        if (list == null || list.vehicles == null || list.vehicles.Length == 0)
        {
            return;
        }

        for (var i = 0; i < Mathf.Min(2, list.vehicles.Length); i++)
        {
            var markerIndex = 3 + i * 2;
            var direction = FlatDirection(markers[markerIndex], markers[markerIndex + 1]);
            var position = LiftToRoad(markers[markerIndex]) + Vector3.Cross(Vector3.up,direction)*(i==0?-2.2f:2.2f) + Vector3.up * .8f;
            var rotation = Quaternion.LookRotation(FlatDirection(markers[markerIndex], markers[markerIndex + 1]), Vector3.up);
            var car = (GameObject)PrefabUtility.InstantiatePrefab(list.vehicles[(i + 1) % list.vehicles.Length]);
            car.name = "EasyRoads AI " + (i + 1);
            car.tag = "AI";
            car.transform.position = position;
            car.transform.rotation = rotation;
            foreach (var input in car.GetComponents<inputManager>())
            {
                input.acceleration = 0.45f + i * 0.1f;
            }
            foreach (var ai in car.GetComponents<AIcontroller>())
            {
                ai.waypoints = waypoints;
                ai.nodes = waypoints.nodes;
                ai.currentWaypoint = waypoints.nodes.Count > 0 ? waypoints.nodes[0] : car.transform;
                ai.acceleration = 0.45f + i * 0.1f;
            }
        }
    }

    private static Text MakeText(Transform parent, string name, string value, Vector2 anchoredPosition, int size, TextAnchor alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(320f, 80f);
        rect.anchorMin = rect.anchorMax = AnchorFromPosition(anchoredPosition);
        rect.anchoredPosition = anchoredPosition;
        var text = go.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = alignment;
        return text;
    }

    private static GameObject MakePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        var image = go.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.65f);
        return go;
    }

    private static Slider MakeSlider(Transform parent, string name, Vector2 anchoredPosition)
    {
        var root = MakePanel(parent, name, anchoredPosition, new Vector2(320f, 28f));
        var slider = root.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        var fill = MakePanel(root.transform, "Fill", Vector2.zero, new Vector2(300f, 20f));
        fill.GetComponent<Image>().color = new Color(0.1f, 0.7f, 1f, 0.85f);
        slider.fillRect = fill.GetComponent<RectTransform>();
        return slider;
    }

    private static GameObject MakeFinishRow(Transform parent)
    {
        var row = MakePanel(parent, "FinishRowTemplate", Vector2.zero, new Vector2(360f, 44f));
        MakeText(row.transform, "vehicle name", "", new Vector2(-60f, 0f), 24, TextAnchor.MiddleLeft);
        MakeText(row.transform, "vehicle node", "", new Vector2(105f, 0f), 22, TextAnchor.MiddleRight);
        return row;
    }

    private static Vector2 AnchorFromPosition(Vector2 position)
    {
        if (position.x < -1f && position.y < -1f)
        {
            return new Vector2(1f, 1f);
        }

        if (position.x > 1f && position.y < -1f)
        {
            return new Vector2(0f, 1f);
        }

        return new Vector2(0.5f, 0.5f);
    }

    private static Vector3 LiftToRoad(Vector3 point)
    {
        var ray = new Ray(point + Vector3.up * 80f, Vector3.down);
        if (Physics.Raycast(ray, out var hit, 200f, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }

        return point;
    }

    private static Vector3 FlatDirection(Vector3 from, Vector3 to)
    {
        var direction = to - from;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.01f ? direction.normalized : Vector3.forward;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static void AddSelectorButton()
    {
        EditorSceneManager.OpenScene(AwakeScenePath, OpenSceneMode.Single);
        var manager = Object.FindFirstObjectByType<awakeManager>();
        if (manager == null || manager.mapSelectorCanvas == null)
        {
            throw new MissingReferenceException("awakeScene is missing awakeManager or mapSelectorCanvas.");
        }

        foreach (var existing in Resources.FindObjectsOfTypeAll<GameObject>().Where(go => go.name == "EasyRoadsMapButton").ToArray())
        {
            Object.DestroyImmediate(existing);
        }

        var button = MakeMenuButton(manager.mapSelectorCanvas.transform, "EasyRoadsMapButton", "EASY ROADS 3D", new Vector2(0f, -155f));
        UnityEventTools.AddPersistentListener(button.onClick, manager.loadEasyRoadsMap);
        var activeScene = EditorSceneManager.GetActiveScene();
        if (!EditorSceneManager.SaveScene(activeScene))
        {
            throw new IOException("Could not save " + activeScene.path);
        }
    }

    private static Button MakeMenuButton(Transform parent, string name, string label, Vector2 anchoredPosition)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(360f, 72f);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        var image = go.AddComponent<Image>();
        image.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
        var button = go.AddComponent<Button>();

        var text = MakeText(go.transform, "Text", label, Vector2.zero, 30, TextAnchor.MiddleCenter);
        text.color = Color.white;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    private static void EnsureBuildSettings()
    {
        var paths = new[] { AwakeScenePath, "Assets/Scenes/superMarioMap.unity", "Assets/Scenes/ComunityMap.unity", BuiltScenePath };
        EditorBuildSettings.scenes = paths
            .Where(File.Exists)
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();
    }
}
