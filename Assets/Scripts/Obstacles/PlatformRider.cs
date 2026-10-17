// PlatformRider.cs
// Attach to every racer. Uses OnControllerColliderHit to detect when standing
// on a moving platform and registers as a rider so the platform carries them.

using UnityEngine;

namespace FunRaceParkour
{
    [RequireComponent(typeof(CharacterController))]
    public class PlatformRider : MonoBehaviour
    {
        private CharacterController _cc;
        private MovingPlatformExtended _currentPlatform;
        private global::MovingPlatform _currentLegacyPlatform;

        private void Awake() => _cc = GetComponent<CharacterController>();

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // Only register when hitting from above (we're landing on top)
            if (hit.moveDirection.y >= -0.1f) return;

            var platform = hit.collider.GetComponent<MovingPlatformExtended>();
            var legacyPlatform = hit.collider.GetComponent<global::MovingPlatform>();

            if (platform != null && platform != _currentPlatform)
            {
                RemoveCurrentPlatform();
                _currentPlatform = platform;
                platform.AddRider(_cc);
            }
            else if (legacyPlatform != null && legacyPlatform != _currentLegacyPlatform)
            {
                RemoveCurrentPlatform();
                _currentLegacyPlatform = legacyPlatform;
                legacyPlatform.AddRider(_cc);
            }
            else if (platform == null && legacyPlatform == null)
            {
                RemoveCurrentPlatform();
            }
        }

        private void Update()
        {
            // Detect when we've left the platform (no longer grounded on it)
            bool isGrounded = true;
            var ctrlExt = GetComponent<ThirdPersonControllerExtended>();
            if (ctrlExt != null) isGrounded = ctrlExt.Grounded;
            else
            {
                var ctrl = GetComponent<StarterAssets.ThirdPersonController>();
                if (ctrl != null) isGrounded = ctrl.Grounded;
            }

            if (!isGrounded) RemoveCurrentPlatform();
        }

        private void OnDisable()
        {
            RemoveCurrentPlatform();
        }

        private void RemoveCurrentPlatform()
        {
            _currentPlatform?.RemoveRider(_cc);
            _currentLegacyPlatform?.RemoveRider(_cc);
            _currentPlatform = null;
            _currentLegacyPlatform = null;
        }
    }
}
