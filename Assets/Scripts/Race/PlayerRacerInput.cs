// PlayerRacerInput.cs
// Wraps StarterAssetsInputs for the human player.
// ThirdPersonController reads from this instead of StarterAssetsInputs directly
// when this component is present on the same GameObject.

using UnityEngine;
using StarterAssets;

namespace FunRaceParkour
{
    [RequireComponent(typeof(StarterAssetsInputs))]
    public class PlayerRacerInput : MonoBehaviour, IRacerInput
    {
        private StarterAssetsInputs _sai;
        private bool _inputLocked;

        // ------------------------------------------------------------------
        // IRacerInput
        // ------------------------------------------------------------------
        public float Move    => _inputLocked ? 0f : _sai.move.y;
        public bool  Jump    => _inputLocked ? false : _sai.jump;
        public bool  InputLocked
        {
            get => _inputLocked;
            set => _inputLocked = value;
        }

        public void ConsumeJump()
        {
            // StarterAssetsInputs keeps jump true until explicitly cleared
            _sai.jump = false;
        }

        // ------------------------------------------------------------------
        private void Awake()
        {
            _sai = GetComponent<StarterAssetsInputs>();
        }
    }
}
