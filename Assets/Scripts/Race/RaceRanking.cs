// RaceRanking.cs
// Pure ranking rules used by RaceManager and EditMode tests.
// Finished racers rank by finish time. Unfinished racers rank by progress.
// DNF never invents a finish time.

using System.Collections.Generic;

namespace FunRaceParkour
{
    public static class RaceRanking
    {
        public static List<RacerData> Rank(IList<RacerData> racers)
        {
            var finished = new List<RacerData>();
            var unfinished = new List<RacerData>();

            for (int i = 0; i < racers.Count; i++)
            {
                var racer = racers[i];
                if (racer.status == RacerStatus.Finished)
                    finished.Add(racer);
                else
                    unfinished.Add(racer);
            }

            finished.Sort((a, b) =>
            {
                int byTime = a.finishTime.CompareTo(b.finishTime);
                if (byTime != 0) return byTime;
                int byCrossing = a.finishOrder.CompareTo(b.finishOrder);
                return byCrossing != 0 ? byCrossing : string.CompareOrdinal(a.name, b.name);
            });
            unfinished.Sort((a, b) =>
            {
                int byProgress = b.progress.CompareTo(a.progress);
                return byProgress != 0 ? byProgress : string.CompareOrdinal(a.name, b.name);
            });

            var ranked = new List<RacerData>(racers.Count);
            ranked.AddRange(finished);
            ranked.AddRange(unfinished);
            for (int i = 0; i < ranked.Count; i++)
                ranked[i].finalRank = i + 1;
            return ranked;
        }
    }
}
