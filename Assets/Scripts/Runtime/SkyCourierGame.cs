using System;
using System.Collections;
using System.Collections.Generic;
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
            Contract,
            Map,
            Battle,
            Reward,
            Shop,
            Event,
            Rest,
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
        private readonly List<EnemyDeathFx> enemyDeathFx = new List<EnemyDeathFx>();
        private readonly List<EnemyAttackFx> enemyAttackFx = new List<EnemyAttackFx>();
        private readonly List<EnemyLaneFx> enemyLaneFx = new List<EnemyLaneFx>();
        private readonly bool[] shopBought = new bool[3];
        private readonly HashSet<int> completedRouteNodes = new HashSet<int>();
        private readonly RouteDefinition route = RouteCatalog.WindmillArchipelago;

        private ScreenMode screen = ScreenMode.Title;
        private int routeIndex;
        private int selectedRouteNodeId;
        private int lastCompletedRouteNodeId = -1;
        private float routeScroll;
        private bool eventResolved;
        private string eventResult;
        private bool restResolved;
        private string restResult;
        private int credits;
        private int runHull;
        private int runCargoIntegrity;
        private CargoContract selectedContract = CargoContract.FragileMedicine;
        private int runContractBonus;
        private bool repairBought;
        private int runTurns;
        private int runCardsPlayed;
        private int runDamageTaken;
        private int runOverheats;
        private int runCalamityInterrupts;
        private int runCalamityEvades;
        private int runCalamityHits;
        private int runTrackingHits;
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
        private const string MusicVolumeKey = "SkyCourier.MusicVolume";
        private const string SfxVolumeKey = "SkyCourier.SfxVolume";
        private const string FirstBattleGuideKey = "SkyCourier.FirstBattleGuide";
        private float musicVolume = 0.8f;
        private float sfxVolume = 0.9f;
        private bool paused;
        private bool showFirstBattleGuide;
        private int firstBattleGuidePage;
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
            Application.targetFrameRate = 60;
            musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 0.8f);
            sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 0.9f);
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
        }

        private void Update()
        {
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
                ScreenMode.Contract => titleMusic,
                ScreenMode.Map => routeMusic,
                ScreenMode.Battle => battle.Encounter == EncounterId.Boss ? bossMusic : battleMusic,
                ScreenMode.Reward => restMusic,
                ScreenMode.Shop => restMusic,
                ScreenMode.Rest => restMusic,
                ScreenMode.Event => routeMusic,
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
                shakeX = Mathf.Sin(Time.time * 113f) * shakeMagnitude * remaining;
                shakeY = Mathf.Cos(Time.time * 97f) * shakeMagnitude * 0.55f * remaining;
            }
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX + shakeX * scale, offsetY + shakeY * scale, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));

            if (Event.current.type == EventType.KeyDown)
                HandleKeyboardShortcuts(Event.current);
            if (Event.current.type == EventType.Repaint)
                hoverKeyThisFrame = null;

            bool modalOpen = paused || showFirstBattleGuide;
            bool previousEnabled = GUI.enabled;
            GUI.enabled = !modalOpen;
            DrawSky();
            switch (screen)
            {
                case ScreenMode.Title:
                    DrawTitleScreen();
                    break;
                case ScreenMode.Contract:
                    DrawContractScreen();
                    break;
                case ScreenMode.Map:
                    DrawRouteMap();
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
                case ScreenMode.Complete:
                    DrawRunComplete();
                    break;
            }
            GUI.enabled = previousEnabled;

            DrawScreenTexture();

            if (screen != ScreenMode.Title && !showFirstBattleGuide && !paused)
                DrawSystemButton();
            if (paused)
                DrawPauseOverlay();
            else if (showFirstBattleGuide)
                DrawFirstBattleGuide();

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
            GUI.Label(new Rect(633, 231, 650, 100), "云海邮差", neonTitleStyle);
            GUI.color = new Color(0.18f, 0.95f, 1f, 0.45f);
            GUI.Label(new Rect(621, 225, 650, 100), "云海邮差", neonTitleStyle);
            GUI.color = previous;
            GUI.Label(new Rect(625, 225, 650, 100), "云海邮差", neonTitleStyle);
            GUI.Label(new Rect(630, 315, 650, 50), "三航道空战卡牌肉鸽", neonSubtitleStyle);
            GUI.Label(new Rect(630, 385, 610, 130),
                "运送不可能送达的包裹，穿越风暴云海。\n观察敌人意图，灵活切换航道。\n压榨老旧引擎——但别让它烧起来。", neonBodyStyle);

            DrawRect(new Rect(630, 520, 95, 24), new Color32(17, 34, 67, 245));
            GUI.Label(new Rect(630, 520, 95, 24), "DECK OPS", tinyStyle);
            DrawRect(new Rect(735, 520, 95, 24), new Color32(17, 34, 67, 245));
            GUI.Label(new Rect(735, 520, 95, 24), "AIR LANE", tinyStyle);

            Rect startButton = new Rect(630, 555, 330, 74);
            DrawPixelButton(startButton, "开始配送", PostalRed, () =>
            {
                StartNewRun();
            }, true, "ENTER");

            DrawFittedLabel(new Rect(630, 655, 650, 42), "原型 0.26  //  TACTICAL ROUTES", hudStyle, 11);
        }

        private void HandleKeyboardShortcuts(Event input)
        {
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

#if UNITY_EDITOR
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
                CompleteCurrentRouteNode();
                screen = ScreenMode.Map;
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
#endif

            switch (screen)
            {
                case ScreenMode.Title when confirm:
                    StartNewRun();
                    handled = true;
                    break;
                case ScreenMode.Contract when number >= 0 && number < 4:
                    InitializeRun((CargoContract)number);
                    handled = true;
                    break;
                case ScreenMode.Map when confirm:
                    EnterCurrentNode();
                    handled = true;
                    break;
                case ScreenMode.Battle when battle.Victory && confirm && !DeathAnimationActive():
                    ContinueAfterVictory();
                    handled = true;
                    break;
                case ScreenMode.Battle when battle.Defeat && confirm:
                    StartNewRun();
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
            GUI.Label(new Rect(130, 85, 900, 70), "选择配送合同", neonTitleStyle);
            DrawFittedLabel(new Rect(135, 150, 1160, 45),
                "每份货物有3格完整度；触发合同风险会失去1格，并降低最终评级。", neonBodyStyle, 13);

            DrawContractCard(CargoContract.FragileMedicine, new Rect(95, 225, 330, 510), "护盾冲角 / 锁定狙击",
                "积累护盾转化重击，或叠加锁定打出高倍率轨炮。", "基础报酬");
            DrawContractCard(CargoContract.CryoSerum, new Rect(455, 225, 330, 510), "零度循环 / 熔炉爆发",
                "降温返还能量保持低热，也能主动积热后一次清场。", "报酬 +15%");
            DrawContractCard(CargoContract.StormCore, new Rect(815, 225, 330, 510), "矢量追猎 / 蜂群弹幕",
                "换道积累动量完成追击，或用多段弹幕覆盖全部航道。", "报酬 +25%");
            DrawContractCard(CargoContract.BlackBoxRelay, new Rect(1175, 225, 330, 510), "航迹欺骗 / 逆向追猎",
                "主动控制航迹暴露，把敌方锁定反转为爆发窗口。", "报酬 +30%");

            DrawFittedLabel(new Rect(210, 775, 1180, 34),
                "评级说明：3/3 = S 完好　2/3 = A 轻微受损　1/3 = B 严重受损　0/3 = C 货物损毁", hudCenteredStyle, 9);
        }

        private void DrawContractCard(CargoContract contract, Rect rect, string buildLabel, string description, string reward)
        {
            Color color = CargoColor(contract);
            bool hovered = rect.Contains(Event.current.mousePosition);
            if (hovered)
                RegisterHover($"contract-{contract}", $"点击签署 {CargoName(contract)}");
            DrawRect(new Rect(rect.x + 9, rect.y + 9, rect.width, rect.height), new Color32(2, 7, 22, 255));
            DrawRect(rect, new Color32(8, 20, 45, 250));
            DrawNeonFrame(rect, hovered ? Color.Lerp(color, Color.white, 0.28f) : color, hovered ? 4f : 2f);
            DrawRect(new Rect(rect.x, rect.y, rect.width, 76), new Color32(10, 30, 62, 255));
            DrawRect(new Rect(rect.x, rect.y, 8, rect.height), color);
            DrawCargoIcon(new Vector2(rect.center.x, rect.y + 135), contract, color);

            DrawFittedLabel(new Rect(rect.x + 24, rect.y + 20, rect.width - 48, 44), CargoName(contract), neonSubtitleStyle, 13);
            DrawFittedLabel(new Rect(rect.x + 30, rect.y + 205, rect.width - 60, 28), $"BUILD // {buildLabel}", tinyStyle, 8);
            DrawRect(new Rect(rect.x + 30, rect.y + 245, rect.width - 60, 104), new Color32(3, 11, 31, 230));
            DrawRect(new Rect(rect.x + 30, rect.y + 245, 6, 104), color);
            DrawFittedLabel(new Rect(rect.x + 48, rect.y + 254, rect.width - 96, 88), CargoRule(contract), neonBodyStyle, 14);
            DrawFittedLabel(new Rect(rect.x + 30, rect.y + 358, rect.width - 60, 65), description, tinyStyle, 11);
            DrawFittedLabel(new Rect(rect.x + 30, rect.y + 422, rect.width - 60, 30), reward, hudCenteredStyle, 9);

            DrawPixelButton(new Rect(rect.x + 62, rect.y + 455, rect.width - 124, 48), "签署合同", color,
                () => InitializeRun(contract), true);
        }

        private void DrawCargoIcon(Vector2 center, CargoContract contract, Color color)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 4.8f + (int)contract) * 0.06f;
            DrawRect(new Rect(center.x - 64 * pulse, center.y - 44 * pulse, 128 * pulse, 88 * pulse), new Color32(3, 9, 28, 245));
            DrawPixelOutline(new Rect(center.x - 64 * pulse, center.y - 44 * pulse, 128 * pulse, 88 * pulse), color, 4f);
            if (contract == CargoContract.FragileMedicine)
            {
                DrawRect(new Rect(center.x - 18, center.y - 31, 36, 62), color);
                DrawRect(new Rect(center.x - 28, center.y - 18, 56, 11), Color.white);
                DrawRect(new Rect(center.x - 6, center.y - 2, 12, 24), PostalRed);
            }
            else if (contract == CargoContract.CryoSerum)
            {
                DrawRect(new Rect(center.x - 12, center.y - 36, 24, 72), color);
                DrawRect(new Rect(center.x - 36, center.y - 12, 72, 24), color);
                DrawRect(new Rect(center.x - 25, center.y - 25, 50, 50), new Color32(180, 246, 255, 90));
            }
            else if (contract == CargoContract.StormCore)
            {
                DrawRect(new Rect(center.x - 24, center.y - 24, 48, 48), color);
                DrawPixelOutline(new Rect(center.x - 42, center.y - 42, 84, 84), NeonViolet, 5f);
                DrawRect(new Rect(center.x - 6, center.y - 54, 12, 108), Color.white);
                DrawRect(new Rect(center.x - 54, center.y - 6, 108, 12), color);
            }
            else
            {
                DrawRect(new Rect(center.x - 38, center.y - 28, 76, 56), new Color32(3, 9, 28, 255));
                DrawPixelOutline(new Rect(center.x - 38, center.y - 28, 76, 56), color, 4f);
                DrawRect(new Rect(center.x - 25, center.y - 15, 50, 8), color);
                DrawRect(new Rect(center.x - 25, center.y + 2, 34, 8), Color.white);
                DrawRect(new Rect(center.x + 18, center.y + 2, 7, 8), PostalRed);
            }
        }

        private static string CargoName(CargoContract contract)
        {
            return contract switch
            {
                CargoContract.CryoSerum => "零度血清",
                CargoContract.StormCore => "风暴核心",
                CargoContract.BlackBoxRelay => "幽灵黑匣",
                _ => "易碎药剂"
            };
        }

        private static string CargoRule(CargoContract contract)
        {
            return contract switch
            {
                CargoContract.CryoSerum => "完整度共3格。回合结束时热量达到6点，则失去1格。",
                CargoContract.StormCore => "完整度共3格。连续两回合没有切换航道，则失去1格。",
                CargoContract.BlackBoxRelay => "完整度共3格。回合结束时航迹暴露达到2层，则失去1格。",
                _ => "完整度共3格。单次受到6点以上未抵消伤害，则失去1格。"
            };
        }

        private static string CargoActionHint(CargoContract contract)
        {
            return contract switch
            {
                CargoContract.CryoSerum => "安全操作：结束回合前将热量降到5点以下",
                CargoContract.StormCore => "安全操作：至少每2回合切换1次航道",
                CargoContract.BlackBoxRelay => "安全操作：用扰频或停留将航迹暴露控制在1层",
                _ => "安全操作：用护盾将单次未抵消伤害压到5点以下"
            };
        }

        private static string CargoStatus(int integrity)
        {
            return integrity switch
            {
                3 => "完好",
                2 => "轻微受损",
                1 => "严重受损",
                _ => "货物损毁"
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
                _ => new Color32(67, 188, 153, 255)
            };
        }

        private void StartNewRun()
        {
            SetPaused(false);
            screen = ScreenMode.Contract;
        }

        private void InitializeRun(CargoContract contract)
        {
            selectedContract = contract;
            runDeck.Clear();
            runUpgrades.Clear();
            runUpgradeBranches.Clear();
            runModules.Clear();
            runDeck.AddRange(new[]
            {
                CardId.BurstFire, CardId.BurstFire,
                CardId.BankUp, CardId.BankUp,
                CardId.BankDown, CardId.BankDown,
                CardId.WindGuard, CardId.WindGuard,
                CardId.EmergencyCoolant, CardId.BroadsideVolley,
                CardId.OverloadAim, CardId.EngineOverclock
            });
            runDeck.Add(contract switch
            {
                CargoContract.FragileMedicine => CardId.ReactivePlating,
                CargoContract.CryoSerum => CardId.CryoPump,
                CargoContract.StormCore => CardId.VectorDash,
                _ => CardId.SignalScrambler
            });
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
            runHull = BattleState.MaxPlayerHealth;
            runCargoIntegrity = 3;
            repairBought = false;
            runTurns = 0;
            runCardsPlayed = 0;
            runDamageTaken = 0;
            runOverheats = 0;
            runCalamityInterrupts = 0;
            runCalamityEvades = 0;
            runCalamityHits = 0;
            runTrackingHits = 0;
            runContractBonus = 0;
            lastFieldRepair = 0;
            for (int i = 0; i < shopBought.Length; i++)
                shopBought[i] = false;
            screen = ScreenMode.Map;
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
                    StartBattle(node.Encounter);
                    break;
                case RouteNodeKind.Shop:
                    ResetShopInventory();
                    screen = ScreenMode.Shop;
                    break;
                case RouteNodeKind.Event:
                    screen = ScreenMode.Event;
                    break;
                case RouteNodeKind.Rest:
                    screen = ScreenMode.Rest;
                    break;
            }
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

        private void FocusRouteColumn(int column)
        {
            const float columnSpacing = 250f;
            const float viewportWidth = 1360f;
            float contentWidth = 210f + route.ColumnCount * columnSpacing;
            float target = 105f + column * columnSpacing - viewportWidth * 0.42f;
            routeScroll = Mathf.Clamp(target, 0f, Mathf.Max(0f, contentWidth - viewportWidth));
        }

        private void ResetShopInventory()
        {
            repairBought = false;
            for (int i = 0; i < shopBought.Length; i++)
                shopBought[i] = false;
        }

        private void StartBattle(EncounterId encounter)
        {
            battle.StartEncounter(encounter, runDeck, runHull, runCargoIntegrity, selectedContract, runUpgrades, runModules,
                -1, runUpgradeBranches);
            enemyDeathFx.Clear();
            enemyAttackFx.Clear();
            enemyLaneFx.Clear();
            battleInputLockUntil = 0f;
            commandChain = 0;
            commandChainTurn = 0;
            screen = ScreenMode.Battle;
            bannerText = encounter == EncounterId.Boss
                ? "WARNING // 巨型磁暴反应"
                : battle.EncounterVariant == 1
                    ? $"ANOMALY DETECTED // {battle.FormationName}"
                    : $"CONTRACT ACTIVE // {CargoActionHint(selectedContract).Replace("安全操作：", string.Empty)}";
            bannerUntil = Time.time + 1.85f;
            if (PlayerPrefs.GetInt(FirstBattleGuideKey, 0) == 0)
            {
                firstBattleGuidePage = 0;
                showFirstBattleGuide = true;
                Time.timeScale = 0f;
            }
        }

        private void SetPaused(bool value)
        {
            paused = value;
            if (!showFirstBattleGuide)
                Time.timeScale = paused ? 0f : 1f;
        }

        private void DrawSystemButton()
        {
            Rect rect = new Rect(1490, 122, 54, 48);
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
            Rect panel = new Rect(500, 135, 600, 630);
            DrawRect(panel, new Color32(7, 18, 43, 255));
            DrawNeonFrame(panel, NeonCyan, 3f);
            GUI.Label(new Rect(560, 180, 480, 64), "系统暂停", neonTitleStyle);
            GUI.Label(new Rect(565, 258, 470, 32), "MUSIC // 音乐音量", hudStyle);
            float nextMusic = GUI.HorizontalSlider(new Rect(565, 304, 470, 28), musicVolume, 0f, 1f);
            GUI.Label(new Rect(950, 258, 85, 32), $"{Mathf.RoundToInt(nextMusic * 100)}%", hudStyle);
            GUI.Label(new Rect(565, 352, 470, 32), "SFX // 音效音量", hudStyle);
            float nextSfx = GUI.HorizontalSlider(new Rect(565, 398, 470, 28), sfxVolume, 0f, 1f);
            GUI.Label(new Rect(950, 352, 85, 32), $"{Mathf.RoundToInt(nextSfx * 100)}%", hudStyle);
            if (!Mathf.Approximately(nextMusic, musicVolume) || !Mathf.Approximately(nextSfx, sfxVolume))
            {
                musicVolume = nextMusic;
                sfxVolume = nextSfx;
                PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
                PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
                PlayerPrefs.Save();
            }
            DrawPixelButton(new Rect(610, 452, 380, 58), "继续配送", NeonCyan, () => SetPaused(false), true, "ESC");
            DrawPixelButton(new Rect(610, 526, 380, 58), "操作指南", NeonViolet, () =>
            {
                SetPaused(false);
                firstBattleGuidePage = 0;
                showFirstBattleGuide = true;
                Time.timeScale = 0f;
            });
            DrawPixelButton(new Rect(610, 600, 380, 58), "重新开始本次配送", PostalRed, () =>
            {
                SetPaused(false);
                StartNewRun();
            });
            DrawPixelButton(new Rect(610, 674, 380, 58), "返回标题", Shadow, () =>
            {
                SetPaused(false);
                screen = ScreenMode.Title;
            });
        }

        private void DrawFirstBattleGuide()
        {
            DrawRect(new Rect(0, 0, ReferenceWidth, ReferenceHeight), new Color32(1, 5, 16, 230));
            Rect panel = new Rect(330, 145, 940, 610);
            DrawRect(panel, new Color32(7, 18, 43, 255));
            DrawNeonFrame(panel, firstBattleGuidePage == 2 ? NeonViolet : NeonCyan, 3f);
            string title = firstBattleGuidePage switch
            {
                0 => "先看意图，再决定航道",
                1 => "管理能量与热量",
                _ => "换道不是永久安全区"
            };
            string body = firstBattleGuidePage switch
            {
                0 => "敌人头顶会显示下一步行动。\n同航道武器只能攻击正前方目标；换道可以避开普通攻击。",
                1 => "每回合拥有3点能量，卡牌下方会标记热量。\n热量达到上限会损伤机体；Space 或右侧按钮结束回合。",
                _ => "第一次换道可以安全规避。连续回合依赖换道会累积航迹暴露，敌人将预告并发射追踪弹。\n停留一回合，或使用扰频与刹车牌，可以降低暴露。"
            };
            DrawFittedLabel(new Rect(405, 215, 790, 62), title, neonTitleStyle, 24);
            DrawRect(new Rect(405, 315, 790, 205), new Color32(3, 11, 31, 240));
            DrawFittedLabel(new Rect(450, 345, 700, 145), body, neonBodyStyle, 18);
            DrawFittedLabel(new Rect(500, 552, 600, 34),
                $"操作提示 {firstBattleGuidePage + 1}/3　//　仅首次自动显示", hudCenteredStyle, 9);
            if (firstBattleGuidePage < 2)
            {
                DrawPixelButton(new Rect(585, 625, 430, 66), "下一条", NeonCyan, () => firstBattleGuidePage++);
            }
            else
            {
                DrawPixelButton(new Rect(585, 625, 430, 66), "开始战斗", PostalRed, () =>
                {
                    showFirstBattleGuide = false;
                    PlayerPrefs.SetInt(FirstBattleGuideKey, 1);
                    PlayerPrefs.Save();
                    Time.timeScale = 1f;
                });
            }
        }

        private void PlayCardWithFeedback(int handIndex)
        {
            if (!CanPlayInteractive(handIndex))
                return;

            CardId id = battle.Hand[handIndex];
            bool upgradedCard = battle.IsUpgraded(id);
            int laneBefore = battle.PlayerLane;
            int hullBefore = battle.PlayerHealth;
            int enemyDurabilityBefore = TotalEnemyDurability();
            int[] enemyHealthSnapshot = new int[battle.Enemies.Count];
            int[] enemyArmorSnapshot = new int[battle.Enemies.Count];
            bool[] chargeInterruptedSnapshot = new bool[battle.Enemies.Count];
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                enemyHealthSnapshot[i] = battle.Enemies[i].Health;
                enemyArmorSnapshot[i] = battle.Enemies[i].Armor;
                chargeInterruptedSnapshot[i] = battle.Enemies[i].ChargeInterrupted;
            }
            battle.PlayCard(handIndex);
            bool maneuverCard = id == CardId.BankUp || id == CardId.BankDown || id == CardId.VectorDash;
            bool volleyCard = id == CardId.BroadsideVolley || id == CardId.MeltdownBurst ||
                id == CardId.Scattershot || id == CardId.MissileSwarm || id == CardId.InterceptMine;
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
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                EnemyState enemy = battle.Enemies[i];
                if ((enemyHealthSnapshot[i] > enemy.Health || enemyArmorSnapshot[i] > enemy.Armor) && !volleyCard)
                    impactPoint = EnemyBasePosition(i, enemy);
                if (!chargeInterruptedSnapshot[i] && enemy.ChargeInterrupted)
                    interruptedCharge = true;
                if (enemyHealthSnapshot[i] > 0 && !enemy.Alive)
                {
                    Vector2 deathPosition = EnemyBasePosition(i, enemy);
                    enemyDeathFx.Add(new EnemyDeathFx
                    {
                        Position = deathPosition,
                        StartTime = Time.time,
                        Name = enemy.Name,
                        Seed = 31 + i * 17 + battle.Turn * 13,
                        Kind = enemy.Kind
                    });
                    impactPoint = deathPosition;
                    destroyedTarget = true;
                }
            }

            if (destroyedTarget)
            {
                bannerText = "TARGET BREAK // 敌机解体";
                bannerUntil = Time.time + 1.15f;
                impactFlashUntil = Time.time + 1.2f;
                combatFxDuration = Mathf.Max(combatFxDuration, 1.05f);
                TriggerShake(31f, 0.82f);
                PlayLayeredSound(destructionSound, 0.82f, 0.9f, lowExplosionSound, 0.68f, 0.88f);
                StartCoroutine(DelayedHitStop(0.38f, 0.18f));
                TriggerFullScreenImpact(2.25f, 1.08f, true);
            }

            switch (id)
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
                    combatFx = CombatFx.Shield;
                    combatFxText = $"{CardLibrary.Get(id).Name} // 护盾在线";
                    PlaySound(shieldSound);
                    break;
                case CardId.BankUp:
                case CardId.BankDown:
                case CardId.VectorDash:
                    combatFx = CombatFx.Maneuver;
                    combatFxDuration = 0.72f;
                    combatFxText = $"航道锁定 // {battle.PlayerLane + 1}";
                    if (selectedContract == CargoContract.StormCore)
                    {
                        bannerText = "CONTRACT SAFE // 风暴核心稳定计时已重置";
                        bannerUntil = Time.time + 1.05f;
                    }
                    break;
                case CardId.EmergencyCoolant:
                case CardId.CryoPump:
                    combatFx = CombatFx.Coolant;
                    combatFxText = id == CardId.CryoPump ? "低温循环 // 废热回收" : "热量下降";
                    PlaySound(shieldSound, 0.75f);
                    if (selectedContract == CargoContract.CryoSerum)
                    {
                        bannerText = "CONTRACT SAFE // 血清温度恢复安全范围";
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
                    combatFx = CombatFx.Overclock;
                    combatFxText = id == CardId.TargetLock
                        ? $"TARGET LOCK ×{battle.LockOn}"
                        : id == CardId.HeatCharge ? "HEAT CHARGE // 强制供能" : upgradedCard ? "+2 能量" : "+1 能量";
                    PlaySound(clickSound, 1.35f);
                    bannerText = "OVERDRIVE // 能量回路突破";
                    bannerUntil = Time.time + 0.85f;
                    break;
            }

            if (!destroyedTarget && !string.IsNullOrEmpty(battle.LastModuleProc))
            {
                bannerText = $"MODULE PROC // {battle.LastModuleProc}";
                bannerUntil = Time.time + 1.05f;
                PlaySound(rewardSound, 1.28f, 0.55f);
                TriggerShake(6f, 0.24f);
            }

            if (!destroyedTarget && interruptedCharge)
            {
                bannerText = "CHARGE BREAK // 灾变蓄力已中断";
                bannerUntil = Time.time + 1.15f;
                PlayLayeredSound(shieldSound, 1.35f, 0.72f, impactSound, 1.1f, 0.55f);
                TriggerShake(12f, 0.4f);
                TriggerFullScreenImpact(1.1f, 0.62f, false);
            }

            if (!destroyedTarget && !string.IsNullOrEmpty(battle.LastArmorBreak))
            {
                bannerText = $"ARMOR BREAK // {battle.LastArmorBreak}";
                bannerUntil = Time.time + 1.05f;
                combatFxText = "装甲破裂 // 核心暴露";
                PlayLayeredSound(impactSound, 0.72f, 0.86f, destructionSound, 1.35f, 0.28f);
                TriggerShake(17f, 0.46f);
                StartCoroutine(DelayedHitStop(0.3f, 0.105f));
                TriggerFullScreenImpact(1.35f, 0.64f, false);
            }
            else if (!destroyedTarget && battle.LastAttackCritical)
            {
                bannerText = "CRITICAL // 弱点贯穿";
                bannerUntil = Time.time + 0.95f;
                combatFxText = $"CRITICAL // {damageDealt} DAMAGE";
                PlayLayeredSound(heavyShotSound, 1.2f, 0.72f, impactSound, 0.82f, 0.8f);
                TriggerShake(14f, 0.38f);
                StartCoroutine(DelayedHitStop(0.24f, 0.09f));
            }
            else if (!destroyedTarget && !string.IsNullOrEmpty(battle.LastStatusTrigger))
            {
                bannerText = $"STATUS PROC // {battle.LastStatusTrigger}";
                bannerUntil = Time.time + 0.92f;
                PlaySound(rewardSound, 1.22f, 0.42f);
            }

            if (battle.PlayerHealth < hullBefore)
            {
                dangerFlashUntil = Time.time + 0.32f;
                TriggerShake(8f, 0.3f);
                PlaySound(warningSound);
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
                string intent = battle.IntentFor(enemy);
                bool tracking = intent.Contains("追踪");
                bool attacks = intent.Contains("攻击") || intent.Contains("风暴") || intent.Contains("磁暴") ||
                    intent.Contains("灾变") || intent.Contains("封锁") || (tracking && !trackingQueued);
                if (attacks)
                {
                    trackingQueued |= tracking;
                    bool chargedAttack = enemy.Kind == EnemyKind.CalamityDrone || enemy.Kind == EnemyKind.StormManta;
                    int targetLane = chargedAttack ? enemy.ChargeTargetLane : battle.PlayerLane;
                    int laneDistance = Mathf.Abs(targetLane - battle.PlayerLane);
                    bool chargedHit = enemy.Kind != EnemyKind.StormManta
                        ? targetLane == battle.PlayerLane
                        : laneDistance == 0 || (enemy.Phase == 2 && laneDistance == 1);
                    pendingAttacks.Add(new EnemyAttackFx
                    {
                        Position = EnemyBasePosition(i, enemy),
                        StartTime = Time.time,
                        Kind = enemy.Kind,
                        Hit = !chargedAttack || chargedHit,
                        Damage = tracking ? BattleState.TrackingShotDamage : enemy.Kind == EnemyKind.StormManta
                            ? enemy.Phase == 1 ? BattleState.BossPhaseOneStrikeDamage : BattleState.BossPhaseTwoStrikeDamage
                            : enemy.Kind == EnemyKind.MailEater ? enemy.Damage + 2 : enemy.Damage,
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
            combatFxText = cargoDamaged ? $"合同完整度 -1 // {CargoStatus(battle.CargoIntegrity)}" : receivedDamage > 0 ? $"-{receivedDamage} 机体" : blockedDamage > 0 ? $"SHIELD ABSORB {blockedDamage}" : shiftedEnemies > 0 ? $"敌方变轨 ×{shiftedEnemies}" : "成功规避";
            bannerText = battle.PlayerHealth < hullBefore ? "DANGER // 机体受损" : battle.LastShieldBroken ? "SHIELD BREAK // 护盾耗尽" : blockedDamage > 0 ? "ABSORBED // 护盾完全抵消" : shiftedEnemies > 0 ? "HOSTILE SHIFT // 敌方切换航道" : "EVADE // 完美规避";
            bannerUntil = Time.time + 0.72f;
            if (cargoDamaged)
            {
                bannerText = $"CARGO BREACH // {battle.LastCargoDamageReason}";
                bannerUntil = Time.time + 1.25f;
                dangerFlashUntil = Time.time + 0.5f;
                impactFlashUntil = Time.time + 0.48f;
                TriggerShake(13f, 0.44f);
                PlayLayeredSound(warningSound, 0.72f, 0.82f, impactSound, 0.64f, 0.52f);
                TriggerFullScreenImpact(0.9f, 0.58f, false);
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

        private void DrawRouteMap()
        {
            DrawRect(new Rect(70, 58, 1460, 760), new Color32(2, 7, 22, 255));
            DrawRect(new Rect(78, 66, 1444, 744), PanelNight);
            DrawNeonFrame(new Rect(78, 66, 1444, 744), NeonCyan, 3f);
            DrawFittedLabel(new Rect(125, 88, 680, 64), "风车群岛分支航线", neonTitleStyle, 30);
            DrawFittedLabel(new Rect(128, 147, 830, 38),
                $"区域 {routeIndex + 1}/{route.ColumnCount}　//　前方情报解析范围：2", neonBodyStyle, 13);
            DrawRunHud(new Rect(1050, 88, 405, 96));

            Rect viewport = new Rect(120, 205, 1360, 405);
            const float columnSpacing = 250f;
            const float contentPadding = 105f;
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
            GUI.EndGroup();

            routeScroll = GUI.HorizontalScrollbar(new Rect(250, 625, 1100, 22), routeScroll,
                viewport.width, 0f, maxScroll + viewport.width);
            DrawPixelButton(new Rect(145, 617, 72, 42), "<", Shadow,
                () => routeScroll = Mathf.Clamp(routeScroll - 380f, 0f, maxScroll), routeScroll > 1f);
            DrawPixelButton(new Rect(1383, 617, 72, 42), ">", Shadow,
                () => routeScroll = Mathf.Clamp(routeScroll + 380f, 0f, maxScroll), routeScroll < maxScroll - 1f);

            RouteNodeDefinition selected = route.Get(selectedRouteNodeId);
            Color selectedColor = RouteNodeColor(selected.Kind);
            Rect detail = new Rect(190, 674, 835, 96);
            DrawRect(detail, new Color32(5, 14, 35, 245));
            DrawRect(new Rect(detail.x, detail.y, 8, detail.height), selectedColor);
            DrawRect(new Rect(detail.x + 25, detail.y + 12, 116, 26), new Color32(9, 27, 55, 250));
            DrawPixelOutline(new Rect(detail.x + 25, detail.y + 12, 116, 26), selectedColor, 2f);
            DrawFittedLabel(new Rect(detail.x + 29, detail.y + 12, 108, 26), RouteNodeKindLabel(selected.Kind), tinyStyle, 9);
            DrawFittedLabel(new Rect(detail.x + 160, detail.y + 7, detail.width - 185, 38), selected.Title, neonSubtitleStyle, 13);
            DrawFittedLabel(new Rect(detail.x + 30, detail.y + 48, detail.width - 55, 38), selected.Description, neonBodyStyle, 11);
            string enterLabel = selected.Kind == RouteNodeKind.Boss ? "挑战首领" : $"前往 {selected.Title}";
            DrawPixelButton(new Rect(1060, 683, 350, 72), enterLabel, selectedColor, EnterCurrentNode,
                IsRouteNodeAvailable(selected), "ENTER");
            DrawFittedLabel(new Rect(1080, 762, 310, 28), "滚轮 / 航标拖动浏览路线", tinyStyle, 9);
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
                : "新增卡牌或强化全部同名牌；奖励现在会产生明确的构筑分叉。", neonBodyStyle, 12);
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

            CardId keyCard = selectedContract switch
            {
                CargoContract.CryoSerum => routeIndex % 2 == 0 ? CardId.ZeroPointCalibration : CardId.RedlineIgnition,
                CargoContract.StormCore => routeIndex % 2 == 0 ? CardId.SlipstreamStrike : CardId.SwarmBeacon,
                CargoContract.BlackBoxRelay => CardId.GhostProtocol,
                _ => routeIndex % 2 == 0 ? CardId.PrismEcho : CardId.LockCascade
            };
            CardId coreCard = selectedContract switch
            {
                CargoContract.CryoSerum => CardId.CryoPump,
                CargoContract.StormCore => CardId.VectorDash,
                CargoContract.BlackBoxRelay => CardId.SignalScrambler,
                _ => CardId.ReactivePlating
            };
            return new[]
            {
                new RewardChoice { Kind = RewardKind.AddCard, Card = keyCard },
                new RewardChoice { Kind = RewardKind.UpgradeCard, Card = coreCard, Branch = UpgradeBranch.Alpha },
                new RewardChoice { Kind = RewardKind.UpgradeCard, Card = coreCard, Branch = UpgradeBranch.Beta }
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
            if (selectedContract == CargoContract.StormCore && (card == CardId.BankUp || card == CardId.BankDown))
                return "合同核心 · 重置稳定计时";
            if (selectedContract == CargoContract.CryoSerum && card == CardId.EmergencyCoolant)
                return "合同核心 · 避免高热货损";
            if (selectedContract == CargoContract.FragileMedicine && card == CardId.WindGuard)
                return "合同核心 · 抵消大额伤害";
            if (selectedContract == CargoContract.BlackBoxRelay &&
                (card == CardId.SignalScrambler || card == CardId.AirBrake))
                return "合同核心 · 控制航迹暴露";
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
                CardId.FrostLance => "造成9点伤害；出牌前热量不高于2时额外造成8点。",
                CardId.HeatCharge => "获得3点能量，同时增加4点热量。",
                CardId.MeltdownBurst => "对所有敌人造成3点加当前热量的伤害，然后清空热量。",
                CardId.Scattershot => "对所有敌人造成3点伤害。",
                CardId.MissileSwarm => "发射6枚飞弹，每枚对随机敌人造成2点伤害。",
                CardId.SignalScrambler => "清除全部航迹暴露，获得7点护盾。",
                CardId.CounterPursuit => "追踪最低耐久敌人造成9点伤害；每层航迹暴露额外造成8点，随后清除暴露。",
                CardId.AirBrake => "降低2层航迹暴露并获得8点护盾；若成功降低，获得1点能量。",
                CardId.InterceptMine => "对所有不同航道的敌人造成9点伤害。",
                _ => CardLibrary.Get(card).Rules
            };
        }

        private static string ModuleName(ModuleId module)
        {
            return module switch
            {
                ModuleId.VectorThruster => "矢量回流器",
                ModuleId.PrismBulkhead => "棱镜隔舱",
                ModuleId.CryoHeart => "零度炉心",
                ModuleId.ExecutionChip => "处决芯片",
                ModuleId.PrecisionMatrix => "精密矩阵",
                ModuleId.MomentumFlywheel => "动量飞轮",
                ModuleId.AegisCapacitor => "神盾电容",
                ModuleId.ZeroPointReactor => "零点反应堆",
                ModuleId.RedlineReactor => "红线反应堆",
                ModuleId.SwarmUplink => "蜂群上行链路",
                _ => "幽灵解码器"
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
            if (selectedRouteNodeId == 7 || selectedRouteNodeId == 12)
                return route.Get(selectedRouteNodeId).Title;
            return selectedContract switch
            {
                CargoContract.FragileMedicine => "失压医疗驳船",
                CargoContract.CryoSerum => "冻裂冷却塔",
                CargoContract.StormCore => "雷暴走私信标",
                _ => "失联侦察黑匣"
            };
        }

        private string EventDescription()
        {
            if (selectedRouteNodeId == 7)
                return "破碎舰体在雷云中漂流。稳定供能单元仍可回收，但深入残骸会暴露货舱坐标。";
            if (selectedRouteNodeId == 12)
                return "废弃观测站保存着磁暴鳐的放电记录，也有一条穿越高压云墙的危险捷径。";
            return selectedContract switch
            {
                CargoContract.FragileMedicine => "一艘医疗驳船在乱流中失压，求救信号与货舱坐标同时暴露。",
                CargoContract.CryoSerum => "废弃冷却塔仍有一枚低温核心，但外壳正在快速崩裂。",
                CargoContract.StormCore => "非法信标标出穿越雷暴的短路，同时广播一笔无人认领的高额邮资。",
                _ => "失联侦察机留下加密黑匣；敌方追踪波束正沿着广播信号逼近。"
            };
        }

        private CardId EventUpgradeCard()
        {
            return selectedContract switch
            {
                CargoContract.FragileMedicine => CardId.WindGuard,
                CargoContract.CryoSerum => CardId.EmergencyCoolant,
                CargoContract.StormCore => CardId.BankDown,
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
            DrawFittedLabel(new Rect(1120, 125, 250, 40), "UNSTABLE SIGNAL", neonSubtitleStyle, 10);

            CardId upgrade = EventUpgradeCard();
            string upgradeName = CardLibrary.Get(upgrade).Name;
            int safeRepair = selectedRouteNodeId == 7 ? 6 : selectedRouteNodeId == 12 ? 2 : 4;
            string safeTitle = selectedRouteNodeId == 7 ? "回收稳定单元" : selectedRouteNodeId == 12 ? "解析磁暴周期" : "执行合同协议";
            DrawEventChoice(new Rect(235, 295, 500, 310), safeTitle, "稳定收益",
                $"强化全部【{upgradeName}】并修复{safeRepair}点机体。\n货物完整度不会下降。", eventColor,
                () => ResolveRouteEvent(true));
            int riskCredits = EventRiskCredits();
            int riskHull = selectedRouteNodeId == 7 ? 5 : selectedRouteNodeId == 12 ? 2 : 3;
            DrawEventChoice(new Rect(865, 295, 500, 310), selectedRouteNodeId == 7 ? "深入残骸核心" : "强穿危险航路", "高风险收益",
                $"立即获得{riskCredits}枚邮票。\n机体损失{riskHull}点，货物完整度损失1格。", PostalRed,
                () => ResolveRouteEvent(false));

            if (eventResolved)
            {
                Rect result = new Rect(320, 650, 960, 70);
                DrawRect(result, new Color32(4, 13, 34, 246));
                DrawNeonFrame(result, eventColor, 3f);
                DrawFittedLabel(new Rect(result.x + 20, result.y + 7, result.width - 260, 56), eventResult, hudCenteredStyle, 9);
                DrawPixelButton(new Rect(result.x + result.width - 225, result.y + 9, 205, 52), "继续航行", eventColor, LeaveRouteEvent);
            }
            else
            {
                DrawFittedLabel(new Rect(360, 665, 880, 40),
                    "事件选择不可撤销，风险收益会立即写入本局状态。", hudCenteredStyle, 9);
            }
        }

        private void DrawEventChoice(Rect rect, string title, string badge, string description, Color color, Action action,
            string category = "EVENT")
        {
            bool enabled = category == "SERVICE" ? !restResolved : !eventResolved;
            bool hovered = enabled && rect.Contains(Event.current.mousePosition);
            DrawRect(new Rect(rect.x + 9, rect.y + 9, rect.width, rect.height), new Color32(2, 7, 22, 255));
            DrawRect(rect, new Color32(9, 20, 44, 255));
            DrawNeonFrame(rect, hovered ? Color.Lerp(color, Color.white, 0.2f) : color, hovered ? 4f : 2f);
            DrawRect(new Rect(rect.x, rect.y, rect.width, 66), new Color32(11, 31, 63, 255));
            DrawFittedLabel(new Rect(rect.x + 25, rect.y + 8, rect.width - 50, 50), title, neonSubtitleStyle, 12);
            DrawFittedLabel(new Rect(rect.x + 28, rect.y + 80, rect.width - 56, 30), $"{category} // {badge}", tinyStyle, 8);
            DrawRect(new Rect(rect.x + 28, rect.y + 118, rect.width - 56, 108), new Color32(3, 11, 31, 235));
            DrawFittedLabel(new Rect(rect.x + 46, rect.y + 127, rect.width - 92, 90), description, neonBodyStyle, 15);
            DrawPixelButton(new Rect(rect.x + 105, rect.y + 238, rect.width - 210, 52), "确认选择", color, action, enabled);
        }

        private void ResolveRouteEvent(bool safeChoice)
        {
            if (eventResolved)
                return;
            if (safeChoice)
            {
                CardId upgraded = EventUpgradeCard();
                runUpgrades.Add(upgraded);
                runUpgradeBranches[upgraded] = UpgradeBranch.Alpha;
                int repaired = selectedRouteNodeId == 7 ? 6 : selectedRouteNodeId == 12 ? 2 : 4;
                runHull = Mathf.Min(BattleState.MaxPlayerHealth, runHull + repaired);
                eventResult = $"协议完成：{CardLibrary.Get(upgraded).Name}+，机体修复{repaired}点";
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
                eventResult = $"危险穿越：+{gained}邮票，机体-{hullLoss}，货物完整度-1";
                dangerFlashUntil = Time.time + 0.55f;
                TriggerShake(14f, 0.46f);
                PlayLayeredSound(warningSound, 0.8f, 0.85f, impactSound, 0.7f, 0.62f);
                TriggerFullScreenImpact(1.25f, 0.7f, false);
            }
            eventResolved = true;
        }

        private void LeaveRouteEvent()
        {
            CompleteCurrentRouteNode();
            screen = ScreenMode.Map;
        }

        private int EventRiskCredits()
        {
            int routeBonus = selectedRouteNodeId == 7 ? 10 : selectedRouteNodeId == 12 ? 20 : 0;
            return routeBonus + (selectedContract switch
            {
                CargoContract.BlackBoxRelay => 70,
                CargoContract.StormCore => 65,
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

            CardId tuneCard = selectedRouteNodeId == 14 ? CardId.BurstFire : CardId.WindGuard;
            DrawEventChoice(new Rect(235, 295, 500, 310), "修复机体结构", "恢复 14 机体",
                "船坞完成结构焊接与引擎检修。\n不会改变牌组或货物状态。", restColor,
                () => ResolveRestStop(true), "SERVICE");
            DrawEventChoice(new Rect(865, 295, 500, 310), "校准甲板系统", $"强化 {CardLibrary.Get(tuneCard).Name}",
                $"强化全部【{CardLibrary.Get(tuneCard).Name}】。\n本次停靠不恢复机体。", NeonCyan,
                () => ResolveRestStop(false), "SERVICE");

            if (restResolved)
            {
                Rect result = new Rect(320, 650, 960, 70);
                DrawRect(result, new Color32(4, 13, 34, 246));
                DrawNeonFrame(result, restColor, 3f);
                DrawFittedLabel(new Rect(result.x + 20, result.y + 7, result.width - 260, 56), restResult, hudCenteredStyle, 9);
                DrawPixelButton(new Rect(result.x + result.width - 225, result.y + 9, 205, 52), "重新启航", restColor, LeaveRestStop);
            }
        }

        private void ResolveRestStop(bool repair)
        {
            if (restResolved)
                return;
            if (repair)
            {
                int before = runHull;
                runHull = Mathf.Min(BattleState.MaxPlayerHealth, runHull + 14);
                restResult = $"结构维护完成：机体恢复 {runHull - before} 点";
                PlayLayeredSound(shieldSound, 0.9f, 0.8f, rewardSound, 1.08f, 0.5f);
            }
            else
            {
                CardId tuneCard = selectedRouteNodeId == 14 ? CardId.BurstFire : CardId.WindGuard;
                runUpgrades.Add(tuneCard);
                runUpgradeBranches[tuneCard] = UpgradeBranch.Alpha;
                restResult = $"甲板校准完成：{CardLibrary.Get(tuneCard).Name}+ 全部强化";
                PlayLayeredSound(rewardSound, 1.08f, 0.85f, clickSound, 1.4f, 0.45f);
            }
            restResolved = true;
            TriggerFullScreenImpact(0.8f, 0.42f, false);
        }

        private void LeaveRestStop()
        {
            CompleteCurrentRouteNode();
            screen = ScreenMode.Map;
        }

        private void DrawShopScreen()
        {
            DrawRect(new Rect(70, 55, 1460, 790), new Color32(2, 7, 22, 255));
            DrawRect(new Rect(78, 63, 1444, 774), PanelNight);
            DrawNeonFrame(new Rect(78, 63, 1444, 774), NeonCyan, 3f);
            DrawFittedLabel(new Rect(130, 90, 740, 70), route.Get(selectedRouteNodeId).Title, neonTitleStyle, 26);
            DrawFittedLabel(new Rect(135, 155, 760, 42), "用邮票改装牌组，或修补经历战斗的机体。", neonBodyStyle, 12);
            DrawFittedLabel(new Rect(1180, 105, 260, 50), $"邮票  {credits}", neonSubtitleStyle, 12);

            CardId[] offers = selectedContract switch
            {
                CargoContract.CryoSerum => new[] { CardId.FrostLance, CardId.MeltdownBurst, CardId.CryoPump },
                CargoContract.StormCore => new[] { CardId.PursuitShot, CardId.MissileSwarm, CardId.VectorDash },
                CargoContract.BlackBoxRelay => new[] { CardId.CounterPursuit, CardId.InterceptMine, CardId.AirBrake },
                _ => new[] { CardId.AegisRam, CardId.RailPiercer, CardId.ReactivePlating }
            };
            int[] prices = selectedContract == CargoContract.StormCore ? new[] { 20, 20, 35 } : new[] { 25, 20, 35 };
            for (int i = 0; i < offers.Length; i++)
            {
                int offerIndex = i;
                bool available = !shopBought[i] && credits >= prices[i];
                string footer = shopBought[i] ? "已售出" : $"购买 · {prices[i]}邮票";
                DrawOfferCard(offers[i], new Rect(145 + i * 335, 245, 265, 315), footer, available, () =>
                {
                    credits -= prices[offerIndex];
                    runDeck.Add(offers[offerIndex]);
                    shopBought[offerIndex] = true;
                });
            }

            Rect repairPanel = new Rect(1160, 245, 270, 315);
            DrawRect(new Rect(repairPanel.x + 7, repairPanel.y + 7, repairPanel.width, repairPanel.height), Shadow);
            DrawRect(repairPanel, new Color32(76, 157, 147, 255));
            DrawRect(new Rect(repairPanel.x + 10, repairPanel.y + 10, repairPanel.width - 20, 198), Paper);
            GUI.Label(new Rect(repairPanel.x + 28, repairPanel.y + 38, repairPanel.width - 56, 44), "机体维修", subtitleStyle);
            GUI.Label(new Rect(repairPanel.x + 28, repairPanel.y + 98, repairPanel.width - 56, 75), "恢复12点机体耐久，最多不超过36点。", bodyStyle);
            string repairText = repairBought ? "已维修" : "维修 · 20邮票";
            DrawPixelButton(new Rect(repairPanel.x + 20, repairPanel.y + 230, repairPanel.width - 40, 62), repairText,
                new Color32(76, 157, 147, 255), () =>
                {
                    credits -= 20;
                    runHull = Mathf.Min(BattleState.MaxPlayerHealth, runHull + 12);
                    repairBought = true;
                }, !repairBought && credits >= 20 && runHull < BattleState.MaxPlayerHealth);

            DrawPixelButton(new Rect(590, 685, 420, 72), "离开补给站，继续航行", PostalRed, () =>
            {
                CompleteCurrentRouteNode();
                screen = ScreenMode.Map;
            });
            DrawRunHud(new Rect(1080, 670, 340, 110));
        }

        private void DrawRunComplete()
        {
            DrawRect(new Rect(270, 105, 1060, 685), new Color32(2, 7, 22, 255));
            DrawRect(new Rect(278, 113, 1044, 669), PanelNight);
            DrawNeonFrame(new Rect(278, 113, 1044, 669), NeonCyan, 3f);
            DrawRect(new Rect(278, 113, 1044, 12), NeonViolet);
            DrawFittedLabel(new Rect(390, 190, 820, 82), $"{CargoName(selectedContract)}送达", neonTitleStyle, 28);
            DrawFittedLabel(new Rect(395, 285, 810, 95),
                $"风车群岛的收货信标亮起。\n合同风险报酬额外增加了 {runContractBonus} 枚邮票。", neonBodyStyle, 12);
            GUI.Label(new Rect(420, 405, 380, 40), $"评级 {CargoGrade(runCargoIntegrity)} · {CargoStatus(runCargoIntegrity)}　{CargoPips(runCargoIntegrity)}", neonSubtitleStyle);
            GUI.Label(new Rect(820, 405, 380, 40), $"剩余机体　{runHull}/{BattleState.MaxPlayerHealth}", neonSubtitleStyle);
            GUI.Label(new Rect(420, 460, 380, 40), $"牌组 / 强化　{runDeck.Count} / {runUpgrades.Count}", neonSubtitleStyle);
            GUI.Label(new Rect(820, 460, 380, 40), $"邮票 / 模块　{credits} / {runModules.Count}", neonSubtitleStyle);
            GUI.Label(new Rect(420, 525, 380, 40), $"战斗回合　　{runTurns}", neonSubtitleStyle);
            GUI.Label(new Rect(820, 525, 380, 40), $"打出卡牌　{runCardsPlayed}", neonSubtitleStyle);
            GUI.Label(new Rect(420, 580, 380, 40), $"累计受伤　　{runDamageTaken}", neonSubtitleStyle);
            GUI.Label(new Rect(820, 580, 380, 40), $"过热次数　{runOverheats}", neonSubtitleStyle);
            DrawFittedLabel(new Rect(420, 625, 780, 40),
                $"灾变　打断 {runCalamityInterrupts} / 规避 {runCalamityEvades} / 命中 {runCalamityHits}　|　追踪命中 {runTrackingHits}", neonSubtitleStyle, 10);
            DrawPixelButton(new Rect(440, 700, 330, 72), "再次出发", PostalRed, StartNewRun, true, "ENTER");
            DrawPixelButton(new Rect(830, 700, 330, 72), "返回标题", Shadow, () => screen = ScreenMode.Title);
        }

        private void DrawRunHud(Rect rect)
        {
            DrawRect(new Rect(rect.x + 5, rect.y + 5, rect.width, rect.height), new Color32(2, 7, 22, 255));
            DrawRect(rect, new Color32(10, 27, 55, 245));
            DrawNeonFrame(rect, NeonCyan, 2f);
            DrawFittedLabel(new Rect(rect.x + 16, rect.y + 8, rect.width - 32, 22),
                $"机体 {runHull}/{BattleState.MaxPlayerHealth}　|　{CargoName(selectedContract)}", tinyStyle, 8);
            DrawFittedLabel(new Rect(rect.x + 16, rect.y + 34, rect.width - 32, 22), CargoStatusLine(runCargoIntegrity), tinyStyle, 8);
            DrawFittedLabel(new Rect(rect.x + 16, rect.y + 60, rect.width - 32, 22),
                $"邮票 {credits}　|　牌组 {runDeck.Count}　|　模块 {runModules.Count}", tinyStyle, 8);
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
            Color kindColor = RouteNodeColor(node.Kind);
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

            DrawRouteNodeIcon(node.Kind, center, completed ? new Color32(83, 220, 158, 255) : kindColor);
            string status = completed ? "DONE" : selected ? "SELECT" : available ? "OPEN" : missed ? "CLOSED" : "SCAN";
            DrawRect(new Rect(center.x - 38f, center.y + 36f, 76f, 16f), new Color32(4, 12, 30, 245));
            DrawFittedLabel(new Rect(center.x - 36f, center.y + 36f, 72f, 16f), status, tinyStyle, 8);
            DrawFittedLabel(new Rect(center.x - 78f, center.y + 55f, 156f, 22f),
                revealed ? node.Title : "信号未解析", hudCenteredStyle, 8);

            if (!available)
                return;
            bool hovered = hitRect.Contains(Event.current.mousePosition);
            if (hovered)
            {
                RegisterHover($"route-node-{node.Id}", $"选择航点 {node.Title}");
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

        private void DrawRouteNodeIcon(RouteNodeKind kind, Vector2 center, Color color)
        {
            if (kind == RouteNodeKind.Shop)
            {
                DrawRect(new Rect(center.x - 18, center.y - 12, 36, 26), color);
                DrawRect(new Rect(center.x - 23, center.y - 17, 46, 7), PostalRed);
                DrawRect(new Rect(center.x - 4, center.y + 1, 8, 13), Shadow);
                return;
            }

            if (kind == RouteNodeKind.Boss)
            {
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
            CompleteCurrentRouteNode();
            screen = ScreenMode.Map;
            rewardSelectionLocked = false;
            selectedRewardIndex = -1;
            selectedRewardName = null;
        }

        private void ContinueAfterVictory()
        {
            runHull = battle.PlayerHealth;
            lastFieldRepair = battle.Encounter == EncounterId.Boss ? 0 : battle.Encounter == EncounterId.Hunt ? 12 :
                battle.Encounter == EncounterId.Elite ? 10 : 6;
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

            screen = battle.Encounter == EncounterId.Boss ? ScreenMode.Complete : ScreenMode.Reward;
            if (screen == ScreenMode.Reward)
            {
                rewardEnteredAt = Time.time;
                rewardSelectionLocked = false;
                selectedRewardIndex = -1;
                selectedRewardName = null;
                PlaySound(rewardSound, 0.92f, 0.72f);
            }
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
                Color danger = new Color32(220, 56, 62, 125);
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
                EncounterId.Skirmish => "废弃风标",
                EncounterId.Elite => "雷暴封锁线",
                EncounterId.Hunt => "追迹者空域",
                _ => "磁暴鳐巢"
            };
            DrawRect(new Rect(34, 24, 1532, 88), new Color32(3, 8, 24, 255));
            DrawRect(new Rect(40, 30, 1518, 76), PanelNight);
            DrawNeonFrame(new Rect(40, 30, 1518, 76), NeonCyan, 2f);
            DrawRect(new Rect(40, 30, 7, 76), NeonViolet);
            DrawFittedLabel(new Rect(62, 38, 320, 34), $"{encounterName} // {battle.FormationName}", hudStyle, 12);
            DrawFittedLabel(new Rect(62, 73, 330, 22), CargoActionHint(selectedContract), tinyStyle, 8);

            DrawMeter(new Rect(400, 46, 255, 22), battle.PlayerHealth, BattleState.MaxPlayerHealth,
                new Color32(74, 172, 114, 255), $"机体  {battle.PlayerHealth}/{BattleState.MaxPlayerHealth}");
            DrawMeter(new Rect(690, 46, 230, 22), battle.Heat, battle.HeatLimit,
                battle.Heat >= battle.HeatLimit - 2 ? PostalRed : Gold, $"热量  {battle.Heat}/{battle.HeatLimit}");

            DrawResourcePips(new Rect(958, 42, 150, 34), battle.Energy, 3, NeonCyan, "能量");
            DrawResourcePips(new Rect(1125, 42, 130, 34), Mathf.Min(battle.Armor, 3), 3, NeonViolet, $"护盾 {battle.Armor}");

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
                GUI.Label(new Rect(76, y + 10, 130, 24), $"航道 {lane + 1}", tinyStyle);
            }

            foreach (EnemyState enemy in battle.Enemies)
            {
                if (enemy.Alive && enemy.Kind == EnemyKind.CalamityDrone)
                    DrawCalamityLaneTelegraph(enemy);
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
            GUI.Label(new Rect(165, playerPosition.y - 30, 260, 28), changingLane ? $"邮运-07  //  SHIFT {laneTransitionTo + 1}" : "邮运-07", hudCenteredStyle);
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
                if (enemy.Kind == EnemyKind.CalamityDrone || enemy.Kind == EnemyKind.StormManta)
                    DrawCalamityChargeCore(enemy, new Vector2(x, y));
                DrawEnemy(enemy, new Vector2(x, y));
                GUI.matrix = enemyMatrix;
                string intent = enemyChangingLane ? $"变轨至航道 {laneFx.ToLane + 1}" : battle.IntentFor(enemy);
                Color intentColor = IntentColor(intent);
                Rect intentRect = new Rect(x - 105, y - 58, 210, 25);
                DrawRect(intentRect, new Color32(9, 14, 34, 245));
                DrawNeonFrame(intentRect, intentColor, 2f);
                DrawFittedLabel(new Rect(intentRect.x + 5, intentRect.y + 2, 200, 21), intent, tinyStyle, 10);
                DrawFittedLabel(new Rect(x - 100, y + 42, 200, 24),
                    enemy.Kind == EnemyKind.StormManta
                        ? $"{enemy.Name} · PHASE {enemy.Phase}  //  {enemy.Health}/{enemy.MaxHealth}"
                        : $"{enemy.Name}  //  {enemy.Health}/{enemy.MaxHealth}", hudCenteredStyle, 8);
                DrawRect(new Rect(x - 64, y + 68, 128, 7), Shadow);
                DrawRect(new Rect(x - 62, y + 69, 124 * enemy.Health / enemy.MaxHealth, 5), PostalRed);
                if (enemy.MaxArmor > 0)
                {
                    DrawRect(new Rect(x - 64, y + 78, 128, 6), new Color32(23, 38, 63, 245));
                    DrawRect(new Rect(x - 62, y + 79, 124 * enemy.Armor / enemy.MaxArmor, 4), NeonCyan);
                    DrawFittedLabel(new Rect(x - 64, y + 83, 128, 15), $"装甲 {enemy.Armor}/{enemy.MaxArmor}", tinyStyle, 7);
                }
            }

            DrawCombatEffects();
            DrawEnemyAttackEffects();
            DrawEnemyDeathEffects();

            DrawRect(new Rect(61, 520, 1468, 27), new Color32(5, 13, 32, 235));
            DrawRect(new Rect(61, 520, 7, 27), NeonCyan);
            DrawFittedLabel(new Rect(75, 522, 1438, 23), battle.Log, tinyStyle, 8);
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
            GUI.Label(new Rect(58, 577, 885, 34), $"TURN {battle.Turn:00}   //   手牌 {battle.Hand.Count}   //   抽牌 {battle.DrawCount}   //   弃牌 {battle.DiscardCount}", hudStyle);
            Rect comboStrip = new Rect(965, 574, 170, 38);
            DrawRect(comboStrip, new Color32(7, 18, 43, 235));
            DrawNeonFrame(comboStrip, battle.EvasionExposure >= 2 ? PostalRed : battle.LockOn > 0 ? Gold : NeonCyan, 2f);
            DrawFittedLabel(comboStrip, $"锁定 {battle.LockOn} / 动量 {battle.Momentum} / 航迹 {battle.EvasionExposure}", hudCenteredStyle, 9);
            if (runModules.Count > 0)
            {
                Rect moduleStrip = new Rect(1145, 574, 210, 38);
                DrawRect(moduleStrip, new Color32(70, 43, 15, 235));
                DrawNeonFrame(moduleStrip, new Color32(255, 194, 58, 255), 2f);
                DrawFittedLabel(moduleStrip, $"MODULE // {ModuleName(runModules[0])}", hudCenteredStyle, 11);
            }
            const float cardWidth = 218f;
            const float gap = 18f;
            float handWidth = battle.Hand.Count * cardWidth + Mathf.Max(0, battle.Hand.Count - 1) * gap;
            float startX = Mathf.Max(45, (ReferenceWidth - handWidth) * 0.5f - 70f);

            for (int i = 0; i < battle.Hand.Count; i++)
                DrawCard(i, new Rect(startX + i * (cardWidth + gap), 620, cardWidth, 235));

            Rect endTurn = new Rect(1360, 705, 180, 72);
            DrawPixelButton(endTurn, "结束回合", Shadow, EndTurnWithFeedback,
                !battle.Victory && !battle.Defeat && Time.time >= battleInputLockUntil, "SPACE");
            GUI.Label(new Rect(1352, 790, 194, 42), "敌人将执行\n当前显示的意图", tinyStyle);
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
                else if (fx.Kind == EnemyKind.StormManta)
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

            float t = Mathf.Clamp01(elapsed / fullScreenFxDuration);
            float fade = 1f - t;
            float hitPulse = Mathf.Sin(Mathf.Clamp01((t - 0.12f) / 0.34f) * Mathf.PI);
            float power = fullScreenFxPower;

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

            float normalized = Mathf.Clamp01((Time.time - combatFxStart) / Mathf.Max(0.01f, combatFxDuration));
            float hitWindow = Mathf.Clamp01((normalized - 0.42f) / 0.24f);
            float pulse = Mathf.Sin(hitWindow * Mathf.PI) * Mathf.Clamp01((0.82f - normalized) / 0.2f);
            if (pulse <= 0f)
                return;

            DrawRect(new Rect(0, 0, ReferenceWidth, ReferenceHeight), new Color(0.68f, 0.94f, 1f, pulse * 0.16f * combatFxPower));
            DrawRect(new Rect(0, impactPoint.y - 42, ReferenceWidth, 8), new Color(0.25f, 1f, 1f, pulse * 0.34f));
            DrawRect(new Rect(0, impactPoint.y + 34, ReferenceWidth, 5), new Color(1f, 0.18f, 0.78f, pulse * 0.3f));
            DrawRect(new Rect(0, 0, 22, ReferenceHeight), new Color(1f, 0.2f, 0.68f, pulse * 0.38f));
            DrawRect(new Rect(ReferenceWidth - 22, 0, 22, ReferenceHeight), new Color(0.2f, 0.95f, 1f, pulse * 0.38f));
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

            if (enemy.Kind == EnemyKind.StormBalloon)
            {
                DrawRect(PixelRect(center, -34, -28, 68, 52, 1.15f), new Color32(109, 73, 151, 255));
                DrawRect(PixelRect(center, -44, -12, 88, 24, 1.15f), new Color32(141, 99, 177, 255));
                DrawRect(PixelRect(center, -16, 24, 32, 20, 1.15f), Shadow);
                DrawRect(PixelRect(center, -4, 44, 8, 16, 1.15f), Gold);
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

        private static void DrawCalamityChargeCore(EnemyState enemy, Vector2 center)
        {
            int threshold = enemy.Kind == EnemyKind.StormManta
                ? enemy.Phase == 1 ? BattleState.BossPhaseOneBreakDamage : BattleState.BossPhaseTwoBreakDamage
                : BattleState.CalamityBreakDamage;
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

        private static Color IntentColor(string intent)
        {
            if (intent.Contains("上升") || intent.Contains("下降"))
                return new Color32(49, 151, 174, 255);
            if (intent.Contains("风暴") || intent.Contains("磁暴") || intent.Contains("吞界"))
                return new Color32(123, 77, 166, 255);
            if (intent.Contains("封锁"))
                return new Color32(245, 142, 62, 255);
            if (intent.Contains("灾变"))
                return new Color32(202, 67, 210, 255);
            if (intent.Contains("失衡"))
                return new Color32(49, 190, 205, 255);
            if (intent.Contains("击落"))
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
