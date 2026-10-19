// HazardPredictionTests.cs
// Unit tests for deterministic IPredictableHazard implementations
// ensuring AI racers receive correct prediction windows.

#if UNITY_EDITOR
using UnityEngine;

namespace FunRaceParkour.Tests
{
    public static class HazardPredictionTests
    {
        public static void RunAll()
        {
            TestTimedGate_Prediction();
            TestDisappearingPlatform_PredictionAndReset();
        }

        private static void TestTimedGate_Prediction()
        {
            var go = new GameObject("TestTimedGate");
            var gate = go.AddComponent<TimedGate>();

            try
            {
                gate.ResetState();

                // Initially open, openDuration = 2s, closedDuration = 2s
                // Safe to cross immediately if cross duration <= 2s
                TestAssert.IsTrue(gate.IsSafeAt(0f, 0f, 1.0f), "Timed gate should be safe initially when cross duration is short.");

                // Not safe if required cross duration exceeds open window
                TestAssert.IsFalse(gate.IsSafeAt(0f, 0f, 5.0f), "Timed gate should not be safe if required duration exceeds open period.");

                // Next safe window when currently safe should be 0s
                TestAssert.AreApproximatelyEqual(0f, gate.NextSafeWindow(0f, 1.0f), 0.05f, "Next safe window should be 0 when gate is currently open.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static void TestDisappearingPlatform_PredictionAndReset()
        {
            var go = new GameObject("TestDisappearingPlatform");
            var col = go.AddComponent<BoxCollider>();
            var rend = go.AddComponent<MeshRenderer>();
            var platform = go.AddComponent<DisappearingPlatform>();

            try
            {
                platform.ResetState();

                // Untouched pristine platform is always safe
                TestAssert.IsTrue(platform.IsSafeAt(0f, 0f, 1.0f), "Untouched platform should be safe.");
                TestAssert.AreApproximatelyEqual(0f, platform.NextSafeWindow(0f, 1.0f), 0.01f, "Pristine platform safe window is 0.");

                // Reset works correctly
                platform.ResetState();
                TestAssert.IsTrue(platform.IsSafeAt(0f, 0f, 0.5f), "Platform should return to pristine solid after reset.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
#endif
