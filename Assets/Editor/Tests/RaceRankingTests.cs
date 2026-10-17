#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace FunRaceParkour.Tests
{
    public static class RaceRankingTests
    {
        public static void RunAll()
        {
            var racers = new List<RacerData>
            {
                new() { name = "Player", status = RacerStatus.Finished, finishTime = 20f, finishOrder = 0 },
                new() { name = "AI Racer 2", status = RacerStatus.DNF, progress = 0.9f, finishTime = -1f },
                new() { name = "AI Racer 1", status = RacerStatus.Finished, finishTime = 20f, finishOrder = 1 },
                new() { name = "Late Racer", status = RacerStatus.Finished, finishTime = 25f },
            };

            var ranked = RaceRanking.Rank(racers);
            TestAssert.AreEqual("Player", ranked[0].name, "Equal-time finishers rank by actual crossing order.");
            TestAssert.AreEqual("AI Racer 1", ranked[1].name, "Finished racers keep their crossing order.");
            TestAssert.AreEqual("Late Racer", ranked[2].name, "Finished racers rank by finish time.");
            TestAssert.AreEqual("AI Racer 2", ranked[3].name, "Unfinished racers rank by progress.");
            TestAssert.AreApproximatelyEqual(-1f, ranked[3].finishTime, 0.001f,
                "DNF must keep the no-finish-time sentinel.");
            TestAssert.AreEqual(4, ranked[3].finalRank, "Final ranks are assigned after sorting.");
        }
    }
}
#endif
