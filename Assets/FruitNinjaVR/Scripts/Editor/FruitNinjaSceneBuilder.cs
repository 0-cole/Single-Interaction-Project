using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace FruitNinjaVR.Editor
{
    public static class FruitNinjaSceneBuilder
    {
        private const string SCENE_PATH = "Assets/Scenes/FruitSlicerVR.unity";
        private const string XR_ORIGIN_PREFAB_GUID = "f6336ac4ac8b4d34bc5072418cdc62a0";

        private const string REBUILD_FLAG = "Temp/rebuild_marker_v2.txt";

        [InitializeOnLoadMethod]
        private static void AutoBuildOnLoad()
        {
            if (!File.Exists(REBUILD_FLAG))
            {
                EditorApplication.delayCall += () =>
                {
                    if (!File.Exists(REBUILD_FLAG))
                    {
                        try
                        {
                            string dir = Path.GetDirectoryName(REBUILD_FLAG);
                            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                            File.WriteAllText(REBUILD_FLAG, "rebuilt");
                            BuildScene();
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"Error rebuilding scene: {ex}");
                        }
                    }
                };
            }
        }

        [MenuItem("Tools/Fruit Ninja VR/Build Fruit Slicer Scene")]
        public static void BuildScene()
        {
            // 1. Create or open scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. Setup Lighting & Atmosphere
            SetupAtmosphere();

            // 3. Materials
            Material floorMat = CreateMaterial("Mat_Dojo_Floor", new Color(0.32f, 0.22f, 0.16f), 0.45f, 0f);
            Material wallMat = CreateMaterial("Mat_Dojo_Wall", new Color(0.42f, 0.38f, 0.34f), 0.2f, 0f);
            Material pillarMat = CreateMaterial("Mat_Dojo_Pillar", new Color(0.22f, 0.15f, 0.11f), 0.3f, 0f);
            Material ceilingMat = CreateMaterial("Mat_Dojo_Ceiling", new Color(0.28f, 0.25f, 0.22f), 0.1f, 0f);
            Material standMat = CreateMaterial("Mat_Dojo_Stand", new Color(0.26f, 0.18f, 0.13f), 0.4f, 0f);
            Material bladeMat = CreateMaterial("Mat_Katana_Blade", new Color(0.92f, 0.94f, 0.96f), 0.92f, 0.95f);
            Material handleMat = CreateMaterial("Mat_Katana_Handle", new Color(0.12f, 0.03f, 0.04f), 0.3f, 0f);
            Material guardMat = CreateMaterial("Mat_Katana_Guard", new Color(0.85f, 0.65f, 0.25f), 0.7f, 0.85f);
            Material signMat = CreateMaterial("Mat_Scoreboard_Back", new Color(0.20f, 0.12f, 0.08f), 0.3f, 0f);

            // 4. Build Environment (Enclosed Room)
            GameObject envRoot = new GameObject("Environment");

            // Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.parent = envRoot.transform;
            floor.transform.position = new Vector3(0f, -0.05f, 0f);
            floor.transform.localScale = new Vector3(8f, 0.1f, 8f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;

            // Ceiling (Shadow casting disabled so sunlight gently passes through)
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.parent = envRoot.transform;
            ceiling.transform.position = new Vector3(0f, 4.55f, 0f);
            ceiling.transform.localScale = new Vector3(8f, 0.1f, 8f);
            var ceilingMr = ceiling.GetComponent<MeshRenderer>();
            ceilingMr.sharedMaterial = ceilingMat;
            ceilingMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // 4 Walls
            CreateWall(envRoot.transform, "Wall_Back", new Vector3(0f, 2.25f, -4f), new Vector3(8f, 4.5f, 0.2f), wallMat);
            CreateWall(envRoot.transform, "Wall_Front", new Vector3(0f, 2.25f, 4f), new Vector3(8f, 4.5f, 0.2f), wallMat);
            CreateWall(envRoot.transform, "Wall_Left", new Vector3(-4f, 2.25f, 0f), new Vector3(0.2f, 4.5f, 8f), wallMat);
            CreateWall(envRoot.transform, "Wall_Right", new Vector3(4f, 2.25f, 0f), new Vector3(0.2f, 4.5f, 8f), wallMat);

            // Architectural Pillars
            CreatePillar(envRoot.transform, "Pillar_FL", new Vector3(-3.85f, 2.25f, 3.85f), pillarMat);
            CreatePillar(envRoot.transform, "Pillar_FR", new Vector3(3.85f, 2.25f, 3.85f), pillarMat);
            CreatePillar(envRoot.transform, "Pillar_BL", new Vector3(-3.85f, 2.25f, -3.85f), pillarMat);
            CreatePillar(envRoot.transform, "Pillar_BR", new Vector3(3.85f, 2.25f, -3.85f), pillarMat);

            // Wall Trim Beams
            CreateTrim(envRoot.transform, "Trim_Front", new Vector3(0f, 3.2f, 3.85f), new Vector3(7.6f, 0.12f, 0.1f), pillarMat);
            CreateTrim(envRoot.transform, "Trim_Back", new Vector3(0f, 3.2f, -3.85f), new Vector3(7.6f, 0.12f, 0.1f), pillarMat);
            CreateTrim(envRoot.transform, "Trim_Left", new Vector3(-3.85f, 3.2f, 0f), new Vector3(0.1f, 0.12f, 7.6f), pillarMat);
            CreateTrim(envRoot.transform, "Trim_Right", new Vector3(3.85f, 3.2f, 0f), new Vector3(0.1f, 0.12f, 7.6f), pillarMat);

            // 5. Scoreboard & Instructions
            GameObject scoreboard = CreateScoreboard(envRoot.transform, signMat, pillarMat);

            // 6. Weapon Stand (Pedestal) & Katana
            GameObject standRoot = new GameObject("WeaponStand");
            standRoot.transform.parent = envRoot.transform;
            standRoot.transform.position = new Vector3(0f, 0f, 0.65f);

            // Stand Base
            GameObject standBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            standBase.name = "Stand_Base";
            standBase.transform.parent = standRoot.transform;
            standBase.transform.localPosition = new Vector3(0f, 0.38f, 0f);
            standBase.transform.localScale = new Vector3(0.7f, 0.76f, 0.3f);
            standBase.GetComponent<MeshRenderer>().sharedMaterial = standMat;

            // Stand Pegs
            GameObject pegL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pegL.name = "Peg_L";
            pegL.transform.parent = standRoot.transform;
            pegL.transform.localPosition = new Vector3(-0.2f, 0.81f, 0f);
            pegL.transform.localScale = new Vector3(0.04f, 0.12f, 0.12f);
            pegL.GetComponent<MeshRenderer>().sharedMaterial = standMat;

            GameObject pegR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pegR.name = "Peg_R";
            pegR.transform.parent = standRoot.transform;
            pegR.transform.localPosition = new Vector3(0.2f, 0.81f, 0f);
            pegR.transform.localScale = new Vector3(0.04f, 0.12f, 0.12f);
            pegR.GetComponent<MeshRenderer>().sharedMaterial = standMat;

            // Katana
            GameObject katana = CreateKatana(new Vector3(0f, 0.86f, 0.65f), bladeMat, handleMat, guardMat);

            // 7. Fruit Spawner
            GameObject spawnerGo = new GameObject("FruitSpawnerStation");
            spawnerGo.transform.position = new Vector3(0f, 0f, 2.3f);

            // Spawner Platform
            GameObject launcherBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            launcherBase.name = "LauncherBase";
            launcherBase.transform.parent = spawnerGo.transform;
            launcherBase.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            launcherBase.transform.localScale = new Vector3(0.9f, 0.08f, 0.9f);
            launcherBase.GetComponent<MeshRenderer>().sharedMaterial = standMat;
            Object.DestroyImmediate(launcherBase.GetComponent<Collider>());

            FruitSpawner spawner = spawnerGo.AddComponent<FruitSpawner>();

            GameObject spawnOrigin = new GameObject("SpawnOrigin");
            spawnOrigin.transform.parent = spawnerGo.transform;
            spawnOrigin.transform.localPosition = new Vector3(0f, 0.45f, 0f);

            GameObject targetArea = new GameObject("TargetStrikeZone");
            targetArea.transform.parent = spawnerGo.transform;
            targetArea.transform.position = new Vector3(0f, 2.2f, 1.05f);

            // Wire spawner serialized fields
            var spawnerSo = new SerializedObject(spawner);
            spawnerSo.FindProperty("m_SpawnOrigin").objectReferenceValue = spawnOrigin.transform;
            spawnerSo.FindProperty("m_TargetArea").objectReferenceValue = targetArea.transform;
            spawnerSo.ApplyModifiedPropertiesWithoutUndo();

            // 8. XR Interaction Manager & EventSystem
            GameObject xriManagerGo = new GameObject("XR Interaction Manager");
            xriManagerGo.AddComponent<XRInteractionManager>();

            GameObject eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // 9. XR Origin Rig Prefab
            string xrOriginPath = AssetDatabase.GUIDToAssetPath(XR_ORIGIN_PREFAB_GUID);
            if (!string.IsNullOrEmpty(xrOriginPath))
            {
                GameObject xrOriginPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(xrOriginPath);
                if (xrOriginPrefab != null)
                {
                    GameObject xrOriginInstance = (GameObject)PrefabUtility.InstantiatePrefab(xrOriginPrefab);
                    xrOriginInstance.name = "XR Origin (XR Rig)";
                    xrOriginInstance.transform.position = Vector3.zero;
                    xrOriginInstance.transform.rotation = Quaternion.identity;
                }
            }

            // 10. Save Scene
            string sceneDir = Path.GetDirectoryName(SCENE_PATH);
            if (!Directory.Exists(sceneDir)) Directory.CreateDirectory(sceneDir);

            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 11. Add to Build Settings
            AddSceneToBuildSettings(SCENE_PATH);

            Debug.Log($"<color=green>Successfully created Fruit Slicer VR Scene at {SCENE_PATH}!</color>");
        }

        private static void SetupAtmosphere()
        {
            // Warm ambient illumination
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientSkyColor = new Color(0.38f, 0.35f, 0.40f);

            // Directional Light
            GameObject dirLightGo = new GameObject("Directional Light");
            Light dirLight = dirLightGo.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(1.0f, 0.94f, 0.86f);
            dirLight.intensity = 0.95f;
            dirLight.shadows = LightShadows.Soft;
            dirLightGo.transform.rotation = Quaternion.Euler(45f, 25f, 0f);

            // Spotlight 1: Overhead Arena Light focused on fruit strike zone
            GameObject spot1Go = new GameObject("Spotlight_Arena");
            Light spot1 = spot1Go.AddComponent<Light>();
            spot1.type = LightType.Spot;
            spot1.color = new Color(1.0f, 0.97f, 0.9f);
            spot1.intensity = 26f;
            spot1.range = 10f;
            spot1.spotAngle = 75f;
            spot1.innerSpotAngle = 45f;
            spot1.shadows = LightShadows.Soft;
            spot1Go.transform.position = new Vector3(0f, 4.3f, 1.1f);
            spot1Go.transform.rotation = Quaternion.Euler(75f, 0f, 0f);

            // Spotlight 2: Pedestal illumination
            GameObject spot2Go = new GameObject("Spotlight_Pedestal");
            Light spot2 = spot2Go.AddComponent<Light>();
            spot2.type = LightType.Spot;
            spot2.color = new Color(1.0f, 0.90f, 0.75f);
            spot2.intensity = 18f;
            spot2.range = 7f;
            spot2.spotAngle = 60f;
            spot2.innerSpotAngle = 35f;
            spot2.shadows = LightShadows.Soft;
            spot2Go.transform.position = new Vector3(0f, 4.3f, 0.65f);
            spot2Go.transform.rotation = Quaternion.Euler(85f, 0f, 0f);

            // 4 Corner Lantern Lights (Warm atmospheric orange glow)
            CreateLantern(new Vector3(-3.5f, 2.2f, 3.5f), "Lantern_FL");
            CreateLantern(new Vector3(3.5f, 2.2f, 3.5f), "Lantern_FR");
            CreateLantern(new Vector3(-3.5f, 2.2f, -3.5f), "Lantern_BL");
            CreateLantern(new Vector3(3.5f, 2.2f, -3.5f), "Lantern_BR");
        }

        private static void CreateLantern(Vector3 pos, string name)
        {
            GameObject lanternGo = new GameObject(name);
            lanternGo.transform.position = pos;

            Light pLight = lanternGo.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(1.0f, 0.65f, 0.25f);
            pLight.intensity = 8.5f;
            pLight.range = 8.5f;
            pLight.shadows = LightShadows.None;

            // Small glowing sphere visual
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "LanternMesh";
            visual.transform.parent = lanternGo.transform;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one * 0.16f;

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit == null) unlit = Shader.Find("Unlit/Color");
            Material mat = new Material(unlit);
            mat.color = new Color(1.0f, 0.75f, 0.35f);
            visual.GetComponent<MeshRenderer>().sharedMaterial = mat;

            Object.DestroyImmediate(visual.GetComponent<Collider>());
        }

        private static void CreateWall(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.parent = parent;
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void CreatePillar(Transform parent, string name, Vector3 pos, Material mat)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = name;
            pillar.transform.parent = parent;
            pillar.transform.position = pos;
            pillar.transform.localScale = new Vector3(0.32f, 4.5f, 0.32f);
            pillar.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void CreateTrim(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = name;
            trim.transform.parent = parent;
            trim.transform.position = pos;
            trim.transform.localScale = scale;
            trim.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Object.DestroyImmediate(trim.GetComponent<Collider>());
        }

        private static GameObject CreateKatana(Vector3 spawnPos, Material bladeMat, Material handleMat, Material guardMat)
        {
            GameObject katana = new GameObject("Katana_Sword");
            katana.transform.position = spawnPos;
            katana.transform.rotation = Quaternion.identity;

            Rigidbody rb = katana.AddComponent<Rigidbody>();
            rb.mass = 0.9f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            XRGrabInteractable grab = katana.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grab.throwOnDetach = false;

            // Handle
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.name = "Handle";
            handle.transform.parent = katana.transform;
            handle.transform.localPosition = new Vector3(-0.25f, 0f, 0f);
            handle.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            handle.transform.localScale = new Vector3(0.035f, 0.12f, 0.035f);
            handle.GetComponent<MeshRenderer>().sharedMaterial = handleMat;

            // Guard (Tsuba)
            GameObject guard = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            guard.name = "Guard";
            guard.transform.parent = katana.transform;
            guard.transform.localPosition = new Vector3(-0.13f, 0f, 0f);
            guard.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            guard.transform.localScale = new Vector3(0.09f, 0.012f, 0.075f);
            guard.GetComponent<MeshRenderer>().sharedMaterial = guardMat;

            // Blade
            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Blade";
            blade.transform.parent = katana.transform;
            blade.transform.localPosition = new Vector3(0.24f, 0f, 0f);
            blade.transform.localScale = new Vector3(0.72f, 0.045f, 0.012f);
            blade.GetComponent<MeshRenderer>().sharedMaterial = bladeMat;

            // Blade Collider set as trigger for slicing (generous volume prevents tunneling)
            BoxCollider bladeCol = blade.GetComponent<BoxCollider>();
            bladeCol.isTrigger = true;
            bladeCol.size = new Vector3(1.1f, 1.8f, 5.0f);

            // Tip and Base transforms
            GameObject tipPoint = new GameObject("BladeTip");
            tipPoint.transform.parent = katana.transform;
            tipPoint.transform.localPosition = new Vector3(0.60f, 0f, 0f);

            GameObject basePoint = new GameObject("BladeBase");
            basePoint.transform.parent = katana.transform;
            basePoint.transform.localPosition = new Vector3(-0.12f, 0f, 0f);

            // Trail Renderer
            TrailRenderer trail = blade.AddComponent<TrailRenderer>();
            trail.time = 0.22f;
            trail.startWidth = 0.08f;
            trail.endWidth = 0.005f;

            Shader trailShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (trailShader == null) trailShader = Shader.Find("Particles/Standard Unlit");
            if (trailShader == null) trailShader = Shader.Find("Unlit/Color");
            Material trailMat = new Material(trailShader);
            trailMat.color = new Color(0.4f, 0.9f, 1.0f, 0.8f);
            trail.material = trailMat;

            // SwordBlade component
            SwordBlade swordBlade = blade.AddComponent<SwordBlade>();
            var bladeSo = new SerializedObject(swordBlade);
            bladeSo.FindProperty("m_TipTransform").objectReferenceValue = tipPoint.transform;
            bladeSo.FindProperty("m_BaseTransform").objectReferenceValue = basePoint.transform;
            bladeSo.FindProperty("m_TrailRenderer").objectReferenceValue = trail;
            bladeSo.FindProperty("m_SpawnPosition").vector3Value = spawnPos;
            bladeSo.FindProperty("m_SpawnRotation").quaternionValue = Quaternion.identity;
            bladeSo.ApplyModifiedPropertiesWithoutUndo();

            // Setup grab attach transform
            grab.attachTransform = handle.transform;

            return katana;
        }

        private static GameObject CreateScoreboard(Transform parent, Material signMat, Material frameMat)
        {
            GameObject boardRoot = new GameObject("Scoreboard");
            boardRoot.transform.parent = parent;
            boardRoot.transform.position = new Vector3(0f, 2.3f, 3.85f);
            boardRoot.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Face inward

            // Frame / Backing
            GameObject backPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backPlate.name = "BackPlate";
            backPlate.transform.parent = boardRoot.transform;
            backPlate.transform.localPosition = Vector3.zero;
            backPlate.transform.localScale = new Vector3(2.4f, 1.35f, 0.06f);
            backPlate.GetComponent<MeshRenderer>().sharedMaterial = signMat;

            // Border Trim
            GameObject topTrim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topTrim.name = "TopTrim";
            topTrim.transform.parent = boardRoot.transform;
            topTrim.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            topTrim.transform.localScale = new Vector3(2.55f, 0.08f, 0.09f);
            topTrim.GetComponent<MeshRenderer>().sharedMaterial = frameMat;

            // Text 1: Header
            GameObject headerGo = new GameObject("HeaderTitle");
            headerGo.transform.parent = boardRoot.transform;
            headerGo.transform.localPosition = new Vector3(0f, 0.42f, -0.04f);
            TextMesh headerTm = headerGo.AddComponent<TextMesh>();
            headerTm.text = "FRUIT SLICER VR";
            headerTm.fontSize = 32;
            headerTm.characterSize = 0.042f;
            headerTm.anchor = TextAnchor.MiddleCenter;
            headerTm.alignment = TextAlignment.Center;
            headerTm.color = new Color(1.0f, 0.85f, 0.2f); // Gold

            // Text 2: Score Display
            GameObject scoreGo = new GameObject("ScoreDisplay");
            scoreGo.transform.parent = boardRoot.transform;
            scoreGo.transform.localPosition = new Vector3(0f, 0.05f, -0.04f);
            TextMesh scoreTm = scoreGo.AddComponent<TextMesh>();
            scoreTm.text = "SCORE: 0\nSLICES: 0";
            scoreTm.fontSize = 28;
            scoreTm.characterSize = 0.038f;
            scoreTm.anchor = TextAnchor.MiddleCenter;
            scoreTm.alignment = TextAlignment.Center;
            scoreTm.color = Color.white;

            // Text 3: Combo Banner
            GameObject comboGo = new GameObject("ComboBanner");
            comboGo.transform.parent = boardRoot.transform;
            comboGo.transform.localPosition = new Vector3(0f, -0.38f, -0.04f);
            TextMesh comboTm = comboGo.AddComponent<TextMesh>();
            comboTm.text = "GRAB KATANA TO BEGIN!";
            comboTm.fontSize = 22;
            comboTm.characterSize = 0.032f;
            comboTm.anchor = TextAnchor.MiddleCenter;
            comboTm.alignment = TextAlignment.Center;
            comboTm.color = new Color(0.85f, 0.85f, 0.85f);

            // ScoreManager Component
            ScoreManager sm = boardRoot.AddComponent<ScoreManager>();
            var smSo = new SerializedObject(sm);
            smSo.FindProperty("m_ScoreTextMesh").objectReferenceValue = scoreTm;
            smSo.FindProperty("m_ComboTextMesh").objectReferenceValue = comboTm;
            smSo.ApplyModifiedPropertiesWithoutUndo();

            return boardRoot;
        }

        private static Material CreateMaterial(string name, Color color, float smoothness, float metallic)
        {
            string matDir = "Assets/FruitNinjaVR/Materials";
            if (!Directory.Exists(matDir)) Directory.CreateDirectory(matDir);

            string path = $"{matDir}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");

                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.color = color;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
            {
                if (s.path == scenePath) return;
            }

            var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            for (int i = 0; i < scenes.Length; i++)
            {
                newScenes[i] = scenes[i];
            }
            newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = newScenes;
        }
    }
}
