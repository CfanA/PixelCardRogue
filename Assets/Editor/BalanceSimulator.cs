using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SkyCourier;
using UnityEditor;
using UnityEngine;

namespace SkyCourierEditor
{
    public static class BalanceSimulator
    {
        private const int RouteProfileCount = 2;

        private static readonly int[] RegressionSeeds =
        {
            RunSeedUtility.LegacySeed,
            104729,
            130363,
            155921
        };

        private sealed class BuildDefinition
        {
            public string Name;
            public CardId[] Additions;
            public CargoContract Contract;

            public BuildDefinition(string name, CardId[] additions, CargoContract contract)
            {
                Name = name;
                Additions = additions;
                Contract = contract;
            }
        }

        private static readonly BuildDefinition[] Builds =
        {
            new BuildDefinition("锁定狙击", new[] { CardId.TargetLock, CardId.RailPiercer, CardId.LockCascade }, CargoContract.FragileMedicine),
            new BuildDefinition("矢量追猎", new[] { CardId.VectorDash, CardId.PursuitShot, CardId.SlipstreamStrike }, CargoContract.StormCore),
            new BuildDefinition("护盾冲角", new[] { CardId.ReactivePlating, CardId.AegisRam, CardId.PrismEcho }, CargoContract.FragileMedicine),
            new BuildDefinition("零度循环", new[] { CardId.CryoPump, CardId.FrostLance, CardId.ZeroPointCalibration }, CargoContract.CryoSerum),
            new BuildDefinition("熔炉爆发", new[] { CardId.HeatCharge, CardId.MeltdownBurst, CardId.RedlineIgnition }, CargoContract.CryoSerum),
            new BuildDefinition("蜂群弹幕", new[] { CardId.Scattershot, CardId.MissileSwarm, CardId.SwarmBeacon }, CargoContract.StormCore),
            new BuildDefinition("航迹欺骗", new[] { CardId.SignalScrambler, CardId.CounterPursuit, CardId.GhostProtocol }, CargoContract.BlackBoxRelay),
            new BuildDefinition("侧翼雷网", new[] { CardId.AirBrake, CardId.InterceptMine, CardId.GhostProtocol }, CargoContract.BlackBoxRelay),
            new BuildDefinition("余量调度", new[] { CardId.ReserveShot, CardId.StandbyField, CardId.TightSchedule }, CargoContract.SignalSeed)
        };

        private sealed class RunResult
        {
            public string Name;
            public bool Victory;
            public int Hull;
            public int Cargo;
            public int Turns;
            public int Cards;
            public int Damage;
            public int Overheats;
            public int RouteProfile;
            public int CalamityInterrupts;
            public int CalamityEvades;
            public int CalamityHits;
            public int TrackingHits;
            public CargoContract Contract;
            public int Seed;
            public EnemyKind Boss;
            public bool BossReached;
            public bool BossVictory;
            public EncounterId? FailedEncounter;
        }

        [MenuItem("Tools/Sky Courier/Run Balance Suite")]
        public static void RunSuite()
        {
            var results = new List<RunResult>();
            for (int routeProfile = 0; routeProfile < RouteProfileCount; routeProfile++)
            {
                for (int seedIndex = 0; seedIndex < RegressionSeeds.Length; seedIndex++)
                {
                    int seed = RegressionSeeds[seedIndex];
                    int bossVariant = seedIndex % EncounterCatalog.BossVariantCount;
                    foreach (BuildDefinition build in Builds)
                    {
                        results.Add(SimulateRun(build.Name, build.Additions, build.Contract, routeProfile,
                            seed, bossVariant));
                    }
                }
            }

            foreach (RunResult result in results)
            {
                Debug.Log($"BALANCE_RESULT|{result.Name}|种子={result.Seed}|合同={result.Contract}|路线={RouteProfileLabel(result.RouteProfile)}|首领={BossLabel(result.Boss)}|胜利={result.Victory}|首领到达={result.BossReached}|首领胜利={result.BossVictory}|失败阶段={FailureLabel(result)}|机体={result.Hull}|货物={result.Cargo}|回合={result.Turns}|出牌={result.Cards}|受伤={result.Damage}|过热={result.Overheats}|打断={result.CalamityInterrupts}|规避={result.CalamityEvades}|命中={result.CalamityHits}|追踪={result.TrackingHits}");
            }

            int victories = results.Count(result => result.Victory);
            LogAggregates(results);
            Debug.Log($"BALANCE_SUMMARY|样本={results.Count}|固定种子={RegressionSeeds.Length}|通关={victories}/{results.Count}|胜率={WinRate(victories, results.Count):F1}%|平均回合={results.Average(result => result.Turns):F1}|平均受伤={results.Average(result => result.Damage):F1}");
            WriteOnePageReport(results);
        }

