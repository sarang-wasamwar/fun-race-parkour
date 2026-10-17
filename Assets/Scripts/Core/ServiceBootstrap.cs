// ServiceBootstrap.cs
// Ensures the persistent services exist even when a level is opened directly.

using UnityEngine;

namespace FunRaceParkour
{
    public static class ServiceBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Ensure()
        {
            if (GameSession.Instance == null)
                new GameObject("GameSession").AddComponent<GameSession>();
            if (ProgressionService.Instance == null)
                new GameObject("ProgressionService").AddComponent<ProgressionService>();
            if (SceneLoader.Instance == null)
                new GameObject("SceneLoader").AddComponent<SceneLoader>();
            if (AudioManager.Instance == null)
                new GameObject("AudioManager").AddComponent<AudioManager>();

            ApplyStoredSettings();
        }

        public static void ApplyStoredSettings()
        {
            QualitySettings.vSyncCount = PlayerPrefs.GetInt("FRP_VSync", 1);
            int quality = PlayerPrefs.GetInt("FRP_Quality", QualitySettings.GetQualityLevel());
            quality = Mathf.Clamp(quality, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(quality, true);

            int fullscreen = PlayerPrefs.GetInt("FRP_Fullscreen", Screen.fullScreen ? 1 : 0);
            int w = PlayerPrefs.GetInt("FRP_ResW", Screen.width);
            int h = PlayerPrefs.GetInt("FRP_ResH", Screen.height);
            Screen.SetResolution(w, h, fullscreen == 1);

            AudioManager.Instance?.LoadVolumeSettings();
        }
    }
}
