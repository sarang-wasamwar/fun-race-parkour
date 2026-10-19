// TrackValidationTests.cs
// Unit tests verifying track layouts in LevelBuilder:
// - All gaps are within the mathematical reach of racers (no impossible jumps)
// - Monotonically increasing X positions
// - Proper Checkpoint and Finish placement

#if UNITY_EDITOR
using FunRaceParkour.Editor;
using UnityEngine;

namespace FunRaceParkour.Tests
{
    public static class TrackValidationTests
    {
        private const float MoveSpeed   = 2.0f;
        private const float JumpHeight  = 1.2f;
        private const float Gravity     = -15.0f;
        private const float Margin      = 0.15f;

        public static void RunAll()
        {
            TestAssert.Greater(TrackObstacleFactory.LaneWidth, 2f * 1.5f + 1.5f,
                "Platforms and hazards must span all three lanes with edge margin.");
            float maxSafeGap = AIRacerInput.ComputeJumpReach(MoveSpeed, JumpHeight, Gravity, Margin);

            ValidateLayout("Level 1 (Training Run)", LevelBuilder.Level1Layout, maxSafeGap);
            ValidateLayout("Level 2 (Mechanical Mayhem)", LevelBuilder.Level2Layout, maxSafeGap);
            ValidateLayout("Level 3 (Ultimate Gauntlet)", LevelBuilder.Level3Layout, maxSafeGap);
        }

        private static void ValidateLayout(string levelName, LevelBuilder.ObstacleDesc[] layout, float maxSafeGap)
        {
            TestAssert.IsNotNull(layout, $"{levelName} layout must not be null.");
            TestAssert.Greater(layout.Length, 3, $"{levelName} must contain multiple obstacles.");

            float lastX = -1f;
            int checkpointCount = 0;
            bool hasFinish = false;

            for (int i = 0; i < layout.Length; i++)
            {
                var desc = layout[i];

                // Monotonic X order
                TestAssert.Greater(desc.x, lastX, $"{levelName}: Obstacle #{i} at x={desc.x} must be after previous at x={lastX}");
                lastX = desc.x;

                // Gap jumpability
                if (desc.type == "Gap")
                {
                    TestAssert.Less(desc.param1, maxSafeGap,
                        $"{levelName}: Gap #{i} size {desc.param1}m exceeds safe reach {maxSafeGap:F2}m.");
                }

                if (desc.type == "Checkpoint") checkpointCount++;
                if (desc.type == "Finish") hasFinish = true;
            }

            TestAssert.Greater(checkpointCount, 0, $"{levelName} must have at least one checkpoint.");
            TestAssert.IsTrue(hasFinish, $"{levelName} must have a finish line.");
        }
    }
}
#endif
