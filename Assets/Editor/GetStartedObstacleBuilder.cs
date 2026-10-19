#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using FunRaceParkour;

namespace FunRaceParkour.Editor
{
    public static class GetStartedObstacleBuilder
    {
        private const string ParentName = "_Track_Obstacles";
        private const string RequestFile = "Assets/Editor/GetStartedObstacleBuilder.request"; // one-shot editor request

        [InitializeOnLoadMethod]
        private static void BuildRequestedCourse()
        {
            if (!System.IO.File.Exists(RequestFile))
                return;

            System.IO.File.Delete(RequestFile);
            if (SceneManager.GetActiveScene().name == "GetStarted_Scene")
                Build();
        }

        [MenuItem("Tools/FunRace/Build GetStarted Obstacle Course")]
        public static void Build()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != "GetStarted_Scene")
            {
                string scenePath = "Assets/Scenes/GetStarted_Scene.unity";
                if (System.IO.File.Exists(scenePath))
                    scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            if (scene.name != "GetStarted_Scene")
            {
                EditorUtility.DisplayDialog(
                    "GetStarted Obstacle Builder",
                    "Open GetStarted_Scene before building the obstacle course.",
                    "OK");
                return;
            }

            Transform start = FindMarker("StartPoint");
            Transform end = FindMarker("EndPoint");
            if (start == null || end == null)
            {
                EditorUtility.DisplayDialog(
                    "GetStarted Obstacle Builder",
                    "The scene must contain StartPoint and EndPoint transforms.",
                    "OK");
                return;
            }

            GameObject existing = GameObject.Find(ParentName);
            if (existing != null)
                Object.DestroyImmediate(existing);

            GameObject parent = new GameObject(ParentName);
            Undo.RegisterCreatedObjectUndo(parent, "Build GetStarted obstacle course");

            Vector3 path = end.position - start.position;
            float length = path.magnitude;
            Vector3 forward = path.normalized;
            Vector3 lateral = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 basePosition = start.position;

            CreateMovingBlock(parent.transform, PointOnPath(basePosition, forward, lateral, length, 0.14f, -1.2f), forward);
            CreateRotatingSweeper(parent.transform, PointOnPath(basePosition, forward, lateral, length, 0.27f, 1.1f), forward);
            CreateSwingingHammer(parent.transform, PointOnPath(basePosition, forward, lateral, length, 0.40f, -1.0f), forward);
            CreateTimedGate(parent.transform, PointOnPath(basePosition, forward, lateral, length, 0.53f, 0f), forward);
            CreateDisappearingPlatform(parent.transform, PointOnPath(basePosition, forward, lateral, length, 0.66f, 0f));
            CreateExtendedMovingPlatform(parent.transform, PointOnPath(basePosition, forward, lateral, length, 0.77f, 0f), lateral);
            CreateMovingPlatform(parent.transform, PointOnPath(basePosition, forward, lateral, length, 0.89f, 0.9f), lateral);

            AddRacerHelpers();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = parent;
            Debug.Log("[GetStartedObstacleBuilder] Built 7 obstacles under _Track_Obstacles.");
        }

        private static Transform FindMarker(string markerName)
        {
            GameObject marker = GameObject.Find(markerName);
            return marker != null ? marker.transform : null;
        }

        private static Vector3 PointOnPath(
            Vector3 start,
            Vector3 forward,
            Vector3 lateral,
            float length,
            float normalizedDistance,
            float lateralOffset)
        {
            return start + forward * (length * normalizedDistance) + lateral * lateralOffset;
        }

        private static GameObject CreatePrimitive(
            Transform parent,
            string name,
            PrimitiveType type,
            Vector3 position,
            Vector3 scale,
            Quaternion rotation)
        {
            GameObject obstacle = GameObject.CreatePrimitive(type);
            obstacle.name = name;
            obstacle.transform.SetParent(parent, true);
            obstacle.transform.SetPositionAndRotation(position, rotation);
            obstacle.transform.localScale = scale;
            return obstacle;
        }

        private static void CreateMovingBlock(Transform parent, Vector3 position, Vector3 forward)
        {
            GameObject obstacle = CreatePrimitive(
                parent, "01_MovingBlock", PrimitiveType.Cube, position,
                new Vector3(2.2f, 2.0f, 1.2f), Quaternion.LookRotation(forward, Vector3.up));
            Rigidbody rigidbody = obstacle.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            MovingBlock block = obstacle.AddComponent<MovingBlock>();

            Transform pointA = CreateEndpoint(obstacle.transform, "PointA", position - forward * 2.2f);
            Transform pointB = CreateEndpoint(obstacle.transform, "PointB", position + forward * 2.2f);
            Set(block, "pointA", pointA);
            Set(block, "pointB", pointB);
            Set(block, "speed", 2.2f);
            Set(block, "pauseAtEnds", 0.25f);
        }

        private static void CreateRotatingSweeper(Transform parent, Vector3 position, Vector3 forward)
        {
            Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
            GameObject obstacle = CreatePrimitive(
                parent, "02_RotatingSweeper", PrimitiveType.Cube, position,
                new Vector3(0.65f, 0.65f, 5.0f), rotation);
            Rigidbody rigidbody = obstacle.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            RotatingSweeper sweeper = obstacle.AddComponent<RotatingSweeper>();
            Set(sweeper, "degreesPerSecond", 105f);
            Set(sweeper, "axis", Vector3.up);
            Set(sweeper, "phaseOffset", 25f);
            Set(sweeper, "pauseEveryDegrees", 0f);
            Set(sweeper, "pauseDuration", 0f);
        }

