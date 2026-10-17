// AIProfileGenerator.cs
// Editor tool to generate and configure the standard AI Racer Profiles
// under Assets/ScriptableObjects/AI/ for easy assignment in scene / prefabs.

#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;

namespace FunRaceParkour.Editor
{
    public static class AIProfileGenerator
    {
        private const string OutputFolder = "Assets/ScriptableObjects/AI";

        [MenuItem("Tools/FunRace/Generate AI Profiles")]
        public static void GenerateProfiles()
        {
            if (!Directory.Exists(OutputFolder))
                Directory.CreateDirectory(OutputFolder);

            CreateOrUpdateProfile("AIRacer_Balanced", 0.15f, 0.08f, 0.15f, 0.10f, 3.0f, 2, 1.3f);
            CreateOrUpdateProfile("AIRacer_Aggressive", 0.08f, 0.04f, 0.08f, 0.05f, 2.0f, 1, 1.1f);
            CreateOrUpdateProfile("AIRacer_Cautious", 0.25f, 0.12f, 0.22f, 0.15f, 4.0f, 3, 1.6f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AIProfileGenerator] Generated Balanced, Aggressive, and Cautious AI profiles.");
        }

        private static void CreateOrUpdateProfile(
            string profileName,
            float reactionTime,
            float jumpTimingError,
            float safetyMargin,
            float pollInterval,
            float stuckTimeout,
            int maxJumpRetries,
            float safeWindowMultiplier)
        {
            string path = $"{OutputFolder}/{profileName}.asset";
            var profile = AssetDatabase.LoadAssetAtPath<AIRacerProfile>(path);

            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<AIRacerProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            profile.reactionTime = reactionTime;
            profile.jumpTimingError = jumpTimingError;
            profile.safetyMargin = safetyMargin;
            profile.pollInterval = pollInterval;
            profile.stuckTimeout = stuckTimeout;
            profile.maxJumpRetries = maxJumpRetries;
            profile.safeWindowMultiplier = safeWindowMultiplier;

            EditorUtility.SetDirty(profile);
        }
    }
}
#endif
