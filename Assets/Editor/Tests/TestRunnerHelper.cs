// TestRunnerHelper.cs
// Editor menu runner for FunRaceParkour test suite: Tools/FunRace/Run All Tests.
// Runs all EditMode tests, measures execution time, and logs formatted results.

#if UNITY_EDITOR
using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace FunRaceParkour.Tests
{
    public static class TestRunnerHelper
    {
        [MenuItem("Tools/FunRace/Run All Tests")]
        public static void RunAllTests()
        {
            var stopwatch = Stopwatch.StartNew();
            int suitesPassed = 0;
            int suitesFailed = 0;

            Debug.Log("<color=#4FC3F7><b>[TestRunner] Starting Fun Race & Parkour test suite...</b></color>");

            RunSuite("Jump Math & Kinematics", JumpMathTests.RunAll, ref suitesPassed, ref suitesFailed);
            RunSuite("Race Ranking", RaceRankingTests.RunAll, ref suitesPassed, ref suitesFailed);
            RunSuite("Progression & Unlocks",  ProgressionServiceTests.RunAll, ref suitesPassed, ref suitesFailed);
            RunSuite("Hazard Prediction",     HazardPredictionTests.RunAll, ref suitesPassed, ref suitesFailed);
            RunSuite("Track & Gap Safety",    TrackValidationTests.RunAll, ref suitesPassed, ref suitesFailed);

            stopwatch.Stop();

            if (suitesFailed == 0)
            {
                Debug.Log($"<color=#66BB6A><b>[TestRunner] PASSED: All {suitesPassed} test suites succeeded in {stopwatch.ElapsedMilliseconds} ms!</b></color>");
            }
            else
            {
                Debug.LogError($"<color=#EF5350><b>[TestRunner] FAILED: {suitesFailed} suite(s) failed, {suitesPassed} passed in {stopwatch.ElapsedMilliseconds} ms.</b></color>");
            }
        }

        private static void RunSuite(string name, Action testAction, ref int passed, ref int failed)
        {
            try
            {
                testAction();
                Debug.Log($"<color=#81C784>[PASS]</color> {name}");
                passed++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"<color=#E57373>[FAIL]</color> {name}: {ex.Message}\n{ex.StackTrace}");
                failed++;
            }
        }
    }
}
#endif
