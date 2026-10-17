// RacerSetup.cs
// Converts a PlayerRobot instance into either a human or AI racer.

using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using StarterAssets;

namespace FunRaceParkour
{
    public static class RacerSetup
    {
        public static void Prepare(GameObject go, bool isPlayer, AIRacerProfile aiProfile)
        {
            if (go.GetComponent<PlatformRider>() == null)
                go.AddComponent<PlatformRider>();
            if (go.GetComponent<PlatformContactReporter>() == null)
                go.AddComponent<PlatformContactReporter>();

            if (isPlayer)
            {
                go.tag = "Player";
                if (go.GetComponent<PlayerRacerInput>() == null)
                    go.AddComponent<PlayerRacerInput>();
                var playerSeam = go.GetComponent<PlayerRacerInput>();
                var tpc = go.GetComponent<ThirdPersonController>();
                if (tpc != null)
                {
                    tpc.MoveSpeed = 2.0f;
                    tpc.JumpHeight = 1.2f;
                    tpc.Gravity = -15f;
                    tpc.JumpTimeout = 0.5f;
                    tpc.SetRacerInput(playerSeam);
                }
            }
            else
            {
                go.tag = "Untagged";
#if ENABLE_INPUT_SYSTEM
                var playerInput = go.GetComponent<PlayerInput>();
                if (playerInput != null)
                    Object.Destroy(playerInput);
#endif
                var starterInput = go.GetComponent<StarterAssetsInputs>();
                if (starterInput != null)
                    starterInput.enabled = false;

                var respawn = go.GetComponent<RespawnPlayer>();
                if (respawn != null)
                    Object.Destroy(respawn);

                var tpc = go.GetComponent<ThirdPersonController>();
                if (tpc != null)
                    tpc.enabled = false;

                var ext = go.GetComponent<ThirdPersonControllerExtended>();
                if (ext == null)
                    ext = go.AddComponent<ThirdPersonControllerExtended>();
                ext.MoveSpeed = tpc != null ? tpc.MoveSpeed : 2.0f;
                ext.JumpHeight = tpc != null ? tpc.JumpHeight : 1.2f;
                ext.Gravity = tpc != null ? tpc.Gravity : -15f;
                ext.JumpTimeout = tpc != null ? tpc.JumpTimeout : 0.5f;
                ext.SpeedChangeRate = tpc != null ? tpc.SpeedChangeRate : 10f;
                ext.GroundLayers = tpc != null ? tpc.GroundLayers : LayerMask.GetMask("Default");
                ext.LandingAudioClip = tpc != null ? tpc.LandingAudioClip : null;
                ext.FootstepAudioClips = tpc != null ? tpc.FootstepAudioClips : null;
                ext.CinemachineCameraTarget = null;
                ext.enabled = true;

                var ai = go.GetComponent<AIRacerInput>();
                if (ai == null)
                    ai = go.AddComponent<AIRacerInput>();
                ai.SetProfile(aiProfile);
                ext.SetInputSeam(ai);

                if (go.GetComponent<RacerRespawnMonitor>() == null)
                    go.AddComponent<RacerRespawnMonitor>();
            }
        }

        public static void ResetBody(RacerData data, Vector3 position, Quaternion rotation)
        {
            if (data.controller != null)
                data.controller.ResetForRace(position, rotation);
            else if (data.starterController != null)
                data.starterController.ResetForRace(position, rotation);
        }
    }
}
