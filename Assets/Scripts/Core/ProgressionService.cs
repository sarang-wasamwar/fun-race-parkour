// ProgressionService.cs
// Persistent singleton. Manages level unlocks and best times via PlayerPrefs.
// Key prefix: FRP_
// Level N+1 unlocks only after the player legitimately finishes level N.
// Player does not need to WIN — just finish.

using UnityEngine;

namespace FunRaceParkour
{
    public class ProgressionService : MonoBehaviour
    {
        public static ProgressionService Instance { get; private set; }

        private const string PrefixUnlock   = "FRP_Unlocked_";
        private const string PrefixBestTime = "FRP_BestTime_";
        private const int    TotalLevels    = 3;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Level 0 (Level 1) is always unlocked
            if (!PlayerPrefs.HasKey(PrefixUnlock + 0))
                SetUnlocked(0, true);
        }

        // ── Unlock ────────────────────────────────────────────────────────
        public bool IsUnlocked(int levelIndex)
        {
            if (levelIndex <= 0) return true;
            return PlayerPrefs.GetInt(PrefixUnlock + levelIndex, 0) == 1;
        }

        private void SetUnlocked(int levelIndex, bool value)
        {
            PlayerPrefs.SetInt(PrefixUnlock + levelIndex, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        // ── Best time ─────────────────────────────────────────────────────
        public float GetBestTime(int levelIndex)
        {
            return PlayerPrefs.GetFloat(PrefixBestTime + levelIndex, -1f);
        }

        private void SetBestTime(int levelIndex, float time)
        {
            float existing = GetBestTime(levelIndex);
            if (existing < 0f || time < existing)
            {
                PlayerPrefs.SetFloat(PrefixBestTime + levelIndex, time);
                PlayerPrefs.Save();
            }
        }

        // ── Called by RaceManager when player crosses finish ──────────────
        /// <summary>
        /// Call this when the human player legitimately finishes a level.
        /// Records best time and unlocks the next level.
        /// </summary>
        public void OnPlayerFinished(int levelIndex, float finishTime)
        {
            SetBestTime(levelIndex, finishTime);

            int next = levelIndex + 1;
            if (next < TotalLevels && !IsUnlocked(next))
                SetUnlocked(next, true);
        }

        // ── Reset (for testing) ───────────────────────────────────────────
        public void ResetAll()
        {
            for (int i = 0; i < TotalLevels; i++)
            {
                PlayerPrefs.DeleteKey(PrefixUnlock + i);
                PlayerPrefs.DeleteKey(PrefixBestTime + i);
            }
            SetUnlocked(0, true);
            PlayerPrefs.Save();
        }
    }
}
