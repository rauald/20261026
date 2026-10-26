using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public static class SimplePlayerSceneBuilder
{
    const string ScenePath = "Assets/Scenes/PlayerDemo.unity";

    [InitializeOnLoadMethod]
    static void Schedule()
    {
        if (!File.Exists(ScenePath)) EditorApplication.delayCall += Build;
    }

    [MenuItem("Tools/Simple Player/Open Demo")]
    static void Open()
    {
        if (File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath);
        else Build();
    }

    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Ground";
        floor.transform.localScale = new Vector3(4, 1, 4);
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Obstacle";
        cube.transform.position = new Vector3(3, 0.75f, 3);
        cube.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0, 0.05f, 0);
        var controller = player.AddComponent<CharacterController>();
        controller.height = 2.25f;
        controller.radius = 0.32f;
        controller.center = new Vector3(0, 1.125f, 0);
        controller.stepOffset = 0.25f;
        controller.minMoveDistance = 0;
        var motor = player.AddComponent<SimplePlayerMotor>();
        var body = Material("Body", new Color(0.12f, 0.45f, 0.85f));
        var head = Material("Head", new Color(1f, 0.75f, 0.45f));
        var limbs = Material("Limbs", new Color(0.15f, 0.22f, 0.32f));
        Part(player.transform, "Body", PrimitiveType.Cube, new Vector3(0, 1.35f, 0), new Vector3(0.7f, 0.8f, 0.4f), body);
        Part(player.transform, "Head", PrimitiveType.Sphere, new Vector3(0, 2f, 0), Vector3.one * 0.5f, head);
        motor.leftArm = Limb(player.transform, "LeftArm", new Vector3(-0.48f, 1.65f, 0), new Vector3(0.22f, 0.75f, 0.25f), limbs);
        motor.rightArm = Limb(player.transform, "RightArm", new Vector3(0.48f, 1.65f, 0), new Vector3(0.22f, 0.75f, 0.25f), limbs);
        motor.leftLeg = Limb(player.transform, "LeftLeg", new Vector3(-0.21f, 0.95f, 0), new Vector3(0.28f, 0.9f, 0.3f), limbs);
        motor.rightLeg = Limb(player.transform, "RightLeg", new Vector3(0.21f, 0.95f, 0), new Vector3(0.28f, 0.9f, 0.3f), limbs);
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.nearClipPlane = 0.1f;
        cameraObject.AddComponent<AudioListener>();
        var follow = cameraObject.AddComponent<SmoothPlayerCamera>();
        follow.target = player.transform;
        cameraObject.transform.position = new Vector3(0, 4, -5.5f);
        cameraObject.transform.LookAt(player.transform.position + Vector3.up * 1.6f);
        var sun = new GameObject("Directional Light").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 2f;
        sun.transform.rotation = Quaternion.Euler(45, -30, 0);
        RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.5f);
        EditorSceneManager.SaveScene(scene, ScenePath);
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        foreach (var entry in EditorBuildSettings.scenes)
            if (entry.path != ScenePath) scenes.Add(entry);
        EditorBuildSettings.scenes = scenes.ToArray();
        File.WriteAllText("Assets/SimplePlayer/BuildResult.txt", "Scene generated successfully: " + ScenePath);
        AssetDatabase.Refresh();
        Debug.Log("SimplePlayer: PlayerDemo scene created successfully.");
    }

    static Material Material(string name, Color color)
    {
        string path = "Assets/SimplePlayer/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = color;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static Transform Limb(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        var pivot = new GameObject(name + "Pivot").transform;
        pivot.SetParent(parent, false);
        pivot.localPosition = position;
        Part(pivot, name, PrimitiveType.Cube, new Vector3(0, -scale.y / 2f, 0), scale, material);
        return pivot;
    }

    static void Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        var part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        Object.DestroyImmediate(part.GetComponent<Collider>());
        part.GetComponent<Renderer>().sharedMaterial = material;
    }
}

