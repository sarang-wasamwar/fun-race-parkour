// MainMenuController.cs
// Drives the main menu: Play, Level Selection, How to Play, Settings, Quit.
// Uses InputSystemUIInputModule (project uses Input System).
// Initialises GameSession and ProgressionService if not already present.

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace FunRaceParkour
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField, Tooltip("Font used by the generated menu UI.")]
        private TMP_FontAsset uiFont;
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
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Button qualityButton;
        [SerializeField] private Button resolutionButton;
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
            RuntimeUiFactory.SetFont(uiFont);
            EnsurePersistentServices();
            EnsureEventSystem();
            if (playButton == null)
                BuildDefaultUi();
            BuildPreviewBackdrop();

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
            qualityButton?.onClick.AddListener(CycleQuality);
            resolutionButton?.onClick.AddListener(CycleResolution);

            InitLevelCards();
            LoadSettings();
            bool showLevels = GameSession.Instance != null && GameSession.Instance.OpenLevelSelectOnMenu;
            if (GameSession.Instance != null) GameSession.Instance.OpenLevelSelectOnMenu = false;
            ShowPanel(showLevels ? levelSelectPanel : mainPanel);
        }

        private void OnDestroy()
        {
            // Unsubscribe all to prevent accumulation on scene reload
            playButton?.onClick.RemoveAllListeners();
            levelSelectButton?.onClick.RemoveAllListeners();
            howToPlayButton?.onClick.RemoveAllListeners();
            settingsButton?.onClick.RemoveAllListeners();
            quitButton?.onClick.RemoveAllListeners();
            qualityButton?.onClick.RemoveAllListeners();
            resolutionButton?.onClick.RemoveAllListeners();
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
            if (fullscreenToggle != null)
                fullscreenToggle.isOn = PlayerPrefs.GetInt("FRP_Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
            if (qualityButton != null)
                SetButtonCaption(qualityButton, "Quality: " + QualitySettings.names[Mathf.Clamp(
                    PlayerPrefs.GetInt("FRP_Quality", QualitySettings.GetQualityLevel()), 0,
                    QualitySettings.names.Length - 1)]);
            if (resolutionButton != null)
                SetButtonCaption(resolutionButton, "Resolution: " + GetStoredResolutionLabel());
        }

        private void CycleQuality()
        {
            int count = QualitySettings.names.Length;
            int next = (PlayerPrefs.GetInt("FRP_Quality", QualitySettings.GetQualityLevel()) + 1) % count;
            PlayerPrefs.SetInt("FRP_Quality", next);
            SetButtonCaption(qualityButton, "Quality: " + QualitySettings.names[next]);
        }

        private void CycleResolution()
        {
            var resolutions = Screen.resolutions;
            if (resolutions.Length == 0) return;
            int currentW = PlayerPrefs.GetInt("FRP_ResW", Screen.width);
            int currentH = PlayerPrefs.GetInt("FRP_ResH", Screen.height);
            int selected = 0;
            for (int i = 0; i < resolutions.Length; i++)
                if (resolutions[i].width == currentW && resolutions[i].height == currentH) selected = i;
            selected = (selected + 1) % resolutions.Length;
            PlayerPrefs.SetInt("FRP_ResW", resolutions[selected].width);
            PlayerPrefs.SetInt("FRP_ResH", resolutions[selected].height);
            SetButtonCaption(resolutionButton, $"Resolution: {resolutions[selected].width} × {resolutions[selected].height}");
        }

        private static string GetStoredResolutionLabel()
        {
            int w = PlayerPrefs.GetInt("FRP_ResW", Screen.width);
            int h = PlayerPrefs.GetInt("FRP_ResH", Screen.height);
            return $"{w} × {h}";
        }

        private static void SetButtonCaption(Button button, string caption)
        {
            if (button == null) return;
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = caption;
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
            if (fullscreenToggle != null)
                PlayerPrefs.SetInt("FRP_Fullscreen", fullscreenToggle.isOn ? 1 : 0);
            int quality = Mathf.Clamp(PlayerPrefs.GetInt("FRP_Quality", QualitySettings.GetQualityLevel()),
                0, QualitySettings.names.Length - 1);
            PlayerPrefs.SetInt("FRP_Quality", quality);
            QualitySettings.SetQualityLevel(quality, true);

            PlayerPrefs.Save();
            ServiceBootstrap.ApplyStoredSettings();

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

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        private void BuildDefaultUi()
        {
            var canvas = RuntimeUiFactory.CreateCanvas("MainMenuCanvas", transform);

            mainPanel = RuntimeUiFactory.Panel(canvas.transform, "MainPanel", new Color(0.02f, 0.05f, 0.1f, 0.55f)).gameObject;
            var title = RuntimeUiFactory.Label(mainPanel.transform, "Title", "FUN RACE & PARKOUR", 72,
                TextAlignmentOptions.Center, Color.white);
            RuntimeUiFactory.Anchor(title.rectTransform, new Vector2(0.5f, 0.86f), new Vector2(0.5f, 0.86f),
                new Vector2(1400, 90), Vector2.zero);
            title.enableVertexGradient = true;

            playButton = MakeMenuButton(mainPanel.transform, "Play", "Play / Race", 0);
            levelSelectButton = MakeMenuButton(mainPanel.transform, "Levels", "Level Selection", 1);
            howToPlayButton = MakeMenuButton(mainPanel.transform, "HowTo", "How to Play", 2);
            settingsButton = MakeMenuButton(mainPanel.transform, "Settings", "Settings", 3);
            quitButton = MakeMenuButton(mainPanel.transform, "Quit", "Quit", 4);

            levelSelectPanel = RuntimeUiFactory.Panel(canvas.transform, "LevelSelect", new Color(0.02f, 0.05f, 0.1f, 0.82f)).gameObject;
            var levelTitle = RuntimeUiFactory.Label(levelSelectPanel.transform, "LSTitle", "LEVEL SELECTION", 48,
                TextAlignmentOptions.Center, Color.white);
            RuntimeUiFactory.Anchor(levelTitle.rectTransform, new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f),
                new Vector2(900, 70), Vector2.zero);
            levelCards = new LevelCard[3];
            for (int i = 0; i < 3; i++)
            {
                var cardGo = RuntimeUiFactory.Button(levelSelectPanel.transform, $"Card{i}", "", new Vector2(360, 280)).gameObject;
                RuntimeUiFactory.Anchor(cardGo.GetComponent<RectTransform>(), new Vector2(0.2f + i * 0.3f, 0.5f),
                    new Vector2(0.2f + i * 0.3f, 0.5f), new Vector2(360, 280), Vector2.zero);
                var card = new LevelCard
                {
                    button = cardGo.GetComponent<Button>(),
                    nameText = RuntimeUiFactory.Label(cardGo.transform, "Name", "Level", 28, TextAlignmentOptions.Center, Color.white),
                    difficultyText = RuntimeUiFactory.Label(cardGo.transform, "Diff", "", 20, TextAlignmentOptions.Center, Color.cyan),
                    bestTimeText = RuntimeUiFactory.Label(cardGo.transform, "Best", "--:--", 22, TextAlignmentOptions.Center, Color.white),
                    lockOverlay = RuntimeUiFactory.Panel(cardGo.transform, "Lock", new Color(0f, 0f, 0f, 0.65f)).gameObject
                };
                var lockedLabel = RuntimeUiFactory.Label(card.lockOverlay.transform, "LockedLabel", "LOCKED", 32,
                    TextAlignmentOptions.Center, Color.white);
                RuntimeUiFactory.Stretch(lockedLabel.rectTransform);
                RuntimeUiFactory.Anchor(card.nameText.rectTransform, new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), new Vector2(320, 40), Vector2.zero);
                RuntimeUiFactory.Anchor(card.difficultyText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(320, 30), Vector2.zero);
                RuntimeUiFactory.Anchor(card.bestTimeText.rectTransform, new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), new Vector2(320, 30), Vector2.zero);
                levelCards[i] = card;
            }
            backFromLevelSelectButton = MakeBottomButton(levelSelectPanel.transform, "BackLS", "Back", 0.5f);

            howToPlayPanel = RuntimeUiFactory.Panel(canvas.transform, "HowToPlay", new Color(0.02f, 0.05f, 0.1f, 0.82f)).gameObject;
            var how = RuntimeUiFactory.Label(howToPlayPanel.transform, "HowText",
                "W / S — move along the track\nSpace — jump\nEsc — pause\n\nRace two AI on the same course. Hazards knock you back; falling respawns at the last checkpoint.\nFinish the course to unlock the next level — you do not need to win.",
                28, TextAlignmentOptions.Center, Color.white);
            RuntimeUiFactory.Anchor(how.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(1200, 500), Vector2.zero);
            backFromHowToPlayButton = MakeBottomButton(howToPlayPanel.transform, "BackHow", "Back", 0.5f);

            settingsPanel = RuntimeUiFactory.Panel(canvas.transform, "Settings", new Color(0.02f, 0.05f, 0.1f, 0.82f)).gameObject;
            masterVolumeSlider = AddLabeledSlider(settingsPanel.transform, "Master Volume", 180);
            musicVolumeSlider = AddLabeledSlider(settingsPanel.transform, "Music Volume", 120);
            sfxVolumeSlider = AddLabeledSlider(settingsPanel.transform, "Effects Volume", 60);
            sensitivitySlider = AddLabeledSlider(settingsPanel.transform, "Look Sensitivity", 0);
            sensitivitySlider.minValue = 1f;
            sensitivitySlider.maxValue = 20f;

            vsyncToggle = MakeToggle(settingsPanel.transform, "VSync", new Vector2(-220, -80));
            fullscreenToggle = MakeToggle(settingsPanel.transform, "Fullscreen", new Vector2(220, -80));

            qualityButton = RuntimeUiFactory.Button(settingsPanel.transform, "Quality", "Quality", new Vector2(340, 48));
            RuntimeUiFactory.Anchor(qualityButton.GetComponent<RectTransform>(), new Vector2(0.35f, 0.22f), new Vector2(0.35f, 0.22f),
                new Vector2(340, 48), Vector2.zero);
            resolutionButton = RuntimeUiFactory.Button(settingsPanel.transform, "Resolution", "Resolution", new Vector2(420, 48));
            RuntimeUiFactory.Anchor(resolutionButton.GetComponent<RectTransform>(), new Vector2(0.68f, 0.22f), new Vector2(0.68f, 0.22f),
                new Vector2(420, 48), Vector2.zero);

            applySettingsButton = MakeBottomButton(settingsPanel.transform, "Apply", "Apply", 0.35f);
            backFromSettingsButton = MakeBottomButton(settingsPanel.transform, "BackSet", "Back", 0.65f);
        }

        private static Button MakeMenuButton(Transform parent, string name, string caption, int index)
        {
            var button = RuntimeUiFactory.Button(parent, name, caption, new Vector2(420, 58));
            RuntimeUiFactory.Anchor(button.GetComponent<RectTransform>(), new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f),
                new Vector2(420, 58), new Vector2(0, -index * 70));
            return button;
        }

        private static Button MakeBottomButton(Transform parent, string name, string caption, float xAnchor)
        {
            var button = RuntimeUiFactory.Button(parent, name, caption, new Vector2(300, 54));
            RuntimeUiFactory.Anchor(button.GetComponent<RectTransform>(), new Vector2(xAnchor, 0.1f),
                new Vector2(xAnchor, 0.1f), new Vector2(300, 54), Vector2.zero);
            return button;
        }

        private static Slider AddLabeledSlider(Transform parent, string label, float y)
        {
            var labelText = RuntimeUiFactory.Label(parent, label + "Lbl", label, 22, TextAlignmentOptions.MidlineRight, Color.white);
            RuntimeUiFactory.Anchor(labelText.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f),
                new Vector2(240, 30), new Vector2(-390, y));
            var slider = RuntimeUiFactory.Slider(parent, label, 520);
            RuntimeUiFactory.Anchor(slider.GetComponent<RectTransform>(), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f),
                new Vector2(520, 24), new Vector2(0, y));
            return slider;
        }

        private static Toggle MakeToggle(Transform parent, string label, Vector2 pos)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Toggle), typeof(Image));
            go.transform.SetParent(parent, false);
            RuntimeUiFactory.Anchor(go.GetComponent<RectTransform>(), new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f),
                new Vector2(220, 36), pos);
            var toggle = go.GetComponent<Toggle>();
            go.GetComponent<Image>().color = new Color(0.08f, 0.18f, 0.32f, 0.92f);
            toggle.targetGraphic = go.GetComponent<Image>();
            var check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(go.transform, false);
            var checkRect = check.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0f, 0.5f);
            checkRect.anchorMax = new Vector2(0f, 0.5f);
            checkRect.pivot = new Vector2(0f, 0.5f);
            checkRect.anchoredPosition = new Vector2(8f, 0f);
            checkRect.sizeDelta = new Vector2(20f, 20f);
            toggle.graphic = check.GetComponent<Image>();
            check.GetComponent<Image>().color = new Color(0.2f, 0.9f, 1f);
            RuntimeUiFactory.Label(go.transform, "Lbl", label, 20, TextAlignmentOptions.Center, Color.white);
            return toggle;
        }

        private void BuildPreviewBackdrop()
        {
            if (GameObject.Find("MenuPreviewCamera") != null) return;

            var camGo = new GameObject("MenuPreviewCamera");
            camGo.transform.position = new Vector3(6f, 4f, -10f);
            camGo.transform.rotation = Quaternion.Euler(12f, 25f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.08f, 0.14f);
            cam.depth = -10;
            camGo.tag = "MainCamera";

            var light = new GameObject("MenuLight");
            var l = light.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(40f, -30f, 0f);

            for (int i = 0; i < 5; i++)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "PreviewObstacle";
                cube.transform.position = new Vector3(i * 2.2f, 0.5f + (i % 2) * 0.4f, 2f);
                cube.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);
                var spin = cube.AddComponent<MenuPreviewSpin>();
                spin.SetSpeed(20f + i * 8f);
            }

            var previewFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            previewFloor.name = "PreviewTrack";
            previewFloor.transform.position = new Vector3(4f, -0.35f, 2f);
            previewFloor.transform.localScale = new Vector3(16f, 0.3f, 7f);
            previewFloor.GetComponent<Renderer>().material.color = new Color(0.04f, 0.3f, 0.5f);
        }
    }

    public class MenuPreviewSpin : MonoBehaviour
    {
        [SerializeField, Tooltip("Preview obstacle rotation speed.")]
        private float degreesPerSecond = 25f;
        public void SetSpeed(float value) => degreesPerSecond = value;
        private void Update() => transform.Rotate(0f, degreesPerSecond * Time.unscaledDeltaTime, 0f);
    }
}