        private static void CreateSwingingHammer(Transform parent, Vector3 position, Vector3 forward)
        {
            GameObject obstacle = new GameObject("03_SwingingHammer");
            obstacle.transform.SetParent(parent, true);
            obstacle.transform.SetPositionAndRotation(position + Vector3.up * 2.8f, Quaternion.identity);

            Rigidbody rigidbody = obstacle.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            SwingingHammer hammer = obstacle.AddComponent<SwingingHammer>();
            Set(hammer, "maxAngle", 58f);
            Set(hammer, "period", 2.4f);
            Set(hammer, "phaseOffset", 0f);
            Set(hammer, "pauseAtEnds", 0.1f);
            Set(hammer, "swingAxis", forward);

            GameObject head = CreatePrimitive(
                obstacle.transform, "HammerHead", PrimitiveType.Cube,
                position + Vector3.down * 1.7f, new Vector3(2.2f, 1.2f, 1.2f),
                Quaternion.LookRotation(forward, Vector3.up));
            Set(hammer, "hammerHeadCollider", head.GetComponent<Collider>());
        }

        private static void CreateTimedGate(Transform parent, Vector3 position, Vector3 forward)
        {
            GameObject obstacle = CreatePrimitive(
                parent, "04_TimedGate", PrimitiveType.Cube, position + Vector3.up * 1.4f,
                new Vector3(0.8f, 2.8f, 5.5f), Quaternion.LookRotation(forward, Vector3.up));
            TimedGate gate = obstacle.AddComponent<TimedGate>();
            Set(gate, "gateGeometry", obstacle);
            Set(gate, "gateCollider", obstacle.GetComponent<Collider>());
            Set(gate, "openTime", 2.2f);
            Set(gate, "closedTime", 2.0f);
            Set(gate, "phaseOffset", 0.5f);
        }

        private static void CreateDisappearingPlatform(Transform parent, Vector3 position)
        {
            GameObject obstacle = CreatePrimitive(
                parent, "05_DisappearingPlatform", PrimitiveType.Cube, position,
                new Vector3(5.0f, 0.45f, 3.5f), Quaternion.identity);
            DisappearingPlatform platform = obstacle.AddComponent<DisappearingPlatform>();
            Set(platform, "platformRenderer", obstacle.GetComponent<Renderer>());
            Set(platform, "platformCollider", obstacle.GetComponent<Collider>());
            Set(platform, "stayTime", 1.7f);
            Set(platform, "goneTime", 2.2f);
            Set(platform, "warnTime", 0.45f);
        }

        private static void CreateExtendedMovingPlatform(
            Transform parent,
            Vector3 position,
            Vector3 lateral)
        {
            GameObject obstacle = CreatePrimitive(
                parent, "06_MovingPlatformExtended", PrimitiveType.Cube, position,
                new Vector3(4.0f, 0.45f, 3.5f), Quaternion.identity);
            Rigidbody rigidbody = obstacle.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            MovingPlatformExtended platform = obstacle.AddComponent<MovingPlatformExtended>();
            Transform pointA = CreateEndpoint(obstacle.transform, "PointA", position - lateral * 3.0f);
            Transform pointB = CreateEndpoint(obstacle.transform, "PointB", position + lateral * 3.0f);
            Set(platform, "pointA", pointA);
            Set(platform, "pointB", pointB);
            Set(platform, "speed", 2f);
            Set(platform, "pauseAtEnds", 0.3f);
        }

        private static void CreateMovingPlatform(
            Transform parent,
            Vector3 position,
            Vector3 lateral)
        {
            GameObject obstacle = CreatePrimitive(
                parent, "07_MovingPlatform", PrimitiveType.Cube, position,
                new Vector3(4.0f, 0.45f, 3.5f), Quaternion.identity);
            global::MovingPlatform platform = obstacle.AddComponent<global::MovingPlatform>();
            Set(platform, "movementDirection", lateral);
            Set(platform, "movementDistance", 5f);
            Set(platform, "speed", 2.4f);
        }

        private static Transform CreateEndpoint(Transform parent, string name, Vector3 position)
        {
            GameObject endpoint = new GameObject(name);
            endpoint.transform.SetParent(parent, true);
            endpoint.transform.position = position;
            endpoint.hideFlags = HideFlags.HideInHierarchy;
            return endpoint.transform;
        }

        private static void AddRacerHelpers()
        {
            CharacterController[] racers = Object.FindObjectsByType<CharacterController>(FindObjectsInactive.Include);
            foreach (CharacterController racer in racers)
            {
                if (racer.GetComponent<PlatformRider>() == null)
                    Undo.AddComponent<PlatformRider>(racer.gameObject);
                if (racer.GetComponent<PlatformContactReporter>() == null)
                    Undo.AddComponent<PlatformContactReporter>(racer.gameObject);
            }
        }

        private static void Set(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(Object target, string propertyName, float value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(Object target, string propertyName, Vector3 value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).vector3Value = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
