// LevelBuilder.cs
// Editor script: Tools/FunRace/Build Level 1|2|3|All
// Generates each level scene procedurally from data tables and saves under Assets/Scenes/.
// Also provides: Update Build Settings, Validate Levels.
//
// NOTE: This script lives in Assets/Editor/ and is excluded from build.
// Run menu items in the Unity Editor after opening the project.

#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using TMPro;

namespace FunRaceParkour.Editor
{
    public static class LevelBuilder
    {
        // ── Scene names ────────────────────────────────────────────────────
        private const string SCENE_MENU  = "MainMenu";
        private const string SCENE_L1    = "Level1_TrainingRun";
        private const string SCENE_L2    = "Level2_MechanicalMayhem";
        private const string SCENE_L3    = "Level3_UltimateGauntlet";
        private const string SCENES_PATH = "Assets/Scenes/";

        // ── Menu items ─────────────────────────────────────────────────────
        [MenuItem("Tools/FunRace/Build Level 1 (Training Run)")]
        public static void BuildLevel1() => BuildLevel(0);

        [MenuItem("Tools/FunRace/Build Level 2 (Mechanical Mayhem)")]
        public static void BuildLevel2() => BuildLevel(1);

        [MenuItem("Tools/FunRace/Build Level 3 (Ultimate Gauntlet)")]
        public static void BuildLevel3() => BuildLevel(2);

        [MenuItem("Tools/FunRace/Build All Levels")]
        public static void BuildAll()
        {
            BuildAIRacerPrefab();
            BuildMainMenuScene();
            BuildLevel(0);
            BuildLevel(1);
            BuildLevel(2);
            UpdateBuildSettings();
            Debug.Log("[LevelBuilder] All levels built.");
        }

        [MenuItem("Tools/FunRace/Build AI Racer Prefab")]
        public static void BuildAIRacerPrefab()
        {
            const string output = "Assets/Prefabs/AIRacer.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(output) != null) return;

            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerRobot.prefab");
            if (source == null)
            {
                Debug.LogError("[LevelBuilder] PlayerRobot.prefab not found; cannot create AIRacer variant.");
                return;
            }

            var instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            instance.name = "AIRacer";
            RemoveComponent<StarterAssets.ThirdPersonController>(instance);
            RemoveComponent<StarterAssets.StarterAssetsInputs>(instance);
#if ENABLE_INPUT_SYSTEM
            RemoveComponent<UnityEngine.InputSystem.PlayerInput>(instance);
#endif
            RemoveComponent<FunRaceParkour.PlayerRacerInput>(instance);
            RemoveComponent<FunRaceParkour.RacerRespawnMonitor>(instance);
            RemoveComponent<FunRaceParkour.ThirdPersonControllerExtended>(instance);
            RemoveComponent<FunRaceParkour.AIRacerInput>(instance);
            RemoveComponent<StarterAssets.RespawnPlayer>(instance);
            instance.tag = "Untagged";

            instance.AddComponent<FunRaceParkour.ThirdPersonControllerExtended>();
            var ai = instance.AddComponent<FunRaceParkour.AIRacerInput>();
            ai.SetProfile(AssetDatabase.LoadAssetAtPath<FunRaceParkour.AIRacerProfile>(
                "Assets/ScriptableObjects/AI/AIRacer_Aggressive.asset"));
            instance.AddComponent<FunRaceParkour.PlatformRider>();
            instance.AddComponent<FunRaceParkour.PlatformContactReporter>();
            instance.AddComponent<FunRaceParkour.RacerRespawnMonitor>();

            PrefabUtility.SaveAsPrefabAssetAndConnect(instance, output, InteractionMode.AutomatedAction);
            Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LevelBuilder] Created AI prefab variant at {output}.");
        }

