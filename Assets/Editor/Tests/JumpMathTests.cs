// JumpMathTests.cs
// Unit tests for kinematic jump reach and air duration formulas
// used by AIRacerInput and level gap validation.

#if UNITY_EDITOR
using UnityEngine;

namespace FunRaceParkour.Tests
{
    public static class JumpMathTests
    {
        public static void RunAll()
        {
            TestComputeJumpDuration_ExactValues();
            TestComputeJumpReach_StandardParameters();
            TestSafetyMargin_ReducesReach();
            TestZeroSafetyMargin_GivesFullKinematicDistance();
        }

        public static void TestComputeJumpDuration_ExactValues()
        {
            // height = 1.2, gravity = -15
            // v0 = sqrt(1.2 * -2 * -15) = sqrt(36) = 6.0
            // tPeak = 6 / 15 = 0.4
            // duration = 0.8
            float duration = AIRacerInput.ComputeJumpDuration(1.2f, -15f);
            TestAssert.AreApproximatelyEqual(0.8f, duration, 0.001f, "Standard jump air duration must be 0.8s");
        }

        public static void TestComputeJumpReach_StandardParameters()
        {
            // moveSpeed = 2.0, duration = 0.8, margin = 0.15
            // reach = 2.0 * 0.8 * (1 - 0.15) = 1.6 * 0.85 = 1.36
            float reach = AIRacerInput.ComputeJumpReach(2.0f, 1.2f, -15f, 0.15f);
            TestAssert.AreApproximatelyEqual(1.36f, reach, 0.001f, "Jump reach at 2m/s with 15% safety margin must be 1.36m");
        }

        public static void TestSafetyMargin_ReducesReach()
        {
            float reachConservative = AIRacerInput.ComputeJumpReach(2.0f, 1.2f, -15f, 0.25f);
            float reachAggressive   = AIRacerInput.ComputeJumpReach(2.0f, 1.2f, -15f, 0.05f);

            TestAssert.Less(reachConservative, reachAggressive, "Higher safety margin must yield shorter safe reach");
        }

        public static void TestZeroSafetyMargin_GivesFullKinematicDistance()
        {
            float fullDistance = AIRacerInput.ComputeJumpReach(2.0f, 1.2f, -15f, 0f);
            TestAssert.AreApproximatelyEqual(1.6f, fullDistance, 0.001f, "0% margin should equal theoretical maximum 1.6m");
        }
    }
}
#endif
