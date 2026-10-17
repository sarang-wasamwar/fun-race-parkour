// FixedTrackCamera.cs
// Ensures the Cinemachine virtual camera stays locked directly behind the player
// at a fixed third-person angle along the track (+X direction), disabling mouse look.

using UnityEngine;
using Unity.Cinemachine;

namespace FunRaceParkour
{
    [ExecuteAlways]
    public class FixedTrackCamera : MonoBehaviour
    {
        [Header("Camera Lock Settings")]
        [Tooltip("Fixed camera distance behind the player.")]
        public float cameraDistance = 6.0f;

        [Tooltip("Height offset of the camera arm.")]
        public float verticalArmLength = 0.35f;

        [Tooltip("Track forward yaw (90 degrees for +X direction).")]
        public float trackYaw = 90.0f;

        [Tooltip("Downward pitch angle.")]
        public float cameraPitch = 12.0f;

        private CinemachineThirdPersonFollow _thirdPersonFollow;
        private CinemachineCamera _vcam;

        private void Awake()
        {
            ApplySettings();
        }

        private void Start()
        {
            ApplySettings();
        }

        private void LateUpdate()
        {
            ApplySettings();
        }

        public void ApplySettings()
        {
            if (_vcam == null) _vcam = GetComponent<CinemachineCamera>();
            if (_thirdPersonFollow == null) _thirdPersonFollow = GetComponent<CinemachineThirdPersonFollow>();

            if (_thirdPersonFollow != null)
            {
                // Lock directly behind: centered, no lateral shoulder offset
                _thirdPersonFollow.CameraSide = 0.5f;
                _thirdPersonFollow.ShoulderOffset = Vector3.zero;
                _thirdPersonFollow.CameraDistance = cameraDistance;
                _thirdPersonFollow.VerticalArmLength = verticalArmLength;
            }
        }
    }
}
