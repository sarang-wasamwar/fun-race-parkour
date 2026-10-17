// PlatformContactReporter.cs
// Add to every racer. Uses OnControllerColliderHit to detect when the racer is
// standing on a DisappearingPlatform and report contact to it.
// CharacterController does not fire OnCollisionEnter, so this is the correct approach.

using UnityEngine;

namespace FunRaceParkour
{
    [RequireComponent(typeof(CharacterController))]
    public class PlatformContactReporter : MonoBehaviour
    {
        private DisappearingPlatform _currentPlatform;

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // Only care about contact from below (landing on top)
            if (hit.moveDirection.y >= -0.1f) return;

            var dp = hit.collider.GetComponent<DisappearingPlatform>();
            if (dp != null && dp != _currentPlatform)
            {
                _currentPlatform?.RemoveContact(gameObject);
                _currentPlatform = dp;
                dp.RegisterContact(gameObject);
            }
            else if (dp == null && _currentPlatform != null)
            {
                _currentPlatform.RemoveContact(gameObject);
                _currentPlatform = null;
            }
        }

        private void OnDisable()
        {
            _currentPlatform?.RemoveContact(gameObject);
            _currentPlatform = null;
        }
    }
}
