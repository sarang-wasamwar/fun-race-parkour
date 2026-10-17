// PlatformRider.cs
// Attach to every racer. Uses OnControllerColliderHit to detect when standing
// on a MovingPlatformExtended and registers as a rider so the platform carries them.

using UnityEngine;

namespace FunRaceParkour
{
    [RequireComponent(typeof(CharacterController))]
    public class PlatformRider : MonoBehaviour
    {
        private CharacterController _cc;
        private MovingPlatformExtended _currentPlatform;

        private void Awake() => _cc = GetComponent<CharacterController>();

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // Only register when hitting from above (we're landing on top)
            if (hit.moveDirection.y >= -0.1f) return;

            var platform = hit.collider.GetComponent<MovingPlatformExtended>();

            if (platform != null && platform != _currentPlatform)
            {
                _currentPlatform?.RemoveRider(_cc);
                _currentPlatform = platform;
                platform.AddRider(_cc);
            }
            else if (platform == null && _currentPlatform != null)
            {
                _currentPlatform.RemoveRider(_cc);
                _currentPlatform = null;
            }
        }

        private void Update()
        {
            // Detect when we've left the platform (no longer grounded on it)
            // A simple grounded check: if controller is not touching ground, leave platform
            var ctrl = GetComponent<ThirdPersonControllerExtended>();
            if (ctrl != null && !ctrl.Grounded && _currentPlatform != null)
            {
                _currentPlatform.RemoveRider(_cc);
                _currentPlatform = null;
            }
        }

        private void OnDisable()
        {
            _currentPlatform?.RemoveRider(_cc);
            _currentPlatform = null;
        }
    }
}
