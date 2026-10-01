using System;
using System.Collections;
using System.Collections.Generic;
using Momentum.Audio;
using Momentum.Bosses;
using Momentum.PlayerSystems;
using Momentum.SaveSystem;
using Momentum.Weapons;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Momentum.Levels
{
    public enum LevelMode
    {
        Campaign,
        Custom,
        EditorTest
    }

    public enum RunState
    {
        Preparing,
        WaitingForStart,
        Running,
        Finished
    }

    /// <summary>
    /// Per-scene run controller: spawns the player, starts/stops the timer, handles deaths, fall-outs and
    /// checkpoint respawns (without reloading the scene), computes ranks, records best times and
    /// publishes the result. Works for campaign scenes, custom JSON levels and level-editor test mode.
    /// </summary>
    [RequireComponent(typeof(LevelTimer))]
    [RequireComponent(typeof(CheckpointManager))]
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [SerializeField] LevelDefinition definition;
        [SerializeField] LevelMode mode = LevelMode.Campaign;
        [Tooltip("Begin automatically on Start (disabled in the level editor scene).")]
        [SerializeField] bool autoBegin = true;
        [SerializeField] float killHeight = -40f;
        [SerializeField] List<WeaponDefinition> startingWeapons = new List<WeaponDefinition>();
        [SerializeField] Transform levelRoot;
        [SerializeField] LevelLoader loader;
        [SerializeField] NavMeshBaker navMeshBaker;
        [SerializeField] MusicTrack music = MusicTrack.Level;

        LevelTimer timer;
        CheckpointManager checkpoints;
        PlayerController player;
        bool respawnPending;
        float respawnAt;
        bool hasStartGate;
        Vector3 spawnFeet;
        float spawnYaw;
        string levelId = "";
        string levelName = "";
        RankThresholds ranks = RankThresholds.Default;

        public LevelDefinition Definition => definition;
        public LevelMode Mode => mode;
        public RunState State { get; private set; } = RunState.Preparing;
        public string LevelId => levelId;
        public string LevelName => levelName;
        public RankThresholds Ranks => ranks;
        public PlayerController Player => player;
        public LevelTimer Timer => timer;
        public int Deaths { get; private set; }
        public int Kills { get; private set; }
        public LevelData CustomData { get; private set; }

        public float BestTime => SaveManager.Instance != null && mode != LevelMode.EditorTest ? SaveManager.Instance.GetBestTime(levelId) : -1f;
        public bool IsBossLevel => BossBase.All.Count > 0;

        /// <summary>Raised when "Restart Level" is requested in editor test mode.</summary>
        public event Action RestartRequested;

        void Awake()
        {
            Instance = this;
            timer = GetComponent<LevelTimer>();
            checkpoints = GetComponent<CheckpointManager>();
            ArenaRegistry.Clear();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            GameEvents.PlayerDied += OnPlayerDied;
            GameEvents.StartLineCrossed += OnStartLine;
            GameEvents.FinishLineCrossed += OnFinishLine;
            GameEvents.EnemyKilled += OnEnemyKilled;
        }

        void OnDisable()
        {
            GameEvents.PlayerDied -= OnPlayerDied;
            GameEvents.StartLineCrossed -= OnStartLine;
            GameEvents.FinishLineCrossed -= OnFinishLine;
            GameEvents.EnemyKilled -= OnEnemyKilled;
        }

        void Start()
        {
            if (autoBegin) StartCoroutine(BeginRoutine());
        }

        IEnumerator BeginRoutine()
        {
            State = RunState.Preparing;
            if (mode == LevelMode.Custom)
            {
                string path = GameManager.Instance != null ? GameManager.Instance.PendingCustomLevelPath : null;
                var data = CustomLevelStorage.Load(path);
                if (data == null)
                {
                    GameEvents.RaiseNotification("Could not load custom level", Color.red);
                    yield return new WaitForSeconds(1.5f);
                    if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu(MenuReturnTarget.CustomMaps);
                    yield break;
                }
                if (loader == null) loader = gameObject.AddComponent<LevelLoader>();
                levelRoot = loader.Build(data, LevelBuildMode.Play);
                ApplyCustomData(data);
            }
            else
            {
                if (definition == null && GameManager.Instance != null) definition = GameManager.Instance.CurrentLevel;
                if (definition != null)
                {
                    levelId = definition.id;
                    levelName = definition.displayName;
                    ranks = definition.rankTimes;
                    music = definition.music;
                }
                else
                {
                    levelId = SceneManager.GetActiveScene().name;
                    levelName = levelId;
                }
            }

            // Let freshly created level objects run Start() before baking and spawning.
            yield return null;
            if (navMeshBaker != null) navMeshBaker.Bake(levelRoot);
            SpawnPlayer();
            BeginRun();
        }

        void ApplyCustomData(LevelData data)
        {
            CustomData = data;
            levelId = mode == LevelMode.EditorTest ? "editor-test" : CustomLevelStorage.RecordKey(data);
            levelName = data.levelName;
            ranks = data.rankTimes;
            killHeight = data.killHeight;
            startingWeapons = new List<WeaponDefinition>();
            var registry = PrefabRegistry.Instance;
            if (registry != null && data.startingWeapons != null)
            {
                foreach (var id in data.startingWeapons)
                {
                    var def = registry.GetWeapon(id);
                    if (def != null) startingWeapons.Add(def);
                }
            }
            if (!Enum.TryParse(data.music, true, out music)) music = MusicTrack.Level;
        }

        // ------------------------------------------------------------------ Editor test mode

        /// <summary>Starts a playtest of an already-built level (used by the in-game level editor).</summary>
        public void BeginEditorTest(LevelData data, Transform root)
        {
            StopAllCoroutines();
            mode = LevelMode.EditorTest;
            levelRoot = root;
            ApplyCustomData(data);
            StartCoroutine(EditorTestRoutine());
        }

        IEnumerator EditorTestRoutine()
        {
            State = RunState.Preparing;
            yield return null;
            if (navMeshBaker != null) navMeshBaker.Bake(levelRoot);
            SpawnPlayer();
            BeginRun();
        }

        /// <summary>Ends a playtest: removes the player, transient objects and the NavMesh.</summary>
        public void EndEditorTest()
        {
            StopAllCoroutines();
            if (player != null) Destroy(player.gameObject);
            player = null;
            timer.ResetTimer();
            State = RunState.Preparing;
            respawnPending = false;
            if (navMeshBaker != null) navMeshBaker.Clear();
            ArenaRegistry.Clear();
            PoolManager.DespawnAll();
            if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
            AudioManager.SetAmbient(false);
        }

        // ------------------------------------------------------------------ Run flow

        void SpawnPlayer()
        {
            var sp = SpawnPoint.First;
            spawnFeet = sp != null ? sp.FeetPosition : Vector3.up * 0.5f;
            spawnYaw = sp != null ? sp.Yaw : 0f;
            if (sp == null) Debug.LogWarning("[Momentum] No SpawnPoint in level; spawning at origin.");

            if (player == null)
            {
                var prefab = PrefabRegistry.Instance != null ? PrefabRegistry.Instance.player : null;
                if (prefab == null)
                {
                    Debug.LogError("[Momentum] No player prefab registered. Run Tools > Parkour FPS > Setup Complete Game.");
                    return;
                }
                var go = Instantiate(prefab, spawnFeet + Vector3.up * 1.05f, Quaternion.identity);
                go.name = "Player";
                player = go.GetComponent<PlayerController>();
            }
            if (player == null) return;

            player.Respawn(spawnFeet, spawnYaw);
            if (player.Weapons != null)
            {
                player.Weapons.ClearAll();
                foreach (var w in startingWeapons)
                {
                    if (w != null) player.Weapons.GiveWeapon(w, false);
                }
            }
        }

        void BeginRun()
        {
            timer.ResetTimer();
            checkpoints.Initialize(spawnFeet, spawnYaw, levelId, mode != LevelMode.EditorTest);
            Deaths = 0;
            Kills = 0;
            respawnPending = false;
            hasStartGate = StartTrigger.All.Count > 0;
            State = RunState.WaitingForStart;
            if (mode != LevelMode.EditorTest && SaveManager.Instance != null) SaveManager.Instance.RecordAttempt(levelId);
            AudioManager.PlayMusic(IsBossLevel && music == MusicTrack.Level ? MusicTrack.Intense : music);
            AudioManager.SetAmbient(true);
            if (player != null) player.SetControlsEnabled(true);
            GameManager.SetCursorLocked(true);
        }

        void Update()
        {
            if (player == null) return;
            bool paused = GameManager.Instance != null && GameManager.Instance.IsPaused;

            if (State == RunState.WaitingForStart && !hasStartGate && !paused)
            {
                var input = player.InputHandler;
                if (input != null && (input.Move.sqrMagnitude > 0.01f || input.JumpHeld || input.FireHeld)) StartRun();
            }

            if (!player.IsDead && State != RunState.Finished && player.transform.position.y < killHeight)
            {
                player.Kill(DamageType.Fall);
            }

            if (respawnPending && Time.time >= respawnAt) RespawnPlayer();

            if (!paused && State != RunState.Finished && player.InputHandler != null &&
                Input.GetKeyDown(player.InputHandler.Bindings.restartCheckpoint))
            {
                RestartFromCheckpoint();
            }

            if (GameConfig.Instance.developerMode && !paused)
            {
                if (Input.GetKeyDown(KeyCode.F5)) RestartLevel();
                if (Input.GetKeyDown(KeyCode.F6) && State != RunState.Finished)
                {
                    if (State == RunState.WaitingForStart) StartRun();
                    Finish();
                }
            }
        }

        void StartRun()
        {
            if (State != RunState.WaitingForStart) return;
            State = RunState.Running;
            timer.StartTimer();
            GameEvents.RaiseRunStarted();
        }

        void OnStartLine()
        {
            StartRun();
        }

        void OnPlayerDied(PlayerController p, DamageInfo info)
        {
            if (p != player || State == RunState.Finished) return;
            Deaths++;
            if (mode != LevelMode.EditorTest && SaveManager.Instance != null) SaveManager.Instance.RecordDeath(levelId);
            var config = GameConfig.Instance;
            respawnPending = true;
            respawnAt = Time.time + (info.type == DamageType.Fall ? config.fallRespawnDelay : config.respawnDelay);
        }

        void RespawnPlayer()
        {
            respawnPending = false;
            if (player != null) player.Respawn(checkpoints.RespawnPosition, checkpoints.RespawnYaw);
        }

        /// <summary>Instantly returns the player to the last checkpoint (timer keeps running).</summary>
        public void RestartFromCheckpoint()
        {
            if (player == null || State == RunState.Finished || State == RunState.Preparing) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) GameManager.Instance.SetPaused(false);
            respawnPending = false;
            player.Respawn(checkpoints.RespawnPosition, checkpoints.RespawnYaw);
        }

        /// <summary>Restarts the whole level (scene reload, or rebuild in editor test mode).</summary>
        public void RestartLevel()
        {
            if (mode == LevelMode.EditorTest)
            {
                RestartRequested?.Invoke();
                return;
            }
            if (GameManager.Instance != null) GameManager.Instance.RestartScene();
            else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        void OnFinishLine()
        {
            if (State == RunState.Finished || State == RunState.Preparing || player == null || player.IsDead) return;
            if (State == RunState.WaitingForStart) StartRun();
            Finish();
        }

        void Finish()
        {
            timer.StopTimer();
            State = RunState.Finished;
            respawnPending = false;
            if (player != null) player.SetControlsEnabled(false);

            float time = timer.CurrentTime;
            var rank = RankCalculator.Calculate(time, ranks);
            var splits = checkpoints.GetSplits();
            var result = new LevelResult
            {
                levelId = levelId,
                levelName = levelName,
                time = time,
                rank = rank,
                thresholds = ranks,
                isCustomLevel = mode != LevelMode.Campaign,
                isEditorTest = mode == LevelMode.EditorTest,
                deaths = Deaths,
                kills = Kills,
                secretsFound = SecretArea.FoundCount,
                secretsTotal = SecretArea.All.Count
            };
            result.splits.AddRange(splits);

            var save = SaveManager.Instance;
            for (int i = 0; i < splits.Count; i++)
            {
                float pb = save != null && mode != LevelMode.EditorTest ? save.GetPbSplit(levelId, i) : -1f;
                result.splitDeltas.Add(splits[i] >= 0f && pb > 0f ? splits[i] - pb : float.NaN);
            }

            if (mode != LevelMode.EditorTest && save != null)
            {
                var record = save.RecordRun(levelId, time, rank.ToString(), splits);
                result.isNewBest = record.isNewBest;
                result.previousBest = record.previousBest;
                result.firstCompletion = record.firstCompletion;
            }
            result.hasNextLevel = mode == LevelMode.Campaign && GameManager.Instance != null && GameManager.Instance.HasNextLevel();

            AudioManager.Play2D(SoundId.Finish, 1f);
            GameEvents.RaiseRunFinished(result);
        }

        void OnEnemyKilled(Enemies.EnemyBase enemy)
        {
            Kills++;
        }
    }
}
