using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SkyCourier
{
    [DefaultExecutionOrder(-100)]
    public sealed class SkyCourierGame : MonoBehaviour
    {
        private sealed class EnemyDeathFx
        {
            public Vector2 Position;
            public float StartTime;
            public string Name;
            public int Seed;
            public EnemyKind Kind;
        }

        private sealed class EnemyAttackFx
        {
            public Vector2 Position;
            public float StartTime;
            public EnemyKind Kind;
            public bool Hit;
            public int Damage;
            public int TargetLane;
            public int Seed;
        }

        private sealed class EnemyLaneFx
        {
            public EnemyState Enemy;
            public int FromLane;
            public int ToLane;
            public float StartTime;
            public float Duration;
            public int Seed;
        }

        private enum RewardKind
        {
            AddCard,
            UpgradeCard,
            Module
        }

        private sealed class RewardChoice
        {
            public RewardKind Kind;
            public CardId Card;
            public ModuleId Module;
            public UpgradeBranch Branch;
        }

        private enum ScreenMode
        {
            Title,
            Archive,
            Challenge,
            Contract,
            DepartureBriefing,
            Map,
            Retrofit,
            FinalApproach,
            Battle,
            Reward,
            Shop,
            ShopPurge,
            WorkshopCardSelect,
            WorkshopBranch,
            Event,
            EventPurge,
            Rest,
            DeckPurge,
            FinalTrim,
            CoreUpgrade,
            Complete
        }

        private enum CombatFx
        {
            None,
            Shot,
            Volley,
            Shield,
            Maneuver,
            Coolant,
            Overclock,
            EnemyHit
        }

        private const float ReferenceWidth = 1600f;
        private const float ReferenceHeight = 900f;
        private readonly BattleState battle = new BattleState();
        private readonly List<CardId> runDeck = new List<CardId>();
        private readonly HashSet<CardId> runUpgrades = new HashSet<CardId>();
        private readonly Dictionary<CardId, UpgradeBranch> runUpgradeBranches = new Dictionary<CardId, UpgradeBranch>();
        private readonly List<ModuleId> runModules = new List<ModuleId>();
        private readonly List<RunBuildSnapshot> runBuildSnapshots = new List<RunBuildSnapshot>();
        private readonly List<EnemyDeathFx> enemyDeathFx = new List<EnemyDeathFx>();
        private readonly List<EnemyAttackFx> enemyAttackFx = new List<EnemyAttackFx>();
        private readonly List<EnemyLaneFx> enemyLaneFx = new List<EnemyLaneFx>();
        private readonly bool[] shopBought = new bool[3];
        private readonly HashSet<int> completedRouteNodes = new HashSet<int>();
        private readonly RouteDefinition route = RouteCatalog.WindmillArchipelago;
        private DeliveryArchiveData archiveData;

        private ScreenMode screen = ScreenMode.Title;
        private int routeIndex;
        private int selectedRouteNodeId;
        private int lastCompletedRouteNodeId = -1;
        private float routeScroll;
        private bool eventResolved;
        private string eventResult;
        private bool restResolved;
        private string restResult;
        private int deckPurgePage;
        private int credits;
        private int runHull;
        private int runCargoIntegrity;
        private CargoContract selectedContract = CargoContract.FragileMedicine;
        private ChallengeId currentChallenge = ChallengeId.Standard;
        private AirframeModification runModification = AirframeModification.None;
        private RouteStoryState routeStoryState = RouteStoryState.None;
        private RouteIntel routeIntel = RouteIntel.None;
        private DepartureDirective departureDirective = DepartureDirective.Unselected;
        private FinalApproachPlan finalApproachPlan = FinalApproachPlan.Unselected;
        private FinaleEnding finaleEnding = FinaleEnding.None;
        private int runContractBonus;
        private int runContractProcs;
        private bool repairBought;
        private bool shopPurgeBought;
        private bool shopCalibrationBought;
        private int workshopCardValue = -1;
        private int workshopPage;
        private int runTurns;
        private int runCardsPlayed;
        private int runDamageTaken;
        private int runOverheats;
        private int runCalamityInterrupts;
        private int runCalamityEvades;
        private int runCalamityHits;
        private int runTrackingHits;
        private int runSeed;
        private int activeEncounterSeed;
        private string runAttemptId;
        private bool archiveFailureRecorded;
        private CombatFx combatFx;
        private CardId combatFxCard;
        private float combatFxStart;
        private float combatFxDuration;
        private int combatFxLane;
        private string combatFxText;
        private float combatFxPower;
        private float dangerFlashUntil;
        private float shakeUntil;
        private float shakeMagnitude;
        private float bannerUntil;
        private string bannerText;
        private float impactFlashUntil;
        private float enemyRecoilUntil;
        private float fullScreenFxStart;
        private float fullScreenFxDuration;
        private float fullScreenFxPower;
        private bool fullScreenFxKill;
        private float rewardEnteredAt;
        private float rewardConfirmUntil;
        private bool rewardSelectionLocked;
        private int selectedRewardIndex = -1;
        private string selectedRewardName;
        private int lastRewardCredits;
        private int lastFieldRepair;
        private float battleInputLockUntil;
        private int commandChain;
        private int commandChainTurn;
        private float commandChainUntil;
        private string hoverKeyThisFrame;
        private string hoverKeyLastFrame;
        private float laneTransitionStart = -10f;
        private float laneTransitionDuration = 0.62f;
        private int laneTransitionFrom;
        private int laneTransitionTo;
        private int impactDamage;
        private Vector2 impactPoint;
        private AudioSource audioSource;
        private AudioSource audioLayerSource;
        private AudioSource bgmSource;
        private AudioSource bgmFadeSource;
        private AudioClip titleMusic;
        private AudioClip routeMusic;
        private AudioClip battleMusic;
        private AudioClip bossMusic;
        private AudioClip restMusic;
        private AudioClip activeMusic;
        private float bgmFadeStartedAt = -10f;
        private const float BgmVolume = 0.32f;
        private const float BgmFadeDuration = 0.8f;
        private const string FirstBattleGuideKey = "SkyCourier.FirstBattleGuide";
        private static readonly Vector2Int[] SupportedResolutions =
        {
            new Vector2Int(1280, 720),
            new Vector2Int(1600, 900),
            new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1440),
            new Vector2Int(3840, 2160)
        };
        private GameSettingsData gameSettings;
        private float musicVolume = 0.8f;
        private float sfxVolume = 0.9f;
        private string saveStatusMessage;
        private float saveStatusUntil;
        private bool settingsOpen;
        private bool settingsReturnToPause;
        private bool controllerActive;
        private bool keyboardFocusActive;
        private int controllerSelection;
        private string controllerContext;
        private bool controllerAxisHeld;
        private float controllerNextMoveAt;
        private float contractCarouselVisual;
        private float contractCarouselTarget;
        private float contractCarouselVelocity;
        private int archivePage;
        private bool paused;
        private bool showFirstBattleGuide;
        private int firstBattleGuidePage;
        private TutorialProgressData tutorialProgress;
        private TutorialTopic activeTutorialTopic = TutorialTopic.Intent;
        private bool tutorialRulebookMode;
#if UNITY_EDITOR
        private int editorAttackPreviewIndex;
#endif
        private AudioClip clickSound;
        private AudioClip shotSound;
        private AudioClip heavyShotSound;
        private AudioClip shieldSound;
        private AudioClip maneuverSound;
        private AudioClip maneuverLockSound;
        private AudioClip warningSound;
        private AudioClip rewardSound;
        private AudioClip impactSound;
        private AudioClip destructionSound;
        private AudioClip lowExplosionSound;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallStyle;
        private GUIStyle tinyStyle;
        private GUIStyle centeredStyle;
        private GUIStyle cardTitleStyle;
        private GUIStyle cardBodyStyle;
        private GUIStyle moduleTitleStyle;
        private GUIStyle moduleBodyStyle;
        private GUIStyle buttonLabelStyle;
        private GUIStyle hudStyle;
        private GUIStyle hudCenteredStyle;
        private GUIStyle neonTitleStyle;
        private GUIStyle neonSubtitleStyle;
        private GUIStyle neonBodyStyle;
        private GUIStyle contractBadgeStyle;
        private Font uiFont;
        private Font displayFont;
        private Font terminalFont;

        private static readonly Color32 Ink = new Color32(28, 34, 55, 255);
        private static readonly Color32 Paper = new Color32(255, 244, 207, 255);
        private static readonly Color32 SkyTop = new Color32(85, 191, 216, 255);
        private static readonly Color32 SkyBottom = new Color32(170, 226, 218, 255);
        private static readonly Color32 PostalRed = new Color32(214, 70, 66, 255);
        private static readonly Color32 Shadow = new Color32(35, 62, 82, 255);
        private static readonly Color32 Gold = new Color32(244, 181, 70, 255);
        private static readonly Color32 NeonCyan = new Color32(74, 236, 241, 255);
        private static readonly Color32 NeonViolet = new Color32(180, 91, 255, 255);
        private static readonly Color32 Night = new Color32(10, 18, 42, 255);
        private static readonly Color32 PanelNight = new Color32(20, 32, 62, 245);

        private void Awake()
        {
            RunDiagnosticsService.Initialize();
            archiveData = DeliveryArchiveService.Load(out bool archiveRestoredBackup, out string archiveError);
            if (archiveRestoredBackup)
            {
                saveStatusMessage = "邮政档案已从备份恢复";
                saveStatusUntil = Time.unscaledTime + 6f;
            }
            else if (!string.IsNullOrEmpty(archiveError))
            {
                saveStatusMessage = "邮政档案不可读，已使用新档案";
                saveStatusUntil = Time.unscaledTime + 6f;
                Debug.LogWarning($"DELIVERY_ARCHIVE_LOAD_FAILED: {archiveError}");
            }
            gameSettings = GameSettingsService.Load();
            LocalizationService.Initialize((GameLanguage)gameSettings.Language);
            tutorialProgress = TutorialProgressService.Load(PlayerPrefs.GetInt(FirstBattleGuideKey, 0) != 0);
            musicVolume = gameSettings.MusicVolume;
            sfxVolume = gameSettings.SfxVolume;
            GameSettingsService.Apply(gameSettings, true);
            if (GetComponent<AudioListener>() == null)
                gameObject.AddComponent<AudioListener>();
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = 0.86f;
            audioLayerSource = gameObject.AddComponent<AudioSource>();
            audioLayerSource.playOnAwake = false;
            audioLayerSource.volume = 0.78f;
            bgmSource = CreateMusicSource();
            bgmFadeSource = CreateMusicSource();
            titleMusic = Resources.Load<AudioClip>("Audio/BGM/HorizonsUnfold");
            routeMusic = Resources.Load<AudioClip>("Audio/BGM/AtmosphericDescent");
            battleMusic = Resources.Load<AudioClip>("Audio/BGM/BreakingOrbit");
            bossMusic = Resources.Load<AudioClip>("Audio/BGM/EchoesOfTheWarp");
            restMusic = Resources.Load<AudioClip>("Audio/BGM/SolarWindLullaby");
            clickSound = CreateTone("界面点击", 520f, 0.07f, 0.12f, 0.02f);
            shotSound = LoadSound("Audio/KenneySciFi/laser_small", "像素射击", 210f, 0.16f, 0.18f, 0.18f);
            heavyShotSound = LoadSound("Audio/KenneySciFi/laser_heavy", "过载射击", 148f, 0.28f, 0.2f, 0.2f);
            shieldSound = LoadSound("Audio/KenneySciFi/shield", "护盾展开", 680f, 0.22f, 0.12f, 0f);
            maneuverSound = LoadSound("Audio/KenneySciFi/lane_dash", "航道推进", 330f, 0.18f, 0.11f, 0.05f);
            maneuverLockSound = LoadSound("Audio/KenneySciFi/lane_lock", "航道锁定", 460f, 0.13f, 0.1f, 0.02f);
            warningSound = CreateTone("过热警报", 135f, 0.28f, 0.16f, 0.06f);
            rewardSound = CreateTone("奖励", 790f, 0.3f, 0.12f, 0f);
            impactSound = LoadSound("Audio/KenneySciFi/impact_metal", "重型命中", 82f, 0.26f, 0.24f, 0.42f);
            destructionSound = LoadSound("Audio/KenneySciFi/explosion_crunch", "目标爆炸", 54f, 0.48f, 0.3f, 0.58f);
            lowExplosionSound = LoadSound("Audio/KenneySciFi/explosion_low", "低频爆破", 46f, 0.42f, 0.28f, 0.5f);
            string contractCapture = CommandLineValue("-captureContractPreview");
            string settingsCapture = CommandLineValue("-captureSettingsPreview");
            string retrofitCapture = CommandLineValue("-captureRetrofitPreview");
            string avionicsBattleCapture = CommandLineValue("-captureOpenAvionicsBattle");
            string countermeasureCapture = CommandLineValue("-captureCountermeasureBattle");
            string storyEventCapture = CommandLineValue("-captureStoryEvent");
            string adaptiveBossCapture = CommandLineValue("-captureAdaptiveBoss");
            string cloudWyrmCapture = CommandLineValue("-captureCloudWyrmBoss");
            string dualFinaleMapCapture = CommandLineValue("-captureDualFinaleMap");
            string airspaceMapCapture = CommandLineValue("-captureAirspaceMap");
            string finalePreludeCapture = CommandLineValue("-captureFinalePrelude");
            string finaleIntelCapture = CommandLineValue("-captureFinaleIntel");
            string finaleEndingCapture = CommandLineValue("-captureFinaleEnding");
            string finaleArchiveCapture = CommandLineValue("-captureFinaleArchive");
            string challengeBoardCapture = CommandLineValue("-captureChallengeBoard");
            string progressionArchiveCapture = CommandLineValue("-captureProgressionArchive");
            if (!string.IsNullOrEmpty(contractCapture) || !string.IsNullOrEmpty(settingsCapture) ||
                !string.IsNullOrEmpty(retrofitCapture) || !string.IsNullOrEmpty(avionicsBattleCapture) ||
                !string.IsNullOrEmpty(countermeasureCapture) || !string.IsNullOrEmpty(storyEventCapture) ||
                !string.IsNullOrEmpty(adaptiveBossCapture) || !string.IsNullOrEmpty(cloudWyrmCapture) ||
                !string.IsNullOrEmpty(dualFinaleMapCapture) || !string.IsNullOrEmpty(airspaceMapCapture) ||
                !string.IsNullOrEmpty(finalePreludeCapture) || !string.IsNullOrEmpty(finaleIntelCapture) ||
                !string.IsNullOrEmpty(finaleEndingCapture) || !string.IsNullOrEmpty(finaleArchiveCapture) ||
                !string.IsNullOrEmpty(challengeBoardCapture) || !string.IsNullOrEmpty(progressionArchiveCapture))
                StartCoroutine(CaptureUiPreviews(contractCapture, settingsCapture, retrofitCapture,
                    avionicsBattleCapture, countermeasureCapture, storyEventCapture, adaptiveBossCapture,
                    cloudWyrmCapture, dualFinaleMapCapture, airspaceMapCapture, finalePreludeCapture,
                    finaleIntelCapture, finaleEndingCapture, finaleArchiveCapture, challengeBoardCapture,
                    progressionArchiveCapture));
        }

        private static string CommandLineValue(string argument)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            int index = Array.FindIndex(arguments, value =>
                string.Equals(value, argument, StringComparison.OrdinalIgnoreCase));
            return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null;
        }

        private IEnumerator CaptureUiPreviews(string contractPath, string settingsPath, string retrofitPath,
            string avionicsBattlePath, string countermeasurePath, string storyEventPath, string adaptiveBossPath,
            string cloudWyrmPath, string dualFinaleMapPath, string airspaceMapPath, string finalePreludePath,
            string finaleIntelPath, string finaleEndingPath, string finaleArchivePath, string challengeBoardPath,
            string progressionArchivePath)
        {
            Screen.SetResolution(1600, 900, FullScreenMode.Windowed);
            yield return null;
            if (!string.IsNullOrEmpty(challengeBoardPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(challengeBoardPath) ?? ".");
                StartNewRun();
                for (int i = 0; i < 8; i++)
                    yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(challengeBoardPath);
                yield return WaitForCapture(challengeBoardPath);
            }
            if (!string.IsNullOrEmpty(contractPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(contractPath) ?? ".");
                StartNewRun();
                BeginContractSelection(ChallengeId.Standard);
                SetContractPreview((int)CargoContract.SignalSeed);
                for (int i = 0; i < 45; i++)
                    yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(contractPath);
                yield return WaitForCapture(contractPath);
            }
            if (!string.IsNullOrEmpty(settingsPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath) ?? ".");
                screen = ScreenMode.Title;
                OpenSettings(false);
                for (int i = 0; i < 6; i++)
                    yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(settingsPath);
                yield return WaitForCapture(settingsPath);
            }
            if (!string.IsNullOrEmpty(retrofitPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(retrofitPath) ?? ".");
                InitializeRun(CargoContract.BlackBoxRelay);
                routeIndex = 4;
                screen = ScreenMode.Retrofit;
                controllerSelection = 1;
                for (int i = 0; i < 8; i++)
                    yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(retrofitPath);
                yield return WaitForCapture(retrofitPath);
            }
            if (!string.IsNullOrEmpty(avionicsBattlePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(avionicsBattlePath) ?? ".");
                InitializeRun(CargoContract.BlackBoxRelay);
                runModification = AirframeModification.OpenAvionics;
                StartBattle(EncounterId.Skirmish);
                showFirstBattleGuide = false;
                paused = false;
                Time.timeScale = 1f;
                for (int i = 0; i < 8; i++)
                    yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(avionicsBattlePath);
                yield return WaitForCapture(avionicsBattlePath);
            }
            if (!string.IsNullOrEmpty(countermeasurePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(countermeasurePath) ?? ".");
                InitializeRun(CargoContract.BlackBoxRelay);
                runModification = AirframeModification.OpenAvionics;
                routeIndex = 4;
                battle.StartEncounter(EncounterId.Skirmish, runDeck, runHull, runCargoIntegrity, selectedContract,
                    runUpgrades, runModules, 3, runUpgradeBranches, 37001, runModification, routeStoryState);
                screen = ScreenMode.Battle;
                showFirstBattleGuide = false;
                paused = false;
                Time.timeScale = 1f;
                for (int i = 0; i < 8; i++)
                    yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(countermeasurePath);
                yield return WaitForCapture(countermeasurePath);
            }
            if (!string.IsNullOrEmpty(storyEventPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(storyEventPath) ?? ".");
                InitializeRun(CargoContract.BlackBoxRelay);
                runModification = AirframeModification.OpenAvionics;
                routeIndex = 5;
                selectedRouteNodeId = 12;
                lastCompletedRouteNodeId = 10;
                routeStoryState = RouteStoryState.PromiseStrengthened;
                eventResolved = false;
                eventResult = null;
                screen = ScreenMode.Event;
                controllerSelection = 0;
                paused = false;
                Time.timeScale = 1f;
                for (int i = 0; i < 8; i++)
                    yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(storyEventPath);
                yield return WaitForCapture(storyEventPath);
            }
            if (!string.IsNullOrEmpty(adaptiveBossPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(adaptiveBossPath) ?? ".");
                InitializeRun(CargoContract.BlackBoxRelay);
                runModification = AirframeModification.OpenAvionics;
                routeStoryState = RouteStoryState.PromiseFulfilled;
                routeIndex = 7;
                selectedRouteNodeId = 18;
                battle.StartEncounter(EncounterId.Boss, runDeck, runHull, runCargoIntegrity, selectedContract,
                    runUpgrades, runModules, 0, runUpgradeBranches, 39001, runModification, routeStoryState);
                EnemyState boss = battle.Enemies.Single();
                boss.Phase = 2;
                boss.PhaseTransitionPending = false;
                boss.Health = boss.MaxHealth / 2;
                boss.Armor = 4;
                boss.MaxArmor = Math.Max(boss.MaxArmor, boss.Armor);
                boss.ChargeTargetLane = battle.PlayerLane;
                boss.ChargeDamageTaken = 0;
                boss.ChargeInterrupted = false;
                screen = ScreenMode.Battle;
                showFirstBattleGuide = false;
                paused = false;
                Time.timeScale = 1f;
                for (int i = 0; i < 8; i++)
                    yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(adaptiveBossPath);
                yield return WaitForCapture(adaptiveBossPath);
            }
            if (!string.IsNullOrEmpty(cloudWyrmPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(cloudWyrmPath) ?? ".");
                InitializeRun(CargoContract.StormCore);
                runModification = AirframeModification.RedlineTurbine;
                routeStoryState = RouteStoryState.PromiseFulfilled;
                routeIndex = 7;
                selectedRouteNodeId = 19;
                battle.StartEncounter(EncounterId.Boss, runDeck, runHull, runCargoIntegrity, selectedContract,
                    runUpgrades, runModules, 1, runUpgradeBranches, 40001, runModification, routeStoryState);
                EnemyState boss = battle.Enemies.Single();
                boss.Phase = 2;
                boss.PhaseTransitionPending = false;
                boss.Health = boss.MaxHealth / 2;
                boss.Armor = 4;
                boss.MaxArmor = Math.Max(boss.MaxArmor, boss.Armor);
                boss.ChargeTargetLane = 0;
                boss.ChargeDamageTaken = 0;
                boss.ChargeInterrupted = false;
                screen = ScreenMode.Battle;
                showFirstBattleGuide = false;
                paused = false;
                Time.timeScale = 1f;
                for (int i = 0; i < 8; i++)
                    yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(cloudWyrmPath);
                yield return WaitForCapture(cloudWyrmPath);
            }
            if (!string.IsNullOrEmpty(dualFinaleMapPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dualFinaleMapPath) ?? ".");
                InitializeRun(CargoContract.StormCore);
                runModification = AirframeModification.RedlineTurbine;
                routeStoryState = RouteStoryState.PromiseFulfilled;
                completedRouteNodes.Add(0);
                completedRouteNodes.Add(2);
                completedRouteNodes.Add(4);
                completedRouteNodes.Add(7);
                completedRouteNodes.Add(10);
                completedRouteNodes.Add(13);
                completedRouteNodes.Add(16);
                lastCompletedRouteNodeId = 16;
                routeIndex = 7;
                selectedRouteNodeId = 19;
                screen = ScreenMode.Map;
                FocusRouteColumn(routeIndex);
                paused = false;
                Time.timeScale = 1f;
                for (int i = 0; i < 8; i++)
                    yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(dualFinaleMapPath);
                yield return WaitForCapture(dualFinaleMapPath);
            }
            if (!string.IsNullOrEmpty(airspaceMapPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(airspaceMapPath) ?? ".");
                InitializeRun(CargoContract.BlackBoxRelay);
                completedRouteNodes.Add(0);
                completedRouteNodes.Add(2);
                lastCompletedRouteNodeId = 2;
                routeIndex = 2;
                selectedRouteNodeId = 4;
                screen = ScreenMode.Map;
                FocusRouteColumn(routeIndex);
                paused = false;
                Time.timeScale = 1f;
                for (int i = 0; i < 8; i++)
                    yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(airspaceMapPath);
                yield return WaitForCapture(airspaceMapPath);
            }
            if (!string.IsNullOrEmpty(finalePreludePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(finalePreludePath) ?? ".");
                InitializeRun(CargoContract.BlackBoxRelay);
                runModification = AirframeModification.OpenAvionics;
                routeStoryState = RouteStoryState.PromiseFulfilled;
                routeIndex = 6;
                selectedRouteNodeId = 16;
                battle.StartEncounter(EncounterId.Elite, runDeck, runHull, runCargoIntegrity, selectedContract,
                    runUpgrades, runModules, 4, runUpgradeBranches, 44010, runModification, routeStoryState);
                screen = ScreenMode.Battle;
                showFirstBattleGuide = false;
                paused = false;
                Time.timeScale = 1f;
                for (int i = 0; i < 8; i++)
                    yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(finalePreludePath);
                yield return WaitForCapture(finalePreludePath);
            }
            if (!string.IsNullOrEmpty(finaleIntelPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(finaleIntelPath) ?? ".");
                InitializeRun(CargoContract.StormCore);
                runModification = AirframeModification.RedlineTurbine;
                routeStoryState = RouteStoryState.PromiseFulfilled;
                routeIntel = RouteIntel.CurtainCipher;
                routeIndex = 7;
                selectedRouteNodeId = 19;
                battle.StartEncounter(EncounterId.Boss, runDeck, runHull, runCargoIntegrity, selectedContract,
                    runUpgrades, runModules, 1, runUpgradeBranches, 44011, runModification, routeStoryState,
                    routeIntel);
                screen = ScreenMode.Battle;
                showFirstBattleGuide = false;
                paused = false;
                Time.timeScale = 1f;
                for (int i = 0; i < 8; i++)
                    yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(finaleIntelPath);
                yield return WaitForCapture(finaleIntelPath);
            }
            if (!string.IsNullOrEmpty(finaleEndingPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(finaleEndingPath) ?? ".");
                InitializeRun(CargoContract.StormCore);
                runModification = AirframeModification.RedlineTurbine;
                routeStoryState = RouteStoryState.PromiseFulfilled;
                routeIntel = RouteIntel.CurtainCipher;
                finaleEnding = FinaleEnding.WyrmSignalCovenant;
                routeIndex = 7;
                selectedRouteNodeId = 19;
                runHull = 24;
                runCargoIntegrity = 3;
                screen = ScreenMode.Complete;
                paused = false;
                Time.timeScale = 1f;
                for (int i = 0; i < 8; i++)
                    yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(finaleEndingPath);
                yield return WaitForCapture(finaleEndingPath);
            }
            if (!string.IsNullOrEmpty(finaleArchivePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(finaleArchivePath) ?? ".");
                archiveData = new DeliveryArchiveData();
                DeliveryArchiveService.RegisterRunStarted(archiveData, (int)CargoContract.StormCore, runDeck.Select(card => (int)card));
                DeliveryArchiveService.RegisterBattleStarted(archiveData,
                    new[] { (int)EnemyKind.CurtainHerald, (int)EnemyKind.FluxSkimmer, (int)EnemyKind.CloudWyrm },
                    runDeck.Select(card => (int)card), new[] { (int)ModuleId.VectorThruster });
                DeliveryArchiveService.RegisterBattleWon(archiveData);
                DeliveryArchiveService.RegisterRunResult(archiveData, new ArchivedRunRecord
                {
                    RunSeed = 440044,
                    Contract = (int)CargoContract.StormCore,
                    RouteNodeId = 19,
                    Encounter = (int)EncounterId.Boss,
                    CargoIntegrity = 3,
                    Hull = 24,
                    Credits = 96,
                    Turns = 34,
                    CardsPlayed = 71,
                    DeckCount = 17,
                    ModuleCount = 3,
                    RouteIntel = (int)RouteIntel.CurtainCipher,
                    FinaleEnding = (int)FinaleEnding.WyrmSignalCovenant,
                    BossKind = (int)EnemyKind.CloudWyrm
                }, true);
                DeliveryArchiveService.RegisterRunResult(archiveData, new ArchivedRunRecord
                {
                    RunSeed = 430043,
                    Contract = (int)CargoContract.BlackBoxRelay,
                    RouteNodeId = 18,
                    Encounter = (int)EncounterId.Boss,
                    CargoIntegrity = 2,
                    Hull = 11,
                    Credits = 82,
                    Turns = 39,
                    CardsPlayed = 80,
                    DeckCount = 18,
                    ModuleCount = 2,
                    RouteIntel = (int)RouteIntel.FluxCompass,
                    FinaleEnding = (int)FinaleEnding.MantaScavengerCrown,
                    BossKind = (int)EnemyKind.StormManta
                }, true);
                screen = ScreenMode.Archive;
                archivePage = 2;
                paused = false;
                Time.timeScale = 1f;
                for (int i = 0; i < 8; i++)
                    yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(finaleArchivePath);
                yield return WaitForCapture(finaleArchivePath);
            }
            if (!string.IsNullOrEmpty(progressionArchivePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(progressionArchivePath) ?? ".");
                archiveData = new DeliveryArchiveData();
                for (int i = 0; i < ContractCatalog.All.Count; i++)
                {
                    CargoContract contract = ContractCatalog.All[i];
                    ChallengeId challenge = i < 3
                        ? ChallengeCatalog.All[i + 1].Id
                        : ChallengeId.Standard;
                    int[] deck =
                    {
                        (int)CardId.BurstFire,
                        (int)CardId.WindGuard,
                        (int)ContractCatalog.StarterCard(contract)
                    };
                    DeliveryArchiveService.RegisterRunStarted(
                        archiveData, (int)contract, deck, (int)challenge);
                    DeliveryArchiveService.RegisterRunResult(archiveData, new ArchivedRunRecord
                    {
                        RunSeed = 470100 + i,
                        Contract = (int)contract,
                        Challenge = (int)challenge,
                        RouteNodeId = i % 2 == 0 ? 18 : 19,
                        Encounter = (int)EncounterId.Boss,
                        CargoIntegrity = i % 3 == 0 ? 3 : 2,
                        Hull = 20 + i,
                        Credits = 70 + i * 5,
                        Turns = 30 + i,
                        CardsPlayed = 60 + i * 3,
                        DeckCount = 16,
                        ModuleCount = 2,
                        BossKind = i % 2 == 0
                            ? (int)EnemyKind.StormManta
                            : (int)EnemyKind.CloudWyrm,
                        FinaleEnding = i % 2 == 0
                            ? (int)FinaleEnding.MantaCalmSea
                            : (int)FinaleEnding.WyrmClearSky
                    }, true);
                }
                screen = ScreenMode.Archive;
                archivePage = 1;
                paused = false;
                Time.timeScale = 1f;
                for (int i = 0; i < 8; i++)
                    yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(progressionArchivePath);
                yield return WaitForCapture(progressionArchivePath);
            }
            Application.Quit();
        }

        private static IEnumerator WaitForCapture(string path)
        {
            float deadline = Time.realtimeSinceStartup + 5f;
            while (!File.Exists(path) && Time.realtimeSinceStartup < deadline)
                yield return null;
            yield return new WaitForSecondsRealtime(0.25f);
        }

        private void Update()
        {
            HandleControllerInput();
            if (screen == ScreenMode.Battle && battle.Defeat && !archiveFailureRecorded)
                RegisterArchiveFailure();
            AudioClip target = MusicForCurrentScreen();
            if (target != null && target != activeMusic)
                BeginMusicTransition(target);

            if (audioSource != null)
                audioSource.volume = 0.86f * sfxVolume;
            if (audioLayerSource != null)
                audioLayerSource.volume = 0.78f * sfxVolume;
            float fade = Mathf.Clamp01((Time.unscaledTime - bgmFadeStartedAt) / BgmFadeDuration);
            if (bgmSource != null)
                bgmSource.volume = BgmVolume * musicVolume * fade;
            if (bgmFadeSource != null)
            {
                bgmFadeSource.volume = BgmVolume * musicVolume * (1f - fade);
                if (fade >= 1f && bgmFadeSource.isPlaying)
                    bgmFadeSource.Stop();
            }

        }

        private void HandleControllerInput()
        {
            bool joystickConnected = Input.GetJoystickNames().Any(name => !string.IsNullOrWhiteSpace(name));
            bool controllerButton = Input.GetKeyDown(KeyCode.JoystickButton0) ||
                Input.GetKeyDown(KeyCode.JoystickButton1) ||
                Input.GetKeyDown(KeyCode.JoystickButton3) ||
                Input.GetKeyDown(KeyCode.JoystickButton7);
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            bool controllerAxis = joystickConnected && (Mathf.Abs(horizontal) > 0.55f || Mathf.Abs(vertical) > 0.55f);
            if (controllerButton || controllerAxis)
            {
                controllerActive = true;
                keyboardFocusActive = false;
            }
            if (!controllerActive)
                return;

            string nextContext = settingsOpen ? "Settings" : paused ? "Pause" : showFirstBattleGuide ? "Guide" :
                screen == ScreenMode.Battle ? $"Battle-{battle.Victory}-{battle.Defeat}" : screen.ToString();
            if (controllerContext != nextContext)
            {
                controllerContext = nextContext;
                controllerSelection = screen == ScreenMode.Contract ? ContractIndex(selectedContract) : 0;
                if (screen == ScreenMode.Contract)
                {
                    contractCarouselTarget = controllerSelection;
                    contractCarouselVisual = contractCarouselTarget;
                    contractCarouselVelocity = 0f;
                }
                controllerAxisHeld = false;
            }

            if (Input.GetKeyDown(KeyCode.JoystickButton7) && !settingsOpen && !showFirstBattleGuide &&
                screen != ScreenMode.Title && screen != ScreenMode.Archive && screen != ScreenMode.Challenge)
            {
                SetPaused(!paused);
                PlaySound(clickSound, 0.9f, 0.45f);
                return;
            }

            if (Input.GetKeyDown(KeyCode.JoystickButton1))
            {
                if (settingsOpen)
                    CloseSettings();
                else if (paused)
                    SetPaused(false);
                else if (screen == ScreenMode.Archive)
                    screen = ScreenMode.Title;
                else if (screen == ScreenMode.Challenge)
                    screen = ScreenMode.Title;
                else if (screen == ScreenMode.DeckPurge || screen == ScreenMode.FinalTrim ||
                    screen == ScreenMode.ShopPurge || screen == ScreenMode.EventPurge)
                    CancelDeckPurge();
                else if (screen == ScreenMode.WorkshopCardSelect)
                    CancelWorkshop();
                else if (screen == ScreenMode.WorkshopBranch)
                    BackToWorkshopCards();
                else if (screen == ScreenMode.CoreUpgrade)
                    CancelCoreUpgrade();
                else if (screen != ScreenMode.Title && !showFirstBattleGuide)
                    SetPaused(true);
                return;
            }

            int horizontalStep = 0;
            int verticalStep = 0;
            bool axisPressed = Mathf.Abs(horizontal) > 0.55f || Mathf.Abs(vertical) > 0.55f;
            if (!axisPressed)
            {
                controllerAxisHeld = false;
            }
            else if (!controllerAxisHeld || Time.unscaledTime >= controllerNextMoveAt)
            {
                if (Mathf.Abs(horizontal) >= Mathf.Abs(vertical))
                    horizontalStep = horizontal > 0f ? 1 : -1;
                else
                    verticalStep = vertical > 0f ? -1 : 1;
                controllerAxisHeld = true;
                controllerNextMoveAt = Time.unscaledTime + 0.2f;
            }

            if (horizontalStep != 0 || verticalStep != 0)
                MoveControllerSelection(horizontalStep, verticalStep);

            if (Input.GetKeyDown(KeyCode.JoystickButton3) && screen == ScreenMode.Battle && !paused &&
                !settingsOpen && !showFirstBattleGuide && !battle.Victory && !battle.Defeat)
            {
                EndTurnWithFeedback();
                return;
            }

            if (Input.GetKeyDown(KeyCode.JoystickButton0))
                ActivateControllerSelection();
        }

        private void MoveControllerSelection(int horizontal, int vertical)
        {
            int direction = horizontal != 0 ? horizontal : vertical;
            if (settingsOpen)
            {
                if (horizontal != 0 && controllerSelection < 11)
                    AdjustControllerSetting(horizontal);
                else
                    controllerSelection = WrapSelection(controllerSelection + direction, 12);
                PlaySound(clickSound, 1.25f, 0.22f);
                return;
            }
            if (paused)
            {
                controllerSelection = WrapSelection(controllerSelection + direction, 5);
                PlaySound(clickSound, 1.25f, 0.22f);
                return;
            }
            if (showFirstBattleGuide)
            {
                if (tutorialRulebookMode && direction != 0)
                    CycleTutorialTopic(direction);
                return;
            }
            if (screen == ScreenMode.Contract)
            {
                if (horizontal != 0)
                    CycleContractPreview(horizontal);
                else if (vertical != 0)
                    CycleContractPreview(vertical);
                return;
            }

            int count = 1;
            switch (screen)
            {
                case ScreenMode.Title:
                    count = RunSaveService.HasSave ? 4 : 3;
                    break;
                case ScreenMode.Archive:
                    count = 5;
                    break;
                case ScreenMode.Challenge:
                    count = ChallengeCatalog.All.Count;
                    break;
                case ScreenMode.DepartureBriefing:
                    count = 3;
                    break;
                case ScreenMode.Map:
                {
                    RouteNodeDefinition[] available = route.AtColumn(routeIndex).Where(IsRouteNodeAvailable).ToArray();
                    if (available.Length > 0)
                    {
                        int current = Array.FindIndex(available, node => node.Id == selectedRouteNodeId);
                        current = WrapSelection((current < 0 ? 0 : current) + direction, available.Length);
                        SelectRouteNode(available[current].Id);
                    }
                    return;
                }
                case ScreenMode.Retrofit:
                    count = 3;
                    break;
                case ScreenMode.FinalApproach:
                    count = 4;
                    break;
                case ScreenMode.Battle:
                    count = battle.Victory ? 1 : battle.Defeat ? 3 : Mathf.Max(1, battle.Hand.Count + 1);
                    break;
                case ScreenMode.Reward:
                    count = 4;
                    break;
                case ScreenMode.Shop:
                    count = 7;
                    break;
                case ScreenMode.Event:
                    count = eventResolved ? 1 : 3;
                    break;
                case ScreenMode.Rest:
                    count = restResolved ? 1 : 3;
                    break;
                case ScreenMode.DeckPurge:
                case ScreenMode.FinalTrim:
                case ScreenMode.ShopPurge:
                case ScreenMode.EventPurge:
                    count = PurgeCandidates().Length + 1;
                    break;
                case ScreenMode.WorkshopCardSelect:
                    count = WorkshopCandidates().Length + 1;
                    break;
                case ScreenMode.WorkshopBranch:
                    count = 3;
                    break;
                case ScreenMode.CoreUpgrade:
                    count = 3;
                    break;
                case ScreenMode.Complete:
                    count = 3;
                    break;
            }
            controllerSelection = WrapSelection(controllerSelection + direction, count);
            if (screen == ScreenMode.DeckPurge || screen == ScreenMode.FinalTrim ||
                screen == ScreenMode.ShopPurge || screen == ScreenMode.EventPurge)
            {
                int candidateCount = PurgeCandidates().Length;
                if (controllerSelection < candidateCount)
                    deckPurgePage = controllerSelection / 10;
            }
            else if (screen == ScreenMode.WorkshopCardSelect)
            {
                int candidateCount = WorkshopCandidates().Length;
                if (controllerSelection < candidateCount)
                    workshopPage = controllerSelection / 10;
            }
            PlaySound(clickSound, 1.25f, 0.22f);
        }

        private void ActivateControllerSelection()
        {
            if (settingsOpen)
            {
                if (controllerSelection < 11)
                    AdjustControllerSetting(1);
                else if (controllerSelection == 11)
                    CloseSettings();
                return;
            }
            if (paused)
            {
                switch (controllerSelection)
                {
                    case 0:
                        SetPaused(false);
                        break;
                    case 1:
                        OpenSettings(true);
                        break;
                    case 2:
                        SetPaused(false);
                        OpenRulebook();
                        break;
                    case 3:
                        SetPaused(false);
                        StartNewRun();
                        break;
                    case 4:
                        SetPaused(false);
                        screen = ScreenMode.Title;
                        break;
                }
                return;
            }
            if (showFirstBattleGuide)
            {
                CloseTutorialOverlay();
                return;
            }

            switch (screen)
            {
                case ScreenMode.Title:
                    if (RunSaveService.HasSave)
                    {
                        if (controllerSelection == 0)
                            TryContinueRun();
                        else if (controllerSelection == 1)
                            StartNewRun();
                        else if (controllerSelection == 2)
                            screen = ScreenMode.Archive;
                        else
                            OpenSettings(false);
                    }
                    else if (controllerSelection == 0)
                        StartNewRun();
                    else if (controllerSelection == 1)
                        screen = ScreenMode.Archive;
                    else
                        OpenSettings(false);
                    break;
                case ScreenMode.Archive:
                    if (controllerSelection < 4)
                        archivePage = controllerSelection;
                    else
                        screen = ScreenMode.Title;
                    break;
                case ScreenMode.Challenge:
                    SelectChallenge(controllerSelection);
                    break;
                case ScreenMode.Contract:
                    InitializeRun(ContractCatalog.All[Mathf.Clamp(controllerSelection, 0,
                        ContractCatalog.All.Count - 1)]);
                    break;
                case ScreenMode.DepartureBriefing:
                    ApplyDepartureDirective((DepartureDirective)(Mathf.Clamp(controllerSelection, 0, 2) + 2));
                    break;
                case ScreenMode.Map:
                    EnterCurrentNode();
                    break;
                case ScreenMode.Retrofit:
                    InstallAirframeModification((AirframeModification)(controllerSelection + 1));
                    break;
                case ScreenMode.FinalApproach:
                    SelectFinalApproachOption(controllerSelection);
                    break;
                case ScreenMode.Battle:
                    if (battle.Victory && !DeathAnimationActive())
                        ContinueAfterVictory();
                    else if (battle.Defeat)
                    {
                        if (controllerSelection == 0)
                            RestartSameSeed();
                        else if (controllerSelection == 1 &&
                                 ChallengeCatalog.Get(currentChallenge).FixedSeed == 0)
                            RestartSameContract();
                        else if (controllerSelection == 2)
                            ChangeContractAfterFailure();
                    }
                    else if (controllerSelection >= battle.Hand.Count)
                        EndTurnWithFeedback();
                    else if (CanPlayInteractive(controllerSelection))
                        PlayCardWithFeedback(controllerSelection);
                    break;
                case ScreenMode.Reward:
                    if (rewardSelectionLocked)
                        break;
                    if (controllerSelection < 3)
                        ChooseRewardChoice(CurrentRewardChoices()[controllerSelection], controllerSelection);
                    else
                        SkipReward();
                    break;
                case ScreenMode.Shop:
                    if (controllerSelection < 3)
                        TryBuyShopOffer(controllerSelection);
                    else if (controllerSelection == 3)
                        TryBuyShopRepair();
                    else if (controllerSelection == 4)
                        OpenShopPurge();
                    else if (controllerSelection == 5)
                        OpenWorkshopCardSelect();
                    else
                        LeaveShop();
                    break;
                case ScreenMode.Event:
                    if (eventResolved)
                        LeaveRouteEvent();
                    else if (controllerSelection == 0)
                        ResolveRouteEvent(true);
                    else if (controllerSelection == 1)
                        ResolveRouteEvent(false);
                    else
                        OpenEventPurge();
                    break;
                case ScreenMode.Rest:
                    if (restResolved)
                        LeaveRestStop();
                    else if (controllerSelection == 0)
                        ResolveRestRepair();
                    else if (controllerSelection == 1)
                        OpenCoreUpgrade();
                    else
                        OpenDeckPurge();
                    break;
                case ScreenMode.DeckPurge:
                case ScreenMode.FinalTrim:
                case ScreenMode.ShopPurge:
                case ScreenMode.EventPurge:
                {
                    CardId[] candidates = PurgeCandidates();
                    if (controllerSelection < candidates.Length)
                        RemoveCardFromDeck(candidates[controllerSelection]);
                    else
                        CancelDeckPurge();
                    break;
                }
                case ScreenMode.WorkshopCardSelect:
                {
                    CardId[] candidates = WorkshopCandidates();
                    if (controllerSelection < candidates.Length)
                        SelectWorkshopCard(candidates[controllerSelection]);
                    else
                        CancelWorkshop();
                    break;
                }
                case ScreenMode.WorkshopBranch:
                    if (controllerSelection < 2)
                        ApplyWorkshopBranch(controllerSelection == 0 ? UpgradeBranch.Alpha : UpgradeBranch.Beta);
                    else
                        BackToWorkshopCards();
                    break;
                case ScreenMode.CoreUpgrade:
                    if (controllerSelection < 2)
                        ApplyCoreUpgrade(controllerSelection == 0 ? UpgradeBranch.Alpha : UpgradeBranch.Beta);
                    else
                        CancelCoreUpgrade();
                    break;
                case ScreenMode.Complete:
                    if (controllerSelection == 0)
                        StartNewRun();
                    else if (controllerSelection == 1)
                        screen = ScreenMode.Archive;
                    else
                        screen = ScreenMode.Title;
                    break;
            }
        }

        private void AdjustControllerSetting(int direction)
        {
            switch (controllerSelection)
            {
                case 0:
                    if (direction < 0) CycleDisplayModeBackward(); else CycleDisplayModeForward();
                    break;
                case 1:
                    CycleResolution(direction);
                    break;
                case 2:
                    ToggleVSync();
                    break;
                case 3:
                    CycleFrameRate(direction);
                    break;
                case 4:
                    CycleLanguage();
                    break;
                case 5:
                    gameSettings.ContextualTutorials = !gameSettings.ContextualTutorials;
                    SaveAndApplySettings(false);
                    break;
                case 6:
                    gameSettings.FocusHints = !gameSettings.FocusHints;
                    SaveAndApplySettings(false);
                    break;
                case 7:
                    musicVolume = Mathf.Clamp01(musicVolume + direction * 0.05f);
                    SaveAndApplySettings(false);
                    break;
                case 8:
                    sfxVolume = Mathf.Clamp01(sfxVolume + direction * 0.05f);
                    SaveAndApplySettings(false);
                    break;
                case 9:
                    gameSettings.ShakeIntensity = Mathf.Clamp01(gameSettings.ShakeIntensity + direction * 0.05f);
                    SaveAndApplySettings(false);
                    break;
                case 10:
                    gameSettings.FlashIntensity = Mathf.Clamp01(gameSettings.FlashIntensity + direction * 0.05f);
                    SaveAndApplySettings(false);
                    break;
            }
        }

        private static int WrapSelection(int value, int count)
        {
            if (count <= 0)
                return 0;
            return (value % count + count) % count;
        }

        private static string L(string key, string chineseFallback, params object[] arguments)
        {
            return LocalizationService.Text(key, chineseFallback, arguments);
        }

        private AudioSource CreateMusicSource()
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.volume = 0f;
            source.ignoreListenerPause = true;
            return source;
        }

        private AudioClip MusicForCurrentScreen()
        {
            return screen switch
            {
                ScreenMode.Title => titleMusic,
                ScreenMode.Archive => titleMusic,
                ScreenMode.Contract => titleMusic,
                ScreenMode.DepartureBriefing => routeMusic,
                ScreenMode.Map => routeMusic,
                ScreenMode.Retrofit => restMusic,
                ScreenMode.FinalApproach => restMusic,
                ScreenMode.Battle => battle.Encounter == EncounterId.Boss ? bossMusic : battleMusic,
                ScreenMode.Reward => restMusic,
                ScreenMode.Shop => restMusic,
                ScreenMode.ShopPurge => restMusic,
                ScreenMode.WorkshopCardSelect => restMusic,
                ScreenMode.WorkshopBranch => restMusic,
                ScreenMode.Rest => restMusic,
                ScreenMode.DeckPurge => restMusic,
                ScreenMode.FinalTrim => restMusic,
                ScreenMode.CoreUpgrade => restMusic,
                ScreenMode.Event => routeMusic,
                ScreenMode.EventPurge => routeMusic,
                ScreenMode.Complete => titleMusic,
                _ => titleMusic
            };
        }

        private void BeginMusicTransition(AudioClip next)
        {
            AudioSource previous = bgmSource;
            bgmSource = bgmFadeSource;
            bgmFadeSource = previous;
            activeMusic = next;
            bgmFadeStartedAt = Time.unscaledTime;
            bgmSource.clip = next;
            bgmSource.volume = 0f;
            bgmSource.Play();
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
        }

        private void OnDestroy()
        {
            RunDiagnosticsService.Shutdown();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                SaveRunCheckpoint();
        }

        private void OnApplicationQuit()
        {
            SaveRunCheckpoint();
            RecordRunDiagnostic("session_ended");
            RunDiagnosticsService.Shutdown();
        }

        private void OnGUI()
        {
            EnsureStyles();
            Matrix4x4 previous = GUI.matrix;
            float scale = Mathf.Min(Screen.width / ReferenceWidth, Screen.height / ReferenceHeight);
            float offsetX = (Screen.width - ReferenceWidth * scale) * 0.5f;
            float offsetY = (Screen.height - ReferenceHeight * scale) * 0.5f;
            float shakeX = 0f;
            float shakeY = 0f;
            if (Time.time < shakeUntil)
            {
                float remaining = Mathf.Clamp01((shakeUntil - Time.time) / 0.28f);
                shakeX = Mathf.Sin(Time.time * 113f) * shakeMagnitude * remaining * gameSettings.ShakeIntensity;
                shakeY = Mathf.Cos(Time.time * 97f) * shakeMagnitude * 0.55f * remaining * gameSettings.ShakeIntensity;
            }
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX + shakeX * scale, offsetY + shakeY * scale, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));

            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseMove ||
                Event.current.type == EventType.ScrollWheel)
            {
                controllerActive = false;
                keyboardFocusActive = false;
            }
            if (Event.current.type == EventType.KeyDown)
            {
                bool keyboardInput = Event.current.keyCode < KeyCode.JoystickButton0 ||
                    Event.current.keyCode > KeyCode.Joystick8Button19;
                if (keyboardInput)
                {
                    controllerActive = false;
                    keyboardFocusActive = true;
                }
                HandleKeyboardShortcuts(Event.current);
            }
            if (Event.current.type == EventType.Repaint)
                hoverKeyThisFrame = null;

            bool modalOpen = paused || showFirstBattleGuide || settingsOpen;
            bool previousEnabled = GUI.enabled;
            GUI.enabled = !modalOpen;
            DrawSky();
            switch (screen)
            {
                case ScreenMode.Title:
                    DrawTitleScreen();
                    break;
                case ScreenMode.Archive:
                    DrawArchiveScreen();
                    break;
                case ScreenMode.Challenge:
                    DrawChallengeScreen();
                    break;
                case ScreenMode.Contract:
                    DrawContractScreen();
                    break;
                case ScreenMode.DepartureBriefing:
                    DrawDepartureBriefing();
                    break;
                case ScreenMode.Map:
                    DrawRouteMap();
                    break;
                case ScreenMode.Retrofit:
                    DrawRetrofitScreen();
                    break;
                case ScreenMode.FinalApproach:
                    DrawFinalApproachScreen();
                    break;
                case ScreenMode.Battle:
                    DrawBattleScreen();
                    break;
                case ScreenMode.Reward:
                    DrawRewardScreen();
                    break;
                case ScreenMode.Shop:
                    DrawShopScreen();
                    break;
                case ScreenMode.Event:
                    DrawEventScreen();
                    break;
                case ScreenMode.Rest:
                    DrawRestScreen();
                    break;
                case ScreenMode.DeckPurge:
                case ScreenMode.FinalTrim:
                case ScreenMode.ShopPurge:
                case ScreenMode.EventPurge:
                    DrawDeckPurgeScreen();
                    break;
                case ScreenMode.WorkshopCardSelect:
                    DrawWorkshopCardSelect();
                    break;
                case ScreenMode.WorkshopBranch:
                    DrawWorkshopBranch();
                    break;
                case ScreenMode.CoreUpgrade:
                    DrawCoreUpgradeScreen();
                    break;
                case ScreenMode.Complete:
                    DrawRunComplete();
                    break;
            }
            GUI.enabled = previousEnabled;

            DrawScreenTexture();

            if (screen != ScreenMode.Title && screen != ScreenMode.Archive && screen != ScreenMode.Challenge &&
                !showFirstBattleGuide && !paused && !settingsOpen)
                DrawSystemButton();
            if (paused && !settingsOpen)
                DrawPauseOverlay();
            else if (showFirstBattleGuide)
                DrawFirstBattleGuide();
            if (settingsOpen)
                DrawSettingsOverlay();
            if ((controllerActive || keyboardFocusActive) && (gameSettings.FocusHints || settingsOpen))
                DrawControllerFocus();

            if (Event.current.type == EventType.Repaint)
            {
                if (!string.IsNullOrEmpty(hoverKeyThisFrame) && hoverKeyThisFrame != hoverKeyLastFrame)
                    PlaySound(clickSound, 1.65f, 0.16f);
                hoverKeyLastFrame = hoverKeyThisFrame;
            }

            GUI.matrix = previous;
        }

        private void DrawTitleScreen()
        {
            DrawRect(new Rect(585, 170, 760, 585), new Color32(5, 10, 30, 218));
            DrawNeonFrame(new Rect(585, 170, 760, 585), NeonCyan, 3f);
            DrawRect(new Rect(585, 170, 10, 585), NeonViolet);
            DrawEngineTrail(new Vector2(250f, 425f));
            DrawPixelPlane(new Vector2(250f, 425f), 2.8f, false);
            DrawRect(new Rect(0, 0, ReferenceWidth, 12), NeonCyan);
            DrawRect(new Rect(0, ReferenceHeight - 12, ReferenceWidth, 12), NeonViolet);

            Color previous = GUI.color;
            GUI.color = new Color(1f, 0.22f, 0.95f, 0.38f);
            GUI.Label(new Rect(633, 231, 650, 100), L("title.game", "云海邮差"), neonTitleStyle);
            GUI.color = new Color(0.18f, 0.95f, 1f, 0.45f);
            GUI.Label(new Rect(621, 225, 650, 100), L("title.game", "云海邮差"), neonTitleStyle);
            GUI.color = previous;
            GUI.Label(new Rect(625, 225, 650, 100), L("title.game", "云海邮差"), neonTitleStyle);
            DrawFittedLabel(new Rect(630, 315, 650, 50), L("title.subtitle", "三航道空战卡牌肉鸽"),
                neonSubtitleStyle, 12);
            GUI.Label(new Rect(630, 385, 610, 130),
                L("title.pitch", "运送不可能送达的包裹，穿越风暴云海。\n观察敌人意图，灵活切换航道。\n压榨老旧引擎——但别让它烧起来。"),
                neonBodyStyle);

            DrawRect(new Rect(630, 520, 95, 24), new Color32(17, 34, 67, 245));
            GUI.Label(new Rect(630, 520, 95, 24), "DECK OPS", tinyStyle);
            DrawRect(new Rect(735, 520, 95, 24), new Color32(17, 34, 67, 245));
            GUI.Label(new Rect(735, 520, 95, 24), "AIR LANE", tinyStyle);

            bool hasSave = RunSaveService.HasSave;
            if (hasSave)
            {
                DrawPixelButton(new Rect(630, 545, 330, 68), L("title.continue", "继续配送"), NeonCyan, TryContinueRun, true, "ENTER");
                DrawPixelButton(new Rect(630, 630, 330, 62), L("title.new", "新配送"), PostalRed, StartNewRun);
                DrawPixelButton(new Rect(985, 545, 250, 68), L("title.archive", "邮政档案"), Gold,
                    () => screen = ScreenMode.Archive);
            }
            else
            {
                DrawPixelButton(new Rect(630, 555, 330, 74), L("title.start", "开始配送"), PostalRed, StartNewRun, true, "ENTER");
                DrawPixelButton(new Rect(985, 555, 250, 55), L("title.archive", "邮政档案"), Gold,
                    () => screen = ScreenMode.Archive);
            }
            DrawPixelButton(new Rect(985, hasSave ? 630 : 625, 250, hasSave ? 62 : 55),
                L("title.settings", "系统设置"), Shadow,
                () => OpenSettings(false));

            if (!string.IsNullOrEmpty(saveStatusMessage) && Time.unscaledTime < saveStatusUntil)
                DrawFittedLabel(new Rect(985, 500, 300, 38), saveStatusMessage, tinyStyle, 8);
            DrawFittedLabel(new Rect(630, 710, 650, 32), L("title.version",
                "v0.52  //  ACCESSIBLE FLIGHT MANUAL"), hudStyle, 11);
        }

        private void DrawChallengeScreen()
        {
            archiveData ??= new DeliveryArchiveData();
            DeliveryArchiveService.Normalize(archiveData);
            DrawRect(new Rect(70, 48, 1460, 805), new Color32(2, 7, 22, 250));
            DrawRect(new Rect(78, 56, 1444, 789), PanelNight);
            DrawNeonFrame(new Rect(78, 56, 1444, 789), NeonCyan, 3f);
            DrawRect(new Rect(78, 56, 1444, 10), Gold);
            DrawFittedLabel(new Rect(125, 82, 840, 62),
                L("challenge.title", "选择本次派遣"), neonTitleStyle, 26);
            DrawFittedLabel(new Rect(1000, 92, 430, 35),
                L("challenge.board", "DISPATCH BOARD // NO POWER BONUSES"), hudCenteredStyle, 8);
            DrawFittedLabel(new Rect(130, 145, 1340, 42),
                L("challenge.subtitle", "挑战使用固定种子与公开限制；完成后只解锁档案、签章与精通记录。"),
                neonBodyStyle, 11);

            for (int index = 0; index < ChallengeCatalog.All.Count; index++)
            {
                ChallengeDefinition challenge = ChallengeCatalog.All[index];
                Rect rect = new Rect(155 + index % 2 * 655, 215 + index / 2 * 245, 610, 205);
                DrawChallengeCard(challenge, rect, index);
            }

            List<ProgressGoal> goals = LongTermProgressionRules.NextGoals(archiveData, 2);
            string goalLine = goals.Count == 0
                ? L("goal.complete", "全部长期目标均已完成；可以继续刷新挑战成绩。")
                : string.Join("　//　", goals.Select(ProgressGoalLabel));
            DrawRect(new Rect(155, 720, 1265, 58), new Color32(7, 18, 43, 245));
            DrawPixelOutline(new Rect(155, 720, 1265, 58), NeonViolet, 2f);
            DrawFittedLabel(new Rect(175, 730, 1225, 36),
                L("challenge.next_goal", "下一局目标 // {0}", goalLine), hudCenteredStyle, 9);
            DrawFittedLabel(new Rect(430, 802, 740, 26),
                L("challenge.controls", "点击派遣卡　方向键选择　1—4 快速开始　ESC 返回"), tinyStyle, 8);
        }

        private void DrawChallengeCard(ChallengeDefinition challenge, Rect rect, int index)
        {
            bool selected = controllerSelection == index;
            bool hovered = rect.Contains(Event.current.mousePosition);
            ChallengeProgressRecord progress = archiveData.ChallengeProgress.FirstOrDefault(record =>
                record.Challenge == (int)challenge.Id);
            bool completed = challenge.Id == ChallengeId.Standard || (progress?.Completions ?? 0) > 0;
            Color color = challenge.Id == ChallengeId.Standard
                ? NeonCyan
                : completed ? new Color32(83, 220, 158, 255) : Gold;
            DrawRect(new Rect(rect.x + 8, rect.y + 8, rect.width, rect.height), new Color32(1, 5, 18, 255));
            DrawRect(rect, new Color32(8, 20, 45, 252));
            DrawPixelOutline(rect, selected || hovered ? Color.Lerp(color, Color.white, 0.22f) : color,
                selected ? 4f : 2f);
            DrawRect(new Rect(rect.x, rect.y, 8, rect.height), color);
            DrawRect(new Rect(rect.x, rect.y, rect.width, 48), new Color32(12, 34, 66, 255));
            DrawFittedLabel(new Rect(rect.x + 24, rect.y + 8, 330, 32), ChallengeName(challenge.Id),
                neonSubtitleStyle, 12);
            DrawFittedLabel(new Rect(rect.x + 370, rect.y + 10, 215, 28),
                challenge.Id == ChallengeId.Standard
                    ? L("challenge.random_seed", "RANDOM SEED")
                    : $"FIXED SEED {challenge.FixedSeed:X8}", tinyStyle, 8);
            DrawFittedLabel(new Rect(rect.x + 28, rect.y + 60, rect.width - 56, 62),
                ChallengeRule(challenge.Id), neonBodyStyle, 10);
            DrawFittedLabel(new Rect(rect.x + 28, rect.y + 126, rect.width - 255, 28),
                ChallengeProgressLabel(challenge.Id, progress), tinyStyle, 8);
            DrawPixelButton(new Rect(rect.x + rect.width - 205, rect.y + 135, 175, 48),
                L("challenge.depart", "选择派遣"), color, () => SelectChallenge(index), true,
                (index + 1).ToString());
        }

        private void SelectChallenge(int index)
        {
            int clamped = Mathf.Clamp(index, 0, ChallengeCatalog.All.Count - 1);
            BeginContractSelection(ChallengeCatalog.All[clamped].Id);
        }

        private static string ChallengeName(ChallengeId challenge)
        {
            return challenge switch
            {
                ChallengeId.RedlineRelay => L("challenge.RedlineRelay.name", "红线中继"),
                ChallengeId.NoSafeHarbor => L("challenge.NoSafeHarbor.name", "无港航线"),
                ChallengeId.LeanManifest => L("challenge.LeanManifest.name", "轻装清单"),
                _ => L("challenge.Standard.name", "标准派遣")
            };
        }

        private static string ChallengeRule(ChallengeId challenge)
        {
            return challenge switch
            {
                ChallengeId.RedlineRelay => L("challenge.RedlineRelay.rule",
                    "每场战斗以3点热量开始。固定种子让热量规划与路线选择可以重复比较。"),
                ChallengeId.NoSafeHarbor => L("challenge.NoSafeHarbor.rule",
                    "战斗胜利后不再获得自动抢修；维修坞和补给站仍可使用。"),
                ChallengeId.LeanManifest => L("challenge.LeanManifest.rule",
                    "本次配送只以28点机体耐久出发，其余规则与奖励保持标准。"),
                _ => L("challenge.Standard.rule", "使用随机种子与标准规则，适合探索新合同和未发现结局。")
            };
        }

        private static string ChallengeProgressLabel(ChallengeId challenge, ChallengeProgressRecord progress)
        {
            if (challenge == ChallengeId.Standard)
                return L("challenge.standard_note", "自由配送 · 不计入挑战完成数");
            if (progress == null || progress.Attempts <= 0)
                return L("challenge.untried", "尚未尝试");
            return L("challenge.progress", "完成 {0} / 尝试 {1}　最佳货物 {2}",
                progress.Completions, progress.Attempts,
                progress.BestCargo < 0 ? "--" : CargoGrade(progress.BestCargo));
        }

        private static int ContractIndex(CargoContract contract)
        {
            for (int i = 0; i < ContractCatalog.All.Count; i++)
            {
                if (ContractCatalog.All[i] == contract)
                    return i;
            }
            return 0;
        }

        private void HandleKeyboardShortcuts(Event input)
        {
            if (showFirstBattleGuide)
            {
                if (tutorialRulebookMode && (input.keyCode == KeyCode.LeftArrow ||
                    input.keyCode == KeyCode.UpArrow || input.keyCode == KeyCode.A ||
                    input.keyCode == KeyCode.W))
                    CycleTutorialTopic(-1);
                else if (tutorialRulebookMode && (input.keyCode == KeyCode.RightArrow ||
                    input.keyCode == KeyCode.DownArrow || input.keyCode == KeyCode.D ||
                    input.keyCode == KeyCode.S))
                    CycleTutorialTopic(1);
                else if (input.keyCode == KeyCode.Escape || input.keyCode == KeyCode.Return ||
                    input.keyCode == KeyCode.KeypadEnter || input.keyCode == KeyCode.Space)
                    CloseTutorialOverlay();
                else
                    return;
                input.Use();
                return;
            }
            if (input.keyCode == KeyCode.F1 && screen != ScreenMode.Title)
            {
                OpenRulebook();
                input.Use();
                return;
            }
            if (settingsOpen)
            {
                bool settingsHandled = true;
                if (input.keyCode == KeyCode.Escape)
                    CloseSettings();
                else if (input.keyCode == KeyCode.UpArrow || input.keyCode == KeyCode.W)
                    controllerSelection = WrapSelection(controllerSelection - 1, 12);
                else if (input.keyCode == KeyCode.DownArrow || input.keyCode == KeyCode.S)
                    controllerSelection = WrapSelection(controllerSelection + 1, 12);
                else if ((input.keyCode == KeyCode.LeftArrow || input.keyCode == KeyCode.A) &&
                    controllerSelection < 11)
                    AdjustControllerSetting(-1);
                else if ((input.keyCode == KeyCode.RightArrow || input.keyCode == KeyCode.D) &&
                    controllerSelection < 11)
                    AdjustControllerSetting(1);
                else if (input.keyCode == KeyCode.Return || input.keyCode == KeyCode.KeypadEnter ||
                    input.keyCode == KeyCode.Space)
                {
                    if (controllerSelection == 11)
                        CloseSettings();
                    else
                        AdjustControllerSetting(1);
                }
                else
                    settingsHandled = false;

                if (settingsHandled)
                {
                    PlaySound(clickSound, 1.25f, 0.22f);
                    input.Use();
                }
                return;
            }
            if (input.keyCode == KeyCode.Escape && screen == ScreenMode.Archive)
            {
                screen = ScreenMode.Title;
                input.Use();
                return;
            }
            if (input.keyCode == KeyCode.Escape && screen == ScreenMode.Challenge)
            {
                screen = ScreenMode.Title;
                input.Use();
                return;
            }
            if (input.keyCode == KeyCode.Escape &&
                (screen == ScreenMode.DeckPurge || screen == ScreenMode.FinalTrim ||
                 screen == ScreenMode.ShopPurge || screen == ScreenMode.EventPurge))
            {
                CancelDeckPurge();
                input.Use();
                return;
            }
            if (input.keyCode == KeyCode.Escape && screen == ScreenMode.WorkshopCardSelect)
            {
                CancelWorkshop();
                input.Use();
                return;
            }
            if (input.keyCode == KeyCode.Escape && screen == ScreenMode.WorkshopBranch)
            {
                BackToWorkshopCards();
                input.Use();
                return;
            }
            if (input.keyCode == KeyCode.Escape && !showFirstBattleGuide && screen != ScreenMode.Title)
            {
                SetPaused(!paused);
                input.Use();
                return;
            }
            if (paused || showFirstBattleGuide)
                return;

            bool handled = false;
            bool confirm = input.keyCode == KeyCode.Return || input.keyCode == KeyCode.KeypadEnter;
            int number = input.keyCode >= KeyCode.Alpha1 && input.keyCode <= KeyCode.Alpha9
                ? (int)input.keyCode - (int)KeyCode.Alpha1
                : input.keyCode >= KeyCode.Keypad1 && input.keyCode <= KeyCode.Keypad9
                    ? (int)input.keyCode - (int)KeyCode.Keypad1
                    : -1;
            int purgeNavigationStep =
                input.keyCode == KeyCode.LeftArrow || input.keyCode == KeyCode.A ? -1 :
                input.keyCode == KeyCode.RightArrow || input.keyCode == KeyCode.D ? 1 :
                input.keyCode == KeyCode.UpArrow || input.keyCode == KeyCode.W ? -5 :
                input.keyCode == KeyCode.DownArrow || input.keyCode == KeyCode.S ? 5 : 0;

#if UNITY_EDITOR
            if (input.keyCode == KeyCode.F2)
            {
                paused = false;
                OpenRulebook();
                input.Use();
                return;
            }

            if (input.keyCode == KeyCode.F3)
            {
                paused = false;
                settingsReturnToPause = false;
                controllerSelection = 0;
                settingsOpen = true;
                input.Use();
                return;
            }

            if (input.keyCode == KeyCode.F4)
            {
                paused = false;
                settingsOpen = false;
                activeTutorialTopic = TutorialTopic.Tracking;
                firstBattleGuidePage = (int)TutorialTopic.Tracking;
                tutorialRulebookMode = false;
                showFirstBattleGuide = true;
                Time.timeScale = 0f;
                input.Use();
                return;
            }

            if (input.keyCode == KeyCode.F5)
            {
                InitializeRun(CargoContract.StormCore);
                completedRouteNodes.Add(0);
                completedRouteNodes.Add(1);
                completedRouteNodes.Add(3);
                lastCompletedRouteNodeId = 3;
                routeIndex = 3;
                selectedRouteNodeId = 6;
                screen = ScreenMode.Rest;
                input.Use();
                return;
            }

            if (input.keyCode == KeyCode.F6 && screen == ScreenMode.Battle)
            {
                PreviewNextAttackEffect();
                input.Use();
                return;
            }

            if (input.keyCode == KeyCode.F7)
            {
                InitializeRun(CargoContract.StormCore);
                AdvanceAfterCurrentRouteNode();
                input.Use();
                return;
            }

            if (input.keyCode == KeyCode.F8 || input.keyCode == KeyCode.F9)
            {
                bool rarePreview = input.keyCode == KeyCode.F9;
                InitializeRun(CargoContract.StormCore);
                StartBattle(rarePreview ? EncounterId.Elite : EncounterId.Skirmish);
                lastRewardCredits = rarePreview ? 56 : 38;
                rewardEnteredAt = Time.time;
                rewardSelectionLocked = false;
                selectedRewardIndex = -1;
                selectedRewardName = null;
                screen = ScreenMode.Reward;
                input.Use();
                return;
            }

            if (input.keyCode == KeyCode.F10)
            {
                InitializeRun(CargoContract.StormCore);
                runModules.Add(ModuleId.VectorThruster);
                runUpgrades.Add(CardId.BankDown);
                StartBattle(EncounterId.Skirmish);
                input.Use();
                return;
            }

            if (input.keyCode == KeyCode.F11)
            {
                InitializeRun(CargoContract.SignalSeed);
                departureDirective = DepartureDirective.StandardManifest;
                credits += 10;
                runModification = AirframeModification.OpenAvionics;
                completedRouteNodes.UnionWith(new[] { 0, 2, 5, 8, 11, 14 });
                lastCompletedRouteNodeId = 14;
                routeIndex = RunStructureCatalog.FinalApproachColumn;
                selectedRouteNodeId = 16;
                FocusRouteColumn(routeIndex);
                screen = ScreenMode.FinalApproach;
                input.Use();
                return;
            }

            if (input.keyCode == KeyCode.F12)
            {
                selectedContract = CargoContract.SignalSeed;
                currentChallenge = ChallengeId.Standard;
                runSeed = 0x050050;
                runAttemptId = "editor-debrief-preview";
                activeEncounterSeed = 50050;
                runDeck.Clear();
                runDeck.AddRange(CardPoolCatalog.CreateStarterDeck(selectedContract));
                runDeck.Remove(CardId.BankUp);
                runDeck.AddRange(new[] { CardId.ReserveShot, CardId.TightSchedule, CardId.StandbyField });
                runUpgrades.Clear();
                runUpgrades.UnionWith(new[] { CardId.ReserveShot, CardId.StandbyField });
                runUpgradeBranches.Clear();
                runUpgradeBranches[CardId.ReserveShot] = UpgradeBranch.Beta;
                runModules.Clear();
                runModules.Add(ModuleId.PrecisionMatrix);
                runBuildSnapshots.Clear();
                runModification = AirframeModification.OpenAvionics;
                routeStoryState = RouteStoryState.SilenceMaintained;
                departureDirective = DepartureDirective.AdvancePayment;
                finalApproachPlan = FinalApproachPlan.CargoOverclock;
                routeIndex = 1;
                selectedRouteNodeId = 2;
                credits = 72;
                runHull = 34;
                runCargoIntegrity = 2;
                CaptureBuildSnapshot(RunBuildSnapshotMoment.Departure, "editor_departure");
                routeIndex = 6;
                selectedRouteNodeId = 16;
                credits = 118;
                runHull = 4;
                runCargoIntegrity = 1;
                runTurns = 27;
                runCardsPlayed = 63;
                runDamageTaken = 39;
                runOverheats = 2;
                runCalamityInterrupts = 1;
                runCalamityEvades = 1;
                runCalamityHits = 2;
                runTrackingHits = 3;
                runContractProcs = 6;
                runContractBonus = 24;
                battle.StartEncounter(EncounterId.Skirmish,
                    Enumerable.Repeat(CardId.HeatCharge, 8).ToArray(), 4, 1, selectedContract,
                    null, runModules, 0, null, activeEncounterSeed, runModification, routeStoryState);
                battle.PlayCard(0);
                battle.PlayCard(0);
                battle.PlayCard(0);
                CaptureBuildSnapshot(RunBuildSnapshotMoment.RunResult, "editor_result_lost",
                    battle.PlayerHealth, battle.CargoIntegrity);
                screen = ScreenMode.Battle;
                controllerSelection = 0;
                archiveFailureRecorded = true;
                showFirstBattleGuide = false;
                paused = false;
                Time.timeScale = 1f;
                input.Use();
                return;
            }
#endif

            switch (screen)
            {
                case ScreenMode.Title when confirm:
                    if (RunSaveService.HasSave)
                        TryContinueRun();
                    else
                        StartNewRun();
                    handled = true;
                    break;
                case ScreenMode.Archive when confirm:
                    screen = ScreenMode.Title;
                    handled = true;
                    break;
                case ScreenMode.Challenge when number >= 0 && number < ChallengeCatalog.All.Count:
                    SelectChallenge(number);
                    handled = true;
                    break;
                case ScreenMode.Challenge when confirm:
                    SelectChallenge(controllerSelection);
                    handled = true;
                    break;
                case ScreenMode.Contract when number >= 0 && number < ContractCatalog.All.Count:
                    SetContractPreview(number);
                    handled = true;
                    break;
                case ScreenMode.Contract when input.keyCode == KeyCode.LeftArrow || input.keyCode == KeyCode.A:
                    CycleContractPreview(-1);
                    handled = true;
                    break;
                case ScreenMode.Contract when input.keyCode == KeyCode.RightArrow || input.keyCode == KeyCode.D:
                    CycleContractPreview(1);
                    handled = true;
                    break;
                case ScreenMode.Contract when confirm:
                    InitializeRun(ContractCatalog.All[Mathf.Clamp(controllerSelection, 0,
                        ContractCatalog.All.Count - 1)]);
                    handled = true;
                    break;
                case ScreenMode.DepartureBriefing when number >= 0 && number < 3:
                    controllerSelection = number;
                    handled = true;
                    break;
                case ScreenMode.DepartureBriefing when confirm:
                    ApplyDepartureDirective((DepartureDirective)(Mathf.Clamp(controllerSelection, 0, 2) + 2));
                    handled = true;
                    break;
                case ScreenMode.Map when confirm:
                    EnterCurrentNode();
                    handled = true;
                    break;
                case ScreenMode.Retrofit when number >= 0 && number < 3:
                    controllerSelection = number;
                    handled = true;
                    break;
                case ScreenMode.Retrofit when confirm:
                    InstallAirframeModification((AirframeModification)(Mathf.Clamp(controllerSelection, 0, 2) + 1));
                    handled = true;
                    break;
                case ScreenMode.FinalApproach when number >= 0 && number < 4:
                    controllerSelection = number;
                    handled = true;
                    break;
                case ScreenMode.FinalApproach when confirm:
                    SelectFinalApproachOption(Mathf.Clamp(controllerSelection, 0, 3));
                    handled = true;
                    break;
                case ScreenMode.DeckPurge when purgeNavigationStep != 0:
                case ScreenMode.FinalTrim when purgeNavigationStep != 0:
                case ScreenMode.ShopPurge when purgeNavigationStep != 0:
                case ScreenMode.EventPurge when purgeNavigationStep != 0:
                {
                    int candidateCount = PurgeCandidates().Length;
                    controllerSelection = WrapSelection(controllerSelection + purgeNavigationStep,
                        candidateCount + 1);
                    if (controllerSelection < candidateCount)
                        deckPurgePage = controllerSelection / 10;
                    handled = true;
                    break;
                }
                case ScreenMode.DeckPurge when confirm:
                case ScreenMode.FinalTrim when confirm:
                case ScreenMode.ShopPurge when confirm:
                case ScreenMode.EventPurge when confirm:
                {
                    CardId[] candidates = PurgeCandidates();
                    if (controllerSelection < candidates.Length)
                        RemoveCardFromDeck(candidates[controllerSelection]);
                    else
                        CancelDeckPurge();
                    handled = true;
                    break;
                }
                case ScreenMode.WorkshopCardSelect when purgeNavigationStep != 0:
                {
                    int candidateCount = WorkshopCandidates().Length;
                    controllerSelection = WrapSelection(controllerSelection + purgeNavigationStep,
                        candidateCount + 1);
                    if (controllerSelection < candidateCount)
                        workshopPage = controllerSelection / 10;
                    handled = true;
                    break;
                }
                case ScreenMode.WorkshopCardSelect when confirm:
                {
                    CardId[] candidates = WorkshopCandidates();
                    if (controllerSelection < candidates.Length)
                        SelectWorkshopCard(candidates[controllerSelection]);
                    else
                        CancelWorkshop();
                    handled = true;
                    break;
                }
                case ScreenMode.WorkshopBranch when number >= 0 && number < 2:
                    controllerSelection = number;
                    handled = true;
                    break;
                case ScreenMode.WorkshopBranch when
                    input.keyCode == KeyCode.LeftArrow || input.keyCode == KeyCode.A ||
                    input.keyCode == KeyCode.RightArrow || input.keyCode == KeyCode.D:
                    controllerSelection = WrapSelection(controllerSelection +
                        (input.keyCode == KeyCode.LeftArrow || input.keyCode == KeyCode.A ? -1 : 1), 3);
                    handled = true;
                    break;
                case ScreenMode.WorkshopBranch when confirm:
                    if (controllerSelection < 2)
                        ApplyWorkshopBranch(controllerSelection == 0 ? UpgradeBranch.Alpha : UpgradeBranch.Beta);
                    else
                        BackToWorkshopCards();
                    handled = true;
                    break;
                case ScreenMode.Event when number >= 0 && number < 3:
                    controllerSelection = number;
                    handled = true;
                    break;
                case ScreenMode.Event when confirm:
                    if (eventResolved)
                        LeaveRouteEvent();
                    else if (controllerSelection == 0)
                        ResolveRouteEvent(true);
                    else if (controllerSelection == 1)
                        ResolveRouteEvent(false);
                    else
                        OpenEventPurge();
                    handled = true;
                    break;
                case ScreenMode.Shop when number >= 0 && number < 7:
                    controllerSelection = number;
                    handled = true;
                    break;
                case ScreenMode.Shop when confirm:
                    ActivateControllerSelection();
                    handled = true;
                    break;
                case ScreenMode.Battle when battle.Victory && confirm && !DeathAnimationActive():
                    ContinueAfterVictory();
                    handled = true;
                    break;
                case ScreenMode.Battle when battle.Defeat && confirm:
                    ActivateControllerSelection();
                    handled = true;
                    break;
                case ScreenMode.Battle when !battle.Victory && !battle.Defeat:
                    if (number >= 0 && number < battle.Hand.Count && CanPlayInteractive(number))
                    {
                        PlayCardWithFeedback(number);
                        handled = true;
                    }
                    else if (input.keyCode == KeyCode.Space)
                    {
                        EndTurnWithFeedback();
                        handled = true;
                    }
                    break;
                case ScreenMode.Reward when !rewardSelectionLocked:
                    if (number >= 0 && number < 3)
                    {
                        ChooseRewardChoice(CurrentRewardChoices()[number], number);
                        handled = true;
                    }
                    else if (input.keyCode == KeyCode.X)
                    {
                        SkipReward();
                        handled = true;
                    }
                    break;
                case ScreenMode.Complete when confirm:
                    StartNewRun();
                    handled = true;
                    break;
            }

            if (handled)
                input.Use();
        }

        private void RegisterHover(string key, string _)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            hoverKeyThisFrame = key;
        }

        private void DrawContractScreen()
        {
            DrawRect(new Rect(70, 48, 1460, 805), new Color32(2, 7, 22, 250));
            DrawRect(new Rect(78, 56, 1444, 789), PanelNight);
            DrawNeonFrame(new Rect(78, 56, 1444, 789), NeonCyan, 3f);
            DrawFittedLabel(new Rect(125, 82, 940, 62),
                L("contract_select.title", "选择配送合同"), neonTitleStyle, 26);
            DrawFittedLabel(new Rect(1080, 92, 350, 35),
                L("contract_select.hangar", "HANGAR // CARGO ASSIGNMENT"), hudCenteredStyle, 8);
            DrawFittedLabel(new Rect(130, 142, 1340, 38),
                L("contract_select.subtitle", "每份货物有3格完整度；触发合同风险会失去1格，并降低最终评级。"),
                neonBodyStyle, 12);

            DrawContractHangarDepth();
            if (Event.current.type == EventType.Repaint)
            {
                contractCarouselVisual = Mathf.SmoothDamp(contractCarouselVisual, contractCarouselTarget,
                    ref contractCarouselVelocity, 0.12f, 16f, Mathf.Max(0.001f, Time.unscaledDeltaTime));
            }

            int contractCount = ContractCatalog.All.Count;
            int[] drawOrder = Enumerable.Range(0, contractCount)
                .OrderByDescending(index => Mathf.Abs(ContractCarouselDistance(index))).ToArray();
            foreach (int index in drawOrder)
            {
                float distance = ContractCarouselDistance(index);
                float depth = Mathf.Clamp01(Mathf.Abs(distance) / 1.65f);
                float scale = Mathf.Lerp(1f, 0.46f, depth);
                // The shortest signed distance keeps the carousel stable as contracts are added.
                // Keep that deepest card inside the hangar instead of pushing it past the viewport edge.
                float horizontalDistance = Mathf.Clamp(distance, -1.35f, 1.35f);
                float centerX = 800f + horizontalDistance * 405f;
                float centerY = 458f + depth * 74f;
                Rect cardRect = new Rect(centerX - 285f * scale, centerY - 252f * scale,
                    570f * scale, 504f * scale);
                bool focused = index == controllerSelection && Mathf.Abs(distance) < 0.42f;
                DrawContractCarouselCard(ContractCatalog.All[index], cardRect, scale, depth, focused);
            }

            DrawFittedLabel(new Rect(430, 802, 740, 28),
                L("contract_select.controls", "点击两侧货舱切换　滚轮 / 方向键浏览　1—5 快速定位"),
                hudCenteredStyle, 8);

            if (Event.current.type == EventType.ScrollWheel)
            {
                CycleContractPreview(Event.current.delta.y > 0f ? 1 : -1);
                Event.current.Use();
            }
        }

        private void DrawContractHangarDepth()
        {
            DrawRect(new Rect(110, 188, 1380, 555), new Color32(2, 8, 24, 210));
            for (int i = 0; i < 6; i++)
            {
                float inset = i * 76f;
                byte alpha = (byte)(75 - i * 8);
                DrawPixelOutline(new Rect(125 + inset, 198 + i * 18, 1350 - inset * 2f, 510 - i * 24f),
                    new Color32(76, 204, 235, alpha), 2f);
            }
            DrawRect(new Rect(170, 694, 1260, 7), new Color32(76, 204, 235, 70));
            DrawRect(new Rect(285, 717, 1030, 6), new Color32(199, 83, 255, 58));
            DrawRect(new Rect(450, 739, 700, 5), new Color32(255, 211, 82, 55));
            for (int i = 0; i < 7; i++)
            {
                float width = 1240f - i * 142f;
                DrawRect(new Rect(800f - width * 0.5f, 670f + i * 12f, width, 2f),
                    new Color32(70, 123, 164, (byte)(48 - i * 4)));
            }
            DrawRect(new Rect(770, 188, 60, 545), new Color32(102, 220, 255, 13));
            DrawRect(new Rect(793, 188, 14, 545), new Color32(255, 255, 255, 17));
        }

        private void DrawContractCarouselCard(CargoContract contract, Rect rect, float scale, float depth, bool focused)
        {
            Color color = CargoColor(contract);
            bool hovered = rect.Contains(Event.current.mousePosition);
            float opacity = Mathf.Lerp(1f, 0.42f, depth);
            Color previousGuiColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, opacity);
            if (hovered)
                RegisterHover($"contract-preview-{contract}", focused
                    ? $"开始配送 {CargoName(contract)}"
                    : $"查看 {CargoName(contract)}");
            DrawRect(new Rect(rect.x + 12f * scale, rect.y + 18f * scale, rect.width, rect.height),
                new Color32(1, 4, 16, 245));
            DrawRect(rect, new Color32(8, 20, 45, 250));
            DrawPixelOutline(rect, focused ? Color.Lerp(color, Color.white, 0.22f) : color,
                focused ? 4f : 2f);
            DrawRect(new Rect(rect.x, rect.y, rect.width, 70f * scale), new Color32(10, 30, 62, 255));
            DrawRect(new Rect(rect.x, rect.y, 8f * scale, rect.height), color);
            if (focused)
                DrawNeonFrame(rect, color, 3f);

            DrawFittedLabel(new Rect(rect.x + 22f * scale, rect.y + 13f * scale,
                    rect.width - 44f * scale, 45f * scale),
                CargoName(contract), focused ? neonSubtitleStyle : hudCenteredStyle, focused ? 14 : 9);
            DrawCargoIcon(new Vector2(rect.center.x, rect.y + 137f * scale), contract, color,
                Mathf.Lerp(0.68f, 1.18f, scale));

            if (focused)
            {
                ContractMasteryRecord mastery = archiveData?.ContractMastery.FirstOrDefault(record =>
                    record.Contract == (int)contract);
                DrawFittedLabel(new Rect(rect.x + 40, rect.y + 195, rect.width - 80, 26),
                    L("contract_select.integrity_mastery", "货物完整度 {0}　//　精通 {1}",
                        "■■■　3/3",
                        LongTermProgressionRules.MasteryLevel(mastery)), hudCenteredStyle, 8);
                Rect riskRect = new Rect(rect.x + 38, rect.y + 228, rect.width - 76, 88);
                DrawRect(riskRect, new Color32(3, 11, 31, 238));
                DrawRect(new Rect(riskRect.x, riskRect.y, 7, riskRect.height), color);
                DrawFittedLabel(new Rect(riskRect.x + 18, riskRect.y + 8, riskRect.width - 28, 22),
                    L("contract_select.fail", "合同风险"), tinyStyle, 8);
                DrawFittedLabel(new Rect(riskRect.x + 18, riskRect.y + 31, riskRect.width - 30, 49),
                    CargoRule(contract), neonBodyStyle, 11);
                DrawFittedLabel(new Rect(rect.x + 42, rect.y + 326, rect.width - 84, 24),
                    L("contract_select.passive", "专属被动 // {0}", ContractPassiveName(contract)),
                    hudCenteredStyle, 8);
                DrawFittedLabel(new Rect(rect.x + 42, rect.y + 351, rect.width - 84, 40),
                    ContractPassiveDescription(contract), neonBodyStyle, 10);
                DrawFittedLabel(new Rect(rect.x + 42, rect.y + 393, rect.width - 84, 22),
                    L("contract_select.build", "推荐构筑 // {0}", ContractBuildLabel(contract)),
                    tinyStyle, 8);
                DrawFittedLabel(new Rect(rect.x + 42, rect.y + 417, rect.width - 84, 20),
                    ContractRewardLabel(contract), hudCenteredStyle, 8);
                DrawPixelButton(new Rect(rect.x + 120, rect.y + 442, rect.width - 240, 48),
                    L("contract_select.sign", "签署合同"), color, () => InitializeRun(contract), true, "ENTER");
            }
            else
            {
                DrawFittedLabel(new Rect(rect.x + 18f * scale, rect.yMax - 62f * scale,
                        rect.width - 36f * scale, 32f * scale),
                    ContractRewardLabel(contract), hudCenteredStyle, 7);
                bool oldEnabled = GUI.enabled;
                GUI.enabled = true;
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                    SetContractPreview((int)contract);
                GUI.enabled = oldEnabled;
            }
            GUI.color = previousGuiColor;
        }

        private float ContractCarouselDistance(int index)
        {
            int count = ContractCatalog.All.Count;
            int selected = WrapSelection(controllerSelection, count);
            int raw = (index - selected + count) % count;
            if (raw > count / 2)
                raw -= count;
            float motion = contractCarouselVisual - contractCarouselTarget;
            return raw - motion;
        }

        private void CycleContractPreview(int direction)
        {
            if (direction == 0)
                return;
            int step = direction > 0 ? 1 : -1;
            controllerSelection = WrapSelection(controllerSelection + step, ContractCatalog.All.Count);
            selectedContract = ContractCatalog.All[controllerSelection];
            contractCarouselTarget += step;
            PlaySound(clickSound, step > 0 ? 1.08f : 0.94f, 0.32f);
        }

        private void SetContractPreview(int index)
        {
            int count = ContractCatalog.All.Count;
            int next = WrapSelection(index, count);
            int current = WrapSelection(controllerSelection, count);
            int delta = next - current;
            if (delta > count / 2)
                delta -= count;
            else if (delta < -count / 2)
                delta += count;
            if (delta == 0)
                return;
            controllerSelection = next;
            selectedContract = ContractCatalog.All[next];
            contractCarouselTarget += delta;
            PlaySound(clickSound, delta > 0 ? 1.08f : 0.94f, 0.32f);
        }

        private static string ContractBuildLabel(CargoContract contract)
        {
            return contract switch
            {
                CargoContract.CryoSerum => L("contract_select.build.CryoSerum", "零度循环 / 熔炉爆发"),
                CargoContract.StormCore => L("contract_select.build.StormCore", "矢量追猎 / 蜂群弹幕"),
                CargoContract.BlackBoxRelay => L("contract_select.build.BlackBoxRelay", "航迹欺骗 / 逆向追猎"),
                CargoContract.SignalSeed => L("contract_select.build.SignalSeed", "余量调度 / 指令循环"),
                _ => L("contract_select.build.FragileMedicine", "护盾冲角 / 锁定狙击")
            };
        }

        private static string ContractPassiveName(CargoContract contract)
        {
            return contract switch
            {
                CargoContract.CryoSerum => L("contract_select.passive_name.CryoSerum", "低温回收"),
                CargoContract.StormCore => L("contract_select.passive_name.StormCore", "矢量电荷"),
                CargoContract.BlackBoxRelay => L("contract_select.passive_name.BlackBoxRelay", "幽灵译码"),
                CargoContract.SignalSeed => L("contract_select.passive_name.SignalSeed", "余量回授"),
                _ => L("contract_select.passive_name.FragileMedicine", "密封缓冲")
            };
        }

        private static string ContractPassiveDescription(CargoContract contract)
        {
            return contract switch
            {
                CargoContract.CryoSerum => L("contract_select.passive_desc.CryoSerum",
                    "每回合首次由卡牌降低至少3点热量时，获得1点能量。"),
                CargoContract.StormCore => L("contract_select.passive_desc.StormCore",
                    "每回合打出的第一张机动牌额外获得1层动量。"),
                CargoContract.BlackBoxRelay => L("contract_select.passive_desc.BlackBoxRelay",
                    "每回合首次主动清除航迹暴露时，获得1层锁定。"),
                CargoContract.SignalSeed => L("contract_select.passive_desc.SignalSeed",
                    "每回合首次在出牌后恰好保留1点能量时，抽1张牌。"),
                _ => L("contract_select.passive_desc.FragileMedicine",
                    "每回合首次用护盾完全抵消敌方攻击时，获得1层锁定。")
            };
        }

        private static string ContractPassiveHud(CargoContract contract)
        {
            return contract switch
            {
                CargoContract.CryoSerum => L("contract_hud.passive.CryoSerum",
                    "低温回收 · 单牌降温3+ → 能量+1"),
                CargoContract.StormCore => L("contract_hud.passive.StormCore",
                    "矢量电荷 · 首次机动 → 额外动量+1"),
                CargoContract.BlackBoxRelay => L("contract_hud.passive.BlackBoxRelay",
                    "幽灵译码 · 主动清轨 → 锁定+1"),
                CargoContract.SignalSeed => L("contract_hud.passive.SignalSeed",
                    "余量回授 · 出牌后保留1能量 → 抽1"),
                _ => L("contract_hud.passive.FragileMedicine",
                    "密封缓冲 · 完全格挡 → 锁定+1")
            };
        }

        private static string ContractRewardLabel(CargoContract contract)
        {
            return contract switch
            {
                CargoContract.CryoSerum => L("contract_select.reward.CryoSerum", "报酬 +15%"),
                CargoContract.StormCore => L("contract_select.reward.StormCore", "报酬 +25%"),
                CargoContract.BlackBoxRelay => L("contract_select.reward.BlackBoxRelay", "报酬 +30%"),
                CargoContract.SignalSeed => L("contract_select.reward.SignalSeed", "报酬 +20%"),
                _ => L("contract_select.reward.FragileMedicine", "基础报酬")
            };
        }

        private void DrawCargoIcon(Vector2 center, CargoContract contract, Color color, float size = 1f)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 4.8f + (int)contract) * 0.06f;
            float s = pulse * size;
            DrawRect(new Rect(center.x - 64 * s, center.y - 44 * s, 128 * s, 88 * s), new Color32(3, 9, 28, 245));
            DrawPixelOutline(new Rect(center.x - 64 * s, center.y - 44 * s, 128 * s, 88 * s), color, 4f * size);
            if (contract == CargoContract.FragileMedicine)
            {
                DrawRect(new Rect(center.x - 18 * size, center.y - 31 * size, 36 * size, 62 * size), color);
                DrawRect(new Rect(center.x - 28 * size, center.y - 18 * size, 56 * size, 11 * size), Color.white);
                DrawRect(new Rect(center.x - 6 * size, center.y - 2 * size, 12 * size, 24 * size), PostalRed);
            }
            else if (contract == CargoContract.CryoSerum)
            {
                DrawRect(new Rect(center.x - 12 * size, center.y - 36 * size, 24 * size, 72 * size), color);
                DrawRect(new Rect(center.x - 36 * size, center.y - 12 * size, 72 * size, 24 * size), color);
                DrawRect(new Rect(center.x - 25 * size, center.y - 25 * size, 50 * size, 50 * size), new Color32(180, 246, 255, 90));
            }
            else if (contract == CargoContract.StormCore)
            {
                DrawRect(new Rect(center.x - 24 * size, center.y - 24 * size, 48 * size, 48 * size), color);
                DrawPixelOutline(new Rect(center.x - 42 * size, center.y - 42 * size, 84 * size, 84 * size),
                    NeonViolet, 5f * size);
                DrawRect(new Rect(center.x - 6 * size, center.y - 54 * size, 12 * size, 108 * size), Color.white);
                DrawRect(new Rect(center.x - 54 * size, center.y - 6 * size, 108 * size, 12 * size), color);
            }
            else if (contract == CargoContract.BlackBoxRelay)
            {
                DrawRect(new Rect(center.x - 38 * size, center.y - 28 * size, 76 * size, 56 * size), new Color32(3, 9, 28, 255));
                DrawPixelOutline(new Rect(center.x - 38 * size, center.y - 28 * size, 76 * size, 56 * size),
                    color, 4f * size);
                DrawRect(new Rect(center.x - 25 * size, center.y - 15 * size, 50 * size, 8 * size), color);
                DrawRect(new Rect(center.x - 25 * size, center.y + 2 * size, 34 * size, 8 * size), Color.white);
                DrawRect(new Rect(center.x + 18 * size, center.y + 2 * size, 7 * size, 8 * size), PostalRed);
            }
            else
            {
                DrawPixelOutline(new Rect(center.x - 34 * size, center.y - 34 * size, 68 * size, 68 * size),
                    color, 4f * size);
                DrawRect(new Rect(center.x - 10 * size, center.y - 42 * size, 20 * size, 84 * size), color);
                DrawRect(new Rect(center.x - 42 * size, center.y - 10 * size, 84 * size, 20 * size), color);
                DrawRect(new Rect(center.x - 17 * size, center.y - 17 * size, 34 * size, 34 * size),
                    new Color32(3, 9, 28, 255));
                DrawRect(new Rect(center.x - 5 * size, center.y - 5 * size, 10 * size, 10 * size), Color.white);
            }
        }

        private static string CargoName(CargoContract contract)
        {
            return contract switch
            {
                CargoContract.CryoSerum => L("contract.CryoSerum", "零度血清"),
                CargoContract.StormCore => L("contract.StormCore", "风暴核心"),
                CargoContract.BlackBoxRelay => L("contract.BlackBoxRelay", "幽灵黑匣"),
                CargoContract.SignalSeed => L("contract.SignalSeed", "信标种子"),
                _ => L("contract.FragileMedicine", "易碎药剂")
            };
        }

        private static string CargoRule(CargoContract contract)
        {
            return contract switch
            {
                CargoContract.CryoSerum => L("contract_select.rule.CryoSerum",
                    "回合结束时热量达到6点，则失去1格完整度。"),
                CargoContract.StormCore => L("contract_select.rule.StormCore",
                    "连续两回合没有切换航道，则失去1格完整度。"),
                CargoContract.BlackBoxRelay => L("contract_select.rule.BlackBoxRelay",
                    "回合结束时航迹暴露达到2层，则失去1格完整度。"),
                CargoContract.SignalSeed => L("contract_select.rule.SignalSeed",
                    "回合结束时没有保留能量，则失去1格完整度。"),
                _ => L("contract_select.rule.FragileMedicine",
                    "单次受到6点以上未抵消伤害，则失去1格完整度。")
            };
        }

        private static string CargoActionHint(CargoContract contract)
        {
            return contract switch
            {
                CargoContract.CryoSerum => "安全操作：结束回合前将热量降到5点以下",
                CargoContract.StormCore => "安全操作：至少每2回合切换1次航道",
                CargoContract.BlackBoxRelay => "安全操作：用扰频或停留将航迹暴露控制在1层",
                CargoContract.SignalSeed => "安全操作：结束回合前保留至少1点能量",
                _ => "安全操作：用护盾将单次未抵消伤害压到5点以下"
            };
        }

        private static string CargoStatus(int integrity)
        {
            return integrity switch
            {
                3 => L("cargo.pristine", "完好"),
                2 => L("cargo.light", "轻微受损"),
                1 => L("cargo.heavy", "严重受损"),
                _ => L("cargo.destroyed", "货物损毁")
            };
        }

        private static string CargoGrade(int integrity)
        {
            return integrity switch
            {
                3 => "S",
                2 => "A",
                1 => "B",
                _ => "C"
            };
        }

        private static string CargoPips(int integrity)
        {
            int value = Mathf.Clamp(integrity, 0, 3);
            return new string('■', value) + new string('□', 3 - value);
        }

        private static string CargoStatusLine(int integrity)
        {
            return $"{CargoPips(integrity)}  {integrity}/3 · {CargoStatus(integrity)}";
        }

        private static float CargoRewardMultiplier(CargoContract contract)
        {
            return contract switch
            {
                CargoContract.CryoSerum => 1.15f,
                CargoContract.StormCore => 1.25f,
                CargoContract.BlackBoxRelay => 1.3f,
                CargoContract.SignalSeed => 1.2f,
                _ => 1f
            };
        }

        private static Color CargoColor(CargoContract contract)
        {
            return contract switch
            {
                CargoContract.CryoSerum => new Color32(78, 215, 255, 255),
                CargoContract.StormCore => new Color32(199, 83, 255, 255),
                CargoContract.BlackBoxRelay => new Color32(255, 92, 154, 255),
                CargoContract.SignalSeed => new Color32(255, 197, 74, 255),
                _ => new Color32(67, 188, 153, 255)
            };
        }

        private void StartNewRun()
        {
            SetPaused(false);
            currentChallenge = ChallengeId.Standard;
            controllerSelection = 0;
            archiveData ??= new DeliveryArchiveData();
            DeliveryArchiveService.Normalize(archiveData);
            if (FirstRunGuidanceRules.ChallengesAvailable(archiveData))
            {
                screen = ScreenMode.Challenge;
            }
            else
            {
                BeginContractSelection(ChallengeId.Standard);
                saveStatusMessage = L("tutorial.first_run_standard",
                    "首局使用标准派遣；挑战任务将在首次结算后开放。");
                saveStatusUntil = Time.unscaledTime + 5f;
            }
        }

        private void BeginContractSelection(ChallengeId challenge)
        {
            currentChallenge = challenge;
            controllerSelection = ContractIndex(selectedContract);
            contractCarouselTarget = controllerSelection;
            contractCarouselVisual = contractCarouselTarget;
            contractCarouselVelocity = 0f;
            screen = ScreenMode.Contract;
        }

        private void RestartSameContract()
        {
            EnsureFailureArchived();
            CargoContract contract = selectedContract;
            SetPaused(false);
            InitializeRun(contract);
            saveStatusMessage = $"同合同新种子：{runSeed:X8}";
            saveStatusUntil = Time.unscaledTime + 4f;
        }

        private void RestartSameSeed()
        {
            EnsureFailureArchived();
            CargoContract contract = selectedContract;
            int seed = runSeed;
            SetPaused(false);
            InitializeRun(contract, seed);
            saveStatusMessage = $"同种子复飞：{runSeed:X8}";
            saveStatusUntil = Time.unscaledTime + 4f;
        }

        private void ChangeContractAfterFailure()
        {
            EnsureFailureArchived();
            BeginContractSelection(currentChallenge);
        }

        private void ReturnToTitleAfterFailure()
        {
            EnsureFailureArchived();
            screen = ScreenMode.Title;
        }

        private void EnsureFailureArchived()
        {
            if (screen == ScreenMode.Battle && battle.Defeat && !archiveFailureRecorded)
                RegisterArchiveFailure();
        }

        private void TryContinueRun()
        {
            if (!RunSaveService.TryLoad(out RunSaveData data, out bool restoredBackup, out string error))
            {
                RecordRunDiagnostic("save_load_failed", error);
                saveStatusMessage = $"无法读取存档\n{error}";
                saveStatusUntil = Time.unscaledTime + 6f;
                return;
            }

            try
            {
                RestoreRun(data);
                saveStatusMessage = restoredBackup ? "主存档异常，已恢复备份" : null;
                saveStatusUntil = Time.unscaledTime + 5f;
                PlaySound(rewardSound, 0.92f, 0.55f);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                saveStatusMessage = $"存档内容无效\n{exception.Message}";
                saveStatusUntil = Time.unscaledTime + 6f;
                screen = ScreenMode.Title;
            }
        }

        private void RestoreRun(RunSaveData data)
        {
            if (!Enum.IsDefined(typeof(CargoContract), data.Contract))
                throw new InvalidDataException("合同数据无效");
            if (!Enum.IsDefined(typeof(AirframeModification), data.AirframeModification))
                throw new InvalidDataException("机体改装数据无效");
            if (!Enum.IsDefined(typeof(RouteStoryState), data.RouteStoryState))
                throw new InvalidDataException("航线纪事数据无效");
            if (!Enum.IsDefined(typeof(RouteIntel), data.RouteIntel))
                throw new InvalidDataException("终局情报数据无效");
            if (!Enum.IsDefined(typeof(ChallengeId), data.Challenge))
                throw new InvalidDataException("挑战任务数据无效");
            if (!Enum.IsDefined(typeof(DepartureDirective), data.DepartureDirective))
                throw new InvalidDataException("派遣条款数据无效");
            if (!Enum.IsDefined(typeof(FinalApproachPlan), data.FinalApproachPlan))
                throw new InvalidDataException("终局进场方案数据无效");
            if (data.WorkshopCard >= 0 && !Enum.IsDefined(typeof(CardId), data.WorkshopCard))
                throw new InvalidDataException("工坊卡牌数据无效");
            if (!route.Nodes.Any(node => node.Id == data.SelectedRouteNodeId))
                throw new InvalidDataException("当前航点无效");
            if (data.LastCompletedRouteNodeId >= 0 &&
                !route.Nodes.Any(node => node.Id == data.LastCompletedRouteNodeId))
                throw new InvalidDataException("已完成航点无效");

            selectedContract = (CargoContract)data.Contract;
            runAttemptId = string.IsNullOrWhiteSpace(data.AttemptId)
                ? Guid.NewGuid().ToString("N")
                : data.AttemptId;
            runModification = (AirframeModification)data.AirframeModification;
            routeStoryState = (RouteStoryState)data.RouteStoryState;
            routeIntel = (RouteIntel)data.RouteIntel;
            currentChallenge = (ChallengeId)data.Challenge;
            departureDirective = (DepartureDirective)data.DepartureDirective;
            finalApproachPlan = (FinalApproachPlan)data.FinalApproachPlan;
            workshopCardValue = data.WorkshopCard;
            finaleEnding = FinaleEnding.None;
            runSeed = data.RunSeed == 0 ? RunSeedUtility.LegacySeed : data.RunSeed;
            activeEncounterSeed = data.EncounterSeed == 0 ? RunSeedUtility.LegacySeed : data.EncounterSeed;
            runDeck.Clear();
            runDeck.AddRange(data.Deck.Select(CardFromSave));
            runUpgrades.Clear();
            foreach (int value in data.Upgrades)
                runUpgrades.Add(CardFromSave(value));
            runUpgradeBranches.Clear();
            int branchCount = Math.Min(data.UpgradeBranchCards?.Count ?? 0, data.UpgradeBranches?.Count ?? 0);
            for (int i = 0; i < branchCount; i++)
            {
                CardId card = CardFromSave(data.UpgradeBranchCards[i]);
                int branchValue = data.UpgradeBranches[i];
                if (!Enum.IsDefined(typeof(UpgradeBranch), branchValue))
                    throw new InvalidDataException("强化分支数据无效");
                runUpgradeBranches[card] = (UpgradeBranch)branchValue;
            }
            runModules.Clear();
            foreach (int value in data.Modules)
            {
                if (!Enum.IsDefined(typeof(ModuleId), value))
                    throw new InvalidDataException("模块数据无效");
                runModules.Add((ModuleId)value);
            }
            runBuildSnapshots.Clear();
            runBuildSnapshots.AddRange(RunBuildSnapshotRules.Clone(data.BuildSnapshots));
            completedRouteNodes.Clear();
            foreach (int nodeId in data.CompletedRouteNodes)
            {
                if (!route.Nodes.Any(node => node.Id == nodeId))
                    throw new InvalidDataException("航线进度数据无效");
                completedRouteNodes.Add(nodeId);
            }

            routeIndex = Mathf.Clamp(data.RouteIndex, 0, route.ColumnCount - 1);
            selectedRouteNodeId = data.SelectedRouteNodeId;
            lastCompletedRouteNodeId = data.LastCompletedRouteNodeId;
            routeScroll = Mathf.Max(0f, data.RouteScroll);
            eventResolved = data.EventResolved;
            eventResult = data.EventResult;
            restResolved = data.RestResolved;
            restResult = data.RestResult;
            credits = Mathf.Max(0, data.Credits);
            runHull = Mathf.Clamp(data.Hull, 1, BattleState.MaxPlayerHealth);
            runCargoIntegrity = Mathf.Clamp(data.CargoIntegrity, 0, 3);
            runContractBonus = Mathf.Max(0, data.ContractBonus);
            runContractProcs = Mathf.Max(0, data.ContractProcs);
            repairBought = data.RepairBought;
            shopPurgeBought = data.ShopPurgeBought;
            shopCalibrationBought = data.ShopCalibrationBought;
            for (int i = 0; i < shopBought.Length; i++)
                shopBought[i] = data.ShopBought != null && i < data.ShopBought.Length && data.ShopBought[i];
            runTurns = Mathf.Max(0, data.Turns);
            runCardsPlayed = Mathf.Max(0, data.CardsPlayed);
            runDamageTaken = Mathf.Max(0, data.DamageTaken);
            runOverheats = Mathf.Max(0, data.Overheats);
            runCalamityInterrupts = Mathf.Max(0, data.CalamityInterrupts);
            runCalamityEvades = Mathf.Max(0, data.CalamityEvades);
            runCalamityHits = Mathf.Max(0, data.CalamityHits);
            runTrackingHits = Mathf.Max(0, data.TrackingHits);
            lastRewardCredits = Mathf.Max(0, data.LastRewardCredits);
            lastFieldRepair = Mathf.Max(0, data.LastFieldRepair);
            rewardSelectionLocked = false;
            selectedRewardIndex = -1;
            selectedRewardName = null;
            SetPaused(false);

            if (!Enum.TryParse(data.Screen, out ScreenMode restoredScreen) ||
                restoredScreen == ScreenMode.Title || restoredScreen == ScreenMode.Challenge ||
                restoredScreen == ScreenMode.Contract ||
                restoredScreen == ScreenMode.Complete)
                throw new InvalidDataException("恢复界面数据无效");

            if (restoredScreen == ScreenMode.Battle || restoredScreen == ScreenMode.Reward)
            {
                if (!Enum.IsDefined(typeof(EncounterId), data.Encounter))
                    throw new InvalidDataException("遭遇数据无效");
                EncounterId encounter = (EncounterId)data.Encounter;
                battle.StartEncounter(encounter, runDeck, runHull, runCargoIntegrity, selectedContract, runUpgrades,
                    runModules, EncounterVariantForRun(encounter), runUpgradeBranches, activeEncounterSeed,
                    runModification, routeStoryState, routeIntel, ChallengeCatalog.Get(currentChallenge).StartingHeat);
                archiveFailureRecorded = false;
                if (restoredScreen == ScreenMode.Battle)
                {
                    screen = ScreenMode.Battle;
                    bannerText = L("feedback.run_restored", "RUN RESTORED // 战斗从入口重新开始");
                    bannerUntil = Time.time + 2.1f;
                }
                else
                {
                    screen = ScreenMode.Reward;
                    rewardEnteredAt = Time.time;
                }
            }
            else
            {
                screen = restoredScreen;
                if (screen == ScreenMode.Map)
                    screen = ScreenAfterRouteTransition();
            }
            DeliveryArchiveService.RegisterRewardDiscoveries(archiveData,
                runDeck.Select(card => (int)card), runModules.Select(module => (int)module));
            if (screen == ScreenMode.Battle || screen == ScreenMode.Reward)
            {
                DeliveryArchiveService.RegisterBattleStarted(archiveData,
                    battle.Enemies.Select(enemy => (int)enemy.Kind),
                    runDeck.Select(card => (int)card), runModules.Select(module => (int)module));
            }
            SaveArchive();
            RecordRunDiagnostic("run_restored");
        }

        private static CardId CardFromSave(int value)
        {
            if (!Enum.IsDefined(typeof(CardId), value))
                throw new InvalidDataException($"卡牌数据无效：{value}");
            return (CardId)value;
        }

        private void SaveRunCheckpoint()
        {
            if (screen == ScreenMode.Title || screen == ScreenMode.Archive ||
                screen == ScreenMode.Challenge || screen == ScreenMode.Contract || screen == ScreenMode.Complete)
                return;

            try
            {
                var data = new RunSaveData
                {
                    AttemptId = runAttemptId,
                    RunSeed = runSeed,
                    EncounterSeed = activeEncounterSeed,
                    Screen = screen.ToString(),
                    Encounter = screen == ScreenMode.Battle || screen == ScreenMode.Reward ? (int)battle.Encounter : 0,
                    Contract = (int)selectedContract,
                    AirframeModification = (int)runModification,
                    RouteStoryState = (int)routeStoryState,
                    RouteIntel = (int)routeIntel,
                    Challenge = (int)currentChallenge,
                    DepartureDirective = (int)departureDirective,
                    FinalApproachPlan = (int)finalApproachPlan,
                    WorkshopCard = workshopCardValue,
                    Deck = runDeck.Select(card => (int)card).ToList(),
                    Upgrades = runUpgrades.Select(card => (int)card).ToList(),
                    Modules = runModules.Select(module => (int)module).ToList(),
                    BuildSnapshots = RunBuildSnapshotRules.Clone(runBuildSnapshots),
                    CompletedRouteNodes = completedRouteNodes.OrderBy(node => node).ToList(),
                    RouteIndex = routeIndex,
                    SelectedRouteNodeId = selectedRouteNodeId,
                    LastCompletedRouteNodeId = lastCompletedRouteNodeId,
                    RouteScroll = routeScroll,
                    EventResolved = eventResolved,
                    EventResult = eventResult,
                    RestResolved = restResolved,
                    RestResult = restResult,
                    Credits = credits,
                    Hull = runHull,
                    CargoIntegrity = runCargoIntegrity,
                    ContractBonus = runContractBonus,
                    ContractProcs = runContractProcs,
                    RepairBought = repairBought,
                    ShopPurgeBought = shopPurgeBought,
                    ShopCalibrationBought = shopCalibrationBought,
                    ShopBought = (bool[])shopBought.Clone(),
                    Turns = runTurns,
                    CardsPlayed = runCardsPlayed,
                    DamageTaken = runDamageTaken,
                    Overheats = runOverheats,
                    CalamityInterrupts = runCalamityInterrupts,
                    CalamityEvades = runCalamityEvades,
                    CalamityHits = runCalamityHits,
                    TrackingHits = runTrackingHits,
                    LastRewardCredits = lastRewardCredits,
                    LastFieldRepair = lastFieldRepair
                };
                foreach (KeyValuePair<CardId, UpgradeBranch> entry in runUpgradeBranches.OrderBy(entry => entry.Key))
                {
                    data.UpgradeBranchCards.Add((int)entry.Key);
                    data.UpgradeBranches.Add((int)entry.Value);
                }
                RunSaveService.Save(data);
                RecordRunDiagnostic("checkpoint_saved");
            }
            catch (Exception exception)
            {
                Debug.LogError($"RUN_SAVE_FAILED: {exception}");
                RecordRunDiagnostic("checkpoint_failed", exception.Message);
            }
        }

        private void CaptureBuildSnapshot(RunBuildSnapshotMoment moment, string key,
            int? hullOverride = null, int? cargoOverride = null)
        {
            if (string.IsNullOrWhiteSpace(key) || runDeck.Count == 0)
                return;

            var snapshot = new RunBuildSnapshot
            {
                Key = key,
                CapturedAtUtc = DateTime.UtcNow.ToString("O"),
                Moment = (int)moment,
                RouteColumn = routeIndex,
                RouteNodeId = selectedRouteNodeId,
                Act = (int)RunStructureCatalog.ActForColumn(routeIndex),
                Hull = hullOverride ?? runHull,
                CargoIntegrity = cargoOverride ?? runCargoIntegrity,
                Credits = credits,
                AirframeModification = (int)runModification,
                RouteStoryState = (int)routeStoryState,
                Deck = runDeck.Select(card => (int)card).ToList(),
                Upgrades = runUpgrades.Select(card => (int)card).OrderBy(card => card).ToList(),
                Modules = runModules.Select(module => (int)module).ToList()
            };
            foreach (KeyValuePair<CardId, UpgradeBranch> entry in runUpgradeBranches.OrderBy(entry => entry.Key))
            {
                snapshot.UpgradeBranchCards.Add((int)entry.Key);
                snapshot.UpgradeBranches.Add((int)entry.Value);
            }

            int existing = runBuildSnapshots.FindIndex(item => item != null && item.Key == key);
            if (existing >= 0)
                runBuildSnapshots[existing] = snapshot;
            else
                runBuildSnapshots.Add(snapshot);
            if (runBuildSnapshots.Count > RunBuildSnapshotRules.MaximumSnapshots)
                runBuildSnapshots.RemoveRange(0,
                    runBuildSnapshots.Count - RunBuildSnapshotRules.MaximumSnapshots);
        }

        private void InitializeRun(CargoContract contract, int? forcedSeed = null)
        {
            ChallengeDefinition challenge = ChallengeCatalog.Get(currentChallenge);
            runSeed = challenge.FixedSeed != 0
                ? challenge.FixedSeed
                : forcedSeed ?? RunSeedUtility.Create();
            runAttemptId = Guid.NewGuid().ToString("N");
            activeEncounterSeed = 0;
            selectedContract = contract;
            runModification = AirframeModification.None;
            routeStoryState = RouteStoryState.None;
            routeIntel = RouteIntel.None;
            departureDirective = DepartureDirective.Unselected;
            finalApproachPlan = FinalApproachPlan.Unselected;
            finaleEnding = FinaleEnding.None;
            runDeck.Clear();
            runUpgrades.Clear();
            runUpgradeBranches.Clear();
            runModules.Clear();
            runBuildSnapshots.Clear();
            runDeck.AddRange(CardPoolCatalog.CreateStarterDeck(contract));
            routeIndex = 0;
            completedRouteNodes.Clear();
            lastCompletedRouteNodeId = -1;
            selectedRouteNodeId = route.AtColumn(0).First().Id;
            routeScroll = 0f;
            eventResolved = false;
            eventResult = null;
            restResolved = false;
            restResult = null;
            credits = 40;
            runHull = challenge.StartingHull;
            runCargoIntegrity = 3;
            repairBought = false;
            shopPurgeBought = false;
            shopCalibrationBought = false;
            workshopCardValue = -1;
            workshopPage = 0;
            runTurns = 0;
            runCardsPlayed = 0;
            runDamageTaken = 0;
            runOverheats = 0;
            runCalamityInterrupts = 0;
            runCalamityEvades = 0;
            runCalamityHits = 0;
            runTrackingHits = 0;
            runContractBonus = 0;
            runContractProcs = 0;
            lastFieldRepair = 0;
            for (int i = 0; i < shopBought.Length; i++)
                shopBought[i] = false;
            screen = ScreenMode.DepartureBriefing;
            DeliveryArchiveService.RegisterRunStarted(archiveData, (int)selectedContract,
                runDeck.Select(card => (int)card), (int)currentChallenge);
            SaveArchive();
            RecordRunDiagnostic("run_started");
            SaveRunCheckpoint();
        }

        private void DrawDepartureBriefing()
        {
            DrawRect(new Rect(70, 48, 1460, 805), new Color32(2, 7, 22, 250));
            DrawRect(new Rect(78, 56, 1444, 789), PanelNight);
            DrawNeonFrame(new Rect(78, 56, 1444, 789), NeonCyan, 3f);
            DrawFittedLabel(new Rect(125, 82, 850, 62),
                L("departure.title", "离港派遣条款"), neonTitleStyle, 26);
            DrawFittedLabel(new Rect(1040, 92, 390, 35),
                L("departure.header", "ACT I // DEPARTURE CLAUSE"), hudCenteredStyle, 8);
            DrawFittedLabel(new Rect(130, 146, 1340, 42),
                L("departure.subtitle", "在第一架敌机升空前确定本局开场优势；收益会立即生效，条款签署后不可更改。"),
                neonBodyStyle, 12);

            Rect manifest = new Rect(300, 202, 1000, 42);
            DrawRect(manifest, new Color32(7, 23, 49, 245));
            DrawPixelOutline(manifest, CargoColor(selectedContract), 2f);
            DrawFittedLabel(new Rect(manifest.x + 20, manifest.y + 7, manifest.width - 40, 28),
                L("departure.contract", "合同 // {0}　初始牌组 {1} 张　机体 {2}/{3}　货物 {4}/3",
                    CargoName(selectedContract), runDeck.Count, runHull, BattleState.MaxPlayerHealth,
                    runCargoIntegrity), hudCenteredStyle, 9);

            DrawRunStructureChoice(new Rect(165, 270, 390, 430),
                L("departure.standard.title", "标准舱单"),
                L("departure.standard.badge", "稳健 // 邮票 +10"),
                L("departure.standard.benefit", "携带常规周转金离港，保留完整机体与货物。"),
                L("departure.standard.cost", "无附加代价。适合先观察首段路线再决定构筑。"),
                NeonCyan, true,
                () => ApplyDepartureDirective(DepartureDirective.StandardManifest),
                L("departure.sign", "签署条款"), 0);
            DrawRunStructureChoice(new Rect(605, 270, 390, 430),
                L("departure.advance.title", "预支报酬"),
                L("departure.advance.badge", "高周转 // 邮票 +32"),
                L("departure.advance.benefit", "提前获得大额邮票，可在首个补给站快速成形。"),
                L("departure.advance.cost", "货物完整度 -1。更高购买力换来更脆弱的合同。"),
                Gold, true,
                () => ApplyDepartureDirective(DepartureDirective.AdvancePayment),
                L("departure.sign", "签署条款"), 1);
            DrawRunStructureChoice(new Rect(1045, 270, 390, 430),
                L("departure.hot.title", "热启动"),
                L("departure.hot.badge", "构筑先手 // 核心牌 A"),
                L("departure.hot.benefit", "立即写入合同核心牌的增幅分支，全部同名副本共享。"),
                L("departure.hot.cost", "机体 -6。用早期结构损伤换取从第一战开始的构筑方向。"),
                NeonViolet, true,
                () => ApplyDepartureDirective(DepartureDirective.HotLaunch),
                L("departure.sign", "签署条款"), 2);

            DrawFittedLabel(new Rect(410, 752, 780, 34),
                L("departure.note", "三项条款只改变本局开场资源，不提供永久属性加成。"),
                tinyStyle, 9);
        }

        private void ApplyDepartureDirective(DepartureDirective directive)
        {
            if (departureDirective != DepartureDirective.Unselected ||
                (directive != DepartureDirective.StandardManifest &&
                 directive != DepartureDirective.AdvancePayment &&
                 directive != DepartureDirective.HotLaunch))
                return;

            switch (directive)
            {
                case DepartureDirective.StandardManifest:
                    credits += 10;
                    break;
                case DepartureDirective.AdvancePayment:
                    credits += 32;
                    runCargoIntegrity = Mathf.Max(0, runCargoIntegrity - 1);
                    break;
                case DepartureDirective.HotLaunch:
                {
                    CardId core = ContractCatalog.StarterCard(selectedContract);
                    runUpgrades.Add(core);
                    runUpgradeBranches[core] = UpgradeBranch.Alpha;
                    runHull = Mathf.Max(1, runHull - 6);
                    break;
                }
            }

            departureDirective = directive;
            CaptureBuildSnapshot(RunBuildSnapshotMoment.Departure, "departure");
            screen = ScreenAfterRouteTransition();
            controllerSelection = 0;
            PlayLayeredSound(rewardSound, 1.05f, 0.75f, clickSound, 0.8f, 0.5f);
            TriggerFullScreenImpact(0.7f, 0.35f, false);
            RecordRunDiagnostic("departure_directive",
                $"{directive}|credits={credits}|hull={runHull}|cargo={runCargoIntegrity}");
            SaveRunCheckpoint();
        }

        private void DrawRunStructureChoice(Rect rect, string title, string badge, string benefit,
            string cost, Color color, bool enabled, Action action, string actionLabel, int index)
        {
            bool selected = controllerSelection == index;
            bool hovered = rect.Contains(Event.current.mousePosition);
            if (hovered)
            {
                controllerSelection = index;
                RegisterHover($"run-structure-{screen}-{index}", title);
            }

            Color shown = enabled ? color : new Color32(92, 101, 120, 255);
            DrawRect(new Rect(rect.x + 9, rect.y + 11, rect.width, rect.height), new Color32(1, 5, 18, 255));
            DrawRect(rect, new Color32(9, 21, 45, 252));
            DrawPixelOutline(rect, selected ? Color.Lerp(shown, Color.white, 0.2f) : shown,
                selected ? 4f : 2f);
            DrawRect(new Rect(rect.x, rect.y, rect.width, 62), new Color32(13, 37, 70, 255));
            DrawRect(new Rect(rect.x, rect.y, 7, rect.height), shown);
            DrawFittedLabel(new Rect(rect.x + 22, rect.y + 11, rect.width - 44, 39),
                title, neonSubtitleStyle, 13);

            DrawRect(new Rect(rect.x + 24, rect.y + 78, rect.width - 48, 38), new Color32(5, 17, 39, 245));
            DrawPixelOutline(new Rect(rect.x + 24, rect.y + 78, rect.width - 48, 38), shown, 2f);
            DrawFittedLabel(new Rect(rect.x + 34, rect.y + 84, rect.width - 68, 26),
                badge, hudCenteredStyle, 8);

            Rect benefitRect = new Rect(rect.x + 24, rect.y + 135, rect.width - 48, 92);
            DrawRect(benefitRect, new Color32(4, 27, 34, 235));
            DrawPixelOutline(benefitRect, NeonCyan, 2f);
            DrawFittedLabel(new Rect(benefitRect.x + 14, benefitRect.y + 11,
                benefitRect.width - 28, benefitRect.height - 22), benefit, neonBodyStyle, 10);

            Rect costRect = new Rect(rect.x + 24, rect.y + 244, rect.width - 48, 92);
            DrawRect(costRect, new Color32(49, 15, 28, 235));
            DrawPixelOutline(costRect, enabled ? PostalRed : shown, 2f);
            DrawFittedLabel(new Rect(costRect.x + 14, costRect.y + 11,
                costRect.width - 28, costRect.height - 22), cost, neonBodyStyle, 10);

            DrawPixelButton(new Rect(rect.x + 48, rect.y + 361, rect.width - 96, 48),
                enabled ? actionLabel : L("run_structure.unavailable", "条件不足"),
                shown, action, enabled, (index + 1).ToString());
        }

        private void EnterCurrentNode()
        {
            RouteNodeDefinition node = route.Get(selectedRouteNodeId);
            if (!IsRouteNodeAvailable(node))
                return;

            eventResolved = false;
            eventResult = null;
            restResolved = false;
            restResult = null;
            switch (node.Kind)
            {
                case RouteNodeKind.Skirmish:
                case RouteNodeKind.Elite:
                case RouteNodeKind.Hunt:
                case RouteNodeKind.Boss:
                    if (node.Kind == RouteNodeKind.Boss)
                        CaptureBuildSnapshot(RunBuildSnapshotMoment.BossApproach, $"boss_{node.Id}");
                    StartBattle(node.Encounter);
                    break;
                case RouteNodeKind.Shop:
                    ResetShopInventory();
                    screen = ScreenMode.Shop;
                    TryShowTutorialOnce(TutorialTopic.Outpost);
                    break;
                case RouteNodeKind.Event:
                    screen = ScreenMode.Event;
                    TryShowTutorialOnce(TutorialTopic.Chronicle);
                    break;
                case RouteNodeKind.Rest:
                    screen = ScreenMode.Rest;
                    break;
            }
            SaveRunCheckpoint();
        }

        private bool IsRouteNodeAvailable(RouteNodeDefinition node)
        {
            if (node.Column != routeIndex)
                return false;
            if (routeIndex == 0)
                return node.Id == route.AtColumn(0).First().Id;
            return lastCompletedRouteNodeId >= 0 && route.Get(lastCompletedRouteNodeId).Next.Contains(node.Id);
        }

        private void SelectRouteNode(int nodeId)
        {
            RouteNodeDefinition node = route.Get(nodeId);
            if (!IsRouteNodeAvailable(node))
                return;
            selectedRouteNodeId = nodeId;
            PlaySound(clickSound, 1.18f, 0.42f);
            SaveRunCheckpoint();
        }

        private void CompleteCurrentRouteNode()
        {
            RouteNodeDefinition completed = route.Get(selectedRouteNodeId);
            completedRouteNodes.Add(completed.Id);
            lastCompletedRouteNodeId = completed.Id;
            routeIndex = Mathf.Min(route.ColumnCount - 1, completed.Column + 1);
            RouteNodeDefinition next = completed.Next.Select(route.Get).OrderBy(node => node.Lane).FirstOrDefault();
            if (next != null)
                selectedRouteNodeId = next.Id;
            FocusRouteColumn(routeIndex);
        }

        private void AdvanceAfterCurrentRouteNode()
        {
            CompleteCurrentRouteNode();
            screen = ScreenAfterRouteTransition();
            controllerSelection = 0;
            if (screen == ScreenMode.Retrofit)
                TryShowTutorialOnce(TutorialTopic.Retrofit);
        }

        private ScreenMode ScreenAfterRouteTransition()
        {
            if (departureDirective == DepartureDirective.Unselected)
                return ScreenMode.DepartureBriefing;
            if (runModification == AirframeModification.None && routeIndex >= RunStructureCatalog.RetrofitColumn)
                return ScreenMode.Retrofit;
            if (finalApproachPlan == FinalApproachPlan.Unselected &&
                routeIndex >= RunStructureCatalog.FinalApproachColumn)
                return ScreenMode.FinalApproach;
            return ScreenMode.Map;
        }

        private void FocusRouteColumn(int column)
        {
            const float columnSpacing = 250f;
            const float viewportWidth = 1360f;
            const float contentPadding = 145f;
            float contentWidth = contentPadding * 2f + route.ColumnCount * columnSpacing;
            float target = contentPadding + column * columnSpacing - viewportWidth * 0.42f;
            routeScroll = Mathf.Clamp(target, 0f, Mathf.Max(0f, contentWidth - viewportWidth));
        }

        private void ResetShopInventory()
        {
            repairBought = false;
            shopPurgeBought = false;
            shopCalibrationBought = false;
            workshopCardValue = -1;
            workshopPage = 0;
            for (int i = 0; i < shopBought.Length; i++)
                shopBought[i] = false;
        }

        private void StartBattle(EncounterId encounter)
        {
            activeEncounterSeed = RunSeedUtility.DeriveEncounterSeed(runSeed, selectedRouteNodeId, encounter);
            battle.StartEncounter(encounter, runDeck, runHull, runCargoIntegrity, selectedContract, runUpgrades, runModules,
                EncounterVariantForRun(encounter), runUpgradeBranches, activeEncounterSeed, runModification,
                routeStoryState, routeIntel, ChallengeCatalog.Get(currentChallenge).StartingHeat);
            enemyDeathFx.Clear();
            enemyAttackFx.Clear();
            enemyLaneFx.Clear();
            battleInputLockUntil = 0f;
            archiveFailureRecorded = false;
            commandChain = 0;
            commandChainTurn = 0;
            screen = ScreenMode.Battle;
            bannerText = encounter == EncounterId.Boss
                ? battle.EncounterVariant == 1
                    ? "WARNING // 天穹雷幕展开"
                    : "WARNING // 巨型磁暴反应"
                : $"AIRSPACE // {AirspaceRuleCatalog.Name(CurrentAirspace())} · {battle.FormationName}";
            bannerUntil = Time.time + 1.85f;
            TryShowTutorialOnce(encounter == EncounterId.Boss ? TutorialTopic.Boss : TutorialTopic.Intent);
            DeliveryArchiveService.RegisterBattleStarted(archiveData,
                battle.Enemies.Select(enemy => (int)enemy.Kind),
                runDeck.Select(card => (int)card), runModules.Select(module => (int)module));
            SaveArchive();
            RecordRunDiagnostic("battle_started", battle.FormationName);
            SaveRunCheckpoint();
        }

        private int EncounterVariantForRun(EncounterId encounter)
        {
            if (encounter == EncounterId.Boss)
                return selectedRouteNodeId == 19 ? 1 : 0;
            if (selectedRouteNodeId == 15 || selectedRouteNodeId == 16)
                return 4;
            if (selectedRouteNodeId == 17)
                return 5;
            return AirspaceRuleCatalog.EncounterVariant(CurrentAirspace(), activeEncounterSeed);
        }

        private void SetPaused(bool value)
        {
            paused = value;
            if (!showFirstBattleGuide)
                Time.timeScale = paused ? 0f : 1f;
        }

        private void DrawSystemButton()
        {
            Rect rect = screen == ScreenMode.Contract
                ? new Rect(1445, 92, 54, 48)
                : new Rect(1490, 122, 54, 48);
            bool hovered = rect.Contains(Event.current.mousePosition);
            Color frame = hovered ? NeonCyan : new Color32(122, 78, 190, 255);
            DrawRect(new Rect(rect.x + 5, rect.y + 5, rect.width, rect.height), new Color32(2, 6, 18, 230));
            DrawRect(rect, new Color32(7, 18, 43, 248));
            DrawPixelOutline(rect, frame, 3f);
            DrawRect(new Rect(rect.x + 17, rect.y + 12, 6, 24), frame);
            DrawRect(new Rect(rect.x + 31, rect.y + 12, 6, 24), frame);
            if (hovered)
            {
                RegisterHover("button-pause", "暂停");
                DrawNeonFrame(new Rect(rect.x - 2, rect.y - 2, rect.width + 4, rect.height + 4), NeonCyan, 2f);
            }
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                PlaySound(clickSound);
                SetPaused(true);
            }
        }

        private void DrawPauseOverlay()
        {
            DrawRect(new Rect(0, 0, ReferenceWidth, ReferenceHeight), new Color32(1, 5, 16, 225));
            Rect panel = new Rect(500, 125, 600, 650);
            DrawRect(panel, new Color32(7, 18, 43, 255));
            DrawNeonFrame(panel, NeonCyan, 3f);
            GUI.Label(new Rect(560, 180, 480, 64), L("pause.title", "系统暂停"), neonTitleStyle);
            DrawFittedLabel(new Rect(570, 250, 460, 42),
                $"DISPLAY // {DisplayModeLabel()}　{gameSettings.ResolutionWidth}×{gameSettings.ResolutionHeight}",
                hudCenteredStyle, 9);
            DrawFittedLabel(new Rect(570, 280, 460, 24), $"RUN SEED // {runSeed:X8}", tinyStyle, 9);
            DrawPixelButton(new Rect(610, 310, 380, 58), L("pause.resume", "继续配送"), NeonCyan, () => SetPaused(false), true, "ESC");
            DrawPixelButton(new Rect(610, 380, 380, 58), L("pause.settings", "系统设置"), Gold, () => OpenSettings(true));
            DrawPixelButton(new Rect(610, 450, 380, 58), L("pause.guide", "规则说明"), NeonViolet, () =>
            {
                SetPaused(false);
                OpenRulebook();
            });
            DrawPixelButton(new Rect(610, 520, 380, 58), L("pause.restart", "重新开始本次配送"), PostalRed, () =>
            {
                SetPaused(false);
                StartNewRun();
            });
            DrawPixelButton(new Rect(610, 590, 380, 58), L("pause.title_button", "返回标题"), Shadow, () =>
            {
                SetPaused(false);
                screen = ScreenMode.Title;
            });
        }

        private void OpenSettings(bool returnToPause)
        {
            settingsReturnToPause = returnToPause;
            settingsOpen = true;
        }

        private void CloseSettings()
        {
            SaveAndApplySettings(false);
            settingsOpen = false;
            if (!settingsReturnToPause)
                SetPaused(false);
        }

        private void DrawSettingsOverlay()
        {
            DrawRect(new Rect(0, 0, ReferenceWidth, ReferenceHeight), new Color32(1, 5, 16, 235));
            Rect panel = new Rect(285, 60, 1030, 790);
            DrawRect(panel, new Color32(7, 18, 43, 255));
            DrawNeonFrame(panel, NeonCyan, 3f);
            DrawFittedLabel(new Rect(350, 95, 650, 62), L("settings.title", "系统设置"), neonTitleStyle, 26);
            DrawFittedLabel(new Rect(1000, 108, 245, 36), L("settings.section", "ACCESS // DISPLAY"), hudCenteredStyle, 8);

            DrawSettingStepper(165, L("settings.display", "显示模式"), DisplayModeLabel(),
                CycleDisplayModeBackward, CycleDisplayModeForward);
            DrawSettingStepper(215, L("settings.resolution", "分辨率"),
                $"{gameSettings.ResolutionWidth} × {gameSettings.ResolutionHeight}",
                () => CycleResolution(-1), () => CycleResolution(1));
            DrawSettingStepper(265, L("settings.vsync", "垂直同步"),
                gameSettings.VSync ? L("settings.on", "开启") : L("settings.off", "关闭"),
                ToggleVSync, ToggleVSync);
            DrawSettingStepper(315, L("settings.framerate", "帧率上限"),
                gameSettings.VSync ? L("settings.monitor", "由显示器同步") : $"{gameSettings.FrameRate} FPS",
                () => CycleFrameRate(-1), () => CycleFrameRate(1));
            DrawSettingStepper(365, L("settings.language", "语言"),
                L("language.name", "简体中文"), CycleLanguage, CycleLanguage);
            DrawSettingStepper(420, L("settings.tutorials", "情境教学"),
                gameSettings.ContextualTutorials ? L("settings.on", "开启") : L("settings.off", "关闭"),
                ToggleContextualTutorials, ToggleContextualTutorials);
            DrawSettingStepper(470, L("settings.focus_hints", "焦点提示"),
                gameSettings.FocusHints ? L("settings.on", "开启") : L("settings.off", "关闭"),
                ToggleFocusHints, ToggleFocusHints);

            float previousMusic = musicVolume;
            float previousSfx = sfxVolume;
            float previousShake = gameSettings.ShakeIntensity;
            float previousFlash = gameSettings.FlashIntensity;
            DrawSettingsSlider(530, L("settings.music", "音乐音量"), ref musicVolume);
            DrawSettingsSlider(580, L("settings.sfx", "音效音量"), ref sfxVolume);
            float shake = gameSettings.ShakeIntensity;
            DrawSettingsSlider(630, L("settings.shake", "震屏强度"), ref shake);
            gameSettings.ShakeIntensity = shake;
            float flash = gameSettings.FlashIntensity;
            DrawSettingsSlider(680, L("settings.flash", "闪光强度"), ref flash);
            gameSettings.FlashIntensity = flash;
            gameSettings.MusicVolume = musicVolume;
            gameSettings.SfxVolume = sfxVolume;
            if (!Mathf.Approximately(previousMusic, musicVolume) || !Mathf.Approximately(previousSfx, sfxVolume) ||
                !Mathf.Approximately(previousShake, gameSettings.ShakeIntensity) ||
                !Mathf.Approximately(previousFlash, gameSettings.FlashIntensity))
                SaveAndApplySettings(false);

            DrawPixelButton(new Rect(570, 760, 460, 58), settingsReturnToPause
                    ? L("settings.back_pause", "返回暂停菜单") : L("settings.save_back", "保存并返回"),
                NeonCyan, CloseSettings, true, "ESC");
        }

        private void DrawSettingStepper(float y, string label, string value, Action previous, Action next)
        {
            DrawFittedLabel(new Rect(365, y, 265, 48), label, hudStyle, 10);
            Rect valuePanel = new Rect(720, y - 2, 360, 52);
            DrawRect(valuePanel, new Color32(3, 11, 31, 245));
            DrawNeonFrame(valuePanel, NeonViolet, 2f);
            DrawFittedLabel(valuePanel, value, hudCenteredStyle, 9);
            DrawStepperArrowButton(new Rect(645, y - 2, 58, 52), "<", previous);
            DrawStepperArrowButton(new Rect(1097, y - 2, 58, 52), ">", next);
        }

        private void DrawStepperArrowButton(Rect rect, string arrow, Action action)
        {
            bool hovered = rect.Contains(Event.current.mousePosition);
            Color color = hovered ? Gold : new Color32(30, 70, 105, 255);
            DrawRect(new Rect(rect.x + 5, rect.y + 6, rect.width, rect.height), new Color32(2, 7, 20, 250));
            DrawRect(rect, color);
            DrawPixelOutline(rect, hovered ? NeonCyan : new Color32(7, 16, 38, 255), hovered ? 3f : 2f);
            DrawRect(new Rect(rect.x + 7, rect.y + 7, rect.width - 14, rect.height - 14),
                new Color32(4, 13, 34, 220));
            var arrowStyle = new GUIStyle(neonSubtitleStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24
            };
            GUI.Label(rect, arrow, arrowStyle);
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                PlaySound(clickSound, arrow == "<" ? 0.94f : 1.08f, 0.32f);
                action?.Invoke();
            }
        }

        private void DrawSettingsSlider(float y, string label, ref float value)
        {
            DrawFittedLabel(new Rect(365, y, 265, 42), label, hudStyle, 10);
            float next = GUI.HorizontalSlider(new Rect(650, y + 12, 410, 26), value, 0f, 1f);
            DrawFittedLabel(new Rect(1080, y, 90, 42), $"{Mathf.RoundToInt(next * 100)}%", hudCenteredStyle, 8);
            if (!Mathf.Approximately(next, value))
                value = next;
        }

        private string DisplayModeLabel()
        {
            return (FullScreenMode)gameSettings.DisplayMode switch
            {
                FullScreenMode.FullScreenWindow => L("display.borderless", "无边框全屏"),
                FullScreenMode.ExclusiveFullScreen => L("display.exclusive", "独占全屏"),
                _ => L("display.windowed", "窗口")
            };
        }

        private void CycleLanguage()
        {
            GameLanguage next = (GameLanguage)gameSettings.Language == GameLanguage.English
                ? GameLanguage.SimplifiedChinese : GameLanguage.English;
            gameSettings.Language = (int)next;
            LocalizationService.SetLanguage(next);
            SaveAndApplySettings(false);
        }

        private void CycleDisplayModeBackward()
        {
            FullScreenMode mode = (FullScreenMode)gameSettings.DisplayMode;
            gameSettings.DisplayMode = (int)(mode == FullScreenMode.Windowed
                ? FullScreenMode.ExclusiveFullScreen
                : mode == FullScreenMode.ExclusiveFullScreen
                    ? FullScreenMode.FullScreenWindow
                    : FullScreenMode.Windowed);
            SaveAndApplySettings(true);
        }

        private void CycleDisplayModeForward()
        {
            FullScreenMode mode = (FullScreenMode)gameSettings.DisplayMode;
            gameSettings.DisplayMode = (int)(mode == FullScreenMode.Windowed
                ? FullScreenMode.FullScreenWindow
                : mode == FullScreenMode.FullScreenWindow
                    ? FullScreenMode.ExclusiveFullScreen
                    : FullScreenMode.Windowed);
            SaveAndApplySettings(true);
        }

        private void CycleResolution(int direction)
        {
            int current = Array.FindIndex(SupportedResolutions,
                resolution => resolution.x == gameSettings.ResolutionWidth &&
                    resolution.y == gameSettings.ResolutionHeight);
            if (current < 0)
                current = 1;
            int next = (current + direction + SupportedResolutions.Length) % SupportedResolutions.Length;
            gameSettings.ResolutionWidth = SupportedResolutions[next].x;
            gameSettings.ResolutionHeight = SupportedResolutions[next].y;
            SaveAndApplySettings(true);
        }

        private void ToggleVSync()
        {
            gameSettings.VSync = !gameSettings.VSync;
            SaveAndApplySettings(false);
        }

        private void ToggleContextualTutorials()
        {
            gameSettings.ContextualTutorials = !gameSettings.ContextualTutorials;
            SaveAndApplySettings(false);
        }

        private void ToggleFocusHints()
        {
            gameSettings.FocusHints = !gameSettings.FocusHints;
            SaveAndApplySettings(false);
        }

        private void CycleFrameRate(int direction)
        {
            int[] rates = { 30, 60, 120, 144, 240 };
            int current = Array.IndexOf(rates, gameSettings.FrameRate);
            if (current < 0)
                current = 1;
            gameSettings.FrameRate = rates[(current + direction + rates.Length) % rates.Length];
            SaveAndApplySettings(false);
        }

        private void SaveAndApplySettings(bool applyDisplay)
        {
            gameSettings.MusicVolume = musicVolume;
            gameSettings.SfxVolume = sfxVolume;
            GameSettingsService.Save(gameSettings);
            GameSettingsService.Apply(gameSettings, applyDisplay);
        }

        private void DrawControllerFocus()
        {
            Rect focus = default;
            if (settingsOpen)
            {
                focus = controllerSelection switch
                {
                    0 => new Rect(350, 155, 800, 55),
                    1 => new Rect(350, 205, 800, 55),
                    2 => new Rect(350, 255, 800, 55),
                    3 => new Rect(350, 305, 800, 55),
                    4 => new Rect(350, 355, 800, 55),
                    5 => new Rect(350, 410, 800, 55),
                    6 => new Rect(350, 460, 800, 55),
                    7 => new Rect(350, 520, 800, 50),
                    8 => new Rect(350, 570, 800, 50),
                    9 => new Rect(350, 620, 800, 50),
                    10 => new Rect(350, 670, 800, 50),
                    _ => new Rect(570, 760, 460, 58)
                };
            }
            else if (paused)
            {
                focus = new Rect(610, 310 + controllerSelection * 70, 380, 58);
            }
            else if (showFirstBattleGuide)
            {
                focus = tutorialRulebookMode
                    ? new Rect(1035, 735, 385, 58)
                    : new Rect(585, 625, 430, 66);
            }
            else
            {
                switch (screen)
                {
                    case ScreenMode.Title:
                        if (RunSaveService.HasSave)
                            focus = controllerSelection switch
                            {
                                0 => new Rect(630, 545, 330, 68),
                                1 => new Rect(630, 630, 330, 62),
                                2 => new Rect(985, 545, 250, 68),
                                _ => new Rect(985, 630, 250, 62)
                            };
                        else
                            focus = controllerSelection switch
                            {
                                0 => new Rect(630, 555, 330, 74),
                                1 => new Rect(985, 555, 250, 55),
                                _ => new Rect(985, 625, 250, 55)
                            };
                        break;
                    case ScreenMode.Archive:
                        focus = controllerSelection switch
                        {
                            0 => new Rect(680, 120, 180, 38),
                            1 => new Rect(870, 120, 180, 38),
                            2 => new Rect(1060, 120, 180, 38),
                            3 => new Rect(1250, 120, 180, 38),
                            _ => new Rect(640, 772, 320, 56)
                        };
                        break;
                    case ScreenMode.Challenge:
                        focus = new Rect(155 + controllerSelection % 2 * 655,
                            245 + controllerSelection / 2 * 245, 610, 205);
                        break;
                    case ScreenMode.Contract:
                        focus = new Rect(515, 196, 570, 520);
                        break;
                    case ScreenMode.DepartureBriefing:
                        focus = new Rect(165 + controllerSelection * 440, 270, 390, 430);
                        break;
                    case ScreenMode.Map:
                        focus = new Rect(1060, 683, 350, 72);
                        break;
                    case ScreenMode.Retrofit:
                        focus = new Rect(165 + controllerSelection * 425, 270, 390, 430);
                        break;
                    case ScreenMode.FinalApproach:
                        focus = controllerSelection < 3
                            ? new Rect(165 + controllerSelection * 440, 260, 390, 430)
                            : new Rect(515, 735, 570, 58);
                        break;
                    case ScreenMode.Battle:
                        if (battle.Victory)
                        {
                            focus = new Rect(635, 565, 330, 66);
                        }
                        else if (battle.Defeat)
                        {
                            focus = controllerSelection switch
                            {
                                0 => new Rect(125, 705, 405, 66),
                                1 => new Rect(598, 705, 405, 66),
                                _ => new Rect(1070, 705, 390, 66)
                            };
                        }
                        else if (controllerSelection >= battle.Hand.Count)
                        {
                            focus = new Rect(1360, 705, 180, 72);
                        }
                        else
                        {
                            GetHandLayout(out float cardWidth, out float gap, out float startX);
                            focus = new Rect(startX + controllerSelection * (cardWidth + gap), 620, cardWidth, 235);
                        }
                        break;
                    case ScreenMode.Reward:
                        focus = controllerSelection < 3
                            ? new Rect(260 + controllerSelection * 360, 285, 280, 345)
                            : new Rect(610, 690, 380, 62);
                        break;
                    case ScreenMode.Shop:
                        focus = controllerSelection < 3
                            ? new Rect(125 + controllerSelection * 305, 245, 270, 315)
                            : controllerSelection == 3
                                ? new Rect(1063, 285, 354, 102)
                                : controllerSelection == 4
                                    ? new Rect(1063, 399, 354, 102)
                                    : controllerSelection == 5
                                        ? new Rect(1063, 513, 354, 102)
                                        : new Rect(590, 685, 420, 72);
                        break;
                    case ScreenMode.Event:
                        focus = eventResolved
                            ? new Rect(1055, 659, 205, 52)
                            : new Rect(controllerSelection == 0 ? 175 :
                                controllerSelection == 1 ? 605 : 1035, 295, 390, 310);
                        break;
                    case ScreenMode.Rest:
                        focus = restResolved
                            ? new Rect(1055, 659, 205, 52)
                            : new Rect(controllerSelection == 0 ? 225 : controllerSelection == 1 ? 625 : 1025,
                                295, 350, 310);
                        break;
                    case ScreenMode.DeckPurge:
                    case ScreenMode.FinalTrim:
                    case ScreenMode.ShopPurge:
                    case ScreenMode.EventPurge:
                    {
                        int candidateCount = PurgeCandidates().Length;
                        if (controllerSelection >= candidateCount)
                            focus = new Rect(1050, 690, 360, 58);
                        else
                        {
                            int local = controllerSelection % 10;
                            focus = new Rect(135 + (local % 5) * 275, 225 + (local / 5) * 220, 245, 185);
                        }
                        break;
                    }
                    case ScreenMode.WorkshopCardSelect:
                    {
                        int candidateCount = WorkshopCandidates().Length;
                        if (controllerSelection >= candidateCount)
                            focus = new Rect(1050, 690, 360, 58);
                        else
                        {
                            int local = controllerSelection % 10;
                            focus = new Rect(135 + (local % 5) * 275, 225 + (local / 5) * 220, 245, 185);
                        }
                        break;
                    }
                    case ScreenMode.WorkshopBranch:
                        focus = controllerSelection == 0
                            ? new Rect(310, 245, 360, 390)
                            : controllerSelection == 1
                                ? new Rect(930, 245, 360, 390)
                                : new Rect(610, 700, 380, 58);
                        break;
                    case ScreenMode.CoreUpgrade:
                        focus = controllerSelection == 0
                            ? new Rect(310, 245, 360, 390)
                            : controllerSelection == 1
                                ? new Rect(930, 245, 360, 390)
                                : new Rect(610, 700, 380, 58);
                        break;
                    case ScreenMode.Complete:
                        focus = controllerSelection switch
                        {
                            0 => new Rect(340, 700, 280, 72),
                            1 => new Rect(660, 700, 280, 72),
                            _ => new Rect(980, 700, 280, 72)
                        };
                        break;
                }
            }

            if (focus.width > 0f)
            {
                float pulse = 2f + (Mathf.Sin(Time.unscaledTime * 8f) + 1f) * 1.2f;
                DrawNeonFrame(new Rect(focus.x - 7, focus.y - 7, focus.width + 14, focus.height + 14),
                    new Color32(255, 211, 82, 255), pulse);
            }
            DrawRect(new Rect(510, 855, 580, 30), new Color32(3, 10, 28, 230));
            DrawFittedLabel(new Rect(520, 858, 560, 24),
                settingsOpen && keyboardFocusActive ? L("keyboard.settings",
                    "方向键 / WASD 选择与调整　ENTER 确认　ESC 返回") :
                settingsOpen ? L("controller.settings", "左摇杆选择 / 调整　[A] 确认　[B] 返回") :
                screen == ScreenMode.Battle && !paused ? L("controller.battle",
                    "左摇杆选择　[A] 确认　[Y] 结束回合　[MENU] 暂停") :
                screen == ScreenMode.Retrofit ? L("controller.retrofit",
                    "左摇杆选择　[A] 执行永久改装　[MENU] 暂停") :
                screen == ScreenMode.DepartureBriefing ? L("controller.departure", "左摇杆选择　[A] 签署条款　[MENU] 暂停") :
                screen == ScreenMode.FinalApproach ? L("controller.final_approach", "左摇杆选择　[A] 执行方案　[MENU] 暂停") :
                L("controller.default", "左摇杆选择　[A] 确认　[B] 返回　[MENU] 暂停"),
                hudCenteredStyle, 8);
        }

        private void DrawFirstBattleGuide()
        {
            DrawRect(new Rect(0, 0, ReferenceWidth, ReferenceHeight), new Color32(1, 5, 16, 230));
            Rect panel = tutorialRulebookMode
                ? new Rect(115, 70, 1370, 760)
                : new Rect(330, 145, 940, 610);
            DrawRect(panel, new Color32(7, 18, 43, 255));
            Color topicColor = TutorialTopicColor(activeTutorialTopic);
            DrawNeonFrame(panel, topicColor, 3f);
            RuleGlossaryEntry entry = RuleGlossaryCatalog.Get(activeTutorialTopic);

            if (tutorialRulebookMode)
            {
                DrawFittedLabel(new Rect(165, 100, 780, 54),
                    L("rulebook.title", "规则说明与术语词典"), neonTitleStyle, 22);
                DrawFittedLabel(new Rect(1030, 110, 390, 32),
                    L("rulebook.status", "F1 随时查看 // 方向键切换"), hudCenteredStyle, 8);
                for (int i = 0; i < RuleGlossaryCatalog.All.Count; i++)
                {
                    RuleGlossaryEntry listed = RuleGlossaryCatalog.All[i];
                    Rect item = new Rect(165, 172 + i * 52, 325, 44);
                    bool selected = listed.Topic == activeTutorialTopic;
                    Color listedColor = TutorialTopicColor(listed.Topic);
                    DrawRect(item, selected ? new Color32(20, 52, 82, 255) : new Color32(4, 15, 35, 245));
                    DrawPixelOutline(item, selected ? listedColor : new Color32(47, 83, 116, 255),
                        selected ? 3f : 1f);
                    DrawFittedLabel(new Rect(item.x + 12, item.y + 7, 45, 30), listed.Symbol,
                        hudCenteredStyle, 9);
                    DrawFittedLabel(new Rect(item.x + 62, item.y + 6, 245, 31), listed.Title,
                        hudStyle, 9);
                    if (GUI.Button(item, GUIContent.none, GUIStyle.none))
                    {
                        activeTutorialTopic = listed.Topic;
                        firstBattleGuidePage = i;
                        PlaySound(clickSound, 1.12f, 0.28f);
                    }
                }
                DrawTutorialRuleDetail(new Rect(535, 172, 885, 520), entry, topicColor, true);
                DrawPixelButton(new Rect(1035, 735, 385, 58), L("rulebook.close", "返回配送"),
                    NeonCyan, CloseTutorialOverlay, true, "ESC");
                DrawFittedLabel(new Rect(550, 715, 450, 38),
                    L("rulebook.controls", "左右方向键 / 左摇杆浏览　ENTER / [A] 返回"),
                    tinyStyle, 8);
                return;
            }

            DrawFittedLabel(new Rect(405, 185, 790, 52),
                L("tutorial.contextual", "渐进教学 // 根据当前行为触发"), hudCenteredStyle, 9);
            DrawTutorialRuleDetail(new Rect(405, 255, 790, 280), entry, topicColor, false);
            DrawFittedLabel(new Rect(470, 560, 660, 38),
                L("tutorial.once", "本条只自动显示一次；可按 F1 随时重看。"),
                hudCenteredStyle, 9);
            DrawPixelButton(new Rect(585, 625, 430, 66), L("tutorial.understood", "明白，继续"),
                topicColor, CloseTutorialOverlay, true, "ENTER");
        }

        private void DrawTutorialRuleDetail(Rect rect, RuleGlossaryEntry entry, Color color, bool glossary)
        {
            DrawRect(rect, new Color32(3, 11, 31, 245));
            DrawPixelOutline(rect, color, 2f);
            Rect symbol = new Rect(rect.x + 26, rect.y + 28, 92, 92);
            DrawRect(symbol, new Color(color.r * 0.22f, color.g * 0.22f, color.b * 0.22f, 1f));
            DrawPixelOutline(symbol, color, 3f);
            DrawFittedLabel(symbol, entry.Symbol, neonTitleStyle, 24);
            DrawFittedLabel(new Rect(rect.x + 140, rect.y + 22, rect.width - 170, 48), entry.Title,
                neonTitleStyle, 20);
            DrawFittedLabel(new Rect(rect.x + 140, rect.y + 76, rect.width - 170, 30),
                L("tutorial.category", "规则类型 // {0}", entry.Category), hudStyle, 9);
            if (!glossary)
            {
                DrawRect(new Rect(rect.x + 26, rect.y + 136, rect.width - 52, 44),
                    new Color32(13, 35, 61, 255));
                DrawFittedLabel(new Rect(rect.x + 42, rect.y + 143, rect.width - 84, 30),
                    entry.Trigger, hudStyle, 9);
                DrawFittedLabel(new Rect(rect.x + 42, rect.y + 194, rect.width - 84, rect.height - 216),
                    entry.Body, neonBodyStyle, 13);
            }
            else
            {
                DrawFittedLabel(new Rect(rect.x + 42, rect.y + 145, rect.width - 84, rect.height - 180),
                    entry.Glossary, neonBodyStyle, 13);
            }
        }

        private void TryShowTutorialOnce(TutorialTopic topic)
        {
            tutorialProgress ??= TutorialProgressService.Load();
            if (!gameSettings.ContextualTutorials || showFirstBattleGuide || settingsOpen || paused ||
                TutorialProgressService.HasSeen(tutorialProgress, topic))
                return;
            activeTutorialTopic = topic;
            firstBattleGuidePage = (int)topic;
            tutorialRulebookMode = false;
            showFirstBattleGuide = true;
            Time.timeScale = 0f;
        }

        private void OpenRulebook()
        {
            tutorialProgress ??= TutorialProgressService.Load();
            firstBattleGuidePage = Mathf.Clamp(firstBattleGuidePage, 0, RuleGlossaryCatalog.All.Count - 1);
            activeTutorialTopic = RuleGlossaryCatalog.All[firstBattleGuidePage].Topic;
            tutorialRulebookMode = true;
            showFirstBattleGuide = true;
            Time.timeScale = 0f;
        }

        private void CycleTutorialTopic(int direction)
        {
            firstBattleGuidePage = WrapSelection(firstBattleGuidePage + direction, RuleGlossaryCatalog.All.Count);
            activeTutorialTopic = RuleGlossaryCatalog.All[firstBattleGuidePage].Topic;
            PlaySound(clickSound, direction < 0 ? 0.94f : 1.08f, 0.26f);
        }

        private void CloseTutorialOverlay()
        {
            if (!tutorialRulebookMode)
                TutorialProgressService.MarkSeen(tutorialProgress, activeTutorialTopic);
            tutorialRulebookMode = false;
            showFirstBattleGuide = false;
            Time.timeScale = paused ? 0f : 1f;
        }

        private static Color TutorialTopicColor(TutorialTopic topic)
        {
            return topic switch
            {
                TutorialTopic.Intent => new Color32(255, 211, 82, 255),
                TutorialTopic.LaneAttack => new Color32(255, 104, 96, 255),
                TutorialTopic.Heat => new Color32(255, 165, 65, 255),
                TutorialTopic.Cargo => new Color32(83, 220, 158, 255),
                TutorialTopic.LaneShift => NeonCyan,
                TutorialTopic.Tracking => new Color32(255, 92, 154, 255),
                TutorialTopic.Retrofit => NeonViolet,
                TutorialTopic.Chronicle => new Color32(120, 178, 255, 255),
                TutorialTopic.Outpost => Gold,
                _ => PostalRed
            };
        }

        private void PlayCardWithFeedback(int handIndex)
        {
            if (!CanPlayInteractive(handIndex))
                return;

            CardId id = battle.Hand[handIndex];
            bool upgradedCard = battle.IsUpgraded(id);
            int laneBefore = battle.PlayerLane;
            int heatBefore = battle.Heat;
            int hullBefore = battle.PlayerHealth;
            int enemyDurabilityBefore = TotalEnemyDurability();
            int[] enemyHealthSnapshot = new int[battle.Enemies.Count];
            int[] enemyArmorSnapshot = new int[battle.Enemies.Count];
            int[] enemyPhaseSnapshot = new int[battle.Enemies.Count];
            bool[] chargeInterruptedSnapshot = new bool[battle.Enemies.Count];
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                enemyHealthSnapshot[i] = battle.Enemies[i].Health;
                enemyArmorSnapshot[i] = battle.Enemies[i].Armor;
                enemyPhaseSnapshot[i] = battle.Enemies[i].Phase;
                chargeInterruptedSnapshot[i] = battle.Enemies[i].ChargeInterrupted;
            }
            battle.PlayCard(handIndex);
            bool maneuverCard = CardLibrary.Get(id).Family == CardFamily.Maneuver;
            bool volleyCard = id == CardId.BroadsideVolley || id == CardId.MeltdownBurst ||
                id == CardId.Scattershot || id == CardId.MissileSwarm || id == CardId.InterceptMine ||
                ExpandedCardCatalog.IsVolley(id);
            battleInputLockUntil = Time.time + (maneuverCard ? 0.58f : 0.12f);
            if (commandChainTurn != battle.Turn)
            {
                commandChainTurn = battle.Turn;
                commandChain = 0;
            }
            commandChain++;
            commandChainUntil = Time.time + 0.9f;
            if (maneuverCard && laneBefore != battle.PlayerLane)
                BeginLaneTransition(laneBefore, battle.PlayerLane);
            int damageDealt = Mathf.Max(0, enemyDurabilityBefore - TotalEnemyDurability());
            combatFxStart = Time.time;
            combatFxDuration = 0.7f;
            combatFxLane = battle.PlayerLane;
            combatFxCard = id;
            combatFxPower = 1f;

            if (damageDealt > 0)
            {
                impactDamage = damageDealt;
                impactPoint = new Vector2(volleyCard ? 1110f : 1060f,
                    volleyCard ? 320f : 190f + battle.PlayerLane * 130f);
                impactFlashUntil = Time.time + 0.86f;
                enemyRecoilUntil = Time.time + 0.62f;
                StartCoroutine(DelayedHitStop(id == CardId.OverloadAim ? 0.27f : 0.23f,
                    id == CardId.OverloadAim || id == CardId.BroadsideVolley ? 0.14f : 0.075f));
            }

            bool destroyedTarget = false;
            bool interruptedCharge = false;
            bool bossPhaseTransition = false;
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                EnemyState enemy = battle.Enemies[i];
                if ((enemyHealthSnapshot[i] > enemy.Health || enemyArmorSnapshot[i] > enemy.Armor) && !volleyCard)
                    impactPoint = EnemyBasePosition(i, enemy);
                if (!chargeInterruptedSnapshot[i] && enemy.ChargeInterrupted)
                    interruptedCharge = true;
                if ((enemy.Kind == EnemyKind.StormManta || enemy.Kind == EnemyKind.CloudWyrm) &&
                    enemy.Phase > enemyPhaseSnapshot[i])
                    bossPhaseTransition = true;
                if (enemyHealthSnapshot[i] > 0 && !enemy.Alive)
                {
                    Vector2 deathPosition = EnemyBasePosition(i, enemy);
                    enemyDeathFx.Add(new EnemyDeathFx
                    {
                        Position = deathPosition,
                        StartTime = Time.time,
                        Name = ArchiveEnemyName(enemy.Kind),
                        Seed = 31 + i * 17 + battle.Turn * 13,
                        Kind = enemy.Kind
                    });
                    impactPoint = deathPosition;
                    destroyedTarget = true;
                }
            }

            if (destroyedTarget)
            {
                bannerText = L("feedback.target_break", "TARGET BREAK // 敌机解体");
                bannerUntil = Time.time + 1.15f;
                impactFlashUntil = Time.time + 1.2f;
                combatFxDuration = Mathf.Max(combatFxDuration, 1.05f);
                TriggerShake(31f, 0.82f);
                PlayLayeredSound(destructionSound, 0.82f, 0.9f, lowExplosionSound, 0.68f, 0.88f);
                StartCoroutine(DelayedHitStop(0.38f, 0.18f));
                TriggerFullScreenImpact(2.25f, 1.08f, true);
            }
            else if (bossPhaseTransition)
            {
                bannerText = $"BOSS MATRIX // {BossStoryAlignmentName()}";
                bannerUntil = Time.time + 1.65f;
                PlayLayeredSound(warningSound, 0.72f, 0.9f, rewardSound,
                    battle.ActiveBossStoryAlignment == BossStoryAlignment.Allied ? 1.2f : 0.78f, 0.58f);
                TriggerShake(18f, 0.58f);
                TriggerFullScreenImpact(1.45f, 0.82f, false);
            }

            bool expandedFeedback = ExpandedCardCatalog.Contains(id);
            if (expandedFeedback)
            {
                CardSpec expandedCard = CardLibrary.Get(id);
                if (ExpandedCardCatalog.IsDamaging(id))
                {
                    combatFx = volleyCard ? CombatFx.Volley : CombatFx.Shot;
                    combatFxPower = AttackFxPower(id);
                    combatFxDuration = AttackFxDuration(id);
                    combatFxText = $"{expandedCard.Name} // {damageDealt} {(volleyCard ? "TOTAL" : "HIT")}";
                    PlayAttackSound(id, damageDealt > 0);
                    TriggerShake(AttackShake(id), AttackShakeDuration(id));
                    if (!destroyedTarget)
                        TriggerFullScreenImpact(AttackScreenPower(id), AttackFxDuration(id), false);
                }
                else if (expandedCard.Family == CardFamily.Maneuver)
                {
                    combatFx = CombatFx.Maneuver;
                    combatFxDuration = 0.72f;
                    combatFxText = $"{expandedCard.Name} // {L("feedback.lane_value", "航道 {0}", battle.PlayerLane + 1)}";
                }
                else if (ExpandedCardCatalog.IsCooling(id))
                {
                    combatFx = CombatFx.Coolant;
                    combatFxText = $"{expandedCard.Name} // {L("feedback.heat_value", "热量 {0}", battle.Heat)}";
                    PlaySound(shieldSound, 0.75f);
                }
                else if (expandedCard.Family == CardFamily.Defense)
                {
                    combatFx = CombatFx.Shield;
                    combatFxText = $"{expandedCard.Name} // {L("feedback.shield_value", "护盾 {0}", battle.Armor)}";
                    PlaySound(shieldSound);
                }
                else
                {
                    combatFx = CombatFx.Overclock;
                    combatFxText = $"{expandedCard.Name} // {L("feedback.command_complete", "指令完成")}";
                    PlaySound(clickSound, 1.25f);
                }
            }

            if (!expandedFeedback) switch (id)
            {
                case CardId.BurstFire:
                case CardId.OverloadAim:
                case CardId.RailPiercer:
                case CardId.PursuitShot:
                case CardId.SlipstreamStrike:
                case CardId.AegisRam:
                case CardId.PrismEcho:
                case CardId.FrostLance:
                case CardId.CounterPursuit:
                case CardId.GhostProtocol:
                case CardId.ReserveShot:
                    combatFx = CombatFx.Shot;
                    combatFxPower = AttackFxPower(id);
                    combatFxDuration = AttackFxDuration(id);
                    combatFxText = $"{CardLibrary.Get(id).Name}{(upgradedCard ? "+" : string.Empty)} // {damageDealt} HIT";
                    PlayAttackSound(id, damageDealt > 0);
                    TriggerShake(AttackShake(id), AttackShakeDuration(id));
                    if (!destroyedTarget)
                        TriggerFullScreenImpact(AttackScreenPower(id), AttackFxDuration(id), false);
                    break;
                case CardId.BroadsideVolley:
                case CardId.MeltdownBurst:
                case CardId.Scattershot:
                case CardId.MissileSwarm:
                case CardId.InterceptMine:
                    combatFx = CombatFx.Volley;
                    combatFxPower = AttackFxPower(id);
                    combatFxDuration = AttackFxDuration(id);
                    combatFxText = $"{CardLibrary.Get(id).Name}{(upgradedCard ? "+" : string.Empty)} // {damageDealt} TOTAL";
                    PlayAttackSound(id, damageDealt > 0);
                    TriggerShake(AttackShake(id), AttackShakeDuration(id));
                    if (!destroyedTarget)
                        TriggerFullScreenImpact(AttackScreenPower(id), AttackFxDuration(id), false);
                    break;
                case CardId.WindGuard:
                case CardId.ReactivePlating:
                case CardId.SignalScrambler:
                case CardId.AirBrake:
                case CardId.ReactiveSeal:
                case CardId.StandbyField:
                case CardId.ReserveRouting:
                    combatFx = CombatFx.Shield;
                    combatFxText = $"{CardLibrary.Get(id).Name} // {L("feedback.shield_online", "护盾在线")}";
                    PlaySound(shieldSound);
                    break;
                case CardId.BankUp:
                case CardId.BankDown:
                case CardId.VectorDash:
                case CardId.EyeTransit:
                case CardId.RelayStep:
                    combatFx = CombatFx.Maneuver;
                    combatFxDuration = 0.72f;
                    combatFxText = L("feedback.lane_locked", "航道锁定 // {0}", battle.PlayerLane + 1);
                    if (selectedContract == CargoContract.StormCore)
                    {
                        bannerText = L("feedback.storm_core_safe", "CONTRACT SAFE // 风暴核心稳定计时已重置");
                        bannerUntil = Time.time + 1.05f;
                    }
                    break;
                case CardId.EmergencyCoolant:
                case CardId.CryoPump:
                case CardId.PhaseExchange:
                    combatFx = CombatFx.Coolant;
                    combatFxText = id == CardId.PhaseExchange
                        ? L("feedback.phase_exchange", "相变循环 // 废热转化为手牌")
                        : id == CardId.CryoPump ? L("feedback.cryo_cycle", "低温循环 // 废热回收")
                        : L("feedback.heat_down", "热量下降");
                    PlaySound(shieldSound, 0.75f);
                    if (selectedContract == CargoContract.CryoSerum)
                    {
                        bannerText = L("feedback.cryo_safe", "CONTRACT SAFE // 血清温度恢复安全范围");
                        bannerUntil = Time.time + 1.05f;
                    }
                    break;
                case CardId.EngineOverclock:
                case CardId.HeatCharge:
                case CardId.TargetLock:
                case CardId.LockCascade:
                case CardId.ZeroPointCalibration:
                case CardId.RedlineIgnition:
                case CardId.SwarmBeacon:
                case CardId.FalseTelemetry:
                case CardId.TightSchedule:
                    combatFx = CombatFx.Overclock;
                    combatFxText = id == CardId.FalseTelemetry
                        ? L("feedback.false_trace", "FALSE TRACE // 暴露 ×{0}", battle.EvasionExposure)
                        : id == CardId.TargetLock
                        ? $"TARGET LOCK ×{battle.LockOn}"
                        : id == CardId.HeatCharge ? L("feedback.heat_charge", "HEAT CHARGE // 强制供能")
                        : L("feedback.energy_gain", "+{0} 能量", upgradedCard ? 2 : 1);
                    PlaySound(clickSound, 1.35f);
                    bannerText = id == CardId.FalseTelemetry
                        ? L("feedback.decoy_burst", "DECOY BURST // 伪造航迹注入")
                        : L("feedback.overdrive", "OVERDRIVE // 能量回路突破");
                    bannerUntil = Time.time + 0.85f;
                    break;
            }

            if (!destroyedTarget && !string.IsNullOrEmpty(battle.LastModuleProc))
            {
                bannerText = "MODULE PROC // " + (LocalizationService.IsEnglish
                    ? L("feedback.module_triggered", "模块已触发") : battle.LastModuleProc);
                bannerUntil = Time.time + 1.05f;
                PlaySound(rewardSound, 1.28f, 0.55f);
                TriggerShake(6f, 0.24f);
            }

            if (!destroyedTarget && interruptedCharge)
            {
                bannerText = L("feedback.charge_break", "CHARGE BREAK // 灾变蓄力已中断");
                bannerUntil = Time.time + 1.15f;
                PlayLayeredSound(shieldSound, 1.35f, 0.72f, impactSound, 1.1f, 0.55f);
                TriggerShake(12f, 0.4f);
                TriggerFullScreenImpact(1.1f, 0.62f, false);
            }

            if (!destroyedTarget && !string.IsNullOrEmpty(battle.LastArmorBreak))
            {
                bannerText = "ARMOR BREAK // " + (LocalizationService.IsEnglish
                    ? L("feedback.enemy_core_exposed", "敌方核心暴露") : battle.LastArmorBreak);
                bannerUntil = Time.time + 1.05f;
                combatFxText = L("feedback.armor_broken", "装甲破裂 // 核心暴露");
                PlayLayeredSound(impactSound, 0.72f, 0.86f, destructionSound, 1.35f, 0.28f);
                TriggerShake(17f, 0.46f);
                StartCoroutine(DelayedHitStop(0.3f, 0.105f));
                TriggerFullScreenImpact(1.35f, 0.64f, false);
            }
            else if (!destroyedTarget && battle.LastAttackCritical)
            {
                bannerText = L("feedback.critical", "CRITICAL // 弱点贯穿");
                bannerUntil = Time.time + 0.95f;
                combatFxText = $"CRITICAL // {damageDealt} DAMAGE";
                PlayLayeredSound(heavyShotSound, 1.2f, 0.72f, impactSound, 0.82f, 0.8f);
                TriggerShake(14f, 0.38f);
                StartCoroutine(DelayedHitStop(0.24f, 0.09f));
            }
            else if (!destroyedTarget && !string.IsNullOrEmpty(battle.LastStatusTrigger))
            {
                bannerText = "STATUS PROC // " + (LocalizationService.IsEnglish
                    ? L("feedback.status_triggered", "状态规则已触发") : battle.LastStatusTrigger);
                bannerUntil = Time.time + 0.92f;
                PlaySound(rewardSound, 1.22f, 0.42f);
            }
            if (!destroyedTarget && battle.ContractPassiveTriggered)
            {
                bannerText = $"CONTRACT PROC // {ContractPassiveName(selectedContract)}";
                bannerUntil = Time.time + 1.08f;
                PlaySound(rewardSound, 1.34f, 0.56f);
                TriggerShake(6f, 0.24f);
            }
            if (!destroyedTarget && bossPhaseTransition)
            {
                bannerText = "BOSS MATRIX // " + (LocalizationService.IsEnglish
                    ? L("feedback.boss_counter", "首领反制已触发") : battle.LastStatusTrigger);
                bannerUntil = Time.time + 1.65f;
            }

            if (battle.PlayerHealth < hullBefore)
            {
                dangerFlashUntil = Time.time + 0.32f;
                TriggerShake(8f, 0.3f);
                PlaySound(warningSound);
            }

            bool hitSameLane = Enumerable.Range(0, battle.Enemies.Count).Any(i =>
                enemyHealthSnapshot[i] > battle.Enemies[i].Health && battle.Enemies[i].Lane == battle.PlayerLane);
            if (hitSameLane)
                TryShowTutorialOnce(TutorialTopic.LaneAttack);
            if (battle.Heat > heatBefore)
                TryShowTutorialOnce(TutorialTopic.Heat);
            if (maneuverCard && laneBefore != battle.PlayerLane)
            {
                TryShowTutorialOnce(TutorialTopic.LaneShift);
                if (battle.EvasionExposure >= 1)
                    TryShowTutorialOnce(TutorialTopic.Tracking);
            }

            if (battle.Victory)
                PlaySound(rewardSound);
        }

        private bool CanPlayInteractive(int handIndex)
        {
            return Time.time >= battleInputLockUntil && !battle.Victory && !battle.Defeat && battle.CanPlay(handIndex);
        }

        private void EndTurnWithFeedback()
        {
            if (Time.time < battleInputLockUntil || battle.Victory || battle.Defeat)
                return;
            battleInputLockUntil = Time.time + 0.92f;
            int hullBefore = battle.PlayerHealth;
            int cargoBefore = battle.CargoIntegrity;
            int[] enemyLaneBefore = new int[battle.Enemies.Count];
            var pendingAttacks = new List<EnemyAttackFx>();
            bool trackingQueued = false;
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                EnemyState enemy = battle.Enemies[i];
                enemyLaneBefore[i] = enemy.Lane;
                if (!enemy.Alive)
                    continue;
                bool tracking = enemy.Kind == EnemyKind.RustKite && battle.ChangedLaneThisTurn &&
                    battle.EvasionExposure >= 1;
                bool attacks = IntentCreatesAttackFx(enemy, tracking && !trackingQueued);
                bool chargeEnemy = enemy.Kind == EnemyKind.CalamityDrone ||
                    enemy.Kind == EnemyKind.StormManta || enemy.Kind == EnemyKind.CloudWyrm ||
                    enemy.Kind == EnemyKind.CurtainHerald || enemy.Kind == EnemyKind.FluxSkimmer;
                if (chargeEnemy && (enemy.ChargeTargetLane < 0 || enemy.ChargeInterrupted ||
                    enemy.PhaseTransitionPending))
                    attacks = false;
                if (attacks)
                {
                    trackingQueued |= tracking;
                    bool chargedAttack = chargeEnemy;
                    int targetLane = chargedAttack ? enemy.ChargeTargetLane : battle.PlayerLane;
                    int laneDistance = Mathf.Abs(targetLane - battle.PlayerLane);
                    bool chargedHit = enemy.Kind == EnemyKind.StormManta
                        ? laneDistance == 0 || (enemy.Phase == 2 && laneDistance == 1)
                        : enemy.Kind == EnemyKind.CloudWyrm
                            ? targetLane != battle.PlayerLane
                            : enemy.Kind == EnemyKind.CurtainHerald
                                ? targetLane != battle.PlayerLane
                            : enemy.Kind == EnemyKind.FluxSkimmer
                                ? laneDistance <= 1
                            : targetLane == battle.PlayerLane;
                    pendingAttacks.Add(new EnemyAttackFx
                    {
                        Position = EnemyBasePosition(i, enemy),
                        StartTime = Time.time,
                        Kind = enemy.Kind,
                        Hit = !chargedAttack || chargedHit,
                        Damage = tracking ? BattleState.TrackingShotDamage : enemy.Kind == EnemyKind.StormManta
                            ? enemy.Phase == 1 ? BattleState.BossPhaseOneStrikeDamage : BattleState.BossPhaseTwoStrikeDamage
                            : enemy.Kind == EnemyKind.CloudWyrm
                                ? enemy.Phase == 1 ? BattleState.CloudWyrmPhaseOneStrikeDamage :
                                    BattleState.CloudWyrmPhaseTwoStrikeDamage
                            : enemy.Kind == EnemyKind.MailEater ? enemy.Damage + 2
                            : enemy.Kind == EnemyKind.ShieldLeech ? 0 : enemy.Damage,
                        TargetLane = targetLane,
                        Seed = battle.Turn * 29 + i * 11
                    });
                }
            }
            battle.EndTurn();
            enemyAttackFx.AddRange(pendingAttacks);
            int shiftedEnemies = 0;
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                EnemyState enemy = battle.Enemies[i];
                if (!enemy.Alive || enemyLaneBefore[i] == enemy.Lane)
                    continue;
                enemyLaneFx.RemoveAll(fx => fx.Enemy == enemy);
                enemyLaneFx.Add(new EnemyLaneFx
                {
                    Enemy = enemy,
                    FromLane = enemyLaneBefore[i],
                    ToLane = enemy.Lane,
                    StartTime = Time.time + 0.08f + shiftedEnemies * 0.035f,
                    Duration = 0.7f,
                    Seed = battle.Turn * 31 + i * 17
                });
                shiftedEnemies++;
            }
            if (shiftedEnemies > 0)
            {
                PlaySound(maneuverSound, 0.76f, 0.72f);
                StartCoroutine(PlayLaneArrivalSound(0.55f, 0.82f));
                TriggerShake(5f, 0.3f);
            }
            bool anyAttackHit = pendingAttacks.Exists(fx => fx.Hit);
            combatFx = anyAttackHit ? CombatFx.EnemyHit : CombatFx.None;
            combatFxStart = Time.time;
            combatFxDuration = pendingAttacks.Count > 0 ? 0.92f : 0.46f;
            int receivedDamage = hullBefore - battle.PlayerHealth;
            int blockedDamage = battle.LastShieldAbsorbed;
            bool cargoDamaged = battle.CargoIntegrity < cargoBefore;
            combatFxText = cargoDamaged ? L("feedback.cargo_lost", "合同完整度 -1 // {0}", CargoStatus(battle.CargoIntegrity))
                : receivedDamage > 0 ? L("feedback.hull_lost", "-{0} 机体", receivedDamage)
                : blockedDamage > 0 ? $"SHIELD ABSORB {blockedDamage}"
                : !string.IsNullOrEmpty(battle.LastStatusTrigger) ? (LocalizationService.IsEnglish
                    ? L("feedback.status_triggered", "状态规则已触发") : battle.LastStatusTrigger)
                : shiftedEnemies > 0 ? L("feedback.enemy_shift", "敌方变轨 ×{0}", shiftedEnemies)
                : L("feedback.evaded", "成功规避");
            bannerText = battle.PlayerHealth < hullBefore ? L("feedback.hull_damaged", "DANGER // 机体受损")
                : battle.LastShieldBroken ? L("feedback.shield_broken", "SHIELD BREAK // 护盾耗尽")
                : blockedDamage > 0 ? L("feedback.absorbed", "ABSORBED // 护盾完全抵消")
                : shiftedEnemies > 0 ? L("feedback.hostile_shift", "HOSTILE SHIFT // 敌方切换航道")
                : L("feedback.perfect_evade", "EVADE // 完美规避");
            bannerUntil = Time.time + 0.72f;
            if (cargoDamaged)
            {
                TryShowTutorialOnce(TutorialTopic.Cargo);
                bannerText = "CARGO BREACH // " + (LocalizationService.IsEnglish
                    ? L("feedback.cargo_rule_triggered", "合同风险已触发") : battle.LastCargoDamageReason);
                bannerUntil = Time.time + 1.25f;
                dangerFlashUntil = Time.time + 0.5f;
                impactFlashUntil = Time.time + 0.48f;
                TriggerShake(13f, 0.44f);
                PlayLayeredSound(warningSound, 0.72f, 0.82f, impactSound, 0.64f, 0.52f);
                TriggerFullScreenImpact(0.9f, 0.58f, false);
            }
            else if (battle.ContractPassiveTriggered)
            {
                bannerText = $"CONTRACT PROC // {ContractPassiveName(selectedContract)}";
                bannerUntil = Time.time + 1.08f;
                PlaySound(rewardSound, 1.34f, 0.56f);
            }
            else if (!string.IsNullOrEmpty(battle.LastStatusTrigger))
            {
                bannerText = "COUNTERMEASURE // " + (LocalizationService.IsEnglish
                    ? L("feedback.status_triggered", "状态规则已触发") : battle.LastStatusTrigger);
                bannerUntil = Time.time + 1.08f;
                PlaySound(warningSound, 1.16f, 0.62f);
            }
            if (battle.PlayerHealth < hullBefore)
            {
                dangerFlashUntil = Time.time + 0.35f;
                TriggerShake(10f, 0.32f);
                PlaySound(warningSound, 0.85f);
            }
            else if (blockedDamage > 0)
            {
                impactFlashUntil = Time.time + 0.28f;
                TriggerShake(battle.LastShieldBroken ? 9f : 4f, 0.28f);
                PlayLayeredSound(shieldSound, battle.LastShieldBroken ? 0.7f : 1.1f, 0.72f,
                    impactSound, battle.LastShieldBroken ? 0.82f : 1.25f, 0.4f);
                if (battle.LastShieldBroken)
                    StartCoroutine(DelayedHitStop(0.18f, 0.055f));
            }
            else
            {
                PlaySound(clickSound, 0.8f);
            }
        }

        private bool IntentCreatesAttackFx(EnemyState enemy, bool tracking)
        {
            if (tracking)
                return true;
            if (enemy.Kind == EnemyKind.CalamityDrone || enemy.Kind == EnemyKind.StormManta ||
                enemy.Kind == EnemyKind.CloudWyrm || enemy.Kind == EnemyKind.CurtainHerald ||
                enemy.Kind == EnemyKind.FluxSkimmer)
                return true;
            if (enemy.Kind == EnemyKind.StormBalloon)
                return true;
            if (enemy.Kind == EnemyKind.ShieldLeech)
                return battle.Armor >= 5;
            if (enemy.Kind == EnemyKind.HandJammer)
                return battle.Hand.Count >= 5;
            if (enemy.Kind == EnemyKind.HeatSeeker)
                return battle.Heat >= 4;
            if (enemy.Kind == EnemyKind.SignalHijacker)
                return false;
            return enemy.Lane == battle.PlayerLane;
        }

        private void DrawRouteMap()
        {
            RunAct currentAct = RunStructureCatalog.ActForColumn(routeIndex);
            DrawRect(new Rect(70, 58, 1460, 760), new Color32(2, 7, 22, 255));
            DrawRect(new Rect(78, 66, 1444, 744), PanelNight);
            DrawNeonFrame(new Rect(78, 66, 1444, 744), NeonCyan, 3f);
            DrawFittedLabel(new Rect(125, 88, 540, 64),
                L("map.title", "风车群岛分支航线"), neonTitleStyle, 30);
            DrawRunActIndicator(currentAct);
            DrawFittedLabel(new Rect(128, 147, 830, 38),
                L("map.act_status", "第 {0} 幕 / 3 · {1}　//　区域 {2}/{3}　//　信标纪事：{4}",
                    RunStructureCatalog.ActNumber(currentAct), RunActName(currentAct),
                    routeIndex + 1, route.ColumnCount, RouteStoryStatus()), neonBodyStyle, 10);
            DrawRunHud(new Rect(1050, 88, 405, 96));

            Rect viewport = new Rect(120, 205, 1360, 405);
            const float columnSpacing = 250f;
            const float contentPadding = 145f;
            float contentWidth = contentPadding * 2f + route.ColumnCount * columnSpacing;
            int revealThrough = Mathf.Min(route.ColumnCount - 1, routeIndex + 2);
            float knownContentRight = contentPadding + revealThrough * columnSpacing + 125f;
            float maxScroll = Mathf.Min(Mathf.Max(0f, contentWidth - viewport.width),
                Mathf.Max(0f, knownContentRight - viewport.width));
            routeScroll = Mathf.Clamp(routeScroll, 0f, maxScroll);
            if (viewport.Contains(Event.current.mousePosition) && Event.current.type == EventType.ScrollWheel)
            {
                routeScroll = Mathf.Clamp(routeScroll + Event.current.delta.y * 72f, 0f, maxScroll);
                Event.current.Use();
            }

            DrawRect(viewport, new Color32(3, 11, 29, 248));
            DrawNeonFrame(viewport, new Color32(51, 111, 146, 255), 2f);
            GUI.BeginGroup(new Rect(viewport.x + 3, viewport.y + 3, viewport.width - 6, viewport.height - 6));
            float offsetX = contentPadding - routeScroll;

            for (int lane = 0; lane < 3; lane++)
            {
                AirspaceCondition condition = lane switch
                {
                    0 => AirspaceCondition.JetstreamCorridor,
                    1 => AirspaceCondition.StaticFront,
                    _ => AirspaceCondition.WreckageTide
                };
                Color bandColor = AirspaceColor(condition);
                float centerY = 64f + lane * 122f;
                DrawRect(new Rect(0, centerY - 49f, contentWidth + 500f, 98f),
                    new Color(bandColor.r, bandColor.g, bandColor.b, 0.055f));
            }

            for (int column = 0; column <= revealThrough; column++)
            {
                float markerX = offsetX + column * columnSpacing;
                Color marker = column == routeIndex ? NeonCyan : new Color32(61, 78, 111, 150);
                DrawRect(new Rect(markerX - 1f, 22f, 2f, 355f), new Color(marker.r, marker.g, marker.b, 0.16f));
                DrawRect(new Rect(markerX - 23f, 8f, 46f, 20f), new Color32(5, 14, 35, 245));
                DrawPixelOutline(new Rect(markerX - 23f, 8f, 46f, 20f), marker, 2f);
                DrawFittedLabel(new Rect(markerX - 20f, 8f, 40f, 20f), $"{column + 1:00}", tinyStyle, 9);
            }

            foreach (RouteNodeDefinition from in route.Nodes)
            {
                if (from.Column > revealThrough)
                    continue;
                Vector2 start = RouteNodeCenter(from, offsetX, columnSpacing);
                foreach (int nextId in from.Next)
                {
                    RouteNodeDefinition to = route.Get(nextId);
                    if (to.Column > revealThrough)
                        continue;
                    Vector2 end = RouteNodeCenter(to, offsetX, columnSpacing);
                    bool travelled = completedRouteNodes.Contains(from.Id) &&
                        (completedRouteNodes.Contains(to.Id) || to.Id == selectedRouteNodeId);
                    bool reachable = from.Id == lastCompletedRouteNodeId && IsRouteNodeAvailable(to);
                    Color line = travelled ? new Color32(79, 225, 164, 245) :
                        reachable ? new Color32(95, 219, 235, 225) : new Color32(57, 72, 104, 170);
                    DrawMapConnection(start, end, new Color32(1, 5, 17, 220), travelled || reachable ? 7f : 5f);
                    DrawMapConnection(start, end, line, travelled || reachable ? 4f : 2f);
                }
            }

            foreach (RouteNodeDefinition node in route.Nodes)
                DrawRouteNode(node, offsetX, columnSpacing, revealThrough);

            float fogX = offsetX + (revealThrough + 0.5f) * columnSpacing;
            if (revealThrough < route.ColumnCount - 1)
            {
                DrawRect(new Rect(fogX, 0, contentWidth + 500f, 399), new Color32(5, 9, 28, 248));
                DrawRect(new Rect(fogX, 0, 4, 399), NeonViolet);
                for (int stripe = 0; stripe < 7; stripe++)
                    DrawRect(new Rect(fogX + 18 + stripe * 54f, 0, 2, 399), new Color32(180, 91, 255, 18));
                DrawFittedLabel(new Rect(fogX + 28, 164, 270, 58), "SIGNAL LOST\n航路情报未解析", hudCenteredStyle, 10);
            }

            for (int lane = 0; lane < 3; lane++)
            {
                AirspaceCondition condition = lane switch
                {
                    0 => AirspaceCondition.JetstreamCorridor,
                    1 => AirspaceCondition.StaticFront,
                    _ => AirspaceCondition.WreckageTide
                };
                Rect labelRect = RouteBandLabelRect(lane);
                Color bandColor = AirspaceColor(condition);
                DrawRect(labelRect, new Color32(4, 12, 30, 238));
                DrawPixelOutline(labelRect, bandColor, 1f);
                DrawFittedLabel(new Rect(labelRect.x + 3f, labelRect.y, labelRect.width - 6f, labelRect.height),
                    $"{AirspaceRuleCatalog.Band(condition)}·{AirspaceRuleCatalog.Name(condition)}", tinyStyle, 7);
            }
            GUI.EndGroup();

            routeScroll = GUI.HorizontalScrollbar(new Rect(250, 625, 1100, 22), routeScroll,
                viewport.width, 0f, maxScroll + viewport.width);
            DrawPixelButton(new Rect(145, 617, 72, 42), "<", Shadow,
                () => routeScroll = Mathf.Clamp(routeScroll - 380f, 0f, maxScroll), routeScroll > 1f);
            DrawPixelButton(new Rect(1383, 617, 72, 42), ">", Shadow,
                () => routeScroll = Mathf.Clamp(routeScroll + 380f, 0f, maxScroll), routeScroll < maxScroll - 1f);

            RouteNodeDefinition selected = route.Get(selectedRouteNodeId);
            Color selectedColor = selected.Id == 19
                ? new Color32(73, 211, 220, 255)
                : RouteNodeColor(selected.Kind);
            Rect detail = new Rect(190, 674, 835, 96);
            DrawRect(detail, new Color32(5, 14, 35, 245));
            DrawRect(new Rect(detail.x, detail.y, 8, detail.height), selectedColor);
            DrawRect(new Rect(detail.x + 25, detail.y + 12, 116, 26), new Color32(9, 27, 55, 250));
            DrawPixelOutline(new Rect(detail.x + 25, detail.y + 12, 116, 26), selectedColor, 2f);
            DrawFittedLabel(new Rect(detail.x + 29, detail.y + 12, 108, 26), RouteNodeKindLabel(selected.Kind), tinyStyle, 9);
            Color airspaceColor = AirspaceColor(selected.Airspace);
            DrawRect(new Rect(detail.x + 151, detail.y + 12, 166, 26), new Color32(9, 27, 55, 250));
            DrawPixelOutline(new Rect(detail.x + 151, detail.y + 12, 166, 26), airspaceColor, 2f);
            DrawFittedLabel(new Rect(detail.x + 155, detail.y + 12, 158, 26),
                $"{AirspaceRuleCatalog.Band(selected.Airspace)} // {AirspaceRuleCatalog.Name(selected.Airspace)}",
                tinyStyle, 8);
            Color riskColor = RouteRiskColor(selected.Kind);
            DrawRect(new Rect(detail.x + 327, detail.y + 12, 118, 26), new Color32(9, 27, 55, 250));
            DrawPixelOutline(new Rect(detail.x + 327, detail.y + 12, 118, 26), riskColor, 2f);
            DrawFittedLabel(new Rect(detail.x + 331, detail.y + 12, 110, 26),
                RouteRiskLabel(selected.Kind), tinyStyle, 8);
            string selectedTitle = selected.Kind == RouteNodeKind.Event ? EventTitleForNode(selected.Id) : selected.Title;
            string selectedDescription = selected.Kind == RouteNodeKind.Event
                ? EventDescriptionForNode(selected.Id) : selected.Description;
            DrawFittedLabel(new Rect(detail.x + 465, detail.y + 7, detail.width - 490, 38),
                selectedTitle, neonSubtitleStyle, 13);
            DrawFittedLabel(new Rect(detail.x + 30, detail.y + 45, detail.width - 55, 45),
                L("map.node_detail", "{0}\n预期产出：{1}　//　空域：{2}",
                    selectedDescription, RouteExpectedReward(selected.Kind),
                    AirspaceRuleCatalog.EncounterRule(selected.Airspace)),
                neonBodyStyle, 9);
            string enterLabel = selected.Kind == RouteNodeKind.Boss
                ? L("map.enter_boss", "挑战首领")
                : L("map.enter_node", "前往 {0}", selectedTitle);
            DrawPixelButton(new Rect(1060, 683, 350, 72), enterLabel, selectedColor, EnterCurrentNode,
                IsRouteNodeAvailable(selected), "ENTER");
            DrawFittedLabel(new Rect(1080, 762, 310, 28),
                L("map.scroll_help", "滚轮 / 航标拖动浏览路线"), tinyStyle, 9);
        }

        private void DrawRunActIndicator(RunAct currentAct)
        {
            RunAct[] acts = { RunAct.Departure, RunAct.Pivot, RunAct.FinalApproach };
            for (int i = 0; i < acts.Length; i++)
            {
                RunAct act = acts[i];
                bool active = act == currentAct;
                bool completed = (int)act < (int)currentAct;
                Color color = active ? NeonCyan : completed
                    ? new Color32(77, 220, 159, 255)
                    : new Color32(65, 82, 112, 255);
                Rect rect = new Rect(690 + i * 112, 103, 102, 34);
                DrawRect(rect, active ? new Color32(8, 39, 64, 250) : new Color32(5, 17, 37, 238));
                DrawPixelOutline(rect, color, active ? 3f : 1f);
                DrawFittedLabel(new Rect(rect.x + 5, rect.y + 3, rect.width - 10, rect.height - 6),
                    $"{i + 1} // {RunActName(act)}", tinyStyle, 7);
            }
        }

        private static string RunActName(RunAct act)
        {
            return act switch
            {
                RunAct.Pivot => L("run_act.pivot", "改装转折"),
                RunAct.FinalApproach => L("run_act.final", "终局进场"),
                _ => L("run_act.departure", "离港构筑")
            };
        }

        private static string RouteRiskLabel(RouteNodeKind kind)
        {
            if (kind == RouteNodeKind.Rest || kind == RouteNodeKind.Shop)
                return L("map.risk.low", "低风险");
            return kind switch
            {
                RouteNodeKind.Event => L("map.risk.variable", "风险交易"),
                RouteNodeKind.Skirmish => L("map.risk.standard", "标准威胁"),
                RouteNodeKind.Hunt => L("map.risk.high", "高风险"),
                RouteNodeKind.Elite => L("map.risk.severe", "严重威胁"),
                _ => L("map.risk.extreme", "终局威胁")
            };
        }

        private static Color RouteRiskColor(RouteNodeKind kind)
        {
            if (kind == RouteNodeKind.Rest || kind == RouteNodeKind.Shop)
                return new Color32(78, 211, 174, 255);
            return kind switch
            {
                RouteNodeKind.Event => Gold,
                RouteNodeKind.Skirmish => NeonCyan,
                RouteNodeKind.Hunt => NeonViolet,
                _ => PostalRed
            };
        }

        private static string RouteExpectedReward(RouteNodeKind kind)
        {
            return kind switch
            {
                RouteNodeKind.Shop => L("map.reward.shop", "购买卡牌、模块与维修"),
                RouteNodeKind.Event => L("map.reward.event", "纪事分支与资源交易"),
                RouteNodeKind.Rest => L("map.reward.rest", "维修、强化或删牌"),
                RouteNodeKind.Elite => L("map.reward.elite", "稀有模块与高额邮票"),
                RouteNodeKind.Hunt => L("map.reward.hunt", "卡牌与追猎报酬"),
                RouteNodeKind.Boss => L("map.reward.boss", "终局结局与合同精通"),
                _ => L("map.reward.skirmish", "卡牌与邮票")
            };
        }

        public static Rect RouteBandLabelRect(int lane)
        {
            int safeLane = Mathf.Clamp(lane, 0, 2);
            return new Rect(7f, 64f + safeLane * 122f - 43f, 86f, 20f);
        }

        private void DrawRewardScreen()
        {
            bool moduleReward = battle.Encounter == EncounterId.Elite;
            DrawRect(new Rect(160, 80, 1280, 730), new Color32(2, 7, 22, 255));
            DrawRect(new Rect(168, 88, 1264, 714), PanelNight);
            DrawNeonFrame(new Rect(168, 88, 1264, 714), NeonViolet, 3f);
            DrawFittedLabel(new Rect(230, 118, 900, 70), moduleReward ? "稀有模块协议已解密" : "战利品协议已解密", neonTitleStyle, 28);
            DrawFittedLabel(new Rect(235, 188, 900, 45), moduleReward
                ? "选择一枚永久生效的金色模块；它会改变最后一战的行动规则。"
                : "从三张不同指令中选择一张；每组奖励都包含伤害牌与支援牌。", neonBodyStyle, 12);
            DrawFittedLabel(new Rect(1030, 125, 345, 38), $"报酬 +{lastRewardCredits}　|　维护 +{lastFieldRepair} 机体", neonSubtitleStyle, 10);
            DrawFittedLabel(new Rect(1080, 170, 300, 32), $"牌组 {runDeck.Count}　强化 {runUpgrades.Count}　模块 {runModules.Count}", hudCenteredStyle, 8);
            DrawBattleDebrief(new Rect(350, 238, 900, 34));

            RewardChoice[] rewards = CurrentRewardChoices();

            for (int i = 0; i < rewards.Length; i++)
            {
                float reveal = Mathf.Clamp01((Time.time - rewardEnteredAt - i * 0.13f) / 0.3f);
                if (reveal <= 0.01f)
                    continue;
                RewardChoice reward = rewards[i];
                Rect cardRect = new Rect(260 + i * 360, 285 + (1f - reveal) * 58f, 280, 345);
                Color oldColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, reveal);
                bool selected = selectedRewardIndex == i;
                if (selected)
                    DrawNeonFrame(new Rect(cardRect.x - 12, cardRect.y - 12, cardRect.width + 24, cardRect.height + 24), Color.white, 5f);
                int rewardIndex = i;
                DrawRewardChoice(reward, cardRect, selected, rewardIndex);
                GUI.color = oldColor;
            }

            DrawPixelButton(new Rect(610, 690, 380, 62), moduleReward ? "回收模块，获得25枚邮票" : "跳过奖励，获得15枚邮票", Shadow,
                SkipReward, !rewardSelectionLocked, "X");

            if (Time.time < rewardConfirmUntil && !string.IsNullOrEmpty(selectedRewardName))
            {
                float pulse = 1f + Mathf.Sin(Time.time * 24f) * 0.04f;
                Rect confirmed = new Rect(430 - (pulse - 1f) * 260f, 390 - (pulse - 1f) * 50f, 740 * pulse, 100 * pulse);
                DrawRect(confirmed, new Color32(5, 13, 34, 246));
                DrawNeonFrame(confirmed, moduleReward ? new Color32(255, 204, 74, 255) : NeonCyan, 4f);
                DrawFittedLabel(confirmed, $"SYSTEM INSTALLED // {selectedRewardName}", hudCenteredStyle, 9);
            }

            DrawFullScreenImpact();
        }

        private RewardChoice[] CurrentRewardChoices()
        {
            if (battle.Encounter == EncounterId.Elite)
            {
                ModuleId[] modules = selectedContract switch
                {
                    CargoContract.CryoSerum => routeIndex < 4
                        ? new[] { ModuleId.ZeroPointReactor, ModuleId.CryoHeart, ModuleId.RedlineReactor }
                        : new[] { ModuleId.RedlineReactor, ModuleId.ZeroPointReactor, ModuleId.ExecutionChip },
                    CargoContract.StormCore => routeIndex < 4
                        ? new[] { ModuleId.MomentumFlywheel, ModuleId.VectorThruster, ModuleId.SwarmUplink }
                        : new[] { ModuleId.SwarmUplink, ModuleId.MomentumFlywheel, ModuleId.ExecutionChip },
                    CargoContract.BlackBoxRelay => new[] { ModuleId.GhostDecoder, ModuleId.VectorThruster, ModuleId.ExecutionChip },
                    CargoContract.SignalSeed => routeIndex < 4
                        ? new[] { ModuleId.PrecisionMatrix, ModuleId.ZeroPointReactor, ModuleId.ExecutionChip }
                        : new[] { ModuleId.ExecutionChip, ModuleId.PrecisionMatrix, ModuleId.AegisCapacitor },
                    _ => routeIndex < 4
                        ? new[] { ModuleId.AegisCapacitor, ModuleId.PrismBulkhead, ModuleId.PrecisionMatrix }
                        : new[] { ModuleId.PrecisionMatrix, ModuleId.AegisCapacitor, ModuleId.ExecutionChip }
                };
                return new[]
                {
                    new RewardChoice { Kind = RewardKind.Module, Module = modules[0] },
                    new RewardChoice { Kind = RewardKind.Module, Module = modules[1] },
                    new RewardChoice { Kind = RewardKind.Module, Module = modules[2] }
                };
            }

            CardId signatureCard = ContractCardCatalog.SignatureCard(selectedContract);
            CardId? guaranteed = runDeck.Contains(signatureCard) ? null : signatureCard;
            int offerSeed = runSeed ^ selectedRouteNodeId * 73856093 ^ routeIndex * 19349663;
            CardId[] cards = CardOfferCatalog.Select(selectedContract, CurrentAirspace(), offerSeed, 3, runDeck,
                guaranteed);
            return cards.Select(card => new RewardChoice { Kind = RewardKind.AddCard, Card = card }).ToArray();
        }

        private static CardId AirspacePreferredRewardCard(CargoContract contract, AirspaceCondition condition)
        {
            return contract switch
            {
                CargoContract.CryoSerum => condition switch
                {
                    AirspaceCondition.JetstreamCorridor => CardId.ZeroPointCalibration,
                    AirspaceCondition.StaticFront => CardId.FrostLance,
                    _ => CardId.RedlineIgnition
                },
                CargoContract.StormCore => condition switch
                {
                    AirspaceCondition.JetstreamCorridor => CardId.SlipstreamStrike,
                    AirspaceCondition.StaticFront => CardId.SwarmBeacon,
                    _ => CardId.MissileSwarm
                },
                CargoContract.BlackBoxRelay => condition switch
                {
                    AirspaceCondition.JetstreamCorridor => CardId.AirBrake,
                    AirspaceCondition.StaticFront => CardId.GhostProtocol,
                    _ => CardId.CounterPursuit
                },
                CargoContract.SignalSeed => condition switch
                {
                    AirspaceCondition.JetstreamCorridor => CardId.RelayStep,
                    AirspaceCondition.StaticFront => CardId.TightSchedule,
                    _ => CardId.StandbyField
                },
                _ => condition switch
                {
                    AirspaceCondition.JetstreamCorridor => CardId.PrismEcho,
                    AirspaceCondition.StaticFront => CardId.LockCascade,
                    _ => CardId.AegisRam
                }
            };
        }

        private void DrawRewardChoice(RewardChoice reward, Rect rect, bool selected, int index)
        {
            bool enabled = !rewardSelectionLocked;
            Action choose = () => ChooseRewardChoice(reward, index);
            string shortcut = (index + 1).ToString();
            if (reward.Kind == RewardKind.Module)
            {
                DrawModuleOffer(reward.Module, rect, selected ? "模块已装载" : "安装稀有模块", enabled, choose, shortcut);
                return;
            }

            if (reward.Kind == RewardKind.UpgradeCard)
            {
                CardSpec card = CardLibrary.Get(reward.Card);
                string branchLabel = reward.Branch == UpgradeBranch.Alpha ? "A // 增幅分支" : "B // 机制分支";
                DrawOfferCard(reward.Card, rect, selected ? "分支已写入" : "选择升级分支", enabled, choose,
                    branchLabel, shortcut, $"{card.Name}+{(reward.Branch == UpgradeBranch.Alpha ? "A" : "B")}",
                    UpgradedRules(reward.Card, reward.Branch));
                return;
            }

            DrawOfferCard(reward.Card, rect, selected ? "卡牌已装载" : "加入牌组", enabled, choose,
                RewardSynergy(reward.Card), shortcut);
        }

        private string RewardSynergy(CardId card)
        {
            if (card == AirspacePreferredRewardCard(selectedContract, CurrentAirspace()))
                return $"{AirspaceRuleCatalog.Name(CurrentAirspace())}适配 · {AirspaceRuleCatalog.RewardRule(CurrentAirspace())}";
            if (ContractCardCatalog.BelongsTo(card, selectedContract))
                return "合同专属 · 改变核心资源循环";
            if (selectedContract == CargoContract.StormCore && (card == CardId.BankUp || card == CardId.BankDown))
                return "合同核心 · 重置稳定计时";
            if (selectedContract == CargoContract.CryoSerum && card == CardId.EmergencyCoolant)
                return "合同核心 · 避免高热货损";
            if (selectedContract == CargoContract.FragileMedicine && card == CardId.WindGuard)
                return "合同核心 · 抵消大额伤害";
            if (selectedContract == CargoContract.BlackBoxRelay &&
                (card == CardId.SignalScrambler || card == CardId.AirBrake))
                return "合同核心 · 控制航迹暴露";
            if (selectedContract == CargoContract.SignalSeed &&
                (card == CardId.ReserveShot || card == CardId.StandbyField ||
                 card == CardId.TightSchedule || card == CardId.RelayStep))
                return "合同核心 · 精确保留1点能量";
            return card switch
            {
                CardId.TargetLock => "锁定狙击 · 积累倍率",
                CardId.RailPiercer => "锁定狙击 · 消耗层数终结",
                CardId.VectorDash => "矢量追猎 · 换道积累动量",
                CardId.PursuitShot => "矢量追猎 · 动量转化伤害",
                CardId.ReactivePlating => "护盾冲角 · 储备护盾",
                CardId.AegisRam => "护盾冲角 · 护盾转化伤害",
                CardId.SignalScrambler => "航迹欺骗 · 清除暴露",
                CardId.CounterPursuit => "逆向追猎 · 暴露转化伤害",
                CardId.AirBrake => "航迹欺骗 · 降低暴露并回能",
                CardId.InterceptMine => "侧翼封锁 · 惩罚追击敌机",
                CardId.ReserveShot => "余量调度 · 保留能量增伤",
                CardId.StandbyField => "余量调度 · 保留能量增盾",
                CardId.TightSchedule => "指令循环 · 手牌与锁定",
                CardId.RelayStep => "余量调度 · 换道并清除航迹",
                CardId.ReserveRouting => "合同专属 · 余量转化为手牌",
                CardId.CryoPump => "零度循环 · 降温返还能量",
                CardId.FrostLance => "零度循环 · 低热高伤",
                CardId.HeatCharge => "熔炉爆发 · 主动积热",
                CardId.MeltdownBurst => "熔炉爆发 · 储热清场",
                CardId.Scattershot => "蜂群弹幕 · 低费铺场",
                CardId.MissileSwarm => "蜂群弹幕 · 随机连击",
                CardId.OverloadAim => "爆发强化 · 单体终结",
                CardId.BroadsideVolley => "火力强化 · 全场压制",
                CardId.EngineOverclock => "循环强化 · 扩展行动",
                CardId.BurstFire => "稳定输出 · 低费攻击",
                CardId.WindGuard => "生存强化 · 6点护盾",
                CardId.LockCascade => "锁定狙击 · 标定后协同射击",
                CardId.SlipstreamStrike => "矢量追猎 · 不消耗动量",
                CardId.PrismEcho => "护盾冲角 · 防御同步输出",
                CardId.ZeroPointCalibration => "零度循环 · 冷却后必定暴击",
                CardId.RedlineIgnition => "熔炉爆发 · 高热暴击窗口",
                CardId.SwarmBeacon => "蜂群弹幕 · 强化下一次齐射",
                CardId.GhostProtocol => "航迹欺骗 · 主动暴露换取爆发",
                CardId.ReactiveSeal => "合同专属 · 锁定转化为双倍密封",
                CardId.PhaseExchange => "合同专属 · 废热转化为额外手牌",
                CardId.EyeTransit => "合同专属 · 跨越航道积累动量",
                CardId.FalseTelemetry => "合同专属 · 主动暴露扩展循环",
                CardId.ThermalBarrier => "共享桥牌 · 热量转化为护盾",
                CardId.CapacitorDump => "共享桥牌 · 护盾转化为能量与锁定",
                CardId.KineticBroadside => "共享桥牌 · 动量强化齐射",
                CardId.TracerSwarm => "共享桥牌 · 锁定强化蜂群",
                CardId.QueueDirective => "共享桥牌 · 锁定转化为手牌",
                CardId.EmergencySort => "共享桥牌 · 整手重排并消耗",
                CardId.HoldFormation => "共享桥牌 · 承担暴露保留手牌",
                CardId.ArmorySearch => "共享桥牌 · 定向检索武器",
                _ => "机动强化 · 调整航道"
            };
        }

        private static string UpgradedRules(CardId card, UpgradeBranch branch = UpgradeBranch.Alpha)
        {
            if (branch == UpgradeBranch.Beta)
            {
                return card switch
                {
                    CardId.BankUp => "向上移动1条航道，获得3点护盾并降低1层航迹暴露。",
                    CardId.BankDown => "向下移动1条航道，获得3点护盾并降低1层航迹暴露。",
                    CardId.EmergencyCoolant => "降低5点热量，并使下一次攻击必定暴击。",
                    CardId.WindGuard => "获得6点护盾，并使下一次攻击必定暴击。",
                    CardId.EngineOverclock => "获得1点能量，并使下一次攻击必定暴击。",
                    CardId.TargetLock => "获得1层锁定，并使下一次攻击必定暴击。",
                    CardId.RailPiercer => "造成11点伤害；无视装甲，消耗锁定获得额外伤害。",
                    CardId.VectorDash => "切换航道，获得2点护盾与1层动量，并降低1层航迹暴露。",
                    CardId.PursuitShot => "造成7点加动量伤害；结算后保留1层动量。",
                    CardId.ReactivePlating => "获得7点护盾；已有5点护盾时额外获得1点能量。",
                    CardId.AegisRam => "造成护盾转化伤害，但只消耗一半当前护盾。",
                    CardId.CryoPump => "降低6点热量；有效回收废热后返还1点能量并使下一击暴击。",
                    CardId.HeatCharge => "获得2点能量并增加4点热量，使下一次攻击必定暴击。",
                    CardId.MeltdownBurst => "释放热量攻击全体，结算后保留2点热量。",
                    CardId.SignalScrambler => "清除航迹；每清除1层额外获得3点护盾。",
                    CardId.CounterPursuit => "暴露转化伤害后保留1层航迹暴露。",
                    CardId.AirBrake => "降低航迹并获得5点护盾，同时获得1层动量。",
                    CardId.ReserveShot => "造成11点伤害；保留1点能量时额外造成4点并获得1层锁定。",
                    _ => CardLibrary.Get(card).Rules + " 机制分支：触发后保留关键构筑资源。"
                };
            }
            return card switch
            {
                CardId.BurstFire => "对同航道首个敌人造成9点伤害。",
                CardId.BankUp => "向上移动1条航道，获得5点护盾。",
                CardId.BankDown => "向下移动1条航道，获得5点护盾。",
                CardId.EmergencyCoolant => "降低5点热量。",
                CardId.BroadsideVolley => "对所有敌人造成5点伤害。",
                CardId.WindGuard => "获得9点护盾。",
                CardId.OverloadAim => "造成13点伤害；已有4点热量时造成16点。",
                CardId.EngineOverclock => "获得2点能量。",
                CardId.TargetLock => "获得2层锁定；穿甲轨炮消耗锁定，每层额外造成6点伤害。",
                CardId.RailPiercer => "造成11点伤害；每层锁定额外造成6点伤害并消耗全部锁定。",
                CardId.VectorDash => "向下移动1条航道；已在最下方时改为向上。获得4点护盾与2层动量。",
                CardId.PursuitShot => "造成7点伤害；每层动量额外造成5点伤害并消耗全部动量。",
                CardId.ReactivePlating => "获得11点护盾，为护盾冲角储备伤害。",
                CardId.AegisRam => "造成6点伤害，并追加最多14点当前护盾值，然后清空护盾。",
                CardId.CryoPump => "降低6点热量；若实际降低至少3点，获得2点能量。",
                CardId.FrostLance => "造成9点伤害；出牌前热量不高于2时额外造成7点。",
                CardId.HeatCharge => "获得3点能量，同时增加4点热量。",
                CardId.MeltdownBurst => "对所有敌人造成3点加当前热量的伤害，然后清空热量。",
                CardId.Scattershot => "对所有敌人造成3点伤害。",
                CardId.MissileSwarm => "发射6枚飞弹，每枚对随机敌人造成2点伤害。",
                CardId.SignalScrambler => "清除全部航迹暴露，获得7点护盾。",
                CardId.CounterPursuit => "追踪最低耐久敌人造成9点伤害；每层航迹暴露额外造成8点，随后清除暴露。",
                CardId.AirBrake => "降低2层航迹暴露并获得8点护盾；若成功降低，获得1点能量。",
                CardId.InterceptMine => "对所有不同航道的敌人造成9点伤害。",
                CardId.ReserveShot => "对同航道造成11点伤害；保留1点能量时额外造成4点。",
                _ => CardLibrary.Get(card).Rules
            };
        }

        private void DrawRetrofitScreen()
        {
            DrawRect(new Rect(70, 48, 1460, 805), new Color32(2, 7, 22, 250));
            DrawRect(new Rect(78, 56, 1444, 789), PanelNight);
            DrawNeonFrame(new Rect(78, 56, 1444, 789), Gold, 3f);
            DrawFittedLabel(new Rect(125, 82, 850, 62),
                L("retrofit.title", "永久机体改装"), neonTitleStyle, 26);
            DrawFittedLabel(new Rect(1040, 92, 390, 35),
                L("retrofit.header", "MID-RUN AIRFRAME REWRITE"), hudCenteredStyle, 8);
            DrawFittedLabel(new Rect(130, 146, 1340, 42),
                L("retrofit.subtitle", "航线已进入后半程。选择一项改装焊入机体：本局不可拆除，也不能跳过。"),
                neonBodyStyle, 12);

            Rect warning = new Rect(305, 205, 990, 42);
            DrawRect(warning, new Color32(72, 35, 18, 235));
            DrawPixelOutline(warning, Gold, 2f);
            DrawFittedLabel(new Rect(warning.x + 18, warning.y + 7, warning.width - 36, 28),
                L("retrofit.warning", "IRREVERSIBLE // 确认后将改变之后每场战斗的基础规则"), hudCenteredStyle, 9);

            AirframeModification[] options =
            {
                AirframeModification.SealedBulkhead,
                AirframeModification.OpenAvionics,
                AirframeModification.RedlineTurbine
            };
            for (int i = 0; i < options.Length; i++)
                DrawRetrofitCard(options[i], new Rect(165 + i * 425, 270, 390, 430), i);

            DrawFittedLabel(new Rect(390, 735, 820, 30),
                L("retrofit.contract", "当前合同 // {0}　专属牌与改装规则可以交叉组合", CargoName(selectedContract)),
                hudCenteredStyle, 9);
            DrawFittedLabel(new Rect(430, 785, 740, 24),
                L("retrofit.controls", "方向键 / 1—3 选择　ENTER 焊接确认"), tinyStyle, 8);
        }

        private void DrawRetrofitCard(AirframeModification modification, Rect rect, int index)
        {
            Color color = AirframeModificationColor(modification);
            bool selected = controllerSelection == index;
            bool hovered = rect.Contains(Event.current.mousePosition);
            if (hovered)
            {
                controllerSelection = index;
                RegisterHover($"retrofit-{modification}", AirframeModificationName(modification));
            }

            DrawRect(new Rect(rect.x + 9, rect.y + 11, rect.width, rect.height), new Color32(1, 5, 18, 255));
            DrawRect(rect, new Color32(9, 21, 45, 252));
            DrawPixelOutline(rect, selected ? Color.Lerp(color, Color.white, 0.2f) : color, selected ? 4f : 2f);
            DrawRect(new Rect(rect.x, rect.y, rect.width, 63), new Color32(13, 37, 70, 255));
            DrawRect(new Rect(rect.x, rect.y, 7, rect.height), color);
            DrawFittedLabel(new Rect(rect.x + 22, rect.y + 11, rect.width - 44, 39),
                AirframeModificationName(modification), neonSubtitleStyle, 13);
            DrawFittedLabel(new Rect(rect.x + 22, rect.y + 65, rect.width - 44, 24),
                AirframeModificationDoctrine(modification), tinyStyle, 8);
            DrawAirframeModificationIcon(new Vector2(rect.center.x, rect.y + 145), modification, color);

            Rect benefit = new Rect(rect.x + 24, rect.y + 205, rect.width - 48, 72);
            DrawRect(benefit, new Color32(4, 27, 34, 235));
            DrawPixelOutline(benefit, NeonCyan, 2f);
            DrawFittedLabel(new Rect(benefit.x + 12, benefit.y + 7, benefit.width - 24, 20),
                L("retrofit.benefit", "结构增益"), tinyStyle, 8);
            DrawFittedLabel(new Rect(benefit.x + 12, benefit.y + 28, benefit.width - 24, 37),
                AirframeModificationBenefit(modification), neonBodyStyle, 10);

            Rect cost = new Rect(rect.x + 24, rect.y + 288, rect.width - 48, 65);
            DrawRect(cost, new Color32(49, 15, 28, 235));
            DrawPixelOutline(cost, PostalRed, 2f);
            DrawFittedLabel(new Rect(cost.x + 12, cost.y + 7, cost.width - 24, 18),
                L("retrofit.cost", "永久代价"), tinyStyle, 8);
            DrawFittedLabel(new Rect(cost.x + 12, cost.y + 25, cost.width - 24, 34),
                AirframeModificationCost(modification), neonBodyStyle, 10);

            DrawPixelButton(new Rect(rect.x + 42, rect.y + 369, rect.width - 84, 44),
                L("retrofit.install", "焊入机体"), color, () => InstallAirframeModification(modification),
                true, (index + 1).ToString());
        }

        private static void DrawAirframeModificationIcon(Vector2 center, AirframeModification modification, Color color)
        {
            DrawPixelOutline(new Rect(center.x - 52, center.y - 42, 104, 84), color, 4f);
            DrawRect(new Rect(center.x - 34, center.y - 10, 68, 20), color);
            DrawRect(new Rect(center.x - 10, center.y - 34, 20, 68), color);
            switch (modification)
            {
                case AirframeModification.SealedBulkhead:
                    DrawRect(new Rect(center.x - 27, center.y - 27, 54, 54), new Color32(5, 20, 39, 255));
                    DrawPixelOutline(new Rect(center.x - 27, center.y - 27, 54, 54), Color.white, 3f);
                    break;
                case AirframeModification.OpenAvionics:
                    DrawRect(new Rect(center.x - 42, center.y - 4, 84, 8), Color.white);
                    DrawRect(new Rect(center.x - 4, center.y - 42, 8, 84), Color.white);
                    break;
                case AirframeModification.RedlineTurbine:
                    DrawRect(new Rect(center.x - 18, center.y - 30, 36, 60), PostalRed);
                    DrawRect(new Rect(center.x - 30, center.y - 8, 60, 16), Color.white);
                    break;
            }
        }

        private void InstallAirframeModification(AirframeModification modification)
        {
            if (runModification != AirframeModification.None || modification == AirframeModification.None)
                return;
            runModification = modification;
            CaptureBuildSnapshot(RunBuildSnapshotMoment.Retrofit, "act2_retrofit");
            screen = ScreenAfterRouteTransition();
            controllerSelection = 0;
            PlayLayeredSound(rewardSound, 0.82f, 0.95f, impactSound, 0.64f, 0.62f);
            TriggerShake(12f, 0.42f);
            TriggerFullScreenImpact(1.35f, 0.7f, false);
            RecordRunDiagnostic("airframe_modified", modification.ToString());
            SaveRunCheckpoint();
        }

        private void DrawFinalApproachScreen()
        {
            ModuleId? suggestedModule = RunStructureCatalog.SuggestedFinalModule(selectedContract, runModules);
            bool patchAvailable = credits >= 20 && runHull < BattleState.MaxPlayerHealth;
            bool trimAvailable = runDeck.Count > 0;
            bool overclockAvailable = runCargoIntegrity > 1 && suggestedModule.HasValue;

            DrawRect(new Rect(70, 48, 1460, 805), new Color32(2, 7, 22, 250));
            DrawRect(new Rect(78, 56, 1444, 789), PanelNight);
            DrawNeonFrame(new Rect(78, 56, 1444, 789), PostalRed, 3f);
            DrawFittedLabel(new Rect(125, 82, 850, 62),
                L("final_approach.title", "终局进场方案"), neonTitleStyle, 26);
            DrawFittedLabel(new Rect(1040, 92, 390, 35),
                L("final_approach.header", "ACT III // FINAL APPROACH"), hudCenteredStyle, 8);
            DrawFittedLabel(new Rect(130, 146, 1340, 42),
                L("final_approach.subtitle", "终局前哨已进入雷达。执行一次不可逆的战前整备，或保持当前构筑直接进场。"),
                neonBodyStyle, 12);

            Rect status = new Rect(300, 202, 1000, 42);
            DrawRect(status, new Color32(7, 23, 49, 245));
            DrawPixelOutline(status, Gold, 2f);
            DrawFittedLabel(new Rect(status.x + 20, status.y + 7, status.width - 40, 28),
                L("final_approach.status", "机体 {0}/{1}　//　货物 {2}/3　//　邮票 {3}　//　牌组 {4}　//　模块 {5}",
                    runHull, BattleState.MaxPlayerHealth, runCargoIntegrity, credits, runDeck.Count, runModules.Count),
                hudCenteredStyle, 9);

            DrawRunStructureChoice(new Rect(165, 260, 390, 430),
                L("final_approach.patch.title", "战地补片"),
                L("final_approach.patch.badge", "结构整备 // 恢复 10"),
                L("final_approach.patch.benefit", "在终局前修复 10 点机体耐久，降低先遣战连续受损的压力。"),
                L("final_approach.patch.cost", "支付 20 枚邮票。机体完整或邮票不足时无法执行。"),
                NeonCyan, patchAvailable, ResolveFinalFieldPatch,
                L("final_approach.execute", "执行方案"), 0);
            DrawRunStructureChoice(new Rect(605, 260, 390, 430),
                L("final_approach.trim.title", "抛弃冗余"),
                L("final_approach.trim.badge", "牌组整备 // 删除 1 张"),
                L("final_approach.trim.benefit", "从当前牌组删除任意一张卡牌副本，让终局循环更集中。"),
                L("final_approach.trim.cost", "不获得机体或资源补偿；删除确认后不可撤销。"),
                NeonViolet, trimAvailable, OpenFinalTrim,
                L("final_approach.execute", "执行方案"), 1);
            DrawRunStructureChoice(new Rect(1045, 260, 390, 430),
                L("final_approach.overclock.title", "货舱超频"),
                suggestedModule.HasValue
                    ? L("final_approach.overclock.badge", "合同模块 // {0}", ModuleName(suggestedModule.Value))
                    : L("final_approach.overclock.complete", "合同模块 // 已全部安装"),
                L("final_approach.overclock.benefit", "安装一枚尚未持有的合同协同模块，立即改变终局战斗循环。"),
                L("final_approach.overclock.cost", "货物完整度 -1；货物只剩 1 点时禁止执行。"),
                Gold, overclockAvailable, ResolveCargoOverclock,
                L("final_approach.execute", "执行方案"), 2);

            DrawPixelButton(new Rect(515, 735, 570, 58),
                L("final_approach.hold", "保持航向 // 不改变当前状态"),
                Shadow, () => FinalizeApproach(FinalApproachPlan.HoldCourse, "hold_course"), true, "4");
        }

        private void SelectFinalApproachOption(int index)
        {
            switch (index)
            {
                case 0:
                    ResolveFinalFieldPatch();
                    break;
                case 1:
                    OpenFinalTrim();
                    break;
                case 2:
                    ResolveCargoOverclock();
                    break;
                default:
                    FinalizeApproach(FinalApproachPlan.HoldCourse, "hold_course");
                    break;
            }
        }

        private void ResolveFinalFieldPatch()
        {
            if (finalApproachPlan != FinalApproachPlan.Unselected ||
                credits < 20 || runHull >= BattleState.MaxPlayerHealth)
                return;
            int before = runHull;
            credits -= 20;
            runHull = Mathf.Min(BattleState.MaxPlayerHealth, runHull + 10);
            FinalizeApproach(FinalApproachPlan.FieldPatch, $"field_patch|healed={runHull - before}");
        }

        private void OpenFinalTrim()
        {
            if (finalApproachPlan != FinalApproachPlan.Unselected || runDeck.Count == 0)
                return;
            screen = ScreenMode.FinalTrim;
            deckPurgePage = 0;
            controllerSelection = 0;
            SaveRunCheckpoint();
        }

        private void ResolveCargoOverclock()
        {
            ModuleId? module = RunStructureCatalog.SuggestedFinalModule(selectedContract, runModules);
            if (finalApproachPlan != FinalApproachPlan.Unselected || runCargoIntegrity <= 1 || !module.HasValue)
                return;
            runCargoIntegrity--;
            runModules.Add(module.Value);
            DeliveryArchiveService.RegisterRewardDiscoveries(archiveData, Array.Empty<int>(),
                new[] { (int)module.Value });
            SaveArchive();
            FinalizeApproach(FinalApproachPlan.CargoOverclock, $"cargo_overclock|module={module.Value}");
        }

        private void FinalizeApproach(FinalApproachPlan plan, string diagnostic)
        {
            if (finalApproachPlan != FinalApproachPlan.Unselected || plan == FinalApproachPlan.Unselected)
                return;
            finalApproachPlan = plan;
            CaptureBuildSnapshot(RunBuildSnapshotMoment.FinalApproach, "act3_plan");
            screen = ScreenMode.Map;
            controllerSelection = 0;
            PlayLayeredSound(rewardSound, 0.9f, 0.8f, impactSound, 0.72f, 0.52f);
            TriggerFullScreenImpact(0.9f, 0.45f, false);
            RecordRunDiagnostic("final_approach_plan", diagnostic);
            SaveRunCheckpoint();
        }

        private static string AirframeModificationName(AirframeModification modification)
        {
            return modification switch
            {
                AirframeModification.SealedBulkhead => L("retrofit.name.SealedBulkhead", "密封隔舱"),
                AirframeModification.OpenAvionics => L("retrofit.name.OpenAvionics", "开放航电"),
                AirframeModification.RedlineTurbine => L("retrofit.name.RedlineTurbine", "红线涡轮"),
                _ => L("retrofit.name.None", "未改装")
            };
        }

        private static string AirframeModificationDoctrine(AirframeModification modification)
        {
            return modification switch
            {
                AirframeModification.SealedBulkhead => L("retrofit.doctrine.SealedBulkhead", "DEFENSE DOCTRINE // 防御循环"),
                AirframeModification.OpenAvionics => L("retrofit.doctrine.OpenAvionics", "HAND DOCTRINE // 扩展循环"),
                _ => L("retrofit.doctrine.RedlineTurbine", "ENERGY DOCTRINE // 高热循环")
            };
        }

        private static string AirframeModificationBenefit(AirframeModification modification)
        {
            return modification switch
            {
                AirframeModification.SealedBulkhead => L("retrofit.benefit.SealedBulkhead", "每场战斗及每回合开始获得5点护盾。"),
                AirframeModification.OpenAvionics => L("retrofit.benefit.OpenAvionics", "每回合将手牌补至6张。"),
                _ => L("retrofit.benefit.RedlineTurbine", "每回合拥有4点基础能量。")
            };
        }

        private static string AirframeModificationCost(AirframeModification modification)
        {
            return modification switch
            {
                AirframeModification.SealedBulkhead => L("retrofit.cost.SealedBulkhead", "每回合只将手牌补至4张。"),
                AirframeModification.OpenAvionics => L("retrofit.cost.OpenAvionics", "每场战斗及每回合开始获得1层航迹暴露。"),
                _ => L("retrofit.cost.RedlineTurbine", "回合结束时不再自然降低热量。")
            };
        }

        private static string AirframeModificationHud(AirframeModification modification)
        {
            return modification switch
            {
                AirframeModification.SealedBulkhead => L("retrofit.hud.SealedBulkhead", "回合护盾5 / 手牌4"),
                AirframeModification.OpenAvionics => L("retrofit.hud.OpenAvionics", "手牌6 / 回合暴露+1"),
                AirframeModification.RedlineTurbine => L("retrofit.hud.RedlineTurbine", "能量4 / 无自然冷却"),
                _ => L("retrofit.hud.None", "标准机体")
            };
        }

        private static Color AirframeModificationColor(AirframeModification modification)
        {
            return modification switch
            {
                AirframeModification.SealedBulkhead => new Color32(63, 205, 177, 255),
                AirframeModification.OpenAvionics => new Color32(80, 204, 255, 255),
                _ => new Color32(255, 104, 88, 255)
            };
        }

        private static string ModuleName(ModuleId module)
        {
            return module switch
            {
                ModuleId.VectorThruster => L("module.VectorThruster", "矢量回流器"),
                ModuleId.PrismBulkhead => L("module.PrismBulkhead", "棱镜隔舱"),
                ModuleId.CryoHeart => L("module.CryoHeart", "零度炉心"),
                ModuleId.ExecutionChip => L("module.ExecutionChip", "处决芯片"),
                ModuleId.PrecisionMatrix => L("module.PrecisionMatrix", "精密矩阵"),
                ModuleId.MomentumFlywheel => L("module.MomentumFlywheel", "动量飞轮"),
                ModuleId.AegisCapacitor => L("module.AegisCapacitor", "神盾电容"),
                ModuleId.ZeroPointReactor => L("module.ZeroPointReactor", "零点反应堆"),
                ModuleId.RedlineReactor => L("module.RedlineReactor", "红线反应堆"),
                ModuleId.SwarmUplink => L("module.SwarmUplink", "蜂群上行链路"),
                _ => L("module.GhostDecoder", "幽灵解码器")
            };
        }

        private static string ModuleRules(ModuleId module)
        {
            return module switch
            {
                ModuleId.VectorThruster => "每回合首次切换航道时，返还1点能量。",
                ModuleId.PrismBulkhead => "战斗开始及每回合开始时，保留3点护盾。",
                ModuleId.CryoHeart => "过热上限提高2点；回合结束额外降低1点热量。",
                ModuleId.ExecutionChip => "每回合首张攻击牌额外造成4点单体伤害；齐射对每名敌人额外造成2点。",
                ModuleId.PrecisionMatrix => "穿甲轨炮结算后保留1层锁定。",
                ModuleId.MomentumFlywheel => "动量不再于回合结束时清空。",
                ModuleId.AegisCapacitor => "每回合首次达到10点护盾时，获得1点能量。",
                ModuleId.ZeroPointReactor => "每回合首次在低热状态攻击时必定暴击。",
                ModuleId.RedlineReactor => "每回合首次在5点以上热量攻击时必定暴击。",
                ModuleId.SwarmUplink => "所有齐射与飞弹基础伤害提高，并强化蜂群信标。",
                _ => "每回合首次清除航迹暴露时，获得1点能量。"
            };
        }

        private string ModuleSynergy(ModuleId module)
        {
            if (selectedContract == CargoContract.StormCore && module == ModuleId.VectorThruster)
                return "合同超频 · 免费维持稳定";
            if (selectedContract == CargoContract.CryoSerum && module == ModuleId.CryoHeart)
                return "合同超频 · 热量容错翻倍";
            if (selectedContract == CargoContract.FragileMedicine && module == ModuleId.PrismBulkhead)
                return "合同超频 · 常驻伤害缓冲";
            return module switch
            {
                ModuleId.ExecutionChip => "爆发质变 · 每回合首击强化",
                ModuleId.VectorThruster => "循环质变 · 机动不再亏费",
                ModuleId.CryoHeart => "热量质变 · 支持高热连打",
                ModuleId.PrismBulkhead => "生存质变 · 回合自带护盾",
                ModuleId.PrecisionMatrix => "锁定狙击 · 终结后继续循环",
                ModuleId.MomentumFlywheel => "矢量追猎 · 动量跨回合保留",
                ModuleId.AegisCapacitor => "护盾冲角 · 高盾返还能量",
                ModuleId.ZeroPointReactor => "零度循环 · 低热必定暴击",
                ModuleId.RedlineReactor => "熔炉爆发 · 高热必定暴击",
                ModuleId.SwarmUplink => "蜂群弹幕 · 全部齐射增幅",
                _ => "航迹欺骗 · 清除暴露返还能量"
            };
        }

        private void DrawModuleOffer(ModuleId module, Rect rect, string footer, bool enabled, Action action, string shortcut)
        {
            bool hovered = enabled && rect.Contains(Event.current.mousePosition);
            if (hovered)
            {
                rect.y -= 12f + Mathf.Sin(Time.time * 9f) * 2f;
                RegisterHover($"module-{module}", $"点击安装稀有模块 · {ModuleName(module)}");
            }

            Color gold = enabled ? new Color32(255, 195, 64, 255) : new Color32(118, 119, 116, 255);
            DrawRect(new Rect(rect.x + 9, rect.y + 9, rect.width, rect.height), new Color32(2, 7, 22, 255));
            DrawRect(rect, new Color32(15, 17, 35, 255));
            DrawNeonFrame(rect, hovered ? Color.white : gold, hovered ? 5f : 3f);
            DrawRect(new Rect(rect.x, rect.y, rect.width, 58), new Color32(97, 56, 21, 255));
            DrawRect(new Rect(rect.x, rect.y, rect.width, 6), gold);
            DrawFittedLabel(new Rect(rect.x + 18, rect.y + 9, rect.width - 36, 42), ModuleName(module), moduleTitleStyle, 11);

            float pulse = 0.75f + Mathf.Sin(Time.time * 7f + (int)module) * 0.15f;
            Vector2 core = new Vector2(rect.center.x, rect.y + 119f);
            DrawPixelOutline(new Rect(core.x - 34f - pulse * 5f, core.y - 34f - pulse * 5f,
                68f + pulse * 10f, 68f + pulse * 10f), new Color32(255, 196, 61, 170), 3f);
            DrawRect(new Rect(core.x - 22, core.y - 22, 44, 44), new Color32(255, 161, 44, 255));
            DrawRect(new Rect(core.x - 11, core.y - 32, 22, 64), gold);
            DrawRect(new Rect(core.x - 32, core.y - 11, 64, 22), gold);
            DrawRect(new Rect(core.x - 8, core.y - 8, 16, 16), Color.white);

            DrawRect(new Rect(rect.x + 18, rect.y + 164, rect.width - 36, 27), new Color32(85, 48, 17, 235));
            GUI.Label(new Rect(rect.x + 22, rect.y + 166, rect.width - 44, 23), "LEGENDARY // 永久模块", tinyStyle);
            DrawFittedLabel(new Rect(rect.x + 22, rect.y + 199, rect.width - 44, 64), ModuleRules(module), moduleBodyStyle, 10);
            DrawRect(new Rect(rect.x + 18, rect.y + 266, rect.width - 36, 27), new Color32(7, 18, 43, 232));
            DrawFittedLabel(new Rect(rect.x + 22, rect.y + 268, rect.width - 44, 23), ModuleSynergy(module), tinyStyle, 8);
            DrawPixelButton(new Rect(rect.x + 18, rect.y + rect.height - 48, rect.width - 36, 42), footer, gold, action, enabled);
        }

        private string EventTitle()
        {
            return EventTitleForNode(selectedRouteNodeId);
        }

        private string EventTitleForNode(int nodeId)
        {
            if (nodeId == 7 && RouteStoryRules.IsSilent(routeStoryState))
                return L("event.story.mid.silent.title", "回声二：静默拆解场");
            if (nodeId == 12 && RouteStoryRules.IsSilent(routeStoryState))
                return L("event.story.final.silent.title", "回声终章：无声航标");
            if (nodeId == 7 && RouteStoryRules.IsPromise(routeStoryState))
                return L("event.story.mid.promise.title", "回声二：漂流救援舱");
            if (nodeId == 7 && RouteStoryRules.IsDebt(routeStoryState))
                return L("event.story.mid.debt.title", "回声二：追债者残骸带");
            if (nodeId == 12 && RouteStoryRules.IsPromise(routeStoryState))
                return L("event.story.final.promise.title", "回声终章：风眼护航");
            if (nodeId == 12 && RouteStoryRules.IsDebt(routeStoryState))
                return L("event.story.final.debt.title", "回声终章：债务封锁");
            if (nodeId == 7 || nodeId == 12)
                return route.Get(nodeId).Title;
            return selectedContract switch
            {
                CargoContract.FragileMedicine => L("event.open.FragileMedicine.title", "失压医疗驳船"),
                CargoContract.CryoSerum => L("event.open.CryoSerum.title", "冻裂冷却塔"),
                CargoContract.StormCore => L("event.open.StormCore.title", "雷暴走私信标"),
                CargoContract.SignalSeed => L("event.open.SignalSeed.title", "休眠信标苗圃"),
                _ => L("event.open.BlackBoxRelay.title", "失联侦察黑匣")
            };
        }

        private string EventDescription()
        {
            return EventDescriptionForNode(selectedRouteNodeId);
        }

        private string EventDescriptionForNode(int nodeId)
        {
            if (nodeId == 7 && RouteStoryRules.IsSilent(routeStoryState))
                return L("event.story.mid.silent.desc",
                    "你封存的广播没有消失，而是被一座无登记拆解场接收。这里可以继续保持静默，也可以重新接入援助或残骸网络。");
            if (nodeId == 12 && RouteStoryRules.IsSilent(routeStoryState))
                return L("event.story.final.silent.desc",
                    "无声航标已经拼出一条不属于任何阵营的终局通路。最后一次裁掉冗余，便能带着完整的构筑记录进入风眼。");
            if (nodeId == 7 && RouteStoryRules.IsPromise(routeStoryState))
                return L("event.story.mid.promise.desc",
                    "你曾回应的信号再次出现。获救信使正守着一座补给舱，等待你确认这条互助航线。");
            if (nodeId == 7 && RouteStoryRules.IsDebt(routeStoryState))
                return L("event.story.mid.debt.desc",
                    "从旧信标取走的核心带有追踪码。债权人的无人艇已经先一步抵达残骸带。");
            if (nodeId == 12 && RouteStoryRules.IsPromise(routeStoryState))
                return L("event.story.final.promise.desc",
                    "一路响应你的信使组成临时船队，正在风眼外等待最后一次护航。兑现承诺会换来完整的援助网络。");
            if (nodeId == 12 && RouteStoryRules.IsDebt(routeStoryState))
                return L("event.story.final.debt.desc",
                    "被你带走的残骸资产终于引来债务封锁。现在可以交还收益终止追踪，或烧毁应答器强行突围。");
            if (nodeId == 7)
                return L("event.wreckage.desc",
                    "破碎舰体在雷云中漂流。稳定供能单元仍可回收，但深入残骸会暴露货舱坐标。");
            if (nodeId == 12)
                return L("event.observatory.desc",
                    "废弃观测站保存着磁暴鳐的放电记录，也有一条穿越高压云墙的危险捷径。");
            return selectedContract switch
            {
                CargoContract.FragileMedicine => L("event.open.FragileMedicine.desc",
                    "一艘医疗驳船在乱流中失压，求救信号与货舱坐标同时暴露。"),
                CargoContract.CryoSerum => L("event.open.CryoSerum.desc",
                    "废弃冷却塔仍有一枚低温核心，但外壳正在快速崩裂。"),
                CargoContract.StormCore => L("event.open.StormCore.desc",
                    "非法信标标出穿越雷暴的短路，同时广播一笔无人认领的高额邮资。"),
                CargoContract.SignalSeed => L("event.open.SignalSeed.desc",
                    "一座休眠苗圃仍在培育导航信标；它需要精确保留一段供能脉冲才能安全唤醒。"),
                _ => L("event.open.BlackBoxRelay.desc",
                    "失联侦察机留下加密黑匣；敌方追踪波束正沿着广播信号逼近。")
            };
        }

        private string RouteStoryStatus()
        {
            return routeStoryState switch
            {
                RouteStoryState.BeaconPromise => L("event.story.status.promise", "援助承诺待兑现"),
                RouteStoryState.SalvageDebt => L("event.story.status.debt", "残骸债务正在追踪"),
                RouteStoryState.PromiseStrengthened => L("event.story.status.promise_strong", "护航盟约已确认"),
                RouteStoryState.DebtDeepened => L("event.story.status.debt_deep", "高额债务已经锁定"),
                RouteStoryState.PromiseFulfilled => L("event.story.status.fulfilled", "互助航线已经建立"),
                RouteStoryState.PromiseBetrayed => L("event.story.status.betrayed", "救援坐标已经售出"),
                RouteStoryState.DebtRepaid => L("event.story.status.repaid", "残骸债务已经结清"),
                RouteStoryState.DebtDefied => L("event.story.status.defied", "追债应答器已经烧毁"),
                RouteStoryState.SignalSevered => L("event.story.status.silent", "原始广播已经封存"),
                RouteStoryState.SilenceMaintained => L("event.story.status.silent_strong", "静默航线保持完整"),
                RouteStoryState.SilentRouteSecured => L("event.story.status.silent_final", "无声航标已经锁定"),
                _ => L("event.story.status.none", "尚未建立信标纪事")
            };
        }

        private bool IsStoryFinale()
        {
            return selectedRouteNodeId == 12 && RouteStoryRules.IsPending(routeStoryState);
        }

        private string EventChapterLabel()
        {
            if (IsStoryFinale())
                return L("event.story.chapter.final", "SIGNAL THREAD // FINAL");
            if (selectedRouteNodeId == 7 && RouteStoryRules.IsPending(routeStoryState))
                return L("event.story.chapter.mid", "SIGNAL THREAD // ECHO II");
            return L("event.story.chapter.open", "SIGNAL THREAD // ORIGIN");
        }

        private string EventSafeTitle()
        {
            if (IsStoryFinale() && RouteStoryRules.IsSilent(routeStoryState))
                return L("event.story.final.silent.safe", "解封援助频段");
            if (IsStoryFinale() && RouteStoryRules.IsPromise(routeStoryState))
                return L("event.story.final.promise.safe", "兑现护航承诺");
            if (IsStoryFinale())
                return L("event.story.final.debt.safe", "归还残骸收益");
            if (selectedRouteNodeId == 7 && RouteStoryRules.IsPromise(routeStoryState))
                return L("event.story.mid.promise.safe", "守住援助频段");
            if (selectedRouteNodeId == 7 && RouteStoryRules.IsDebt(routeStoryState))
                return L("event.story.mid.debt.safe", "归还追踪识别码");
            if (selectedRouteNodeId == 7 && RouteStoryRules.IsSilent(routeStoryState))
                return L("event.story.mid.silent.safe", "重新接入援助网");
            if (selectedRouteNodeId == 7)
                return L("event.wreckage.safe", "回收稳定单元");
            if (selectedRouteNodeId == 12)
                return L("event.observatory.safe", "解析磁暴周期");
            return L("event.standard.safe", "执行合同协议");
        }

        private string EventRiskTitle()
        {
            if (IsStoryFinale() && RouteStoryRules.IsSilent(routeStoryState))
                return L("event.story.final.silent.risk", "出售静默坐标");
            if (IsStoryFinale() && RouteStoryRules.IsPromise(routeStoryState))
                return L("event.story.final.promise.risk", "出售救援航标");
            if (IsStoryFinale())
                return L("event.story.final.debt.risk", "烧毁追债应答器");
            if (selectedRouteNodeId == 7 && RouteStoryRules.IsPromise(routeStoryState))
                return L("event.story.mid.promise.risk", "拆走救援核心");
            if (selectedRouteNodeId == 7 && RouteStoryRules.IsDebt(routeStoryState))
                return L("event.story.mid.debt.risk", "洗劫黑匣舱");
            if (selectedRouteNodeId == 7 && RouteStoryRules.IsSilent(routeStoryState))
                return L("event.story.mid.silent.risk", "广播拆解坐标");
            if (selectedRouteNodeId == 7)
                return L("event.wreckage.risk", "深入残骸核心");
            return L("event.standard.risk", "强穿危险航路");
        }

        private string EventSafeDescription(CardId upgrade)
        {
            if (IsStoryFinale() && RouteStoryRules.IsSilent(routeStoryState))
                return L("event.story.final.silent.safe_desc",
                    "将全部【{0}】改写为B分支，并修复6点机体。", CardLibrary.Get(upgrade).Name);
            if (IsStoryFinale() && RouteStoryRules.IsPromise(routeStoryState))
                return L("event.story.final.promise.safe_desc",
                    "强化全部【{0}】，修复8点机体，并恢复1格货物完整度。", CardLibrary.Get(upgrade).Name);
            if (IsStoryFinale())
                return L("event.story.final.debt.safe_desc",
                    "交出最多30枚邮票，修复8点机体，并恢复1格货物完整度。");
            int repair = selectedRouteNodeId == 7 ? 6 : selectedRouteNodeId == 12 ? 2 : 4;
            return L("event.standard.safe_desc",
                "强化全部【{0}】并修复{1}点机体。\n货物完整度不会下降。",
                CardLibrary.Get(upgrade).Name, repair);
        }

        private string EventRiskDescription()
        {
            int hullLoss = selectedRouteNodeId == 7 ? 5 : selectedRouteNodeId == 12 ? 2 : 3;
            if (IsStoryFinale() && RouteStoryRules.IsDebt(routeStoryState))
                hullLoss = 5;
            else if (IsStoryFinale() && RouteStoryRules.IsSilent(routeStoryState))
                hullLoss = 3;
            return L("event.standard.risk_desc",
                "立即获得{0}枚邮票。\n机体损失{1}点，货物完整度损失1格。",
                EventRiskCredits(), hullLoss);
        }

        private string EventIndependentTitle()
        {
            if (selectedRouteNodeId == 12)
                return L("event.independent.final.title", "封存无声航标");
            if (selectedRouteNodeId == 7)
                return L("event.independent.mid.title", "执行静默拆解");
            return L("event.independent.open.title", "切断广播链路");
        }

        private string EventIndependentDescription()
        {
            if (selectedRouteNodeId == 12)
            {
                ModuleId? module = RunStructureCatalog.SuggestedFinalModule(selectedContract, runModules);
                return module.HasValue
                    ? L("event.independent.final.desc", "删除任意1张牌，并安装【{0}】。\n纪事转入中立无声结局。",
                        ModuleName(module.Value))
                    : L("event.independent.final.fallback", "删除任意1张牌，获得25枚邮票。\n纪事转入中立无声结局。");
            }
            if (selectedRouteNodeId == 7)
            {
                CardId core = ContractCatalog.StarterCard(selectedContract);
                return L("event.independent.mid.desc", "删除任意1张牌，获得12枚邮票，并将【{0}】改写为B分支。",
                    CardLibrary.Get(core).Name);
            }
            return L("event.independent.open.desc",
                "删除任意1张牌并获得18枚邮票。\n后续事件将沿静默航线展开。");
        }

        private CardId EventUpgradeCard()
        {
            return selectedContract switch
            {
                CargoContract.FragileMedicine => CardId.WindGuard,
                CargoContract.CryoSerum => CardId.EmergencyCoolant,
                CargoContract.StormCore => CardId.BankDown,
                CargoContract.SignalSeed => CardId.ReserveShot,
                _ => CardId.SignalScrambler
            };
        }

        private void DrawEventScreen()
        {
            Color eventColor = CargoColor(selectedContract);
            DrawRect(new Rect(145, 70, 1310, 770), new Color32(2, 7, 22, 255));
            DrawRect(new Rect(153, 78, 1294, 754), PanelNight);
            DrawNeonFrame(new Rect(153, 78, 1294, 754), eventColor, 3f);
            DrawFittedLabel(new Rect(225, 112, 850, 68), EventTitle(), neonTitleStyle, 26);
            DrawFittedLabel(new Rect(230, 178, 1120, 64), EventDescription(), neonBodyStyle, 12);
            DrawFittedLabel(new Rect(1090, 125, 280, 40), EventChapterLabel(), neonSubtitleStyle, 9);
            DrawRect(new Rect(230, 248, 1140, 30), new Color32(8, 22, 49, 245));
            DrawRect(new Rect(230, 248, 7, 30),
                RouteStoryRules.IsDebt(routeStoryState) ? PostalRed : eventColor);
            DrawFittedLabel(new Rect(250, 251, 1100, 24),
                L("event.story.thread", "信标纪事 // {0}", RouteStoryStatus()), tinyStyle, 8);

            CardId upgrade = EventUpgradeCard();
            DrawEventChoice(new Rect(175, 295, 390, 310), EventSafeTitle(),
                L("event.choice.cooperative", "协作路线"), EventSafeDescription(upgrade), eventColor,
                () => ResolveRouteEvent(true), 0);
            DrawEventChoice(new Rect(605, 295, 390, 310), EventRiskTitle(),
                L("event.choice.opportunist", "机会路线"), EventRiskDescription(), PostalRed,
                () => ResolveRouteEvent(false), 1);
            DrawEventChoice(new Rect(1035, 295, 390, 310), EventIndependentTitle(),
                L("event.choice.independent", "静默路线"), EventIndependentDescription(), NeonViolet,
                OpenEventPurge, 2, "EVENT", runDeck.Count > 0);

            if (eventResolved)
            {
                Rect result = new Rect(320, 650, 960, 70);
                DrawRect(result, new Color32(4, 13, 34, 246));
                DrawNeonFrame(result, eventColor, 3f);
                DrawFittedLabel(new Rect(result.x + 20, result.y + 7, result.width - 260, 56), eventResult, hudCenteredStyle, 9);
                DrawPixelButton(new Rect(result.x + result.width - 225, result.y + 9, 205, 52),
                    L("event.continue", "继续航行"), eventColor, LeaveRouteEvent);
            }
            else
            {
                DrawFittedLabel(new Rect(360, 665, 880, 40),
                    L("event.irreversible", "三条事件路线均不可撤销，并会改变后续信标纪事与构筑。"), hudCenteredStyle, 9);
            }
        }

        private void DrawEventChoice(Rect rect, string title, string badge, string description, Color color, Action action,
            int index, string category = "EVENT", bool condition = true)
        {
            bool enabled = (category == "SERVICE" ? !restResolved : !eventResolved) && condition;
            bool hovered = enabled && rect.Contains(Event.current.mousePosition);
            if (hovered)
                controllerSelection = index;
            DrawRect(new Rect(rect.x + 9, rect.y + 9, rect.width, rect.height), new Color32(2, 7, 22, 255));
            DrawRect(rect, new Color32(9, 20, 44, 255));
            bool focused = controllerActive && controllerSelection == index;
            DrawNeonFrame(rect, focused ? Color.white : hovered ? Color.Lerp(color, Color.white, 0.2f) : color,
                focused || hovered ? 4f : 2f);
            DrawRect(new Rect(rect.x, rect.y, rect.width, 66), new Color32(11, 31, 63, 255));
            DrawFittedLabel(new Rect(rect.x + 25, rect.y + 8, rect.width - 50, 50), title, neonSubtitleStyle, 12);
            DrawFittedLabel(new Rect(rect.x + 28, rect.y + 80, rect.width - 56, 30), $"{category} // {badge}", tinyStyle, 8);
            DrawRect(new Rect(rect.x + 28, rect.y + 118, rect.width - 56, 108), new Color32(3, 11, 31, 235));
            DrawFittedLabel(new Rect(rect.x + 46, rect.y + 127, rect.width - 92, 90), description, neonBodyStyle, 15);
            DrawPixelButton(new Rect(rect.x + 105, rect.y + 238, rect.width - 210, 52),
                L("event.confirm", "确认选择"), color, action, enabled);
        }

        private void ResolveRouteEvent(bool safeChoice)
        {
            if (eventResolved)
                return;
            if (IsStoryFinale())
            {
                ResolveStoryFinale(safeChoice);
                eventResolved = true;
                CaptureBuildSnapshot(RunBuildSnapshotMoment.RouteEvent, $"event_{selectedRouteNodeId}");
                SaveRunCheckpoint();
                return;
            }

            if (safeChoice)
            {
                CardId upgraded = EventUpgradeCard();
                runUpgrades.Add(upgraded);
                runUpgradeBranches[upgraded] = UpgradeBranch.Alpha;
                int repaired = selectedRouteNodeId == 7 ? 6 : selectedRouteNodeId == 12 ? 2 : 4;
                runHull = Mathf.Min(BattleState.MaxPlayerHealth, runHull + repaired);
                eventResult = L("event.result.safe", "协议完成：{0}+，机体修复{1}点",
                    CardLibrary.Get(upgraded).Name, repaired);
                PlayLayeredSound(rewardSound, 1.05f, 0.9f, shieldSound, 0.9f, 0.55f);
                TriggerFullScreenImpact(0.9f, 0.45f, false);
            }
            else
            {
                int gained = EventRiskCredits();
                int hullLoss = selectedRouteNodeId == 7 ? 5 : selectedRouteNodeId == 12 ? 2 : 3;
                credits += gained;
                runHull = Mathf.Max(1, runHull - hullLoss);
                runCargoIntegrity = Mathf.Max(0, runCargoIntegrity - 1);
                eventResult = L("event.result.risk", "危险穿越：+{0}邮票，机体-{1}，货物完整度-1",
                    gained, hullLoss);
                dangerFlashUntil = Time.time + 0.55f;
                TriggerShake(14f, 0.46f);
                PlayLayeredSound(warningSound, 0.8f, 0.85f, impactSound, 0.7f, 0.62f);
                TriggerFullScreenImpact(1.25f, 0.7f, false);
            }
            if (selectedRouteNodeId == 2)
                routeStoryState = RouteStoryRules.Begin(safeChoice);
            else if (selectedRouteNodeId == 7)
                routeStoryState = RouteStoryRules.ContinueAtWreckage(routeStoryState, safeChoice);
            eventResolved = true;
            CaptureBuildSnapshot(RunBuildSnapshotMoment.RouteEvent, $"event_{selectedRouteNodeId}");
            SaveRunCheckpoint();
        }

        private void ResolveStoryFinale(bool cooperativeChoice)
        {
            CardId upgraded = EventUpgradeCard();
            if (RouteStoryRules.IsPromise(routeStoryState))
            {
                if (cooperativeChoice)
                {
                    runUpgrades.Add(upgraded);
                    runUpgradeBranches[upgraded] = UpgradeBranch.Alpha;
                    int hullBefore = runHull;
                    int cargoBefore = runCargoIntegrity;
                    runHull = Mathf.Min(BattleState.MaxPlayerHealth, runHull + 8);
                    runCargoIntegrity = Mathf.Min(3, runCargoIntegrity + 1);
                    eventResult = L("event.story.result.fulfilled",
                        "承诺兑现：{0}+，机体修复{1}，货物完整度恢复{2}格",
                        CardLibrary.Get(upgraded).Name, runHull - hullBefore, runCargoIntegrity - cargoBefore);
                    PlayLayeredSound(rewardSound, 1.05f, 0.92f, shieldSound, 0.88f, 0.62f);
                    TriggerFullScreenImpact(1.05f, 0.52f, false);
                }
                else
                {
                    ApplyRiskEventOutcome(2);
                    eventResult = L("event.story.result.betrayed",
                        "坐标售出：+{0}邮票，机体-2，货物完整度-1；援助网络终止",
                        EventRiskCredits());
                }
            }
            else if (RouteStoryRules.IsDebt(routeStoryState) && cooperativeChoice)
            {
                int payment = Mathf.Min(30, credits);
                int hullBefore = runHull;
                int cargoBefore = runCargoIntegrity;
                credits -= payment;
                runHull = Mathf.Min(BattleState.MaxPlayerHealth, runHull + 8);
                runCargoIntegrity = Mathf.Min(3, runCargoIntegrity + 1);
                eventResult = L("event.story.result.repaid",
                    "债务结清：-{0}邮票，机体修复{1}，货物完整度恢复{2}格",
                    payment, runHull - hullBefore, runCargoIntegrity - cargoBefore);
                PlayLayeredSound(rewardSound, 0.96f, 0.84f, shieldSound, 0.82f, 0.58f);
                TriggerFullScreenImpact(0.9f, 0.45f, false);
            }
            else if (RouteStoryRules.IsDebt(routeStoryState))
            {
                ApplyRiskEventOutcome(5);
                eventResult = L("event.story.result.defied",
                    "强行突围：+{0}邮票，机体-5，货物完整度-1；追债信标已烧毁",
                    EventRiskCredits());
            }
            else if (cooperativeChoice)
            {
                runUpgrades.Add(upgraded);
                runUpgradeBranches[upgraded] = UpgradeBranch.Beta;
                int hullBefore = runHull;
                runHull = Mathf.Min(BattleState.MaxPlayerHealth, runHull + 6);
                eventResult = L("event.story.result.silent_cooperate",
                    "静默解封：{0}+B，机体修复{1}；航线重新接入援助网络",
                    CardLibrary.Get(upgraded).Name, runHull - hullBefore);
                PlayLayeredSound(rewardSound, 0.98f, 0.86f, shieldSound, 0.88f, 0.58f);
                TriggerFullScreenImpact(0.9f, 0.46f, false);
            }
            else
            {
                ApplyRiskEventOutcome(3);
                eventResult = L("event.story.result.silent_risk",
                    "静默坐标售出：+{0}邮票，机体-3，货物完整度-1",
                    EventRiskCredits());
            }

            routeStoryState = RouteStoryRules.ResolveAtObservatory(routeStoryState, cooperativeChoice);
        }

        private void OpenEventPurge()
        {
            if (eventResolved || runDeck.Count == 0)
                return;
            screen = ScreenMode.EventPurge;
            deckPurgePage = 0;
            controllerSelection = 0;
            SaveRunCheckpoint();
        }

        private void ResolveIndependentRouteEvent(CardId removedCard)
        {
            int nodeId = selectedRouteNodeId;
            routeStoryState = RouteStoryRules.ChooseIndependent(routeStoryState, nodeId);
            if (nodeId == 2)
            {
                int gained = RouteDecisionCatalog.IndependentEventCredits(nodeId);
                credits += gained;
                eventResult = L("event.independent.result.open",
                    "广播已封存：移除【{0}】，获得{1}邮票", CardLibrary.Get(removedCard).Name, gained);
            }
            else if (nodeId == 7)
            {
                int gained = RouteDecisionCatalog.IndependentEventCredits(nodeId);
                CardId core = ContractCatalog.StarterCard(selectedContract);
                credits += gained;
                runUpgrades.Add(core);
                runUpgradeBranches[core] = UpgradeBranch.Beta;
                eventResult = L("event.independent.result.mid",
                    "静默拆解完成：移除【{0}】，获得{1}邮票，{2}+B",
                    CardLibrary.Get(removedCard).Name, gained, CardLibrary.Get(core).Name);
            }
            else
            {
                ModuleId? module = RunStructureCatalog.SuggestedFinalModule(selectedContract, runModules);
                if (module.HasValue)
                {
                    runModules.Add(module.Value);
                    DeliveryArchiveService.RegisterRewardDiscoveries(archiveData, Array.Empty<int>(),
                        new[] { (int)module.Value });
                    SaveArchive();
                    eventResult = L("event.independent.result.final_module",
                        "无声航标锁定：移除【{0}】，安装【{1}】",
                        CardLibrary.Get(removedCard).Name, ModuleName(module.Value));
                }
                else
                {
                    int gained = RouteDecisionCatalog.IndependentEventCredits(nodeId);
                    credits += gained;
                    eventResult = L("event.independent.result.final_credits",
                        "无声航标锁定：移除【{0}】，获得{1}邮票",
                        CardLibrary.Get(removedCard).Name, gained);
                }
            }

            eventResolved = true;
            screen = ScreenMode.Event;
            controllerSelection = 0;
            CaptureBuildSnapshot(RunBuildSnapshotMoment.RouteEvent, $"event_{nodeId}");
            PlayLayeredSound(clickSound, 0.74f, 0.62f, rewardSound, 1.12f, 0.62f);
            TriggerFullScreenImpact(0.8f, 0.4f, false);
            SaveRunCheckpoint();
        }

        private void ApplyRiskEventOutcome(int hullLoss)
        {
            credits += EventRiskCredits();
            runHull = Mathf.Max(1, runHull - hullLoss);
            runCargoIntegrity = Mathf.Max(0, runCargoIntegrity - 1);
            dangerFlashUntil = Time.time + 0.55f;
            TriggerShake(14f, 0.46f);
            PlayLayeredSound(warningSound, 0.8f, 0.85f, impactSound, 0.7f, 0.62f);
            TriggerFullScreenImpact(1.25f, 0.7f, false);
        }

        private void LeaveRouteEvent()
        {
            AdvanceAfterCurrentRouteNode();
            SaveRunCheckpoint();
        }

        private int EventRiskCredits()
        {
            int routeBonus = selectedRouteNodeId == 7 ? 10 : selectedRouteNodeId == 12 ? 20 : 0;
            return routeBonus + (selectedContract switch
            {
                CargoContract.BlackBoxRelay => 70,
                CargoContract.StormCore => 65,
                CargoContract.SignalSeed => 60,
                _ => 55
            });
        }

        private void DrawRestScreen()
        {
            Color restColor = RouteNodeColor(RouteNodeKind.Rest);
            RouteNodeDefinition node = route.Get(selectedRouteNodeId);
            DrawRect(new Rect(145, 70, 1310, 770), new Color32(2, 7, 22, 255));
            DrawRect(new Rect(153, 78, 1294, 754), PanelNight);
            DrawNeonFrame(new Rect(153, 78, 1294, 754), restColor, 3f);
            DrawFittedLabel(new Rect(225, 112, 850, 68), node.Title, neonTitleStyle, 26);
            DrawFittedLabel(new Rect(230, 178, 1120, 64), node.Description, neonBodyStyle, 12);
            DrawFittedLabel(new Rect(1120, 125, 250, 40), "SERVICE DOCK", neonSubtitleStyle, 10);

            CardId tuneCard = ContractCatalog.StarterCard(selectedContract);
            DrawEventChoice(new Rect(225, 295, 350, 310), "修复机体结构", "恢复 14 机体",
                "船坞完成结构焊接与引擎检修。\n不会改变牌组或货物状态。", restColor,
                ResolveRestRepair, 0, "SERVICE");
            DrawEventChoice(new Rect(625, 295, 350, 310), "核心分支校准", $"{CardLibrary.Get(tuneCard).Name} A / B",
                $"为全部【{CardLibrary.Get(tuneCard).Name}】选择增幅或机制分支。\n本次停靠不恢复机体。", NeonCyan,
                OpenCoreUpgrade, 1, "SERVICE");
            DrawEventChoice(new Rect(1025, 295, 350, 310), "精简指令牌组", "删除 1 张牌",
                "从牌组中删除任意一张卡牌副本。\n本次配送的构筑取舍完全由你决定。", NeonViolet,
                OpenDeckPurge, 2, "SERVICE");

            if (restResolved)
            {
                Rect result = new Rect(320, 650, 960, 70);
                DrawRect(result, new Color32(4, 13, 34, 246));
                DrawNeonFrame(result, restColor, 3f);
                DrawFittedLabel(new Rect(result.x + 20, result.y + 7, result.width - 260, 56), restResult, hudCenteredStyle, 9);
                DrawPixelButton(new Rect(result.x + result.width - 225, result.y + 9, 205, 52), "重新启航", restColor, LeaveRestStop);
            }
        }

        private void ResolveRestRepair()
        {
            if (restResolved)
                return;
            int before = runHull;
            runHull = Mathf.Min(BattleState.MaxPlayerHealth, runHull + 14);
            restResult = $"结构维护完成：机体恢复 {runHull - before} 点";
            PlayLayeredSound(shieldSound, 0.9f, 0.8f, rewardSound, 1.08f, 0.5f);
            restResolved = true;
            CaptureBuildSnapshot(RunBuildSnapshotMoment.ServiceDock, $"service_{selectedRouteNodeId}");
            TriggerFullScreenImpact(0.8f, 0.42f, false);
            SaveRunCheckpoint();
        }

        private void LeaveRestStop()
        {
            AdvanceAfterCurrentRouteNode();
            SaveRunCheckpoint();
        }

        private void OpenDeckPurge()
        {
            if (restResolved)
                return;
            screen = ScreenMode.DeckPurge;
            deckPurgePage = 0;
            controllerSelection = 0;
            SaveRunCheckpoint();
        }

        private void DrawDeckPurgeScreen()
        {
            const int pageSize = 10;
            bool finalTrim = screen == ScreenMode.FinalTrim;
            bool shopPurge = screen == ScreenMode.ShopPurge;
            bool eventPurge = screen == ScreenMode.EventPurge;
            CardId[] candidates = PurgeCandidates();
            int pageCount = Mathf.Max(1, Mathf.CeilToInt(candidates.Length / (float)pageSize));
            deckPurgePage = Mathf.Clamp(deckPurgePage, 0, pageCount - 1);
            int damageCount = runDeck.Count(CardPoolCatalog.IsDamageCard);

            if (Event.current.type == EventType.ScrollWheel)
            {
                deckPurgePage = Mathf.Clamp(deckPurgePage + (Event.current.delta.y > 0f ? 1 : -1), 0, pageCount - 1);
                Event.current.Use();
            }

            DrawRect(new Rect(80, 52, 1440, 800), new Color32(2, 7, 22, 255));
            DrawRect(new Rect(88, 60, 1424, 784), PanelNight);
            DrawNeonFrame(new Rect(88, 60, 1424, 784), NeonViolet, 3f);
            DrawFittedLabel(new Rect(145, 88, 780, 62),
                finalTrim
                    ? L("final_trim.title", "终局抛弃清单")
                    : shopPurge
                        ? L("shop.purge.title", "补给站拆牌台")
                        : eventPurge
                            ? L("event.purge.title", "静默舱单裁切")
                            : L("purge.title", "精简指令牌组"),
                neonTitleStyle, 27);
            DrawFittedLabel(new Rect(150, 148, 1030, 42),
                finalTrim
                    ? L("final_trim.subtitle", "选择一种卡牌并永久删除其中一张副本；确认后将直接进入终局前哨航线。")
                    : shopPurge
                        ? L("shop.purge.subtitle", "支付{0}邮票，选择一种卡牌并永久删除其中一张副本。",
                            RouteDecisionCatalog.ShopPurgeCost(selectedRouteNodeId))
                        : eventPurge
                            ? L("event.purge.subtitle", "选择一种卡牌并永久删除其中一张副本，随后结算静默事件分支。")
                            : L("purge.subtitle", "选择一种卡牌并删除其中一张副本；被删除的卡牌不会进入本场配送的弃牌堆。"),
                neonBodyStyle, 11);
            DrawFittedLabel(new Rect(1170, 100, 270, 38),
                L("purge.deck_status", "牌组 {0}　伤害牌 {1}", runDeck.Count, damageCount),
                hudCenteredStyle, 9);

            int start = deckPurgePage * pageSize;
            int end = Mathf.Min(candidates.Length, start + pageSize);
            for (int i = start; i < end; i++)
            {
                int local = i - start;
                Rect rect = new Rect(135 + (local % 5) * 275, 225 + (local / 5) * 220, 245, 185);
                DrawPurgeCard(candidates[i], rect, controllerSelection == i);
            }

            DrawPixelButton(new Rect(130, 694, 100, 48), "<", Shadow,
                () => deckPurgePage = Mathf.Max(0, deckPurgePage - 1), deckPurgePage > 0);
            DrawFittedLabel(new Rect(250, 700, 220, 36),
                L("purge.page", "牌页 {0} / {1}", deckPurgePage + 1, pageCount), hudCenteredStyle, 9);
            DrawPixelButton(new Rect(490, 694, 100, 48), ">", Shadow,
                () => deckPurgePage = Mathf.Min(pageCount - 1, deckPurgePage + 1), deckPurgePage < pageCount - 1);
            DrawPixelButton(new Rect(1050, 690, 360, 58),
                finalTrim
                    ? L("final_trim.cancel", "返回进场方案")
                    : shopPurge
                        ? L("shop.purge.cancel", "取消拆牌，返回补给站")
                        : eventPurge
                            ? L("event.purge.cancel", "取消裁切，返回事件")
                            : L("purge.cancel", "取消精简，返回维修坞"),
                Shadow, CancelDeckPurge, true, "B");
            DrawFittedLabel(new Rect(220, 770, 1160, 38),
                finalTrim
                    ? L("final_trim.note", "终局整备 // 可移除任意 1 张卡牌副本；不设置牌组或伤害牌下限")
                    : shopPurge
                        ? L("shop.purge.note", "付费精简 // 本补给站限用一次；不设置牌组或伤害牌下限")
                        : eventPurge
                            ? L("event.purge.note", "静默分支 // 可在确认删除前返回事件重新选择")
                            : L("purge.note", "构筑自治 // 本次停靠可移除任意 1 张卡牌副本"),
                tinyStyle, 9);
        }

        private void DrawPurgeCard(CardId cardId, Rect rect, bool selected)
        {
            CardSpec card = CardLibrary.Get(cardId);
            Color family = CardLibrary.FamilyColor(card.Family);
            bool removable = CanRemoveCard(cardId);
            int copies = runDeck.Count(candidate => candidate == cardId);
            Color shown = removable ? family : new Color32(92, 98, 112, 255);
            bool hovered = removable && rect.Contains(Event.current.mousePosition);
            if (hovered || selected)
                DrawNeonFrame(new Rect(rect.x - 5, rect.y - 5, rect.width + 10, rect.height + 10), NeonCyan, 3f);
            DrawRect(new Rect(rect.x + 6, rect.y + 6, rect.width, rect.height), Shadow);
            DrawRect(rect, new Color32(235, 232, 211, 255));
            DrawPixelOutline(rect, shown, 3f);
            DrawRect(new Rect(rect.x, rect.y, rect.width, 42), shown);
            DrawFittedLabel(new Rect(rect.x + 12, rect.y + 5, rect.width - 75, 34), card.Name, cardTitleStyle, 11);
            DrawFittedLabel(new Rect(rect.x + rect.width - 60, rect.y + 7, 48, 30), $"×{copies}", tinyStyle, 9);
            DrawFittedLabel(new Rect(rect.x + 14, rect.y + 54, rect.width - 28, 70), card.Rules, cardBodyStyle, 9);
            DrawFittedLabel(new Rect(rect.x + 14, rect.y + 128, rect.width - 28, 24),
                $"费用 {card.Cost}　热量 +{card.Heat}", tinyStyle, 8);
            DrawRect(new Rect(rect.x + 12, rect.y + 156, rect.width - 24, 22), new Color32(7, 16, 38, 235));
            DrawFittedLabel(new Rect(rect.x + 15, rect.y + 156, rect.width - 30, 22),
                "删除一张副本", hudCenteredStyle, 7);

            bool oldEnabled = GUI.enabled;
            GUI.enabled = removable;
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                RemoveCardFromDeck(cardId);
            GUI.enabled = oldEnabled;
        }

        private CardId[] PurgeCandidates()
        {
            return runDeck.Distinct()
                .OrderBy(card => CardLibrary.Get(card).Family)
                .ThenBy(card => CardLibrary.Get(card).Cost)
                .ThenBy(card => CardLibrary.Get(card).Name)
                .ToArray();
        }

        private bool CanRemoveCard(CardId card)
        {
            if (!runDeck.Contains(card))
                return false;
            if (screen == ScreenMode.ShopPurge)
                return !shopPurgeBought && credits >= RouteDecisionCatalog.ShopPurgeCost(selectedRouteNodeId);
            return true;
        }

        private void RemoveCardFromDeck(CardId card)
        {
            if (!CanRemoveCard(card))
                return;
            bool finalTrim = screen == ScreenMode.FinalTrim;
            bool shopPurge = screen == ScreenMode.ShopPurge;
            bool eventPurge = screen == ScreenMode.EventPurge;
            runDeck.Remove(card);
            if (!runDeck.Contains(card))
            {
                runUpgrades.Remove(card);
                runUpgradeBranches.Remove(card);
            }
            if (finalTrim)
            {
                PlaySound(clickSound, 0.78f, 0.65f);
                FinalizeApproach(FinalApproachPlan.DeadweightTrim,
                    $"deadweight_trim|card={card}|deck={runDeck.Count}");
                return;
            }
            if (eventPurge)
            {
                ResolveIndependentRouteEvent(card);
                return;
            }
            if (shopPurge)
            {
                int cost = RouteDecisionCatalog.ShopPurgeCost(selectedRouteNodeId);
                credits -= cost;
                shopPurgeBought = true;
                screen = ScreenMode.Shop;
                controllerSelection = 3;
                CaptureBuildSnapshot(RunBuildSnapshotMoment.ShopService, $"shop_{selectedRouteNodeId}_purge");
                PlayLayeredSound(clickSound, 0.72f, 0.62f, rewardSound, 1.16f, 0.48f);
                SaveRunCheckpoint();
                return;
            }
            restResolved = true;
            restResult = $"牌组精简完成：移除一张【{CardLibrary.Get(card).Name}】";
            screen = ScreenMode.Rest;
            controllerSelection = 0;
            CaptureBuildSnapshot(RunBuildSnapshotMoment.ServiceDock, $"service_{selectedRouteNodeId}");
            PlayLayeredSound(clickSound, 0.78f, 0.65f, rewardSound, 1.2f, 0.45f);
            SaveRunCheckpoint();
        }

        private void CancelDeckPurge()
        {
            screen = screen switch
            {
                ScreenMode.FinalTrim => ScreenMode.FinalApproach,
                ScreenMode.ShopPurge => ScreenMode.Shop,
                ScreenMode.EventPurge => ScreenMode.Event,
                _ => ScreenMode.Rest
            };
            controllerSelection = 0;
            SaveRunCheckpoint();
        }

        private void OpenCoreUpgrade()
        {
            if (restResolved)
                return;
            screen = ScreenMode.CoreUpgrade;
            controllerSelection = 0;
            SaveRunCheckpoint();
        }

        private void DrawCoreUpgradeScreen()
        {
            CardId core = ContractCatalog.StarterCard(selectedContract);
            CardSpec card = CardLibrary.Get(core);
            DrawRect(new Rect(120, 65, 1360, 760), new Color32(2, 7, 22, 255));
            DrawRect(new Rect(128, 73, 1344, 744), PanelNight);
            DrawNeonFrame(new Rect(128, 73, 1344, 744), NeonCyan, 3f);
            DrawFittedLabel(new Rect(205, 100, 830, 62), "核心牌分支校准", neonTitleStyle, 27);
            DrawFittedLabel(new Rect(210, 160, 1120, 48),
                $"选择【{card.Name}】的本局强化方向；全部同名副本共享该分支。", neonBodyStyle, 12);
            if (runUpgrades.Contains(core))
                DrawFittedLabel(new Rect(1090, 112, 290, 34), "将覆盖当前分支", hudCenteredStyle, 8);

            Rect alpha = new Rect(310, 245, 360, 390);
            Rect beta = new Rect(930, 245, 360, 390);
            if (controllerSelection == 0)
                DrawNeonFrame(new Rect(alpha.x - 10, alpha.y - 10, alpha.width + 20, alpha.height + 20), Color.white, 4f);
            if (controllerSelection == 1)
                DrawNeonFrame(new Rect(beta.x - 10, beta.y - 10, beta.width + 20, beta.height + 20), Color.white, 4f);
            DrawOfferCard(core, alpha, "写入 A 分支", true, () => ApplyCoreUpgrade(UpgradeBranch.Alpha),
                "A // 增幅分支", "A", $"{card.Name}+A", UpgradedRules(core, UpgradeBranch.Alpha));
            DrawOfferCard(core, beta, "写入 B 分支", true, () => ApplyCoreUpgrade(UpgradeBranch.Beta),
                "B // 机制分支", "B", $"{card.Name}+B", UpgradedRules(core, UpgradeBranch.Beta));
            DrawPixelButton(new Rect(610, 700, 380, 58), "取消校准，返回维修坞", Shadow, CancelCoreUpgrade, true, "B");
        }

        private void ApplyCoreUpgrade(UpgradeBranch branch)
        {
            CardId core = ContractCatalog.StarterCard(selectedContract);
            runUpgrades.Add(core);
            runUpgradeBranches[core] = branch;
            restResolved = true;
            restResult = $"核心校准完成：{CardLibrary.Get(core).Name}+{(branch == UpgradeBranch.Alpha ? "A" : "B")}";
            screen = ScreenMode.Rest;
            controllerSelection = 0;
            CaptureBuildSnapshot(RunBuildSnapshotMoment.ServiceDock, $"service_{selectedRouteNodeId}");
            PlayLayeredSound(rewardSound, branch == UpgradeBranch.Alpha ? 1.08f : 0.88f, 0.85f,
                clickSound, 1.4f, 0.45f);
            SaveRunCheckpoint();
        }

        private void CancelCoreUpgrade()
        {
            screen = ScreenMode.Rest;
            controllerSelection = 0;
            SaveRunCheckpoint();
        }

        private void DrawShopScreen()
        {
            DrawRect(new Rect(70, 55, 1460, 790), new Color32(2, 7, 22, 255));
            DrawRect(new Rect(78, 63, 1444, 774), PanelNight);
            DrawNeonFrame(new Rect(78, 63, 1444, 774), NeonCyan, 3f);
            DrawFittedLabel(new Rect(130, 90, 740, 70), route.Get(selectedRouteNodeId).Title, neonTitleStyle, 26);
            DrawFittedLabel(new Rect(135, 155, 900, 42),
                L("shop.subtitle.v049", "购买新牌，或在航线工坊中维修、删牌与重写卡牌分支。"),
                neonBodyStyle, 12);
            DrawFittedLabel(new Rect(1180, 105, 260, 50), $"邮票  {credits}", neonSubtitleStyle, 12);

            CardId[] offers = CurrentShopOffers();
            int[] prices = CurrentShopPrices();
            for (int i = 0; i < offers.Length; i++)
            {
                int offerIndex = i;
                bool available = !shopBought[i] && credits >= prices[i];
                string footer = shopBought[i] ? "已售出" : $"购买 · {prices[i]}邮票";
                DrawOfferCard(offers[i], new Rect(125 + i * 305, 245, 270, 315), footer, available, () =>
                {
                    TryBuyShopOffer(offerIndex);
                });
            }

            Rect servicePanel = new Rect(1045, 225, 390, 420);
            DrawRect(new Rect(servicePanel.x + 8, servicePanel.y + 8, servicePanel.width, servicePanel.height), Shadow);
            DrawRect(servicePanel, new Color32(6, 18, 42, 255));
            DrawPixelOutline(servicePanel, Gold, 3f);
            DrawFittedLabel(new Rect(servicePanel.x + 20, servicePanel.y + 14, servicePanel.width - 40, 34),
                L("shop.workbench", "ROUTE WORKBENCH // 航线工坊"), hudCenteredStyle, 9);

            int purgeCost = RouteDecisionCatalog.ShopPurgeCost(selectedRouteNodeId);
            int calibrationCost = RouteDecisionCatalog.ShopCalibrationCost(selectedRouteNodeId);
            DrawShopServiceRow(new Rect(servicePanel.x + 18, servicePanel.y + 60, servicePanel.width - 36, 102),
                L("shop.service.repair", "结构维修 // 恢复12机体"),
                repairBought ? L("shop.service.used", "本停靠已使用") :
                    L("shop.service.cost", "{0} 邮票", 20),
                new Color32(76, 157, 147, 255), !repairBought && credits >= 20 &&
                    runHull < BattleState.MaxPlayerHealth, TryBuyShopRepair, 3);
            DrawShopServiceRow(new Rect(servicePanel.x + 18, servicePanel.y + 174, servicePanel.width - 36, 102),
                L("shop.service.purge", "拆牌台 // 删除任意1张"),
                shopPurgeBought ? L("shop.service.used", "本停靠已使用") :
                    L("shop.service.cost", "{0} 邮票", purgeCost),
                NeonViolet, !shopPurgeBought && credits >= purgeCost && runDeck.Count > 0,
                OpenShopPurge, 4);
            DrawShopServiceRow(new Rect(servicePanel.x + 18, servicePanel.y + 288, servicePanel.width - 36, 102),
                L("shop.service.calibrate", "分支改写 // 任意卡牌 A / B"),
                shopCalibrationBought ? L("shop.service.used", "本停靠已使用") :
                    L("shop.service.cost", "{0} 邮票", calibrationCost),
                NeonCyan, !shopCalibrationBought && credits >= calibrationCost && runDeck.Count > 0,
                OpenWorkshopCardSelect, 5);

            DrawPixelButton(new Rect(590, 685, 420, 72), "离开补给站，继续航行", PostalRed, () =>
            {
                LeaveShop();
            });
            DrawRunHud(new Rect(1080, 670, 340, 110));
        }

        private void DrawShopServiceRow(Rect rect, string title, string status, Color color, bool enabled,
            Action action, int index)
        {
            Color shown = enabled ? color : new Color32(82, 91, 110, 255);
            DrawRect(rect, new Color32(3, 11, 30, 245));
            DrawPixelOutline(rect, controllerActive && controllerSelection == index ? Color.white : shown, 2f);
            DrawFittedLabel(new Rect(rect.x + 15, rect.y + 10, rect.width - 30, 30),
                title, neonBodyStyle, 9);
            DrawPixelButton(new Rect(rect.x + 15, rect.y + 51, rect.width - 30, 38),
                status, shown, action, enabled);
        }

        private void OpenShopPurge()
        {
            int cost = RouteDecisionCatalog.ShopPurgeCost(selectedRouteNodeId);
            if (shopPurgeBought || credits < cost || runDeck.Count == 0)
                return;
            screen = ScreenMode.ShopPurge;
            deckPurgePage = 0;
            controllerSelection = 0;
            SaveRunCheckpoint();
        }

        private void OpenWorkshopCardSelect()
        {
            int cost = RouteDecisionCatalog.ShopCalibrationCost(selectedRouteNodeId);
            if (shopCalibrationBought || credits < cost || runDeck.Count == 0)
                return;
            workshopCardValue = -1;
            workshopPage = 0;
            controllerSelection = 0;
            screen = ScreenMode.WorkshopCardSelect;
            SaveRunCheckpoint();
        }

        private CardId[] WorkshopCandidates()
        {
            return runDeck.Distinct()
                .OrderBy(card => CardLibrary.Get(card).Family)
                .ThenBy(card => CardLibrary.Get(card).Cost)
                .ThenBy(card => CardLibrary.Get(card).Name)
                .ToArray();
        }

        private void DrawWorkshopCardSelect()
        {
            const int pageSize = 10;
            CardId[] candidates = WorkshopCandidates();
            int pageCount = Mathf.Max(1, Mathf.CeilToInt(candidates.Length / (float)pageSize));
            workshopPage = Mathf.Clamp(workshopPage, 0, pageCount - 1);
            int start = workshopPage * pageSize;
            int end = Mathf.Min(candidates.Length, start + pageSize);

            DrawRect(new Rect(80, 52, 1440, 800), new Color32(2, 7, 22, 255));
            DrawRect(new Rect(88, 60, 1424, 784), PanelNight);
            DrawNeonFrame(new Rect(88, 60, 1424, 784), NeonCyan, 3f);
            DrawFittedLabel(new Rect(145, 88, 780, 62),
                L("workshop.select.title", "航线工坊：选择改写卡牌"), neonTitleStyle, 26);
            DrawFittedLabel(new Rect(150, 148, 1060, 42),
                L("workshop.select.subtitle", "选择一种现有卡牌，再决定写入A增幅分支或B机制分支；全部同名副本共享。"),
                neonBodyStyle, 11);
            DrawFittedLabel(new Rect(1190, 102, 250, 38),
                L("workshop.cost", "费用 {0} 邮票", RouteDecisionCatalog.ShopCalibrationCost(selectedRouteNodeId)),
                hudCenteredStyle, 8);

            for (int i = start; i < end; i++)
            {
                int candidateIndex = i;
                int local = i - start;
                Rect rect = new Rect(135 + (local % 5) * 275, 225 + (local / 5) * 220, 245, 185);
                DrawWorkshopCard(candidates[i], rect, controllerSelection == i,
                    () => SelectWorkshopCard(candidates[candidateIndex]));
            }

            DrawPixelButton(new Rect(130, 694, 100, 48), "<", Shadow,
                () => workshopPage = Mathf.Max(0, workshopPage - 1), workshopPage > 0);
            DrawFittedLabel(new Rect(250, 700, 220, 36),
                L("purge.page", "牌页 {0} / {1}", workshopPage + 1, pageCount), hudCenteredStyle, 9);
            DrawPixelButton(new Rect(490, 694, 100, 48), ">", Shadow,
                () => workshopPage = Mathf.Min(pageCount - 1, workshopPage + 1), workshopPage < pageCount - 1);
            DrawPixelButton(new Rect(1050, 690, 360, 58),
                L("workshop.cancel", "取消改写，返回补给站"), Shadow, CancelWorkshop, true, "B");
            DrawFittedLabel(new Rect(220, 770, 1160, 38),
                L("workshop.select.note", "改写不会增加卡牌副本；已强化卡牌可以切换分支。"),
                tinyStyle, 9);
        }

        private void DrawWorkshopCard(CardId cardId, Rect rect, bool selected, Action action)
        {
            CardSpec card = CardLibrary.Get(cardId);
            Color family = CardLibrary.FamilyColor(card.Family);
            int copies = runDeck.Count(candidate => candidate == cardId);
            bool hovered = rect.Contains(Event.current.mousePosition);
            if (hovered || selected)
                DrawNeonFrame(new Rect(rect.x - 5, rect.y - 5, rect.width + 10, rect.height + 10), NeonCyan, 3f);
            DrawRect(new Rect(rect.x + 6, rect.y + 6, rect.width, rect.height), Shadow);
            DrawRect(rect, new Color32(235, 232, 211, 255));
            DrawPixelOutline(rect, family, 3f);
            DrawRect(new Rect(rect.x, rect.y, rect.width, 42), family);
            DrawFittedLabel(new Rect(rect.x + 12, rect.y + 5, rect.width - 75, 34), card.Name, cardTitleStyle, 11);
            DrawFittedLabel(new Rect(rect.x + rect.width - 60, rect.y + 7, 48, 30), $"×{copies}", tinyStyle, 9);
            DrawFittedLabel(new Rect(rect.x + 14, rect.y + 54, rect.width - 28, 70), card.Rules, cardBodyStyle, 9);
            string branch = runUpgradeBranches.TryGetValue(cardId, out UpgradeBranch current)
                ? $"+{(current == UpgradeBranch.Alpha ? "A" : "B")}"
                : L("workshop.unmodified", "未改写");
            DrawRect(new Rect(rect.x + 12, rect.y + 150, rect.width - 24, 25), new Color32(7, 16, 38, 235));
            DrawFittedLabel(new Rect(rect.x + 15, rect.y + 152, rect.width - 30, 21),
                branch, hudCenteredStyle, 7);
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                action?.Invoke();
        }

        private void SelectWorkshopCard(CardId card)
        {
            if (!runDeck.Contains(card))
                return;
            workshopCardValue = (int)card;
            controllerSelection = 0;
            screen = ScreenMode.WorkshopBranch;
            SaveRunCheckpoint();
        }

        private void DrawWorkshopBranch()
        {
            if (workshopCardValue < 0 || !Enum.IsDefined(typeof(CardId), workshopCardValue) ||
                !runDeck.Contains((CardId)workshopCardValue))
            {
                screen = ScreenMode.WorkshopCardSelect;
                workshopCardValue = -1;
                return;
            }

            CardId cardId = (CardId)workshopCardValue;
            CardSpec card = CardLibrary.Get(cardId);
            int cost = RouteDecisionCatalog.ShopCalibrationCost(selectedRouteNodeId);
            DrawRect(new Rect(120, 65, 1360, 760), new Color32(2, 7, 22, 255));
            DrawRect(new Rect(128, 73, 1344, 744), PanelNight);
            DrawNeonFrame(new Rect(128, 73, 1344, 744), NeonCyan, 3f);
            DrawFittedLabel(new Rect(205, 100, 830, 62),
                L("workshop.branch.title", "卡牌分支改写"), neonTitleStyle, 27);
            DrawFittedLabel(new Rect(210, 160, 1120, 48),
                L("workshop.branch.subtitle", "为全部【{0}】写入分支；确认后支付{1}邮票。",
                    card.Name, cost), neonBodyStyle, 12);
            if (runUpgrades.Contains(cardId))
                DrawFittedLabel(new Rect(1090, 112, 290, 34),
                    L("workshop.overwrite", "将覆盖当前分支"), hudCenteredStyle, 8);

            Rect alpha = new Rect(310, 245, 360, 390);
            Rect beta = new Rect(930, 245, 360, 390);
            if (controllerSelection == 0)
                DrawNeonFrame(new Rect(alpha.x - 10, alpha.y - 10, alpha.width + 20, alpha.height + 20), Color.white, 4f);
            if (controllerSelection == 1)
                DrawNeonFrame(new Rect(beta.x - 10, beta.y - 10, beta.width + 20, beta.height + 20), Color.white, 4f);
            DrawOfferCard(cardId, alpha, L("workshop.write_alpha", "写入 A 分支"), credits >= cost,
                () => ApplyWorkshopBranch(UpgradeBranch.Alpha), "A // 增幅分支", "A",
                $"{card.Name}+A", UpgradedRules(cardId, UpgradeBranch.Alpha));
            DrawOfferCard(cardId, beta, L("workshop.write_beta", "写入 B 分支"), credits >= cost,
                () => ApplyWorkshopBranch(UpgradeBranch.Beta), "B // 机制分支", "B",
                $"{card.Name}+B", UpgradedRules(cardId, UpgradeBranch.Beta));
            DrawPixelButton(new Rect(610, 700, 380, 58),
                L("workshop.branch.back", "返回卡牌清单"), Shadow, BackToWorkshopCards, true, "B");
        }

        private void ApplyWorkshopBranch(UpgradeBranch branch)
        {
            int cost = RouteDecisionCatalog.ShopCalibrationCost(selectedRouteNodeId);
            if (shopCalibrationBought || credits < cost || workshopCardValue < 0 ||
                !Enum.IsDefined(typeof(CardId), workshopCardValue))
                return;
            CardId card = (CardId)workshopCardValue;
            if (!runDeck.Contains(card))
                return;
            credits -= cost;
            runUpgrades.Add(card);
            runUpgradeBranches[card] = branch;
            shopCalibrationBought = true;
            screen = ScreenMode.Shop;
            controllerSelection = 5;
            CaptureBuildSnapshot(RunBuildSnapshotMoment.ShopService,
                $"shop_{selectedRouteNodeId}_calibration");
            PlayLayeredSound(rewardSound, branch == UpgradeBranch.Alpha ? 1.04f : 0.9f, 0.82f,
                clickSound, 1.36f, 0.42f);
            SaveRunCheckpoint();
        }

        private void BackToWorkshopCards()
        {
            workshopCardValue = -1;
            screen = ScreenMode.WorkshopCardSelect;
            controllerSelection = 0;
            SaveRunCheckpoint();
        }

        private void CancelWorkshop()
        {
            workshopCardValue = -1;
            screen = ScreenMode.Shop;
            controllerSelection = 0;
            SaveRunCheckpoint();
        }

        private CardId[] CurrentShopOffers()
        {
            int offerSeed = runSeed ^ selectedRouteNodeId * 83492791 ^ 0x51A7;
            return CardOfferCatalog.Select(selectedContract, CurrentAirspace(), offerSeed, 3, runDeck);
        }

        private int[] CurrentShopPrices()
        {
            return selectedContract == CargoContract.StormCore ? new[] { 20, 20, 35 } :
                selectedContract == CargoContract.SignalSeed ? new[] { 25, 20, 25 } :
                new[] { 25, 20, 35 };
        }

        private void TryBuyShopOffer(int index)
        {
            CardId[] offers = CurrentShopOffers();
            int[] prices = CurrentShopPrices();
            if (index < 0 || index >= offers.Length || shopBought[index] || credits < prices[index])
                return;
            credits -= prices[index];
            runDeck.Add(offers[index]);
            shopBought[index] = true;
            CaptureBuildSnapshot(RunBuildSnapshotMoment.ShopService,
                $"shop_{selectedRouteNodeId}_card_{index}");
            DeliveryArchiveService.RegisterRewardDiscoveries(archiveData,
                runDeck.Select(card => (int)card), runModules.Select(module => (int)module));
            SaveArchive();
            SaveRunCheckpoint();
        }

        private void TryBuyShopRepair()
        {
            if (repairBought || credits < 20 || runHull >= BattleState.MaxPlayerHealth)
                return;
            credits -= 20;
            runHull = Mathf.Min(BattleState.MaxPlayerHealth, runHull + 12);
            repairBought = true;
            CaptureBuildSnapshot(RunBuildSnapshotMoment.ShopService,
                $"shop_{selectedRouteNodeId}_repair");
            SaveRunCheckpoint();
        }

        private void LeaveShop()
        {
            AdvanceAfterCurrentRouteNode();
            SaveRunCheckpoint();
        }

        private void DrawArchiveScreen()
        {
            archiveData ??= new DeliveryArchiveData();
            DeliveryArchiveService.Normalize(archiveData);
            DrawRect(new Rect(60, 42, 1480, 800), new Color32(2, 7, 22, 255));
            DrawRect(new Rect(68, 50, 1464, 784), PanelNight);
            DrawNeonFrame(new Rect(68, 50, 1464, 784), Gold, 3f);
            DrawRect(new Rect(68, 50, 1464, 10), NeonViolet);
            DrawFittedLabel(new Rect(115, 78, 700, 62), L("archive.title", "群岛邮政档案"), neonTitleStyle, 28);
            DrawFittedLabel(new Rect(1080, 88, 360, 38), "CAREER // LOCAL PROFILE", hudCenteredStyle, 9);

            DrawPixelButton(new Rect(680, 120, 180, 38), L("archive.tab.overview", "履历总览"),
                archivePage == 0 ? NeonCyan : Shadow, () => archivePage = 0);
            DrawPixelButton(new Rect(870, 120, 180, 38), L("archive.tab.progression", "挑战精通"),
                archivePage == 1 ? Gold : Shadow, () => archivePage = 1);
            DrawPixelButton(new Rect(1060, 120, 180, 38), L("archive.tab.dossiers", "首领终局"),
                archivePage == 2 ? NeonViolet : Shadow, () => archivePage = 2);
            DrawPixelButton(new Rect(1250, 120, 180, 38), L("archive.tab.stats", "胜率分析"),
                archivePage == 3 ? PostalRed : Shadow, () => archivePage = 3);

            if (archivePage == 1)
            {
                DrawArchiveProgressionPage();
                DrawPixelButton(new Rect(640, 772, 320, 56), L("archive.back", "返回标题"), Shadow,
                    () => screen = ScreenMode.Title, true, "ESC");
                return;
            }
            if (archivePage == 2)
            {
                DrawArchiveDossierPage();
                DrawPixelButton(new Rect(640, 772, 320, 56), L("archive.back", "返回标题"), Shadow,
                    () => screen = ScreenMode.Title, true, "ESC");
                return;
            }
            if (archivePage == 3)
            {
                DrawArchiveStatisticsPage();
                DrawPixelButton(new Rect(640, 772, 320, 56), L("archive.back", "返回标题"), Shadow,
                    () => screen = ScreenMode.Title, true, "ESC");
                return;
            }

            Rect careerPanel = new Rect(105, 155, 410, 565);
            Rect collectionPanel = new Rect(540, 155, 430, 565);
            Rect historyPanel = new Rect(995, 155, 495, 565);
            DrawArchivePanel(careerPanel, L("archive.career", "邮差履历"), NeonCyan);
            DrawArchivePanel(collectionPanel, L("archive.collection", "发现图鉴"), NeonViolet);
            DrawArchivePanel(historyPanel, L("archive.recent", "最近记录"), Gold);

            DrawFittedLabel(new Rect(135, 210, 350, 42),
                DeliveryArchiveService.CourierRank(archiveData), neonSubtitleStyle, 13);
            DrawFittedLabel(new Rect(135, 258, 350, 30),
                $"{L("archive.started", "出发 {0}", archiveData.RunsStarted)}　" +
                $"{L("archive.delivered", "送达 {0}", archiveData.DeliveriesCompleted)}　" +
                L("archive.lost", "失事 {0}", archiveData.EncountersLost),
                tinyStyle, 9);
            DrawFittedLabel(new Rect(135, 292, 350, 30),
                $"{L("archive.battles", "战斗胜利 {0}", archiveData.BattlesWon)}　" +
                L("archive.best_cargo", "最佳货物 {0}", ArchiveBestCargoLabel()),
                tinyStyle, 9);
            DrawFittedLabel(new Rect(135, 326, 350, 30),
                $"{L("archive.best_credits", "最高邮票 {0}", archiveData.BestCredits)}　" +
                L("archive.total_turns", "累计回合 {0}", archiveData.TotalTurns),
                tinyStyle, 9);
            DrawFittedLabel(new Rect(135, 354, 350, 22),
                L("archive.total_cards", "累计出牌 {0}", archiveData.TotalCardsPlayed), tinyStyle, 8);

            DrawArchiveBadge(new Rect(130, 380, 360, 54), L("badge.first.title", "首航签章"),
                L("badge.first.desc", "完成第一次出发登记"),
                archiveData.RunsStarted > 0);
            DrawArchiveBadge(new Rect(130, 442, 360, 54), L("badge.boss.title", "风暴见证"),
                L("badge.boss.desc", "亲眼发现任意终局首领"),
                archiveData.DiscoveredEnemies.Contains((int)EnemyKind.StormManta) ||
                archiveData.DiscoveredEnemies.Contains((int)EnemyKind.CloudWyrm));
            DrawArchiveBadge(new Rect(130, 504, 360, 54), L("badge.perfect.title", "完好送达"),
                L("badge.perfect.desc", "以完整货物完成配送"),
                archiveData.BestCargoIntegrity >= 3);
            DrawArchiveBadge(new Rect(130, 566, 360, 54), L("badge.resolve.title", "不屈航线"),
                L("badge.resolve.desc", "失事后仍完成过配送"),
                archiveData.EncountersLost > 0 && archiveData.DeliveriesCompleted > 0);
            DrawArchiveBadge(new Rect(130, 628, 360, 54), L("badge.contracts.title", "五方合同"),
                L("badge.contracts.desc", "登记全部五种合同"),
                archiveData.DiscoveredContracts.Count >= Enum.GetValues(typeof(CargoContract)).Length);

            int cardTotal = Enum.GetValues(typeof(CardId)).Length;
            int moduleTotal = Enum.GetValues(typeof(ModuleId)).Length;
            int enemyTotal = Enum.GetValues(typeof(EnemyKind)).Length;
            int contractTotal = Enum.GetValues(typeof(CargoContract)).Length;
            int endingTotal = Enum.GetValues(typeof(FinaleEnding)).Length - 1;
            DrawArchiveProgress(new Rect(570, 210, 370, 42), L("archive.contracts", "合同"),
                archiveData.DiscoveredContracts.Count, contractTotal, NeonCyan);
            DrawArchiveProgress(new Rect(570, 262, 370, 42), L("archive.cards", "卡牌"),
                archiveData.DiscoveredCards.Count, cardTotal, PostalRed);
            DrawArchiveProgress(new Rect(570, 314, 370, 42), L("archive.modules", "模块"),
                archiveData.DiscoveredModules.Count, moduleTotal, Gold);
            DrawArchiveProgress(new Rect(570, 366, 370, 42), L("archive.enemies", "敌机"),
                archiveData.DiscoveredEnemies.Count, enemyTotal, NeonViolet);
            DrawArchiveProgress(new Rect(570, 418, 370, 42), L("archive.endings", "终局"),
                archiveData.DiscoveredEndings.Count, endingTotal, Gold);
            DrawFittedLabel(new Rect(570, 470, 370, 22), L("archive.known_contracts", "已登记合同"), tinyStyle, 8);
            DrawFittedLabel(new Rect(570, 492, 370, 44), ArchiveContractSummary(), neonBodyStyle, 8);
            DrawFittedLabel(new Rect(570, 542, 370, 22), L("archive.recent_cards", "最近发现卡牌"), tinyStyle, 8);
            DrawFittedLabel(new Rect(570, 564, 370, 54), ArchiveCardSummary(), neonBodyStyle, 8);
            DrawFittedLabel(new Rect(570, 626, 370, 22), L("archive.known_enemies", "已发现敌机"), tinyStyle, 8);
            DrawFittedLabel(new Rect(570, 648, 370, 47), ArchiveEnemySummary(), neonBodyStyle, 8);

            if (archiveData.RecentRuns.Count == 0)
            {
                DrawFittedLabel(new Rect(1040, 315, 405, 120),
                    L("archive.empty", "尚无配送记录。\n完成一次配送或遭遇失事后，\n航线摘要会保存在这里。"),
                    neonBodyStyle, 11);
            }
            else
            {
                int shown = Mathf.Min(5, archiveData.RecentRuns.Count);
                for (int i = 0; i < shown; i++)
                    DrawArchivedRun(new Rect(1025, 205 + i * 97, 435, 82), archiveData.RecentRuns[i]);
            }

            DrawPixelButton(new Rect(640, 772, 320, 56), L("archive.back", "返回标题"), Shadow,
                () => screen = ScreenMode.Title, true, "ESC");
        }

        private void DrawArchiveStatisticsPage()
        {
            DrawFittedLabel(new Rect(115, 170, 1100, 34),
                L("stats.header", "已结算尝试胜率 // 仅统计有明确终局的本地记录"),
                neonSubtitleStyle, 12);
            DrawFittedLabel(new Rect(1110, 173, 360, 28),
                L("stats.note", "Boss 仅统计实际到达样本"), tinyStyle, 8);
            DrawWinRatePanel(new Rect(105, 215, 680, 235),
                L("stats.contract", "合同胜率"), DeliveryArchiveService.ContractDimension, NeonCyan);
            DrawWinRatePanel(new Rect(815, 215, 680, 235),
                L("stats.build", "构筑胜率"), DeliveryArchiveService.BuildDimension, NeonViolet);
            DrawWinRatePanel(new Rect(105, 475, 680, 235),
                L("stats.route", "路线胜率"), DeliveryArchiveService.RouteDimension, Gold);
            DrawWinRatePanel(new Rect(815, 475, 680, 235),
                L("stats.boss", "Boss 胜率 // 实际到达"), DeliveryArchiveService.BossDimension,
                PostalRed);
        }

        private void DrawWinRatePanel(Rect rect, string title, string dimension, Color color)
        {
            DrawArchivePanel(rect, title, color);
            RunWinRateRecord[] records = (archiveData.PerformanceStats ?? new List<RunWinRateRecord>())
                .Where(record => record != null && record.Dimension == dimension && record.Attempts > 0)
                .OrderByDescending(record => record.Attempts)
                .ThenBy(record => record.Key)
                .Take(5)
                .ToArray();
            if (records.Length == 0)
            {
                DrawFittedLabel(new Rect(rect.x + 30, rect.y + 85, rect.width - 60, 75),
                    L("stats.empty", "尚无可统计的已结算样本。\n完成配送或记录一次失事后，这里会自动更新。"),
                    neonBodyStyle, 9);
                return;
            }

            for (int i = 0; i < records.Length; i++)
            {
                RunWinRateRecord record = records[i];
                Rect row = new Rect(rect.x + 24, rect.y + 52 + i * 34, rect.width - 48, 29);
                DrawRect(row, new Color32(8, 21, 43, 245));
                DrawRect(new Rect(row.x, row.yMax - 3,
                    row.width * Mathf.Clamp01(record.Wins / (float)record.Attempts), 3), color);
                DrawFittedLabel(new Rect(row.x + 12, row.y + 3, row.width - 240, 22),
                    WinRateKeyLabel(dimension, record.Key), tinyStyle, 8);
                DrawFittedLabel(new Rect(row.xMax - 220, row.y + 3, 205, 22),
                    L("stats.ratio", "{0}/{1}　{2:0}%", record.Wins, record.Attempts,
                        record.Wins * 100f / record.Attempts), hudCenteredStyle, 8);
            }
        }

        private static string WinRateKeyLabel(string dimension, string key)
        {
            if (dimension == DeliveryArchiveService.ContractDimension &&
                int.TryParse(key, out int contract) && Enum.IsDefined(typeof(CargoContract), contract))
                return CargoName((CargoContract)contract);
            if (dimension == DeliveryArchiveService.BossDimension &&
                int.TryParse(key, out int boss) && Enum.IsDefined(typeof(EnemyKind), boss))
                return ArchiveEnemyName((EnemyKind)boss);
            if (dimension == DeliveryArchiveService.BuildDimension)
                return BuildProfileLabel(key);
            if (dimension == DeliveryArchiveService.RouteDimension)
                return RouteProfileLabel(key);
            return key;
        }

        private void DrawArchiveProgressionPage()
        {
            DrawFittedLabel(new Rect(115, 170, 600, 34),
                L("archive.challenge_header", "固定种子挑战"), neonSubtitleStyle, 12);
            ChallengeDefinition[] challenges = ChallengeCatalog.All
                .Where(challenge => challenge.Id != ChallengeId.Standard).ToArray();
            for (int i = 0; i < challenges.Length; i++)
            {
                ChallengeDefinition challenge = challenges[i];
                ChallengeProgressRecord progress = archiveData.ChallengeProgress.FirstOrDefault(record =>
                    record.Challenge == (int)challenge.Id);
                Rect rect = new Rect(105 + i * 475, 212, 430, 150);
                bool complete = (progress?.Completions ?? 0) > 0;
                Color color = complete ? new Color32(83, 220, 158, 255) : Gold;
                DrawRect(rect, new Color32(7, 18, 43, 245));
                DrawPixelOutline(rect, color, 2f);
                DrawRect(new Rect(rect.x, rect.y, 7, rect.height), color);
                DrawFittedLabel(new Rect(rect.x + 22, rect.y + 12, rect.width - 44, 30),
                    ChallengeName(challenge.Id), hudStyle, 11);
                DrawFittedLabel(new Rect(rect.x + 22, rect.y + 48, rect.width - 44, 52),
                    ChallengeRule(challenge.Id), tinyStyle, 8);
                DrawFittedLabel(new Rect(rect.x + 22, rect.y + 108, rect.width - 44, 25),
                    ChallengeProgressLabel(challenge.Id, progress), tinyStyle, 8);
            }

            Rect masteryPanel = new Rect(105, 395, 835, 325);
            DrawArchivePanel(masteryPanel, L("archive.mastery", "合同精通"), NeonCyan);
            for (int i = 0; i < ContractCatalog.All.Count; i++)
            {
                CargoContract contract = ContractCatalog.All[i];
                ContractMasteryRecord mastery = archiveData.ContractMastery.FirstOrDefault(record =>
                    record.Contract == (int)contract);
                DrawContractMasteryRow(new Rect(128, 449 + i * 50, 790, 41), contract, mastery);
            }

            Rect achievementPanel = new Rect(965, 395, 525, 325);
            DrawArchivePanel(achievementPanel, L("archive.honors", "长期荣誉"), Gold);
            AchievementId[] achievements = Enum.GetValues(typeof(AchievementId)).Cast<AchievementId>().ToArray();
            for (int i = 0; i < achievements.Length; i++)
                DrawAchievementRow(new Rect(988, 449 + i * 50, 479, 41), achievements[i]);
        }

        private void DrawContractMasteryRow(Rect rect, CargoContract contract, ContractMasteryRecord mastery)
        {
            int level = LongTermProgressionRules.MasteryLevel(mastery);
            int points = LongTermProgressionRules.MasteryPoints(mastery);
            int next = LongTermProgressionRules.MasteryLevelThreshold(Math.Min(4, level + 1));
            float ratio = level >= 4 ? 1f : Mathf.InverseLerp(
                LongTermProgressionRules.MasteryLevelThreshold(level), next, points);
            Color color = CargoColor(contract);
            DrawRect(rect, new Color32(8, 22, 45, 245));
            DrawRect(new Rect(rect.x, rect.yMax - 5, rect.width * ratio, 4), color);
            DrawFittedLabel(new Rect(rect.x + 14, rect.y + 7, 210, 25), CargoName(contract), tinyStyle, 9);
            DrawFittedLabel(new Rect(rect.x + 230, rect.y + 7, 130, 25),
                L("mastery.level", "精通 {0}", level), hudCenteredStyle, 8);
            DrawFittedLabel(new Rect(rect.x + 375, rect.y + 7, 390, 25),
                L("mastery.stats", "出发 {0}　送达 {1}　完好 {2}　挑战 {3}",
                    mastery?.Runs ?? 0, mastery?.Deliveries ?? 0, mastery?.PristineDeliveries ?? 0,
                    mastery?.ChallengeDeliveries ?? 0), tinyStyle, 8);
        }

        private void DrawAchievementRow(Rect rect, AchievementId achievement)
        {
            bool unlocked = LongTermProgressionRules.AchievementUnlocked(archiveData, achievement);
            Color color = unlocked ? Gold : new Color32(67, 75, 96, 255);
            DrawRect(rect, new Color32(8, 22, 45, 245));
            DrawPixelOutline(rect, color, 1f);
            DrawRect(new Rect(rect.x + 10, rect.y + 10, 20, 20), color);
            DrawFittedLabel(new Rect(rect.x + 42, rect.y + 5, 175, 29),
                unlocked ? AchievementName(achievement) : L("achievement.locked", "未解锁荣誉"), tinyStyle, 8);
            DrawFittedLabel(new Rect(rect.x + 225, rect.y + 5, 235, 29),
                AchievementRule(achievement), tinyStyle, 7);
        }

        private void DrawArchiveDossierPage()
        {
            EnemyKind[] bosses = { EnemyKind.StormManta, EnemyKind.CloudWyrm };
            for (int i = 0; i < bosses.Length; i++)
            {
                EnemyKind boss = bosses[i];
                BossDossierRecord dossier = archiveData.BossDossiers.FirstOrDefault(record =>
                    record.Boss == (int)boss);
                Rect rect = new Rect(105 + i * 480, 185, 445, 230);
                Color color = boss == EnemyKind.CloudWyrm ? NeonCyan : NeonViolet;
                DrawRect(rect, new Color32(7, 18, 43, 245));
                DrawPixelOutline(rect, color, 2f);
                DrawRect(new Rect(rect.x, rect.y, 8, rect.height), color);
                DrawFittedLabel(new Rect(rect.x + 25, rect.y + 15, rect.width - 50, 38),
                    ArchiveEnemyName(boss), neonSubtitleStyle, 12);
                DrawFittedLabel(new Rect(rect.x + 25, rect.y + 62, rect.width - 50, 28),
                    L("dossier.stats", "遭遇 {0}　击破 {1}", dossier?.Encounters ?? 0, dossier?.Victories ?? 0),
                    hudCenteredStyle, 9);
                DrawFittedLabel(new Rect(rect.x + 25, rect.y + 101, rect.width - 50, 67),
                    BossDossierRule(boss), neonBodyStyle, 9);
                DrawFittedLabel(new Rect(rect.x + 25, rect.y + 178, rect.width - 50, 30),
                    L("dossier.endings", "已解析终局 {0}/3", dossier?.Endings?.Count ?? 0), tinyStyle, 8);
            }

            Rect endingPanel = new Rect(1085, 185, 405, 535);
            DrawArchivePanel(endingPanel, L("archive.endings", "六类终局"), Gold);
            FinaleEnding[] endings = Enum.GetValues(typeof(FinaleEnding)).Cast<FinaleEnding>()
                .Where(ending => ending != FinaleEnding.None).ToArray();
            for (int i = 0; i < endings.Length; i++)
            {
                FinaleEnding ending = endings[i];
                bool discovered = archiveData.DiscoveredEndings.Contains((int)ending);
                Rect row = new Rect(1110, 245 + i * 66, 355, 52);
                Color color = discovered ? Gold : new Color32(65, 73, 94, 255);
                DrawRect(row, new Color32(8, 22, 45, 245));
                DrawPixelOutline(row, color, 1f);
                DrawFittedLabel(new Rect(row.x + 16, row.y + 5, row.width - 32, 22),
                    discovered ? FinaleEndingName(ending) : L("ending.unknown", "未解析终局"), tinyStyle, 8);
                DrawFittedLabel(new Rect(row.x + 16, row.y + 27, row.width - 32, 18),
                    EndingRouteLabel(ending), tinyStyle, 7);
            }

            Rect goalsPanel = new Rect(105, 445, 925, 275);
            DrawArchivePanel(goalsPanel, L("archive.next_goals", "下一局目标"), NeonCyan);
            List<ProgressGoal> goals = LongTermProgressionRules.NextGoals(archiveData, 3);
            if (goals.Count == 0)
            {
                DrawFittedLabel(new Rect(145, 530, 845, 80),
                    L("goal.complete", "全部长期目标均已完成；可以继续刷新挑战成绩。"),
                    neonBodyStyle, 11);
            }
            else
            {
                for (int i = 0; i < goals.Count; i++)
                DrawGoalRow(new Rect(140, 510 + i * 61, 855, 48), goals[i]);
            }
        }

        private void DrawGoalRow(Rect rect, ProgressGoal goal)
        {
            float ratio = goal.Target <= 0 ? 0f : Mathf.Clamp01(goal.Current / (float)goal.Target);
            DrawRect(rect, new Color32(8, 22, 45, 245));
            DrawPixelOutline(rect, NeonCyan, 1f);
            DrawRect(new Rect(rect.x + 3, rect.yMax - 6, (rect.width - 6) * ratio, 3), NeonCyan);
            DrawFittedLabel(new Rect(rect.x + 16, rect.y + 8, rect.width - 32, 28),
                ProgressGoalLabel(goal), tinyStyle, 9);
        }

        private void DrawArchivePanel(Rect rect, string title, Color color)
        {
            DrawRect(rect, new Color32(5, 14, 35, 245));
            DrawPixelOutline(rect, color, 2f);
            DrawRect(new Rect(rect.x, rect.y, rect.width, 42), new Color32(10, 27, 55, 250));
            DrawFittedLabel(new Rect(rect.x + 18, rect.y + 7, rect.width - 36, 28), title, hudStyle, 12);
        }

        private void DrawArchiveBadge(Rect rect, string title, string description, bool unlocked)
        {
            Color color = unlocked ? Gold : new Color32(72, 82, 105, 255);
            DrawRect(rect, unlocked ? new Color32(38, 31, 35, 245) : new Color32(10, 19, 37, 235));
            DrawPixelOutline(rect, color, 2f);
            DrawRect(new Rect(rect.x + 10, rect.y + 11, 30, 30), unlocked ? color : new Color32(43, 51, 69, 255));
            DrawFittedLabel(new Rect(rect.x + 52, rect.y + 6, rect.width - 65, 23),
                unlocked ? title : L("archive.locked_badge", "未解锁签章"), tinyStyle, 9);
            DrawFittedLabel(new Rect(rect.x + 52, rect.y + 29, rect.width - 65, 19), description, tinyStyle, 8);
        }

        private void DrawArchiveProgress(Rect rect, string label, int current, int total, Color color)
        {
            DrawRect(rect, new Color32(8, 19, 40, 245));
            DrawPixelOutline(rect, color, 2f);
            float ratio = total <= 0 ? 0f : Mathf.Clamp01(current / (float)total);
            DrawRect(new Rect(rect.x + 3, rect.yMax - 7, (rect.width - 6) * ratio, 4), color);
            DrawFittedLabel(new Rect(rect.x + 14, rect.y + 7, rect.width - 28, 25),
                $"{label}　{current}/{total}", tinyStyle, 9);
        }

        private void DrawArchivedRun(Rect rect, ArchivedRunRecord record)
        {
            bool delivered = record.Outcome == "DELIVERED";
            Color color = delivered ? new Color32(83, 220, 158, 255) : PostalRed;
            DrawRect(rect, new Color32(8, 19, 40, 245));
            DrawRect(new Rect(rect.x, rect.y, 7, rect.height), color);
            DrawPixelOutline(rect, color, 2f);
            string result = delivered
                ? $"{L("archive.delivered_result", "送达 · {0}级货物", CargoGrade(record.CargoIntegrity))} · " +
                  $"{(Enum.IsDefined(typeof(FinaleEnding), record.FinaleEnding) ? FinaleEndingName((FinaleEnding)record.FinaleEnding) : "终局未记录")}"
                : L("archive.lost_result", "失事 · {0}", ArchiveFailureLabel(record));
            DrawFittedLabel(new Rect(rect.x + 18, rect.y + 8, 240, 26), result, tinyStyle, 9);
            DrawFittedLabel(new Rect(rect.x + 265, rect.y + 8, 150, 26), $"SEED {record.RunSeed:X8}", tinyStyle, 8);
            DrawFittedLabel(new Rect(rect.x + 18, rect.y + 40, 395, 26),
                L("archive.run_line", "{0}　航点 {1:00}　回合 {2}　牌组 {3}",
                    ArchiveContractLabel(record.Contract), record.RouteNodeId + 1, record.Turns, record.DeckCount),
                tinyStyle, 8);
        }

        private string ArchiveBestCargoLabel()
        {
            return archiveData.BestCargoIntegrity < 0 ? "--" :
                $"{CargoGrade(archiveData.BestCargoIntegrity)} / {CargoStatus(archiveData.BestCargoIntegrity)}";
        }

        private string ArchiveContractSummary()
        {
            if (archiveData.DiscoveredContracts.Count == 0)
                return L("archive.wait_contract", "等待首次合同登记");
            return string.Join(" · ", archiveData.DiscoveredContracts
                .Where(value => Enum.IsDefined(typeof(CargoContract), value))
                .Select(value => CargoName((CargoContract)value)));
        }

        private string ArchiveCardSummary()
        {
            if (archiveData.DiscoveredCards.Count == 0)
                return L("archive.wait_deck", "等待首次牌组登记");
            string[] names = archiveData.DiscoveredCards
                .Where(value => Enum.IsDefined(typeof(CardId), value))
                .Select(value => CardLibrary.Get((CardId)value).Name)
                .Take(10).ToArray();
            string suffix = archiveData.DiscoveredCards.Count > names.Length ? " · …" : string.Empty;
            return string.Join(" · ", names) + suffix;
        }

        private string ArchiveEnemySummary()
        {
            if (archiveData.DiscoveredEnemies.Count == 0)
                return L("archive.wait_enemy", "等待首次敌情登记");
            return string.Join(" · ", archiveData.DiscoveredEnemies
                .Where(value => Enum.IsDefined(typeof(EnemyKind), value))
                .Select(value => ArchiveEnemyName((EnemyKind)value)));
        }

        private static string ArchiveContractLabel(int value)
        {
            return Enum.IsDefined(typeof(CargoContract), value)
                ? CargoName((CargoContract)value)
                : L("archive.unknown_contract", "未知合同");
        }

        private static string ArchiveEncounterLabel(int value)
        {
            if (!Enum.IsDefined(typeof(EncounterId), value))
                return L("archive.unknown_encounter", "未知遭遇");
            return (EncounterId)value switch
            {
                EncounterId.Elite => L("encounter.elite", "精英封锁"),
                EncounterId.Hunt => L("encounter.hunt", "追猎空域"),
                EncounterId.Boss => L("encounter.boss", "磁暴核心"),
                _ => L("encounter.standard", "航线拦截")
            };
        }

        private static string ArchiveFailureLabel(ArchivedRunRecord record)
        {
            if (record != null && Enum.IsDefined(typeof(PlayerDamageSource), record.DefeatSource))
                return FailureCauseTitle((PlayerDamageSource)record.DefeatSource);
            return record == null ? L("archive.unknown_reason", "未知原因") : ArchiveEncounterLabel(record.Encounter);
        }

        private static string ArchiveEnemyName(EnemyKind enemy)
        {
            return enemy switch
            {
                EnemyKind.RustKite => L("enemy.RustKite", "锈翼鸢"),
                EnemyKind.MailEater => L("enemy.MailEater", "噬邮兽"),
                EnemyKind.StormBalloon => L("enemy.StormBalloon", "风暴气囊"),
                EnemyKind.StormManta => L("enemy.StormManta", "磁暴鳐"),
                EnemyKind.CloudWyrm => L("enemy.CloudWyrm", "雷幕云龙"),
                EnemyKind.CalamityDrone => L("enemy.CalamityDrone", "灾变无人机"),
                EnemyKind.ShieldLeech => L("enemy.ShieldLeech", "盾蚀水蛭"),
                EnemyKind.HandJammer => L("enemy.HandJammer", "噪声织网"),
                EnemyKind.HeatSeeker => L("enemy.HeatSeeker", "热寻隼"),
                EnemyKind.SignalHijacker => L("enemy.SignalHijacker", "协议劫持机"),
                EnemyKind.CurtainHerald => L("enemy.CurtainHerald", "雷幕先导"),
                _ => L("enemy.FluxSkimmer", "磁针鳐卫")
            };
        }

        private static string FinaleEndingName(FinaleEnding ending)
        {
            return ending switch
            {
                FinaleEnding.WyrmClearSky => "晴空航权",
                FinaleEnding.WyrmSignalCovenant => "信标共鸣",
                FinaleEnding.WyrmBlackout => "永夜静默",
                FinaleEnding.MantaCalmSea => "无磁云海",
                FinaleEnding.MantaPostalShield => "群岛邮盾",
                FinaleEnding.MantaScavengerCrown => "残骸王冠",
                _ => "未记录的终局"
            };
        }

        private static string AchievementName(AchievementId achievement)
        {
            return achievement switch
            {
                AchievementId.FirstChallenge => L("achievement.FirstChallenge.name", "试炼首航"),
                AchievementId.FiveContracts => L("achievement.FiveContracts.name", "五方通邮"),
                AchievementId.TwinBossArchive => L("achievement.TwinBossArchive.name", "双核见证"),
                AchievementId.SixEndings => L("achievement.SixEndings.name", "六路归航"),
                _ => L("achievement.ContractMaster.name", "合同专家")
            };
        }

        private static string AchievementRule(AchievementId achievement)
        {
            return achievement switch
            {
                AchievementId.FirstChallenge => L("achievement.FirstChallenge.rule", "完成任意固定种子挑战"),
                AchievementId.FiveContracts => L("achievement.FiveContracts.rule", "送达全部五份合同"),
                AchievementId.TwinBossArchive => L("achievement.TwinBossArchive.rule", "击破两类终局首领"),
                AchievementId.SixEndings => L("achievement.SixEndings.rule", "发现全部六类终局"),
                _ => L("achievement.ContractMaster.rule", "全部合同达到精通2")
            };
        }

        private static string BossDossierRule(EnemyKind boss)
        {
            return boss == EnemyKind.CloudWyrm
                ? L("dossier.CloudWyrm.rule",
                    "雷幕标出唯一安全航道。集中火力可打断蓄力，第二阶段会提高打断门槛。")
                : L("dossier.StormManta.rule",
                    "磁暴核心标出危险航道。第二阶段会波及邻道，需要拉开距离或打断蓄力。");
        }

        private static string EndingRouteLabel(FinaleEnding ending)
        {
            return ending switch
            {
                FinaleEnding.WyrmSignalCovenant =>
                    L("ending.route.allied", "盟约纪事"),
                FinaleEnding.MantaPostalShield =>
                    L("ending.route.allied", "盟约纪事"),
                FinaleEnding.WyrmBlackout =>
                    L("ending.route.hostile", "敌对纪事"),
                FinaleEnding.MantaScavengerCrown =>
                    L("ending.route.hostile", "敌对纪事"),
                _ => L("ending.route.neutral", "中立纪事")
            };
        }

        private static string ProgressGoalLabel(ProgressGoal goal)
        {
            if (goal == null)
                return string.Empty;
            string label = goal.Id switch
            {
                "challenges" => L("goal.challenges", "完成固定种子挑战"),
                "contracts" => L("goal.contracts", "完成不同合同送达"),
                "bosses" => L("goal.bosses", "击破两类终局首领"),
                "endings" => L("goal.endings", "解析六类终局"),
                _ => L("goal.mastery", "将全部合同提升至精通2")
            };
            return $"{label}　{goal.Current}/{goal.Target}";
        }

        private static string FinaleEndingDescription(FinaleEnding ending)
        {
            return ending switch
            {
                FinaleEnding.WyrmClearSky => "雷幕在龙脊上空散去，群岛第一次拥有不依赖旧信标的晴空航权。",
                FinaleEnding.WyrmSignalCovenant => "获救信标接入雷幕神经，云龙化作覆盖群岛的活体导航网。",
                FinaleEnding.WyrmBlackout => "被出售的坐标烧毁了最后一批信标，邮差只能在永久静默中重建航线。",
                FinaleEnding.MantaCalmSea => "磁暴核心沉入云海，风车群岛迎来一段无人能够保证长度的平静期。",
                FinaleEnding.MantaPostalShield => "互助信号重写磁暴甲壳，巨鳐留下的场域成为保护民用航线的邮盾。",
                FinaleEnding.MantaScavengerCrown => "债务信标接管磁暴残骸，黑市拾荒者在新风暴中心加冕。",
                _ => "配送已经完成，但终局信号没有留下可解析记录。"
            };
        }

        private void DrawRunComplete()
        {
            DrawRect(new Rect(270, 105, 1060, 685), new Color32(2, 7, 22, 255));
            DrawRect(new Rect(278, 113, 1044, 669), PanelNight);
            DrawNeonFrame(new Rect(278, 113, 1044, 669), NeonCyan, 3f);
            DrawRect(new Rect(278, 113, 1044, 12), NeonViolet);
            DrawFittedLabel(new Rect(390, 158, 820, 72),
                $"ENDING // {FinaleEndingName(finaleEnding)}", neonTitleStyle, 25);
            DrawFittedLabel(new Rect(395, 238, 810, 132),
                $"{CargoName(selectedContract)}送达。\n{FinaleEndingDescription(finaleEnding)}\n" +
                $"终局情报：{RouteIntelName(routeIntel)} · 合同风险报酬 +{runContractBonus} 邮票。",
                neonBodyStyle, 11);
            GUI.Label(new Rect(420, 405, 380, 40), $"评级 {CargoGrade(runCargoIntegrity)} · {CargoStatus(runCargoIntegrity)}　{CargoPips(runCargoIntegrity)}", neonSubtitleStyle);
            GUI.Label(new Rect(820, 405, 380, 40), $"剩余机体　{runHull}/{BattleState.MaxPlayerHealth}", neonSubtitleStyle);
            GUI.Label(new Rect(420, 460, 380, 40), $"牌组 / 强化　{runDeck.Count} / {runUpgrades.Count}", neonSubtitleStyle);
            GUI.Label(new Rect(820, 460, 380, 40), $"邮票 / 模块　{credits} / {runModules.Count}", neonSubtitleStyle);
            GUI.Label(new Rect(420, 525, 380, 40), $"战斗回合　　{runTurns}", neonSubtitleStyle);
            GUI.Label(new Rect(820, 525, 380, 40), $"打出卡牌　{runCardsPlayed}", neonSubtitleStyle);
            GUI.Label(new Rect(420, 580, 380, 40), $"累计受伤　　{runDamageTaken}", neonSubtitleStyle);
            GUI.Label(new Rect(820, 580, 380, 40), $"过热次数　{runOverheats}", neonSubtitleStyle);
            ContractMasteryRecord mastery = archiveData.ContractMastery.FirstOrDefault(record =>
                record.Contract == (int)selectedContract);
            DrawFittedLabel(new Rect(420, 625, 780, 24),
                L("complete.progression", "派遣 {0}　|　合同精通 {1}　|　挑战完成 {2}",
                    ChallengeName(currentChallenge), LongTermProgressionRules.MasteryLevel(mastery),
                    currentChallenge != ChallengeId.Standard ? "✓" : "--"), tinyStyle, 8);
            ProgressGoal nextGoal = LongTermProgressionRules.NextGoals(archiveData, 1).FirstOrDefault();
            DrawFittedLabel(new Rect(420, 650, 780, 24),
                nextGoal == null
                    ? L("goal.complete", "全部长期目标均已完成；可以继续刷新挑战成绩。")
                    : L("complete.next_goal", "下一局目标 // {0}", ProgressGoalLabel(nextGoal)),
                tinyStyle, 8);
            DrawFittedLabel(new Rect(420, 675, 780, 20),
                L("complete.story_seed", "信标纪事 // {0}　|　RUN SEED // {1}",
                    RouteStoryStatus(), $"{runSeed:X8}"), tinyStyle, 7);
            DrawPixelButton(new Rect(340, 700, 280, 72), "再次出发", PostalRed, StartNewRun, true, "ENTER");
            DrawPixelButton(new Rect(660, 700, 280, 72), "查看档案", Gold,
                () => screen = ScreenMode.Archive);
            DrawPixelButton(new Rect(980, 700, 280, 72), "返回标题", Shadow,
                () => screen = ScreenMode.Title);
        }

        private void DrawRunHud(Rect rect)
        {
            DrawRect(new Rect(rect.x + 5, rect.y + 5, rect.width, rect.height), new Color32(2, 7, 22, 255));
            DrawRect(rect, new Color32(10, 27, 55, 245));
            DrawNeonFrame(rect, NeonCyan, 2f);
            DrawFittedLabel(new Rect(rect.x + 16, rect.y + 8, rect.width - 32, 22),
                $"机体 {runHull}/{BattleState.MaxPlayerHealth}　|　{CargoName(selectedContract)}　|　{ChallengeName(currentChallenge)}",
                tinyStyle, 8);
            DrawFittedLabel(new Rect(rect.x + 16, rect.y + 34, rect.width - 32, 22), CargoStatusLine(runCargoIntegrity), tinyStyle, 8);
            DrawFittedLabel(new Rect(rect.x + 16, rect.y + 60, rect.width - 32, 22),
                $"邮票 {credits}　|　牌组 {runDeck.Count}　|　改装 {AirframeModificationName(runModification)}　|　情报 {RouteIntelName(routeIntel)}",
                tinyStyle, 8);
        }

        private AirspaceCondition CurrentAirspace()
        {
            return route.Get(selectedRouteNodeId).Airspace;
        }

        private static string RouteIntelName(RouteIntel intel)
        {
            return intel switch
            {
                RouteIntel.CurtainCipher => "雷幕航道密钥",
                RouteIntel.FluxCompass => "磁针偏航罗盘",
                RouteIntel.DualChannelDecoder => "双频终局解码器",
                _ => "尚未取得"
            };
        }

        private static string RouteIntelRule(RouteIntel intel)
        {
            return intel switch
            {
                RouteIntel.CurtainCipher => "雷幕云龙首轮安全航道固定在当前位置",
                RouteIntel.FluxCompass => "磁暴鳐首轮锁定被偏转至其他航道",
                RouteIntel.DualChannelDecoder => "自动适配最终选择的任一首领",
                _ => "击破终局前兆编队后获得"
            };
        }

        private static Color AirspaceColor(AirspaceCondition condition)
        {
            return condition switch
            {
                AirspaceCondition.JetstreamCorridor => new Color32(73, 211, 220, 255),
                AirspaceCondition.StaticFront => new Color32(180, 91, 255, 255),
                _ => new Color32(244, 181, 70, 255)
            };
        }

        private static Vector2 RouteNodeCenter(RouteNodeDefinition node, float offsetX, float columnSpacing)
        {
            float stagger = ((node.Column % 3) - 1) * 6f;
            return new Vector2(offsetX + node.Column * columnSpacing, 64f + node.Lane * 122f + stagger);
        }

        private void DrawMapConnection(Vector2 start, Vector2 end, Color color, float thickness)
        {
            Vector2 direction = end - start;
            float length = direction.magnitude;
            if (length < 1f)
                return;

            Vector2 normal = direction / length;
            const float nodeEdge = 39f;
            start += normal * nodeEdge;
            end -= normal * nodeEdge;
            length = Vector2.Distance(start, end);
            if (length < 1f)
                return;

            float spacing = Mathf.Max(3f, thickness * 0.72f);
            int steps = Mathf.Max(1, Mathf.CeilToInt(length / spacing));
            float pixelSize = Mathf.Max(2f, thickness);
            for (int step = 0; step <= steps; step++)
            {
                Vector2 point = Vector2.Lerp(start, end, step / (float)steps);
                DrawRect(new Rect(
                    Mathf.Round(point.x - pixelSize * 0.5f),
                    Mathf.Round(point.y - pixelSize * 0.5f),
                    pixelSize,
                    pixelSize), color);
            }
        }

        private void DrawRouteNode(RouteNodeDefinition node, float offsetX, float columnSpacing, int revealThrough)
        {
            Vector2 center = RouteNodeCenter(node, offsetX, columnSpacing);
            Rect iconRect = new Rect(center.x - 30f, center.y - 30f, 60f, 60f);
            Rect hitRect = new Rect(center.x - 72f, center.y - 40f, 144f, 116f);
            bool revealed = node.Column <= revealThrough;
            bool completed = completedRouteNodes.Contains(node.Id);
            bool available = IsRouteNodeAvailable(node);
            bool selected = available && node.Id == selectedRouteNodeId;
            bool missed = node.Column < routeIndex && !completed;
            Color kindColor = node.Id == 19
                ? new Color32(73, 211, 220, 255)
                : RouteNodeColor(node.Kind);
            Color frame = completed ? new Color32(83, 220, 158, 255) : selected ? Color.white :
                available ? kindColor : missed ? new Color32(45, 52, 69, 255) : new Color32(72, 88, 116, 255);

            if (!revealed)
                kindColor = new Color32(48, 55, 83, 255);
            Color fill = missed ? new Color32(7, 13, 28, 250) : new Color32(9, 22, 47, 252);
            DrawRect(new Rect(iconRect.x + 7, iconRect.y + 7, iconRect.width, iconRect.height), new Color32(1, 6, 18, 255));
            DrawRect(iconRect, fill);
            DrawPixelOutline(iconRect, frame, selected ? 5f : available ? 4f : 3f);
            DrawRect(new Rect(iconRect.x - 6, iconRect.y + 12, 6, 36), frame);
            DrawRect(new Rect(iconRect.xMax, iconRect.y + 12, 6, 36), frame);
            DrawRect(new Rect(iconRect.x + 12, iconRect.y - 6, 36, 6), frame);
            DrawRect(new Rect(iconRect.x + 12, iconRect.yMax, 36, 6), frame);
            if (selected)
                DrawNeonFrame(new Rect(iconRect.x - 4, iconRect.y - 4, iconRect.width + 8, iconRect.height + 8), frame, 2f);

            DrawRouteNodeIcon(node, center, completed ? new Color32(83, 220, 158, 255) : kindColor);
            string status = completed ? "DONE" : selected ? "SELECT" : available ? "OPEN" : missed ? "CLOSED" : "SCAN";
            DrawRect(new Rect(center.x - 38f, center.y + 36f, 76f, 16f), new Color32(4, 12, 30, 245));
            DrawFittedLabel(new Rect(center.x - 36f, center.y + 36f, 72f, 16f), status, tinyStyle, 8);
            string displayTitle = node.Kind == RouteNodeKind.Event ? EventTitleForNode(node.Id) : node.Title;
            DrawFittedLabel(new Rect(center.x - 78f, center.y + 55f, 156f, 22f),
                revealed ? displayTitle : "信号未解析", hudCenteredStyle, 8);

            if (!available)
                return;
            bool hovered = hitRect.Contains(Event.current.mousePosition);
            if (hovered)
            {
                RegisterHover($"route-node-{node.Id}", $"选择航点 {displayTitle}");
                DrawNeonFrame(new Rect(iconRect.x - 3, iconRect.y - 3, iconRect.width + 6, iconRect.height + 6), kindColor, 2f);
            }
            if (GUI.Button(hitRect, GUIContent.none, GUIStyle.none))
                SelectRouteNode(node.Id);
        }

        private static string RouteNodeKindLabel(RouteNodeKind kind)
        {
            return kind switch
            {
                RouteNodeKind.Skirmish => "普通战",
                RouteNodeKind.Elite => "精英战",
                RouteNodeKind.Hunt => "追猎战",
                RouteNodeKind.Shop => "补给站",
                RouteNodeKind.Event => "航线事件",
                RouteNodeKind.Rest => "维修坞",
                _ => "首领"
            };
        }

        private static Color RouteNodeColor(RouteNodeKind kind)
        {
            return kind switch
            {
                RouteNodeKind.Skirmish => new Color32(67, 157, 170, 255),
                RouteNodeKind.Elite => new Color32(196, 91, 167, 255),
                RouteNodeKind.Hunt => new Color32(255, 92, 154, 255),
                RouteNodeKind.Shop => new Color32(244, 181, 70, 255),
                RouteNodeKind.Event => new Color32(180, 91, 255, 255),
                RouteNodeKind.Rest => new Color32(83, 203, 150, 255),
                _ => new Color32(214, 70, 66, 255)
            };
        }

        private void DrawRouteNodeIcon(RouteNodeDefinition node, Vector2 center, Color color)
        {
            RouteNodeKind kind = node.Kind;
            if (kind == RouteNodeKind.Shop)
            {
                DrawRect(new Rect(center.x - 18, center.y - 12, 36, 26), color);
                DrawRect(new Rect(center.x - 23, center.y - 17, 46, 7), PostalRed);
                DrawRect(new Rect(center.x - 4, center.y + 1, 8, 13), Shadow);
                return;
            }

            if (kind == RouteNodeKind.Boss)
            {
                if (node.Id == 19)
                {
                    DrawPixelOutline(new Rect(center.x - 18, center.y - 18, 36, 36), color, 3f);
                    DrawRect(new Rect(center.x - 7, center.y - 16, 14, 10), color);
                    DrawRect(new Rect(center.x + 1, center.y - 7, 14, 9), color);
                    DrawRect(new Rect(center.x - 9, center.y + 1, 14, 9), color);
                    DrawRect(new Rect(center.x - 3, center.y + 10, 10, 12), color);
                    DrawRect(new Rect(center.x - 24, center.y - 3, 9, 6), color);
                    DrawRect(new Rect(center.x + 15, center.y - 3, 9, 6), color);
                    return;
                }
                DrawRect(new Rect(center.x - 21, center.y - 6, 42, 12), color);
                DrawRect(new Rect(center.x - 10, center.y - 15, 20, 30), color);
                DrawRect(new Rect(center.x - 27, center.y + 5, 54, 7), color);
                return;
            }

            if (kind == RouteNodeKind.Event)
            {
                DrawPixelOutline(new Rect(center.x - 14, center.y - 14, 28, 28), color, 3f);
                DrawRect(new Rect(center.x - 3, center.y - 8, 6, 11), color);
                DrawRect(new Rect(center.x - 3, center.y + 7, 6, 5), color);
                return;
            }

            if (kind == RouteNodeKind.Rest)
            {
                DrawRect(new Rect(center.x - 4, center.y - 16, 8, 32), color);
                DrawRect(new Rect(center.x - 16, center.y - 4, 32, 8), color);
                return;
            }

            if (kind == RouteNodeKind.Hunt)
            {
                DrawRect(new Rect(center.x - 20, center.y - 3, 40, 6), color);
                DrawRect(new Rect(center.x - 3, center.y - 20, 6, 40), color);
                DrawPixelOutline(new Rect(center.x - 15, center.y - 15, 30, 30), color, 2f);
                return;
            }

            DrawRect(new Rect(center.x - 19, center.y - 4, 38, 8), color);
            DrawRect(new Rect(center.x - 7, center.y - 14, 14, 28), color);
            if (kind == RouteNodeKind.Elite)
                DrawPixelOutline(new Rect(center.x - 24, center.y - 19, 48, 38), color, 2f);
        }

        private void DrawOfferCard(CardId cardId, Rect rect, string footer, bool enabled, Action action,
            string badge = null, string shortcut = null, string titleOverride = null, string rulesOverride = null)
        {
            CardSpec card = CardLibrary.Get(cardId);
            bool hovered = enabled && rect.Contains(Event.current.mousePosition);
            if (hovered)
            {
                rect.y -= 12f + Mathf.Sin(Time.time * 9f) * 2f;
                RegisterHover($"offer-{cardId}", $"点击{footer} · {card.Name}");
            }
            Color family = enabled ? CardLibrary.FamilyColor(card.Family) : new Color32(118, 119, 116, 255);
            DrawRect(new Rect(rect.x + 7, rect.y + 7, rect.width, rect.height), new Color32(2, 7, 22, 255));
            DrawRect(rect, new Color32(239, 235, 211, 255));
            DrawNeonFrame(rect, hovered ? Color.Lerp(family, Color.white, 0.28f) : family, hovered ? 4f : 2f);
            DrawRect(new Rect(rect.x, rect.y, rect.width, 56), family);
            DrawFittedLabel(new Rect(rect.x + 18, rect.y + 8, rect.width - 36, 42),
                string.IsNullOrEmpty(titleOverride) ? card.Name : titleOverride, cardTitleStyle, 13);
            if (!string.IsNullOrEmpty(badge))
            {
                DrawRect(new Rect(rect.x + 18, rect.y + 64, rect.width - 36, 26), new Color32(7, 18, 43, 232));
                GUI.Label(new Rect(rect.x + 22, rect.y + 65, rect.width - 44, 24), badge, tinyStyle);
            }
            DrawFittedLabel(new Rect(rect.x + 22, rect.y + (string.IsNullOrEmpty(badge) ? 82 : 100), rect.width - 44,
                string.IsNullOrEmpty(badge) ? 105 : 87), string.IsNullOrEmpty(rulesOverride) ? card.Rules : rulesOverride, cardBodyStyle, 12);
            GUI.Label(new Rect(rect.x + 22, rect.y + 200, rect.width - 44, 32), $"费用 {card.Cost}   热量 +{card.Heat}", centeredStyle);
            float scanX = rect.x + Mathf.Repeat(Time.time * 78f + rect.x * 0.17f, rect.width + 55f) - 34f;
            DrawRect(new Rect(scanX, rect.y + 58, 10, rect.height - 144), new Color32(255, 255, 255, 28));
            Rect offerButton = new Rect(rect.x + 18, rect.y + rect.height - 76, rect.width - 36, 54);
            if (footer.StartsWith("购买 ·"))
                DrawPurchaseButton(offerButton, footer.Replace("购买 ·", string.Empty).Trim(), family, action, enabled);
            else
                DrawPixelButton(offerButton, footer, family, action, enabled);
        }

        private void DrawPurchaseButton(Rect rect, string price, Color color, Action action, bool enabled)
        {
            Color shown = enabled ? color : new Color32(74, 78, 94, 255);
            bool hovered = enabled && rect.Contains(Event.current.mousePosition);
            if (hovered)
                shown = Color.Lerp(shown, NeonCyan, 0.12f);

            DrawRect(new Rect(rect.x + 6, rect.y + 6, rect.width, rect.height), new Color32(2, 7, 22, 255));
            DrawRect(rect, shown);
            DrawPixelOutline(rect, new Color32(7, 16, 38, 255), 3f);

            Rect actionArea = new Rect(rect.x + 8, rect.y + 7, rect.width * 0.48f, rect.height - 14);
            Rect priceArea = new Rect(rect.x + rect.width * 0.51f, rect.y + 8, rect.width * 0.44f, rect.height - 16);
            DrawRect(actionArea, new Color32(5, 13, 34, hovered ? (byte)205 : (byte)155));
            DrawRect(priceArea, new Color32(5, 13, 34, 220));
            DrawPixelOutline(priceArea, enabled ? NeonCyan : new Color32(95, 101, 115, 255), 2f);
            DrawCyberLabel(actionArea, "购买", buttonLabelStyle, enabled ? NeonViolet : shown);
            GUI.Label(priceArea, price.Replace("邮票", " 邮票"), tinyStyle);

            if (hovered)
                DrawNeonFrame(new Rect(rect.x - 3, rect.y - 3, rect.width + 6, rect.height + 6), NeonCyan, 2f);

            bool oldEnabled = GUI.enabled;
            GUI.enabled = enabled;
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                PlaySound(clickSound);
                action?.Invoke();
            }
            GUI.enabled = oldEnabled;
        }

        private void ChooseRewardChoice(RewardChoice reward, int index)
        {
            if (rewardSelectionLocked)
                return;
            rewardSelectionLocked = true;
            selectedRewardIndex = index;
            switch (reward.Kind)
            {
                case RewardKind.AddCard:
                    runDeck.Add(reward.Card);
                    selectedRewardName = $"{CardLibrary.Get(reward.Card).Name} 已加入牌组";
                    break;
                case RewardKind.UpgradeCard:
                    runUpgrades.Add(reward.Card);
                    runUpgradeBranches[reward.Card] = reward.Branch;
                    selectedRewardName = $"{CardLibrary.Get(reward.Card).Name}+{(reward.Branch == UpgradeBranch.Alpha ? "A" : "B")} 分支已写入";
                    break;
                case RewardKind.Module:
                    if (!runModules.Contains(reward.Module))
                        runModules.Add(reward.Module);
                    selectedRewardName = $"{ModuleName(reward.Module)} 已接入机体";
                    break;
            }
            CaptureBuildSnapshot(RunBuildSnapshotMoment.Reward,
                $"reward_{selectedRouteNodeId}");
            DeliveryArchiveService.RegisterRewardDiscoveries(archiveData,
                runDeck.Select(card => (int)card), runModules.Select(module => (int)module));
            SaveArchive();
            rewardConfirmUntil = Time.time + 0.72f;
            bool rare = reward.Kind == RewardKind.Module;
            PlayLayeredSound(rewardSound, rare ? 0.82f : 1.08f, rare ? 1f : 0.95f,
                impactSound, rare ? 0.64f : 0.92f, rare ? 0.7f : 0.42f);
            TriggerShake(rare ? 13f : 8f, rare ? 0.48f : 0.34f);
            TriggerFullScreenImpact(rare ? 1.55f : 1.15f, rare ? 0.76f : 0.62f, false);
            StartCoroutine(AdvanceAfterRewardDelay());
        }

        private void SkipReward()
        {
            if (rewardSelectionLocked)
                return;
            credits += battle.Encounter == EncounterId.Elite ? 25 : 15;
            CaptureBuildSnapshot(RunBuildSnapshotMoment.Reward,
                $"reward_{selectedRouteNodeId}");
            PlaySound(clickSound, 1.25f, 0.7f);
            AdvanceAfterReward();
        }

        private IEnumerator AdvanceAfterRewardDelay()
        {
            yield return new WaitForSecondsRealtime(0.74f);
            AdvanceAfterReward();
        }

        private void AdvanceAfterReward()
        {
            AdvanceAfterCurrentRouteNode();
            rewardSelectionLocked = false;
            selectedRewardIndex = -1;
            selectedRewardName = null;
            SaveRunCheckpoint();
        }

        private void ContinueAfterVictory()
        {
            runHull = battle.PlayerHealth;
            bool fieldRepairs = ChallengeCatalog.Get(currentChallenge).FieldRepairsEnabled;
            lastFieldRepair = !fieldRepairs || battle.Encounter == EncounterId.Boss ? 0 :
                battle.Encounter == EncounterId.Hunt ? 12 : battle.Encounter == EncounterId.Elite ? 10 : 6;
            runHull = Mathf.Min(BattleState.MaxPlayerHealth, runHull + lastFieldRepair);
            runCargoIntegrity = battle.CargoIntegrity;
            runTurns += battle.Turn;
            runCardsPlayed += battle.CardsPlayed;
            runDamageTaken += battle.DamageTaken;
            runOverheats += battle.OverheatCount;
            runCalamityInterrupts += battle.CalamityInterrupts;
            runCalamityEvades += battle.CalamityEvades;
            runCalamityHits += battle.CalamityHits;
            runTrackingHits += battle.TrackingHits;
            runContractProcs += battle.ContractPassiveProcs;
            RouteIntel acquiredIntel = FinaleProgressionRules.IntelForPreludeNode(selectedRouteNodeId);
            if (battle.Encounter != EncounterId.Boss && acquiredIntel != RouteIntel.None)
                routeIntel = acquiredIntel;
            int baseReward = battle.Encounter switch
            {
                EncounterId.Skirmish => 30,
                EncounterId.Elite => 45,
                EncounterId.Hunt => 50,
                _ => 80
            };
            int contractBonus = Mathf.RoundToInt(baseReward * (CargoRewardMultiplier(selectedContract) - 1f));
            credits += baseReward + contractBonus;
            runContractBonus += contractBonus;
            lastRewardCredits = baseReward + contractBonus;
            DeliveryArchiveService.RegisterBattleWon(archiveData);
            RecordRunDiagnostic("battle_won", battle.FormationName);

            screen = battle.Encounter == EncounterId.Boss ? ScreenMode.Complete : ScreenMode.Reward;
            if (screen == ScreenMode.Reward)
            {
                rewardEnteredAt = Time.time;
                rewardSelectionLocked = false;
                selectedRewardIndex = -1;
                selectedRewardName = null;
                PlaySound(rewardSound, 0.92f, 0.72f);
                SaveRunCheckpoint();
            }
            else
            {
                EnemyKind defeatedBoss = battle.Enemies.Any(enemy => enemy.Kind == EnemyKind.CloudWyrm)
                    ? EnemyKind.CloudWyrm
                    : EnemyKind.StormManta;
                finaleEnding = FinaleProgressionRules.EndingFor(defeatedBoss, battle.ActiveBossStoryAlignment);
                CaptureBuildSnapshot(RunBuildSnapshotMoment.RunResult, "result_delivered", runHull,
                    runCargoIntegrity);
                DeliveryArchiveService.RegisterRunResult(archiveData, CreateArchiveRecord(
                    runTurns, runCardsPlayed, runHull, runCargoIntegrity, battle.Encounter), true);
                SaveArchive();
                RecordRunDiagnostic("run_completed");
                RunSaveService.Delete();
            }
        }

        private void RegisterArchiveFailure()
        {
            archiveFailureRecorded = true;
            CaptureBuildSnapshot(RunBuildSnapshotMoment.RunResult, "result_lost", battle.PlayerHealth,
                battle.CargoIntegrity);
            int turns = runTurns + battle.Turn;
            int cardsPlayed = runCardsPlayed + battle.CardsPlayed;
            DeliveryArchiveService.RegisterRunResult(archiveData,
                CreateArchiveRecord(turns, cardsPlayed, battle.PlayerHealth, battle.CargoIntegrity, battle.Encounter),
                false);
            if (SaveArchive())
            {
                RunSaveService.Delete();
            }
            else
            {
                archiveFailureRecorded = false;
                return;
            }
            RecordRunDiagnostic("run_failed",
                $"{FailureCauseTitle(battle.DefeatSource)} | {battle.DefeatDealer} | {battle.FormationName}");
        }

        private ArchivedRunRecord CreateArchiveRecord(int turns, int cardsPlayed, int hull, int cargoIntegrity,
            EncounterId encounter)
        {
            return new ArchivedRunRecord
            {
                AttemptId = runAttemptId,
                RunSeed = runSeed,
                Contract = (int)selectedContract,
                RouteNodeId = selectedRouteNodeId,
                Encounter = (int)encounter,
                CargoIntegrity = cargoIntegrity,
                Hull = hull,
                Credits = credits,
                Turns = turns,
                CardsPlayed = cardsPlayed,
                DeckCount = runDeck.Count,
                ModuleCount = runModules.Count,
                BuildSnapshots = RunBuildSnapshotRules.Clone(runBuildSnapshots),
                RouteIntel = (int)routeIntel,
                FinaleEnding = (int)finaleEnding,
                Challenge = (int)currentChallenge,
                BossKind = battle.Enemies.Any(enemy => enemy.Kind == EnemyKind.CloudWyrm)
                    ? (int)EnemyKind.CloudWyrm
                    : battle.Enemies.Any(enemy => enemy.Kind == EnemyKind.StormManta)
                        ? (int)EnemyKind.StormManta
                        : -1,
                DefeatSource = battle.Defeat && battle.HasDefeatCause ? (int)battle.DefeatSource : -1,
                DefeatDealer = battle.Defeat && battle.HasDefeatCause ? battle.DefeatDealer : string.Empty,
                DefeatDamage = battle.Defeat && battle.HasDefeatCause ? battle.DefeatDamage : 0,
                DefeatRawDamage = battle.Defeat && battle.HasDefeatCause ? battle.DefeatRawDamage : 0,
                DefeatShieldAbsorbed = battle.Defeat && battle.HasDefeatCause
                    ? battle.DefeatShieldAbsorbed
                    : 0,
                DefeatHullBefore = battle.Defeat && battle.HasDefeatCause ? battle.DefeatHullBefore : 0,
                DefeatTurn = battle.Defeat && battle.HasDefeatCause ? battle.DefeatTurn : 0,
                DamageTaken = runDamageTaken + (battle.Defeat ? battle.DamageTaken : 0),
                Overheats = runOverheats + (battle.Defeat ? battle.OverheatCount : 0),
                CalamityInterrupts = runCalamityInterrupts +
                    (battle.Defeat ? battle.CalamityInterrupts : 0),
                CalamityEvades = runCalamityEvades + (battle.Defeat ? battle.CalamityEvades : 0),
                CalamityHits = runCalamityHits + (battle.Defeat ? battle.CalamityHits : 0),
                TrackingHits = runTrackingHits + (battle.Defeat ? battle.TrackingHits : 0),
                ContractProcs = runContractProcs + (battle.Defeat ? battle.ContractPassiveProcs : 0),
                ContractBonusCredits = runContractBonus,
                AirframeModification = (int)runModification,
                RouteStoryState = (int)routeStoryState,
                DepartureDirective = (int)departureDirective,
                FinalApproachPlan = (int)finalApproachPlan,
                BuildProfile = CurrentBuildProfile(),
                RouteProfile = CurrentRouteProfile()
            };
        }

        private string CurrentBuildProfile()
        {
            Dictionary<CardFamily, int> counts = Enum.GetValues(typeof(CardFamily)).Cast<CardFamily>()
                .ToDictionary(family => family, _ => 0);
            foreach (CardId card in runDeck)
                counts[CardLibrary.Get(card).Family]++;
            int highest = counts.Values.DefaultIfEmpty().Max();
            CardFamily[] leaders = counts.Where(entry => entry.Value == highest)
                .Select(entry => entry.Key).ToArray();
            if (leaders.Length != 1 || highest < Mathf.CeilToInt(runDeck.Count * 0.34f))
                return "hybrid";
            return leaders[0] switch
            {
                CardFamily.Weapon => "weapon",
                CardFamily.Maneuver => "maneuver",
                CardFamily.Defense => "defense",
                _ => "utility"
            };
        }

        private string CurrentRouteProfile()
        {
            RouteNodeDefinition[] visited = completedRouteNodes.Append(selectedRouteNodeId).Distinct()
                .Select(id => route.Nodes.FirstOrDefault(node => node.Id == id))
                .Where(node => node != null)
                .ToArray();
            int pressure = visited.Count(node => node.Kind == RouteNodeKind.Elite ||
                node.Kind == RouteNodeKind.Hunt);
            int service = visited.Count(node => node.Kind == RouteNodeKind.Shop ||
                node.Kind == RouteNodeKind.Rest);
            if (RouteStoryRules.IsSilent(routeStoryState) ||
                routeStoryState == RouteStoryState.SilentRouteSecured)
                return "silent";
            if (pressure > service)
                return "pressure";
            if (service > pressure)
                return "service";
            return "mixed";
        }

        private bool SaveArchive()
        {
            try
            {
                DeliveryArchiveService.Save(archiveData);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"DELIVERY_ARCHIVE_SAVE_FAILED: {exception}");
                RecordRunDiagnostic("archive_save_failed", exception.Message);
                return false;
            }
        }

        private void RecordRunDiagnostic(string eventName, string message = null)
        {
            bool hasEncounter = screen == ScreenMode.Battle || screen == ScreenMode.Reward;
            RunDiagnosticsService.Record(new RunDiagnosticRecord
            {
                Event = eventName,
                RunSeed = runSeed,
                Screen = screen.ToString(),
                RouteNodeId = selectedRouteNodeId,
                Encounter = hasEncounter ? (int)battle.Encounter : -1,
                Contract = runDeck.Count > 0 ? (int)selectedContract : -1,
                Hull = hasEncounter ? battle.PlayerHealth : runHull,
                CargoIntegrity = hasEncounter ? battle.CargoIntegrity : runCargoIntegrity,
                Turn = hasEncounter ? battle.Turn : 0,
                Message = message
            });
        }

        private void DrawBattleScreen()
        {
            DrawTopBar();
            DrawBattlefield();
            DrawHand();
            DrawCommandChain();
            DrawPhaseBanner();
            DrawImpactScreenFlash();
            DrawFullScreenImpact();

            if (Time.time < dangerFlashUntil)
            {
                Color danger = new Color32(220, 56, 62,
                    (byte)Mathf.RoundToInt(125f * gameSettings.FlashIntensity));
                DrawRect(new Rect(0, 0, ReferenceWidth, 18), danger);
                DrawRect(new Rect(0, ReferenceHeight - 18, ReferenceWidth, 18), danger);
                DrawRect(new Rect(0, 0, 18, ReferenceHeight), danger);
                DrawRect(new Rect(ReferenceWidth - 18, 0, 18, ReferenceHeight), danger);
            }

            if (battle.Victory && !DeathAnimationActive())
                DrawResultOverlay(true);
            else if (battle.Defeat)
                DrawResultOverlay(false);
        }

        private bool DeathAnimationActive()
        {
            for (int i = 0; i < enemyDeathFx.Count; i++)
            {
                if (Time.time - enemyDeathFx[i].StartTime < 1.45f)
                    return true;
            }
            return false;
        }

        private void DrawCommandChain()
        {
            if (commandChain < 2 || Time.time >= commandChainUntil || battle.Victory || battle.Defeat)
                return;
            float pulse = 1f + Mathf.Sin(Time.time * 22f) * 0.05f;
            Rect rect = new Rect(1225 - (pulse - 1f) * 150f, 630 - (pulse - 1f) * 28f, 300f * pulse, 56f * pulse);
            DrawRect(new Rect(rect.x + 5, rect.y + 5, rect.width, rect.height), new Color32(2, 6, 18, 230));
            DrawRect(rect, new Color32(8, 18, 43, 245));
            DrawNeonFrame(rect, commandChain >= 4 ? NeonViolet : NeonCyan, 3f);
            GUI.Label(rect, $"COMMAND CHAIN ×{commandChain} // 连续指令", hudCenteredStyle);
        }

        private void DrawTopBar()
        {
            string encounterName = battle.Encounter switch
            {
                EncounterId.Skirmish => L("encounter.skirmish.name", "废弃风标"),
                EncounterId.Elite => L("encounter.elite.name", "雷暴封锁线"),
                EncounterId.Hunt => L("encounter.hunt.name", "追迹者空域"),
                _ => battle.Enemies.Any(enemy => enemy.Kind == EnemyKind.CloudWyrm)
                    ? L("encounter.wyrm.name", "雷幕龙脊")
                    : L("encounter.manta.name", "磁暴鳐巢")
            };
            DrawRect(new Rect(34, 24, 1532, 88), new Color32(3, 8, 24, 255));
            DrawRect(new Rect(40, 30, 1518, 76), PanelNight);
            DrawNeonFrame(new Rect(40, 30, 1518, 76), NeonCyan, 2f);
            DrawRect(new Rect(40, 30, 7, 76), NeonViolet);
            DrawFittedLabel(new Rect(62, 38, 320, 34), $"{encounterName} // {battle.FormationName}", hudStyle, 12);
            DrawFittedLabel(new Rect(62, 73, 330, 22), ContractPassiveHud(selectedContract), tinyStyle, 8);
            if (battle.Encounter == EncounterId.Boss)
                DrawFittedLabel(new Rect(400, 75, 850, 18),
                    L("boss.matrix.summary", "BOSS MATRIX // {0} · {1} · {2}",
                        BossContractProtocolName(), BossAirframeProtocolName(), BossStoryAlignmentName()) +
                    $"　|　INTEL // {RouteIntelName(routeIntel)}",
                    tinyStyle, 7);
            else
            {
                AirspaceCondition condition = CurrentAirspace();
                string airframe = runModification == AirframeModification.None
                    ? string.Empty
                    : $"　|　AIRFRAME // {AirframeModificationName(runModification)}";
                DrawFittedLabel(new Rect(400, 75, 850, 18),
                    $"AIRSPACE // {AirspaceRuleCatalog.Name(condition)} · {AirspaceRuleCatalog.EncounterRule(condition)}{airframe}",
                    tinyStyle, 7);
            }

            DrawMeter(new Rect(400, 46, 255, 22), battle.PlayerHealth, BattleState.MaxPlayerHealth,
                new Color32(74, 172, 114, 255),
                L("battle.hull", "机体  {0}/{1}", battle.PlayerHealth, BattleState.MaxPlayerHealth));
            DrawMeter(new Rect(690, 46, 230, 22), battle.Heat, battle.HeatLimit,
                battle.Heat >= battle.HeatLimit - 2 ? PostalRed : Gold,
                L("battle.heat", "# 热量  {0}/{1}", battle.Heat, battle.HeatLimit));

            DrawResourcePips(new Rect(958, 42, 150, 34), battle.Energy, battle.TurnEnergy, NeonCyan,
                L("battle.energy", "能量"));
            DrawResourcePips(new Rect(1125, 42, 130, 34), Mathf.Min(battle.Armor, 3), 3, NeonViolet,
                L("battle.armor", "护盾 {0}", battle.Armor));

            Color cargoColor = battle.CargoIntegrity > 1 ? CargoColor(selectedContract) : PostalRed;
            DrawRect(new Rect(1278, 39, 254, 54), new Color32(9, 22, 45, 255));
            DrawNeonFrame(new Rect(1278, 39, 254, 54), cargoColor, 3f);
            GUI.Label(new Rect(1290, 43, 230, 25), CargoName(selectedContract), hudCenteredStyle);
            GUI.Label(new Rect(1290, 68, 230, 20), CargoStatusLine(battle.CargoIntegrity), tinyStyle);
        }

        private void DrawBattlefield()
        {
            Rect field = new Rect(40, 128, 1518, 435);
            DrawRect(field, new Color32(3, 8, 25, 255));
            Rect innerField = new Rect(48, 136, 1502, 419);
            DrawRect(innerField, new Color32(13, 38, 66, 255));
            DrawArcadeScrollingBackdrop(innerField, true);
            DrawNeonFrame(field, NeonCyan, 3f);
            DrawBattleSpeedLines(innerField);
            DrawAmbientPixels(innerField);

            for (int lane = 0; lane < 3; lane++)
            {
                float y = 150 + lane * 130;
                Color laneColor = lane == battle.PlayerLane
                    ? new Color32(45, 216, 236, 38)
                    : new Color32(49, 74, 115, 24);
                DrawRect(new Rect(62, y, 1466, 105), laneColor);
                DrawRect(new Rect(62, y + 104, 1466, lane == battle.PlayerLane ? 3 : 1),
                    lane == battle.PlayerLane ? NeonCyan : new Color32(65, 104, 146, 160));
                if (lane == battle.PlayerLane)
                {
                    float markerX = 66 + Mathf.Repeat(Time.time * 190f, 1420f);
                    DrawRect(new Rect(markerX, y + 101, 62, 6), new Color32(145, 255, 248, 210));
                }
                GUI.Label(new Rect(76, y + 10, 180, 24), lane == battle.PlayerLane
                    ? L("battle.lane.current", "[>] 航道 {0} // 当前", lane + 1)
                    : L("battle.lane", "航道 {0}", lane + 1), tinyStyle);
            }

            foreach (EnemyState enemy in battle.Enemies)
            {
                if (!enemy.Alive)
                    continue;
                if (enemy.Kind == EnemyKind.CalamityDrone)
                    DrawCalamityLaneTelegraph(enemy);
                else if (enemy.Kind == EnemyKind.CloudWyrm || enemy.Kind == EnemyKind.CurtainHerald)
                    DrawCloudWyrmLaneTelegraph(enemy);
                else if (enemy.Kind == EnemyKind.FluxSkimmer)
                    DrawFluxSkimmerLaneTelegraph(enemy);
            }

            float laneT = LaneTransitionProgress();
            bool changingLane = laneT < 1f;
            float playerBob = changingLane ? 0f : Mathf.Sin(Time.time * 5.2f) * 3f;
            Vector2 playerPosition = PlayerVisualPosition(laneT);
            playerPosition.y += playerBob;
            if (changingLane)
                DrawLaneTransitionFx(playerPosition, laneT);
            DrawEngineTrail(playerPosition);
            Matrix4x4 planeMatrix = GUI.matrix;
            if (changingLane)
            {
                float direction = Mathf.Sign(laneTransitionTo - laneTransitionFrom);
                float bankAngle = direction * Mathf.Sin(laneT * Mathf.PI) * 17f;
                GUIUtility.RotateAroundPivot(bankAngle, playerPosition);
            }
            DrawPixelPlane(playerPosition, 1.7f, false);
            GUI.matrix = planeMatrix;
            float labelAlpha = changingLane ? 0.45f + laneT * 0.55f : 1f;
            Color labelColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, labelAlpha);
            GUI.Label(new Rect(165, playerPosition.y - 30, 260, 28), changingLane
                ? L("battle.courier.shift", "邮运-07 // 换至航道 {0}", laneTransitionTo + 1)
                : L("battle.courier", "邮运-07 // 邮差"), hudCenteredStyle);
            GUI.color = labelColor;

            enemyLaneFx.RemoveAll(fx => Time.time >= fx.StartTime + fx.Duration);
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                EnemyState enemy = battle.Enemies[i];
                if (!enemy.Alive)
                    continue;
                EnemyLaneFx laneFx;
                float enemyLaneT;
                Vector2 enemyPosition = EnemyVisualPosition(i, enemy, out laneFx, out enemyLaneT);
                bool enemyChangingLane = laneFx != null;
                float x = enemyPosition.x;
                float y = enemyPosition.y + (enemyChangingLane ? 0f : Mathf.Sin(Time.time * 3.7f + i * 1.9f) * 4f);
                if (enemyChangingLane)
                    DrawEnemyLaneTransitionFx(i, laneFx, new Vector2(x, y), enemyLaneT);
                if (Time.time < enemyRecoilUntil)
                {
                    float recoil = Mathf.Clamp01((enemyRecoilUntil - Time.time) / 0.62f);
                    x += Mathf.Abs(Mathf.Sin(Time.time * 92f + i)) * 32f * recoil;
                    y += Mathf.Sin(Time.time * 117f + i * 1.7f) * 6f * recoil;
                }
                Matrix4x4 enemyMatrix = GUI.matrix;
                if (enemyChangingLane)
                {
                    float direction = Mathf.Sign(laneFx.ToLane - laneFx.FromLane);
                    GUIUtility.RotateAroundPivot(-direction * Mathf.Sin(enemyLaneT * Mathf.PI) * 14f, new Vector2(x, y));
                }
                if (enemy.Kind == EnemyKind.CalamityDrone || enemy.Kind == EnemyKind.StormManta ||
                    enemy.Kind == EnemyKind.CloudWyrm || enemy.Kind == EnemyKind.CurtainHerald ||
                    enemy.Kind == EnemyKind.FluxSkimmer)
                    DrawCalamityChargeCore(enemy, new Vector2(x, y));
                DrawEnemy(enemy, new Vector2(x, y));
                GUI.matrix = enemyMatrix;
                if (enemy.Kind == EnemyKind.StormManta || enemy.Kind == EnemyKind.CloudWyrm)
                    DrawBossProtocolPanels(enemy, new Vector2(x, y));
                string intent = enemyChangingLane
                    ? L("battle.intent.shift", "变轨至航道 {0}", laneFx.ToLane + 1)
                    : battle.IntentFor(enemy);
                Color intentColor = IntentColor(intent);
                Rect intentRect = new Rect(x - 105, y - 58, 210, 25);
                DrawRect(intentRect, new Color32(9, 14, 34, 245));
                DrawNeonFrame(intentRect, intentColor, 2f);
                DrawFittedLabel(new Rect(intentRect.x + 5, intentRect.y + 2, 200, 21),
                    $"{IntentSymbol(intent)} {intent}", tinyStyle, 9);
                DrawFittedLabel(new Rect(x - 100, y + 42, 200, 24),
                    enemy.Kind == EnemyKind.StormManta || enemy.Kind == EnemyKind.CloudWyrm
                        ? $"{ArchiveEnemyName(enemy.Kind)} · PHASE {enemy.Phase}  //  {enemy.Health}/{enemy.MaxHealth}"
                        : $"{ArchiveEnemyName(enemy.Kind)}  //  {enemy.Health}/{enemy.MaxHealth}", hudCenteredStyle, 8);
                DrawRect(new Rect(x - 64, y + 68, 128, 7), Shadow);
                DrawRect(new Rect(x - 62, y + 69, 124 * enemy.Health / enemy.MaxHealth, 5), PostalRed);
                if (enemy.MaxArmor > 0)
                {
                    DrawRect(new Rect(x - 64, y + 78, 128, 6), new Color32(23, 38, 63, 245));
                    DrawRect(new Rect(x - 62, y + 79, 124 * enemy.Armor / enemy.MaxArmor, 4), NeonCyan);
                    DrawFittedLabel(new Rect(x - 64, y + 83, 128, 15),
                        L("battle.enemy_armor", "装甲 {0}/{1}", enemy.Armor, enemy.MaxArmor), tinyStyle, 7);
                }
            }

            DrawCombatEffects();
            DrawEnemyAttackEffects();
            DrawEnemyDeathEffects();

            DrawRect(new Rect(61, 520, 1468, 27), new Color32(5, 13, 32, 235));
            DrawRect(new Rect(61, 520, 7, 27), NeonCyan);
            DrawFittedLabel(new Rect(75, 522, 1438, 23), LocalizationService.IsEnglish
                ? L("battle.log.summary", "TURN {0} // READ INTENT, THEN PLAY OR SHIFT", battle.Turn)
                : battle.Log, tinyStyle, 8);
        }

        private void DrawBossProtocolPanels(EnemyState boss, Vector2 center)
        {
            bool phaseTwo = boss.Phase == 2;
            bool contractActive = phaseTwo && battle.BossContractProtocolWillTrigger();
            bool airframeActive = phaseTwo && battle.BossAirframeProtocolWillTrigger();
            Rect contractRect = new Rect(center.x - 220, center.y - 119, 440, 24);
            Rect airframeRect = new Rect(center.x - 220, center.y - 91, 440, 24);
            DrawBossProtocolLine(contractRect,
                L("boss.matrix.contract_line", "合同反制 // {0}", BossContractProtocolRule()),
                contractActive ? PostalRed : NeonCyan, phaseTwo);
            DrawBossProtocolLine(airframeRect,
                L("boss.matrix.airframe_line", "改装反制 // {0}", BossAirframeProtocolRule()),
                airframeActive ? PostalRed : NeonViolet, phaseTwo);
        }

        private void DrawBossProtocolLine(Rect rect, string text, Color color, bool online)
        {
            DrawRect(rect, new Color32(5, 12, 31, 246));
            DrawRect(new Rect(rect.x, rect.y, 6, rect.height), color);
            DrawPixelOutline(rect, color, online ? 2f : 1f);
            DrawFittedLabel(new Rect(rect.x + 13, rect.y + 2, rect.width - 20, rect.height - 4),
                online ? text : L("boss.matrix.phase_two", "PHASE 2 待机 // {0}", text), tinyStyle, 7);
        }

        private string BossContractProtocolName()
        {
            return battle.ActiveBossContractProtocol switch
            {
                BossContractProtocol.SealMirror => L("boss.protocol.contract.SealMirror.name", "密封镜像"),
                BossContractProtocol.CryoInversion => L("boss.protocol.contract.CryoInversion.name", "低温逆转"),
                BossContractProtocol.VectorIntercept => L("boss.protocol.contract.VectorIntercept.name", "矢量截获"),
                BossContractProtocol.ReserveSiphon => L("boss.protocol.contract.ReserveSiphon.name", "余量虹吸"),
                _ => L("boss.protocol.contract.GhostTrace.name", "幽灵追迹")
            };
        }

        private string BossContractProtocolRule()
        {
            return battle.ActiveBossContractProtocol switch
            {
                BossContractProtocol.SealMirror => L("boss.protocol.contract.SealMirror.rule",
                    "保留锁定 → 锁定-1，首领装甲+3"),
                BossContractProtocol.CryoInversion => L("boss.protocol.contract.CryoInversion.rule",
                    "热量≤1 → 首领装甲+3"),
                BossContractProtocol.VectorIntercept => L("boss.protocol.contract.VectorIntercept.rule",
                    "保留动量 → 动量-1，首领装甲+3"),
                BossContractProtocol.ReserveSiphon => L("boss.protocol.contract.ReserveSiphon.rule",
                    "保留1能量 → 首领装甲+3"),
                _ => L("boss.protocol.contract.GhostTrace.rule",
                    "航迹暴露1+ → 暴露+1，首领装甲+3")
            };
        }

        private string BossAirframeProtocolName()
        {
            return battle.ActiveBossAirframeProtocol switch
            {
                BossAirframeProtocol.ShieldCrack => L("boss.protocol.airframe.ShieldCrack.name", "裂盾回波"),
                BossAirframeProtocol.WidebandJam => L("boss.protocol.airframe.WidebandJam.name", "宽频干扰"),
                BossAirframeProtocol.ThermalLock => L("boss.protocol.airframe.ThermalLock.name", "热源锁定"),
                _ => L("boss.protocol.airframe.None.name", "标准扫描")
            };
        }

        private string BossAirframeProtocolRule()
        {
            return battle.ActiveBossAirframeProtocol switch
            {
                BossAirframeProtocol.ShieldCrack => L("boss.protocol.airframe.ShieldCrack.rule",
                    "护盾5+ → 清空当前护盾"),
                BossAirframeProtocol.WidebandJam => L("boss.protocol.airframe.WidebandJam.rule",
                    "保留5+张手牌 → 4点干扰伤害"),
                BossAirframeProtocol.ThermalLock => L("boss.protocol.airframe.ThermalLock.rule",
                    "热量4+ → 4点热寻伤害"),
                _ => L("boss.protocol.airframe.None.rule", "没有额外改装反制")
            };
        }

        private string BossStoryAlignmentName()
        {
            return battle.ActiveBossStoryAlignment switch
            {
                BossStoryAlignment.Allied => L("boss.story.Allied", "盟友反向脉冲"),
                BossStoryAlignment.Hostile => L("boss.story.Hostile", "敌对航路上传"),
                _ => L("boss.story.Neutral", "无信标支援")
            };
        }

        private void BeginLaneTransition(int fromLane, int toLane)
        {
            laneTransitionFrom = fromLane;
            laneTransitionTo = toLane;
            laneTransitionStart = Time.time;
            laneTransitionDuration = 0.62f;
            PlaySound(maneuverSound, toLane < fromLane ? 1.08f : 0.94f, 0.82f);
            StartCoroutine(PlayLaneArrivalSound(0.34f, toLane < fromLane ? 1.12f : 1f));
            TriggerShake(5.5f, 0.28f);
        }

        private IEnumerator PlayLaneArrivalSound(float delay, float pitch)
        {
            yield return new WaitForSecondsRealtime(delay);
            PlaySound(maneuverLockSound, pitch, 0.58f);
        }

        private float LaneTransitionProgress()
        {
            if (laneTransitionStart < 0f)
                return 1f;
            return Mathf.Clamp01((Time.time - laneTransitionStart) / Mathf.Max(0.01f, laneTransitionDuration));
        }

        private Vector2 PlayerVisualPosition(float t)
        {
            if (t >= 1f)
                return new Vector2(242f, 190f + battle.PlayerLane * 130f);

            float fromY = 190f + laneTransitionFrom * 130f;
            float toY = 190f + laneTransitionTo * 130f;
            float eased = t * t * (3f - 2f * t);
            float direction = Mathf.Sign(toY - fromY);
            float anticipation = t < 0.16f ? Mathf.Sin(t / 0.16f * Mathf.PI) * -13f * direction : 0f;
            float settle = Mathf.Sin(t * Mathf.PI) * 9f * direction;
            float boostX = Mathf.Sin(t * Mathf.PI) * 58f;
            return new Vector2(242f + boostX, Mathf.Lerp(fromY, toY, eased) + anticipation + settle);
        }

        private void DrawLaneTransitionFx(Vector2 playerPosition, float t)
        {
            float fromY = 190f + laneTransitionFrom * 130f;
            float toY = 190f + laneTransitionTo * 130f;
            float fade = 1f - Mathf.Abs(t * 2f - 1f) * 0.4f;
            float routeY = Mathf.Min(fromY, toY) - 22f;
            float routeHeight = Mathf.Abs(toY - fromY) + 44f;

            DrawRect(new Rect(232, routeY, 20, routeHeight), new Color32(74, 236, 241, (byte)(34 * fade)));
            DrawPixelOutline(new Rect(224, routeY, 36, routeHeight), new Color32(180, 91, 255, (byte)(115 * fade)), 3f);

            for (int i = 1; i <= 5; i++)
            {
                float ghostT = Mathf.Clamp01(t - i * 0.065f);
                Vector2 ghost = PlayerVisualPosition(ghostT);
                byte alpha = (byte)Mathf.Clamp(110 - i * 16, 18, 110);
                Color32 ghostColor = i % 2 == 0
                    ? new Color32(74, 236, 241, alpha)
                    : new Color32(255, 66, 218, alpha);
                DrawRect(new Rect(ghost.x - 50f, ghost.y - 13f, 72f, 26f), ghostColor);
                DrawRect(new Rect(ghost.x - 8f, ghost.y - 30f, 21f, 60f), new Color32(ghostColor.r, ghostColor.g, ghostColor.b, (byte)(alpha * 0.75f)));
            }

            for (int i = 0; i < 7; i++)
            {
                float streakX = playerPosition.x - 90f - i * 42f - t * 85f;
                float streakY = playerPosition.y - 34f + i * 11f;
                DrawRect(new Rect(streakX, streakY, 86f - i * 7f, 4f),
                    new Color32(i % 2 == 0 ? (byte)74 : (byte)255, i % 2 == 0 ? (byte)236 : (byte)66,
                        i % 2 == 0 ? (byte)241 : (byte)218, (byte)(190 - i * 20)));
            }

            float targetPulse = 0.55f + Mathf.Sin(Time.time * 30f) * 0.22f;
            DrawPixelOutline(new Rect(176f - 8f * targetPulse, toY - 50f - 8f * targetPulse,
                    132f + 16f * targetPulse, 100f + 16f * targetPulse),
                new Color32(74, 236, 241, (byte)(155 * (1f - t * 0.35f))), 4f);

            if (t > 0.72f)
            {
                float lockT = (t - 0.72f) / 0.28f;
                DrawImpactBurst(new Vector2(242f, toY), lockT, NeonCyan);
                DrawRect(new Rect(62, toY - 52, 1466, 104), new Color32(74, 236, 241, (byte)(45 * (1f - lockT))));
            }
        }

        private Vector2 EnemyBasePosition(int index, EnemyState enemy)
        {
            float x = battle.Encounter == EncounterId.Boss ? 1120f : 900f + index * 210f;
            return new Vector2(x, 188f + enemy.Lane * 130f);
        }

        private Vector2 EnemyVisualPosition(int index, EnemyState enemy, out EnemyLaneFx activeFx, out float t)
        {
            activeFx = enemyLaneFx.Find(fx => fx.Enemy == enemy && Time.time < fx.StartTime + fx.Duration);
            if (activeFx == null)
            {
                t = 1f;
                return EnemyBasePosition(index, enemy);
            }

            t = Mathf.Clamp01((Time.time - activeFx.StartTime) / Mathf.Max(0.01f, activeFx.Duration));
            return EnemyLaneTransitionPosition(index, activeFx, t);
        }

        private Vector2 EnemyLaneTransitionPosition(int index, EnemyLaneFx fx, float t)
        {
            float baseX = battle.Encounter == EncounterId.Boss ? 1120f : 900f + index * 210f;
            float fromY = 188f + fx.FromLane * 130f;
            float toY = 188f + fx.ToLane * 130f;
            float eased = t * t * (3f - 2f * t);
            float direction = Mathf.Sign(toY - fromY);
            float anticipation = t < 0.14f ? -Mathf.Sin(t / 0.14f * Mathf.PI) * 12f * direction : 0f;
            float settle = Mathf.Sin(t * Mathf.PI) * 8f * direction;
            float attackSwoop = -Mathf.Sin(t * Mathf.PI) * 78f;
            return new Vector2(baseX + attackSwoop, Mathf.Lerp(fromY, toY, eased) + anticipation + settle);
        }

        private void DrawEnemyLaneTransitionFx(int index, EnemyLaneFx fx, Vector2 position, float t)
        {
            float baseX = battle.Encounter == EncounterId.Boss ? 1120f : 900f + index * 210f;
            float fromY = 188f + fx.FromLane * 130f;
            float toY = 188f + fx.ToLane * 130f;
            float railTop = Mathf.Min(fromY, toY) - 42f;
            float railHeight = Mathf.Abs(toY - fromY) + 84f;
            byte routeAlpha = (byte)Mathf.Clamp(120f * (1f - t * 0.55f), 35f, 120f);

            DrawRect(new Rect(baseX - 5f, railTop, 10f, railHeight), new Color32(255, 56, 188, (byte)(routeAlpha / 2)));
            DrawPixelOutline(new Rect(baseX - 15f, railTop, 30f, railHeight), new Color32(179, 92, 255, routeAlpha), 3f);

            for (int i = 1; i <= 5; i++)
            {
                float ghostT = Mathf.Clamp01(t - i * 0.065f);
                Vector2 ghost = EnemyLaneTransitionPosition(index, fx, ghostT);
                byte alpha = (byte)Mathf.Clamp(105 - i * 15, 24, 105);
                Color32 ghostColor = i % 2 == 0
                    ? new Color32(255, 64, 174, alpha)
                    : new Color32(142, 91, 255, alpha);
                DrawRect(new Rect(ghost.x - 44f, ghost.y - 13f, 88f, 26f), ghostColor);
                DrawRect(new Rect(ghost.x - 17f, ghost.y - 31f, 34f, 62f), new Color32(ghostColor.r, ghostColor.g, ghostColor.b, (byte)(alpha * 0.72f)));
            }

            for (int i = 0; i < 6; i++)
            {
                float streakX = position.x + 48f + i * 34f + t * 42f;
                float streakY = position.y - 30f + i * 12f;
                DrawRect(new Rect(streakX, streakY, 82f - i * 8f, 4f),
                    new Color32(i % 2 == 0 ? (byte)255 : (byte)137, i % 2 == 0 ? (byte)58 : (byte)92,
                        i % 2 == 0 ? (byte)178 : (byte)255, (byte)(190 - i * 22)));
            }

            float targetPulse = 0.6f + Mathf.Sin(Time.time * 34f + fx.Seed) * 0.22f;
            DrawPixelOutline(new Rect(baseX - 84f - targetPulse * 7f, toY - 58f - targetPulse * 7f,
                    168f + targetPulse * 14f, 116f + targetPulse * 14f),
                new Color32(255, 65, 184, (byte)(170 * (1f - t * 0.35f))), 4f);

            if (t > 0.72f)
            {
                float lockT = (t - 0.72f) / 0.28f;
                DrawImpactBurst(new Vector2(baseX, toY), lockT, NeonViolet);
                DrawRect(new Rect(62, toY - 52, 1466, 104), new Color32(255, 51, 172, (byte)(40 * (1f - lockT))));
            }
        }

        private void DrawHand()
        {
            DrawRect(new Rect(40, 574, 920, 38), new Color32(7, 16, 39, 225));
            DrawRect(new Rect(40, 574, 6, 38), NeonViolet);
            GUI.Label(new Rect(58, 577, 885, 34), L("battle.piles",
                "回合 {0:00} // 手牌 {1} // 抽牌 {2} // 弃牌 {3}",
                battle.Turn, battle.Hand.Count, battle.DrawCount, battle.DiscardCount), hudStyle);
            Rect comboStrip = new Rect(965, 574, 170, 38);
            DrawRect(comboStrip, new Color32(7, 18, 43, 235));
            DrawNeonFrame(comboStrip, battle.EvasionExposure >= 2 ? PostalRed : battle.LockOn > 0 ? Gold : NeonCyan, 2f);
            DrawFittedLabel(comboStrip, L("battle.resources", "锁定 {0} / 动量 {1} / 航迹 {2}",
                battle.LockOn, battle.Momentum, battle.EvasionExposure), hudCenteredStyle, 8);
            if (runModules.Count > 0)
            {
                Rect moduleStrip = new Rect(1145, 574, 210, 38);
                DrawRect(moduleStrip, new Color32(70, 43, 15, 235));
                DrawNeonFrame(moduleStrip, new Color32(255, 194, 58, 255), 2f);
                DrawFittedLabel(moduleStrip, $"MODULE // {ModuleName(runModules[0])}", hudCenteredStyle, 11);
            }
            GetHandLayout(out float cardWidth, out float gap, out float startX);

            for (int i = 0; i < battle.Hand.Count; i++)
                DrawCard(i, new Rect(startX + i * (cardWidth + gap), 620, cardWidth, 235));

            Rect endTurn = new Rect(1360, 705, 180, 72);
            DrawPixelButton(endTurn, L("battle.end_turn", "结束回合"), Shadow, EndTurnWithFeedback,
                !battle.Victory && !battle.Defeat && Time.time >= battleInputLockUntil, "SPACE");
            GUI.Label(new Rect(1352, 790, 194, 42),
                L("battle.intent_warning", "! 敌人将执行\n当前显示的意图"), tinyStyle);
        }

        private void GetHandLayout(out float cardWidth, out float gap, out float startX)
        {
            int count = Mathf.Max(1, battle.Hand.Count);
            if (count >= 6)
            {
                const float availableWidth = 1265f;
                gap = 12f;
                cardWidth = Mathf.Clamp((availableWidth - (count - 1) * gap) / count, 160f, 218f);
                float handWidth = count * cardWidth + (count - 1) * gap;
                startX = 45f + Mathf.Max(0f, (availableWidth - handWidth) * 0.5f);
                return;
            }

            cardWidth = 218f;
            gap = 18f;
            float defaultHandWidth = count * cardWidth + Mathf.Max(0, count - 1) * gap;
            startX = Mathf.Max(45f, (ReferenceWidth - defaultHandWidth) * 0.5f - 70f);
        }

        private void DrawCard(int index, Rect rect)
        {
            bool underMouse = rect.Contains(Event.current.mousePosition);
            bool hovered = underMouse && CanPlayInteractive(index);
            if (hovered)
            {
                rect.y -= 14f + Mathf.Sin(Time.time * 8f) * 2f;
                RegisterHover($"hand-{index}", "点击打出这张牌");
            }
            CardSpec card = CardLibrary.Get(battle.Hand[index]);
            bool upgraded = battle.IsUpgraded(card.Id);
            bool rulesPlayable = battle.CanPlay(index);
            bool playable = CanPlayInteractive(index);
            Color family = CardLibrary.FamilyColor(card.Family);
            if (upgraded)
                family = Color.Lerp(family, new Color32(255, 202, 75, 255), 0.45f);
            if (!playable)
                family = Color.Lerp(family, new Color32(90, 96, 108, 255), 0.6f);

            if (hovered)
            {
                DrawRect(new Rect(rect.x - 5, rect.y - 5, rect.width + 10, rect.height + 10), new Color32(72, 232, 240, 75));
                DrawNeonFrame(new Rect(rect.x - 3, rect.y - 3, rect.width + 6, rect.height + 6), NeonCyan, 2f);
            }
            DrawRect(new Rect(rect.x + 7, rect.y + 7, rect.width, rect.height), new Color32(2, 7, 20, 255));
            DrawRect(rect, new Color32(239, 235, 211, 255));
            DrawPixelOutline(rect, new Color32(10, 22, 49, 255), 3f);
            if (upgraded)
                DrawNeonFrame(new Rect(rect.x - 2, rect.y - 2, rect.width + 4, rect.height + 4), new Color32(255, 205, 82, 255), 2f);
            DrawRect(new Rect(rect.x, rect.y, rect.width, 48), family);
            const float footerHeight = 28f;
            DrawRect(new Rect(rect.x, rect.y + rect.height - footerHeight, rect.width, footerHeight), family);

            if (playable)
            {
                float sheenX = rect.x + Mathf.Repeat(Time.time * 95f + index * 47f, rect.width + 70f) - 45f;
                DrawRect(new Rect(sheenX, rect.y + 50, 14, rect.height - 80), new Color32(255, 255, 255, hovered ? (byte)28 : (byte)16));
                DrawRect(new Rect(sheenX + 14, rect.y + 50, 5, rect.height - 80), new Color32(75, 238, 245, hovered ? (byte)38 : (byte)22));
            }

            DrawRect(new Rect(rect.x + 12, rect.y + 57, 46, 46), Shadow);
            GUI.Label(new Rect(rect.x + 12, rect.y + 62, 46, 35), card.Cost.ToString(), tinyStyle);
            DrawFittedLabel(new Rect(rect.x + 66, rect.y + 60, rect.width - 76, 45), upgraded ? card.Name + "+" : card.Name, cardTitleStyle, 12);
            DrawFittedLabel(new Rect(rect.x + 14, rect.y + 110, rect.width - 28, 92),
                upgraded ? UpgradedRules(card.Id, battle.UpgradeBranchFor(card.Id)) : card.Rules, cardBodyStyle, 11);
            GUI.Label(new Rect(rect.x + 14, rect.y + rect.height - footerHeight, rect.width - 28, footerHeight),
                card.Heat > 0 ? $"+{card.Heat} 热量" : "无热量", tinyStyle);

            if (!playable)
            {
                if (underMouse)
                {
                    int missing = Mathf.Max(0, card.Cost - battle.Energy);
                    RegisterHover($"hand-locked-{index}", !rulesPlayable && missing > 0 ? $"还差 {missing} 点能量" : Time.time < battleInputLockUntil ? "动作执行中，请等待反馈完成" : "当前无法打出这张牌");
                }
                DrawRect(new Rect(rect.x, rect.y, rect.width, rect.height), new Color32(8, 13, 27, 105));
                DrawRect(new Rect(rect.x + 18, rect.y + 102, rect.width - 36, 30), new Color32(20, 27, 45, 225));
                GUI.Label(new Rect(rect.x + 18, rect.y + 104, rect.width - 36, 26),
                    Time.time < battleInputLockUntil ? "动作执行中" : "能量不足", tinyStyle);
            }

            bool oldEnabled = GUI.enabled;
            GUI.enabled = playable;
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                PlayCardWithFeedback(index);
            GUI.enabled = oldEnabled;
        }

        private void DrawResultOverlay(bool victory)
        {
            if (!victory)
            {
                DrawFailureDebrief();
                return;
            }

            DrawRect(new Rect(0, 0, ReferenceWidth, ReferenceHeight), new Color32(20, 29, 43, 210));
            Rect panel = new Rect(430, 215, 740, 470);
            DrawRect(new Rect(panel.x + 10, panel.y + 10, panel.width, panel.height), Shadow);
            DrawRect(panel, Paper);
            DrawRect(new Rect(panel.x, panel.y, panel.width, 18), victory ? new Color32(62, 167, 137, 255) : PostalRed);

            string victoryTitle = battle.Encounter == EncounterId.Boss ? "首领击破" : "战斗胜利";
            DrawFittedLabel(new Rect(480, 270, 640, 72), victory ? victoryTitle : "航线失败", titleStyle, 26);
            string result = victory
                ? battle.Encounter == EncounterId.Boss
                    ? "磁暴鳐已经坠入云海，通往群岛诊所的航路终于畅通。"
                    : battle.CargoIntegrity == 3
                        ? "该段航线已经清理，货物保持完好。"
                        : "敌机已被击退，但货物在交火中受到了一些损伤。"
                : "风车群岛仍在等待这批救命药剂。";
            DrawFittedLabel(new Rect(520, 365, 560, 86), result, bodyStyle, 11);
            DrawFittedLabel(new Rect(500, 460, 600, 58), victory
                ? $"合同评级 {CargoGrade(battle.CargoIntegrity)} · {CargoStatus(battle.CargoIntegrity)}　|　完整度 {CargoPips(battle.CargoIntegrity)} {battle.CargoIntegrity}/3"
                : "提示：优先规避高伤害攻击意图", subtitleStyle, 10);
            DrawBattleDebrief(new Rect(515, 520, 570, 32));

            if (victory)
            {
                DrawPixelButton(new Rect(635, 565, 330, 66), battle.Encounter == EncounterId.Boss ? "完成配送" : "领取战利品",
                    PostalRed, ContinueAfterVictory, true, "ENTER");
            }
            else
            {
                DrawPixelButton(new Rect(545, 565, 235, 66), "重新出发", PostalRed, StartNewRun, true, "ENTER");
                DrawPixelButton(new Rect(820, 565, 235, 66), "返回标题", Shadow, () => screen = ScreenMode.Title);
            }
        }

        private void DrawFailureDebrief()
        {
            DrawRect(new Rect(0, 0, ReferenceWidth, ReferenceHeight), new Color32(4, 8, 20, 232));
            Rect panel = new Rect(80, 42, 1440, 816);
            DrawRect(new Rect(panel.x + 10, panel.y + 10, panel.width, panel.height), Shadow);
            DrawRect(panel, new Color32(7, 18, 43, 255));
            DrawNeonFrame(panel, PostalRed, 3f);
            DrawRect(new Rect(panel.x, panel.y, panel.width, 14), PostalRed);
            DrawFittedLabel(new Rect(125, 72, 710, 62),
                L("failure.title", "航线失事复盘"), neonTitleStyle, 25);
            DrawFittedLabel(new Rect(945, 81, 515, 36),
                L("failure.archived", "档案已记录 // 第 {0} 次失事 · SEED {1}",
                    archiveData.EncountersLost, runSeed.ToString("X8")),
                hudCenteredStyle, 9);

            RunDebriefSummary debrief = CurrentFailureDebrief();

            Rect cause = new Rect(125, 150, 650, 120);
            DrawRect(cause, new Color32(34, 16, 32, 248));
            DrawPixelOutline(cause, PostalRed, 3f);
            DrawFittedLabel(new Rect(cause.x + 24, cause.y + 12, cause.width - 48, 30),
                L("failure.cause", "致命原因 // {0}", FailureCauseTitle(battle.DefeatSource)), hudStyle, 11);
            string dealer = string.IsNullOrEmpty(battle.DefeatDealer)
                ? L("failure.unknown", "未知威胁")
                : battle.DefeatDealer;
            int rawDamage = battle.DefeatRawDamage > 0 ? battle.DefeatRawDamage : battle.DefeatDamage;
            DrawFittedLabel(new Rect(cause.x + 24, cause.y + 49, cause.width - 48, 48),
                L("failure.damage_detail",
                    "第 {0} 回合 · {1}：原始伤害 {2} / 护盾吸收 {3} / 机体损失 {4}（击中前 {5}）",
                    Mathf.Max(1, battle.DefeatTurn), dealer, rawDamage, battle.DefeatShieldAbsorbed,
                    battle.DefeatDamage, battle.DefeatHullBefore), tinyStyle, 9);

            Rect mistake = new Rect(125, 286, 650, 116);
            DrawRect(mistake, new Color32(26, 20, 39, 248));
            DrawPixelOutline(mistake, Gold, 2f);
            DrawFittedLabel(new Rect(mistake.x + 22, mistake.y + 10, mistake.width - 44, 25),
                L("failure.mistake", "本局关键失误 // 基于战斗记录的风险判断"), tinyStyle, 9);
            DrawFittedLabel(new Rect(mistake.x + 22, mistake.y + 39, mistake.width - 44, 62),
                debrief.KeyMistakeMessage, neonBodyStyle, 9);

            Rect weakness = new Rect(125, 418, 650, 116);
            DrawRect(weakness, new Color32(8, 24, 46, 248));
            DrawPixelOutline(weakness, NeonViolet, 2f);
            DrawFittedLabel(new Rect(weakness.x + 22, weakness.y + 10, weakness.width - 44, 25),
                L("failure.weakness", "构筑短板 // 终局快照"), tinyStyle, 9);
            DrawFittedLabel(new Rect(weakness.x + 22, weakness.y + 39, weakness.width - 44, 62),
                debrief.BuildWeaknessMessage, neonBodyStyle, 9);

            Rect strategy = new Rect(125, 550, 650, 125);
            DrawRect(strategy, new Color32(8, 31, 42, 248));
            DrawPixelOutline(strategy, NeonCyan, 3f);
            DrawFittedLabel(new Rect(strategy.x + 22, strategy.y + 10, strategy.width - 44, 25),
                L("failure.next", "再次出发策略"), tinyStyle, 9);
            DrawFittedLabel(new Rect(strategy.x + 22, strategy.y + 39, strategy.width - 44, 68),
                debrief.NextStrategy, neonBodyStyle, 10);

            Rect build = new Rect(800, 150, 660, 244);
            DrawRect(build, new Color32(8, 21, 43, 248));
            DrawPixelOutline(build, NeonCyan, 2f);
            DrawFittedLabel(new Rect(build.x + 22, build.y + 11, build.width - 44, 26),
                L("failure.build_title", "构筑清单 // {0}", BuildProfileLabel(CurrentBuildProfile())),
                hudStyle, 10);
            DrawFittedLabel(new Rect(build.x + 22, build.y + 43, build.width - 44, 30),
                debrief.BuildSummaryMessage, tinyStyle, 8);
            DrawFittedLabel(new Rect(build.x + 22, build.y + 78, build.width - 44, 44),
                L("failure.deck", "核心牌 // {0}", DeckDebriefSummary()), neonBodyStyle, 8);
            DrawFittedLabel(new Rect(build.x + 22, build.y + 126, build.width - 44, 40),
                L("failure.upgrades", "分支强化 // {0}", UpgradeDebriefSummary()), neonBodyStyle, 8);
            DrawFittedLabel(new Rect(build.x + 22, build.y + 171, build.width - 44, 52),
                L("failure.modules", "模块 // {0}\n机体 // {1}", ModuleDebriefSummary(),
                    AirframeModificationName(runModification)), neonBodyStyle, 8);

            Rect routeSummary = new Rect(800, 410, 660, 150);
            DrawRect(routeSummary, new Color32(27, 24, 35, 248));
            DrawPixelOutline(routeSummary, Gold, 2f);
            DrawFittedLabel(new Rect(routeSummary.x + 22, routeSummary.y + 10, routeSummary.width - 44, 26),
                L("failure.route_title", "路线收益 // {0}", RouteProfileLabel(CurrentRouteProfile())),
                hudStyle, 10);
            DrawFittedLabel(new Rect(routeSummary.x + 22, routeSummary.y + 42, routeSummary.width - 44, 45),
                debrief.RouteGainsMessage, neonBodyStyle, 8);
            DrawFittedLabel(new Rect(routeSummary.x + 22, routeSummary.y + 91, routeSummary.width - 44, 43),
                L("failure.route_context", "纪事：{0}　|　合同被动 {1} 次　|　风险报酬 +{2} 邮票",
                    RouteStoryStatus(), debrief.ContractPassiveProcs, debrief.ContractBonusCredits),
                tinyStyle, 8);

            Rect metrics = new Rect(800, 576, 660, 99);
            DrawRect(metrics, new Color32(10, 18, 37, 248));
            DrawPixelOutline(metrics, new Color32(79, 105, 142, 255), 2f);
            DrawFittedLabel(new Rect(metrics.x + 20, metrics.y + 12, metrics.width - 40, 31),
                L("failure.metrics", "航程 {0} 回合 · 出牌 {1} · 机体受损 {2} · 过热 {3}",
                    debrief.Turns, debrief.CardsPlayed, debrief.DamageTaken, debrief.Overheats),
                tinyStyle, 9);
            DrawFittedLabel(new Rect(metrics.x + 20, metrics.y + 49, metrics.width - 40, 31),
                L("failure.telegraphs", "灾变：打断 {0} / 规避 {1} / 命中 {2}　·　追踪命中 {3}",
                    debrief.CalamityInterrupts, debrief.CalamityEvades, debrief.CalamityHits,
                    debrief.TrackingHits), tinyStyle, 9);

            bool canChangeSeed = ChallengeCatalog.Get(currentChallenge).FixedSeed == 0;
            DrawPixelButton(new Rect(125, 705, 405, 66),
                L("failure.same_seed", "同种子复飞"), PostalRed, RestartSameSeed, true, "ENTER");
            DrawPixelButton(new Rect(598, 705, 405, 66), canChangeSeed
                    ? L("failure.new_seed", "同合同 · 新种子")
                    : L("failure.fixed_seed", "挑战种子固定"),
                NeonCyan, RestartSameContract, canChangeSeed);
            DrawPixelButton(new Rect(1070, 705, 390, 66),
                L("failure.change", "调整合同"), Gold, ChangeContractAfterFailure);
            DrawFittedLabel(new Rect(145, 786, 1295, 35), canChangeSeed
                    ? L("failure.retry_note",
                        "同种子用于验证单一策略；新种子保留【{0}】但重排编队、奖励与商店。失败已结算，不保留可继续检查点。",
                        CargoName(selectedContract))
                    : L("failure.retry_fixed_note",
                        "当前挑战锁定种子；可同种子验证策略，或调整合同后以同一挑战种子重新出发。失败已结算。"),
                tinyStyle, 8);
        }

        private RunDebriefSummary CurrentFailureDebrief()
        {
            return RunDebriefAnalyzer.Analyze(runBuildSnapshots, new RunDebriefMetrics
            {
                Contract = (int)selectedContract,
                Encounter = (int)battle.Encounter,
                DefeatSource = battle.HasDefeatCause ? (int)battle.DefeatSource : -1,
                DefeatDealer = battle.DefeatDealer,
                DefeatDamage = battle.DefeatDamage,
                Turns = runTurns + battle.Turn,
                CardsPlayed = runCardsPlayed + battle.CardsPlayed,
                DamageTaken = runDamageTaken + battle.DamageTaken,
                Overheats = runOverheats + battle.OverheatCount,
                CalamityInterrupts = runCalamityInterrupts + battle.CalamityInterrupts,
                CalamityEvades = runCalamityEvades + battle.CalamityEvades,
                CalamityHits = runCalamityHits + battle.CalamityHits,
                TrackingHits = runTrackingHits + battle.TrackingHits,
                ContractPassiveProcs = runContractProcs + battle.ContractPassiveProcs,
                ContractBonusCredits = runContractBonus,
                FinalHull = battle.PlayerHealth,
                FinalCargoIntegrity = battle.CargoIntegrity,
                FinalCredits = credits
            });
        }

        private string DeckDebriefSummary()
        {
            IGrouping<CardId, CardId>[] groups = runDeck.GroupBy(card => card)
                .OrderByDescending(group => runUpgrades.Contains(group.Key))
                .ThenByDescending(group => group.Count())
                .ThenBy(group => group.Key)
                .ToArray();
            string shown = string.Join(" · ", groups.Take(6).Select(group =>
                $"{CardLibrary.Get(group.Key).Name}{(runUpgrades.Contains(group.Key) ? "+" : string.Empty)}" +
                (group.Count() > 1 ? $"×{group.Count()}" : string.Empty)));
            return groups.Length > 6 ? $"{shown} · 另 {groups.Length - 6} 种" : shown;
        }

        private string UpgradeDebriefSummary()
        {
            if (runUpgrades.Count == 0)
                return L("failure.none", "无");
            return string.Join(" · ", runUpgrades.OrderBy(card => card).Take(5).Select(card =>
            {
                string branch = runUpgradeBranches.TryGetValue(card, out UpgradeBranch value)
                    ? value == UpgradeBranch.Alpha ? "A" : "B"
                    : "基础";
                return $"{CardLibrary.Get(card).Name} {branch}";
            })) + (runUpgrades.Count > 5 ? $" · +{runUpgrades.Count - 5}" : string.Empty);
        }

        private string ModuleDebriefSummary()
        {
            return runModules.Count == 0
                ? L("failure.none", "无")
                : string.Join(" · ", runModules.Select(ModuleName));
        }

        private static string BuildProfileLabel(string key)
        {
            return key switch
            {
                "weapon" => L("stats.build.weapon", "火力主轴"),
                "maneuver" => L("stats.build.maneuver", "机动主轴"),
                "defense" => L("stats.build.defense", "防护主轴"),
                "utility" => L("stats.build.utility", "调度主轴"),
                _ => L("stats.build.hybrid", "混合构筑")
            };
        }

        private static string RouteProfileLabel(string key)
        {
            return key switch
            {
                "pressure" => L("stats.route.pressure", "高压战线"),
                "service" => L("stats.route.service", "补给航线"),
                "silent" => L("stats.route.silent", "静默航线"),
                _ => L("stats.route.mixed", "混合航线")
            };
        }

        private static string FailureCauseTitle(PlayerDamageSource source)
        {
            return source switch
            {
                PlayerDamageSource.LaneBlock => L("failure.block", "航道封锁"),
                PlayerDamageSource.StormField => L("failure.storm", "全航道风暴"),
                PlayerDamageSource.TrackingShot => L("failure.tracking", "航迹追踪"),
                PlayerDamageSource.CalamityStrike => L("failure.calamity", "灾变蓄力"),
                PlayerDamageSource.BossStrike => L("failure.boss", "首领核心冲击"),
                PlayerDamageSource.BossSplash => L("failure.splash", "吞界磁暴溅射"),
                PlayerDamageSource.Overheat => L("failure.overheat", "引擎过热"),
                PlayerDamageSource.HandJam => L("failure.hand_jam", "手牌干扰反噬"),
                PlayerDamageSource.HeatSeek => L("failure.heat_seek", "高热追踪"),
                PlayerDamageSource.BossWidebandJam => L("failure.boss_wideband", "首领宽频干扰"),
                PlayerDamageSource.BossThermalLock => L("failure.boss_thermal", "首领热源锁定"),
                PlayerDamageSource.BossCurtain => L("failure.boss_curtain", "雷幕封航"),
                PlayerDamageSource.PreludeCurtain => "先导雷幕",
                PlayerDamageSource.PreludeMagnet => "磁针扫掠",
                _ => L("failure.direct", "同航道直接攻击")
            };
        }

        private static string FailureAdvice(PlayerDamageSource source)
        {
            return source switch
            {
                PlayerDamageSource.LaneBlock =>
                    L("failure.advice.block", "噬邮兽只会重击自己封锁的航道。结束回合前离开它所在航道，或先用护盾吸收伤害。"),
                PlayerDamageSource.StormField =>
                    L("failure.advice.storm", "风暴气囊会覆盖全部航道，换道无法规避。优先击落它，或在结束回合前建立足够护盾。"),
                PlayerDamageSource.TrackingShot =>
                    L("failure.advice.tracking", "连续回合换道会提高航迹暴露。停留一回合，或使用信号扰频、矢量刹车主动清除暴露。"),
                PlayerDamageSource.CalamityStrike =>
                    L("failure.advice.calamity", "观察紫色锁定航道：提前换道即可规避，也可以在蓄力回合集中伤害打断灾变无人机。"),
                PlayerDamageSource.BossStrike =>
                    L("failure.advice.boss", "磁暴鳐会提前标记核心冲击航道。离开标记区，或集中火力达到打断阈值。"),
                PlayerDamageSource.BossSplash =>
                    L("failure.advice.splash", "首领第二阶段会伤及相邻航道。不要只移动一格；尽量与锁定航道拉开两格距离或打断蓄力。"),
                PlayerDamageSource.Overheat =>
                    L("failure.advice.overheat", "红线出牌会直接损伤机体。为应急冷却或低温泵保留行动顺序，在打出高热牌前检查热量。"),
                PlayerDamageSource.HandJam =>
                    L("failure.advice.hand_jam", "噪声织网只会在你以5张以上手牌结束回合时开火。主动打出低费牌，把手牌降至4张以下。"),
                PlayerDamageSource.HeatSeek =>
                    L("failure.advice.heat_seek", "热寻隼会锁定4点以上热量。结束回合前主动冷却，或优先击落它以保留高热爆发窗口。"),
                PlayerDamageSource.BossWidebandJam =>
                    L("failure.advice.boss_wideband", "开放航电会让首领读取结束回合手牌。将手牌降至4张以下，再处理已预告的核心冲击。"),
                PlayerDamageSource.BossThermalLock =>
                    L("failure.advice.boss_thermal", "红线涡轮会让首领锁定4点以上热量。结束回合前冷却到安全范围，再处理核心冲击航道。"),
                PlayerDamageSource.BossCurtain =>
                    L("failure.advice.boss_curtain", "雷幕云龙标出的不是危险区，而是唯一安全航道。结束回合前进入青色航道，或集中伤害打断雷幕。"),
                PlayerDamageSource.PreludeCurtain =>
                    "雷幕先导会标出唯一安全航道。进入青色航道，或在蓄力期间造成足够伤害使其短路。",
                PlayerDamageSource.PreludeMagnet =>
                    "磁针鳐卫会同时扫掠锁定航道与邻道。移动到距离标记两格的航道，或在结算前打断蓄力。",
                _ => L("failure.advice.direct", "结束回合前再次检查全部敌人意图。换离攻击航道，或用护盾把即将承受的机体伤害降到安全范围。")
            };
        }

        private void DrawBattleDebrief(Rect rect)
        {
            DrawRect(rect, new Color32(5, 13, 34, 238));
            DrawPixelOutline(rect, battle.Enemies.Any(enemy => enemy.Kind == EnemyKind.CalamityDrone) ? NeonViolet : NeonCyan, 2f);
            string report = battle.Enemies.Any(enemy => enemy.Kind == EnemyKind.CalamityDrone)
                ? $"战术复盘 // 打断 {battle.CalamityInterrupts}　规避 {battle.CalamityEvades}　命中 {battle.CalamityHits}"
                : $"敌情复盘 // {battle.FormationName}";
            DrawFittedLabel(new Rect(rect.x + 10, rect.y + 3, rect.width - 20, rect.height - 6), report, tinyStyle, 10);
        }

        private void DrawSky()
        {
            DrawArcadeScrollingBackdrop(new Rect(0, 0, ReferenceWidth, ReferenceHeight), screen == ScreenMode.Battle);
        }

        private void DrawCombatEffects()
        {
            float elapsed = Time.time - combatFxStart;
            if (combatFx == CombatFx.None || elapsed < 0f || elapsed > combatFxDuration)
                return;

            float t = Mathf.Clamp01(elapsed / combatFxDuration);
            float laneY = 190 + combatFxLane * 130;
            Color cyan = new Color32(104, 242, 231, 220);
            Color yellow = new Color32(255, 227, 92, 245);

            switch (combatFx)
            {
                case CombatFx.Shot:
                    DrawAttackCardEffect(combatFxCard, t, laneY);
                    break;
                case CombatFx.Volley:
                    DrawAttackCardEffect(combatFxCard, t, laneY);
                    break;
                case CombatFx.Shield:
                    DrawRect(new Rect(150 - 12 * t, laneY - 72 - 12 * t, 184 + 24 * t, 144 + 24 * t), new Color32(61, 239, 246, (byte)(52 * (1f - t))));
                    DrawPixelOutline(new Rect(164 - 14 * t, laneY - 62 - 14 * t, 156 + 28 * t, 124 + 28 * t), cyan, 6);
                    break;
                case CombatFx.Maneuver:
                    float routeTop = 190 + Mathf.Min(laneTransitionFrom, laneTransitionTo) * 130f;
                    float routeBottom = 190 + Mathf.Max(laneTransitionFrom, laneTransitionTo) * 130f;
                    DrawRect(new Rect(226, routeTop, 32, Mathf.Max(4f, routeBottom - routeTop)),
                        new Color32(74, 236, 241, (byte)(55 * (1f - t))));
                    DrawPixelOutline(new Rect(218, routeTop - 12, 48, routeBottom - routeTop + 24),
                        new Color32(180, 91, 255, (byte)(200 * (1f - t))), 3f);
                    break;
                case CombatFx.Coolant:
                    for (int i = 0; i < 4; i++)
                        DrawRect(new Rect(180 + i * 30, laneY - 55 - t * 50 - i * 5, 16, 26), new Color32(91, 224, 235, (byte)(220 - i * 35)));
                    break;
                case CombatFx.Overclock:
                    for (int i = 0; i < 6; i++)
                    {
                        float sparkX = 205 + Mathf.Sin(t * 9f + i) * (38 + i * 3);
                        float sparkY = laneY + Mathf.Cos(t * 11f + i * 1.7f) * (26 + i * 2);
                        DrawRect(new Rect(sparkX, sparkY, 8, 8), yellow);
                    }
                    break;
                case CombatFx.EnemyHit:
                    DrawRect(new Rect(0, 128, ReferenceWidth, 435), new Color32(232, 45, 74, (byte)(60 * (1f - t))));
                    DrawPixelOutline(new Rect(175 - 12 * t, laneY - 58 - 12 * t, 145 + 24 * t, 116 + 24 * t), PostalRed, 7);
                    DrawImpactBurst(new Vector2(242, laneY), t, PostalRed);
                    break;
            }

            if (!string.IsNullOrEmpty(combatFxText))
            {
                float textY = laneY - 92 - t * 28;
                DrawRect(new Rect(515, textY, 250, 30), new Color32(35, 62, 82, (byte)(230 * (1f - t))));
                GUI.Label(new Rect(520, textY + 2, 240, 25), combatFxText, tinyStyle);
            }
        }

        // Weapon cards share impact primitives, but each keeps a distinct silhouette and cadence.
        private void DrawAttackCardEffect(CardId card, float t, float laneY)
        {
            Color yellow = new Color32(255, 227, 92, 245);
            Color cyan = new Color32(104, 242, 231, 235);
            Color violet = new Color32(205, 92, 255, 235);
            Color frost = new Color32(178, 244, 255, 240);
            float targetX = impactPoint.x;

            switch (card)
            {
                case CardId.BurstFire:
                {
                    for (int shot = 0; shot < 3; shot++)
                    {
                        float local = Mathf.Clamp01((t - shot * 0.105f) / 0.38f);
                        if (local <= 0f || local >= 1f)
                            continue;
                        float x = Mathf.Lerp(310f, targetX, local);
                        float y = laneY + (shot - 1) * 11f;
                        DrawRect(new Rect(x - 42f, y - 3f, 66f, 6f), shot == 1 ? Color.white : yellow);
                        DrawRect(new Rect(300f, y - 1f, Mathf.Max(0f, x - 300f), 2f), new Color32(255, 210, 74, 80));
                    }
                    if (t > 0.46f)
                        DrawHitExplosion(impactPoint, (t - 0.46f) / 0.54f, yellow, impactDamage, 0.82f);
                    break;
                }
                case CardId.OverloadAim:
                {
                    float charge = Mathf.Clamp01(t / 0.3f);
                    float fire = Mathf.Clamp01((t - 0.28f) / 0.3f);
                    float aperture = 38f * (1f - charge);
                    DrawPixelOutline(new Rect(242f - aperture, laneY - aperture, aperture * 2f, aperture * 2f), violet, 4f);
                    if (fire > 0f)
                    {
                        float width = Mathf.Lerp(0f, targetX - 278f, fire);
                        DrawRect(new Rect(278f, laneY - 19f, width, 38f), new Color32(255, 70, 186, 110));
                        DrawRect(new Rect(278f, laneY - 7f, width, 14f), Color.white);
                    }
                    if (t > 0.48f)
                        DrawHitExplosion(impactPoint, (t - 0.48f) / 0.52f, violet, impactDamage, 1.75f);
                    break;
                }
                case CardId.RailPiercer:
                {
                    float fire = Mathf.Clamp01(t / 0.24f);
                    float x = Mathf.Lerp(292f, targetX + 85f, fire);
                    DrawRect(new Rect(285f, laneY - 10f, Mathf.Max(0f, x - 285f), 20f), new Color32(69, 238, 255, 72));
                    DrawRect(new Rect(285f, laneY - 2f, Mathf.Max(0f, x - 285f), 4f), Color.white);
                    for (int ring = 0; ring < 5; ring++)
                    {
                        float ringX = Mathf.Lerp(330f, targetX, ring / 4f);
                        float ringSize = 16f + Mathf.Repeat(t * 70f + ring * 9f, 24f);
                        DrawPixelOutline(new Rect(ringX - 3f, laneY - ringSize, 6f, ringSize * 2f), cyan, 2f);
                    }
                    if (t > 0.22f)
                        DrawHitExplosion(impactPoint, (t - 0.22f) / 0.78f, cyan, impactDamage, 1.28f);
                    break;
                }
                case CardId.PursuitShot:
                case CardId.SlipstreamStrike:
                {
                    float travel = Mathf.Clamp01(t / 0.58f);
                    for (int segment = 0; segment < 10; segment++)
                    {
                        float segmentT = Mathf.Clamp01(travel - segment * 0.045f);
                        float x = Mathf.Lerp(300f, targetX, segmentT);
                        float y = Mathf.Lerp(laneY, impactPoint.y, segmentT) + Mathf.Sin(segmentT * 12f + segment) * 18f * (1f - segmentT);
                        float size = Mathf.Max(3f, 14f - segment);
                        DrawRect(new Rect(x - size, y - size * 0.35f, size * 2f, size * 0.7f), segment < 3 ? yellow : cyan);
                    }
                    float reticle = 28f + Mathf.Sin(t * 22f) * 6f;
                    DrawPixelOutline(new Rect(targetX - reticle, impactPoint.y - reticle, reticle * 2f, reticle * 2f), yellow, 3f);
                    if (t > 0.56f)
                        DrawHitExplosion(impactPoint, (t - 0.56f) / 0.44f, yellow, impactDamage, 1.05f);
                    break;
                }
                case CardId.AegisRam:
                case CardId.PrismEcho:
                {
                    float travel = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.62f));
                    float x = Mathf.Lerp(245f, targetX, travel);
                    DrawRect(new Rect(245f, laneY - 30f, Mathf.Max(0f, x - 245f), 60f), new Color32(79, 235, 247, 45));
                    DrawPixelOutline(new Rect(x - 24f, laneY - 55f, 48f, 110f), cyan, 8f);
                    DrawRect(new Rect(x - 13f, laneY - 43f, 26f, 86f), new Color32(255, 213, 81, 180));
                    if (t > 0.56f)
                        DrawHitExplosion(impactPoint, (t - 0.56f) / 0.44f, cyan, impactDamage, 1.45f);
                    break;
                }
                case CardId.FrostLance:
                {
                    float travel = Mathf.Clamp01(t / 0.52f);
                    float x = Mathf.Lerp(295f, targetX, travel);
                    DrawRect(new Rect(290f, laneY - 5f, Mathf.Max(0f, x - 290f), 10f), new Color32(155, 237, 255, 105));
                    DrawRect(new Rect(x - 95f, laneY - 3f, 116f, 6f), frost);
                    for (int shard = 0; shard < 7; shard++)
                    {
                        float shardX = x - shard * 18f;
                        float offset = (shard % 2 == 0 ? -1f : 1f) * (9f + shard * 2f);
                        DrawRect(new Rect(shardX, laneY + offset, 16f, 4f), frost);
                    }
                    if (t > 0.5f)
                        DrawHitExplosion(impactPoint, (t - 0.5f) / 0.5f, frost, impactDamage, 1.2f);
                    break;
                }
                case CardId.CounterPursuit:
                case CardId.GhostProtocol:
                {
                    float lockPhase = Mathf.Clamp01(t / 0.3f);
                    float box = Mathf.Lerp(72f, 30f, lockPhase);
                    DrawPixelOutline(new Rect(targetX - box, impactPoint.y - box, box * 2f, box * 2f), violet, 4f);
                    if (t > 0.24f)
                    {
                        float rebound = Mathf.Clamp01((t - 0.24f) / 0.42f);
                        float x = Mathf.Lerp(targetX, 285f, rebound);
                        float returnX = Mathf.Lerp(285f, targetX, Mathf.Clamp01((rebound - 0.48f) * 1.92f));
                        DrawRect(new Rect(Mathf.Min(x, targetX), laneY - 4f, Mathf.Abs(targetX - x), 8f), new Color32(205, 92, 255, 135));
                        DrawRect(new Rect(Mathf.Min(285f, returnX), laneY - 2f, Mathf.Abs(returnX - 285f), 4f), Color.white);
                    }
                    if (t > 0.62f)
                        DrawHitExplosion(impactPoint, (t - 0.62f) / 0.38f, violet, impactDamage, 1.5f);
                    break;
                }
                case CardId.BroadsideVolley:
                {
                    for (int lane = 0; lane < 3; lane++)
                    {
                        float local = Mathf.Clamp01((t - lane * 0.08f) / 0.48f);
                        float x = Mathf.Lerp(290f, 1220f, local);
                        float y = 188f + lane * 130f;
                        DrawRect(new Rect(285f, y - 9f, Mathf.Max(0f, x - 285f), 18f), new Color32(255, 79, 202, 72));
                        DrawRect(new Rect(x - 32f, y - 5f, 108f, 10f), lane == 1 ? Color.white : yellow);
                    }
                    if (t > 0.45f)
                    {
                        float blast = (t - 0.45f) / 0.55f;
                        for (int lane = 0; lane < 3; lane++)
                            DrawHitExplosion(new Vector2(1130f + lane * 24f, 188f + lane * 130f), blast, violet,
                                lane == 1 ? impactDamage : 0, 1.35f);
                    }
                    break;
                }
                case CardId.MeltdownBurst:
                {
                    float wave = Mathf.Clamp01(t / 0.68f);
                    float radius = 40f + wave * 920f;
                    DrawPixelOutline(new Rect(245f - radius, laneY - radius * 0.25f, radius * 2f, radius * 0.5f), PostalRed, 9f);
                    DrawRect(new Rect(235f, 160f, Mathf.Min(1050f, radius), 340f), new Color(1f, 0.18f, 0.08f, (1f - wave) * 0.12f));
                    for (int lane = 0; lane < 3; lane++)
                    {
                        float y = 188f + lane * 130f;
                        DrawRect(new Rect(280f, y - 3f, Mathf.Min(radius, 900f), 6f), new Color32(255, 126, 45, 170));
                    }
                    if (t > 0.54f)
                        DrawHitExplosion(new Vector2(1110f, 318f), (t - 0.54f) / 0.46f, PostalRed, impactDamage, 1.7f);
                    break;
                }
                case CardId.Scattershot:
                {
                    float travel = Mathf.Clamp01(t / 0.58f);
                    for (int pellet = 0; pellet < 15; pellet++)
                    {
                        int lane = pellet % 3;
                        float x = Mathf.Lerp(295f, 1180f + (pellet % 5) * 11f, travel);
                        float spread = (pellet % 5 - 2) * 13f;
                        float y = 188f + lane * 130f + spread * travel;
                        float size = 4f + pellet % 3 * 2f;
                        DrawRect(new Rect(x - size, y - size, size * 2f, size * 2f), pellet % 2 == 0 ? yellow : cyan);
                    }
                    if (t > 0.55f)
                        DrawHitExplosion(new Vector2(1135f, 318f), (t - 0.55f) / 0.45f, yellow, impactDamage, 0.9f);
                    break;
                }
                case CardId.MissileSwarm:
                {
                    for (int missile = 0; missile < 6; missile++)
                    {
                        float local = Mathf.Clamp01((t - missile * 0.055f) / 0.72f);
                        if (local <= 0f)
                            continue;
                        int lane = missile % 3;
                        float x = Mathf.Lerp(290f, 1160f + lane * 25f, local);
                        float arc = Mathf.Sin(local * Mathf.PI) * (70f + missile * 7f) * (missile % 2 == 0 ? -1f : 1f);
                        float y = Mathf.Lerp(laneY, 188f + lane * 130f, local) + arc;
                        DrawRect(new Rect(x - 30f, y - 2f, 24f, 4f), new Color32(81, 231, 255, 105));
                        DrawRect(new Rect(x - 6f, y - 5f, 18f, 10f), missile % 2 == 0 ? PostalRed : yellow);
                    }
                    if (t > 0.68f)
                        DrawHitExplosion(new Vector2(1135f, 318f), (t - 0.68f) / 0.32f, PostalRed, impactDamage, 1.35f);
                    break;
                }
                case CardId.InterceptMine:
                {
                    float arm = Mathf.Clamp01(t / 0.4f);
                    for (int lane = 0; lane < 3; lane++)
                    {
                        if (lane == combatFxLane)
                            continue;
                        float y = 188f + lane * 130f;
                        float x = Mathf.Lerp(340f, 970f + lane * 70f, arm);
                        float pulse = 24f + Mathf.Sin(t * 30f + lane) * 7f;
                        DrawPixelOutline(new Rect(x - pulse, y - pulse, pulse * 2f, pulse * 2f), violet, 5f);
                        DrawRect(new Rect(x - 7f, y - 7f, 14f, 14f), PostalRed);
                        if (t > 0.58f)
                            DrawHitExplosion(new Vector2(x, y), (t - 0.58f) / 0.42f, violet,
                                lane == 1 ? impactDamage : 0, 1.2f);
                    }
                    break;
                }
                default:
                {
                    float travel = Mathf.Clamp01(t / 0.58f);
                    float x = Mathf.Lerp(315f, targetX, travel);
                    DrawRect(new Rect(300f, laneY - 4f, Mathf.Max(0f, x - 300f), 8f), yellow);
                    if (t > 0.5f)
                        DrawHitExplosion(impactPoint, (t - 0.5f) / 0.5f, yellow, impactDamage, combatFxPower);
                    break;
                }
            }
        }

        private void DrawEnemyDeathEffects()
        {
            for (int index = enemyDeathFx.Count - 1; index >= 0; index--)
            {
                EnemyDeathFx fx = enemyDeathFx[index];
                float elapsed = Time.time - fx.StartTime;
                if (elapsed > 1.5f)
                {
                    enemyDeathFx.RemoveAt(index);
                    continue;
                }

                float t = Mathf.Clamp01(elapsed / 1.45f);
                float fade = 1f - t;
                Vector2 center = fx.Position;

                // 每种敌机保留自己的“死亡签名”：速度、咬合、雷暴与磁场。
                if (fx.Kind == EnemyKind.RustKite)
                {
                    for (int slash = 0; slash < 5; slash++)
                    {
                        float sweep = Mathf.Clamp01(elapsed * 3.8f - slash * 0.08f);
                        float sx = center.x - 170f + sweep * 340f;
                        DrawRect(new Rect(sx, center.y - 54f + slash * 25f, 118f * fade, 5f),
                            slash % 2 == 0 ? new Color(0.2f, 1f, 1f, fade) : new Color(1f, 0.12f, 0.78f, fade));
                    }
                }
                else if (fx.Kind == EnemyKind.MailEater)
                {
                    float jaw = Mathf.Clamp01(elapsed / 0.34f);
                    DrawPixelOutline(new Rect(center.x - 92f - jaw * 32f, center.y - 55f, 78f, 110f),
                        new Color(1f, 0.24f, 0.35f, fade), 8f);
                    DrawPixelOutline(new Rect(center.x + 14f + jaw * 32f, center.y - 55f, 78f, 110f),
                        new Color(1f, 0.72f, 0.16f, fade), 8f);
                }
                else if (fx.Kind == EnemyKind.StormBalloon)
                {
                    for (int bolt = 0; bolt < 4; bolt++)
                    {
                        float bx = center.x - 70f + bolt * 46f + Mathf.Sin(elapsed * 45f + bolt) * 12f;
                        DrawRect(new Rect(bx, 142f, 7f, Mathf.Max(0f, center.y - 142f)),
                            new Color(0.45f, 0.88f, 1f, fade * 0.85f));
                    }
                }
                else if (fx.Kind == EnemyKind.CalamityDrone)
                {
                    for (int spoke = 0; spoke < 8; spoke++)
                    {
                        float angle = spoke * Mathf.PI * 0.25f + elapsed * 5f;
                        float distance = 35f + t * 150f;
                        float sx = center.x + Mathf.Cos(angle) * distance;
                        float sy = center.y + Mathf.Sin(angle) * distance;
                        DrawRect(new Rect(sx - 22f, sy - 4f, 44f, 8f),
                            spoke % 2 == 0 ? new Color(0.2f, 1f, 1f, fade) : new Color(1f, 0.1f, 0.72f, fade));
                    }
                    DrawPixelOutline(new Rect(center.x - 82f - t * 70f, center.y - 82f - t * 70f,
                        164f + t * 140f, 164f + t * 140f), new Color(0.85f, 0.22f, 1f, fade), 7f);
                }
                else if (fx.Kind == EnemyKind.StormManta || fx.Kind == EnemyKind.CloudWyrm)
                {
                    float field = Mathf.Clamp01(elapsed / 1.15f);
                    for (int ring = 0; ring < 5; ring++)
                    {
                        float radius = 55f + field * (145f + ring * 42f);
                        DrawPixelOutline(new Rect(center.x - radius, center.y - radius * 0.52f,
                            radius * 2f, radius * 1.04f), new Color(0.82f, 0.2f, 1f, fade * (0.72f - ring * 0.1f)), 5f);
                    }
                }

                // 第一阶段：敌机过曝冻结与色散撕裂。
                if (elapsed < 0.24f)
                {
                    float strobe = 0.55f + Mathf.Sin(Time.unscaledTime * 72f) * 0.45f;
                    DrawRect(new Rect(center.x - 85, center.y - 58, 170, 116), new Color(0.55f, 0.95f, 1f, 0.22f * strobe));
                    DrawRect(new Rect(center.x - 68, center.y - 22, 136, 44), new Color(1f, 1f, 1f, 0.92f * strobe));
                    DrawRect(new Rect(center.x - 82, center.y - 15, 164, 12), new Color(1f, 0.12f, 0.78f, 0.82f));
                    DrawRect(new Rect(center.x - 72, center.y + 5, 144, 10), new Color(0.12f, 0.95f, 1f, 0.88f));
                }

                // 第二阶段：核心爆炸、十字波与多层冲击环。
                if (elapsed > 0.1f && elapsed < 1.05f)
                {
                    float blast = Mathf.Clamp01((elapsed - 0.1f) / 0.9f);
                    DrawHitExplosion(center, blast, NeonViolet, 0, 2.35f);
                    float crossFade = 1f - blast;
                    DrawRect(new Rect(center.x - 260f * blast, center.y - 8, 520f * blast, 16),
                        new Color(0.22f, 1f, 1f, crossFade * 0.78f));
                    DrawRect(new Rect(center.x - 7, center.y - 180f * blast, 14, 360f * blast),
                        new Color(1f, 0.18f, 0.83f, crossFade * 0.7f));
                }

                // 第三阶段：带重力的像素残骸与上升烟尘。
                for (int piece = 0; piece < 30; piece++)
                {
                    float angle = (piece * 0.71f + fx.Seed * 0.13f) % (Mathf.PI * 2f);
                    float speed = 75f + (piece % 7) * 22f;
                    float distance = speed * t * 1.7f;
                    float px = center.x + Mathf.Cos(angle) * distance;
                    float py = center.y + Mathf.Sin(angle) * distance + t * t * 135f;
                    float size = Mathf.Max(2f, (9f - piece % 4) * fade);
                    Color debris = piece % 4 == 0
                        ? new Color(1f, 0.42f, 0.08f, fade)
                        : piece % 2 == 0
                            ? new Color(0.15f, 0.95f, 1f, fade)
                            : new Color(0.95f, 0.16f, 0.87f, fade);
                    DrawRect(new Rect(px, py, size * 1.8f, size), debris);
                }

                for (int smoke = 0; smoke < 9; smoke++)
                {
                    float smokeT = Mathf.Clamp01((elapsed - 0.38f - smoke * 0.035f) / 0.95f);
                    if (smokeT <= 0f)
                        continue;
                    float sx = center.x + Mathf.Sin(smoke * 2.4f + fx.Seed) * (24f + smokeT * 42f);
                    float sy = center.y - smokeT * (55f + smoke * 8f);
                    float size = 18f + smokeT * 24f;
                    DrawRect(new Rect(sx - size * 0.5f, sy - size * 0.5f, size, size),
                        new Color(0.18f, 0.12f, 0.34f, (1f - smokeT) * 0.62f));
                }

                if (elapsed < 1.05f)
                {
                    float labelY = Mathf.Max(168f, center.y - 142f - t * 22f);
                    Rect label = new Rect(center.x - 155, labelY, 310, 42);
                    DrawRect(label, new Color32(6, 10, 28, (byte)(225 * fade)));
                    DrawNeonFrame(label, NeonViolet, 3f);
                    DrawCyberLabel(label, $"TARGET BREAK  //  {fx.Name}", buttonLabelStyle, NeonViolet);
                }
            }
        }

        private void DrawEnemyAttackEffects()
        {
            for (int index = enemyAttackFx.Count - 1; index >= 0; index--)
            {
                EnemyAttackFx fx = enemyAttackFx[index];
                float elapsed = Time.time - fx.StartTime;
                if (elapsed > 1.05f)
                {
                    enemyAttackFx.RemoveAt(index);
                    continue;
                }

                float t = Mathf.Clamp01(elapsed / 0.92f);
                float fade = 1f - t;
                Vector2 player = new Vector2(242f, 190f + battle.PlayerLane * 130f);

                if (fx.Kind == EnemyKind.RustKite)
                {
                    float dash = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.62f));
                    float x = Mathf.Lerp(fx.Position.x, player.x + 95f, dash);
                    DrawRect(new Rect(x, player.y - 34f, 210f * fade, 8f), new Color(1f, 0.12f, 0.72f, fade * 0.85f));
                    DrawRect(new Rect(x - 36f, player.y + 19f, 265f * fade, 5f), new Color(0.16f, 0.95f, 1f, fade));
                    if (t > 0.5f)
                        DrawImpactBurst(player, (t - 0.5f) * 2f, NeonCyan);
                }
                else if (fx.Kind == EnemyKind.MailEater)
                {
                    float ram = Mathf.Sin(Mathf.Clamp01(t / 0.68f) * Mathf.PI);
                    Vector2 head = Vector2.Lerp(fx.Position, player + new Vector2(105f, 0f), ram);
                    float jaw = 24f + Mathf.Sin(t * 28f) * 18f;
                    DrawPixelOutline(new Rect(head.x - 56f, head.y - jaw - 30f, 112f, 38f), new Color(1f, 0.25f, 0.34f, fade), 7f);
                    DrawPixelOutline(new Rect(head.x - 56f, head.y + jaw - 8f, 112f, 38f), new Color(1f, 0.66f, 0.12f, fade), 7f);
                    if (t > 0.42f)
                        DrawHitExplosion(player, (t - 0.42f) / 0.58f, PostalRed, 0, 1.15f);
                }
                else if (fx.Kind == EnemyKind.StormBalloon)
                {
                    for (int lane = 0; lane < 3; lane++)
                    {
                        float laneY = 188f + lane * 130f;
                        float pulse = 0.45f + Mathf.Sin(elapsed * 48f + lane) * 0.35f;
                        DrawRect(new Rect(player.x - 44f + lane * 26f, 140f, 10f, laneY - 118f),
                            new Color(0.3f, 0.9f, 1f, fade * pulse));
                        DrawImpactBurst(new Vector2(player.x + lane * 25f - 25f, laneY), t, NeonViolet);
                    }
                }
                else if (fx.Kind == EnemyKind.ShieldLeech)
                {
                    float collapse = Mathf.Lerp(130f, 26f, Mathf.SmoothStep(0f, 1f, t));
                    for (int ring = 0; ring < 3; ring++)
                    {
                        float radius = collapse + ring * 24f;
                        DrawPixelOutline(new Rect(player.x - radius, player.y - radius,
                            radius * 2f, radius * 2f), new Color(0.18f, 1f, 0.82f, fade * 0.75f), 5f);
                    }
                    DrawRect(new Rect(player.x - 115f, player.y - 5f, 230f, 10f),
                        new Color(1f, 1f, 1f, fade * 0.72f));
                }
                else if (fx.Kind == EnemyKind.HandJammer)
                {
                    float scanY = Mathf.Lerp(590f, 850f, t);
                    DrawRect(new Rect(40f, scanY, 1280f, 8f), new Color(0.18f, 0.92f, 1f, fade * 0.8f));
                    for (int column = 0; column < 8; column++)
                    {
                        float x = 55f + column * 165f;
                        DrawRect(new Rect(x, 600f, 4f, 250f),
                            new Color(0.35f, 0.75f, 1f, fade * 0.24f));
                    }
                }
                else if (fx.Kind == EnemyKind.HeatSeeker)
                {
                    float lockT = Mathf.Clamp01(t / 0.42f);
                    float reticle = Mathf.Lerp(92f, 34f, lockT);
                    DrawPixelOutline(new Rect(player.x - reticle, player.y - reticle,
                        reticle * 2f, reticle * 2f), new Color(1f, 0.24f, 0.12f, fade), 5f);
                    float beam = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.35f) / 0.4f));
                    DrawRect(new Rect(player.x, player.y - 7f, (fx.Position.x - player.x) * beam, 14f),
                        new Color(1f, 0.38f, 0.08f, fade * 0.86f));
                    if (t > 0.55f)
                        DrawHitExplosion(player, (t - 0.55f) / 0.45f, Gold, fx.Damage, 1.1f);
                }
                else if (fx.Kind == EnemyKind.CalamityDrone)
                {
                    float targetY = 190f + fx.TargetLane * 130f;
                    float fire = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.14f) / 0.42f));
                    float beamWidth = Mathf.Lerp(3f, 30f, Mathf.Sin(fire * Mathf.PI));
                    DrawRect(new Rect(62f, targetY - beamWidth * 0.5f, fx.Position.x - 62f, beamWidth),
                        new Color(1f, 0.12f, 0.7f, fade * 0.78f));
                    DrawRect(new Rect(62f, targetY - 4f, fx.Position.x - 62f, 8f),
                        new Color(0.75f, 0.95f, 1f, fade));
                    float reticle = 48f + Mathf.Sin(elapsed * 35f) * 8f;
                    DrawPixelOutline(new Rect(player.x - reticle, targetY - reticle, reticle * 2f, reticle * 2f),
                        fx.Hit ? PostalRed : NeonCyan, 5f);
                    if (t > 0.45f)
                        DrawImpactBurst(new Vector2(player.x, targetY), (t - 0.45f) / 0.55f, fx.Hit ? PostalRed : NeonCyan);
                }
                else if (fx.Kind == EnemyKind.CloudWyrm || fx.Kind == EnemyKind.CurtainHerald)
                {
                    for (int lane = 0; lane < 3; lane++)
                    {
                        float laneY = 190f + lane * 130f;
                        bool safe = lane == fx.TargetLane;
                        if (safe)
                        {
                            float opening = 34f + Mathf.Sin(elapsed * 24f) * 9f;
                            DrawPixelOutline(new Rect(70f, laneY - opening, 520f, opening * 2f),
                                new Color(0.18f, 1f, 0.8f, fade * 0.88f), 5f);
                            continue;
                        }
                        for (int bolt = 0; bolt < 7; bolt++)
                        {
                            float x = 70f + bolt * 76f + Mathf.Sin(elapsed * 52f + bolt * 1.7f) * 18f;
                            DrawRect(new Rect(x, laneY - 51f, 7f, 102f),
                                new Color(0.72f, 0.92f, 1f, fade * (0.42f + (bolt % 2) * 0.28f)));
                        }
                        DrawRect(new Rect(62f, laneY - 48f, 535f, 96f),
                            new Color(0.95f, 0.08f, 0.42f, fade * 0.1f));
                    }
                    if (t > 0.48f)
                        DrawImpactBurst(player, (t - 0.48f) / 0.52f, fx.Hit ? PostalRed : NeonCyan);
                }
                else if (fx.Kind == EnemyKind.FluxSkimmer)
                {
                    for (int lane = 0; lane < 3; lane++)
                    {
                        float laneY = 190f + lane * 130f;
                        bool swept = Mathf.Abs(lane - fx.TargetLane) <= 1;
                        if (!swept)
                        {
                            DrawPixelOutline(new Rect(78f, laneY - 37f, 500f, 74f),
                                new Color(0.18f, 1f, 0.82f, fade * 0.75f), 4f);
                            continue;
                        }
                        float band = 14f + Mathf.Sin(elapsed * 38f + lane) * 6f;
                        DrawRect(new Rect(62f, laneY - band, 535f, band * 2f),
                            new Color(0.72f, 0.12f, 1f, fade * 0.22f));
                        for (int ring = 0; ring < 3; ring++)
                        {
                            float radius = 30f + ring * 26f + t * 46f;
                            DrawPixelOutline(new Rect(player.x - radius, laneY - radius,
                                radius * 2f, radius * 2f),
                                new Color(0.9f, 0.32f, 1f, fade * (0.72f - ring * 0.13f)), 5f);
                        }
                    }
                    if (t > 0.48f)
                        DrawImpactBurst(player, (t - 0.48f) / 0.52f, fx.Hit ? NeonViolet : NeonCyan);
                }
                else
                {
                    float radius = 70f + t * 760f;
                    for (int ring = 0; ring < 4; ring++)
                    {
                        float r = radius + ring * 48f;
                        DrawPixelOutline(new Rect(fx.Position.x - r, fx.Position.y - r * 0.34f, r * 2f, r * 0.68f),
                            new Color(0.75f, 0.18f, 1f, fade * (0.65f - ring * 0.1f)), 7f);
                    }
                    DrawRect(new Rect(0, player.y - 12f, ReferenceWidth, 24f), new Color(0.15f, 0.95f, 1f, fade * 0.22f));
                }
            }
        }

        private void TriggerFullScreenImpact(float power, float duration, bool kill)
        {
            fullScreenFxStart = Time.unscaledTime;
            fullScreenFxDuration = duration;
            fullScreenFxPower = power;
            fullScreenFxKill = kill;
        }

        private void DrawFullScreenImpact()
        {
            float elapsed = Time.unscaledTime - fullScreenFxStart;
            if (fullScreenFxDuration <= 0f || elapsed < 0f || elapsed > fullScreenFxDuration)
                return;
            if (gameSettings.FlashIntensity <= 0.001f)
                return;

            float t = Mathf.Clamp01(elapsed / fullScreenFxDuration);
            float fade = 1f - t;
            float hitPulse = Mathf.Sin(Mathf.Clamp01((t - 0.12f) / 0.34f) * Mathf.PI);
            float power = fullScreenFxPower;
            Color previousGuiColor = GUI.color;
            GUI.color = new Color(previousGuiColor.r, previousGuiColor.g, previousGuiColor.b,
                previousGuiColor.a * gameSettings.FlashIntensity);

            // 命中前压暗四周，让随后的白闪形成更大的明暗跨度。
            if (t < 0.3f)
                DrawRect(new Rect(0, 0, ReferenceWidth, ReferenceHeight),
                    new Color(0.005f, 0.008f, 0.035f, (0.16f + t * 0.5f) * Mathf.Min(1f, power)));

            // 全屏反白与左右双色分屏，重击和击杀才达到最高亮度。
            if (hitPulse > 0f)
            {
                DrawRect(new Rect(0, 0, ReferenceWidth, ReferenceHeight),
                    new Color(0.8f, 0.98f, 1f, hitPulse * 0.16f * power));
                DrawRect(new Rect(0, 0, ReferenceWidth * 0.5f, ReferenceHeight),
                    new Color(1f, 0.04f, 0.67f, hitPulse * 0.075f * power));
                DrawRect(new Rect(ReferenceWidth * 0.5f, 0, ReferenceWidth * 0.5f, ReferenceHeight),
                    new Color(0.02f, 0.95f, 1f, hitPulse * 0.09f * power));
            }

            // 从命中点贯穿全屏的放射速度线。
            Matrix4x4 oldMatrix = GUI.matrix;
            for (int ray = 0; ray < 24; ray++)
            {
                float angle = ray * 15f + (fullScreenFxKill ? 7.5f : 0f);
                float length = (260f + (ray % 5) * 95f) * (0.5f + hitPulse) * Mathf.Min(1.45f, power);
                float thickness = ray % 4 == 0 ? 7f : 3f;
                GUIUtility.RotateAroundPivot(angle, impactPoint);
                DrawRect(new Rect(impactPoint.x + 32f, impactPoint.y - thickness * 0.5f, length, thickness),
                    ray % 3 == 0
                        ? new Color(1f, 0.12f, 0.78f, fade * 0.72f)
                        : new Color(0.1f, 0.95f, 1f, fade * 0.68f));
                GUI.matrix = oldMatrix;
            }

            // 横向撕裂条会越过 HUD，确保效果真正覆盖整个屏幕。
            for (int strip = 0; strip < 12; strip++)
            {
                float y = (strip * 79f + fullScreenFxStart * 173f) % ReferenceHeight;
                float width = 180f + (strip % 5) * 155f;
                float direction = strip % 2 == 0 ? 1f : -1f;
                float x = impactPoint.x + direction * (90f + t * 520f) - width * 0.5f;
                Color stripColor = strip % 2 == 0
                    ? new Color(0.05f, 0.95f, 1f, fade * 0.42f * Mathf.Min(1f, power))
                    : new Color(1f, 0.08f, 0.72f, fade * 0.38f * Mathf.Min(1f, power));
                DrawRect(new Rect(x, y, width, strip % 4 == 0 ? 8f : 3f), stripColor);
            }

            float edge = (28f + hitPulse * 72f) * Mathf.Min(1.25f, power);
            DrawRect(new Rect(0, 0, edge, ReferenceHeight), new Color(1f, 0.06f, 0.58f, fade * 0.28f));
            DrawRect(new Rect(ReferenceWidth - edge, 0, edge, ReferenceHeight), new Color(0.05f, 0.92f, 1f, fade * 0.3f));
            DrawRect(new Rect(0, 0, ReferenceWidth, edge * 0.45f), new Color(0.2f, 0.8f, 1f, fade * 0.2f));
            DrawRect(new Rect(0, ReferenceHeight - edge * 0.45f, ReferenceWidth, edge * 0.45f), new Color(0.9f, 0.08f, 0.65f, fade * 0.2f));

            if (fullScreenFxKill)
            {
                float cross = Mathf.Clamp01(t / 0.58f);
                DrawRect(new Rect(0, impactPoint.y - 11f, ReferenceWidth, 22f), new Color(1f, 1f, 1f, fade * 0.72f));
                DrawRect(new Rect(impactPoint.x - 10f, 0, 20f, ReferenceHeight), new Color(0.5f, 0.96f, 1f, fade * 0.64f));
                DrawPixelOutline(new Rect(impactPoint.x - 120f - cross * 350f, impactPoint.y - 70f - cross * 205f,
                    240f + cross * 700f, 140f + cross * 410f), new Color(1f, 0.18f, 0.86f, fade * 0.8f), 9f);
            }
            GUI.color = previousGuiColor;
        }

        private void TriggerShake(float magnitude, float duration)
        {
            shakeMagnitude = Mathf.Max(shakeMagnitude, magnitude);
            shakeUntil = Mathf.Max(shakeUntil, Time.time + duration);
        }

        private int TotalEnemyHealth()
        {
            int total = 0;
            for (int i = 0; i < battle.Enemies.Count; i++)
                total += Mathf.Max(0, battle.Enemies[i].Health);
            return total;
        }

        private int TotalEnemyDurability()
        {
            int total = 0;
            for (int i = 0; i < battle.Enemies.Count; i++)
                total += Mathf.Max(0, battle.Enemies[i].Health) + Mathf.Max(0, battle.Enemies[i].Armor);
            return total;
        }

        private IEnumerator DelayedHitStop(float delay, float duration)
        {
            yield return new WaitForSecondsRealtime(delay);
            Time.timeScale = 0.06f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
        }

        private void DrawArcadeScrollingBackdrop(Rect bounds, bool intense)
        {
            DrawRect(bounds, Night);
            GUI.BeginGroup(bounds);
            float width = bounds.width;
            float height = bounds.height;
            float speedScale = intense ? 1.35f : 0.72f;

            DrawRect(new Rect(0, 0, width, height * 0.28f), new Color32(8, 20, 58, 255));
            DrawRect(new Rect(0, height * 0.28f, width, height * 0.28f), new Color32(19, 37, 78, 255));
            DrawRect(new Rect(0, height * 0.56f, width, height * 0.44f), new Color32(7, 16, 43, 255));
            DrawRect(new Rect(0, height * 0.37f, width, 5), new Color32(226, 74, 239, 95));
            DrawRect(new Rect(0, height * 0.38f, width, 3), new Color32(75, 236, 248, 150));

            // 远景星点：速度最慢，建立稳定参照。
            for (int i = 0; i < 38; i++)
            {
                float x = Mathf.Repeat(i * 193f - Time.time * (12f + i % 4 * 3f) * speedScale, width + 20f) - 10f;
                float y = 16f + (i * 47 % Mathf.Max(20, (int)(height * 0.42f)));
                float size = i % 9 == 0 ? 4f : 2f;
                DrawRect(new Rect(x, y, size * 2f, size), i % 5 == 0
                    ? new Color32(240, 89, 255, 150)
                    : new Color32(116, 232, 255, 145));
            }

            // 赛博霓虹雨：竖向下落与横向卷轴叠加，制造高密度光污染。
            for (int i = 0; i < 24; i++)
            {
                float x = Mathf.Repeat(i * 149f - Time.time * 32f * speedScale, width + 80f) - 40f;
                float y = Mathf.Repeat(i * 97f + Time.time * (150f + i % 5 * 24f), height + 120f) - 60f;
                float length = 24f + (i % 4) * 17f;
                Color rain = i % 3 == 0
                    ? new Color32(247, 57, 255, 72)
                    : new Color32(64, 238, 255, 68);
                DrawRect(new Rect(x, y, i % 6 == 0 ? 3f : 1f, length), rain);
                if (i % 6 == 0)
                    DrawRect(new Rect(x - 4, y + length, 9, 4), new Color(rain.r, rain.g, rain.b, 0.22f));
            }

            // 远景浮岛与像素城灯。
            for (int i = 0; i < 6; i++)
            {
                float x = Mathf.Repeat(i * 345f - Time.time * 22f * speedScale, width + 300f) - 150f;
                float y = height * 0.27f + (i % 3) * 18f;
                DrawRect(new Rect(x, y, 150, 18), new Color32(30, 35, 78, 245));
                DrawRect(new Rect(x + 28, y - 35, 22, 35), new Color32(35, 39, 88, 245));
                DrawRect(new Rect(x + 58, y - 58, 18, 58), new Color32(41, 42, 95, 245));
                DrawRect(new Rect(x + 86, y - 27, 31, 27), new Color32(34, 38, 84, 245));
                DrawRect(new Rect(x + 18, y + 18, 112, 16), new Color32(11, 21, 54, 235));
                DrawRect(new Rect(x + 48, y + 34, 52, 13), new Color32(8, 17, 43, 220));
                for (int lamp = 0; lamp < 5; lamp++)
                    DrawRect(new Rect(x + 18 + lamp * 24, y - 8 - (lamp % 2) * 10, 5, 4),
                        lamp % 2 == 0 ? new Color32(255, 153, 62, 190) : new Color32(239, 78, 244, 180));
            }

            // 中景云带：更快的横向位移产生街机卷轴感。
            for (int i = 0; i < 7; i++)
            {
                float x = Mathf.Repeat(i * 285f - Time.time * 58f * speedScale, width + 360f) - 180f;
                float y = height * 0.49f + (i % 3) * 54f;
                DrawArcadeCloud(new Vector2(x, y), 0.72f + (i % 3) * 0.18f,
                    i % 2 == 0 ? new Color32(34, 78, 130, 190) : new Color32(63, 48, 123, 175));
            }

            // 近景云块与高速光条：速度最快，强化飞行感。
            for (int i = 0; i < 5; i++)
            {
                float x = Mathf.Repeat(i * 430f - Time.time * 105f * speedScale, width + 520f) - 260f;
                float y = height * 0.82f + (i % 2) * 38f;
                DrawArcadeCloud(new Vector2(x, y), 1.35f, new Color32(20, 34, 82, 235));
            }

            for (int i = 0; i < 18; i++)
            {
                float x = Mathf.Repeat(i * 127f - Time.time * (145f + i % 4 * 28f) * speedScale, width + 190f) - 95f;
                float y = height * 0.43f + (i * 53 % Mathf.Max(20, (int)(height * 0.48f)));
                float length = 38f + (i % 5) * 26f;
                DrawRect(new Rect(x, y, length, i % 4 == 0 ? 4f : 2f), i % 3 == 0
                    ? new Color32(237, 78, 255, 70)
                    : new Color32(82, 233, 251, 75));
            }

            GUI.EndGroup();
        }

        private static void DrawArcadeCloud(Vector2 position, float scale, Color color)
        {
            DrawRect(new Rect(position.x, position.y, 190f * scale, 28f * scale), color);
            DrawRect(new Rect(position.x + 35f * scale, position.y - 25f * scale, 118f * scale, 53f * scale), color);
            DrawRect(new Rect(position.x + 74f * scale, position.y - 48f * scale, 55f * scale, 76f * scale), color);
            DrawRect(new Rect(position.x + 15f * scale, position.y + 28f * scale, 150f * scale, 15f * scale),
                new Color(color.r * 0.62f, color.g * 0.62f, color.b * 0.72f, color.a));
        }

        private void DrawBattleSpeedLines(Rect bounds)
        {
            for (int i = 0; i < 22; i++)
            {
                float speed = 95f + (i % 5) * 42f;
                float x = bounds.x + Mathf.Repeat(i * 137f - Time.time * speed, bounds.width + 180f);
                float y = bounds.y + 18f + (i * 61 % (int)(bounds.height - 36f));
                float width = 34f + (i % 4) * 27f;
                byte alpha = (byte)(28 + (i % 3) * 15);
                DrawRect(new Rect(x, y, width, i % 4 == 0 ? 3f : 1f), new Color32(92, 224, 241, alpha));
            }

            for (int x = 75; x < 1530; x += 70)
                DrawRect(new Rect(x, bounds.y, 1, bounds.height), new Color32(75, 121, 163, 19));
        }

        private void DrawAmbientPixels(Rect bounds)
        {
            for (int i = 0; i < 26; i++)
            {
                float speed = 16f + (i % 7) * 8f;
                float x = bounds.x + Mathf.Repeat(i * 211f - Time.time * speed, bounds.width);
                float y = bounds.y + Mathf.Repeat(i * 73f + Mathf.Sin(Time.time * 0.7f + i) * 18f, bounds.height);
                float size = i % 5 == 0 ? 5f : 2f;
                Color color = i % 4 == 0
                    ? new Color32(223, 93, 255, 115)
                    : new Color32(85, 242, 250, 105);
                DrawRect(new Rect(x, y, size, size), color);
            }

            float lightning = Mathf.Clamp01((Mathf.Sin(Time.time * 0.83f) - 0.985f) / 0.015f);
            if (lightning > 0f)
                DrawRect(bounds, new Color(0.4f, 0.75f, 1f, lightning * 0.12f));
        }

        private void DrawEngineTrail(Vector2 center)
        {
            for (int i = 0; i < 12; i++)
            {
                float age = Mathf.Repeat(Time.time * (1.8f + i * 0.03f) + i * 0.083f, 1f);
                float x = center.x - 74f - age * (55f + i * 2.5f);
                float y = center.y + Mathf.Sin(i * 2.1f + Time.time * 7f) * (3f + age * 10f);
                float size = Mathf.Lerp(9f, 2f, age);
                Color color = i % 3 == 0
                    ? new Color(0.93f, 0.32f, 1f, (1f - age) * 0.65f)
                    : new Color(0.18f, 0.92f, 1f, (1f - age) * 0.8f);
                DrawRect(new Rect(x, y, size * 2.4f, size), color);
            }

            float flame = 10f + Mathf.Sin(Time.time * 22f) * 3f;
            DrawRect(new Rect(center.x - 92f - flame, center.y - 7f, flame + 21f, 14f), new Color32(54, 226, 255, 155));
            DrawRect(new Rect(center.x - 84f - flame * 0.55f, center.y - 3f, flame + 11f, 6f), new Color32(255, 240, 158, 235));
        }

        private void DrawResourcePips(Rect rect, int value, int max, Color color, string label)
        {
            GUI.Label(new Rect(rect.x, rect.y - 7, 72, rect.height + 8), label, tinyStyle);
            float startX = rect.x + 72f;
            for (int i = 0; i < max; i++)
            {
                Rect pip = new Rect(startX + i * 22f, rect.y + 4f, 14f, 24f);
                DrawRect(new Rect(pip.x - 2, pip.y - 2, pip.width + 4, pip.height + 4), new Color32(4, 9, 24, 255));
                DrawRect(pip, i < value ? color : new Color32(58, 70, 92, 255));
                if (i < value)
                    DrawRect(new Rect(pip.x + 3, pip.y + 2, pip.width - 6, 4), new Color32(230, 255, 255, 190));
            }
        }

        private void DrawPhaseBanner()
        {
            if (Time.time >= bannerUntil || string.IsNullOrEmpty(bannerText))
                return;

            float remaining = bannerUntil - Time.time;
            float pulse = 0.72f + Mathf.Sin(Time.time * 13f) * 0.12f;
            float width = 660f;
            Rect bar = new Rect((ReferenceWidth - width) * 0.5f, 136, width, 44);
            DrawRect(new Rect(0, bar.y + 7, ReferenceWidth, 30), new Color32(4, 8, 25, 155));
            DrawRect(bar, new Color32(12, 24, 53, 245));
            DrawNeonFrame(bar, bannerText.StartsWith("DANGER") || bannerText.StartsWith("WARNING") ? PostalRed : NeonCyan, 3f);
            Color old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(remaining * 2f) * pulse);
            DrawCyberLabel(bar, bannerText, buttonLabelStyle,
                bannerText.StartsWith("DANGER") || bannerText.StartsWith("WARNING") ? PostalRed : NeonCyan);
            GUI.color = old;
        }

        private static void DrawImpactBurst(Vector2 center, float t, Color color)
        {
            float phase = Mathf.Clamp01(t);
            float radius = 18f + phase * 72f;
            byte alpha = (byte)(230 * (1f - phase));
            Color faded = new Color(color.r, color.g, color.b, alpha / 255f);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 0.25f;
                float x = center.x + Mathf.Cos(angle) * radius;
                float y = center.y + Mathf.Sin(angle) * radius;
                float w = i % 2 == 0 ? 34f : 18f;
                DrawRect(new Rect(x - w * 0.5f, y - 3f, w, 6f), faded);
            }
            DrawPixelOutline(new Rect(center.x - radius * 0.5f, center.y - radius * 0.5f, radius, radius), faded, 4f);
            DrawRect(new Rect(center.x - 8f, center.y - 8f, 16f, 16f), new Color(1f, 1f, 1f, 1f - phase));
        }

        private void DrawHitExplosion(Vector2 center, float t, Color color, int damage, float power)
        {
            float phase = Mathf.Clamp01(t);
            float fade = 1f - phase;
            float radius = (24f + phase * 105f) * power;
            Color glow = new Color(color.r, color.g, color.b, fade * 0.34f);
            DrawRect(new Rect(center.x - radius * 0.55f, center.y - radius * 0.55f, radius * 1.1f, radius * 1.1f), glow);

            for (int ring = 0; ring < 3; ring++)
            {
                float ringRadius = radius * (0.35f + ring * 0.24f);
                byte alpha = (byte)(210 * fade * (1f - ring * 0.18f));
                DrawPixelOutline(new Rect(center.x - ringRadius, center.y - ringRadius,
                    ringRadius * 2f, ringRadius * 2f), new Color32((byte)(color.r * 255), (byte)(color.g * 255), (byte)(color.b * 255), alpha),
                    Mathf.Max(2f, 7f - ring * 2f));
            }

            for (int i = 0; i < 18; i++)
            {
                float angle = i * Mathf.PI * 2f / 18f + i * 0.37f;
                float distance = radius * (0.45f + (i % 4) * 0.13f);
                float px = center.x + Mathf.Cos(angle) * distance;
                float py = center.y + Mathf.Sin(angle) * distance;
                float size = Mathf.Max(2f, (8f - (i % 3) * 2f) * fade * power);
                Color particle = i % 3 == 0
                    ? new Color(1f, 1f, 1f, fade)
                    : new Color(color.r, color.g, color.b, fade * 0.95f);
                DrawRect(new Rect(px - size, py - size * 0.5f, size * 2f, size), particle);
            }

            DrawRect(new Rect(center.x - 18f * power, center.y - 18f * power, 36f * power, 36f * power),
                new Color(1f, 1f, 1f, fade));
            if (damage > 0 && phase < 0.82f)
            {
                float numberY = Mathf.Max(175f, center.y - 96f - phase * 52f);
                float numberX = center.x - 155f;
                DrawRect(new Rect(numberX, numberY - 4, 132, 38), new Color32(7, 12, 31, (byte)(215 * fade)));
                DrawNeonFrame(new Rect(numberX, numberY - 4, 132, 38), color, 2f);
                DrawCyberLabel(new Rect(numberX + 2, numberY - 2, 128, 34), $"-{damage}  CRASH", buttonLabelStyle, color);
            }
        }

        private void DrawImpactScreenFlash()
        {
            if (Time.time >= impactFlashUntil || (combatFx != CombatFx.Shot && combatFx != CombatFx.Volley))
                return;
            if (gameSettings.FlashIntensity <= 0.001f)
                return;

            float normalized = Mathf.Clamp01((Time.time - combatFxStart) / Mathf.Max(0.01f, combatFxDuration));
            float hitWindow = Mathf.Clamp01((normalized - 0.42f) / 0.24f);
            float pulse = Mathf.Sin(hitWindow * Mathf.PI) * Mathf.Clamp01((0.82f - normalized) / 0.2f);
            if (pulse <= 0f)
                return;

            Color previousGuiColor = GUI.color;
            GUI.color = new Color(previousGuiColor.r, previousGuiColor.g, previousGuiColor.b,
                previousGuiColor.a * gameSettings.FlashIntensity);
            DrawRect(new Rect(0, 0, ReferenceWidth, ReferenceHeight), new Color(0.68f, 0.94f, 1f, pulse * 0.16f * combatFxPower));
            DrawRect(new Rect(0, impactPoint.y - 42, ReferenceWidth, 8), new Color(0.25f, 1f, 1f, pulse * 0.34f));
            DrawRect(new Rect(0, impactPoint.y + 34, ReferenceWidth, 5), new Color(1f, 0.18f, 0.78f, pulse * 0.3f));
            DrawRect(new Rect(0, 0, 22, ReferenceHeight), new Color(1f, 0.2f, 0.68f, pulse * 0.38f));
            DrawRect(new Rect(ReferenceWidth - 22, 0, 22, ReferenceHeight), new Color(0.2f, 0.95f, 1f, pulse * 0.38f));
            GUI.color = previousGuiColor;
        }

        private void DrawFloatingIsland(Vector2 center, float scale)
        {
            Color grass = new Color32(105, 178, 148, 74);
            Color rock = new Color32(79, 112, 125, 58);
            DrawRect(new Rect(center.x - 90 * scale, center.y - 16 * scale, 180 * scale, 26 * scale), grass);
            DrawRect(new Rect(center.x - 65 * scale, center.y + 10 * scale, 130 * scale, 28 * scale), rock);
            DrawRect(new Rect(center.x - 38 * scale, center.y + 38 * scale, 76 * scale, 25 * scale), rock);
            DrawRect(new Rect(center.x - 14 * scale, center.y + 63 * scale, 28 * scale, 18 * scale), rock);
        }

        private static void DrawPixelOutline(Rect rect, Color color, float thickness)
        {
            DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private static void DrawNeonFrame(Rect rect, Color color, float thickness)
        {
            float breathe = 0.27f + Mathf.Sin(Time.time * 4.2f) * 0.065f;
            Color glow = new Color(color.r, color.g, color.b, breathe);
            DrawPixelOutline(new Rect(rect.x - 15, rect.y - 15, rect.width + 30, rect.height + 30), new Color(glow.r, glow.g, glow.b, glow.a * 0.22f), thickness + 10f);
            DrawPixelOutline(new Rect(rect.x - 9, rect.y - 9, rect.width + 18, rect.height + 18), new Color(glow.r, glow.g, glow.b, glow.a * 0.52f), thickness + 6f);
            DrawPixelOutline(new Rect(rect.x - 5, rect.y - 5, rect.width + 10, rect.height + 10), glow, thickness + 3f);
            DrawPixelOutline(rect, color, thickness);
            DrawRect(new Rect(rect.x, rect.y, 24f, thickness + 3f), Color.white);
            DrawRect(new Rect(rect.xMax - 24f, rect.yMax - thickness - 3f, 24f, thickness + 3f), Color.white);
        }

        private void DrawScreenTexture()
        {
            DrawRect(new Rect(0, 0, ReferenceWidth * 0.22f, ReferenceHeight), new Color32(255, 28, 210, 13));
            DrawRect(new Rect(ReferenceWidth * 0.78f, 0, ReferenceWidth * 0.22f, ReferenceHeight), new Color32(32, 235, 255, 15));

            for (int y = 0; y < ReferenceHeight; y += 8)
                DrawRect(new Rect(0, y, ReferenceWidth, 1), new Color32(2, 8, 24, screen == ScreenMode.Battle ? (byte)24 : (byte)15));

            int glitchFrame = Mathf.FloorToInt(Time.time * 14f) % 47;
            if (glitchFrame <= 2)
            {
                float glitchY = 145f + glitchFrame * 173f;
                DrawRect(new Rect(0, glitchY, ReferenceWidth, 7), new Color32(71, 238, 246, 68));
                DrawRect(new Rect(180, glitchY + 9, 920, 3), new Color32(226, 72, 255, 78));
                DrawRect(new Rect(1010, glitchY - 4, 420, 3), new Color32(255, 255, 255, 36));
            }

            DrawRect(new Rect(0, 0, ReferenceWidth, 22), new Color32(2, 5, 18, 125));
            DrawRect(new Rect(0, ReferenceHeight - 22, ReferenceWidth, 22), new Color32(2, 5, 18, 125));
            DrawRect(new Rect(0, 0, 22, ReferenceHeight), new Color32(2, 5, 18, 105));
            DrawRect(new Rect(ReferenceWidth - 22, 0, 22, ReferenceHeight), new Color32(2, 5, 18, 105));
        }

        private void DrawCloud(Vector2 position, float scale)
        {
            Color cloud = new Color32(239, 248, 226, 190);
            DrawRect(new Rect(position.x, position.y, 170 * scale, 28 * scale), cloud);
            DrawRect(new Rect(position.x + 28 * scale, position.y - 22 * scale, 108 * scale, 50 * scale), cloud);
            DrawRect(new Rect(position.x + 60 * scale, position.y - 42 * scale, 48 * scale, 70 * scale), cloud);
        }

        private void DrawPixelPlane(Vector2 center, float scale, bool enemy)
        {
            Color body = enemy ? PostalRed : new Color32(250, 229, 158, 255);
            Color trim = enemy ? new Color32(88, 42, 57, 255) : PostalRed;
            if (!enemy)
            {
                DrawRect(PixelRect(center, -50, -38, 104, 76, scale), new Color32(54, 229, 245, 20));
                DrawPixelOutline(PixelRect(center, -42, -32, 88, 64, scale), new Color32(82, 244, 250, 85), 3f);
            }
            DrawRect(PixelRect(center, -46, -10, 92, 20, scale), Shadow);
            DrawRect(PixelRect(center, -32, -18, 64, 36, scale), body);
            DrawRect(PixelRect(center, -10, -34, 28, 68, scale), body);
            DrawRect(PixelRect(center, -50, -5, 24, 10, scale), trim);
            DrawRect(PixelRect(center, 20, -12, 24, 24, scale), trim);
            DrawRect(PixelRect(center, -4, -22, 14, 12, scale), new Color32(63, 137, 166, 255));
            DrawRect(PixelRect(center, -2, 27, 8, 12, scale), Gold);
            if (!enemy)
            {
                DrawRect(PixelRect(center, -18, -6, 36, 12, scale), new Color32(251, 244, 211, 255));
                DrawRect(PixelRect(center, -4, -4, 8, 8, scale), PostalRed);
                bool verticalPropeller = Mathf.FloorToInt(Time.time * 14f) % 2 == 0;
                Rect propeller = verticalPropeller
                    ? PixelRect(center, -58, -20, 5, 40, scale)
                    : PixelRect(center, -72, -3, 32, 6, scale);
                DrawRect(propeller, new Color32(246, 239, 191, 220));
                DrawRect(PixelRect(center, -57, -5, 8, 10, scale), Shadow);
            }
        }

        private void DrawEnemy(EnemyState enemy, Vector2 center)
        {
            if (enemy.Kind == EnemyKind.CalamityDrone)
            {
                Color32 shell = enemy.ChargeInterrupted || enemy.ChargeTargetLane < 0
                    ? new Color32(66, 205, 220, 255)
                    : new Color32(176, 65, 196, 255);
                DrawRect(PixelRect(center, -38, -30, 76, 60, 1.12f), Shadow);
                DrawRect(PixelRect(center, -26, -24, 52, 48, 1.12f), shell);
                DrawRect(PixelRect(center, -54, -9, 108, 18, 1.12f), new Color32(86, 48, 130, 255));
                DrawRect(PixelRect(center, -9, -49, 18, 98, 1.12f), new Color32(105, 57, 151, 255));
                DrawRect(PixelRect(center, -16, -14, 32, 28, 1.12f), new Color32(4, 15, 37, 255));
                DrawRect(PixelRect(center, -8, -7, 16, 14, 1.12f),
                    enemy.ChargeInterrupted || enemy.ChargeTargetLane < 0 ? NeonCyan : PostalRed);
                DrawRect(PixelRect(center, -52, -5, 12, 10, 1.12f), Gold);
                DrawRect(PixelRect(center, 40, -5, 12, 10, 1.12f), Gold);
                return;
            }

            if (enemy.Kind == EnemyKind.StormManta)
            {
                DrawRect(PixelRect(center, -58, -10, 116, 20, 1.2f), new Color32(105, 67, 148, 255));
                DrawRect(PixelRect(center, -34, -28, 68, 56, 1.2f), new Color32(132, 88, 174, 255));
                DrawRect(PixelRect(center, -76, 7, 152, 15, 1.2f), new Color32(82, 55, 124, 255));
                DrawRect(PixelRect(center, -8, -15, 16, 12, 1.2f), Gold);
                DrawRect(PixelRect(center, -5, 27, 10, 24, 1.2f), PostalRed);
                return;
            }

            if (enemy.Kind == EnemyKind.CloudWyrm)
            {
                Color32 shell = enemy.Phase == 2
                    ? new Color32(91, 218, 219, 255)
                    : new Color32(65, 159, 188, 255);
                DrawRect(PixelRect(center, -70, -9, 140, 18, 1.18f), Shadow);
                DrawRect(PixelRect(center, -52, -18, 104, 36, 1.18f), shell);
                DrawRect(PixelRect(center, -28, -34, 58, 68, 1.18f), new Color32(43, 112, 154, 255));
                DrawRect(PixelRect(center, -78, -4, 34, 12, 1.18f), new Color32(139, 239, 229, 255));
                DrawRect(PixelRect(center, 45, -5, 38, 12, 1.18f), new Color32(139, 239, 229, 255));
                DrawRect(PixelRect(center, -7, -21, 15, 15, 1.18f), Color.white);
                DrawRect(PixelRect(center, -3, -17, 7, 7, 1.18f), Gold);
                DrawRect(PixelRect(center, -5, 33, 10, 29, 1.18f), NeonViolet);
                return;
            }

            if (enemy.Kind == EnemyKind.StormBalloon)
            {
                DrawRect(PixelRect(center, -34, -28, 68, 52, 1.15f), new Color32(109, 73, 151, 255));
                DrawRect(PixelRect(center, -44, -12, 88, 24, 1.15f), new Color32(141, 99, 177, 255));
                DrawRect(PixelRect(center, -16, 24, 32, 20, 1.15f), Shadow);
                DrawRect(PixelRect(center, -4, 44, 8, 16, 1.15f), Gold);
                return;
            }

            if (enemy.Kind == EnemyKind.ShieldLeech)
            {
                DrawPixelOutline(PixelRect(center, -44, -34, 88, 68, 1.1f), NeonCyan, 5f);
                DrawRect(PixelRect(center, -30, -22, 60, 44, 1.1f), new Color32(31, 111, 118, 255));
                DrawRect(PixelRect(center, -54, -7, 108, 14, 1.1f), new Color32(73, 211, 184, 255));
                DrawRect(PixelRect(center, -9, -40, 18, 80, 1.1f), new Color32(42, 153, 147, 255));
                DrawRect(PixelRect(center, -12, -10, 24, 20, 1.1f), Shadow);
                DrawRect(PixelRect(center, -5, -4, 10, 8, 1.1f), Color.white);
                return;
            }

            if (enemy.Kind == EnemyKind.HandJammer)
            {
                DrawRect(PixelRect(center, -46, -30, 92, 60, 1.08f), Shadow);
                DrawPixelOutline(PixelRect(center, -36, -26, 72, 52, 1.08f), NeonCyan, 4f);
                for (int line = -1; line <= 1; line++)
                {
                    DrawRect(PixelRect(center, -31, line * 13 - 3, 62, 6, 1.08f),
                        line == 0 ? Color.white : new Color32(70, 159, 201, 255));
                    DrawRect(PixelRect(center, line * 16 - 3, -22, 6, 44, 1.08f),
                        new Color32(85, 207, 233, 255));
                }
                return;
            }

            if (enemy.Kind == EnemyKind.HeatSeeker)
            {
                DrawRect(PixelRect(center, -54, -8, 108, 16, 1.1f), Shadow);
                DrawRect(PixelRect(center, -40, -13, 80, 26, 1.1f), new Color32(211, 77, 55, 255));
                DrawRect(PixelRect(center, -4, -36, 20, 72, 1.1f), new Color32(246, 111, 52, 255));
                DrawRect(PixelRect(center, 14, -24, 32, 48, 1.1f), PostalRed);
                DrawRect(PixelRect(center, -48, -4, 20, 8, 1.1f), Gold);
                DrawRect(PixelRect(center, 0, -19, 11, 11, 1.1f), Color.white);
                return;
            }

            if (enemy.Kind == EnemyKind.SignalHijacker)
            {
                DrawRect(PixelRect(center, -42, -28, 84, 56, 1.08f), Shadow);
                DrawPixelOutline(PixelRect(center, -34, -24, 68, 48, 1.08f), NeonViolet, 4f);
                DrawRect(PixelRect(center, -27, -17, 54, 34, 1.08f), new Color32(83, 47, 132, 255));
                DrawRect(PixelRect(center, -50, -5, 100, 10, 1.08f), new Color32(194, 82, 232, 255));
                DrawRect(PixelRect(center, -5, -42, 10, 84, 1.08f), new Color32(132, 74, 183, 255));
                DrawRect(PixelRect(center, -9, -8, 18, 16, 1.08f), Gold);
                return;
            }

            if (enemy.Kind == EnemyKind.CurtainHerald)
            {
                DrawRect(PixelRect(center, -58, -8, 116, 16, 1.1f), Shadow);
                DrawRect(PixelRect(center, -42, -18, 84, 36, 1.1f), new Color32(42, 149, 183, 255));
                DrawRect(PixelRect(center, -12, -40, 24, 80, 1.1f), new Color32(77, 218, 217, 255));
                DrawRect(PixelRect(center, -68, -4, 28, 8, 1.1f), NeonCyan);
                DrawRect(PixelRect(center, 40, -4, 28, 8, 1.1f), NeonCyan);
                DrawPixelOutline(PixelRect(center, -19, -21, 38, 42, 1.1f), Color.white, 3f);
                return;
            }

            if (enemy.Kind == EnemyKind.FluxSkimmer)
            {
                DrawRect(PixelRect(center, -62, -10, 124, 20, 1.1f), Shadow);
                DrawRect(PixelRect(center, -48, -20, 96, 40, 1.1f), new Color32(137, 72, 175, 255));
                DrawRect(PixelRect(center, -74, 5, 148, 12, 1.1f), new Color32(204, 78, 220, 255));
                DrawRect(PixelRect(center, -8, -35, 16, 70, 1.1f), PostalRed);
                DrawRect(PixelRect(center, -5, -10, 10, 20, 1.1f), Gold);
                return;
            }

            if (enemy.Kind == EnemyKind.RustKite)
            {
                DrawRect(PixelRect(center, -52, -7, 104, 14, 1.1f), Shadow);
                DrawRect(PixelRect(center, -38, -12, 76, 24, 1.1f), PostalRed);
                DrawRect(PixelRect(center, -8, -34, 26, 68, 1.1f), new Color32(170, 55, 62, 255));
                DrawRect(PixelRect(center, 12, -22, 28, 44, 1.1f), PostalRed);
                DrawRect(PixelRect(center, -48, -4, 18, 8, 1.1f), new Color32(250, 182, 67, 255));
                DrawRect(PixelRect(center, -3, -18, 13, 11, 1.1f), new Color32(74, 169, 184, 255));
                return;
            }

            // 噬邮兽使用厚重机身、咬合齿与外露货箱，和高速锈翼鸢形成轮廓差异。
            DrawRect(PixelRect(center, -46, -26, 92, 52, 1.08f), Shadow);
            DrawRect(PixelRect(center, -36, -30, 68, 60, 1.08f), new Color32(181, 60, 65, 255));
            DrawRect(PixelRect(center, -54, -13, 30, 26, 1.08f), new Color32(122, 48, 61, 255));
            DrawRect(PixelRect(center, 28, -19, 24, 38, 1.08f), PostalRed);
            DrawRect(PixelRect(center, 30, -12, 15, 7, 1.08f), Paper);
            DrawRect(PixelRect(center, 30, 5, 15, 7, 1.08f), Paper);
            DrawRect(PixelRect(center, -4, -39, 27, 19, 1.08f), Gold);
            DrawRect(PixelRect(center, 0, -35, 19, 11, 1.08f), new Color32(242, 214, 131, 255));
            DrawRect(PixelRect(center, -7, -20, 15, 12, 1.08f), new Color32(68, 145, 164, 255));
        }

        private static void DrawCalamityLaneTelegraph(EnemyState enemy)
        {
            if (enemy.ChargeInterrupted || enemy.ChargeTargetLane < 0)
                return;

            float laneY = 150f + enemy.ChargeTargetLane * 130f;
            float pulse = 0.55f + Mathf.Sin(Time.time * 9f) * 0.25f;
            DrawRect(new Rect(62, laneY, 1466, 105), new Color(1f, 0.08f, 0.62f, 0.035f + pulse * 0.025f));
            DrawPixelOutline(new Rect(66, laneY + 4, 1458, 97), new Color(1f, 0.15f, 0.72f, 0.22f + pulse * 0.18f), 3f);
            for (int x = 430; x < 1480; x += 150)
                DrawRect(new Rect(x, laneY + 50, 72, 3), new Color(0.95f, 0.2f, 1f, 0.2f + pulse * 0.22f));
        }

        private void DrawCloudWyrmLaneTelegraph(EnemyState enemy)
        {
            if (enemy.ChargeInterrupted || enemy.ChargeTargetLane < 0)
                return;

            float pulse = 0.55f + Mathf.Sin(Time.time * 9f) * 0.25f;
            for (int lane = 0; lane < 3; lane++)
            {
                float laneY = 150f + lane * 130f;
                bool safe = lane == enemy.ChargeTargetLane;
                Color fill = safe
                    ? new Color(0.12f, 1f, 0.78f, 0.07f + pulse * 0.035f)
                    : new Color(0.95f, 0.08f, 0.38f, 0.045f + pulse * 0.03f);
                Color outline = safe
                    ? new Color(0.2f, 1f, 0.84f, 0.45f + pulse * 0.25f)
                    : new Color(1f, 0.12f, 0.48f, 0.24f + pulse * 0.15f);
                DrawRect(new Rect(62, laneY, 1466, 105), fill);
                DrawPixelOutline(new Rect(66, laneY + 4, 1458, 97), outline, safe ? 5f : 2f);
                DrawFittedLabel(new Rect(385, laneY + 38, 390, 28),
                    safe ? L("battle.safe_lane", "[O] 唯一安全航道") :
                        L("battle.danger_lane", "[X] 雷幕封锁"),
                    tinyStyle, 9);
            }
        }

        private void DrawFluxSkimmerLaneTelegraph(EnemyState enemy)
        {
            if (enemy.ChargeInterrupted || enemy.ChargeTargetLane < 0)
                return;

            float pulse = 0.55f + Mathf.Sin(Time.time * 9f) * 0.25f;
            for (int lane = 0; lane < 3; lane++)
            {
                float laneY = 150f + lane * 130f;
                bool danger = Mathf.Abs(lane - enemy.ChargeTargetLane) <= 1;
                Color outline = danger
                    ? new Color(1f, 0.12f, 0.48f, 0.3f + pulse * 0.16f)
                    : new Color(0.18f, 1f, 0.82f, 0.45f + pulse * 0.22f);
                DrawRect(new Rect(62, laneY, 1466, 105),
                    new Color(outline.r, outline.g, outline.b, danger ? 0.055f : 0.07f));
                DrawPixelOutline(new Rect(66, laneY + 4, 1458, 97), outline, danger ? 3f : 5f);
                DrawFittedLabel(new Rect(385, laneY + 38, 390, 28),
                    danger ? L("battle.magnet_danger", "[X] 磁针扫掠") :
                        L("battle.clear_lane", "[O] 可规避航道"),
                    tinyStyle, 9);
            }
        }

        private static void DrawCalamityChargeCore(EnemyState enemy, Vector2 center)
        {
            int threshold = enemy.Kind switch
            {
                EnemyKind.StormManta => enemy.Phase == 1
                    ? BattleState.BossPhaseOneBreakDamage
                    : BattleState.BossPhaseTwoBreakDamage,
                EnemyKind.CloudWyrm => enemy.Phase == 1
                    ? BattleState.CloudWyrmPhaseOneBreakDamage
                    : BattleState.CloudWyrmPhaseTwoBreakDamage,
                EnemyKind.CurtainHerald => BattleState.PreludeBreakDamage,
                EnemyKind.FluxSkimmer => BattleState.PreludeBreakDamage,
                _ => BattleState.CalamityBreakDamage
            };
            float progress = Mathf.Clamp01(enemy.ChargeDamageTaken / (float)threshold);
            float pulse = 0.75f + Mathf.Sin(Time.time * 13f) * 0.18f;
            bool inactive = enemy.ChargeInterrupted || enemy.ChargeTargetLane < 0;
            Color color = inactive ? NeonCyan : NeonViolet;
            float radius = 61f + pulse * 9f;
            DrawPixelOutline(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f),
                new Color(color.r, color.g, color.b, inactive ? 0.7f : 0.34f), 3f);
            DrawRect(new Rect(center.x - 54f, center.y + 78f, 108f, 7f), new Color32(4, 12, 30, 230));
            DrawRect(new Rect(center.x - 52f, center.y + 80f, 104f * progress, 3f),
                inactive ? NeonCyan : PostalRed);
        }

        private static string IntentSymbol(string intent)
        {
            if (intent.Contains("追踪") || intent.Contains("热寻") || intent.Contains("TRACK"))
                return "X";
            if (intent.Contains("上升") || intent.Contains("下降") || intent.Contains("变轨") ||
                intent.Contains("SHIFT") || intent.Contains("ASCEND") || intent.Contains("DESCEND"))
                return "<>";
            if (intent.Contains("失衡") || intent.Contains("击落") || intent.Contains("STAGGER"))
                return "-";
            if (intent.Contains("安全") || intent.Contains("SAFE"))
                return "O";
            return "!";
        }

        private static Color IntentColor(string intent)
        {
            if (intent.Contains("上升") || intent.Contains("下降") || intent.Contains("SHIFT") ||
                intent.Contains("ASCEND") || intent.Contains("DESCEND"))
                return new Color32(49, 151, 174, 255);
            if (intent.Contains("风暴") || intent.Contains("磁暴") || intent.Contains("吞界") ||
                intent.Contains("雷幕") || intent.Contains("天穹") || intent.Contains("STORM") ||
                intent.Contains("MAGNETIC") || intent.Contains("CURTAIN") || intent.Contains("SKY"))
                return new Color32(123, 77, 166, 255);
            if (intent.Contains("追踪") || intent.Contains("TRACKING"))
                return new Color32(255, 89, 176, 255);
            if (intent.Contains("封锁") || intent.Contains("BLOCK"))
                return new Color32(245, 142, 62, 255);
            if (intent.Contains("盾蚀") || intent.Contains("SHIELD CORROSION"))
                return new Color32(63, 205, 177, 255);
            if (intent.Contains("手牌") || intent.Contains("监听") || intent.Contains("HAND") ||
                intent.Contains("MONITOR"))
                return new Color32(80, 204, 255, 255);
            if (intent.Contains("热寻") || intent.Contains("HEAT-SEEK"))
                return new Color32(255, 104, 88, 255);
            if (intent.Contains("劫持") || intent.Contains("污染") || intent.Contains("协议") ||
                intent.Contains("HIJACK") || intent.Contains("POLLUTE") || intent.Contains("PROTOCOL"))
                return new Color32(199, 83, 255, 255);
            if (intent.Contains("灾变") || intent.Contains("CALAMITY"))
                return new Color32(202, 67, 210, 255);
            if (intent.Contains("失衡") || intent.Contains("STAGGER") || intent.Contains("SHORTED") ||
                intent.Contains("OVERLOAD"))
                return new Color32(49, 190, 205, 255);
            if (intent.Contains("击落") || intent.Contains("DESTROYED"))
                return new Color32(92, 111, 113, 255);
            return PostalRed;
        }

        private void DrawMeter(Rect rect, int value, int max, Color fill, string label)
        {
            DrawRect(new Rect(rect.x - 3, rect.y - 3, rect.width + 6, rect.height + 6), Shadow);
            DrawRect(rect, new Color32(100, 113, 115, 255));
            DrawRect(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(value / (float)max), rect.height), fill);
            DrawFittedLabel(new Rect(rect.x, rect.y - 1, rect.width, rect.height + 3), label, tinyStyle, 8);
        }

        private void DrawPixelButton(Rect rect, string label, Color color, Action action, bool enabled = true, string shortcut = null)
        {
            Rect hitRect = rect;
            Color shown = enabled ? color : new Color32(100, 105, 112, 255);
            bool hovered = enabled && rect.Contains(Event.current.mousePosition);
            bool pressed = hovered && Event.current.type == EventType.MouseDown && Event.current.button == 0;
            if (hovered)
            {
                shown = Color.Lerp(shown, NeonCyan, 0.1f + Mathf.Sin(Time.time * 9f) * 0.025f);
                RegisterHover($"button-{label}", $"点击{label}");
            }
            if (pressed)
                rect.y += 4f;
            DrawRect(new Rect(rect.x + 7, rect.y + (pressed ? 3 : 7), rect.width, rect.height), new Color32(3, 8, 22, 255));
            DrawRect(rect, shown);
            DrawPixelOutline(rect, new Color32(7, 16, 38, 255), 3f);
            DrawRect(new Rect(rect.x + 10, rect.y + 8, rect.width - 20, 3), new Color32(255, 255, 255, hovered ? (byte)210 : (byte)90));
            if (hovered)
                DrawNeonFrame(new Rect(rect.x - 3, rect.y - 3, rect.width + 6, rect.height + 6), NeonCyan, 2f);
            Rect labelRect = rect;
            Rect labelPlate = new Rect(labelRect.x + 4f, rect.y + 9f, Mathf.Max(0f, labelRect.width - 8f), rect.height - 18f);
            DrawRect(labelPlate, new Color32(4, 11, 29, hovered ? (byte)205 : (byte)145));
            DrawCyberLabel(labelRect, $"〈 {label} 〉", buttonLabelStyle, hovered ? new Color32(255, 211, 82, 255) : NeonCyan);

            bool oldEnabled = GUI.enabled;
            GUI.enabled = enabled;
            if (GUI.Button(hitRect, GUIContent.none, GUIStyle.none))
            {
                PlaySound(clickSound);
                action?.Invoke();
            }
            GUI.enabled = oldEnabled;
        }

#if UNITY_EDITOR
        private void PreviewNextAttackEffect()
        {
            CardId[] attacks =
            {
                CardId.BurstFire, CardId.OverloadAim, CardId.RailPiercer, CardId.PursuitShot,
                CardId.AegisRam, CardId.FrostLance, CardId.CounterPursuit, CardId.BroadsideVolley,
                CardId.MeltdownBurst, CardId.Scattershot, CardId.MissileSwarm, CardId.InterceptMine
            };
            CardId card = attacks[editorAttackPreviewIndex % attacks.Length];
            editorAttackPreviewIndex++;
            bool volley = card == CardId.BroadsideVolley || card == CardId.MeltdownBurst ||
                card == CardId.Scattershot || card == CardId.MissileSwarm || card == CardId.InterceptMine;
            combatFx = volley ? CombatFx.Volley : CombatFx.Shot;
            combatFxCard = card;
            combatFxStart = Time.time;
            combatFxDuration = AttackFxDuration(card);
            combatFxLane = battle.PlayerLane;
            combatFxPower = AttackFxPower(card);
            impactPoint = new Vector2(1080f, 190f + battle.PlayerLane * 130f);
            impactDamage = 12;
            impactFlashUntil = Time.time + combatFxDuration;
            combatFxText = $"FX PREVIEW // {CardLibrary.Get(card).Name}";
            PlayAttackSound(card, true);
        }
#endif

        private static float AttackFxPower(CardId card)
        {
            return card switch
            {
                CardId.BurstFire => 0.9f,
                CardId.OverloadAim => 1.9f,
                CardId.RailPiercer => 1.45f,
                CardId.PursuitShot => 1.1f,
                CardId.AegisRam => 1.55f,
                CardId.FrostLance => 1.3f,
                CardId.CounterPursuit => 1.65f,
                CardId.BroadsideVolley => 1.65f,
                CardId.MeltdownBurst => 1.9f,
                CardId.Scattershot => 1f,
                CardId.MissileSwarm => 1.45f,
                CardId.InterceptMine => 1.35f,
                _ => 1f
            };
        }

        private static float AttackFxDuration(CardId card)
        {
            return card switch
            {
                CardId.BurstFire => 0.68f,
                CardId.OverloadAim => 0.92f,
                CardId.RailPiercer => 0.72f,
                CardId.PursuitShot => 0.86f,
                CardId.AegisRam => 0.88f,
                CardId.FrostLance => 0.84f,
                CardId.CounterPursuit => 0.96f,
                CardId.BroadsideVolley => 0.94f,
                CardId.MeltdownBurst => 1.08f,
                CardId.Scattershot => 0.82f,
                CardId.MissileSwarm => 1.12f,
                CardId.InterceptMine => 1.02f,
                _ => 0.78f
            };
        }

        private static float AttackShake(CardId card)
        {
            return card switch
            {
                CardId.BurstFire => 8f,
                CardId.PursuitShot => 11f,
                CardId.Scattershot => 12f,
                CardId.FrostLance => 15f,
                CardId.RailPiercer => 18f,
                CardId.MissileSwarm => 19f,
                CardId.InterceptMine => 20f,
                CardId.AegisRam => 22f,
                CardId.BroadsideVolley => 23f,
                CardId.CounterPursuit => 24f,
                CardId.OverloadAim => 27f,
                CardId.MeltdownBurst => 30f,
                _ => 14f
            };
        }

        private static float AttackShakeDuration(CardId card)
        {
            return Mathf.Lerp(0.32f, 0.72f, Mathf.InverseLerp(8f, 30f, AttackShake(card)));
        }

        private static float AttackScreenPower(CardId card)
        {
            return Mathf.Lerp(0.7f, 2.15f, Mathf.InverseLerp(8f, 30f, AttackShake(card)));
        }

        private void PlayAttackSound(CardId card, bool hit)
        {
            switch (card)
            {
                case CardId.BurstFire:
                    PlayLayeredSound(shotSound, 1.16f, 0.78f, hit ? impactSound : clickSound, 1.28f, hit ? 0.5f : 0.18f);
                    break;
                case CardId.OverloadAim:
                    PlayLayeredSound(heavyShotSound, 0.68f, 1f, hit ? lowExplosionSound : warningSound, 0.58f, 0.82f);
                    break;
                case CardId.RailPiercer:
                    PlayLayeredSound(heavyShotSound, 1.22f, 0.94f, hit ? impactSound : clickSound, 1.48f, 0.82f);
                    break;
                case CardId.PursuitShot:
                    PlayLayeredSound(shotSound, 1.42f, 0.82f, maneuverLockSound, 1.18f, 0.48f);
                    break;
                case CardId.AegisRam:
                    PlayLayeredSound(shieldSound, 0.72f, 0.88f, hit ? impactSound : maneuverSound, 0.62f, 0.96f);
                    break;
                case CardId.FrostLance:
                    PlayLayeredSound(shieldSound, 1.52f, 0.76f, hit ? impactSound : clickSound, 1.22f, 0.68f);
                    break;
                case CardId.CounterPursuit:
                    PlayLayeredSound(maneuverLockSound, 0.76f, 0.88f, hit ? heavyShotSound : shotSound, 0.92f, 0.9f);
                    break;
                case CardId.BroadsideVolley:
                    PlayLayeredSound(heavyShotSound, 1.02f, 0.96f, hit ? lowExplosionSound : shotSound, 0.72f, 0.7f);
                    break;
                case CardId.MeltdownBurst:
                    PlayLayeredSound(lowExplosionSound, 0.52f, 1f, heavyShotSound, 0.58f, 0.84f);
                    break;
                case CardId.Scattershot:
                    PlayLayeredSound(shotSound, 1.62f, 0.9f, hit ? impactSound : clickSound, 1.34f, 0.6f);
                    break;
                case CardId.MissileSwarm:
                    PlayLayeredSound(shotSound, 0.74f, 0.82f, hit ? lowExplosionSound : maneuverSound, 0.94f, 0.7f);
                    break;
                case CardId.InterceptMine:
                    PlayLayeredSound(maneuverLockSound, 1.48f, 0.75f, hit ? destructionSound : warningSound, 1.16f, 0.72f);
                    break;
                default:
                    PlaySound(shotSound);
                    break;
            }
        }

        private void PlaySound(AudioClip clip, float pitch = 1f, float volume = 1f)
        {
            if (audioSource == null || clip == null)
                return;
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private void PlayLayeredSound(AudioClip primary, float primaryPitch, float primaryVolume,
            AudioClip layer, float layerPitch, float layerVolume)
        {
            PlaySound(primary, primaryPitch, primaryVolume);
            if (audioLayerSource == null || layer == null)
                return;
            audioLayerSource.pitch = layerPitch;
            audioLayerSource.PlayOneShot(layer, Mathf.Clamp01(layerVolume));
        }

        private static AudioClip LoadSound(string resourcePath, string fallbackName, float fallbackFrequency,
            float fallbackDuration, float fallbackVolume, float fallbackNoise)
        {
            AudioClip loaded = Resources.Load<AudioClip>(resourcePath);
            return loaded != null
                ? loaded
                : CreateTone(fallbackName, fallbackFrequency, fallbackDuration, fallbackVolume, fallbackNoise);
        }

        private static AudioClip CreateTone(string clipName, float frequency, float duration, float volume, float noiseAmount)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
            var samples = new float[sampleCount];
            var random = new System.Random(clipName.GetHashCode());
            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)sampleRate;
                float normalized = i / (float)sampleCount;
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(normalized));
                float tone = Mathf.Sin(2f * Mathf.PI * frequency * time);
                float harmonic = Mathf.Sin(2f * Mathf.PI * frequency * 2.01f * time) * 0.28f;
                float noise = ((float)random.NextDouble() * 2f - 1f) * noiseAmount;
                samples[i] = (tone + harmonic + noise) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static Rect PixelRect(Vector2 center, float x, float y, float width, float height, float scale)
        {
            return new Rect(center.x + x * scale, center.y + y * scale, width * scale, height * scale);
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void DrawTextureTinted(Rect rect, Texture texture, Color tint)
        {
            if (texture == null)
                return;
            Color previous = GUI.color;
            GUI.color = tint;
            GUI.DrawTexture(rect, texture, ScaleMode.ScaleAndCrop, false);
            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
                return;

            Font pixelFont = Resources.Load<Font>("Fonts/ark-pixel-12px-proportional-zh_cn");
            uiFont = pixelFont != null
                ? pixelFont
                : Font.CreateDynamicFontFromOSFont(
                    new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial" }, 24);
            displayFont = pixelFont != null
                ? pixelFont
                : Font.CreateDynamicFontFromOSFont(
                    new[] { "SimHei", "Microsoft YaHei UI", "Microsoft YaHei", "Arial" }, 32);
            terminalFont = pixelFont != null
                ? pixelFont
                : Font.CreateDynamicFontFromOSFont(
                    new[] { "DengXian", "Microsoft YaHei UI", "Microsoft YaHei", "SimHei" }, 22);
            titleStyle = MakeStyle(48, FontStyle.Bold, Ink, TextAnchor.MiddleLeft);
            titleStyle.font = displayFont;
            subtitleStyle = MakeStyle(24, FontStyle.Bold, Ink, TextAnchor.MiddleLeft);
            subtitleStyle.font = displayFont;
            bodyStyle = MakeStyle(20, FontStyle.Normal, Ink, TextAnchor.UpperLeft);
            bodyStyle.wordWrap = true;
            smallStyle = MakeStyle(17, FontStyle.Bold, Ink, TextAnchor.MiddleLeft);
            tinyStyle = MakeStyle(14, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            tinyStyle.font = terminalFont;
            centeredStyle = MakeStyle(15, FontStyle.Bold, Ink, TextAnchor.MiddleCenter);
            cardTitleStyle = MakeStyle(17, FontStyle.Bold, Ink, TextAnchor.MiddleCenter);
            cardTitleStyle.font = displayFont;
            cardBodyStyle = MakeStyle(16, FontStyle.Normal, Ink, TextAnchor.UpperLeft);
            cardBodyStyle.wordWrap = true;
            moduleTitleStyle = MakeStyle(18, FontStyle.Bold, new Color32(255, 236, 164, 255), TextAnchor.MiddleCenter);
            moduleTitleStyle.font = displayFont;
            moduleBodyStyle = MakeStyle(16, FontStyle.Normal, new Color32(225, 244, 250, 255), TextAnchor.UpperLeft);
            moduleBodyStyle.wordWrap = true;
            buttonLabelStyle = MakeStyle(21, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            buttonLabelStyle.font = displayFont;
            hudStyle = MakeStyle(17, FontStyle.Bold, new Color32(222, 248, 248, 255), TextAnchor.MiddleLeft);
            hudStyle.font = terminalFont;
            hudCenteredStyle = MakeStyle(15, FontStyle.Bold, new Color32(222, 248, 248, 255), TextAnchor.MiddleCenter);
            hudCenteredStyle.font = terminalFont;
            neonTitleStyle = MakeStyle(48, FontStyle.Bold, new Color32(235, 255, 255, 255), TextAnchor.MiddleLeft);
            neonTitleStyle.font = displayFont;
            neonSubtitleStyle = MakeStyle(24, FontStyle.Bold, new Color32(117, 241, 248, 255), TextAnchor.MiddleLeft);
            neonSubtitleStyle.font = displayFont;
            neonBodyStyle = MakeStyle(20, FontStyle.Normal, new Color32(218, 235, 245, 255), TextAnchor.UpperLeft);
            neonBodyStyle.wordWrap = true;
            contractBadgeStyle = MakeStyle(14, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            contractBadgeStyle.font = terminalFont;
            contractBadgeStyle.wordWrap = false;
            contractBadgeStyle.clipping = TextClipping.Clip;
        }

        private GUIStyle MakeStyle(int size, FontStyle fontStyle, Color color, TextAnchor anchor)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                font = uiFont,
                fontSize = size,
                fontStyle = fontStyle,
                alignment = anchor,
                clipping = TextClipping.Clip
            };
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
            style.onNormal.textColor = color;
            style.onHover.textColor = color;
            style.onActive.textColor = color;
            style.onFocused.textColor = color;
            return style;
        }

        private static void DrawFittedLabel(Rect rect, string text, GUIStyle source, int minimumFontSize)
        {
            GUI.Label(rect, text, CreateFittedStyle(rect, text, source, minimumFontSize));
        }

        private static GUIStyle CreateFittedStyle(Rect rect, string text, GUIStyle source, int preferredMinimumFontSize)
        {
            var fitted = new GUIStyle(source)
            {
                wordWrap = true,
                clipping = TextClipping.Clip
            };
            int originalSize = Mathf.Max(6, Mathf.Max(preferredMinimumFontSize, source.fontSize));
            int chosenSize = 6;
            for (int size = originalSize; size >= 6; size--)
            {
                fitted.fontSize = size;
                if (fitted.CalcHeight(new GUIContent(text), rect.width) <= rect.height)
                {
                    chosenSize = size;
                    break;
                }
            }
            fitted.fontSize = chosenSize;
            return fitted;
        }

        private static void DrawCyberLabel(Rect rect, string text, GUIStyle style, Color glow)
        {
            GUIStyle fitted = CreateFittedStyle(rect, text, style, 8);
            Color previous = GUI.color;
            GUI.color = new Color(1f, 0.08f, 0.78f, 0.34f);
            GUI.Label(new Rect(rect.x + 3f, rect.y + 2f, rect.width, rect.height), text, fitted);
            GUI.color = new Color(glow.r, glow.g, glow.b, 0.48f);
            GUI.Label(new Rect(rect.x - 2f, rect.y - 1f, rect.width, rect.height), text, fitted);
            GUI.color = previous;
            GUI.Label(rect, text, fitted);
        }
    }
}
