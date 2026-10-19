// RaceHUD.cs
// Single HUD MonoBehaviour per race scene.
// Shows: countdown, timer, position, per-racer progress bars, leaderboard, results.
// Extended cleanly — no duplicate subscriptions (uses OnEnable/OnDisable pattern).

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FunRaceParkour
{
    public class RaceHUD : MonoBehaviour
    {
        [SerializeField, Tooltip("Font used by the generated race UI.")]
        private TMP_FontAsset uiFont;
        // ── Inspector references ───────────────────────────────────────────
        [Header("Countdown")]
        [SerializeField] private GameObject   countdownPanel;
        [SerializeField] private TextMeshProUGUI countdownText;

        [Header("Race Info")]
        [SerializeField] private GameObject   raceInfoPanel;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI positionText;

        [Header("Progress Bars (3 entries)")]
        [SerializeField] private RacerProgressEntry[] progressEntries = new RacerProgressEntry[3];

        [Header("Checkpoint")]
        [SerializeField] private TextMeshProUGUI checkpointText;
        [SerializeField] private float           checkpointDisplayTime = 2f;

        [Header("Results Screen")]
        [SerializeField] private GameObject   resultsPanel;
        [SerializeField] private TextMeshProUGUI resultsTitleText;
        [SerializeField] private TextMeshProUGUI resultsHintText;
        [SerializeField] private Transform    resultsContainer;
        [SerializeField] private GameObject   resultEntryPrefab;
        [SerializeField] private Button       retryButton;
        [SerializeField] private Button       nextLevelButton;
        [SerializeField] private Button       levelSelectButton;
        [SerializeField] private Button       mainMenuButton;

        // ── Runtime ────────────────────────────────────────────────────────
        private List<RacerData> _racers;
        private Coroutine _checkpointRoutine;
        private RaceManager _rm;

        [System.Serializable]
        public class RacerProgressEntry
        {
            public TextMeshProUGUI nameText;
            public Slider          progressBar;
            public TextMeshProUGUI statusText;
            public Image           barFill;
        }

        // ── Init ───────────────────────────────────────────────────────────
        private void Awake()
        {
            RuntimeUiFactory.SetFont(uiFont);
            EnsureBuilt();
        }

        public void Initialise(List<RacerData> racers)
        {
            EnsureBuilt();
            _racers = racers;
            _rm     = RaceManager.Instance;

            // Hook buttons
            retryButton?.onClick.RemoveAllListeners();
            nextLevelButton?.onClick.RemoveAllListeners();
            levelSelectButton?.onClick.RemoveAllListeners();
            mainMenuButton?.onClick.RemoveAllListeners();

            retryButton?.onClick.AddListener(() => _rm?.Restart());
            nextLevelButton?.onClick.AddListener(() =>
            {
                int next = (GameSession.Instance?.SelectedLevel ?? 0) + 1;
                SceneLoader.Instance?.LoadLevel(next);
            });
            levelSelectButton?.onClick.AddListener(() => SceneLoader.Instance?.LoadLevelSelection());
            mainMenuButton?.onClick.AddListener(() => SceneLoader.Instance?.LoadMainMenu());
            retryButton?.onClick.AddListener(PlayButtonClick);
            nextLevelButton?.onClick.AddListener(PlayButtonClick);
            levelSelectButton?.onClick.AddListener(PlayButtonClick);
            mainMenuButton?.onClick.AddListener(PlayButtonClick);

            // Set up progress entries
            for (int i = 0; i < progressEntries.Length && i < racers.Count; i++)
            {
                var entry = progressEntries[i];
                var data  = racers[i];
                if (entry.nameText   != null) entry.nameText.text  = data.name;
                if (entry.barFill    != null) entry.barFill.color  = data.color;
                if (entry.progressBar != null) entry.progressBar.value = 0f;
            }

            SetPanel(countdownPanel, false);
            SetPanel(raceInfoPanel,  false);
            SetPanel(resultsPanel,   false);
        }

        // ── Countdown ─────────────────────────────────────────────────────
        public void ShowCountdown(int value)
        {
            SetPanel(countdownPanel, true);
            if (countdownText == null) return;

            if (value <= 0)
            {
                countdownText.text  = "GO!";
                countdownText.color = Color.green;
                StartCoroutine(HideCountdownAfter(0.8f));
            }
            else
            {
                countdownText.text  = value.ToString();
                countdownText.color = Color.white;
            }
        }

        private IEnumerator HideCountdownAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            SetPanel(countdownPanel, false);
            SetPanel(raceInfoPanel,  true);
        }

        // ── Race state ─────────────────────────────────────────────────────
        public void UpdateRaceState(List<RacerData> racers, float timer)
        {
            if (timerText != null)
            {
                int minutes = (int)(timer / 60f);
                float seconds = timer - minutes * 60f;
                timerText.SetText("{0:00}:{1:00.00}", minutes, seconds);
            }
            if (positionText != null)
            {
                RacerData player = null;
                for (int i = 0; i < racers.Count; i++)
                {
                    if (racers[i].racerObject != null && racers[i].racerObject.CompareTag("Player"))
                    {
                        player = racers[i];
                        break;
                    }
                }
                int pos = 1;
                if (player != null)
                {
                    for (int i = 0; i < racers.Count; i++)
                        if (racers[i] != player && racers[i].progress > player.progress) pos++;
                }
                positionText.text = OrdinalString(pos);
            }

            for (int i = 0; i < progressEntries.Length && i < racers.Count; i++)
            {
                var entry = progressEntries[i];
                var data  = racers[i];
                if (entry.progressBar != null) entry.progressBar.value = data.progress;
                if (entry.statusText  != null)
                {
                    if (data.status == RacerStatus.Finished)
                    {
                        int minutes = (int)(data.finishTime / 60f);
                        float seconds = data.finishTime - minutes * 60f;
                        entry.statusText.SetText("{0:00}:{1:00.00}", minutes, seconds);
                    }
                    else
                    {
                        entry.statusText.text = data.status == RacerStatus.DNF ? "DNF" : "";
                    }
                }
            }
        }

        // ── Checkpoint notification ────────────────────────────────────────
        public void ShowCheckpointNotification(string racerName, int checkpointIndex)
        {
            if (checkpointText == null) return;
            if (_checkpointRoutine != null) StopCoroutine(_checkpointRoutine);
            _checkpointRoutine = StartCoroutine(ShowCheckpointRoutine(racerName, checkpointIndex));
        }

        private IEnumerator ShowCheckpointRoutine(string racerName, int checkpointIndex)
        {
            checkpointText.text    = $"{racerName} — Checkpoint {checkpointIndex + 1}";
            checkpointText.enabled = true;
            yield return new WaitForSeconds(checkpointDisplayTime);
            checkpointText.enabled = false;
        }

        // ── Results ────────────────────────────────────────────────────────
        public void ShowResults(List<RacerData> racers)
        {
            SetPanel(raceInfoPanel, false);
            SetPanel(resultsPanel,  true);

            // Determine if player won
            var player = racers.Find(d => d.racerObject != null && d.racerObject.CompareTag("Player"));
            bool playerWon    = player != null && player.finalRank == 1;
            bool playerFinished = player != null && player.status == RacerStatus.Finished;

            var winner = racers.Find(d => d.finalRank == 1);
            if (resultsTitleText != null)
            {
                if (playerWon) resultsTitleText.text = "VICTORY!";
                else if (winner != null) resultsTitleText.text = $"DEFEAT — {winner.name} wins";
                else resultsTitleText.text = "DEFEAT";
            }

            // Clear old entries
            if (resultsContainer != null)
            {
                foreach (Transform t in resultsContainer) Destroy(t.gameObject);

                // Populate results
                var sorted = new List<RacerData>(racers);
                sorted.Sort((a, b) => a.finalRank.CompareTo(b.finalRank));

                foreach (var r in sorted)
                {
                    if (resultEntryPrefab == null) break;
                    var go   = Instantiate(resultEntryPrefab, resultsContainer);
                    go.SetActive(true);
                    var txts = go.GetComponentsInChildren<TextMeshProUGUI>();
                    if (txts.Length >= 3)
                    {
                        txts[0].text = r.finalRank.ToString();
                        txts[1].text = r.name;
                        txts[2].text = r.status == RacerStatus.Finished
                            ? FormatTime(r.finishTime)
                            : "DNF";
                        if (txts.Length >= 4 && r.status == RacerStatus.Finished)
                        {
                            float gap = Mathf.Max(0f, r.finishTime - winner.finishTime);
                            txts[3].text = r == winner ? "WINNER" : $"+{gap:0.00}s";
                        }
                    }
                }
            }

            // Show Next Level button only if unlocked
            if (nextLevelButton != null)
            {
                int nextLevel = (GameSession.Instance?.SelectedLevel ?? 0) + 1;
                bool nextUnlocked = ProgressionService.Instance?.IsUnlocked(nextLevel) ?? false;
                nextLevelButton.gameObject.SetActive(nextUnlocked && nextLevel < 3);
            }

            if (resultsHintText != null)
                resultsHintText.text = playerFinished
                    ? "Next level unlocked by finishing the course."
                    : "Finish the course (you do not need to win) to unlock the next level.";
        }

        public void HideResults() => SetPanel(resultsPanel, false);

        // ── Helpers ────────────────────────────────────────────────────────
        private static string FormatTime(float t)
        {
            int   m = (int)(t / 60);
            float s = t % 60f;
            return $"{m:00}:{s:00.00}";
        }

        private static string OrdinalString(int n) => n switch
        {
            1 => "1st", 2 => "2nd", 3 => "3rd", _ => $"{n}th"
        };

        private static void SetPanel(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }

        private static void PlayButtonClick() => AudioManager.Instance?.PlayButtonClick();

        private void EnsureBuilt()
        {
            if (timerText != null) return;

            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                gameObject.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode =
                    UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            countdownPanel = new GameObject("CountdownPanel", typeof(RectTransform));
            countdownPanel.transform.SetParent(transform, false);
            RuntimeUiFactory.Stretch(countdownPanel.GetComponent<RectTransform>());
            countdownText = RuntimeUiFactory.Label(countdownPanel.transform, "Countdown", "3", 120,
                TextAlignmentOptions.Center, Color.white);
            RuntimeUiFactory.Stretch(countdownText.rectTransform);

            raceInfoPanel = new GameObject("RaceInfo", typeof(RectTransform));
            raceInfoPanel.transform.SetParent(transform, false);
            RuntimeUiFactory.Stretch(raceInfoPanel.GetComponent<RectTransform>());

            timerText = RuntimeUiFactory.Label(raceInfoPanel.transform, "Timer", "00:00.00", 42,
                TextAlignmentOptions.Top, Color.white);
            RuntimeUiFactory.Anchor(timerText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(400, 60), new Vector2(0, -40));

            positionText = RuntimeUiFactory.Label(raceInfoPanel.transform, "Position", "1st", 36,
                TextAlignmentOptions.TopLeft, Color.white);
            RuntimeUiFactory.Anchor(positionText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(200, 50), new Vector2(140, -40));

            checkpointText = RuntimeUiFactory.Label(raceInfoPanel.transform, "Checkpoint", "", 28,
                TextAlignmentOptions.Center, new Color(0.4f, 1f, 1f));
            RuntimeUiFactory.Anchor(checkpointText.rectTransform, new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f),
                new Vector2(700, 40), Vector2.zero);

            progressEntries = new RacerProgressEntry[3];
            for (int i = 0; i < 3; i++)
            {
                var entry = new RacerProgressEntry();
                var row = new GameObject($"Progress{i}", typeof(RectTransform));
                row.transform.SetParent(raceInfoPanel.transform, false);
                RuntimeUiFactory.Anchor(row.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(360, 50), new Vector2(-200, -50 - i * 54));
                entry.nameText = RuntimeUiFactory.Label(row.transform, "Name", "Racer", 18,
                    TextAlignmentOptions.Left, Color.white);
                RuntimeUiFactory.Anchor(entry.nameText.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                    new Vector2(140, 24), new Vector2(80, 10));
                entry.progressBar = RuntimeUiFactory.Slider(row.transform, "Bar", 330);
                RuntimeUiFactory.Anchor(entry.progressBar.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
                    Vector2.zero, Vector2.zero);
                entry.barFill = entry.progressBar.fillRect.GetComponent<UnityEngine.UI.Image>();
                entry.statusText = RuntimeUiFactory.Label(row.transform, "Status", "", 16,
                    TextAlignmentOptions.Right, Color.white);
                RuntimeUiFactory.Anchor(entry.statusText.rectTransform, new Vector2(1, 1), new Vector2(1, 1),
                    new Vector2(120, 20), new Vector2(-70, -8));
                progressEntries[i] = entry;
            }

            resultsPanel = RuntimeUiFactory.Panel(transform, "Results", new Color(0.02f, 0.04f, 0.08f, 0.92f)).gameObject;
            resultsTitleText = RuntimeUiFactory.Label(resultsPanel.transform, "Title", "RESULTS", 56,
                TextAlignmentOptions.Center, Color.white);
            RuntimeUiFactory.Anchor(resultsTitleText.rectTransform, new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f),
                new Vector2(900, 80), Vector2.zero);
            resultsHintText = RuntimeUiFactory.Label(resultsPanel.transform, "UnlockHint", "", 20,
                TextAlignmentOptions.Center, new Color(0.5f, 0.9f, 1f));
            RuntimeUiFactory.Anchor(resultsHintText.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f),
                new Vector2(1000, 32), Vector2.zero);

            var list = new GameObject("ResultsList", typeof(RectTransform), typeof(UnityEngine.UI.VerticalLayoutGroup));
            list.transform.SetParent(resultsPanel.transform, false);
            RuntimeUiFactory.Anchor(list.GetComponent<RectTransform>(), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f),
                new Vector2(700, 240), Vector2.zero);
            resultsContainer = list.transform;

            resultEntryPrefab = new GameObject("ResultEntryPrefab", typeof(RectTransform));
            resultEntryPrefab.transform.SetParent(transform, false);
            resultEntryPrefab.SetActive(false);
            resultEntryPrefab.AddComponent<LayoutElement>().preferredHeight = 42f;
            var rankLabel = RuntimeUiFactory.Label(resultEntryPrefab.transform, "Rank", "1", 24, TextAlignmentOptions.Left, Color.white);
            var nameLabel = RuntimeUiFactory.Label(resultEntryPrefab.transform, "Name", "Name", 24, TextAlignmentOptions.Left, Color.white);
            var timeLabel = RuntimeUiFactory.Label(resultEntryPrefab.transform, "Time", "00:00", 24, TextAlignmentOptions.Right, Color.white);
            var gapLabel = RuntimeUiFactory.Label(resultEntryPrefab.transform, "Gap", "", 22, TextAlignmentOptions.Right, Color.cyan);
            RuntimeUiFactory.Anchor(rankLabel.rectTransform, new Vector2(0.04f, 0.5f), new Vector2(0.04f, 0.5f), new Vector2(60, 36), Vector2.zero);
            RuntimeUiFactory.Anchor(nameLabel.rectTransform, new Vector2(0.34f, 0.5f), new Vector2(0.34f, 0.5f), new Vector2(280, 36), Vector2.zero);
            RuntimeUiFactory.Anchor(timeLabel.rectTransform, new Vector2(0.72f, 0.5f), new Vector2(0.72f, 0.5f), new Vector2(160, 36), Vector2.zero);
            RuntimeUiFactory.Anchor(gapLabel.rectTransform, new Vector2(0.94f, 0.5f), new Vector2(0.94f, 0.5f), new Vector2(150, 36), Vector2.zero);

            retryButton = RuntimeUiFactory.Button(resultsPanel.transform, "Retry", "Retry", new Vector2(260, 56));
            RuntimeUiFactory.Anchor(retryButton.GetComponent<RectTransform>(), new Vector2(0.3f, 0.18f), new Vector2(0.3f, 0.18f),
                new Vector2(260, 56), Vector2.zero);
            nextLevelButton = RuntimeUiFactory.Button(resultsPanel.transform, "Next", "Next Level", new Vector2(260, 56));
            RuntimeUiFactory.Anchor(nextLevelButton.GetComponent<RectTransform>(), new Vector2(0.7f, 0.18f), new Vector2(0.7f, 0.18f),
                new Vector2(260, 56), Vector2.zero);
            levelSelectButton = RuntimeUiFactory.Button(resultsPanel.transform, "Levels", "Level Selection", new Vector2(260, 56));
            RuntimeUiFactory.Anchor(levelSelectButton.GetComponent<RectTransform>(), new Vector2(0.3f, 0.08f), new Vector2(0.3f, 0.08f),
                new Vector2(260, 56), Vector2.zero);
            mainMenuButton = RuntimeUiFactory.Button(resultsPanel.transform, "Menu", "Main Menu", new Vector2(260, 56));
            RuntimeUiFactory.Anchor(mainMenuButton.GetComponent<RectTransform>(), new Vector2(0.7f, 0.08f), new Vector2(0.7f, 0.08f),
                new Vector2(260, 56), Vector2.zero);

            SetPanel(countdownPanel, false);
            SetPanel(raceInfoPanel, false);
            SetPanel(resultsPanel, false);
        }
    }
}
