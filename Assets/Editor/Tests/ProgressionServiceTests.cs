// ProgressionServiceTests.cs
// Unit tests for progression state, unlocking, best times, and reset logic.

#if UNITY_EDITOR
using UnityEngine;

namespace FunRaceParkour.Tests
{
    public static class ProgressionServiceTests
    {
        public static void RunAll()
        {
            var hadUnlock = new bool[3];
            var savedUnlock = new int[3];
            var hadTime = new bool[3];
            var savedTime = new float[3];
            for (int i = 0; i < 3; i++)
            {
                string unlockKey = $"FRP_Unlocked_{i}";
                string timeKey = $"FRP_BestTime_{i}";
                hadUnlock[i] = PlayerPrefs.HasKey(unlockKey);
                hadTime[i] = PlayerPrefs.HasKey(timeKey);
                savedUnlock[i] = PlayerPrefs.GetInt(unlockKey);
                savedTime[i] = PlayerPrefs.GetFloat(timeKey);
            }

            var go = new GameObject("TestProgressionService");
            var service = go.AddComponent<ProgressionService>();

            try
            {
                TestDefaultState(service);
                TestLevelUnlockSequence(service);
                TestBestTimeTracking(service);
                TestResetAll(service);
            }
            finally
            {
                service.ResetAll();
                Object.DestroyImmediate(go);
                for (int i = 0; i < 3; i++)
                {
                    string unlockKey = $"FRP_Unlocked_{i}";
                    string timeKey = $"FRP_BestTime_{i}";
                    if (hadUnlock[i]) PlayerPrefs.SetInt(unlockKey, savedUnlock[i]);
                    else PlayerPrefs.DeleteKey(unlockKey);
                    if (hadTime[i]) PlayerPrefs.SetFloat(timeKey, savedTime[i]);
                    else PlayerPrefs.DeleteKey(timeKey);
                }
                PlayerPrefs.Save();
            }
        }

        private static void TestDefaultState(ProgressionService service)
        {
            service.ResetAll();

            TestAssert.IsTrue(service.IsUnlocked(0), "Level 1 (index 0) must always be unlocked by default.");
            TestAssert.IsFalse(service.IsUnlocked(1), "Level 2 (index 1) must be locked initially.");
            TestAssert.IsFalse(service.IsUnlocked(2), "Level 3 (index 2) must be locked initially.");
        }

        private static void TestLevelUnlockSequence(ProgressionService service)
        {
            service.ResetAll();

            // Finishing level 0 should unlock level 1
            service.OnPlayerFinished(0, 35.5f);
            TestAssert.IsTrue(service.IsUnlocked(1), "Level 2 must be unlocked after finishing Level 1.");
            TestAssert.IsFalse(service.IsUnlocked(2), "Level 3 must still be locked.");

            // Finishing level 1 should unlock level 2
            service.OnPlayerFinished(1, 42.0f);
            TestAssert.IsTrue(service.IsUnlocked(2), "Level 3 must be unlocked after finishing Level 2.");

            service.ResetAll();
            service.OnPlayerFinished(0, float.NaN);
            service.OnPlayerFinished(0, -1f);
            TestAssert.IsFalse(service.IsUnlocked(1), "Invalid finish times must not unlock the next level.");
        }

        private static void TestBestTimeTracking(ProgressionService service)
        {
            service.ResetAll();

            // First run: 45.0s
            service.OnPlayerFinished(0, 45.0f);
            TestAssert.AreApproximatelyEqual(45.0f, service.GetBestTime(0), 0.01f, "First finish time should become best time.");

            // Slower run: 50.0s — should not overwrite
            service.OnPlayerFinished(0, 50.0f);
            TestAssert.AreApproximatelyEqual(45.0f, service.GetBestTime(0), 0.01f, "Slower time must not overwrite best time.");

            // Faster run: 39.2s — should overwrite
            service.OnPlayerFinished(0, 39.2f);
            TestAssert.AreApproximatelyEqual(39.2f, service.GetBestTime(0), 0.01f, "Faster time must overwrite best time.");
        }

        private static void TestResetAll(ProgressionService service)
        {
            service.OnPlayerFinished(0, 30.0f);
            service.OnPlayerFinished(1, 35.0f);

            service.ResetAll();

            TestAssert.IsTrue(service.IsUnlocked(0), "Level 1 must remain unlocked after reset.");
            TestAssert.IsFalse(service.IsUnlocked(1), "Level 2 must be locked after reset.");
            TestAssert.AreApproximatelyEqual(-1f, service.GetBestTime(0), 0.01f, "Best time must be cleared to -1.");
        }
    }
}
#endif
