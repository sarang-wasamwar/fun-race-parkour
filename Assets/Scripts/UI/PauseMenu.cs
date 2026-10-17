// PauseMenu.cs
// In-race pause: freezes time, unlocks cursor, blocks racer input.
// Resume restores everything. Restart, LevelSelect, MainMenu use SceneLoader.
// Uses unscaled time for UI animation.
// Properly unsubscribes in OnDestroy to prevent duplicate listeners on restart.

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StarterAssets;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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
            if (resumeButton == null)
                BuildDefaultUi();

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
            bool pressed;
#if ENABLE_INPUT_SYSTEM
            pressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            pressed = Input.GetKeyDown(pauseKey);
#endif
            if (pressed)
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
            SceneLoader.Instance?.LoadLevelSelection();
        }

        private void DoMainMenu()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            SceneLoader.Instance?.LoadMainMenu();
        }

        private void BuildDefaultUi()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 200;
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                gameObject.AddComponent<GraphicRaycaster>();
            }

            pausePanel = RuntimeUiFactory.Panel(transform, "PausePanel", new Color(0f, 0f, 0f, 0.7f)).gameObject;
            var title = RuntimeUiFactory.Label(pausePanel.transform, "Title", "PAUSED", 56,
                TextAlignmentOptions.Center, Color.white);
            RuntimeUiFactory.Anchor(title.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f),
                new Vector2(500, 70), Vector2.zero);

            resumeButton = RuntimeUiFactory.Button(pausePanel.transform, "Resume", "Resume", new Vector2(360, 56));
            RuntimeUiFactory.Anchor(resumeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(360, 56), Vector2.zero);
            restartButton = RuntimeUiFactory.Button(pausePanel.transform, "Restart", "Restart Level", new Vector2(360, 56));
            RuntimeUiFactory.Anchor(restartButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f), new Vector2(360, 56), Vector2.zero);
            levelSelectButton = RuntimeUiFactory.Button(pausePanel.transform, "Levels", "Level Selection", new Vector2(360, 56));
            RuntimeUiFactory.Anchor(levelSelectButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), new Vector2(360, 56), Vector2.zero);
            mainMenuButton = RuntimeUiFactory.Button(pausePanel.transform, "Menu", "Main Menu", new Vector2(360, 56));
            RuntimeUiFactory.Anchor(mainMenuButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f), new Vector2(360, 56), Vector2.zero);

            pausePanel.SetActive(false);
        }
    }
}
