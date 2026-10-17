// SceneLoader.cs
// Single entry point for scene transitions. Resets timeScale and cursor on every load.

using UnityEngine;
using UnityEngine.SceneManagement;

namespace FunRaceParkour
{
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void LoadScene(string sceneName)
        {
            ResetState();
            SceneManager.LoadScene(sceneName);
        }

        public void LoadScene(int buildIndex)
        {
            ResetState();
            SceneManager.LoadScene(buildIndex);
        }

        public void LoadMainMenu()   => LoadScene("MainMenu");
        public void LoadLevel(int i) => LoadScene("Level" + (i + 1) + "_" + LevelName(i));

        private static string LevelName(int i) => i switch
        {
            0 => "TrainingRun",
            1 => "MechanicalMayhem",
            2 => "UltimateGauntlet",
            _ => "TrainingRun"
        };

        private static void ResetState()
        {
            Time.timeScale   = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
    }
}
