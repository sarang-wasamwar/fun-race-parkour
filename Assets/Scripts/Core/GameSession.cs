// GameSession.cs
// Persistent singleton (DontDestroyOnLoad). Carries selected level and session state.
// NOT a second RaceManager or timer.

using UnityEngine;

namespace FunRaceParkour
{
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        [Tooltip("Index of the level being played (0 = Level 1, 1 = Level 2, 2 = Level 3).")]
        public int SelectedLevel { get; set; } = 0;

        public bool OpenLevelSelectOnMenu { get; set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
