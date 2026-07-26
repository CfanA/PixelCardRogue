using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SkyCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SkyCourierEditor
{
    public static class ProjectBuilder
    {
        private const string ScenePath = "Assets/Scenes/Prototype.unity";

        public static void ConfigureProject()
        {
            Directory.CreateDirectory("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Sky Courier Game");
            root.AddComponent<SkyCourierGame>();
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.productName = "Sky Courier Prototype";
            PlayerSettings.companyName = "Portfolio Prototype";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.bundleVersion = "0.44.0";

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("SKY_COURIER_SETUP_COMPLETE");
        }

        [MenuItem("Tools/Sky Courier/Build Windows Prototype")]
        public static void BuildWindowsPrototype()
        {
            PlayerSettings.bundleVersion = "0.44.0";
            ValidateCoreRules();
            BalanceSimulator.RunSuite();
            string outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/SkyCourierPrototype"));
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, true);
            Directory.CreateDirectory(outputDirectory);
            string executablePath = Path.Combine(outputDirectory, "Sky Courier Prototype.exe");

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = executablePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Prototype build failed: {report.summary.result}");

            string burstDebugDirectory = Path.Combine(
                outputDirectory,
                Path.GetFileNameWithoutExtension(executablePath) + "_BurstDebugInformation_DoNotShip");
            if (Directory.Exists(burstDebugDirectory))
                Directory.Delete(burstDebugDirectory, true);

            string playtestReport = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Playtest_Report_v0.44.md"));
            if (File.Exists(playtestReport))
                File.Copy(playtestReport, Path.Combine(outputDirectory, "试玩与平衡报告.md"), true);
            File.WriteAllText(Path.Combine(outputDirectory, "试玩说明.txt"),
                "《云海邮差》v0.44.0\r\n\r\n运行：双击 Sky Courier Prototype.exe\r\n键鼠：合同机库点击两侧货舱、使用滚轮/方向键或1—4切换，点击“签署合同”或按Enter确认。点击航点与卡牌；Space结束回合；Esc暂停。\r\n合同：四份合同各有独立风险、战斗被动与专属机制牌；专属牌只会进入对应合同的奖励与商店。\r\n空域：高空疾风走廊、中层静电锋面、低空残骸潮拥有不同敌方编队池和奖励倾向；地图航点详情会在进入前说明。\r\n纪事：前半程事件会留下援助承诺或残骸债务；后续事件会识别此前选择并产生不同回访与终章。\r\n改装：进入后半段航线时必须焊入一项永久机体改装；本局不可拆除或跳过，后续战斗顶部会显示其规则。\r\n反制：后半程会出现读取护盾、手牌、热量和合同资源的敌机；意图会明确显示触发条件与结算结果。\r\n终局前哨：最后一列前会遭遇雷幕先导、磁针鳐卫或双频先遣队；它们提前教授两名终局首领的航道规则。\r\n航线情报：击破前哨会获得一项本局永久情报，并改变对应首领首轮锁定，地图、战斗顶部和结算页都会显示。\r\n双终局：北线通往雷幕龙脊，南线通往磁暴鳐巢；中央高风险入口可自由选择两个终局。\r\n首领：磁暴鳐要求远离锁定航道；雷幕云龙标出的青色航道则是唯一安全区。两者二阶段都会读取合同、机体改装与信标纪事。\r\n结局：两名首领分别结合信标纪事形成中立、盟约、敌对三种结局，共六种；已发现终局会计入邮政档案。\r\n手柄：左摇杆选择；A确认；B返回；Y结束回合；Menu暂停。\r\n语言：标题页或暂停菜单进入设置，可即时切换简体中文 / English；选择会保存在本机。\r\n存档：节点与战斗入口自动保存；战斗中退出会从该场入口重新开始。\r\n复盘：失败页会说明致命来源、下一次行动建议和档案增量；可保留合同并使用新种子快速再试。\r\n档案：标题页可查看长期配送履历、发现图鉴、六类终局收藏、荣誉签章和最近记录；所有内容只保存在本机。\r\n复现：暂停菜单、航线状态和结算页显示本局种子；本地诊断位于游戏存档目录的 Diagnostics 文件夹，不会上传。\r\n目标：穿越8段分支航线并保护合同货物。\r\n",
                new System.Text.UTF8Encoding(true));

            string musicLicense = Path.GetFullPath(Path.Combine(Application.dataPath, "Resources/Audio/BGM/License.txt"));
            if (File.Exists(musicLicense))
                File.Copy(musicLicense, Path.Combine(outputDirectory, "音乐授权说明.txt"), true);

            Debug.Log($"SKY_COURIER_BUILD_COMPLETE: {executablePath}");
        }

        [MenuItem("Tools/Sky Courier/Validate Core Rules")]
        public static void ValidateCoreRules()
        {
            LocalizationService.Initialize(GameLanguage.SimplifiedChinese);
            string[] sourceLocalizationKeys = Directory.GetFiles(
                    Path.GetFullPath(Path.Combine(Application.dataPath, "Scripts/Runtime")), "*.cs",
                    SearchOption.AllDirectories)
                .SelectMany(path => Regex.Matches(File.ReadAllText(path),
                        @"(?:\bL|\bLocalizationService\.Text)\(""([^""]+)""")
                    .Cast<Match>().Select(match => match.Groups[1].Value))
                .Distinct().ToArray();
            string[] dynamicLocalizationKeys = Enum.GetValues(typeof(CardId)).Cast<CardId>()
                .Select(card => $"card.{card}.name").ToArray();
            if (!LocalizationService.ValidateKeys(sourceLocalizationKeys.Concat(dynamicLocalizationKeys),
                    out string localizationError))
                throw new InvalidOperationException(localizationError);
            if (LocalizationService.Text("title.start", "开始配送") != "开始配送")
                throw new InvalidOperationException("Simplified Chinese localization lookup failed.");
            LocalizationService.SetLanguage(GameLanguage.English);
            if (LocalizationService.Text("title.start", "开始配送") != "START DELIVERY" ||
                CardLibrary.Get(CardId.SignalScrambler).Name != "SIGNAL SCRAMBLER")
                throw new InvalidOperationException("English localization lookup failed.");
            LocalizationService.SetLanguage(GameLanguage.SimplifiedChinese);

            var settingsProbe = new GameSettingsData
            {
                MusicVolume = 0.35f,
                SfxVolume = 0.65f,
                DisplayMode = (int)FullScreenMode.FullScreenWindow,
                ResolutionWidth = 1920,
                ResolutionHeight = 1080,
                VSync = false,
                FrameRate = 120,
                ShakeIntensity = 0.25f,
                FlashIntensity = 0f,
                Language = (int)GameLanguage.English
            };
            GameSettingsData restoredSettings =
                JsonUtility.FromJson<GameSettingsData>(JsonUtility.ToJson(settingsProbe));
            GameSettingsService.Sanitize(restoredSettings);
            if (restoredSettings.Version != GameSettingsService.CurrentVersion ||
                restoredSettings.ResolutionWidth != 1920 || restoredSettings.ResolutionHeight != 1080 ||
                restoredSettings.FrameRate != 120 ||
                restoredSettings.Language != (int)GameLanguage.English ||
                !Mathf.Approximately(restoredSettings.ShakeIntensity, 0.25f) ||
                !Mathf.Approximately(restoredSettings.FlashIntensity, 0f))
                throw new InvalidOperationException("Versioned game settings did not survive JSON round-trip.");
            var legacySettings = new GameSettingsData
            {
                Version = 1,
                Language = (int)GameLanguage.SimplifiedChinese
            };
            GameSettingsService.Sanitize(legacySettings);
            if (legacySettings.Version != GameSettingsService.CurrentVersion ||
                legacySettings.Language != (int)GameLanguage.SimplifiedChinese)
                throw new InvalidOperationException("Version 1 settings language migration failed.");

            var saveProbe = new RunSaveData
            {
                RunSeed = 1357911,
                EncounterSeed = 2468022,
                Screen = "Map",
                Contract = (int)CargoContract.BlackBoxRelay,
                AirframeModification = (int)AirframeModification.OpenAvionics,
                RouteStoryState = (int)RouteStoryState.PromiseStrengthened,
                RouteIntel = (int)RouteIntel.DualChannelDecoder,
                Deck = new System.Collections.Generic.List<int> { (int)CardId.BurstFire, (int)CardId.SignalScrambler },
                SelectedRouteNodeId = 4,
                Hull = 27,
                CargoIntegrity = 2,
                ShopBought = new[] { true, false, true }
            };
            RunSaveData restoredSave = JsonUtility.FromJson<RunSaveData>(JsonUtility.ToJson(saveProbe));
            if (restoredSave == null || restoredSave.Version != RunSaveService.CurrentVersion ||
                restoredSave.Deck.Count != 2 || restoredSave.SelectedRouteNodeId != 4 ||
                restoredSave.Hull != 27 || restoredSave.RunSeed != 1357911 ||
                restoredSave.EncounterSeed != 2468022 ||
                restoredSave.AirframeModification != (int)AirframeModification.OpenAvionics ||
                restoredSave.RouteStoryState != (int)RouteStoryState.PromiseStrengthened ||
                restoredSave.RouteIntel != (int)RouteIntel.DualChannelDecoder ||
                !restoredSave.ShopBought[0] || !restoredSave.ShopBought[2])
                throw new InvalidOperationException("Versioned run save did not survive JSON round-trip.");
            string saveValidationDirectory = Path.Combine(Path.GetTempPath(), $"SkyCourierSaveValidation-{Guid.NewGuid():N}");
            try
            {
                saveProbe.Credits = 11;
                RunSaveService.SaveToDirectory(saveProbe, saveValidationDirectory);
                saveProbe.Credits = 22;
                RunSaveService.SaveToDirectory(saveProbe, saveValidationDirectory);
                File.WriteAllText(Path.Combine(saveValidationDirectory, "run.json"), "{corrupted");
                if (!RunSaveService.TryLoadFromDirectory(saveValidationDirectory, out RunSaveData backupSave,
                        out bool restoredBackup, out string saveError) ||
                    !restoredBackup || backupSave.Credits != 11)
                    throw new InvalidOperationException($"Run save backup recovery failed: {saveError}");

                var legacySave = new RunSaveData
                {
                    Version = 1,
                    Screen = "Map",
                    Deck = new System.Collections.Generic.List<int> { (int)CardId.BurstFire },
                    SelectedRouteNodeId = 0
                };
                File.WriteAllText(Path.Combine(saveValidationDirectory, "run.json"),
                    JsonUtility.ToJson(legacySave));
                if (!RunSaveService.TryLoadFromDirectory(saveValidationDirectory, out RunSaveData migratedSave,
                        out _, out string migrationError) ||
                    migratedSave.Version != RunSaveService.CurrentVersion ||
                    migratedSave.RunSeed != RunSeedUtility.LegacySeed ||
                    migratedSave.EncounterSeed != RunSeedUtility.LegacySeed ||
                    migratedSave.AirframeModification != (int)AirframeModification.None ||
                    migratedSave.RouteStoryState != (int)RouteStoryState.None)
                    throw new InvalidOperationException($"Version 1 run save migration failed: {migrationError}");

                var versionTwoSave = new RunSaveData
                {
                    Version = 2,
                    RunSeed = 112233,
                    EncounterSeed = 445566,
                    Screen = "Map",
                    Deck = new System.Collections.Generic.List<int> { (int)CardId.WindGuard },
                    SelectedRouteNodeId = 10
                };
                File.WriteAllText(Path.Combine(saveValidationDirectory, "run.json"),
                    JsonUtility.ToJson(versionTwoSave));
                if (!RunSaveService.TryLoadFromDirectory(saveValidationDirectory, out RunSaveData migratedVersionTwo,
                        out _, out string versionTwoError) ||
                    migratedVersionTwo.Version != RunSaveService.CurrentVersion ||
                    migratedVersionTwo.AirframeModification != (int)AirframeModification.None ||
                    migratedVersionTwo.RouteStoryState != (int)RouteStoryState.None)
                    throw new InvalidOperationException($"Version 2 run save migration failed: {versionTwoError}");

                var versionThreeSave = new RunSaveData
                {
                    Version = 3,
                    RunSeed = 778899,
                    EncounterSeed = 998877,
                    Screen = "Map",
                    Deck = new System.Collections.Generic.List<int> { (int)CardId.BankUp },
                    SelectedRouteNodeId = 12,
                    AirframeModification = (int)AirframeModification.SealedBulkhead
                };
                File.WriteAllText(Path.Combine(saveValidationDirectory, "run.json"),
                    JsonUtility.ToJson(versionThreeSave));
                if (!RunSaveService.TryLoadFromDirectory(saveValidationDirectory, out RunSaveData migratedVersionThree,
                        out _, out string versionThreeError) ||
                    migratedVersionThree.Version != RunSaveService.CurrentVersion ||
                    migratedVersionThree.AirframeModification != (int)AirframeModification.SealedBulkhead ||
                    migratedVersionThree.RouteStoryState != (int)RouteStoryState.None)
                    throw new InvalidOperationException($"Version 3 run save migration failed: {versionThreeError}");

                var versionFourSave = new RunSaveData
                {
                    Version = 4,
                    RunSeed = 440044,
                    EncounterSeed = 440045,
                    Screen = "Map",
                    Deck = new System.Collections.Generic.List<int> { (int)CardId.BankDown },
                    SelectedRouteNodeId = 15,
                    RouteStoryState = (int)RouteStoryState.PromiseFulfilled
                };
                File.WriteAllText(Path.Combine(saveValidationDirectory, "run.json"),
                    JsonUtility.ToJson(versionFourSave));
                if (!RunSaveService.TryLoadFromDirectory(saveValidationDirectory, out RunSaveData migratedVersionFour,
                        out _, out string versionFourError) ||
                    migratedVersionFour.Version != RunSaveService.CurrentVersion ||
                    migratedVersionFour.RouteIntel != (int)RouteIntel.None)
                    throw new InvalidOperationException($"Version 4 run save migration failed: {versionFourError}");

                var diagnosticProbe = new RunDiagnosticRecord
                {
                    TimestampUtc = DateTime.UtcNow.ToString("O"),
                    Event = "validation_probe",
                    GameVersion = "0.44.0",
                    RunSeed = 1357911,
                    Screen = "Map"
                };
                RunDiagnosticsService.WriteRecordToDirectory(saveValidationDirectory, diagnosticProbe,
                    "diagnostic-probe.jsonl", false);
                string diagnosticJson = File.ReadAllText(Path.Combine(saveValidationDirectory,
                    "diagnostic-probe.jsonl")).Trim();
                RunDiagnosticRecord restoredDiagnostic = JsonUtility.FromJson<RunDiagnosticRecord>(diagnosticJson);
                if (restoredDiagnostic == null || restoredDiagnostic.Event != "validation_probe" ||
                    restoredDiagnostic.RunSeed != 1357911)
                    throw new InvalidOperationException("Local diagnostic record did not survive JSONL round-trip.");

                var archiveProbe = new DeliveryArchiveData();
                DeliveryArchiveService.RegisterRunStarted(archiveProbe, (int)CargoContract.StormCore,
                    new[] { (int)CardId.BurstFire, (int)CardId.BurstFire, (int)CardId.VectorDash });
                DeliveryArchiveService.RegisterBattleStarted(archiveProbe,
                    new[] { (int)EnemyKind.RustKite, (int)EnemyKind.CalamityDrone },
                    new[] { (int)CardId.BurstFire, (int)CardId.VectorDash },
                    new[] { (int)ModuleId.VectorThruster });
                DeliveryArchiveService.RegisterBattleWon(archiveProbe);
                DeliveryArchiveService.RegisterRunResult(archiveProbe, new ArchivedRunRecord
                {
                    RunSeed = 1357911,
                    Contract = (int)CargoContract.StormCore,
                    RouteNodeId = 18,
                    Encounter = (int)EncounterId.Boss,
                    CargoIntegrity = 3,
                    Hull = 19,
                    Credits = 84,
                    Turns = 31,
                    CardsPlayed = 67,
                    DeckCount = 16,
                    ModuleCount = 2,
                    RouteIntel = (int)RouteIntel.FluxCompass,
                    FinaleEnding = (int)FinaleEnding.MantaPostalShield
                }, true);
                DeliveryArchiveService.RegisterRunResult(archiveProbe, new ArchivedRunRecord
                {
                    RunSeed = 2468022,
                    Contract = (int)CargoContract.BlackBoxRelay,
                    RouteNodeId = 7,
                    Encounter = (int)EncounterId.Hunt,
                    CargoIntegrity = 1,
                    Hull = 0,
                    Credits = 36,
                    Turns = 18,
                    CardsPlayed = 29,
                    DeckCount = 14,
                    ModuleCount = 1,
                    DefeatSource = (int)PlayerDamageSource.TrackingShot,
                    DefeatDealer = "追迹锈翼鸢"
                }, false);
                if (archiveProbe.RunsStarted != 1 || archiveProbe.DeliveriesCompleted != 1 ||
                    archiveProbe.EncountersLost != 1 ||
                    archiveProbe.BattlesWon != 1 || archiveProbe.DiscoveredCards.Count != 2 ||
                    archiveProbe.DiscoveredEnemies.Count != 2 || archiveProbe.RecentRuns.Count != 2 ||
                    archiveProbe.DiscoveredEndings.Single() != (int)FinaleEnding.MantaPostalShield ||
                    archiveProbe.RecentRuns[0].DefeatSource != (int)PlayerDamageSource.TrackingShot ||
                    DeliveryArchiveService.CourierRank(archiveProbe) != "正式邮差")
                    throw new InvalidOperationException("Delivery archive progression aggregation is invalid.");

                DeliveryArchiveService.SaveToDirectory(archiveProbe, saveValidationDirectory);
                archiveProbe.BestCredits = 120;
                DeliveryArchiveService.SaveToDirectory(archiveProbe, saveValidationDirectory);
                File.WriteAllText(Path.Combine(saveValidationDirectory, "archive.json"), "{corrupted");
                DeliveryArchiveData restoredArchive = DeliveryArchiveService.LoadFromDirectory(
                    saveValidationDirectory, out bool archiveRestoredBackup, out string archiveError);
                if (!archiveRestoredBackup || restoredArchive.DeliveriesCompleted != 1 ||
                    restoredArchive.BestCredits != 84 || restoredArchive.RecentRuns[0].RunSeed != 2468022 ||
                    restoredArchive.RecentRuns[0].DefeatSource != (int)PlayerDamageSource.TrackingShot ||
                    restoredArchive.RecentRuns[0].DefeatDealer != "追迹锈翼鸢")
                    throw new InvalidOperationException($"Delivery archive backup recovery failed: {archiveError}");

                var legacyArchive = new DeliveryArchiveData
                {
                    Version = 1,
                    RecentRuns = new System.Collections.Generic.List<ArchivedRunRecord>
                    {
                        new ArchivedRunRecord
                        {
                            Outcome = "LOST",
                            RunSeed = 70422,
                            DefeatSource = (int)PlayerDamageSource.BossStrike
                        }
                    }
                };
                File.WriteAllText(Path.Combine(saveValidationDirectory, "archive.json"),
                    JsonUtility.ToJson(legacyArchive));
                DeliveryArchiveData migratedArchive = DeliveryArchiveService.LoadFromDirectory(
                    saveValidationDirectory, out _, out string archiveMigrationError);
                if (migratedArchive.Version != DeliveryArchiveService.CurrentVersion ||
                    migratedArchive.RecentRuns.Count != 1 || migratedArchive.RecentRuns[0].DefeatSource != -1)
                    throw new InvalidOperationException($"Version 1 delivery archive migration failed: {archiveMigrationError}");
            }
            finally
            {
                if (Directory.Exists(saveValidationDirectory))
                    Directory.Delete(saveValidationDirectory, true);
            }

            RouteDefinition route = RouteCatalog.WindmillArchipelago;
            if (route.ColumnCount != 8 || route.Nodes.Count != 20)
                throw new InvalidOperationException("Branching route must contain 8 columns and 20 nodes.");
            if (route.AtColumn(0).Count() != 1 || route.AtColumn(route.ColumnCount - 1).Count() != 2 ||
                route.AtColumn(route.ColumnCount - 1).Any(node => node.Kind != RouteNodeKind.Boss))
                throw new InvalidOperationException("Branching route must have one start and two finale boss nodes.");
            if (Enumerable.Range(1, route.ColumnCount - 2).Any(column => route.AtColumn(column).Count() < 2))
                throw new InvalidOperationException("Every intermediate route column must offer a branch.");
            var reachable = new System.Collections.Generic.HashSet<int>();
            var frontier = new System.Collections.Generic.Queue<int>();
            frontier.Enqueue(route.AtColumn(0).Single().Id);
            while (frontier.Count > 0)
            {
                int nodeId = frontier.Dequeue();
                if (!reachable.Add(nodeId))
                    continue;
                foreach (int nextId in route.Get(nodeId).Next)
                    frontier.Enqueue(nextId);
            }
            if (reachable.Count != route.Nodes.Count ||
                !route.Nodes.Any(node => node.Kind == RouteNodeKind.Shop) ||
                !route.Nodes.Any(node => node.Kind == RouteNodeKind.Event) ||
                !route.Nodes.Any(node => node.Kind == RouteNodeKind.Rest))
                throw new InvalidOperationException("Every route node and utility type must be reachable.");
            if (!route.Get(16).Next.Contains(18) || !route.Get(16).Next.Contains(19))
                throw new InvalidOperationException("The high-risk penultimate route must offer both finales.");
            if (route.Nodes.Any(node => node.Airspace != (node.Lane switch
                {
                    0 => AirspaceCondition.JetstreamCorridor,
                    1 => AirspaceCondition.StaticFront,
                    _ => AirspaceCondition.WreckageTide
                })) ||
                route.Nodes.Select(node => node.Airspace).Distinct().Count() != 3)
                throw new InvalidOperationException("Route altitude bands did not resolve to three airspace identities.");
            int airspaceProbeSeed = 40040;
            int jetstreamVariant = AirspaceRuleCatalog.EncounterVariant(
                AirspaceCondition.JetstreamCorridor, airspaceProbeSeed);
            int staticVariant = AirspaceRuleCatalog.EncounterVariant(
                AirspaceCondition.StaticFront, airspaceProbeSeed);
            int wreckageVariant = AirspaceRuleCatalog.EncounterVariant(
                AirspaceCondition.WreckageTide, airspaceProbeSeed);
            if (jetstreamVariant != 0 || staticVariant != 1 || wreckageVariant != 2 ||
                AirspaceRuleCatalog.EncounterVariant(AirspaceCondition.StaticFront, airspaceProbeSeed) != staticVariant)
                throw new InvalidOperationException("Airspace encounter pools are not distinct and deterministic.");

            RouteStoryState promise = RouteStoryRules.Begin(true);
            RouteStoryState strengthened = RouteStoryRules.ContinueAtWreckage(promise, true);
            if (promise != RouteStoryState.BeaconPromise ||
                strengthened != RouteStoryState.PromiseStrengthened ||
                RouteStoryRules.ResolveAtObservatory(strengthened, true) != RouteStoryState.PromiseFulfilled ||
                RouteStoryRules.ResolveAtObservatory(strengthened, false) != RouteStoryState.PromiseBetrayed)
                throw new InvalidOperationException("Beacon promise story path did not retain its choices.");

            RouteStoryState debt = RouteStoryRules.Begin(false);
            RouteStoryState deepDebt = RouteStoryRules.ContinueAtWreckage(debt, false);
            if (debt != RouteStoryState.SalvageDebt ||
                deepDebt != RouteStoryState.DebtDeepened ||
                RouteStoryRules.ResolveAtObservatory(deepDebt, true) != RouteStoryState.DebtRepaid ||
                RouteStoryRules.ResolveAtObservatory(deepDebt, false) != RouteStoryState.DebtDefied ||
                RouteStoryRules.ContinueAtWreckage(debt, true) != RouteStoryState.BeaconPromise ||
                RouteStoryRules.IsPending(RouteStoryState.PromiseFulfilled))
                throw new InvalidOperationException("Salvage debt story path did not support escalation and redemption.");

            var deck = new[]
            {
                CardId.BurstFire, CardId.BurstFire, CardId.BankUp, CardId.BankDown,
                CardId.WindGuard, CardId.EmergencyCoolant, CardId.BroadsideVolley,
                CardId.OverloadAim, CardId.EngineOverclock
            };
            var state = new BattleState();

            var overheatDefeat = new BattleState();
            overheatDefeat.StartEncounter(EncounterId.Skirmish,
                Enumerable.Repeat(CardId.HeatCharge, 8).ToArray(), 4, 3,
                CargoContract.FragileMedicine, null, null, 0, null, 31001);
            overheatDefeat.PlayCard(0);
            overheatDefeat.PlayCard(0);
            overheatDefeat.PlayCard(0);
            if (!overheatDefeat.Defeat || !overheatDefeat.HasDefeatCause ||
                overheatDefeat.DefeatSource != PlayerDamageSource.Overheat ||
                overheatDefeat.DefeatDealer != "引擎过热")
                throw new InvalidOperationException("Fatal overheat did not retain a structured defeat cause.");

            var laneDefeat = new BattleState();
            laneDefeat.StartEncounter(EncounterId.Skirmish,
                Enumerable.Repeat(CardId.WindGuard, 8).ToArray(), 4, 3,
                CargoContract.FragileMedicine, null, null, 0, null, 31002);
            laneDefeat.EndTurn();
            if (!laneDefeat.Defeat || laneDefeat.DefeatSource != PlayerDamageSource.LaneBlock ||
                !laneDefeat.DefeatDealer.Contains("噬邮兽"))
                throw new InvalidOperationException("Fatal lane block did not retain its enemy source.");

            var calamityDefeat = new BattleState();
            calamityDefeat.StartEncounter(EncounterId.Skirmish,
                Enumerable.Repeat(CardId.WindGuard, 8).ToArray(), 4, 3,
                CargoContract.FragileMedicine, null, null, 1, null, 31003);
            calamityDefeat.EndTurn();
            if (!calamityDefeat.Defeat ||
                calamityDefeat.DefeatSource != PlayerDamageSource.CalamityStrike ||
                !calamityDefeat.DefeatDealer.Contains("灾变无人机"))
                throw new InvalidOperationException("Fatal calamity strike did not retain its telegraphed source.");

            int encounterSeed = RunSeedUtility.DeriveEncounterSeed(1357911, 4, EncounterId.Skirmish);
            if (encounterSeed != RunSeedUtility.DeriveEncounterSeed(1357911, 4, EncounterId.Skirmish) ||
                encounterSeed == RunSeedUtility.DeriveEncounterSeed(1357911, 5, EncounterId.Skirmish))
                throw new InvalidOperationException("Encounter seed derivation is not stable or does not include route identity.");
            var deterministicA = new BattleState();
            var deterministicB = new BattleState();
            deterministicA.StartEncounter(EncounterId.Skirmish, deck, BattleState.MaxPlayerHealth, 3,
                CargoContract.FragileMedicine, null, null, -1, null, encounterSeed);
            deterministicB.StartEncounter(EncounterId.Skirmish, deck, BattleState.MaxPlayerHealth, 3,
                CargoContract.FragileMedicine, null, null, -1, null, encounterSeed);
            string deterministicSignatureA = $"{deterministicA.EncounterVariant}|{string.Join(",", deterministicA.Hand)}|" +
                string.Join(";", deterministicA.Enemies.Select(enemy =>
                    $"{enemy.Kind}:{enemy.Lane}:{enemy.Health}:{enemy.Armor}"));
            string deterministicSignatureB = $"{deterministicB.EncounterVariant}|{string.Join(",", deterministicB.Hand)}|" +
                string.Join(";", deterministicB.Enemies.Select(enemy =>
                    $"{enemy.Kind}:{enemy.Lane}:{enemy.Health}:{enemy.Armor}"));
            if (deterministicA.Seed != encounterSeed || deterministicSignatureA != deterministicSignatureB)
                throw new InvalidOperationException("Matching encounter seeds did not reproduce the opening battle state.");

            state.StartEncounter(EncounterId.Skirmish, deck, BattleState.MaxPlayerHealth, 3);
            if (state.Enemies.Count != 2 || state.CardsPlayed != 0 || state.DamageTaken != 0)
                throw new InvalidOperationException("Skirmish configuration or metrics reset is invalid.");
            int playableIndex = Enumerable.Range(0, state.Hand.Count).FirstOrDefault(state.CanPlay);
            state.PlayCard(playableIndex);
            if (state.CardsPlayed != 1)
                throw new InvalidOperationException("Card play metric did not increment.");

            state.StartEncounter(EncounterId.Elite, deck, BattleState.MaxPlayerHealth, 3);
            if (state.Enemies.Count != 3 || state.Enemies.Sum(enemy => enemy.MaxHealth) != 52)
                throw new InvalidOperationException("Elite balance budget is not 52 total health.");

            state.StartEncounter(EncounterId.Boss, deck, BattleState.MaxPlayerHealth, 3);
            if (state.Enemies.Count != 1 || state.Enemies[0].MaxHealth != 50)
                throw new InvalidOperationException("Boss balance budget is not 50 health.");

            var passiveDeck = Enumerable.Repeat(CardId.WindGuard, 8).ToArray();
            state.StartEncounter(EncounterId.Skirmish, passiveDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.FragileMedicine, null, null, 1);
            EnemyState calamity = state.Enemies.Single(enemy => enemy.Kind == EnemyKind.CalamityDrone);
            if (state.EncounterVariant != 1 || state.FormationName != "灾变猎杀编队" || calamity.ChargeTargetLane != 1 ||
                !state.IntentFor(calamity).Contains($"打断 0/{BattleState.CalamityBreakDamage}"))
                throw new InvalidOperationException("Calamity encounter did not telegraph its lane and break threshold.");
            state.EndTurn();
            if (state.PlayerHealth != BattleState.MaxPlayerHealth - BattleState.CalamityStrikeDamage ||
                state.CargoIntegrity != 2 || state.CalamityHits != 1)
                throw new InvalidOperationException("Unanswered calamity strike did not hit the marked lane.");

            var evadeDeck = Enumerable.Repeat(CardId.BankDown, 8).ToArray();
            state.StartEncounter(EncounterId.Skirmish, evadeDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.FragileMedicine, null, null, 1);
            state.PlayCard(0);
            state.EndTurn();
            if (state.PlayerHealth != BattleState.MaxPlayerHealth || state.CargoIntegrity != 3 || state.CalamityEvades != 1)
                throw new InvalidOperationException("Changing lanes did not evade the marked calamity strike.");

            var breakDeck = Enumerable.Repeat(CardId.RailPiercer, 8).ToArray();
            state.StartEncounter(EncounterId.Skirmish, breakDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.FragileMedicine, null, null, 1);
            calamity = state.Enemies.Single(enemy => enemy.Kind == EnemyKind.CalamityDrone);
            state.PlayCard(0);
            if (!calamity.ChargeInterrupted || !state.IntentFor(calamity).Contains("系统失衡"))
                throw new InvalidOperationException("Rail damage did not interrupt the calamity charge.");
            state.EndTurn();
            if (state.PlayerHealth != BattleState.MaxPlayerHealth || state.CalamityInterrupts != 1 || calamity.ChargeInterrupted ||
                calamity.ChargeDamageTaken != 0 || calamity.ChargeTargetLane != -1 ||
                !state.IntentFor(calamity).Contains("重新校准"))
                throw new InvalidOperationException("Interrupted calamity charge did not skip and reset cleanly.");
            state.EndTurn();
            if (state.CalamityHits != 0 || calamity.ChargeTargetLane != state.PlayerLane)
                throw new InvalidOperationException("Calamity drone did not spend a full turn recalibrating.");

            EncounterDefinition eliteAnomaly = EncounterCatalog.Get(EncounterId.Elite, 1);
            if (eliteAnomaly.Enemies.Length != 3 || eliteAnomaly.Enemies.Sum(enemy => enemy.Health) != 52 ||
                eliteAnomaly.FormationName != "灾变封锁编队")
                throw new InvalidOperationException("Encounter catalog did not preserve the elite balance budget.");

            EncounterDefinition hunt = EncounterCatalog.Get(EncounterId.Hunt, 0);
            EncounterDefinition huntAnomaly = EncounterCatalog.Get(EncounterId.Hunt, 1);
            if (hunt.Enemies.Sum(enemy => enemy.Health) != 40 || huntAnomaly.Enemies.Sum(enemy => enemy.Health) != 38)
                throw new InvalidOperationException("Hunt encounter variants do not match their balance budgets.");

            var kiteDeck = Enumerable.Repeat(CardId.VectorDash, 8).ToArray();
            state.StartEncounter(EncounterId.Hunt, kiteDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.FragileMedicine, null, null, 0);
            state.PlayCard(0);
            state.EndTurn();
            if (state.EvasionExposure != 1 || state.TrackingHits != 0)
                throw new InvalidOperationException("The first evasive turn should expose the route without triggering tracking fire.");
            state.PlayCard(0);
            if (!state.Enemies.Any(enemy => state.IntentFor(enemy).Contains("追踪")))
                throw new InvalidOperationException("A second consecutive maneuver did not telegraph tracking fire.");
            int healthBeforeTracking = state.PlayerHealth;
            int armorBeforeTracking = state.Armor;
            state.EndTurn();
            if (state.EvasionExposure != 2 || state.TrackingHits != 1 ||
                state.PlayerHealth != healthBeforeTracking - Math.Max(0, BattleState.TrackingShotDamage - armorBeforeTracking))
                throw new InvalidOperationException($"Repeated lane kiting mismatch: exposure={state.EvasionExposure}, " +
                    $"tracking={state.TrackingHits}, health={state.PlayerHealth}, before={healthBeforeTracking}, armor={armorBeforeTracking}.");

            state.StartEncounter(EncounterId.Hunt, kiteDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.BlackBoxRelay, null, null, 0);
            state.PlayCard(0);
            state.EndTurn();
            state.PlayCard(0);
            state.EndTurn();
            if (state.CargoIntegrity != 2 || !state.LastCargoDamageReason.Contains("航迹暴露"))
                throw new InvalidOperationException("Black box contract did not react to repeated lane exposure.");

            var counterDeck = new[]
            {
                CardId.SignalScrambler, CardId.CounterPursuit, CardId.AirBrake, CardId.VectorDash,
                CardId.WindGuard
            };
            state.StartEncounter(EncounterId.Hunt, counterDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.BlackBoxRelay, null, null, 0);
            state.PlayCard(state.Hand.IndexOf(CardId.VectorDash));
            state.EndTurn();
            int scrambler = state.Hand.IndexOf(CardId.SignalScrambler);
            if (scrambler >= 0)
                state.PlayCard(scrambler);
            if (state.EvasionExposure != 0)
                throw new InvalidOperationException("Signal scrambler did not clear evasion exposure.");

            var hotDeck = Enumerable.Repeat(CardId.EngineOverclock, 8).ToArray();
            state.StartEncounter(EncounterId.Skirmish, hotDeck, BattleState.MaxPlayerHealth, 3, CargoContract.CryoSerum);
            state.PlayCard(0);
            state.PlayCard(0);
            state.PlayCard(0);
            state.EndTurn();
            if (state.CargoIntegrity != 2 || !state.LastCargoDamageReason.Contains("热量"))
                throw new InvalidOperationException("Cryo serum contract risk did not trigger at 6 heat.");

            var safeDeck = Enumerable.Repeat(CardId.WindGuard, 8).ToArray();
            state.StartEncounter(EncounterId.Skirmish, safeDeck, BattleState.MaxPlayerHealth, 3, CargoContract.StormCore);
            state.EndTurn();
            state.EndTurn();
            if (state.CargoIntegrity != 2 || !state.LastCargoDamageReason.Contains("两回合"))
                throw new InvalidOperationException("Storm core inactivity risk did not trigger after two turns.");

            var medicinePassiveDeck = Enumerable.Repeat(CardId.WindGuard, 5).ToArray();
            var medicinePassive = new BattleState();
            medicinePassive.StartEncounter(EncounterId.Skirmish, medicinePassiveDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.FragileMedicine, null, null, 0, null, 32001);
            medicinePassive.PlayCard(0);
            medicinePassive.PlayCard(0);
            medicinePassive.EndTurn();
            if (!medicinePassive.ContractPassiveTriggered || medicinePassive.ContractPassiveProcs != 1 ||
                medicinePassive.LockOn != 1 || medicinePassive.PlayerHealth != BattleState.MaxPlayerHealth)
                throw new InvalidOperationException("Fragile medicine passive did not reward a fully blocked enemy hit.");

            var cryoPassiveDeck = new[]
            {
                CardId.EngineOverclock, CardId.EngineOverclock, CardId.EmergencyCoolant,
                CardId.WindGuard, CardId.WindGuard
            };
            var cryoPassive = new BattleState();
            cryoPassive.StartEncounter(EncounterId.Skirmish, cryoPassiveDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.CryoSerum, null, null, 0, null, 32002);
            cryoPassive.PlayCard(cryoPassive.Hand.IndexOf(CardId.EngineOverclock));
            cryoPassive.PlayCard(cryoPassive.Hand.IndexOf(CardId.EngineOverclock));
            int energyBeforeCryoPassive = cryoPassive.Energy;
            cryoPassive.PlayCard(cryoPassive.Hand.IndexOf(CardId.EmergencyCoolant));
            if (!cryoPassive.ContractPassiveTriggered || cryoPassive.ContractPassiveProcs != 1 ||
                cryoPassive.Energy != energyBeforeCryoPassive + 1)
                throw new InvalidOperationException("Cryo serum passive did not convert a 3+ Heat card cooldown into Energy.");

            var stormPassiveDeck = Enumerable.Repeat(CardId.BankDown, 5).ToArray();
            var stormPassive = new BattleState();
            stormPassive.StartEncounter(EncounterId.Skirmish, stormPassiveDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.StormCore, null, null, 0, null, 32003);
            stormPassive.PlayCard(0);
            if (!stormPassive.ContractPassiveTriggered || stormPassive.ContractPassiveProcs != 1 ||
                stormPassive.Momentum != 2)
                throw new InvalidOperationException("Storm core passive did not amplify the first maneuver.");

            var relayPassiveDeck = new[]
            {
                CardId.GhostProtocol, CardId.SignalScrambler, CardId.WindGuard,
                CardId.WindGuard, CardId.WindGuard
            };
            var relayPassive = new BattleState();
            relayPassive.StartEncounter(EncounterId.Hunt, relayPassiveDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.BlackBoxRelay, null, null, 0, null, 32004);
            relayPassive.PlayCard(relayPassive.Hand.IndexOf(CardId.GhostProtocol));
            relayPassive.PlayCard(relayPassive.Hand.IndexOf(CardId.SignalScrambler));
            if (!relayPassive.ContractPassiveTriggered || relayPassive.ContractPassiveProcs != 1 ||
                relayPassive.EvasionExposure != 0 || relayPassive.LockOn != 1)
                throw new InvalidOperationException("Black box relay passive did not convert active trace clearing into Lock-On.");

            CargoContract[] contracts = Enum.GetValues(typeof(CargoContract)).Cast<CargoContract>().ToArray();
            CardId[] signatureCards = contracts.Select(ContractCardCatalog.SignatureCard).ToArray();
            if (signatureCards.Distinct().Count() != contracts.Length)
                throw new InvalidOperationException("Contract signature cards are not unique.");
            foreach (CargoContract contract in contracts)
            {
                foreach (CargoContract other in contracts)
                {
                    bool shouldBelong = contract == other;
                    if (ContractCardCatalog.BelongsTo(ContractCardCatalog.SignatureCard(contract), other) != shouldBelong)
                        throw new InvalidOperationException("A signature card leaked into another contract pool.");
                }
            }

            var sealDeck = new[]
            {
                CardId.TargetLock, CardId.ReactiveSeal, CardId.WindGuard,
                CardId.WindGuard, CardId.WindGuard
            };
            var sealState = new BattleState();
            sealState.StartEncounter(EncounterId.Skirmish, sealDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.FragileMedicine, null, null, 0, null, 33001);
            sealState.PlayCard(sealState.Hand.IndexOf(CardId.TargetLock));
            sealState.PlayCard(sealState.Hand.IndexOf(CardId.ReactiveSeal));
            if (sealState.Armor != 12 || sealState.LockOn != 0)
                throw new InvalidOperationException("Reactive Seal did not convert one Lock-On into doubled shielding.");

            var exchangeDeck = new[]
            {
                CardId.HeatCharge, CardId.HeatCharge, CardId.PhaseExchange,
                CardId.WindGuard, CardId.WindGuard
            };
            var exchangeState = new BattleState();
            exchangeState.StartEncounter(EncounterId.Skirmish, exchangeDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.CryoSerum, null, null, 0, null, 33002);
            exchangeState.PlayCard(exchangeState.Hand.IndexOf(CardId.HeatCharge));
            exchangeState.PlayCard(exchangeState.Hand.IndexOf(CardId.HeatCharge));
            exchangeState.PlayCard(exchangeState.Hand.IndexOf(CardId.PhaseExchange));
            if (exchangeState.Heat != 0 || exchangeState.Hand.Count != 4 ||
                !exchangeState.ContractPassiveTriggered || exchangeState.ContractPassiveProcs != 1)
                throw new InvalidOperationException("Phase Exchange did not convert six Heat into two draws and trigger the contract.");

            var transitDeck = Enumerable.Repeat(CardId.EyeTransit, 5).ToArray();
            var transitState = new BattleState();
            transitState.StartEncounter(EncounterId.Skirmish, transitDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.StormCore, null, null, 0, null, 33003);
            int laneBeforeTransit = transitState.PlayerLane;
            transitState.PlayCard(0);
            if (transitState.PlayerLane == laneBeforeTransit || transitState.PlayerLane != 0 ||
                transitState.Momentum != 2 || !transitState.ContractPassiveTriggered)
                throw new InvalidOperationException("Eye Transit did not cross to the outer lane and build contract Momentum.");

            var telemetryDeck = new[]
            {
                CardId.EngineOverclock, CardId.EngineOverclock, CardId.FalseTelemetry,
                CardId.WindGuard, CardId.WindGuard
            };
            var telemetryState = new BattleState();
            telemetryState.StartEncounter(EncounterId.Hunt, telemetryDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.BlackBoxRelay, null, null, 0, null, 33004);
            telemetryState.PlayCard(telemetryState.Hand.IndexOf(CardId.EngineOverclock));
            telemetryState.PlayCard(telemetryState.Hand.IndexOf(CardId.EngineOverclock));
            telemetryState.PlayCard(telemetryState.Hand.IndexOf(CardId.FalseTelemetry));
            if (telemetryState.EvasionExposure != 2 || telemetryState.Hand.Count != 4)
                throw new InvalidOperationException("False Telemetry did not trade two Exposure for two draws.");

            var retrofitDeck = Enumerable.Repeat(CardId.WindGuard, 8).ToArray();
            var bulkheadState = new BattleState();
            bulkheadState.StartEncounter(EncounterId.Skirmish, retrofitDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.FragileMedicine, null, null, 0, null, 34001,
                AirframeModification.SealedBulkhead);
            if (bulkheadState.Modification != AirframeModification.SealedBulkhead ||
                bulkheadState.Armor != 5 || bulkheadState.HandTarget != 4 ||
                bulkheadState.Hand.Count != 4 || bulkheadState.TurnEnergy != 3)
                throw new InvalidOperationException("Sealed Bulkhead did not exchange hand capacity for turn shielding.");
            bulkheadState.EndTurn();
            if (bulkheadState.Armor != 5 || bulkheadState.Hand.Count != 4)
                throw new InvalidOperationException("Sealed Bulkhead did not restore its altered turn baseline.");

            var avionicsState = new BattleState();
            avionicsState.StartEncounter(EncounterId.Skirmish, retrofitDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.FragileMedicine, null, null, 0, null, 34002,
                AirframeModification.OpenAvionics);
            if (avionicsState.Modification != AirframeModification.OpenAvionics ||
                avionicsState.HandTarget != 6 || avionicsState.Hand.Count != 6 ||
                avionicsState.EvasionExposure != 1)
                throw new InvalidOperationException("Open Avionics did not exchange Trace Exposure for a six-card hand.");
            avionicsState.EndTurn();
            if (avionicsState.Hand.Count != 6 || avionicsState.EvasionExposure != 1)
                throw new InvalidOperationException("Open Avionics did not restore its altered turn baseline.");

            var turbineDeck = Enumerable.Repeat(CardId.EngineOverclock, 8).ToArray();
            var turbineState = new BattleState();
            turbineState.StartEncounter(EncounterId.Skirmish, turbineDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.FragileMedicine, null, null, 0, null, 34003,
                AirframeModification.RedlineTurbine);
            if (turbineState.Modification != AirframeModification.RedlineTurbine ||
                turbineState.Energy != 4 || turbineState.TurnEnergy != 4)
                throw new InvalidOperationException("Redline Turbine did not establish four base Energy.");
            turbineState.PlayCard(0);
            turbineState.EndTurn();
            if (turbineState.Energy != 4 || turbineState.Heat != 2)
                throw new InvalidOperationException("Redline Turbine did not disable passive cooling.");

            for (int variant = 0; variant < 4; variant++)
            {
                EncounterDefinition lateVariant = EncounterCatalog.Get(EncounterId.Skirmish, variant);
                if (lateVariant.Variant != variant || lateVariant.Enemies.Length == 0)
                    throw new InvalidOperationException($"Encounter variant {variant} is not addressable.");
            }

            var countermeasureDeck = Enumerable.Repeat(CardId.WindGuard, 8).ToArray();
            var shieldLeechState = new BattleState();
            shieldLeechState.StartEncounter(EncounterId.Skirmish, countermeasureDeck,
                BattleState.MaxPlayerHealth, 3, CargoContract.FragileMedicine, null, null, 2, null, 35001);
            shieldLeechState.PlayCard(0);
            EnemyState shieldLeech = shieldLeechState.Enemies.First(enemy => enemy.Kind == EnemyKind.ShieldLeech);
            if (!shieldLeechState.IntentFor(shieldLeech).Contains("盾蚀"))
                throw new InvalidOperationException("Shield Leech did not telegraph armor erosion.");
            shieldLeechState.EndTurn();
            if (!shieldLeechState.LastShieldBroken ||
                !shieldLeechState.LastStatusTrigger.Contains("盾蚀") ||
                shieldLeechState.PlayerHealth != BattleState.MaxPlayerHealth)
                throw new InvalidOperationException("Shield Leech did not remove a high shield without hidden hull damage.");

            var handJammerState = new BattleState();
            handJammerState.StartEncounter(EncounterId.Skirmish, countermeasureDeck,
                BattleState.MaxPlayerHealth, 3, CargoContract.FragileMedicine, null, null, 3, null, 35002);
            EnemyState handJammer = handJammerState.Enemies.First(enemy => enemy.Kind == EnemyKind.HandJammer);
            if (!handJammerState.IntentFor(handJammer).Contains("手牌干扰"))
                throw new InvalidOperationException("Hand Jammer did not react to a five-card hand.");
            handJammerState.EndTurn();
            if (handJammerState.PlayerHealth != BattleState.MaxPlayerHealth - handJammer.Damage ||
                handJammerState.LastDamageSource != PlayerDamageSource.HandJam)
                throw new InvalidOperationException("Hand Jammer did not punish ending with five cards.");

            var heatCounterDeck = Enumerable.Repeat(CardId.EngineOverclock, 8).ToArray();
            var heatSeekerState = new BattleState();
            heatSeekerState.StartEncounter(EncounterId.Skirmish, heatCounterDeck,
                BattleState.MaxPlayerHealth, 3, CargoContract.FragileMedicine, null, null, 2, null, 35003);
            heatSeekerState.PlayCard(0);
            heatSeekerState.PlayCard(0);
            EnemyState heatSeeker = heatSeekerState.Enemies.First(enemy => enemy.Kind == EnemyKind.HeatSeeker);
            if (!heatSeekerState.IntentFor(heatSeeker).Contains("热寻"))
                throw new InvalidOperationException("Heat Seeker did not react to four Heat.");
            heatSeekerState.EndTurn();
            if (heatSeekerState.PlayerHealth != BattleState.MaxPlayerHealth - heatSeeker.Damage ||
                heatSeekerState.LastDamageSource != PlayerDamageSource.HeatSeek)
                throw new InvalidOperationException("Heat Seeker did not execute its high-Heat strike.");

            var hijackerDeck = new[]
            {
                CardId.BankUp, CardId.TargetLock, CardId.WindGuard,
                CardId.WindGuard, CardId.WindGuard
            };
            var hijackerState = new BattleState();
            hijackerState.StartEncounter(EncounterId.Skirmish, hijackerDeck,
                BattleState.MaxPlayerHealth, 3, CargoContract.FragileMedicine, null, null, 3, null, 35004);
            hijackerState.PlayCard(hijackerState.Hand.IndexOf(CardId.BankUp));
            hijackerState.PlayCard(hijackerState.Hand.IndexOf(CardId.TargetLock));
            EnemyState hijacker = hijackerState.Enemies.First(enemy => enemy.Kind == EnemyKind.SignalHijacker);
            if (!hijackerState.IntentFor(hijacker).Contains("劫持锁定"))
                throw new InvalidOperationException("Signal Hijacker did not preview the resource it would steal.");
            hijackerState.EndTurn();
            if (hijackerState.LockOn != 0 || hijacker.Armor != 3 ||
                !hijackerState.LastStatusTrigger.Contains("协议劫持"))
                throw new InvalidOperationException("Signal Hijacker did not convert a player resource into armor.");

            state.StartEncounter(EncounterId.Skirmish, safeDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.FragileMedicine, new[] { CardId.WindGuard }, new[] { ModuleId.PrismBulkhead });
            if (state.Armor != 3 || !state.IsUpgraded(CardId.WindGuard))
                throw new InvalidOperationException("Prism bulkhead or card upgrade did not initialize.");
            state.PlayCard(0);
            if (state.Armor != 12)
                throw new InvalidOperationException("Upgraded Wind Guard did not add 9 armor.");

            var maneuverDeck = Enumerable.Repeat(CardId.BankDown, 8).ToArray();
            state.StartEncounter(EncounterId.Skirmish, maneuverDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.StormCore, null, new[] { ModuleId.VectorThruster });
            state.PlayCard(0);
            if (state.Energy != 3 || state.LastModuleProc != "矢量回流器")
                throw new InvalidOperationException("Vector thruster did not refund the first maneuver cost.");

            var attackDeck = Enumerable.Repeat(CardId.BurstFire, 8).ToArray();
            state.StartEncounter(EncounterId.Skirmish, attackDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.FragileMedicine, null, new[] { ModuleId.ExecutionChip });
            EnemyState executionTarget = state.Enemies.First(enemy => enemy.Lane == state.PlayerLane);
            int targetDurability = executionTarget.Health + executionTarget.Armor;
            state.PlayCard(0);
            if (executionTarget.Health + executionTarget.Armor != targetDurability - 10)
                throw new InvalidOperationException("Execution chip did not add 4 damage to the first attack.");

            state.StartEncounter(EncounterId.Skirmish, hotDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.CryoSerum, null, new[] { ModuleId.CryoHeart });
            state.PlayCard(0);
            state.PlayCard(0);
            state.PlayCard(0);
            state.PlayCard(0);
            if (state.HeatLimit != 10 || state.PlayerHealth != BattleState.MaxPlayerHealth)
                throw new InvalidOperationException("Cryo heart did not increase the overheat limit.");

            foreach (CardId card in Enum.GetValues(typeof(CardId)))
                _ = CardLibrary.Get(card);

            var precisionDeck = new[] { CardId.TargetLock, CardId.RailPiercer, CardId.TargetLock, CardId.RailPiercer, CardId.TargetLock };
            state.StartEncounter(EncounterId.Skirmish, precisionDeck, BattleState.MaxPlayerHealth, 3);
            state.PlayCard(state.Hand.IndexOf(CardId.TargetLock));
            EnemyState precisionEnemy = state.Enemies.First(enemy => enemy.Lane == state.PlayerLane);
            int precisionTarget = precisionEnemy.Health + precisionEnemy.Armor;
            state.PlayCard(state.Hand.IndexOf(CardId.RailPiercer));
            if (state.LockOn != 0 || precisionEnemy.Health + precisionEnemy.Armor != precisionTarget - 13 ||
                state.LastArmorBreak != precisionEnemy.Name)
                throw new InvalidOperationException("Precision lock build did not convert lock into rail damage.");

            var maneuverBuild = new[] { CardId.VectorDash, CardId.PursuitShot, CardId.VectorDash, CardId.PursuitShot, CardId.BankDown };
            state.StartEncounter(EncounterId.Elite, maneuverBuild, BattleState.MaxPlayerHealth, 3);
            state.PlayCard(state.Hand.IndexOf(CardId.VectorDash));
            EnemyState pursuitEnemy = state.Enemies.First(enemy => enemy.Lane == state.PlayerLane);
            int pursuitTarget = pursuitEnemy.Health + pursuitEnemy.Armor;
            state.PlayCard(state.Hand.IndexOf(CardId.PursuitShot));
            if (state.Momentum != 0 || pursuitEnemy.Health + pursuitEnemy.Armor != pursuitTarget - 9)
                throw new InvalidOperationException("Maneuver build did not convert momentum into pursuit damage.");

            var shieldBuild = new[] { CardId.ReactivePlating, CardId.AegisRam, CardId.ReactivePlating, CardId.AegisRam, CardId.WindGuard };
            state.StartEncounter(EncounterId.Skirmish, shieldBuild, BattleState.MaxPlayerHealth, 3);
            state.PlayCard(state.Hand.IndexOf(CardId.ReactivePlating));
            EnemyState ramEnemy = state.Enemies.First(enemy => enemy.Lane == state.PlayerLane);
            int ramTarget = ramEnemy.Health + ramEnemy.Armor;
            state.PlayCard(state.Hand.IndexOf(CardId.AegisRam));
            if (state.Armor != 0 || ramEnemy.Health + ramEnemy.Armor != ramTarget - 11)
                throw new InvalidOperationException("Shield build did not convert armor into ram damage.");

            var thermalBuild = new[] { CardId.HeatCharge, CardId.CryoPump, CardId.FrostLance, CardId.HeatCharge, CardId.CryoPump };
            state.StartEncounter(EncounterId.Skirmish, thermalBuild, BattleState.MaxPlayerHealth, 3);
            state.PlayCard(state.Hand.IndexOf(CardId.HeatCharge));
            int energyBeforePump = state.Energy;
            state.PlayCard(state.Hand.IndexOf(CardId.CryoPump));
            EnemyState frostEnemy = state.Enemies.First(enemy => enemy.Lane == state.PlayerLane);
            int frostTarget = frostEnemy.Health + frostEnemy.Armor;
            state.PlayCard(state.Hand.IndexOf(CardId.FrostLance));
            if (state.Heat != 1 || state.Energy != energyBeforePump ||
                frostEnemy.Health + frostEnemy.Armor != frostTarget - 20 || !state.LastAttackCritical)
                throw new InvalidOperationException("Cryo loop did not refund energy and enable low-heat Frost Lance.");

            var overheatBuild = new[] { CardId.HeatCharge, CardId.HeatCharge, CardId.MeltdownBurst, CardId.HeatCharge, CardId.MeltdownBurst };
            state.StartEncounter(EncounterId.Elite, overheatBuild, BattleState.MaxPlayerHealth, 3);
            state.PlayCard(state.Hand.IndexOf(CardId.HeatCharge));
            state.PlayCard(state.Hand.IndexOf(CardId.HeatCharge));
            int totalBeforePulse = state.Enemies.Sum(enemy => enemy.Health + enemy.Armor);
            state.PlayCard(state.Hand.IndexOf(CardId.MeltdownBurst));
            if (state.Heat != 0 || totalBeforePulse - state.Enemies.Sum(enemy => enemy.Health + enemy.Armor) != 24)
                throw new InvalidOperationException("Overheat build did not discharge stored heat across all enemies.");

            var barrageBuild = new[] { CardId.Scattershot, CardId.MissileSwarm, CardId.Scattershot, CardId.MissileSwarm, CardId.BroadsideVolley };
            state.StartEncounter(EncounterId.Elite, barrageBuild, BattleState.MaxPlayerHealth, 3);
            int totalBeforeBarrage = state.Enemies.Sum(enemy => enemy.Health + enemy.Armor);
            state.PlayCard(state.Hand.IndexOf(CardId.Scattershot));
            state.PlayCard(state.Hand.IndexOf(CardId.MissileSwarm));
            if (totalBeforeBarrage - state.Enemies.Sum(enemy => enemy.Health + enemy.Armor) != 14)
                throw new InvalidOperationException("Barrage build did not apply all-lane and multi-hit damage.");

            var branchDeck = new[] { CardId.WindGuard, CardId.ReactivePlating, CardId.WindGuard,
                CardId.ReactivePlating, CardId.BurstFire };
            var betaBranches = new System.Collections.Generic.Dictionary<CardId, UpgradeBranch>
            {
                [CardId.ReactivePlating] = UpgradeBranch.Beta
            };
            state.StartEncounter(EncounterId.Skirmish, branchDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.FragileMedicine, new[] { CardId.ReactivePlating }, null, 0, betaBranches);
            state.PlayCard(state.Hand.IndexOf(CardId.WindGuard));
            state.PlayCard(state.Hand.IndexOf(CardId.ReactivePlating));
            if (state.UpgradeBranchFor(CardId.ReactivePlating) != UpgradeBranch.Beta || state.Armor != 13 || state.Energy != 2)
                throw new InvalidOperationException("Reactive plating beta branch did not trade numeric scaling for energy recursion.");

            state.StartEncounter(EncounterId.Skirmish, branchDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.FragileMedicine, null, new[] { ModuleId.AegisCapacitor }, 0);
            state.PlayCard(state.Hand.IndexOf(CardId.WindGuard));
            state.PlayCard(state.Hand.IndexOf(CardId.ReactivePlating));
            if (state.Energy != 2 || state.LastModuleProc != "神盾电容")
                throw new InvalidOperationException("Aegis capacitor did not turn a formed shield build into extra tempo.");

            var bossPressureDeck = Enumerable.Repeat(CardId.RailPiercer, 10).ToArray();
            state.StartEncounter(EncounterId.Boss, bossPressureDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.FragileMedicine, null, null, 0);
            EnemyState boss = state.Enemies.Single();
            if (boss.Phase != 1 || boss.ChargeTargetLane != state.PlayerLane ||
                !state.IntentFor(boss).Contains($"打断 0/{BattleState.BossPhaseOneBreakDamage}"))
                throw new InvalidOperationException("Boss phase one did not telegraph its lane strike and break threshold.");
            state.PlayCard(0);
            state.PlayCard(0);
            if (!boss.ChargeInterrupted)
                throw new InvalidOperationException("Concentrated damage did not interrupt the boss ultimate.");
            state.EndTurn();
            if (state.PlayerHealth != BattleState.MaxPlayerHealth)
                throw new InvalidOperationException("Interrupted boss ultimate still damaged the player.");
            while (boss.Alive && boss.Phase == 1 && !state.Defeat)
            {
                int attacks = 0;
                while (attacks < 3 && state.Hand.Count > 0 && state.CanPlay(0))
                {
                    state.PlayCard(0);
                    attacks++;
                }
                if (boss.Phase == 1)
                    state.EndTurn();
            }
            if (!boss.Alive || boss.Phase != 2 || !boss.PhaseTransitionPending || boss.Armor < 8)
                throw new InvalidOperationException("Boss did not enter its armored second phase at half health.");

            var alliedBossState = new BattleState();
            alliedBossState.StartEncounter(EncounterId.Boss, bossPressureDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.FragileMedicine, null, null, 0, null, 39001,
                AirframeModification.None, RouteStoryState.PromiseFulfilled);
            EnemyState alliedBoss = alliedBossState.Enemies.Single();
            while (alliedBoss.Phase == 1 && !alliedBossState.Defeat)
            {
                while (alliedBoss.Phase == 1 && alliedBossState.Energy > 0 &&
                    alliedBossState.Hand.Count > 0 && alliedBossState.CanPlay(0))
                    alliedBossState.PlayCard(0);
                if (alliedBoss.Phase == 1)
                    alliedBossState.EndTurn();
            }
            if (alliedBoss.Phase != 2 || alliedBoss.Armor != 4 ||
                alliedBossState.ActiveBossStoryAlignment != BossStoryAlignment.Allied)
                throw new InvalidOperationException("Allied Signal Thread did not weaken phase-two boss armor.");

            var hostileBossState = new BattleState();
            hostileBossState.StartEncounter(EncounterId.Boss, bossPressureDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.FragileMedicine, null, null, 0, null, 39002,
                AirframeModification.None, RouteStoryState.DebtDefied);
            EnemyState hostileBoss = hostileBossState.Enemies.Single();
            while (hostileBoss.Phase == 1 && !hostileBossState.Defeat)
            {
                while (hostileBoss.Phase == 1 && hostileBossState.Energy > 0 &&
                    hostileBossState.Hand.Count > 0 && hostileBossState.CanPlay(0))
                    hostileBossState.PlayCard(0);
                if (hostileBoss.Phase == 1)
                    hostileBossState.EndTurn();
            }
            if (hostileBoss.Phase != 2 || hostileBoss.Armor != 12 ||
                hostileBossState.ActiveBossStoryAlignment != BossStoryAlignment.Hostile)
                throw new InvalidOperationException("Hostile Signal Thread did not reinforce phase-two boss armor.");

            var adaptiveBossDeck = Enumerable.Repeat(CardId.WindGuard, 10).ToArray();
            var openBossState = new BattleState();
            openBossState.StartEncounter(EncounterId.Boss, adaptiveBossDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.BlackBoxRelay, null, null, 0, null, 39003,
                AirframeModification.OpenAvionics, RouteStoryState.PromiseFulfilled);
            EnemyState openBoss = openBossState.Enemies.Single();
            openBoss.Phase = 2;
            openBoss.PhaseTransitionPending = false;
            openBoss.Armor = 0;
            openBoss.ChargeInterrupted = true;
            if (!openBossState.BossContractProtocolWillTrigger() ||
                !openBossState.BossAirframeProtocolWillTrigger())
                throw new InvalidOperationException("Adaptive boss did not preview Black Box and Open Avionics counters.");
            openBossState.EndTurn();
            if (openBossState.PlayerHealth != BattleState.MaxPlayerHealth - BattleState.BossAdaptationDamage ||
                openBossState.LastDamageSource != PlayerDamageSource.BossWidebandJam ||
                openBoss.Armor != BattleState.BossAdaptiveArmor ||
                !openBossState.LastStatusTrigger.Contains("宽频干扰"))
                throw new InvalidOperationException("Open Avionics boss counter did not match its preview.");

            var redlineBossDeck = Enumerable.Repeat(CardId.EngineOverclock, 10).ToArray();
            var redlineBossState = new BattleState();
            redlineBossState.StartEncounter(EncounterId.Boss, redlineBossDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.FragileMedicine, null, null, 0, null, 39004,
                AirframeModification.RedlineTurbine, RouteStoryState.None);
            EnemyState redlineBoss = redlineBossState.Enemies.Single();
            redlineBoss.Phase = 2;
            redlineBoss.PhaseTransitionPending = false;
            redlineBoss.ChargeInterrupted = true;
            redlineBossState.PlayCard(0);
            redlineBossState.PlayCard(0);
            if (!redlineBossState.BossAirframeProtocolWillTrigger())
                throw new InvalidOperationException("Redline boss counter did not preview at four Heat.");
            redlineBossState.EndTurn();
            if (redlineBossState.LastDamageSource != PlayerDamageSource.BossThermalLock ||
                redlineBossState.PlayerHealth != BattleState.MaxPlayerHealth - BattleState.BossAdaptationDamage)
                throw new InvalidOperationException("Redline Turbine boss counter did not execute.");

            var sealedBossState = new BattleState();
            sealedBossState.StartEncounter(EncounterId.Boss, adaptiveBossDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.StormCore, null, null, 0, null, 39005,
                AirframeModification.SealedBulkhead, RouteStoryState.None);
            EnemyState sealedBoss = sealedBossState.Enemies.Single();
            sealedBoss.Phase = 2;
            sealedBoss.PhaseTransitionPending = false;
            sealedBoss.ChargeInterrupted = true;
            if (!sealedBossState.BossAirframeProtocolWillTrigger())
                throw new InvalidOperationException("Sealed Bulkhead boss counter did not preview at five Shield.");
            sealedBossState.EndTurn();
            if (!sealedBossState.LastShieldBroken || !sealedBossState.LastStatusTrigger.Contains("裂盾回波"))
                throw new InvalidOperationException("Sealed Bulkhead boss counter did not clear the turn shield.");

            var vectorBossState = new BattleState();
            vectorBossState.StartEncounter(EncounterId.Boss,
                Enumerable.Repeat(CardId.BankUp, 10).ToArray(), BattleState.MaxPlayerHealth, 3,
                CargoContract.StormCore, null, null, 0, null, 39006,
                AirframeModification.RedlineTurbine);
            EnemyState vectorBoss = vectorBossState.Enemies.Single();
            vectorBoss.Phase = 2;
            vectorBoss.PhaseTransitionPending = false;
            vectorBoss.Armor = 0;
            vectorBoss.ChargeInterrupted = true;
            vectorBossState.PlayCard(0);
            vectorBossState.EndTurn();
            if (vectorBossState.Momentum != 0 || vectorBoss.Armor != BattleState.BossAdaptiveArmor)
                throw new InvalidOperationException("Storm Core boss protocol did not intercept held Momentum.");

            EncounterDefinition mantaFinale = EncounterCatalog.Get(EncounterId.Boss, 0);
            EncounterDefinition wyrmFinale = EncounterCatalog.Get(EncounterId.Boss, 1);
            if (mantaFinale.Enemies.Single().Kind != EnemyKind.StormManta ||
                wyrmFinale.Enemies.Single().Kind != EnemyKind.CloudWyrm)
                throw new InvalidOperationException("Boss encounter variants did not resolve to two distinct finales.");

            var safeLaneState = new BattleState();
            safeLaneState.StartEncounter(EncounterId.Boss, Enumerable.Repeat(CardId.BankDown, 10).ToArray(),
                BattleState.MaxPlayerHealth, 3, CargoContract.FragileMedicine, null, null, 1);
            EnemyState safeLaneBoss = safeLaneState.Enemies.Single();
            if (safeLaneBoss.Kind != EnemyKind.CloudWyrm || safeLaneBoss.ChargeTargetLane != 2 ||
                !safeLaneState.IntentFor(safeLaneBoss).Contains("仅航道 3 安全") ||
                !safeLaneState.IntentFor(safeLaneBoss).Contains(
                    $"打断 0/{BattleState.CloudWyrmPhaseOneBreakDamage}"))
                throw new InvalidOperationException("Cloud Wyrm did not clearly telegraph its unique safe corridor.");
            safeLaneState.PlayCard(0);
            safeLaneState.EndTurn();
            if (safeLaneState.PlayerHealth != BattleState.MaxPlayerHealth ||
                safeLaneState.LastDamageSource == PlayerDamageSource.BossCurtain)
                throw new InvalidOperationException("Cloud Wyrm damaged the player inside its marked safe corridor.");

            var unsafeLaneState = new BattleState();
            unsafeLaneState.StartEncounter(EncounterId.Boss, Enumerable.Repeat(CardId.WindGuard, 10).ToArray(),
                BattleState.MaxPlayerHealth, 3, CargoContract.FragileMedicine, null, null, 1);
            unsafeLaneState.EndTurn();
            if (unsafeLaneState.PlayerHealth != BattleState.MaxPlayerHealth -
                    BattleState.CloudWyrmPhaseOneStrikeDamage ||
                unsafeLaneState.LastDamageSource != PlayerDamageSource.BossCurtain)
                throw new InvalidOperationException("Cloud Wyrm did not strike outside its marked safe corridor.");

            var interruptedWyrmState = new BattleState();
            interruptedWyrmState.StartEncounter(EncounterId.Boss, bossPressureDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.FragileMedicine, null, null, 1);
            EnemyState interruptedWyrm = interruptedWyrmState.Enemies.Single();
            interruptedWyrmState.PlayCard(0);
            interruptedWyrmState.PlayCard(0);
            if (!interruptedWyrm.ChargeInterrupted)
                throw new InvalidOperationException("Concentrated damage did not interrupt the Cloud Wyrm curtain.");
            interruptedWyrmState.EndTurn();
            if (interruptedWyrmState.PlayerHealth != BattleState.MaxPlayerHealth)
                throw new InvalidOperationException("Interrupted Cloud Wyrm curtain still damaged the player.");

            var alliedWyrmState = new BattleState();
            alliedWyrmState.StartEncounter(EncounterId.Boss, bossPressureDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.FragileMedicine, null, null, 1, null, 40002,
                AirframeModification.None, RouteStoryState.PromiseFulfilled);
            EnemyState alliedWyrm = alliedWyrmState.Enemies.Single();
            alliedWyrm.Armor = 0;
            alliedWyrm.Health = alliedWyrm.MaxHealth / 2 + 1;
            alliedWyrmState.PlayCard(0);
            if (alliedWyrm.Phase != 2 || !alliedWyrm.PhaseTransitionPending || alliedWyrm.Armor != 4)
                throw new InvalidOperationException("Allied Signal Thread did not weaken the Cloud Wyrm phase transition.");

            var adaptiveWyrmState = new BattleState();
            adaptiveWyrmState.StartEncounter(EncounterId.Boss, adaptiveBossDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.BlackBoxRelay, null, null, 1, null, 40003,
                AirframeModification.OpenAvionics, RouteStoryState.PromiseFulfilled);
            EnemyState adaptiveWyrm = adaptiveWyrmState.Enemies.Single();
            adaptiveWyrm.Phase = 2;
            adaptiveWyrm.PhaseTransitionPending = false;
            adaptiveWyrm.Armor = 0;
            adaptiveWyrm.ChargeInterrupted = true;
            adaptiveWyrmState.EndTurn();
            if (adaptiveWyrmState.LastDamageSource != PlayerDamageSource.BossWidebandJam ||
                adaptiveWyrm.Armor != BattleState.BossAdaptiveArmor)
                throw new InvalidOperationException("Cloud Wyrm did not inherit the adaptive boss matrix.");

            EncounterDefinition curtainPrelude = EncounterCatalog.Get(EncounterId.Hunt, 4);
            EncounterDefinition dualPrelude = EncounterCatalog.Get(EncounterId.Elite, 4);
            EncounterDefinition fluxPrelude = EncounterCatalog.Get(EncounterId.Skirmish, 5);
            if (!curtainPrelude.Enemies.Any(enemy => enemy.Kind == EnemyKind.CurtainHerald) ||
                !dualPrelude.Enemies.Any(enemy => enemy.Kind == EnemyKind.CurtainHerald) ||
                !dualPrelude.Enemies.Any(enemy => enemy.Kind == EnemyKind.FluxSkimmer) ||
                !fluxPrelude.Enemies.Any(enemy => enemy.Kind == EnemyKind.FluxSkimmer))
                throw new InvalidOperationException("Finale prelude formations do not expose both boss teaching enemies.");
            if (FinaleProgressionRules.IntelForPreludeNode(15) != RouteIntel.CurtainCipher ||
                FinaleProgressionRules.IntelForPreludeNode(16) != RouteIntel.DualChannelDecoder ||
                FinaleProgressionRules.IntelForPreludeNode(17) != RouteIntel.FluxCompass ||
                FinaleProgressionRules.IntelForPreludeNode(14) != RouteIntel.None)
                throw new InvalidOperationException("Finale prelude route nodes do not grant their intended intel.");

            var curtainSafeState = new BattleState();
            curtainSafeState.StartEncounter(EncounterId.Hunt, Enumerable.Repeat(CardId.BankDown, 8).ToArray(),
                BattleState.MaxPlayerHealth, 3, CargoContract.FragileMedicine, null, null, 4, null, 44001);
            EnemyState curtainSafe = curtainSafeState.Enemies.Single(enemy => enemy.Kind == EnemyKind.CurtainHerald);
            curtainSafeState.Enemies.Single(enemy => enemy != curtainSafe).Health = 0;
            if (curtainSafe.ChargeTargetLane != 2 ||
                !curtainSafeState.IntentFor(curtainSafe).Contains("仅航道 3 安全"))
                throw new InvalidOperationException("Curtain Herald did not preview its single safe corridor.");
            curtainSafeState.PlayCard(0);
            curtainSafeState.EndTurn();
            if (curtainSafeState.PlayerHealth != BattleState.MaxPlayerHealth)
                throw new InvalidOperationException("Curtain Herald damaged the player inside its safe corridor.");

            var curtainUnsafeState = new BattleState();
            curtainUnsafeState.StartEncounter(EncounterId.Hunt, Enumerable.Repeat(CardId.WindGuard, 8).ToArray(),
                BattleState.MaxPlayerHealth, 3, CargoContract.FragileMedicine, null, null, 4, null, 44002);
            EnemyState curtainUnsafe = curtainUnsafeState.Enemies.Single(enemy => enemy.Kind == EnemyKind.CurtainHerald);
            curtainUnsafeState.Enemies.Single(enemy => enemy != curtainUnsafe).Health = 0;
            curtainUnsafeState.EndTurn();
            if (curtainUnsafeState.LastDamageSource != PlayerDamageSource.PreludeCurtain ||
                curtainUnsafeState.PlayerHealth != BattleState.MaxPlayerHealth - curtainUnsafe.Damage)
                throw new InvalidOperationException("Curtain Herald did not punish leaving its safe corridor.");

            var curtainBreakState = new BattleState();
            curtainBreakState.StartEncounter(EncounterId.Hunt, Enumerable.Repeat(CardId.BurstFire, 8).ToArray(),
                BattleState.MaxPlayerHealth, 3, CargoContract.FragileMedicine, null, null, 4, null, 44003);
            EnemyState curtainBreak = curtainBreakState.Enemies.Single(enemy => enemy.Kind == EnemyKind.CurtainHerald);
            curtainBreakState.PlayCard(0);
            if (!curtainBreak.ChargeInterrupted ||
                !curtainBreakState.IntentFor(curtainBreak).Contains("雷幕短路"))
                throw new InvalidOperationException("Curtain Herald charge could not be interrupted at its shown threshold.");

            var fluxSafeState = new BattleState();
            fluxSafeState.StartEncounter(EncounterId.Skirmish, Enumerable.Repeat(CardId.BankUp, 8).ToArray(),
                BattleState.MaxPlayerHealth, 3, CargoContract.FragileMedicine, null, null, 5, null, 44004);
            EnemyState fluxSafe = fluxSafeState.Enemies.Single(enemy => enemy.Kind == EnemyKind.FluxSkimmer);
            fluxSafeState.Enemies.Single(enemy => enemy != fluxSafe).Health = 0;
            if (fluxSafe.ChargeTargetLane != 2)
                throw new InvalidOperationException("Flux Skimmer opening target lane is not lane 3.");
            if (!fluxSafeState.IntentFor(fluxSafe).Contains("+邻道危险"))
                throw new InvalidOperationException("Flux Skimmer intent is missing its adjacent-lane warning.");
            fluxSafeState.PlayCard(0);
            fluxSafeState.EndTurn();
            if (fluxSafeState.PlayerHealth != BattleState.MaxPlayerHealth)
                throw new InvalidOperationException("Flux Skimmer damaged the player outside its sweep.");

            var fluxUnsafeState = new BattleState();
            fluxUnsafeState.StartEncounter(EncounterId.Skirmish, Enumerable.Repeat(CardId.WindGuard, 8).ToArray(),
                BattleState.MaxPlayerHealth, 3, CargoContract.FragileMedicine, null, null, 5, null, 44005);
            EnemyState fluxUnsafe = fluxUnsafeState.Enemies.Single(enemy => enemy.Kind == EnemyKind.FluxSkimmer);
            fluxUnsafeState.Enemies.Single(enemy => enemy != fluxUnsafe).Health = 0;
            fluxUnsafeState.EndTurn();
            if (fluxUnsafeState.LastDamageSource != PlayerDamageSource.PreludeMagnet ||
                fluxUnsafeState.PlayerHealth != BattleState.MaxPlayerHealth - fluxUnsafe.Damage)
                throw new InvalidOperationException("Flux Skimmer did not strike its target-adjacent lane.");

            var wyrmIntelState = new BattleState();
            wyrmIntelState.StartEncounter(EncounterId.Boss, safeDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.FragileMedicine, null, null, 1, null, 44006,
                AirframeModification.None, RouteStoryState.None, RouteIntel.CurtainCipher);
            if (wyrmIntelState.Intel != RouteIntel.CurtainCipher ||
                wyrmIntelState.Enemies.Single().ChargeTargetLane != wyrmIntelState.PlayerLane)
                throw new InvalidOperationException("Curtain Cipher did not fix the Cloud Wyrm opening safe corridor.");

            var mantaIntelState = new BattleState();
            mantaIntelState.StartEncounter(EncounterId.Boss, safeDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.FragileMedicine, null, null, 0, null, 44007,
                AirframeModification.None, RouteStoryState.None, RouteIntel.FluxCompass);
            if (mantaIntelState.Intel != RouteIntel.FluxCompass ||
                mantaIntelState.Enemies.Single().ChargeTargetLane != 0)
                throw new InvalidOperationException("Flux Compass did not deflect the Storm Manta opening lock.");

            FinaleEnding[] finaleEndings =
            {
                FinaleProgressionRules.EndingFor(EnemyKind.CloudWyrm, BossStoryAlignment.Neutral),
                FinaleProgressionRules.EndingFor(EnemyKind.CloudWyrm, BossStoryAlignment.Allied),
                FinaleProgressionRules.EndingFor(EnemyKind.CloudWyrm, BossStoryAlignment.Hostile),
                FinaleProgressionRules.EndingFor(EnemyKind.StormManta, BossStoryAlignment.Neutral),
                FinaleProgressionRules.EndingFor(EnemyKind.StormManta, BossStoryAlignment.Allied),
                FinaleProgressionRules.EndingFor(EnemyKind.StormManta, BossStoryAlignment.Hostile)
            };
            if (finaleEndings.Any(ending => ending == FinaleEnding.None) ||
                finaleEndings.Distinct().Count() != 6)
                throw new InvalidOperationException("Boss and Signal Thread combinations do not resolve to six unique endings.");

            Debug.Log("SKY_COURIER_RULE_VALIDATION_COMPLETE");
        }
    }
}
