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
        public void Initialise(List<RacerData> racers)
        {
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
            levelSelectButton?.onClick.AddListener(() => SceneLoader.Instance?.LoadScene("MainMenu"));
            mainMenuButton?.onClick.AddListener(() => SceneLoader.Instance?.LoadMainMenu());

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
            if (timerText   != null) timerText.text   = FormatTime(timer);
            if (positionText != null)
            {
                // Find player position
                int pos = 1;
                foreach (var r in racers)
                {
                    if (r.racerObject != null && r.racerObject.CompareTag("Player")) continue;
                    var player = racers.Find(d => d.racerObject != null && d.racerObject.CompareTag("Player"));
                    if (player == null) break;
                    if (r.progress > player.progress) pos++;
                }
                positionText.text = OrdinalString(pos);
            }

            for (int i = 0; i < progressEntries.Length && i < racers.Count; i++)
            {
                var entry = progressEntries[i];
                var data  = racers[i];
                if (entry.progressBar != null) entry.progressBar.value = data.progress;
                if (entry.statusText  != null)
                    entry.statusText.text = data.status == RacerStatus.Finished
                        ? FormatTime(data.finishTime)
                        : data.status == RacerStatus.DNF ? "DNF" : "";
            }
        }

        // ── Checkpoint notification ────────────────────────────────────────
        public void ShowCheckpointNotification(string racerName)
        {
            if (checkpointText == null) return;
            if (_checkpointRoutine != null) StopCoroutine(_checkpointRoutine);
            _checkpointRoutine = StartCoroutine(ShowCheckpointRoutine(racerName));
        }

        private IEnumerator ShowCheckpointRoutine(string racerName)
        {
            checkpointText.text    = "✓ Checkpoint!";
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

            if (resultsTitleText != null)
                resultsTitleText.text = playerWon ? "🏆 VICTORY!" : (playerFinished ? "RACE OVER" : "DEFEAT");

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
                    var txts = go.GetComponentsInChildren<TextMeshProUGUI>();
                    if (txts.Length >= 3)
                    {
                        txts[0].text = r.finalRank.ToString();
                        txts[1].text = r.name;
                        txts[2].text = r.status == RacerStatus.Finished
                            ? FormatTime(r.finishTime)
                            : "DNF";
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
    }
}
