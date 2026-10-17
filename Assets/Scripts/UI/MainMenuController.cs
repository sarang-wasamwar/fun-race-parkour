// MainMenuController.cs
// Drives the main menu: Play, Level Selection, How to Play, Settings, Quit.
// Uses InputSystemUIInputModule (project uses Input System).
// Initialises GameSession and ProgressionService if not already present.

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FunRaceParkour
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject levelSelectPanel;
        [SerializeField] private GameObject howToPlayPanel;
        [SerializeField] private GameObject settingsPanel;

        [Header("Main Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button levelSelectButton;
        [SerializeField] private Button howToPlayButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Level Cards")]
        [SerializeField] private LevelCard[] levelCards = new LevelCard[3];

        [Header("Settings")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private Toggle vsyncToggle;
        [SerializeField] private Button applySettingsButton;
        [SerializeField] private Button backFromSettingsButton;
        [SerializeField] private Button backFromLevelSelectButton;
        [SerializeField] private Button backFromHowToPlayButton;

        [System.Serializable]
        public class LevelCard
        {
            public Button          button;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI difficultyText;
            public TextMeshProUGUI bestTimeText;
            public GameObject      lockOverlay;
        }

        private void Awake()
        {
            // Ensure persistent services exist in this scene
            EnsurePersistentServices();

            // Main cursor visible in menu
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            Time.timeScale   = 1f;
        }

        private void Start()
        {
            // Wire buttons
            playButton?.onClick.AddListener(OnPlay);
            levelSelectButton?.onClick.AddListener(() => ShowPanel(levelSelectPanel));
            howToPlayButton?.onClick.AddListener(() => ShowPanel(howToPlayPanel));
            settingsButton?.onClick.AddListener(OnOpenSettings);
            quitButton?.onClick.AddListener(OnQuit);

            backFromSettingsButton?.onClick.AddListener(() => ShowPanel(mainPanel));
            backFromLevelSelectButton?.onClick.AddListener(() => ShowPanel(mainPanel));
            backFromHowToPlayButton?.onClick.AddListener(() => ShowPanel(mainPanel));
            applySettingsButton?.onClick.AddListener(ApplySettings);

            InitLevelCards();
            LoadSettings();
            ShowPanel(mainPanel);
        }

        private void OnDestroy()
        {
            // Unsubscribe all to prevent accumulation on scene reload
            playButton?.onClick.RemoveAllListeners();
            levelSelectButton?.onClick.RemoveAllListeners();
            howToPlayButton?.onClick.RemoveAllListeners();
            settingsButton?.onClick.RemoveAllListeners();
            quitButton?.onClick.RemoveAllListeners();
        }

        // ── Level cards ────────────────────────────────────────────────────
        private static readonly string[] LevelNames       = { "Training Run", "Mechanical Mayhem", "Ultimate Gauntlet" };
        private static readonly string[] LevelDifficulties = { "★☆☆ Easy", "★★☆ Medium", "★★★ Hard" };

        private void InitLevelCards()
        {
            for (int i = 0; i < levelCards.Length; i++)
            {
                var card = levelCards[i];
                if (card == null) continue;
                int idx = i; // capture for lambda

                bool unlocked = ProgressionService.Instance?.IsUnlocked(i) ?? (i == 0);
                float best    = ProgressionService.Instance?.GetBestTime(i) ?? -1f;

                if (card.nameText      != null) card.nameText.text       = LevelNames[i];
                if (card.difficultyText != null) card.difficultyText.text = LevelDifficulties[i];
                if (card.bestTimeText  != null)
                    card.bestTimeText.text = best >= 0f ? FormatTime(best) : "--:--";
                if (card.lockOverlay   != null) card.lockOverlay.SetActive(!unlocked);
                if (card.button        != null)
                {
                    card.button.interactable = unlocked;
                    card.button.onClick.RemoveAllListeners();
                    if (unlocked)
                        card.button.onClick.AddListener(() => StartLevel(idx));
                }
            }
        }

        private void StartLevel(int idx)
        {
            if (GameSession.Instance != null) GameSession.Instance.SelectedLevel = idx;
            SceneLoader.Instance?.LoadLevel(idx);
        }

        // ── Quick Play (Level 1 or highest unlocked) ──────────────────────
        private void OnPlay()
        {
            // Find highest unlocked level
            int levelToPlay = 0;
            for (int i = 2; i >= 0; i--)
            {
                if (ProgressionService.Instance?.IsUnlocked(i) ?? i == 0) { levelToPlay = i; break; }
            }
            StartLevel(0); // always start at level 1 for Play button
        }

        // ── Settings ───────────────────────────────────────────────────────
        private void OnOpenSettings()
        {
            LoadSettings();
            ShowPanel(settingsPanel);
        }

        private void LoadSettings()
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.value = PlayerPrefs.GetFloat("FRP_MasterVol", 1f);
            if (musicVolumeSlider != null)
                musicVolumeSlider.value = PlayerPrefs.GetFloat("FRP_MusicVol", 1f);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = PlayerPrefs.GetFloat("FRP_SFXVol", 1f);
            if (sensitivitySlider != null)
                sensitivitySlider.value = PlayerPrefs.GetFloat("FRP_Sensitivity", 7.5f);
            if (vsyncToggle != null)
                vsyncToggle.isOn = PlayerPrefs.GetInt("FRP_VSync", 1) == 1;
        }

        private void ApplySettings()
        {
            if (masterVolumeSlider != null)
                PlayerPrefs.SetFloat("FRP_MasterVol", masterVolumeSlider.value);
            if (musicVolumeSlider != null)
                PlayerPrefs.SetFloat("FRP_MusicVol", musicVolumeSlider.value);
            if (sfxVolumeSlider != null)
                PlayerPrefs.SetFloat("FRP_SFXVol", sfxVolumeSlider.value);
            if (sensitivitySlider != null)
                PlayerPrefs.SetFloat("FRP_Sensitivity", sensitivitySlider.value);
            if (vsyncToggle != null)
            {
                QualitySettings.vSyncCount = vsyncToggle.isOn ? 1 : 0;
                PlayerPrefs.SetInt("FRP_VSync", vsyncToggle.isOn ? 1 : 0);
            }

            PlayerPrefs.Save();
            if (AudioManager.Instance != null)
                AudioManager.Instance.LoadVolumeSettings();

            ShowPanel(mainPanel);
        }

        // ── Navigation ─────────────────────────────────────────────────────
        private void ShowPanel(GameObject panel)
        {
            if (mainPanel        != null) mainPanel.SetActive(panel == mainPanel);
            if (levelSelectPanel != null) levelSelectPanel.SetActive(panel == levelSelectPanel);
            if (howToPlayPanel   != null) howToPlayPanel.SetActive(panel == howToPlayPanel);
            if (settingsPanel    != null) settingsPanel.SetActive(panel == settingsPanel);
        }

        // ── Quit ───────────────────────────────────────────────────────────
        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ── Helpers ────────────────────────────────────────────────────────
        private static string FormatTime(float t)
        {
            int   m = (int)(t / 60);
            float s = t % 60f;
            return $"{m:00}:{s:00.00}";
        }

        private static void EnsurePersistentServices()
        {
            if (GameSession.Instance == null)
                new GameObject("GameSession").AddComponent<GameSession>();
            if (ProgressionService.Instance == null)
                new GameObject("ProgressionService").AddComponent<ProgressionService>();
            if (SceneLoader.Instance == null)
                new GameObject("SceneLoader").AddComponent<SceneLoader>();
            if (AudioManager.Instance == null)
                new GameObject("AudioManager").AddComponent<AudioManager>();
        }
    }
}
