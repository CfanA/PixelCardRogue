using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SkyCourier
{
    public enum RunDebriefMistakeCategory
    {
        Unknown,
        DamageControl,
        Positioning,
        TelegraphResponse,
        HeatManagement,
        HandManagement,
        LowTempo,
        ContractExecution
    }

    public enum RunDebriefBuildWeakness
    {
        None,
        MissingSnapshot,
        LowDamage,
        LowSupport,
        BloatedDeck,
        UnderUpgraded,
        MissingModules,
        MissingModification
    }

    [Serializable]
    public sealed class RunDebriefMetrics
    {
        public int Contract = -1;
        public int Encounter = -1;
        public int DefeatSource = -1;
        public string DefeatDealer;
        public int DefeatDamage;
        public int Turns;
        public int CardsPlayed;
        public int DamageTaken;
        public int Overheats;
        public int CalamityInterrupts;
        public int CalamityEvades;
        public int CalamityHits;
        public int TrackingHits;
        public int ContractPassiveProcs;
        public int ContractBonusCredits;
        public int FinalHull;
        public int FinalCargoIntegrity;
        public int FinalCredits;
    }

    [Serializable]
    public sealed class RunDebriefSummary
    {
        public RunDebriefMistakeCategory KeyMistakeCategory;
        public string KeyMistakeMessage;
        public RunDebriefBuildWeakness BuildWeakness;
        public string BuildWeaknessMessage;
        public string NextStrategy;

        public int ValidSnapshotCount;
        public int DeckCount;
        public int DamageCardCount;
        public int SupportCardCount;
        public int DamageCardPercent;
        public int UpgradeCount;
        public int BranchUpgradeCount;
        public int ModuleCount;
        public int AirframeModification;
        public string AirframeModificationName;
        public string BuildSummaryMessage;

        public bool HasRouteComparison;
        public int FirstRouteColumn;
        public int LastRouteColumn;
        public int RouteCreditsDelta;
        public int RouteHullDelta;
        public int RouteCargoIntegrityDelta;
        public int RouteCardsAdded;
        public int RouteCardsRemoved;
        public int RouteUpgradesGained;
        public int RouteBranchChanges;
        public int RouteModulesGained;
        public string RouteGainsMessage;

        public int Turns;
        public int CardsPlayed;
        public int DamageTaken;
        public int Overheats;
        public int CalamityInterrupts;
        public int CalamityEvades;
        public int CalamityHits;
        public int TrackingHits;
        public int ContractPassiveProcs;
        public int ContractBonusCredits;
        public int FinalHull;
        public int FinalCargoIntegrity;
        public int FinalCredits;
    }

    public static class RunDebriefAnalyzer
    {
        private sealed class SnapshotView
        {
            public int RouteColumn;
            public int Hull;
            public int CargoIntegrity;
            public int Credits;
            public AirframeModification Modification;
            public readonly List<CardId> Deck = new List<CardId>();
            public readonly HashSet<CardId> Upgrades = new HashSet<CardId>();
            public readonly Dictionary<CardId, UpgradeBranch> Branches =
                new Dictionary<CardId, UpgradeBranch>();
            public readonly HashSet<ModuleId> Modules = new HashSet<ModuleId>();
        }

        public static RunDebriefSummary Analyze(IReadOnlyList<RunBuildSnapshot> snapshots,
            RunDebriefMetrics metrics)
        {
            RunDebriefMetrics safeMetrics = metrics ?? new RunDebriefMetrics();
            List<SnapshotView> validSnapshots = SanitizeSnapshots(snapshots);
            SnapshotView first = validSnapshots.FirstOrDefault();
            SnapshotView last = validSnapshots.LastOrDefault();

            var summary = new RunDebriefSummary
            {
                ValidSnapshotCount = validSnapshots.Count,
                AirframeModification = (int)AirframeModification.None,
                AirframeModificationName = ModificationName(AirframeModification.None),
                Turns = NonNegative(safeMetrics.Turns),
                CardsPlayed = NonNegative(safeMetrics.CardsPlayed),
                DamageTaken = NonNegative(safeMetrics.DamageTaken),
                Overheats = NonNegative(safeMetrics.Overheats),
                CalamityInterrupts = NonNegative(safeMetrics.CalamityInterrupts),
                CalamityEvades = NonNegative(safeMetrics.CalamityEvades),
                CalamityHits = NonNegative(safeMetrics.CalamityHits),
                TrackingHits = NonNegative(safeMetrics.TrackingHits),
                ContractPassiveProcs = NonNegative(safeMetrics.ContractPassiveProcs),
                ContractBonusCredits = NonNegative(safeMetrics.ContractBonusCredits),
                FinalHull = NonNegative(safeMetrics.FinalHull),
                FinalCargoIntegrity = NonNegative(safeMetrics.FinalCargoIntegrity),
                FinalCredits = NonNegative(safeMetrics.FinalCredits)
            };

            PopulateBuildSummary(summary, last);
            PopulateRouteSummary(summary, first, last);
            summary.KeyMistakeCategory = DetermineMistake(safeMetrics);
            summary.KeyMistakeMessage = MistakeMessage(summary.KeyMistakeCategory, safeMetrics);
            summary.BuildWeakness = DetermineBuildWeakness(summary, last);
            summary.BuildWeaknessMessage = WeaknessMessage(summary.BuildWeakness, summary);
            summary.NextStrategy = StrategyMessage(summary.KeyMistakeCategory, summary.BuildWeakness);
            return summary;
        }

        private static List<SnapshotView> SanitizeSnapshots(IReadOnlyList<RunBuildSnapshot> snapshots)
        {
            var result = new List<SnapshotView>();
            if (snapshots == null)
                return result;

            for (int index = 0; index < snapshots.Count; index++)
            {
                RunBuildSnapshot snapshot = snapshots[index];
                SnapshotView view = SanitizeSnapshot(snapshot);
                if (view != null)
                    result.Add(view);
            }
            return result;
        }

        private static SnapshotView SanitizeSnapshot(RunBuildSnapshot snapshot)
        {
            if (snapshot?.Deck == null)
                return null;

            var view = new SnapshotView
            {
                RouteColumn = NonNegative(snapshot.RouteColumn),
                Hull = NonNegative(snapshot.Hull),
                CargoIntegrity = NonNegative(snapshot.CargoIntegrity),
                Credits = NonNegative(snapshot.Credits),
                Modification = ValidEnum<AirframeModification>(snapshot.AirframeModification)
                    ? (AirframeModification)snapshot.AirframeModification
                    : AirframeModification.None
            };
            foreach (int rawCard in snapshot.Deck)
            {
                if (ValidEnum<CardId>(rawCard))
                    view.Deck.Add((CardId)rawCard);
            }
            if (view.Deck.Count == 0)
                return null;

            var deckCards = new HashSet<CardId>(view.Deck);
            foreach (int rawUpgrade in snapshot.Upgrades ?? Enumerable.Empty<int>())
            {
                if (ValidEnum<CardId>(rawUpgrade) && deckCards.Contains((CardId)rawUpgrade))
                    view.Upgrades.Add((CardId)rawUpgrade);
            }
            foreach (int rawModule in snapshot.Modules ?? Enumerable.Empty<int>())
            {
                if (ValidEnum<ModuleId>(rawModule))
                    view.Modules.Add((ModuleId)rawModule);
            }

            int branchPairCount = Math.Min(snapshot.UpgradeBranchCards?.Count ?? 0,
                snapshot.UpgradeBranches?.Count ?? 0);
            for (int index = 0; index < branchPairCount; index++)
            {
                int rawCard = snapshot.UpgradeBranchCards[index];
                int rawBranch = snapshot.UpgradeBranches[index];
                if (!ValidEnum<CardId>(rawCard) || !ValidEnum<UpgradeBranch>(rawBranch))
                    continue;
                CardId card = (CardId)rawCard;
                if (view.Upgrades.Contains(card) && !view.Branches.ContainsKey(card))
                    view.Branches.Add(card, (UpgradeBranch)rawBranch);
            }
            return view;
        }

        private static void PopulateBuildSummary(RunDebriefSummary summary, SnapshotView latest)
        {
            if (latest == null)
            {
                summary.BuildSummaryMessage = Text("debrief.build.unavailable",
                    "没有可用的构筑快照。");
                return;
            }

            summary.DeckCount = latest.Deck.Count;
            summary.DamageCardCount = latest.Deck.Count(CardPoolCatalog.IsDamageCard);
            summary.SupportCardCount = summary.DeckCount - summary.DamageCardCount;
            summary.DamageCardPercent = summary.DeckCount == 0
                ? 0
                : (summary.DamageCardCount * 100 + summary.DeckCount / 2) / summary.DeckCount;
            summary.UpgradeCount = latest.Upgrades.Count;
            summary.BranchUpgradeCount = latest.Branches.Count;
            summary.ModuleCount = latest.Modules.Count;
            summary.AirframeModification = (int)latest.Modification;
            summary.AirframeModificationName = ModificationName(latest.Modification);
            summary.BuildSummaryMessage = Text("debrief.build.summary",
                "牌组 {0}（攻击 {1} / 支援 {2}）· 升级 {3}（分支 {4}）· 模块 {5} · 改装 {6}",
                summary.DeckCount, summary.DamageCardCount, summary.SupportCardCount,
                summary.UpgradeCount, summary.BranchUpgradeCount, summary.ModuleCount,
                summary.AirframeModificationName);
        }

        private static void PopulateRouteSummary(RunDebriefSummary summary, SnapshotView first, SnapshotView last)
        {
            if (first == null || last == null)
            {
                summary.RouteGainsMessage = Text("debrief.route.unavailable",
                    "没有足够的路线快照可供比较。");
                return;
            }

            summary.FirstRouteColumn = first.RouteColumn;
            summary.LastRouteColumn = last.RouteColumn;
            if (summary.ValidSnapshotCount < 2)
            {
                summary.RouteGainsMessage = Text("debrief.route.unavailable",
                    "没有足够的路线快照可供比较。");
                return;
            }

            summary.HasRouteComparison = true;
            summary.RouteCreditsDelta = last.Credits - first.Credits;
            summary.RouteHullDelta = last.Hull - first.Hull;
            summary.RouteCargoIntegrityDelta = last.CargoIntegrity - first.CargoIntegrity;
            CountDeckChanges(first.Deck, last.Deck, out summary.RouteCardsAdded,
                out summary.RouteCardsRemoved);
            summary.RouteUpgradesGained = last.Upgrades.Count(card => !first.Upgrades.Contains(card));
            summary.RouteModulesGained = last.Modules.Count(module => !first.Modules.Contains(module));
            summary.RouteBranchChanges = last.Branches.Count(entry =>
                !first.Branches.TryGetValue(entry.Key, out UpgradeBranch branch) || branch != entry.Value);
            summary.RouteGainsMessage = Text("debrief.route.summary",
                "路线净变化：邮票 {0} · 机体 {1} · 货物 {2}；入组 {3} / 删牌 {4}，升级 +{5}，模块 +{6}。",
                Signed(summary.RouteCreditsDelta), Signed(summary.RouteHullDelta),
                Signed(summary.RouteCargoIntegrityDelta), summary.RouteCardsAdded,
                summary.RouteCardsRemoved, summary.RouteUpgradesGained, summary.RouteModulesGained);
        }

        private static void CountDeckChanges(IEnumerable<CardId> first, IEnumerable<CardId> last,
            out int added, out int removed)
        {
            Dictionary<CardId, int> before = Counts(first);
            Dictionary<CardId, int> after = Counts(last);
            added = 0;
            removed = 0;
            foreach (CardId card in before.Keys.Concat(after.Keys).Distinct().OrderBy(card => card))
            {
                int difference = after.GetValueOrDefault(card) - before.GetValueOrDefault(card);
                if (difference > 0)
                    added += difference;
                else
                    removed -= difference;
            }
        }

        private static Dictionary<CardId, int> Counts(IEnumerable<CardId> cards)
        {
            var result = new Dictionary<CardId, int>();
            foreach (CardId card in cards ?? Enumerable.Empty<CardId>())
                result[card] = result.GetValueOrDefault(card) + 1;
            return result;
        }

        private static RunDebriefMistakeCategory DetermineMistake(RunDebriefMetrics metrics)
        {
            if (ValidEnum<PlayerDamageSource>(metrics.DefeatSource))
            {
                switch ((PlayerDamageSource)metrics.DefeatSource)
                {
                    case PlayerDamageSource.Overheat:
                    case PlayerDamageSource.HeatSeek:
                    case PlayerDamageSource.BossThermalLock:
                        return RunDebriefMistakeCategory.HeatManagement;
                    case PlayerDamageSource.HandJam:
                    case PlayerDamageSource.BossWidebandJam:
                        return RunDebriefMistakeCategory.HandManagement;
                    case PlayerDamageSource.LaneBlock:
                    case PlayerDamageSource.TrackingShot:
                        return RunDebriefMistakeCategory.Positioning;
                    case PlayerDamageSource.CalamityStrike:
                    case PlayerDamageSource.BossStrike:
                    case PlayerDamageSource.BossSplash:
                    case PlayerDamageSource.BossCurtain:
                    case PlayerDamageSource.PreludeCurtain:
                    case PlayerDamageSource.PreludeMagnet:
                        return RunDebriefMistakeCategory.TelegraphResponse;
                    default:
                        return RunDebriefMistakeCategory.DamageControl;
                }
            }

            if (NonNegative(metrics.Overheats) >= 2)
                return RunDebriefMistakeCategory.HeatManagement;
            if (NonNegative(metrics.CalamityHits) >
                NonNegative(metrics.CalamityInterrupts) + NonNegative(metrics.CalamityEvades))
                return RunDebriefMistakeCategory.TelegraphResponse;
            if (NonNegative(metrics.TrackingHits) >= 2)
                return RunDebriefMistakeCategory.Positioning;
            if (metrics.Turns > 0 && (long)NonNegative(metrics.CardsPlayed) < (long)metrics.Turns * 2)
                return RunDebriefMistakeCategory.LowTempo;
            if (ValidEnum<CargoContract>(metrics.Contract) && metrics.ContractPassiveProcs <= 0)
                return RunDebriefMistakeCategory.ContractExecution;
            return RunDebriefMistakeCategory.Unknown;
        }

        private static string MistakeMessage(RunDebriefMistakeCategory category, RunDebriefMetrics metrics)
        {
            string dealer = string.IsNullOrWhiteSpace(metrics.DefeatDealer)
                ? Text("debrief.threat.unknown", "未知威胁")
                : metrics.DefeatDealer.Trim();
            int finalDamage = NonNegative(metrics.DefeatDamage);
            switch (category)
            {
                case RunDebriefMistakeCategory.HeatManagement:
                    return Text("debrief.mistake.heat",
                        "热量管理失控：累计过热 {0} 次，最后由 {1} 造成 {2} 点机体损失。",
                        NonNegative(metrics.Overheats), dealer, finalDamage);
                case RunDebriefMistakeCategory.HandManagement:
                    return Text("debrief.mistake.hand",
                        "回合末手牌没有降到安全范围，{0} 借此造成了最后 {1} 点损失。",
                        dealer, finalDamage);
                case RunDebriefMistakeCategory.Positioning:
                    return Text("debrief.mistake.position",
                        "航道应对不足：累计承受 {0} 次追踪命中，最后由 {1} 造成 {2} 点损失。",
                        NonNegative(metrics.TrackingHits), dealer, finalDamage);
                case RunDebriefMistakeCategory.TelegraphResponse:
                    return Text("debrief.mistake.telegraph",
                        "没有化解预告攻击：蓄力命中 {0} 次，打断 {1} / 规避 {2}，最后损失 {3} 点机体。",
                        NonNegative(metrics.CalamityHits), NonNegative(metrics.CalamityInterrupts),
                        NonNegative(metrics.CalamityEvades), finalDamage);
                case RunDebriefMistakeCategory.DamageControl:
                    return Text("debrief.mistake.damage",
                        "防护余量不足：全程累计受伤 {0} 点，{1} 造成了最后 {2} 点损失。",
                        NonNegative(metrics.DamageTaken), dealer, finalDamage);
                case RunDebriefMistakeCategory.LowTempo:
                    return Text("debrief.mistake.tempo",
                        "行动效率偏低：{0} 回合只打出 {1} 张牌，威胁累积速度超过了解场速度。",
                        NonNegative(metrics.Turns), NonNegative(metrics.CardsPlayed));
                case RunDebriefMistakeCategory.ContractExecution:
                    return Text("debrief.mistake.contract",
                        "合同机制没有形成循环：整段航程只触发 {0} 次合同被动。",
                        NonNegative(metrics.ContractPassiveProcs));
                default:
                    return Text("debrief.mistake.unknown",
                        "战斗记录不足以锁定单一失误；优先复查最后一回合的敌人意图与资源顺序。");
            }
        }

        private static RunDebriefBuildWeakness DetermineBuildWeakness(RunDebriefSummary summary,
            SnapshotView latest)
        {
            if (latest == null || summary.DeckCount == 0)
                return RunDebriefBuildWeakness.MissingSnapshot;

            int minimumDamage = Math.Max(1, (summary.DeckCount * 3 + 9) / 10);
            int minimumSupport = Math.Max(1, (summary.DeckCount + 3) / 4);
            if (summary.DamageCardCount < minimumDamage)
                return RunDebriefBuildWeakness.LowDamage;
            if (summary.SupportCardCount < minimumSupport)
                return RunDebriefBuildWeakness.LowSupport;
            if (summary.DeckCount >= 18)
                return RunDebriefBuildWeakness.BloatedDeck;
            if (latest.RouteColumn >= 3 && summary.UpgradeCount < Math.Max(1, summary.DeckCount / 8))
                return RunDebriefBuildWeakness.UnderUpgraded;
            if (latest.RouteColumn >= RunStructureCatalog.FinalApproachColumn && summary.ModuleCount == 0)
                return RunDebriefBuildWeakness.MissingModules;
            if (latest.RouteColumn >= RunStructureCatalog.RetrofitColumn &&
                latest.Modification == AirframeModification.None)
                return RunDebriefBuildWeakness.MissingModification;
            return RunDebriefBuildWeakness.None;
        }

        private static string WeaknessMessage(RunDebriefBuildWeakness weakness, RunDebriefSummary summary)
        {
            switch (weakness)
            {
                case RunDebriefBuildWeakness.MissingSnapshot:
                    return Text("debrief.weakness.missing",
                        "构筑快照不完整，无法可靠判断牌组短板。");
                case RunDebriefBuildWeakness.LowDamage:
                    return Text("debrief.weakness.damage",
                        "输出密度不足：{0} 张牌中只有 {1} 张攻击牌，难以在预告窗口内完成打断。",
                        summary.DeckCount, summary.DamageCardCount);
                case RunDebriefBuildWeakness.LowSupport:
                    return Text("debrief.weakness.support",
                        "支援密度不足：{0} 张牌中只有 {1} 张非攻击牌，缺少防护、位移或冷却余量。",
                        summary.DeckCount, summary.SupportCardCount);
                case RunDebriefBuildWeakness.BloatedDeck:
                    return Text("debrief.weakness.bloat",
                        "牌组膨胀到 {0} 张，关键牌和已升级牌的回转速度明显下降。",
                        summary.DeckCount);
                case RunDebriefBuildWeakness.UnderUpgraded:
                    return Text("debrief.weakness.upgrade",
                        "强化投入分散：{0} 张牌组只有 {1} 张升级牌，核心循环缺少明确主轴。",
                        summary.DeckCount, summary.UpgradeCount);
                case RunDebriefBuildWeakness.MissingModules:
                    return Text("debrief.weakness.module",
                        "进入最终航段时仍没有模块，构筑缺少能稳定触发的被动支点。");
                case RunDebriefBuildWeakness.MissingModification:
                    return Text("debrief.weakness.modification",
                        "进入改装航段后仍未确定机体方向，牌组没有围绕明确代价建立循环。");
                default:
                    return Text("debrief.weakness.none",
                        "攻击、支援与强化密度处于合理区间；主要问题来自战斗执行而非构筑数量。");
            }
        }

        private static string StrategyMessage(RunDebriefMistakeCategory mistake,
            RunDebriefBuildWeakness weakness)
        {
            switch (mistake)
            {
                case RunDebriefMistakeCategory.HeatManagement:
                    return Text("debrief.strategy.heat",
                        "新策略：保留至少 2 张主动冷却牌，并在结束回合前把热量压到 4 以下。");
                case RunDebriefMistakeCategory.HandManagement:
                    return Text("debrief.strategy.hand",
                        "新策略：优先低费与弃牌工具，每回合先把手牌降到 4 张再处理输出。");
                case RunDebriefMistakeCategory.Positioning:
                    return Text("debrief.strategy.position",
                        "新策略：每轮保留 1 张位移牌，只在敌人意图确认后决定最终航道。");
                case RunDebriefMistakeCategory.TelegraphResponse:
                    return Text("debrief.strategy.telegraph",
                        "新策略：为预告回合预留位移或爆发，不在前一回合耗尽两种解法。");
            }

            switch (weakness)
            {
                case RunDebriefBuildWeakness.LowDamage:
                    return Text("debrief.strategy.damage",
                        "新策略：接下来两次奖励优先补攻击牌，把攻击占比提高到至少 30%。");
                case RunDebriefBuildWeakness.LowSupport:
                    return Text("debrief.strategy.support",
                        "新策略：先补 2 张防护、位移或冷却牌，再接受新的纯输出奖励。");
                case RunDebriefBuildWeakness.BloatedDeck:
                    return Text("debrief.strategy.bloat",
                        "新策略：把牌组控制在 17 张以内；跳过低协同奖励，并在下一站删掉 1 张基础牌。");
                case RunDebriefBuildWeakness.UnderUpgraded:
                    return Text("debrief.strategy.upgrade",
                        "新策略：暂停扩牌，先把两张核心牌升级并为其中一张确定 A/B 分支。");
                case RunDebriefBuildWeakness.MissingModules:
                    return Text("debrief.strategy.module",
                        "新策略：在最终航段前锁定 1 个能被牌组稳定触发的模块。");
                case RunDebriefBuildWeakness.MissingModification:
                    return Text("debrief.strategy.modification",
                        "新策略：到达改装点时立即选定机体方向，之后只拿能覆盖其代价的牌。");
                case RunDebriefBuildWeakness.MissingSnapshot:
                    return Text("debrief.strategy.review",
                        "新策略：先复查最后一回合敌人意图，再以同种子重开验证单一调整。");
            }

            if (mistake == RunDebriefMistakeCategory.LowTempo)
                return Text("debrief.strategy.tempo",
                    "新策略：降低平均费用，确保普通回合至少能打出 2 张有效牌。");
            if (mistake == RunDebriefMistakeCategory.ContractExecution)
                return Text("debrief.strategy.contract",
                    "新策略：前两次选牌只选择能直接触发当前合同被动的卡牌。");
            return Text("debrief.strategy.defense",
                "新策略：每回合先覆盖已预告伤害，再把剩余能量投入输出。");
        }

        private static string ModificationName(AirframeModification modification)
        {
            switch (modification)
            {
                case AirframeModification.SealedBulkhead:
                    return Text("debrief.modification.sealed_bulkhead", "密封隔舱");
                case AirframeModification.OpenAvionics:
                    return Text("debrief.modification.open_avionics", "开放航电");
                case AirframeModification.RedlineTurbine:
                    return Text("debrief.modification.redline_turbine", "红线涡轮");
                default:
                    return Text("debrief.modification.none", "未改装");
            }
        }

        private static string Signed(int value)
        {
            return value > 0
                ? "+" + value.ToString(CultureInfo.InvariantCulture)
                : value.ToString(CultureInfo.InvariantCulture);
        }

        private static int NonNegative(int value) => Math.Max(0, value);

        private static bool ValidEnum<T>(int value) where T : struct, Enum =>
            Enum.IsDefined(typeof(T), value);

        private static string Text(string key, string fallback, params object[] arguments) =>
            LocalizationService.Text(key, fallback, arguments);
    }
}
