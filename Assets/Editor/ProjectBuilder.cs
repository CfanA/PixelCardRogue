using System;
using System.IO;
using System.Linq;
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

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("SKY_COURIER_SETUP_COMPLETE");
        }

        [MenuItem("Tools/Sky Courier/Build Windows Prototype")]
        public static void BuildWindowsPrototype()
        {
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

            string playtestReport = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Playtest_Report_v0.25.md"));
            if (File.Exists(playtestReport))
                File.Copy(playtestReport, Path.Combine(outputDirectory, "试玩与平衡报告.md"), true);
            File.WriteAllText(Path.Combine(outputDirectory, "试玩说明.txt"),
                "《云海邮差》v0.26\r\n\r\n运行：双击 Sky Courier Prototype.exe\r\n操作：点击图标航点选择分支；滚轮浏览航线；鼠标点击卡牌；Space结束回合；Esc暂停。\r\n目标：穿越8段分支航线并保护合同货物。\r\n",
                new System.Text.UTF8Encoding(true));

            string musicLicense = Path.GetFullPath(Path.Combine(Application.dataPath, "Resources/Audio/BGM/License.txt"));
            if (File.Exists(musicLicense))
                File.Copy(musicLicense, Path.Combine(outputDirectory, "音乐授权说明.txt"), true);

            Debug.Log($"SKY_COURIER_BUILD_COMPLETE: {executablePath}");
        }

        [MenuItem("Tools/Sky Courier/Validate Core Rules")]
        public static void ValidateCoreRules()
        {
            RouteDefinition route = RouteCatalog.WindmillArchipelago;
            if (route.ColumnCount != 8 || route.Nodes.Count != 19)
                throw new InvalidOperationException("Branching route must contain 8 columns and 19 nodes.");
            if (route.AtColumn(0).Count() != 1 || route.AtColumn(route.ColumnCount - 1).Count() != 1)
                throw new InvalidOperationException("Branching route must have one start and one boss node.");
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

            var deck = new[]
            {
                CardId.BurstFire, CardId.BurstFire, CardId.BankUp, CardId.BankDown,
                CardId.WindGuard, CardId.EmergencyCoolant, CardId.BroadsideVolley,
                CardId.OverloadAim, CardId.EngineOverclock
            };
            var state = new BattleState();

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

            Debug.Log("SKY_COURIER_RULE_VALIDATION_COMPLETE");
        }
    }
}