        private static void RemoveComponent<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component != null) Object.DestroyImmediate(component, true);
        }

        [MenuItem("Tools/FunRace/Build Main Menu Scene")]
        public static void BuildMainMenuScene()
        {
            string path = EnsureSceneFolder() + SCENE_MENU + ".unity";
            var scene   = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Minimal: a camera, EventSystem, and MainMenuController placeholder
            CreateEventSystem();

            var menuGO = new GameObject("MainMenu");
            var menuController = menuGO.AddComponent<FunRaceParkour.MainMenuController>();
            ConfigureUiFont(menuController, "uiFont");
            AddConfiguredAudioManager("MenuAudioManager");
            // Add persistent services
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[LevelBuilder] Main menu saved to {path}");
            UpdateBuildSettings();
        }

        [MenuItem("Tools/FunRace/Update Build Settings")]
        public static void UpdateBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            string[] sceneNames = { SCENE_MENU, SCENE_L1, SCENE_L2, SCENE_L3 };
            string folder = EnsureSceneFolder();

            foreach (var name in sceneNames)
            {
                string path = folder + name + ".unity";
                if (File.Exists(path))
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                else
                    Debug.LogWarning($"[LevelBuilder] Scene not found: {path} — run Build first.");
            }

            var registered = new HashSet<string>();
            foreach (var scene in scenes) registered.Add(scene.path);
            foreach (var existing in EditorBuildSettings.scenes)
                if (registered.Add(existing.path)) scenes.Add(existing);

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[LevelBuilder] Build settings updated: {scenes.Count} scenes registered.");
        }

        [MenuItem("Tools/FunRace/Validate Levels")]
        public static void ValidateLevels()
        {
            ValidateLevel(SCENE_L1);
            ValidateLevel(SCENE_L2);
            ValidateLevel(SCENE_L3);
        }

        // ── Level data tables ─────────────────────────────────────────────
        // Layout: list of obstacle descriptors placed along the X axis.
        // X = position along track (racer's forward direction)
        // Type = obstacle type string
        // Param = first config param (speed, period, etc.)

        public struct ObstacleDesc
        {
            public string type;  // "Block","Hammer","Sweeper","Gate","DisPlatform","MovPlatform","Gap"
            public float  x;
            public float  param1; // speed / period
            public float  param2; // secondary param
            public string note;
        }

        public static readonly ObstacleDesc[] Level1Layout =
        {
            new() { type = "Gap",         x =  8f, param1 = 1.0f,  note = "Easy jump" },
            new() { type = "Block",       x = 14f, param1 = 2f,    param2 = 4f, note = "Slow moving block" },
            new() { type = "Hammer",      x = 22f, param1 = 3.5f,  note = "Slow hammer, period=3.5s" },
            new() { type = "Checkpoint",  x = 27f },
            new() { type = "Gap",         x = 32f, param1 = 1.0f,  note = "Second jump" },
            new() { type = "Hammer",      x = 38f, param1 = 4.0f,  note = "Second hammer" },
            new() { type = "MovPlatform", x = 44f, param1 = 1.5f,  note = "First moving platform" },
            new() { type = "Checkpoint",  x = 50f },
            new() { type = "Finish",      x = 55f },
        };

        public static readonly ObstacleDesc[] Level2Layout =
        {
            new() { type = "Hammer",      x =  8f, param1 = 2.5f, note = "Faster hammer" },
            new() { type = "Gap",         x = 15f, param1 = 1.0f },
            new() { type = "Sweeper",     x = 20f, param1 = 90f,  note = "Rotating sweeper 90deg/s" },
            new() { type = "Checkpoint",  x = 25f },
            new() { type = "Gate",        x = 30f, param1 = 2f, param2 = 2f, note = "Timed gate 2/2" },
            new() { type = "MovPlatform", x = 36f, param1 = 2f,   note = "Moving platform" },
            new() { type = "DisPlatform", x = 42f, param1 = 4.2f, note = "Disappearing platform" },
            new() { type = "Checkpoint",  x = 48f },
            new() { type = "Hammer",      x = 52f, param1 = 2.0f, note = "Fast hammer" },
            new() { type = "Sweeper",     x = 57f, param1 = 120f },
            new() { type = "Finish",      x = 65f },
        };

        public static readonly ObstacleDesc[] Level3Layout =
        {
            new() { type = "Sweeper",     x =  8f, param1 = 150f },
            new() { type = "Gap",         x = 14f, param1 = 1.0f },
            new() { type = "Hammer",      x = 20f, param1 = 2.0f },
            new() { type = "Gate",        x = 25f, param1 = 1.5f, param2 = 1.5f },
            new() { type = "Checkpoint",  x = 30f },
            new() { type = "MovPlatform", x = 36f, param1 = 3f },
            new() { type = "DisPlatform", x = 43f, param1 = 4.2f },
            new() { type = "MovPlatform", x = 50f, param1 = 2.5f },
            new() { type = "Checkpoint",  x = 55f },
            new() { type = "Sweeper",     x = 60f, param1 = 180f },
            new() { type = "Hammer",      x = 65f, param1 = 1.8f },
            new() { type = "Gate",        x = 70f, param1 = 1.2f, param2 = 1.8f },
            new() { type = "DisPlatform", x = 76f, param1 = 4.2f },
            new() { type = "Checkpoint",  x = 80f },
            new() { type = "Gap",         x = 85f, param1 = 1.0f },
            new() { type = "Finish",      x = 95f },
        };

        // ── Build a level scene ───────────────────────────────────────────
        private static void BuildLevel(int levelIndex)
        {
            string[] names   = { SCENE_L1, SCENE_L2, SCENE_L3 };
            ObstacleDesc[][] layouts = { Level1Layout, Level2Layout, Level3Layout };
            string   path    = EnsureSceneFolder() + names[levelIndex] + ".unity";

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Lighting
            SetupLighting(levelIndex);

            // Build real floor segments so each validated gap is actually open.
            CreateGround(levelIndex, layouts[levelIndex]);

            // Path markers
            var startPt = CreateMarker("StartPoint",  new Vector3(0f,   0f, 0f));
            var endPt   = CreateMarker("EndPoint",    new Vector3(GetEndX(layouts[levelIndex]), 0f, 0f));

            // Checkpoints & finish
            int cpIdx = 0;
            foreach (var desc in layouts[levelIndex])
            {
                switch (desc.type)
                {
                    case "Checkpoint":
                        CreateCheckpointMarker(desc.x, cpIdx++);
                        break;
                    case "Finish":
                        CreateFinishMarker(desc.x, levelIndex);
                        break;
                    default:
                        CreateObstacle(desc, _trackRoot.transform, levelIndex);
                        break;
                }
            }

            // Spawn lane markers (used by RaceManager)
            CreateSpawnMarkers(0f);

            // Camera
            CreateBasicCamera($"Level{levelIndex+1}Camera", new Vector3(5, 8, -12));

            // Event system
            CreateEventSystem();

            // RaceManager + HUD placeholder GameObjects
            CreateRaceManagerGO(levelIndex, startPt, endPt);

            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[LevelBuilder] Level {levelIndex + 1} saved to {path}");
            AssetDatabase.Refresh();
        }

        // ── Scene object creators ─────────────────────────────────────────
        private static GameObject CreateMarker(string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            return go;
        }

        private static GameObject _trackRoot;

        private static Transform FindTrackRoot()
        {
            if (_trackRoot == null)
            {
                _trackRoot = new GameObject("_GeneratedTrack");
                Undo.RegisterCreatedObjectUndo(_trackRoot, "Create generated track");
            }
            return _trackRoot.transform;
        }

        private static void CreateGround(int level, ObstacleDesc[] layout)
        {
            _trackRoot = new GameObject("_GeneratedTrack");
            float endX = GetEndX(layout);
            Color floorColor = level switch
            {
                0 => new Color(0.4f, 0.9f, 1.0f),  // Training: cyan
                1 => new Color(0.5f, 0.5f, 0.6f),  // Industrial: grey
                2 => new Color(0.1f, 0.0f, 0.2f),  // Gauntlet: dark purple
                _ => Color.grey
            };
            var openings = new List<ObstacleDesc>();
            foreach (var d in layout)
                if (d.type == "Gap" || d.type == "DisPlatform") openings.Add(d);
            openings.Sort((a, b) => a.x.CompareTo(b.x));
            float cursor = -5f;
            foreach (var opening in openings)
            {
                float openingWidth = opening.type == "Gap" ? opening.param1 : 3.5f;
                float left = opening.x - openingWidth * 0.5f;
                CreateFloorSegment(cursor, left, floorColor);
                cursor = opening.x + openingWidth * 0.5f;
            }
            CreateFloorSegment(cursor, endX + 5f, floorColor);
        }

        private static void CreateFloorSegment(float fromX, float toX, Color color)
        {
            if (toX <= fromX) return;
            TrackObstacleFactory.Primitive(_trackRoot.transform, "TrackFloor", PrimitiveType.Cube,
                new Vector3((fromX + toX) * 0.5f, -0.5f, 0f), new Vector3(toX - fromX, 1f, 6f),
                Quaternion.identity, color);
        }

        private static void CreateCheckpointMarker(float x, int idx)
        {
            var go = new GameObject($"Checkpoint_{idx}");
            go.transform.position = new Vector3(x, 1f, 0f);
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size      = new Vector3(0.5f, 3f, 6f);
            var cp = go.AddComponent<FunRaceParkour.RaceCheckpoint>();
            cp.checkpointIndex = idx;
            var respawn = new GameObject($"CheckpointRespawn_{idx}");
            respawn.transform.SetParent(go.transform, false);
            respawn.transform.localPosition = new Vector3(-2.5f, -0.9f, 0f);
            cp.respawnPoint = respawn.transform;
        }

        private static void CreateFinishMarker(float x, int level)
        {
            var go = new GameObject("FinishLine");
            go.transform.position = new Vector3(x, 1f, 0f);
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size      = new Vector3(0.5f, 3f, 6f);
            go.AddComponent<FunRaceParkour.FinishLine>();

            // Visual finish arch
            var arch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arch.name = "FinishArch";
            arch.transform.position   = new Vector3(x, 2f, 0f);
            arch.transform.localScale = new Vector3(0.4f, 4f, 7f);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(1f, 0.84f, 0f); // gold
            arch.GetComponent<Renderer>().sharedMaterial = mat;

            if (level == 2)
                TrackObstacleFactory.Primitive(null, "FinalePlatform", PrimitiveType.Cube,
                    new Vector3(x + 3f, -0.15f, 0f), new Vector3(8f, 0.3f, 6f), Quaternion.identity,
                    new Color(0.7f, 0.1f, 1f));
        }

        private static void CreateObstacle(ObstacleDesc desc, Transform parent, int level)
        {
            if (parent == null) parent = FindTrackRoot();
            Vector3 pos = new Vector3(desc.x, 0f, 0f);
            Color accent = level == 0 ? new Color(0.1f, 0.45f, 1f) :
                level == 1 ? new Color(1f, 0.35f, 0.08f) : new Color(0.9f, 0.1f, 1f);
            switch (desc.type)
            {
                case "Gap": TrackObstacleFactory.CreateGap(parent, pos, desc.param1); break;
                case "Block": TrackObstacleFactory.CreateMovingBlock(parent, pos, accent, desc.param1); break;
                case "Hammer": TrackObstacleFactory.CreateSwingingHammer(parent, pos, accent, desc.param1); break;
                case "Sweeper": TrackObstacleFactory.CreateRotatingSweeper(parent, pos, accent, desc.param1); break;
                case "Gate": TrackObstacleFactory.CreateTimedGate(parent, pos, accent, desc.param1, desc.param2); break;
                case "DisPlatform": TrackObstacleFactory.CreateDisappearingPlatform(parent, pos, accent, desc.param1, 1.5f); break;
                case "MovPlatform": TrackObstacleFactory.CreateMovingPlatform(parent, pos, accent, desc.param1, 3f); break;
            }
        }

        private static void CreateSpawnMarkers(float startX)
        {
            float[] zOffsets = { -1.5f, 0f, 1.5f };
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject($"SpawnPoint_Lane{i}");
                go.transform.position = new Vector3(startX, 0.1f, zOffsets[i]);
            }
        }

        private static void CreateBasicCamera(string name, Vector3 pos)
        {
            var camGO = new GameObject(name);
            camGO.transform.position = pos;
            camGO.tag = "MainCamera";
            camGO.transform.rotation = Quaternion.Euler(12f, -90f, 0f);
            camGO.AddComponent<Camera>();
            camGO.AddComponent<CinemachineBrain>();

            var vcamGO = new GameObject("RaceCamera");
            var vcam = vcamGO.AddComponent<CinemachineCamera>();
            vcamGO.AddComponent<CinemachineThirdPersonFollow>();
            var fixedCamera = vcamGO.AddComponent<FunRaceParkour.FixedTrackCamera>();
            fixedCamera.cameraDistance = 7f;
            fixedCamera.trackYaw = 90f;
        }

        private static void CreateEventSystem()
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            // Use InputSystemUIInputModule if available
            var inputModule = esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        private static void CreateRaceManagerGO(int levelIdx, GameObject startPt, GameObject endPt)
        {
            var rmGO = new GameObject("RaceManager");
            var rm   = rmGO.AddComponent<FunRaceParkour.RaceManager>();

            // Persistent services in level scene (they are DontDestroyOnLoad so this is just an initialiser)
            var hudGO = new GameObject("RaceHUD");
            // RaceHUD requires Canvas — add placeholder
            var canvas = hudGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            hudGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var hud = hudGO.AddComponent<FunRaceParkour.RaceHUD>();
            ConfigureUiFont(hud, "uiFont");

            var pauseGO = new GameObject("PauseMenu");
            pauseGO.AddComponent<FunRaceParkour.PauseMenu>();
            AddConfiguredAudioManager("RaceAudioManager");

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerRobot.prefab");
            var aiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/AIRacer.prefab");
            if (aiPrefab == null) aiPrefab = playerPrefab;
            var configs = new FunRaceParkour.RaceManager.RacerConfig[]
            {
                new() { name = "AI Racer 1", color = new Color(0.9f, 0.25f, 0.25f), prefab = aiPrefab, isPlayer = false },
                new() { name = "Player", color = new Color(0.2f, 0.6f, 1.0f), prefab = playerPrefab, isPlayer = true },
                new() { name = "AI Racer 2", color = new Color(0.25f, 0.9f, 0.35f), prefab = aiPrefab, isPlayer = false },
            };

            rm.ConfigureTrack(startPt.transform, endPt.transform, hud, configs);
        }

        private static void AddConfiguredAudioManager(string objectName)
        {
            var go = new GameObject(objectName);
            var manager = go.AddComponent<FunRaceParkour.AudioManager>();
            var serialized = new SerializedObject(manager);
            SetClip(serialized, "defaultMusicClip", "Music_Exciting.ogg");
            SetClip(serialized, "countdownTickClip", "SFX_PositiveSound01.ogg");
            SetClip(serialized, "countdownGoClip", "SFX_PositiveSound01.ogg");
            SetClip(serialized, "checkpointClip", "SFX_PositiveSound01.ogg");
            SetClip(serialized, "finishClip", "SFX_PositiveSound01.ogg");
            SetClip(serialized, "hazardHitClip", "SFX_AmbienceClose.ogg");
            SetClip(serialized, "respawnClip", "SFX_Respawn.ogg");
            SetClip(serialized, "buttonClickClip", "SFX_PositiveSound01.ogg");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureUiFont(Object target, string propertyName)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SourceFiles/Fonts/Inter-Variable SDF.asset");
            if (font == null) return;
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = font;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetClip(SerializedObject serialized, string propertyName, string fileName)
        {
            string[] guids = AssetDatabase.FindAssets(System.IO.Path.GetFileNameWithoutExtension(fileName) + " t:AudioClip");
            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileName(assetPath) != fileName) continue;
                serialized.FindProperty(propertyName).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                return;
            }
        }

        private static void SetupLighting(int level)
        {
            // Set ambient colour by level theme
            Color ambient = level switch
            {
                0 => new Color(0.6f, 0.9f, 1.0f),
                1 => new Color(0.5f, 0.4f, 0.3f),
                2 => new Color(0.1f, 0.05f, 0.2f),
                _ => Color.grey
            };
            RenderSettings.ambientLight = ambient;
            RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;

            // Sun directional light
            var lightGO = new GameObject("DirectionalLight");
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightGO.AddComponent<Light>();
            light.type      = LightType.Directional;
            light.intensity = 1.2f;
            light.color     = level == 2 ? new Color(0.6f, 0.4f, 1f) : Color.white;
        }

        // ── Validation ────────────────────────────────────────────────────
        private static void ValidateLevel(string sceneName)
        {
            string path = EnsureSceneFolder() + sceneName + ".unity";
            if (!File.Exists(path)) { Debug.LogWarning($"[Validate] Scene not found: {path}"); return; }

            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int errors = 0;

            // Check gap sizes using the jump reach formula
            float moveSpeed  = 2.0f;
            float jumpHeight = 1.2f;
            float gravity    = -15f;
            float margin     = 0.15f;
            float maxSafeGap = FunRaceParkour.AIRacerInput.ComputeJumpReach(2f, jumpHeight, gravity, margin);

            ObstacleDesc[][] layouts = { Level1Layout, Level2Layout, Level3Layout };
            int levelIndex = sceneName == SCENE_L1 ? 0 : sceneName == SCENE_L2 ? 1 : 2;
            foreach (var desc in layouts[levelIndex])
            {
                if (desc.type == "Gap" && desc.param1 >= maxSafeGap)
                {
                    Debug.LogError($"[Validate] {sceneName}: gap at {desc.x:F1}m is {desc.param1:F2}m; safe reach is {maxSafeGap:F2}m.");
                    errors++;
                }
            }

            var checkpoints = Object.FindObjectsByType<FunRaceParkour.RaceCheckpoint>(FindObjectsSortMode.None);
            var finishes = Object.FindObjectsByType<FunRaceParkour.FinishLine>(FindObjectsSortMode.None);
            var managers = Object.FindObjectsByType<FunRaceParkour.RaceManager>(FindObjectsSortMode.None);
            if (checkpoints.Length == 0) { Debug.LogError($"[Validate] {sceneName}: no checkpoints found."); errors++; }
            if (finishes.Length != 1) { Debug.LogError($"[Validate] {sceneName}: expected one finish line; found {finishes.Length}."); errors++; }
            if (managers.Length != 1) { Debug.LogError($"[Validate] {sceneName}: expected one RaceManager; found {managers.Length}."); errors++; }
            if (TrackObstacleFactory.LaneWidth < 2f * 1.5f + 1.5f)
            {
                Debug.LogError($"[Validate] {sceneName}: obstacle lane coverage is too narrow.");
                errors++;
            }

            if (errors == 0)
                Debug.Log($"[Validate] {sceneName}: gaps, lane coverage, checkpoints, finish, and RaceManager are valid.");
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private static string EnsureSceneFolder()
        {
            if (!Directory.Exists(SCENES_PATH))
                Directory.CreateDirectory(SCENES_PATH);
            return SCENES_PATH;
        }

        private static float GetEndX(ObstacleDesc[] layout)
        {
            float max = 60f;
            foreach (var d in layout) if (d.x > max) max = d.x;
            return max + 5f;
        }
    }

    // ── Placeholder note component (Editor-only) ───────────────────────
    // Attach to obstacle placeholder GameObjects so the validator can find gap sizes.
    public class ObstaclePlaceholderNote : MonoBehaviour
    {
        public string obstacleType;
        public float  param1;
        public float  param2;
        public string description;
    }
}
#endif