        private static void WriteOnePageReport(IList<RunResult> results)
        {
            int victories = results.Count(result => result.Victory);
            int expectedSamples = Builds.Length * RouteProfileCount * RegressionSeeds.Length;
            bool coverageComplete = results.Count == expectedSamples &&
                                    results.GroupBy(result => new { result.Name, result.RouteProfile })
                                        .All(group => group.Count() == RegressionSeeds.Length &&
                                                      group.Select(result => result.Boss).Distinct().Count() ==
                                                      EncounterCatalog.BossVariantCount);
            string[] zeroWinBuilds = results.GroupBy(result => result.Name)
                .Where(group => group.All(result => !result.Victory))
                .Select(group => group.Key)
                .ToArray();
            var report = new StringBuilder();
            report.AppendLine("# 《云海邮差》v0.50 自动回归与平衡记录");
            report.AppendLine();
            report.AppendLine($"> 生成日期：{DateTime.Now:yyyy-MM-dd}　|　确定性自动回归：{victories}/{results.Count} 通关（{WinRate(victories, results.Count):F1}%）");
            report.AppendLine();
            report.AppendLine("## 使用边界");
            report.AppendLine();
            report.AppendLine("自动模拟是规则与极端回归测试，用来发现完全失效的构筑、路线断点和首领异常；其胜率不是平衡目标，也不得为了让脚本通过而盲目调数。卡牌、敌人、经济和路线的最终调整应以真人试玩中的决策质量、失败可解释性和策略差异为依据，再用本报告检查是否引入明显回归。");
            report.AppendLine();
            report.AppendLine("本轮只落地两项真人记录与旧回归共同支持的小幅卡牌调整：霜脉长枪低热追加伤害 6→5（升级 8→7），余量点射基础伤害 7→8（升级 10→11）。自动出牌策略不等同于真人策略。");
            report.AppendLine();
            report.AppendLine("## 真人样本与数值边界");
            report.AppendLine();
            report.AppendLine("- 本机可核实的完整真人记录只有 2 局，均为零度血清送达；旧档案没有 v0.49 构筑快照，无法还原消费和路线选择。`RunsStarted=21` 混入编辑器界面预览，不作为真人胜率。 ");
            report.AppendLine("- 霜脉长枪在旧回归的零度循环中 2/2 通关且保持满机体，一费低热上限又高于需要消耗锁定或动量的同级终结牌，因此仅削峰 1 点。 ");
            report.AppendLine("- 余量调度旧回归 1/2、平均 62.5 回合但货物完整，问题更像收尾速度而非生存，所以只给主攻击 +1，不改变精确保留 1 能量的条件。 ");
            report.AppendLine("- 敌人、Boss、经济、路线收益、维修和商店价格本轮保持不变：现有真人数据不足以支持这些跨系统改动；它们进入 v0.50 档案胜率页后再复测。 ");
            report.AppendLine();
            report.AppendLine("## 回归配置");
            report.AppendLine();
            report.AppendLine($"- 样本矩阵：{Builds.Length} 套构筑 × {RouteProfileCount} 条路线 × {RegressionSeeds.Length} 个固定种子 = {expectedSamples} 局。");
            report.AppendLine($"- 固定种子：{string.Join("、", RegressionSeeds)}；每个遭遇再通过局种子、路线位置和遭遇类型派生独立种子。");
            report.AppendLine("- 首领覆盖：种子按固定顺序交替分配磁暴鳐与雷幕云龙，因此每套构筑在每条路线对两名首领各有两局样本。");
            report.AppendLine("- 路线覆盖：安全补给线 4 战、高压封锁线 7 战；计划上限 396 场遭遇，中途失事会提前结束，以控制编辑器回归运行量。");
            report.AppendLine("- 详细日志：每场遭遇输出 `BALANCE_ENCOUNTER`，每局输出 `BALANCE_RESULT`，四类汇总输出 `BALANCE_AGGREGATE`。");
            report.AppendLine();
            report.AppendLine("## 合同胜率");
            report.AppendLine();
            report.AppendLine("| 合同 | 通关胜率 | 平均机体 | 平均货物 | 平均受伤 |");
            report.AppendLine("|---|---:|---:|---:|---:|");
            foreach (IGrouping<CargoContract, RunResult> group in results.GroupBy(result => result.Contract))
            {
                report.AppendLine($"| {ContractLabel(group.Key)} | {WinSummary(group)} | " +
                    $"{group.Average(result => result.Hull):F1} | {group.Average(result => result.Cargo):F1} | " +
                    $"{group.Average(result => result.Damage):F1} |");
            }
            report.AppendLine();
            report.AppendLine("## 构筑胜率");
            report.AppendLine();
            report.AppendLine("| 构筑 | 合同 | 通关胜率 | 平均机体 | 平均货物 | 平均回合 | 平均受伤 | 追踪命中 |");
            report.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|");
            foreach (IGrouping<string, RunResult> group in results.GroupBy(result => result.Name))
            {
                RunResult first = group.First();
                report.AppendLine($"| {group.Key} | {ContractLabel(first.Contract)} | {WinSummary(group)} | " +
                    $"{group.Average(result => result.Hull):F1} | {group.Average(result => result.Cargo):F1} | " +
                    $"{group.Average(result => result.Turns):F1} | {group.Average(result => result.Damage):F1} | " +
                    $"{group.Sum(result => result.TrackingHits)} |");
            }
            report.AppendLine();
            report.AppendLine("## 路线胜率");
            report.AppendLine();
            report.AppendLine("| 路线 | 通关胜率 | 平均机体 | 平均货物 | 平均回合 | 平均受伤 |");
            report.AppendLine("|---|---:|---:|---:|---:|---:|");
            foreach (IGrouping<int, RunResult> group in results.GroupBy(result => result.RouteProfile))
            {
                report.AppendLine($"| {RouteProfileLabel(group.Key)} | {WinSummary(group)} | " +
                    $"{group.Average(result => result.Hull):F1} | {group.Average(result => result.Cargo):F1} | " +
                    $"{group.Average(result => result.Turns):F1} | {group.Average(result => result.Damage):F1} |");
            }
            report.AppendLine();
            report.AppendLine("## Boss 胜率");
            report.AppendLine();
            report.AppendLine("“整局通关”将途中失败计入该首领路线；“首领战胜率”只统计实际到达首领的样本，避免把路线战损误判成首领强度。");
            report.AppendLine();
            report.AppendLine("| Boss | 整局通关胜率 | 到达率 | 首领战胜率 |");
            report.AppendLine("|---|---:|---:|---:|");
            foreach (IGrouping<EnemyKind, RunResult> group in results.GroupBy(result => result.Boss))
            {
                int reached = group.Count(result => result.BossReached);
                int bossVictories = group.Count(result => result.BossVictory);
                report.AppendLine($"| {BossLabel(group.Key)} | {WinSummary(group)} | " +
                    $"{RatioSummary(reached, group.Count())} | {RatioSummary(bossVictories, reached)} |");
            }
            report.AppendLine();
            report.AppendLine("## 回归判断");
            report.AppendLine();
            report.AppendLine(coverageComplete
                ? $"覆盖通过：已生成预期的 {expectedSamples} 局，合同、构筑、路线和两名 Boss 均有独立胜率统计。"
                : $"覆盖失败：预期 {expectedSamples} 局或两名 Boss 全覆盖，实际生成 {results.Count} 局；本报告不可用于回归比较。");
            report.AppendLine(zeroWinBuilds.Length == 0
                ? "未发现跨全部固定种子与路线均为 0 胜的构筑。此结论只表示没有显著功能性断点，不代表数值已经平衡。"
                : $"回归警报：{string.Join("、", zeroWinBuilds)} 在全部固定样本中均为 0 胜；应先安排真人复测定位规则或策略问题，禁止仅为消除警报直接调数。");

            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Playtest_Report_v0.50.md"));
            File.WriteAllText(path, report.ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log($"BALANCE_REPORT_WRITTEN|{path}");
        }

        private static RunResult SimulateRun(string name, IEnumerable<CardId> additions, CargoContract contract,
            int routeProfile, int runSeed, int bossVariant)
        {
            var deck = StarterDeck();
            CardId[] buildCards = additions.ToArray();
            deck.AddRange(buildCards);
            deck.Add(ContractStarter(contract));
            var upgrades = new HashSet<CardId>();
            var modules = new HashSet<ModuleId>();
            int hull = BattleState.MaxPlayerHealth;
            int cargo = 3;
            var result = new RunResult
            {
                Name = name,
                Victory = true,
                RouteProfile = routeProfile,
                Contract = contract,
                Seed = runSeed,
                Boss = BossKindForVariant(bossVariant)
            };
            EncounterId[] encounters = routeProfile == 0
                ? new[] { EncounterId.Skirmish, EncounterId.Skirmish, EncounterId.Elite, EncounterId.Boss }
                : new[] { EncounterId.Skirmish, EncounterId.Elite, EncounterId.Hunt, EncounterId.Hunt,
                    EncounterId.Skirmish, EncounterId.Elite, EncounterId.Boss };
            bool skirmishRewarded = false;
            bool eliteRewarded = false;
            bool huntRewarded = false;

            for (int encounterIndex = 0; encounterIndex < encounters.Length; encounterIndex++)
            {
                EncounterId encounter = encounters[encounterIndex];
                // The safe branch can reach both the mid-route and pre-boss repair docks.
                if (routeProfile == 0 && (encounterIndex == 2 || encounterIndex == 3))
                    hull = Math.Min(BattleState.MaxPlayerHealth, hull + 14);

                int encounterSeed = RunSeedUtility.DeriveEncounterSeed(runSeed,
                    (routeProfile + 1) * 100 + encounterIndex, encounter);
                var state = new BattleState();
                state.StartEncounter(encounter, deck, hull, cargo, contract,
                    upgrades, modules, encounter == EncounterId.Boss ? bossVariant : routeProfile,
                    seed: encounterSeed);
                if (encounter == EncounterId.Boss)
                {
                    result.BossReached = true;
                    EnemyState boss = state.Enemies.FirstOrDefault(enemy =>
                        enemy.Kind == EnemyKind.StormManta || enemy.Kind == EnemyKind.CloudWyrm);
                    if (boss != null)
                        result.Boss = boss.Kind;
                }
                SimulateEncounter(state);
                if (encounter == EncounterId.Boss)
                    result.BossVictory = state.Victory;

                Debug.Log($"BALANCE_ENCOUNTER|{name}|局种子={runSeed}|遭遇种子={encounterSeed}|路线={RouteProfileLabel(routeProfile)}|序号={encounterIndex + 1}|遭遇={encounter}|编队={state.FormationName}|胜利={state.Victory}|机体={state.PlayerHealth}|货物={state.CargoIntegrity}|回合={state.Turn}|出牌={state.CardsPlayed}|受伤={state.DamageTaken}");

                result.Turns += state.Turn;
                result.Cards += state.CardsPlayed;
                result.Damage += state.DamageTaken;
                result.Overheats += state.OverheatCount;
                result.CalamityInterrupts += state.CalamityInterrupts;
                result.CalamityEvades += state.CalamityEvades;
                result.CalamityHits += state.CalamityHits;
                result.TrackingHits += state.TrackingHits;
                hull = state.PlayerHealth;
                cargo = state.CargoIntegrity;

                if (state.Defeat || !state.Victory)
                {
                    result.Victory = false;
                    result.FailedEncounter = encounter;
                    break;
                }

                if (encounter != EncounterId.Boss)
                    hull = Math.Min(BattleState.MaxPlayerHealth,
                        hull + (encounter == EncounterId.Hunt ? 12 : encounter == EncounterId.Elite ? 10 : 6));

                if (encounter == EncounterId.Skirmish)
                {
                    if (!skirmishRewarded)
                    {
                        deck.Add(buildCards[0]);
                        upgrades.Add(buildCards[0]);
                        skirmishRewarded = true;
                    }
                    else
                    {
                        upgrades.Add(CardId.WindGuard);
                    }
                }
                else if (encounter == EncounterId.Elite)
                {
                    if (!eliteRewarded)
                    {
                        deck.Add(buildCards[1]);
                        modules.Add(BuildModule(name));
                        eliteRewarded = true;
                    }
                    else
                    {
                        modules.Add(ModuleId.ExecutionChip);
                    }
                }
                else if (encounter == EncounterId.Hunt)
                {
                    if (!huntRewarded)
                    {
                        deck.Add(buildCards[1]);
                        upgrades.Add(buildCards[1]);
                        huntRewarded = true;
                    }
                    else
                    {
                        upgrades.Add(CardId.WindGuard);
                    }
                }
            }

            result.Hull = hull;
            result.Cargo = cargo;
            return result;
        }

        private static void SimulateEncounter(BattleState state)
        {
            const int turnLimit = 30;
            while (!state.Victory && !state.Defeat && state.Turn <= turnLimit)
            {
                int safety = 0;
                while (safety++ < 12 && TryPlayBestCard(state))
                {
                }
                state.EndTurn();
            }
        }

        private static bool TryPlayBestCard(BattleState state)
        {
            if (state.Hand.Count == 0)
                return false;

            int threat = IncomingThreat(state);
            bool targetInLane = state.Enemies.Any(enemy => enemy.Alive && enemy.Lane == state.PlayerLane);

            if (state.Heat >= 5 && TryPlay(state, CardId.EmergencyCoolant))
                return true;
            if (state.Heat >= 3 && TryPlay(state, CardId.CryoPump))
                return true;
            if (state.Heat >= 3 && TryPlay(state, CardId.ZeroPointCalibration))
                return true;
            if (state.EvasionExposure >= 1 && TryPlay(state, CardId.CounterPursuit))
                return true;
            if (state.EvasionExposure >= 1 && TryPlay(state, CardId.SignalScrambler))
                return true;
            if (state.EvasionExposure >= 1 && TryPlay(state, CardId.AirBrake))
                return true;
            if (threat >= 6 && state.Armor < threat && TryEvade(state, threat))
                return true;
            if (threat >= 6 && state.Armor < threat && TryPlay(state, CardId.WindGuard))
                return true;
            if (threat >= 6 && state.Armor < threat && TryPlay(state, CardId.StandbyField))
                return true;
            if (TryPressureCalamity(state))
                return true;
            if (targetInLane && state.LockOn > 0 && TryPlay(state, CardId.RailPiercer))
                return true;
            if (state.LockOn < 2 && TryPlay(state, CardId.LockCascade))
                return true;
            if (targetInLane && state.Momentum > 0 && TryPlay(state, CardId.PursuitShot))
                return true;
            if (targetInLane && state.Momentum > 0 && TryPlay(state, CardId.SlipstreamStrike))
                return true;
            if (targetInLane && state.LockOn < 2 && TryPlay(state, CardId.TargetLock))
                return true;
            if (targetInLane && state.Armor >= 7 && TryPlay(state, CardId.AegisRam))
                return true;
            if (TryPlay(state, CardId.ReactivePlating))
                return true;
            if (targetInLane && TryPlay(state, CardId.PrismEcho))
                return true;
            if (state.Heat >= 4 && TryPlay(state, CardId.MeltdownBurst))
                return true;
            if (targetInLane && state.Heat <= 2 && TryPlay(state, CardId.FrostLance))
                return true;
            if (targetInLane && state.Heat <= 5 && TryPlay(state, CardId.BurstFire))
                return true;
            if (targetInLane && state.Heat <= 5 && TryPlay(state, CardId.ReserveShot))
                return true;
            if (state.Heat <= 5 && TryPlay(state, CardId.CounterPursuit))
                return true;
            if (targetInLane && state.Heat <= 3 && TryPlay(state, CardId.OverloadAim))
                return true;
            if (state.Heat <= 4 && TryPlay(state, CardId.Scattershot))
                return true;
            if (state.Heat <= 4 && TryPlay(state, CardId.SwarmBeacon))
                return true;
            if (state.Heat <= 4 && TryPlay(state, CardId.MissileSwarm))
                return true;
            if (state.Heat <= 4 && TryPlay(state, CardId.InterceptMine))
                return true;
            if (state.Heat <= 4 && TryPlay(state, CardId.BroadsideVolley))
                return true;
            if (state.Energy >= 3 && TryPlay(state, CardId.TightSchedule))
                return true;

            EnemyState target = state.Enemies.Where(enemy => enemy.Alive).OrderBy(enemy => enemy.Health).FirstOrDefault();
            if (!targetInLane)
            {
                if (target != null && target.Lane < state.PlayerLane && TryPlay(state, CardId.BankUp))
                    return true;
                if (target != null && target.Lane > state.PlayerLane && TryPlay(state, CardId.BankDown))
                    return true;
                if (target != null && state.PlayerLane != 1 && TryPlay(state, CardId.RelayStep))
                    return true;
                if (state.EvasionExposure >= 2 && TryPlay(state, CardId.SignalScrambler))
                    return true;
                if (state.EvasionExposure >= 2 && TryPlay(state, CardId.AirBrake))
                    return true;
            }

            if (state.Heat <= 4 && TryPlay(state, CardId.EngineOverclock))
                return true;
            if (state.Heat <= 3 && state.Energy <= 1 && TryPlay(state, CardId.HeatCharge))
                return true;
            if (state.Heat <= 3 && state.Energy <= 1 && TryPlay(state, CardId.RedlineIgnition))
                return true;
            if (state.EvasionExposure <= 1 && TryPlay(state, CardId.GhostProtocol))
                return true;
            if (state.EvasionExposure == 0 && TryPlay(state, CardId.VectorDash))
                return true;
            if (threat > 0 && TryPlay(state, CardId.WindGuard))
                return true;
            if (threat > 0 && TryPlay(state, CardId.ReserveRouting))
                return true;

            return false;
        }

        private static bool TryPressureCalamity(BattleState state)
        {
            EnemyState drone = state.Enemies.FirstOrDefault(enemy => enemy.Alive &&
                (enemy.Kind == EnemyKind.CalamityDrone || enemy.Kind == EnemyKind.StormManta ||
                    enemy.Kind == EnemyKind.CloudWyrm) && !enemy.ChargeInterrupted &&
                (enemy.Kind == EnemyKind.CloudWyrm
                    ? enemy.ChargeTargetLane != state.PlayerLane
                    : enemy.ChargeTargetLane == state.PlayerLane) &&
                enemy.Lane == state.PlayerLane);
            if (drone == null)
                return false;

            if (state.LockOn > 0 && TryPlay(state, CardId.RailPiercer))
                return true;
            if (state.Momentum > 0 && TryPlay(state, CardId.PursuitShot))
                return true;
            if (state.Armor >= 7 && TryPlay(state, CardId.AegisRam))
                return true;
            if (state.Heat <= 3 && TryPlay(state, CardId.OverloadAim))
                return true;
            if (TryPlay(state, CardId.BurstFire))
                return true;
            if (state.LockOn < 2 && TryPlay(state, CardId.TargetLock))
                return true;
            if (state.Heat <= 2 && TryPlay(state, CardId.FrostLance))
                return true;
            if (state.Heat >= 4 && TryPlay(state, CardId.MeltdownBurst))
                return true;
            if (state.Heat <= 4 && TryPlay(state, CardId.MissileSwarm))
                return true;
            return state.Heat <= 4 && TryPlay(state, CardId.Scattershot);
        }

        private static bool TryEvade(BattleState state, int currentThreat)
        {
            int upThreat = state.PlayerLane > 0 ? ThreatAtLane(state, state.PlayerLane - 1) : int.MaxValue;
            int downThreat = state.PlayerLane < 2 ? ThreatAtLane(state, state.PlayerLane + 1) : int.MaxValue;

            int minimumGain = state.EvasionExposure > 0 ? BattleState.TrackingShotDamage : 1;
            if (upThreat <= downThreat && currentThreat - upThreat >= minimumGain && TryPlay(state, CardId.BankUp))
                return true;
            if (currentThreat - downThreat >= minimumGain && TryPlay(state, CardId.BankDown))
                return true;
            return false;
        }

        private static bool TryPlay(BattleState state, CardId id)
        {
            CardSpec spec = CardLibrary.Get(id);
            if (state.Cargo == CargoContract.SignalSeed && spec.Cost > 0 && spec.Cost >= state.Energy)
                return false;
            for (int i = 0; i < state.Hand.Count; i++)
            {
                if (state.Hand[i] != id || !state.CanPlay(i))
                    continue;
                state.PlayCard(i);
                return true;
            }
            return false;
        }

        private static int IncomingThreat(BattleState state)
        {
            return ThreatAtLane(state, state.PlayerLane);
        }

        private static int ThreatAtLane(BattleState state, int lane)
        {
            int threat = 0;
            foreach (EnemyState enemy in state.Enemies.Where(enemy => enemy.Alive))
            {
                if (enemy.Kind == EnemyKind.CalamityDrone && !enemy.ChargeInterrupted && enemy.ChargeTargetLane == lane)
                    threat += BattleState.CalamityStrikeDamage;
                else if (enemy.Kind == EnemyKind.StormManta && !enemy.ChargeInterrupted)
                {
                    int distance = Math.Abs(enemy.ChargeTargetLane - lane);
                    if (distance == 0)
                        threat += enemy.Phase == 1 ? BattleState.BossPhaseOneStrikeDamage : BattleState.BossPhaseTwoStrikeDamage;
                    else if (enemy.Phase == 2 && distance == 1)
                        threat += BattleState.BossPhaseTwoSplashDamage;
                }
                else if (enemy.Kind == EnemyKind.CloudWyrm && !enemy.ChargeInterrupted &&
                    enemy.ChargeTargetLane >= 0 && enemy.ChargeTargetLane != lane)
                {
                    threat += enemy.Phase == 1
                        ? BattleState.CloudWyrmPhaseOneStrikeDamage
                        : BattleState.CloudWyrmPhaseTwoStrikeDamage;
                }
                else if (enemy.Kind == EnemyKind.StormBalloon)
                    threat += enemy.Damage;
                else if (enemy.Kind == EnemyKind.StormManta && state.Turn % 3 == 0)
                    threat += 5;
                else if (enemy.Lane == lane)
                    threat += enemy.Kind == EnemyKind.MailEater ? enemy.Damage + 2 : enemy.Damage;
            }
            return threat;
        }

        private static List<CardId> StarterDeck()
        {
            return new List<CardId>
            {
                CardId.BurstFire, CardId.BurstFire,
                CardId.BankUp, CardId.BankUp,
                CardId.BankDown, CardId.BankDown,
                CardId.WindGuard, CardId.WindGuard,
                CardId.EmergencyCoolant, CardId.BroadsideVolley,
                CardId.OverloadAim, CardId.EngineOverclock
            };
        }

        private static CardId ContractStarter(CargoContract contract)
        {
            return ContractCatalog.StarterCard(contract);
        }

        private static void LogAggregates(IList<RunResult> results)
        {
            foreach (IGrouping<CargoContract, RunResult> group in results.GroupBy(result => result.Contract))
                Debug.Log($"BALANCE_AGGREGATE|维度=合同|名称={ContractLabel(group.Key)}|胜率={WinSummary(group)}|样本={group.Count()}");
            foreach (IGrouping<string, RunResult> group in results.GroupBy(result => result.Name))
                Debug.Log($"BALANCE_AGGREGATE|维度=构筑|名称={group.Key}|胜率={WinSummary(group)}|样本={group.Count()}");
            foreach (IGrouping<int, RunResult> group in results.GroupBy(result => result.RouteProfile))
                Debug.Log($"BALANCE_AGGREGATE|维度=路线|名称={RouteProfileLabel(group.Key)}|胜率={WinSummary(group)}|样本={group.Count()}");
            foreach (IGrouping<EnemyKind, RunResult> group in results.GroupBy(result => result.Boss))
            {
                int reached = group.Count(result => result.BossReached);
                int bossVictories = group.Count(result => result.BossVictory);
                Debug.Log($"BALANCE_AGGREGATE|维度=Boss|名称={BossLabel(group.Key)}|整局胜率={WinSummary(group)}|到达率={RatioSummary(reached, group.Count())}|首领战胜率={RatioSummary(bossVictories, reached)}|样本={group.Count()}");
            }
        }

        private static string WinSummary(IEnumerable<RunResult> results)
        {
            RunResult[] samples = results.ToArray();
            return RatioSummary(samples.Count(result => result.Victory), samples.Length);
        }

        private static string RatioSummary(int successes, int samples)
        {
            return samples == 0 ? "0/0 (n/a)" : $"{successes}/{samples} ({WinRate(successes, samples):F1}%)";
        }

        private static float WinRate(int victories, int samples)
        {
            return samples == 0 ? 0f : victories * 100f / samples;
        }

        private static string FailureLabel(RunResult result)
        {
            return result.FailedEncounter.HasValue ? result.FailedEncounter.Value.ToString() : "无";
        }

        private static EnemyKind BossKindForVariant(int variant)
        {
            return Math.Abs(variant) % EncounterCatalog.BossVariantCount == 0
                ? EnemyKind.StormManta
                : EnemyKind.CloudWyrm;
        }

        private static string BossLabel(EnemyKind boss)
        {
            return boss == EnemyKind.CloudWyrm ? "雷幕云龙" : "磁暴鳐";
        }

        private static ModuleId BuildModule(string name)
        {
            return name switch
            {
                "矢量追猎" => ModuleId.MomentumFlywheel,
                "护盾冲角" => ModuleId.AegisCapacitor,
                "零度循环" => ModuleId.ZeroPointReactor,
                "熔炉爆发" => ModuleId.RedlineReactor,
                "蜂群弹幕" => ModuleId.SwarmUplink,
                "侧翼雷网" => ModuleId.GhostDecoder,
                "航迹欺骗" => ModuleId.GhostDecoder,
                "余量调度" => ModuleId.ExecutionChip,
                "锁定狙击" => ModuleId.PrecisionMatrix,
                _ => ModuleId.ExecutionChip
            };
        }

        private static string ContractLabel(CargoContract contract)
        {
            return contract switch
            {
                CargoContract.FragileMedicine => "易碎药剂",
                CargoContract.CryoSerum => "零度血清",
                CargoContract.StormCore => "风暴核心",
                CargoContract.BlackBoxRelay => "幽灵黑匣",
                _ => "信标种子"
            };
        }

        private static string RouteProfileLabel(int profile)
        {
            return profile == 0 ? "安全补给线·4战" : "高压封锁线·7战";
        }
    }
}
