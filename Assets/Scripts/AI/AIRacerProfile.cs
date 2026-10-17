// AIRacerProfile.cs
// ScriptableObject defining the variation parameters for one AI racer.
// Speed, jump height and gravity are NEVER varied here — those come from
// ThirdPersonControllerExtended, same values as the human.

using UnityEngine;

namespace FunRaceParkour
{
    [CreateAssetMenu(menuName = "FunRaceParkour/AI Racer Profile", fileName = "AIRacerProfile")]
    public class AIRacerProfile : ScriptableObject
    {
        [Header("Reaction & Timing")]
        [Tooltip("How long after a hazard becomes safe the AI waits before moving (seconds).")]
        [Range(0f, 1f)] public float reactionTime = 0.15f;

        [Tooltip("Random error added to jump take-off distance (metres). Higher = sloppier.")]
        [Range(0f, 0.4f)] public float jumpTimingError = 0.1f;

        [Header("Navigation")]
        [Tooltip("Fraction of jump reach to leave as safety margin (0=edge, 0.2=conservative).")]
        [Range(0f, 0.4f)] public float safetyMargin = 0.15f;

        [Tooltip("How long the AI waits at a standoff before re-checking (seconds).")]
        [Range(0.05f, 0.5f)] public float pollInterval = 0.1f;

        [Tooltip("How long the AI can be stuck before attempting recovery (seconds).")]
        [Range(1f, 10f)] public float stuckTimeout = 3f;

        [Tooltip("Number of stuck-recovery attempts before backing up.")]
        [Range(1, 5)] public int maxJumpRetries = 2;

        [Header("Risk")]
        [Tooltip("Extra time multiplier for safe window requirement. 1.0 = exact, 1.5 = cautious.")]
        [Range(1f, 2.5f)] public float safeWindowMultiplier = 1.5f;
    }
}
