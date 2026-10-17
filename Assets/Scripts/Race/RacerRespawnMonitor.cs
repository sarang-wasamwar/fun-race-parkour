// RacerRespawnMonitor.cs
// Monitors Y threshold and requests a respawn from RaceManager for any racer.
// Replaces the per-script logic in RespawnPlayer for race scenes.
// Works for both human (with camera reset) and AI racers.

using UnityEngine;
using Unity.Cinemachine;
using StarterAssets;

namespace FunRaceParkour
{
    public class RacerRespawnMonitor : MonoBehaviour
    {
        [Tooltip("Y position below which a fall is detected.")]
        [SerializeField] private float yThreshold = -5f;

        [Tooltip("Respawn sound (human only; leave null for AI).")]
        [SerializeField] private AudioClip respawnSound;

        [Tooltip("Reference to the Cinemachine virtual camera (human only).")]
        [SerializeField] private CinemachineCamera vCam;

        private ThirdPersonControllerExtended _controller;
        private CharacterController           _cc;
        private bool _respawning;

        private void Awake()
        {
            _controller = GetComponent<ThirdPersonControllerExtended>();
            _cc         = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (_respawning) return;
            if (transform.position.y < yThreshold)
                TriggerRespawn();
        }

        private void TriggerRespawn()
        {
            _respawning = true;
            var rm   = RaceManager.Instance;
            var data = rm?.GetRacerData(gameObject);

            if (data != null)
            {
                data.falls++;
                rm.RequestRespawn(data);
            }
            else
            {
                // Fallback: respawn at scene start (backwards compat)
                if (_cc != null) _cc.enabled = false;
                transform.position = Vector3.up;
                if (_cc != null) _cc.enabled = true;
                if (_controller != null) _controller.SetVerticalVelocity(0f);
            }

            if (respawnSound != null)
                AudioSource.PlayClipAtPoint(respawnSound, transform.position);

            if (_controller != null && vCam != null)
                _controller.ResetCameraRotation(90f);

            // Allow re-trigger after short delay
            Invoke(nameof(ClearRespawning), 0.5f);
        }

        private void ClearRespawning() => _respawning = false;
    }
}
