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
            BuildLevel(0);
            BuildLevel(1);
            BuildLevel(2);
            Debug.Log("[LevelBuilder] All levels built.");
        }

        [MenuItem("Tools/FunRace/Build Main Menu Scene")]
        public static void BuildMainMenuScene()
        {
            string path = EnsureSceneFolder() + SCENE_MENU + ".unity";
            var scene   = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Minimal: a camera, EventSystem, and MainMenuController placeholder
            CreateBasicCamera("MainMenuCamera", new Vector3(0, 5, -10));
            CreateEventSystem();

            var menuGO = new GameObject("MainMenu");
            menuGO.AddComponent<FunRaceParkour.MainMenuController>();
            // Add persistent services
            new GameObject("GameSession").AddComponent<FunRaceParkour.GameSession>();
            new GameObject("ProgressionService").AddComponent<FunRaceParkour.ProgressionService>();
            new GameObject("SceneLoader").AddComponent<FunRaceParkour.SceneLoader>();
            new GameObject("AudioManager").AddComponent<FunRaceParkour.AudioManager>();

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
            new() { type = "Gap",         x =  8f, param1 = 1.5f,  note = "Easy gap (1.5m)" },
            new() { type = "Block",       x = 14f, param1 = 2f,    param2 = 4f, note = "Slow moving block" },
            new() { type = "Hammer",      x = 22f, param1 = 3.5f,  note = "Slow hammer, period=3.5s" },
            new() { type = "Checkpoint",  x = 27f },
            new() { type = "Gap",         x = 32f, param1 = 1.8f,  note = "Second gap" },
            new() { type = "Hammer",      x = 38f, param1 = 4.0f,  note = "Second hammer" },
            new() { type = "MovPlatform", x = 44f, param1 = 1.5f,  note = "First moving platform" },
            new() { type = "Checkpoint",  x = 50f },
            new() { type = "Finish",      x = 55f },
        };

        public static readonly ObstacleDesc[] Level2Layout =
        {
            new() { type = "Hammer",      x =  8f, param1 = 2.5f, note = "Faster hammer" },
            new() { type = "Gap",         x = 15f, param1 = 2.0f },
            new() { type = "Sweeper",     x = 20f, param1 = 90f,  note = "Rotating sweeper 90deg/s" },
            new() { type = "Checkpoint",  x = 25f },
            new() { type = "Gate",        x = 30f, param1 = 2f, param2 = 2f, note = "Timed gate 2/2" },
            new() { type = "MovPlatform", x = 36f, param1 = 2f,   note = "Moving platform" },
            new() { type = "DisPlatform", x = 42f, param1 = 1.5f, note = "Disappearing platform" },
            new() { type = "Checkpoint",  x = 48f },
            new() { type = "Hammer",      x = 52f, param1 = 2.0f, note = "Fast hammer" },
            new() { type = "Sweeper",     x = 57f, param1 = 120f },
            new() { type = "Finish",      x = 65f },
        };

        public static readonly ObstacleDesc[] Level3Layout =
        {
            new() { type = "Sweeper",     x =  8f, param1 = 150f },
            new() { type = "Gap",         x = 14f, param1 = 2.2f },
            new() { type = "Hammer",      x = 20f, param1 = 2.0f },
            new() { type = "Gate",        x = 25f, param1 = 1.5f, param2 = 1.5f },
            new() { type = "Checkpoint",  x = 30f },
            new() { type = "MovPlatform", x = 36f, param1 = 3f },
            new() { type = "DisPlatform", x = 43f, param1 = 1.2f },
            new() { type = "MovPlatform", x = 50f, param1 = 2.5f },
            new() { type = "Checkpoint",  x = 55f },
            new() { type = "Sweeper",     x = 60f, param1 = 180f },
            new() { type = "Hammer",      x = 65f, param1 = 1.8f },
            new() { type = "Gate",        x = 70f, param1 = 1.2f, param2 = 1.8f },
            new() { type = "DisPlatform", x = 76f, param1 = 1.0f },
            new() { type = "Checkpoint",  x = 80f },
            new() { type = "Gap",         x = 85f, param1 = 2.5f },
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

            // Ground plane
            CreateGround(levelIndex);

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
                        CreateFinishMarker(desc.x);
                        break;
                    default:
                        CreateObstacleMarker(desc);
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

        private static void CreateGround(int level)
        {
            // A long plane along X
            float endX  = 100f;
            var ground  = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(endX * 0.5f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(endX + 10f, 1f, 6f);

            // Set layer for GroundLayers
            ground.layer = LayerMask.NameToLayer("Default");

            // Colour by level
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = level switch
            {
                0 => new Color(0.4f, 0.9f, 1.0f),  // Training: cyan
                1 => new Color(0.5f, 0.5f, 0.6f),  // Industrial: grey
                2 => new Color(0.1f, 0.0f, 0.2f),  // Gauntlet: dark purple
                _ => Color.grey
            };
            ground.GetComponent<Renderer>().sharedMaterial = mat;
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
            // respawnPoint: use the checkpoint transform itself
        }

        private static void CreateFinishMarker(float x)
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
        }

        private static void CreateObstacleMarker(ObstacleDesc desc)
        {
            // Create a placeholder cube at the right position.
            // In the actual game, these are replaced by configured prefabs.
            var go = new GameObject($"{desc.type}_{desc.x:F0}m");
            go.transform.position = new Vector3(desc.x, 1f, 0f);

            // Add a note component or tag
            var note = go.AddComponent<ObstaclePlaceholderNote>();
            note.obstacleType = desc.type;
            note.param1       = desc.param1;
            note.param2       = desc.param2;
            note.description  = desc.note;
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
            camGO.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
            camGO.AddComponent<Camera>();
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
            new GameObject("SceneServices").AddComponent<FunRaceParkour.SceneLoader>();
            new GameObject("GameSession").AddComponent<FunRaceParkour.GameSession>();
            new GameObject("ProgressionService").AddComponent<FunRaceParkour.ProgressionService>();
            new GameObject("AudioManager").AddComponent<FunRaceParkour.AudioManager>();

            var hudGO = new GameObject("RaceHUD");
            // RaceHUD requires Canvas — add placeholder
            var canvas = hudGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            hudGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var hud = hudGO.AddComponent<FunRaceParkour.RaceHUD>();

            var pauseGO = new GameObject("PauseMenu");
            pauseGO.AddComponent<FunRaceParkour.PauseMenu>();

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerRobot.prefab");
            var configs = new FunRaceParkour.RaceManager.RacerConfig[]
            {
                new() { name = "AI Racer 1 (Ruby)", color = new Color(0.9f, 0.25f, 0.25f), prefab = playerPrefab, isPlayer = false },
                new() { name = "Player", color = new Color(0.2f, 0.6f, 1.0f), prefab = playerPrefab, isPlayer = true },
                new() { name = "AI Racer 2 (Jade)", color = new Color(0.25f, 0.9f, 0.35f), prefab = playerPrefab, isPlayer = false },
            };

            rm.ConfigureTrack(startPt.transform, endPt.transform, hud, configs);
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

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int errors = 0;

            // Check gap sizes using the jump reach formula
            float moveSpeed  = 4.0f;
            float jumpHeight = 1.2f;
            float gravity    = -15f;
            float margin     = 0.15f;
            float maxSafeGap = FunRaceParkour.AIRacerInput.ComputeJumpReach(moveSpeed, jumpHeight, gravity, margin);

            foreach (var go in scene.GetRootGameObjects())
            {
                var notes = go.GetComponentsInChildren<ObstaclePlaceholderNote>();
                foreach (var note in notes)
                {
                    if (note.obstacleType == "Gap" && note.param1 > maxSafeGap)
                    {
                        Debug.LogError($"[Validate] {sceneName}: Gap at {go.transform.position.x:F1}m = {note.param1}m exceeds max safe gap {maxSafeGap:F2}m");
                        errors++;
                    }
                }
            }

            if (errors == 0)
                Debug.Log($"[Validate] {sceneName}: All gaps valid. Max safe gap = {maxSafeGap:F2}m");
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
