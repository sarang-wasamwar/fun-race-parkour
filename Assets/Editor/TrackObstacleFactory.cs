#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using FunRaceParkour;

namespace FunRaceParkour.Editor
{
    public static class TrackObstacleFactory
    {
        public const float LaneWidth = 6f;

        public static GameObject Primitive(
            Transform parent,
            string name,
            PrimitiveType type,
            Vector3 position,
            Vector3 scale,
            Quaternion rotation,
            Color color)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = color;
                renderer.sharedMaterial = mat;
            }
            return go;
        }

        public static void CreateMovingBlock(Transform parent, Vector3 position, Color color, float speed)
        {
            var obstacle = Primitive(parent, $"MovingBlock_{position.x:F0}", PrimitiveType.Cube,
                position + Vector3.up, new Vector3(1.6f, 2.0f, LaneWidth), Quaternion.identity, color);
            var rb = obstacle.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            var block = obstacle.AddComponent<MovingBlock>();
            var a = Endpoint(obstacle.transform, "PointA", position + Vector3.up - Vector3.right * 2.4f);
            var b = Endpoint(obstacle.transform, "PointB", position + Vector3.up + Vector3.right * 2.4f);
            Set(block, "pointA", a);
            Set(block, "pointB", b);
            Set(block, "speed", speed);
        }

        public static void CreateRotatingSweeper(Transform parent, Vector3 position, Color color, float degPerSec)
        {
            var obstacle = Primitive(parent, $"Sweeper_{position.x:F0}", PrimitiveType.Cube,
                position + Vector3.up, new Vector3(0.6f, 0.6f, LaneWidth), Quaternion.identity, color);
            var rb = obstacle.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            var sweeper = obstacle.AddComponent<RotatingSweeper>();
            Set(sweeper, "degreesPerSecond", degPerSec);
            Set(sweeper, "axis", Vector3.up);
        }

        public static void CreateSwingingHammer(Transform parent, Vector3 position, Color color, float period)
        {
            var root = new GameObject($"Hammer_{position.x:F0}");
            root.transform.SetParent(parent, true);
            root.transform.position = position + Vector3.up * 3.2f;
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            var hammer = root.AddComponent<SwingingHammer>();
            Set(hammer, "maxAngle", 55f);
            Set(hammer, "period", period);
            Set(hammer, "swingAxis", Vector3.forward);
            var head = Primitive(root.transform, "Head", PrimitiveType.Cube,
                position + Vector3.up * 0.6f, new Vector3(1.6f, 1.2f, LaneWidth * 0.9f), Quaternion.identity, color);
            Set(hammer, "hammerHeadCollider", head.GetComponent<Collider>());
        }

        public static void CreateTimedGate(Transform parent, Vector3 position, Color color, float open, float closed)
        {
            var obstacle = Primitive(parent, $"Gate_{position.x:F0}", PrimitiveType.Cube,
                position + Vector3.up * 1.5f, new Vector3(0.7f, 3f, LaneWidth), Quaternion.identity, color);
            var gate = obstacle.AddComponent<TimedGate>();
            Set(gate, "gateGeometry", obstacle);
            Set(gate, "gateCollider", obstacle.GetComponent<Collider>());
            Set(gate, "openTime", open);
            Set(gate, "closedTime", closed);
        }

        public static void CreateDisappearingPlatform(Transform parent, Vector3 position, Color color, float stay, float gone)
        {
            var obstacle = Primitive(parent, $"DisPlatform_{position.x:F0}", PrimitiveType.Cube,
                position, new Vector3(3.5f, 0.4f, LaneWidth), Quaternion.identity, color);
            var platform = obstacle.AddComponent<DisappearingPlatform>();
            Set(platform, "platformRenderer", obstacle.GetComponent<Renderer>());
            Set(platform, "platformCollider", obstacle.GetComponent<Collider>());
            Set(platform, "stayTime", stay);
            Set(platform, "goneTime", gone);
        }

        public static void CreateMovingPlatform(Transform parent, Vector3 position, Color color, float speed, float travel)
        {
            var obstacle = Primitive(parent, $"MovPlatform_{position.x:F0}", PrimitiveType.Cube,
                position, new Vector3(3.2f, 0.4f, LaneWidth), Quaternion.identity, color);
            var rb = obstacle.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            var platform = obstacle.AddComponent<MovingPlatformExtended>();
            var a = Endpoint(obstacle.transform, "PointA", position - Vector3.right * travel * 0.5f);
            var b = Endpoint(obstacle.transform, "PointB", position + Vector3.right * travel * 0.5f);
            Set(platform, "pointA", a);
            Set(platform, "pointB", b);
            Set(platform, "speed", speed);
            Set(platform, "pauseAtEnds", 0.35f);
        }

        public static void CreateGap(Transform parent, Vector3 position, float width)
        {
            var gap = new GameObject($"Gap_{position.x:F0}");
            gap.transform.SetParent(parent, true);
            gap.transform.position = position;
            // Visual side rails make the opening readable without narrowing the jump lane.
            Primitive(gap.transform, "LeftWarning", PrimitiveType.Cube,
                position + new Vector3(0f, -0.36f, -3.05f), new Vector3(width, 0.08f, 0.08f), Quaternion.identity,
                new Color(1f, 0.35f, 0.1f));
            Primitive(gap.transform, "RightWarning", PrimitiveType.Cube,
                position + new Vector3(0f, -0.36f, 3.05f), new Vector3(width, 0.08f, 0.08f), Quaternion.identity,
                new Color(1f, 0.35f, 0.1f));
        }

        private static Transform Endpoint(Transform parent, string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, true);
            go.transform.position = position;
            return go.transform;
        }

        public static void Set(Object target, string propertyName, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(propertyName).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Set(Object target, string propertyName, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(propertyName).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Set(Object target, string propertyName, Vector3 value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(propertyName).vector3Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
