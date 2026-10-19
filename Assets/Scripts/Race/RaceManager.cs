// RaceManager.cs
// One per race scene. Controls state machine: Waiting → Countdown → Racing → Finished.
// Registers racers, tracks progress, handles finish / checkpoint / respawn events.
// Does NOT duplicate any other system.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace FunRaceParkour
{
    public enum RaceState { Waiting, Countdown, Racing, Finished }

    public class RaceManager : MonoBehaviour
    {
        // ── Singleton (scene-scoped, not DontDestroyOnLoad) ───────────────
        public static RaceManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────
        [Header("Racer Configuration")]
        [SerializeField, Tooltip("Data for 3 racers: Player (blue), AI1 (red), AI2 (green).")]
        private RacerConfig[] racerConfigs = new RacerConfig[3];

        [Header("Track")]
        [SerializeField] private Transform startPoint;
        [SerializeField] private Transform endPoint;
        [SerializeField] private float     laneSpacing = 1.5f;
        [SerializeField, Tooltip("Which lane index (0-2) the human player occupies.")]
        private int playerLane = 1;

        [Header("Countdown")]
        [SerializeField] private float countdownDuration = 3f;

        [Header("Respawn")]
        [SerializeField] private float respawnDelay = 2f;

        [Header("References")]
        [SerializeField] private RaceHUD raceHUD;

        // ── Runtime ───────────────────────────────────────────────────────
        public  RaceState         State     { get; private set; } = RaceState.Waiting;
        public  List<RacerData>   Racers    { get; private set; } = new();
        public  float             RaceTimer { get; private set; }

        private float _totalDistance;
        private int   _finishedCount;
        private int   _nextFinishOrder;

        // Events for HUD / ProgressionService
        public event System.Action<RaceState>  OnStateChanged;
        public event System.Action<RacerData, int> OnCheckpoint;    // racer, checkpointIndex
        public event System.Action<RacerData>  OnRacerFinishedEvt;
        public event System.Action             OnRaceComplete;      // all racers done / timer exceeded

        // ── Serializable helper ───────────────────────────────────────────
        [System.Serializable]
        public class RacerConfig
        {
            public string     name;
            public Color      color = Color.white;
            public GameObject prefab;          // PlayerRobot or AIRacer prefab
            public bool       isPlayer;
        }

        public void ConfigureTrack(Transform start, Transform end, RaceHUD hud, RacerConfig[] configs = null)
        {
            startPoint = start;
            endPoint   = end;
            raceHUD    = hud;
            if (configs != null) racerConfigs = configs;
        }

        // ─────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[RaceManager] Duplicate detected — destroying self.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            SetSelectedLevelFromScene();
            SpawnRacers();
            BindPlayerCamera();
            StartCoroutine(CountdownRoutine());
        }

        private static void SetSelectedLevelFromScene()
        {
            if (GameSession.Instance == null) return;
            switch (SceneManager.GetActiveScene().name)
            {
                case "Level1_TrainingRun": GameSession.Instance.SelectedLevel = 0; break;
                case "Level2_MechanicalMayhem": GameSession.Instance.SelectedLevel = 1; break;
                case "Level3_UltimateGauntlet": GameSession.Instance.SelectedLevel = 2; break;
            }
        }

        private void Update()
        {
            if (State != RaceState.Racing) return;
            RaceTimer += Time.deltaTime;
            UpdateProgress();
        }

        // ─────────────────────────────────────────────────────────────────
        // Spawning
        // ─────────────────────────────────────────────────────────────────
        private void SpawnRacers()
        {
            if (startPoint == null || endPoint == null)
            {
                Debug.LogError("[RaceManager] startPoint or endPoint not assigned.", this);
                return;
            }

            _totalDistance = Mathf.Abs(endPoint.position.x - startPoint.position.x);

            float[] zOffsets = { -laneSpacing, 0f, laneSpacing };
            int playerLaneClamped = Mathf.Clamp(playerLane, 0, 2);

            for (int i = 0; i < Mathf.Min(racerConfigs.Length, 3); i++)
            {
                var cfg = racerConfigs[i];
                if (cfg.prefab == null) { Debug.LogWarning($"[RaceManager] No prefab for racer {i}."); continue; }

                float z = startPoint.position.z + zOffsets[i];
                Vector3    spawnPos = new Vector3(startPoint.position.x, startPoint.position.y, z);
                Quaternion spawnRot = Quaternion.Euler(0f, 90f, 0f);

                var go = Instantiate(cfg.prefab, spawnPos, spawnRot);
                go.name = cfg.name;

                RacerSetup.Prepare(go, cfg.isPlayer, cfg.isPlayer ? null : LoadAiProfile(i));

                var ctrl = go.GetComponent<ThirdPersonControllerExtended>();
                if (ctrl != null)
                {
                    ctrl.startPoint = startPoint;
                    ctrl.endPoint   = endPoint;
                }

                var starter = go.GetComponent<StarterAssets.ThirdPersonController>();
                if (starter != null)
                {
                    starter.startPoint = startPoint;
                    starter.endPoint   = endPoint;
                }

                ApplyRacerColor(go, cfg.color);
                AddNameLabel(go, cfg.name, cfg.color);

                IRacerInput inputSeam = go.GetComponent<IRacerInput>();
                if (inputSeam == null)
                    inputSeam = go.GetComponentInChildren<IRacerInput>();

                var data = new RacerData
                {
                    name           = cfg.name,
                    color          = cfg.color,
                    racerObject    = go,
                    controller     = ctrl,
                    starterController = starter,
                    inputSource    = inputSeam,
                    spawnPosition  = spawnPos,
                    spawnRotation  = spawnRot,
                };
                data.lastCheckpointPos = spawnPos;
                data.lastCheckpointRot = spawnRot;

                Racers.Add(data);

                // Ignore collisions between racers
                for (int j = 0; j < i; j++)
                {
                    var collA = go.GetComponent<Collider>();
                    var collB = Racers[j].racerObject.GetComponent<Collider>();
                    if (collA != null && collB != null)
                        Physics.IgnoreCollision(collA, collB, true);

                    // Also ignore CharacterController capsule pairs
                    var ccA = go.GetComponent<CharacterController>();
                    var ccB = Racers[j].racerObject.GetComponent<CharacterController>();
                    if (ccA != null && ccB != null)
                        Physics.IgnoreCollision(ccA, ccB, true);
                }

                // Lock input until GO
                LockRacer(data, true);

                RacerSetup.ResetBody(data, spawnPos, spawnRot);
            }

            if (raceHUD != null) raceHUD.Initialise(Racers);
        }

        private static AIRacerProfile LoadAiProfile(int racerIndex)
        {
            string path = racerIndex == 0
                ? "Assets/ScriptableObjects/AI/AIRacer_Aggressive.asset"
                : "Assets/ScriptableObjects/AI/AIRacer_Cautious.asset";
#if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<AIRacerProfile>(path);
            if (asset != null) return asset;
#endif
            var runtime = ScriptableObject.CreateInstance<AIRacerProfile>();
            if (racerIndex == 0)
            {
                runtime.reactionTime = 0.08f;
                runtime.jumpTimingError = 0.04f;
                runtime.safetyMargin = 0.08f;
                runtime.riskTolerance = 0.7f;
                runtime.safeWindowMultiplier = 1.1f;
            }
            else
            {
                runtime.reactionTime = 0.25f;
                runtime.jumpTimingError = 0.12f;
                runtime.safetyMargin = 0.22f;
                runtime.riskTolerance = 0.2f;
                runtime.safeWindowMultiplier = 1.6f;
            }
            return runtime;
        }

        private void BindPlayerCamera()
        {
            GameObject player = null;
            foreach (var d in Racers)
            {
                if (d.racerObject != null && d.racerObject.CompareTag("Player"))
                {
                    player = d.racerObject;
                    break;
                }
            }
            if (player == null) return;

            var target = player.transform.Find("PlayerCameraRoot");
            var fixedTrackCamera = FindAnyObjectByType<FixedTrackCamera>();
            var vcam = fixedTrackCamera != null
                ? fixedTrackCamera.GetComponent<Unity.Cinemachine.CinemachineCamera>()
                : null;
            if (vcam != null && target != null)
                vcam.Follow = target;
        }

        private void ApplyRacerColor(GameObject go, Color color)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                // Create per-instance materials so we don't taint shared assets
                var mats = r.sharedMaterials;
                var newMats = new Material[mats.Length];
                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] == null) { newMats[m] = null; continue; }
                    newMats[m] = new Material(mats[m]);
                    newMats[m].color = color;
                }
                r.materials = newMats;
            }
        }

        private void AddNameLabel(GameObject go, string racerName, Color color)
        {
            var labelGO = new GameObject("NameLabel");
            labelGO.transform.SetParent(go.transform);
            labelGO.transform.localPosition = new Vector3(0f, 2.2f, 0f);

            var tmp = labelGO.AddComponent<TextMeshPro>();
            tmp.text              = racerName;
            tmp.fontSize          = 2f;
            tmp.color             = color;
            tmp.alignment         = TextAlignmentOptions.Center;
            tmp.fontStyle         = FontStyles.Bold;

            // Billboard component to always face camera
            labelGO.AddComponent<BillboardLabel>();
        }

        // ─────────────────────────────────────────────────────────────────
        // Countdown
        // ─────────────────────────────────────────────────────────────────
        private IEnumerator CountdownRoutine()
        {
            ChangeState(RaceState.Countdown);
            float step = countdownDuration > 0f ? countdownDuration / 3f : 1f;

            if (raceHUD != null) raceHUD.ShowCountdown(3);
            AudioManager.Instance?.PlayCountdownTick();
            yield return new WaitForSeconds(step);

            if (raceHUD != null) raceHUD.ShowCountdown(2);
            AudioManager.Instance?.PlayCountdownTick();
            yield return new WaitForSeconds(step);

            if (raceHUD != null) raceHUD.ShowCountdown(1);
            AudioManager.Instance?.PlayCountdownTick();
            yield return new WaitForSeconds(step);

            if (raceHUD != null) raceHUD.ShowCountdown(0); // GO
            AudioManager.Instance?.PlayCountdownGo();
            StartRace();
        }

        private void StartRace()
        {
            RaceTimer = 0f;
            _finishedCount = 0;
            _nextFinishOrder = 0;
            foreach (var d in Racers) { d.ResetForRace(); LockRacer(d, false); }
            ChangeState(RaceState.Racing);
        }

        // ─────────────────────────────────────────────────────────────────
        // Progress
        // ─────────────────────────────────────────────────────────────────
        private void UpdateProgress()
        {
            if (_totalDistance <= 0f) return;
            float startX = startPoint.position.x;
            float endX   = endPoint.position.x;

            foreach (var d in Racers)
            {
                if (d.status == RacerStatus.Racing && d.racerObject != null)
                {
                    float dx = d.racerObject.transform.position.x - startX;
                    float total = endX - startX;
                    d.progress = Mathf.Clamp01(Mathf.Abs(total) > 0.01f ? dx / total : 0f);
                    d.elapsedTime = RaceTimer;
                }
            }
            if (raceHUD != null) raceHUD.UpdateRaceState(Racers, RaceTimer);
        }

        // ─────────────────────────────────────────────────────────────────
        // Checkpoint & Finish events
        // ─────────────────────────────────────────────────────────────────
        public RacerData GetRacerData(GameObject go)
        {
            if (go == null) return null;
            var root = go.GetComponentInParent<CharacterController>();
            var rootGo = root != null ? root.gameObject : go;
            foreach (var d in Racers)
            {
                if (d.racerObject == go || d.racerObject == rootGo) return d;
                if (d.racerObject != null && go.transform.IsChildOf(d.racerObject.transform))
                    return d;
            }
            return null;
        }

        public void OnCheckpointReached(RacerData data, int idx)
        {
            OnCheckpoint?.Invoke(data, idx);
            if (raceHUD != null) raceHUD.ShowCheckpointNotification(data.name, idx);

            if (data?.racerObject != null && data.racerObject.CompareTag("Player"))
            {
                AudioManager.Instance?.PlayCheckpoint();
            }
        }

        public void OnRacerFinished(GameObject go)
        {
            if (State != RaceState.Racing) return;
            var data = GetRacerData(go);
            if (data == null || data.status != RacerStatus.Racing) return;

            data.status     = RacerStatus.Finished;
            data.finishTime = RaceTimer;
            data.finishOrder = _nextFinishOrder++;
            data.progress   = 1f;
            _finishedCount++;

            LockRacer(data, true);
            OnRacerFinishedEvt?.Invoke(data);

            // Update progression if this is the player
            if (data.racerObject != null && data.racerObject.CompareTag("Player"))
            {
                ProgressionService.Instance?.OnPlayerFinished(
                    GameSession.Instance?.SelectedLevel ?? 0, data.finishTime);
                AudioManager.Instance?.PlayFinish();
            }

            // Race ends when all finish or after a grace period
            if (_finishedCount >= Racers.Count) FinaliseRace();
            else if (_finishedCount == 1) StartCoroutine(GraceTimerRoutine());
        }

        private IEnumerator GraceTimerRoutine()
        {
            yield return new WaitForSeconds(30f);
            FinaliseRace();
        }

        private void FinaliseRace()
        {
            StopAllCoroutines();

            // Mark remaining racing racers DNF
            foreach (var d in Racers)
                if (d.status == RacerStatus.Racing) { d.status = RacerStatus.DNF; LockRacer(d, true); }

            RaceRanking.Rank(Racers);

            ChangeState(RaceState.Finished);
            if (raceHUD != null) raceHUD.ShowResults(Racers);
            OnRaceComplete?.Invoke();
        }

        // ─────────────────────────────────────────────────────────────────
        // Respawn
        // ─────────────────────────────────────────────────────────────────
        public void RequestRespawn(RacerData data)
        {
            if (State != RaceState.Racing || data == null || data.status != RacerStatus.Racing) return;
            data.falls++;
            StartCoroutine(RespawnRoutine(data));
        }

        private IEnumerator RespawnRoutine(RacerData data)
        {
            data.respawns++;
            // disable input briefly
            if (data.inputSource != null) data.inputSource.InputLocked = true;
            yield return new WaitForSeconds(respawnDelay);
            RacerSetup.ResetBody(data, data.lastCheckpointPos, data.lastCheckpointRot);
            if (data.inputSource != null && data.status == RacerStatus.Racing)
                data.inputSource.InputLocked = false;
        }

        // ─────────────────────────────────────────────────────────────────
        // Restart
        // ─────────────────────────────────────────────────────────────────
        public void Restart()
        {
            StopAllCoroutines();
            _finishedCount = 0;
            _nextFinishOrder = 0;
            RaceTimer      = 0f;

            foreach (var d in Racers)
            {
                d.ResetForRace();
                RacerSetup.ResetBody(d, d.spawnPosition, d.spawnRotation);
                LockRacer(d, true);
            }

            var obstacles = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);
            foreach (var o in obstacles)
                if (o is IResettableObstacle ro) ro.ResetState();

            if (raceHUD != null) raceHUD.Initialise(Racers);
            StartCoroutine(CountdownRoutine());
        }

        // ─────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────
        private void LockRacer(RacerData d, bool locked)
        {
            if (d.inputSource != null) d.inputSource.InputLocked = locked;
        }

        private void ChangeState(RaceState newState)
        {
            State = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
