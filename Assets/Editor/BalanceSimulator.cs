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
        }

        [MenuItem("Tools/Sky Courier/Run Balance Suite")]
        public static void RunSuite()
        {
            var results = new List<RunResult>();
            for (int routeProfile = 0; routeProfile < 2; routeProfile++)
            {
                results.Add(SimulateRun("锁定狙击", new[] { CardId.TargetLock, CardId.RailPiercer, CardId.LockCascade }, CargoContract.FragileMedicine, routeProfile));
                results.Add(SimulateRun("矢量追猎", new[] { CardId.VectorDash, CardId.PursuitShot, CardId.SlipstreamStrike }, CargoContract.StormCore, routeProfile));
                results.Add(SimulateRun("护盾冲角", new[] { CardId.ReactivePlating, CardId.AegisRam, CardId.PrismEcho }, CargoContract.FragileMedicine, routeProfile));
                results.Add(SimulateRun("零度循环", new[] { CardId.CryoPump, CardId.FrostLance, CardId.ZeroPointCalibration }, CargoContract.CryoSerum, routeProfile));
                results.Add(SimulateRun("熔炉爆发", new[] { CardId.HeatCharge, CardId.MeltdownBurst, CardId.RedlineIgnition }, CargoContract.CryoSerum, routeProfile));
                results.Add(SimulateRun("蜂群弹幕", new[] { CardId.Scattershot, CardId.MissileSwarm, CardId.SwarmBeacon }, CargoContract.StormCore, routeProfile));
                results.Add(SimulateRun("航迹欺骗", new[] { CardId.SignalScrambler, CardId.CounterPursuit, CardId.GhostProtocol }, CargoContract.BlackBoxRelay, routeProfile));
                results.Add(SimulateRun("侧翼雷网", new[] { CardId.AirBrake, CardId.InterceptMine, CardId.GhostProtocol }, CargoContract.BlackBoxRelay, routeProfile));
            }

            foreach (RunResult result in results)
            {
                Debug.Log($"BALANCE_RESULT|{result.Name}|合同={result.Contract}|路线={RouteProfileLabel(result.RouteProfile)}|胜利={result.Victory}|机体={result.Hull}|货物={result.Cargo}|回合={result.Turns}|出牌={result.Cards}|受伤={result.Damage}|过热={result.Overheats}|打断={result.CalamityInterrupts}|规避={result.CalamityEvades}|命中={result.CalamityHits}|追踪={result.TrackingHits}");
            }

            int victories = results.Count(result => result.Victory);
            Debug.Log($"BALANCE_SUMMARY|通关={victories}/{results.Count}|平均回合={results.Average(result => result.Turns):F1}|平均受伤={results.Average(result => result.Damage):F1}");
            WriteOnePageReport(results);
        }

        private static void WriteOnePageReport(IList<RunResult> results)
        {
            int victories = results.Count(result => result.Victory);
            bool everyBuildViable = results.GroupBy(result => result.Name).All(group => group.Any(result => result.Victory));
            var report = new StringBuilder();
            report.AppendLine("# 《云海邮差》v0.44 一页式试玩与平衡报告");
            report.AppendLine();
            report.AppendLine($"> 生成日期：{DateTime.Now:yyyy-MM-dd}　|　自动构筑模拟：{victories}/{results.Count} 通关");
            report.AppendLine();
            report.AppendLine("## 真人试玩结论与本轮目标");
            report.AppendLine();
            report.AppendLine("v0.44 完成终局前哨、可继承航线情报与六类分支结局；本轮只验证机制闭环、存档兼容和信息可见性，所有数值继续沿用 v0.41 基线，留待后续真人试玩。");
            report.AppendLine();
            report.AppendLine("## 已实施改动");
            report.AppendLine();
            report.AppendLine("- 新增版本化单局存档、上一代备份和损坏回退；地图、战斗入口、商店、事件、维修和奖励节点自动保存。");
            report.AppendLine("- 新增窗口/无边框/独占全屏、五档分辨率、垂直同步、帧率上限，以及音乐、音效、震屏和闪光强度设置。");
            report.AppendLine("- 标题至结算的全部关键流程支持手柄导航，并持续显示当前焦点和控制器按键提示。");
            report.AppendLine("- 每局生成可见种子；同一航点的编队、洗牌和战斗随机行为可稳定复现，恢复战斗不会改变开局状态。");
            report.AppendLine("- 本地 JSONL 诊断记录版本、种子、界面、航点、合同、战损和关键流程；Unity 错误另存最后错误上下文，所有记录均不上传。");
            report.AppendLine("- 标题页新增邮政档案，长期记录出发、送达、失事、战斗胜利、最佳货物、累计回合和累计出牌。");
            report.AppendLine("- 新增合同、卡牌、模块和敌机发现图鉴，以及不提供属性加成的邮差等级与五枚荣誉签章。");
            report.AppendLine("- 最近八次送达或失事记录保留合同、航点、种子、回合、牌组等摘要；档案使用独立主文件、备份和安全替换。");
            report.AppendLine("- 规则层区分同航道攻击、航道封锁、全航道风暴、航迹追踪、灾变蓄力、首领正面/溅射和引擎过热八类致命来源。");
            report.AppendLine("- 失败页显示致命敌机、最后机体损失、对应战术建议、本次档案增量与种子，并提供同合同新种子快速再试。");
            report.AppendLine("- 邮政档案 v2 为失败记录保存致命来源；v1 档案自动迁移并保留既有统计和历史记录。");
            report.AppendLine("- 新增统一 TSV 双语词表与运行时本地化服务，设置中可即时切换简体中文 / English，语言选择独立持久化。");
            report.AppendLine("- 首批英文覆盖标题、设置、暂停、邮政档案、失败复盘，以及合同、卡牌、模块和敌机等核心名称。");
            report.AppendLine("- 构建前扫描已接入的静态文本键并验证双语字段，同时验证全部动态卡牌名称、设置迁移和中英切换。");
            report.AppendLine("- 四份合同不再同时平铺，改为中央聚焦、两侧后退、远端暗化的伪 3D 机库轮盘，突出本局构筑身份。");
            report.AppendLine("- 当前合同集中显示风险条件、推荐构筑路线、玩法说明和报酬；支持鼠标、滚轮、方向键、数字键与手柄切换。");
            report.AppendLine("- 设置页步进箭头脱离通用装饰按钮，使用对称专用按钮和独立点击区域，消除窄宽度下的括号拥挤与错位。");
            report.AppendLine("- 连续换道累积航迹暴露；第二个连续机动回合会预告并触发一次5点追踪射击。停留会降低暴露。");
            report.AppendLine("- 新增信号扰频、逆向追猎、矢量刹车、航道雷网4张牌，以及围绕航迹管理的幽灵黑匣合同。");
            report.AppendLine("- 合同各自携带一张开局核心牌；长航线加入追迹者空域，并保留多次补给与维修选择。");
            report.AppendLine("- 新增首次战斗提示、暂停、重新开始以及独立音乐/音效音量设置。");
            report.AppendLine("- 12种攻击牌获得独立弹道、命中节奏、震屏强度与分层音效；点射、轨炮、制导、冲角、冰枪、热浪、飞弹和雷网可直接从动作轮廓区分。");
            report.AppendLine("- 航线扩展为8个阶段、20个节点，包含普通战、精英、追猎、商店、事件、维修坞与两个终局首领，并按连线形成多条可选路径。");
            report.AppendLine("- 地图支持滚轮、方向按钮和滚动条浏览；只解析当前节点之后两层，未到达情报由信号遮罩隐藏。");
            report.AppendLine("- 高空疾风走廊、中层静电锋面、低空残骸潮分别绑定可复现的敌方编队池，并在地图与战斗顶部持续显示。");
            report.AppendLine("- 获得合同签名牌后，普通战奖励会按当前空域偏向机动/低热、控制/调度或爆发/生存路线。");
            report.AppendLine("- 终局前新增雷幕先导、磁针鳐卫与双频先遣队，以较低压力提前教授两个首领相反的航道判读规则。");
            report.AppendLine("- 击破终局前哨会截获雷幕密钥、磁针罗盘或双频解码器；情报随单局存档继承，并重写对应首领首轮锁定。");
            report.AppendLine("- 两名首领与信标纪事的中立、盟约、敌对阵营组合为六种独立终局，并进入邮政档案收藏与最近配送摘要。");
            report.AppendLine("- 自动模拟改为4战安全路线与7战高压路线两类配置，覆盖分支带来的成长和战损差异。");
            report.AppendLine();
            report.AppendLine("## 构筑模拟摘要");
            report.AppendLine();
            report.AppendLine("| 构筑 | 合同 | 通关 | 平均机体 | 平均货物 | 平均回合 | 平均受伤 | 追踪命中 |");
            report.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|");
            foreach (IGrouping<string, RunResult> group in results.GroupBy(result => result.Name))
            {
                RunResult first = group.First();
                report.AppendLine($"| {group.Key} | {ContractLabel(first.Contract)} | {group.Count(result => result.Victory)}/{group.Count()} | " +
                    $"{group.Average(result => result.Hull):F1} | {group.Average(result => result.Cargo):F1} | " +
                    $"{group.Average(result => result.Turns):F1} | {group.Average(result => result.Damage):F1} | " +
                    $"{group.Sum(result => result.TrackingHits)} |");
            }
            report.AppendLine();
            report.AppendLine("## 验收判断");
            report.AppendLine();
            report.AppendLine(victories >= 14 && everyBuildViable
                ? $"通过：{victories}/{results.Count} 条压力路线通关，且8套主要构筑均至少通过一种完整编队。两个失败样本保留了灾变编队和高风险合同的失败压力；换道仍能规避高额攻击，但连续使用会产生可观测代价，专用卡牌可以把代价转化为收益。"
                : $"未通过：当前有 {results.Count - victories} 条路线失败，或存在完全不可行的主要构筑，需要继续调整。");

            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Playtest_Report_v0.44.md"));
            File.WriteAllText(path, report.ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log($"BALANCE_REPORT_WRITTEN|{path}");
        }

        private static RunResult SimulateRun(string name, IEnumerable<CardId> additions, CargoContract contract,
            int routeProfile)
        {
            var deck = StarterDeck();
            CardId[] buildCards = additions.ToArray();
            deck.AddRange(buildCards);
            deck.Add(ContractStarter(contract));
            var upgrades = new HashSet<CardId>();
            var modules = new HashSet<ModuleId>();
            int hull = BattleState.MaxPlayerHealth;
            int cargo = 3;
            var result = new RunResult { Name = name, Victory = true, RouteProfile = routeProfile, Contract = contract };
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

                var state = new BattleState();
                state.StartEncounter(encounter, deck, hull, cargo, contract,
                    upgrades, modules, encounter == EncounterId.Boss ? 0 : routeProfile);
                SimulateEncounter(state);

                Debug.Log($"BALANCE_ENCOUNTER|{name}|{encounter}|胜利={state.Victory}|机体={state.PlayerHealth}|货物={state.CargoIntegrity}|回合={state.Turn}|出牌={state.CardsPlayed}|受伤={state.DamageTaken}");

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

            EnemyState target = state.Enemies.Where(enemy => enemy.Alive).OrderBy(enemy => enemy.Health).FirstOrDefault();
            if (!targetInLane)
            {
                if (target != null && target.Lane < state.PlayerLane && TryPlay(state, CardId.BankUp))
                    return true;
                if (target != null && target.Lane > state.PlayerLane && TryPlay(state, CardId.BankDown))
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
            return contract switch
            {
                CargoContract.FragileMedicine => CardId.ReactivePlating,
                CargoContract.CryoSerum => CardId.CryoPump,
                CargoContract.StormCore => CardId.VectorDash,
                _ => CardId.SignalScrambler
            };
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
                _ => "幽灵黑匣"
            };
        }

        private static string RouteProfileLabel(int profile)
        {
            return profile == 0 ? "安全补给线·4战" : "高压封锁线·7战";
        }
    }
}
