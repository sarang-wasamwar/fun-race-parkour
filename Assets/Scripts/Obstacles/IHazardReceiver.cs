// IHazardReceiver.cs  
// Implemented by every racer. Hazards call this to push or stun racers.
// Human and AI share the same implementation (ThirdPersonControllerExtended).

using UnityEngine;

namespace FunRaceParkour
{
    /// <summary>
    /// Applied to every racer GameObject. Hazards call ApplyImpact to push or stun.
    /// X impulse = lateral push (clamped to X axis), Y impulse = upward pop,
    /// stunSeconds = time the racer's own input is suppressed.
    /// No instant kill, no teleport.
    /// </summary>
    public interface IHazardReceiver
    {
        void ApplyImpact(Vector3 impulse, float stunSeconds);
    }
}
