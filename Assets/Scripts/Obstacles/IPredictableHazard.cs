// IPredictableHazard.cs
// Implemented by all timed hazards. AI queries this to predict safe windows.
// ALL timing is a deterministic function of Time.time (no hidden state),
// so the AI reads the same information the human sees visually.

namespace FunRaceParkour
{
    /// <summary>
    /// Timed hazards implement this so the AI can predict safety without cheating.
    /// The implementation must be a pure function of (Time.time + offset) / period.
    /// </summary>
    public interface IPredictableHazard
    {
        /// <summary>
        /// Returns true if xPosition in the racer's lane is safe to occupy
        /// starting <paramref name="timeFromNow"/> seconds from now, for a crossing
        /// duration of <paramref name="crossDuration"/> seconds.
        /// </summary>
        bool IsSafeAt(float xPosition, float timeFromNow, float crossDuration);

        /// <summary>
        /// Returns the soonest time (>= 0) from now when the window described by
        /// IsSafeAt will be true for at least crossDuration seconds.
        /// Returns -1 if no safe window exists within the search horizon.
        /// </summary>
        float NextSafeWindow(float xPosition, float crossDuration, float searchHorizon = 10f);
    }
}
