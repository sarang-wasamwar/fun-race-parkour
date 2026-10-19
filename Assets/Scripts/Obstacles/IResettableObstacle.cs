// IResettableObstacle.cs
// All obstacles implement this so RaceManager.Restart() can reset them.

namespace FunRaceParkour
{
    public interface IResettableObstacle
    {
        void ResetState();
    }
}
