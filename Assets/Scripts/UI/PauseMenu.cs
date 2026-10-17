// PauseMenu.cs
// In-race pause: freezes time, unlocks cursor, blocks racer input.
// Resume restores everything. Restart, LevelSelect, MainMenu use SceneLoader.
// Uses unscaled time for UI animation.
// Properly unsubscribes in OnDestroy to prevent duplicate listeners on restart.

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StarterAssets;

namespace FunRaceParkour
{
    public class PauseMenu : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject pausePanel;

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button levelSelectButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Pause Input Key")]
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

        private bool _isPaused;
        private StarterAssetsInputs _playerInput;

        private void Awake()
        {
            // Find player input on the scene's player
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _playerInput = player.GetComponent<StarterAssetsInputs>();

            resumeButton?.onClick.AddListener(Resume);
            restartButton?.onClick.AddListener(DoRestart);
            levelSelectButton?.onClick.AddListener(DoLevelSelect);
            mainMenuButton?.onClick.AddListener(DoMainMenu);

            if (pausePanel != null) pausePanel.SetActive(false);
        }

        private void OnDestroy()
        {
            resumeButton?.onClick.RemoveListener(Resume);
            restartButton?.onClick.RemoveListener(DoRestart);
            levelSelectButton?.onClick.RemoveListener(DoLevelSelect);
            mainMenuButton?.onClick.RemoveListener(DoMainMenu);
        }

        private void Update()
        {
            if (Input.GetKeyDown(pauseKey))
            {
                if (_isPaused) Resume(); else Pause();
            }
        }

        public void Pause()
        {
            _isPaused        = true;
            Time.timeScale   = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            if (_playerInput != null) _playerInput.cursorLocked = false;

            // Lock all racer inputs
            if (RaceManager.Instance != null)
                foreach (var d in RaceManager.Instance.Racers)
                    if (d.inputSource != null) d.inputSource.InputLocked = true;

            if (pausePanel != null) pausePanel.SetActive(true);
        }

        public void Resume()
        {
            _isPaused        = false;
            Time.timeScale   = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
            if (_playerInput != null) _playerInput.cursorLocked = true;

            // Restore racer inputs (only during Racing state)
            if (RaceManager.Instance != null && RaceManager.Instance.State == RaceState.Racing)
                foreach (var d in RaceManager.Instance.Racers)
                    if (d.inputSource != null) d.inputSource.InputLocked = false;

            if (pausePanel != null) pausePanel.SetActive(false);
        }

        private void DoRestart()
        {
            Resume();
            RaceManager.Instance?.Restart();
        }

        private void DoLevelSelect()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            SceneLoader.Instance?.LoadScene("MainMenu");
        }

        private void DoMainMenu()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            SceneLoader.Instance?.LoadMainMenu();
        }
    }
}
