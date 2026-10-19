// IRacerInput.cs
// Input seam: decouples ThirdPersonController from PlayerInput vs. AI.
// Human: PlayerRacerInput wraps StarterAssetsInputs.
// AI:    AIRacerInput drives movement.

namespace FunRaceParkour
{
    /// <summary>
    /// Abstraction between the ThirdPersonController and its input source.
    /// </summary>
    public interface IRacerInput
    {
        /// <summary>Forward/back axis [-1,1]. Positive = toward finish.</summary>
        float Move { get; }

        /// <summary>True for one frame when a jump is requested.</summary>
        bool Jump { get; }

        /// <summary>Called by the controller after consuming Jump so it is not re-read.</summary>
        void ConsumeJump();

        /// <summary>True while the racer's input should be suppressed (countdown, pause).</summary>
        bool InputLocked { get; set; }
    }
}
