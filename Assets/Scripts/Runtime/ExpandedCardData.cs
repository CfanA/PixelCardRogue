using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyCourier
{
    public enum CardTargetRequirement
    {
        None,
        SameLane,
        AnyEnemy,
        OtherLane
    }

    public static class ExpandedCardCatalog
    {
        public static bool TryGet(CardId id, out CardSpec spec)
        {
            spec = id switch
            {
                CardId.ThermalBarrier => Card(id, "热障转换", "降低最多3点热量；每实际降低1点，获得2点护盾。", 1, 0, CardFamily.Defense),
                CardId.CapacitorDump => Card(id, "电容卸载", "消耗4点护盾，获得1点能量与1层锁定。", 0, 1, CardFamily.Utility),
                CardId.KineticBroadside => Card(id, "动能齐射", "对所有敌人造成2点加当前动量的伤害，不消耗动量。", 2, 2, CardFamily.Weapon),
                CardId.TracerSwarm => Card(id, "标定蜂群", "发射4枚2点飞弹；若有锁定，消耗1层并额外发射2枚。", 2, 2, CardFamily.Weapon),
                CardId.QueueDirective => Card(id, "队列指令", "消耗1层锁定并抽2张牌；支付后剩1点能量时再获得4点护盾。", 1, 0, CardFamily.Utility),
                CardId.EmergencySort => Card(id, "紧急分拣", "弃掉其余手牌，再抽取相同数量加1张牌。消耗。", 0, 0, CardFamily.Utility),
                CardId.HoldFormation => Card(id, "保持编队", "获得4点护盾与1层航迹暴露；本回合结束时保留其余手牌。", 1, 0, CardFamily.Defense),
                CardId.ArmorySearch => Card(id, "武库检索", "从抽牌堆检索1张武器牌；没有武器牌时改为抽1张。", 1, 1, CardFamily.Utility),

                CardId.AblativeFoam => Card(id, "烧蚀泡沫", "获得3点护盾；此前没有护盾时再获得3点。", 0, 0, CardFamily.Defense),
                CardId.PrecisionSeal => Card(id, "精密密封", "获得5点护盾与1层锁定。", 1, 0, CardFamily.Defense),
                CardId.LockBastion => Card(id, "锁定壁垒", "获得5点护盾；每层锁定再获得4点，随后消耗全部锁定。", 1, 0, CardFamily.Defense),
                CardId.MirrorPlating => Card(id, "镜面装甲", "获得6点护盾，并对同航道造成当前护盾一半的伤害。", 1, 1, CardFamily.Defense),
                CardId.BulkheadPulse => Card(id, "舱壁脉冲", "对所有敌人造成当前护盾三分之一的伤害，至少造成2点。", 2, 1, CardFamily.Weapon),
                CardId.CompressionRam => Card(id, "压缩冲角", "对同航道造成6点加当前护盾的伤害，随后保留一半护盾。", 2, 2, CardFamily.Weapon),
                CardId.CargoScreen => Card(id, "货舱屏障", "获得12点护盾；若这是手中最后一张牌，抽1张牌。", 2, 0, CardFamily.Defense),
                CardId.ImpactLedger => Card(id, "冲击账本", "拥有至少8点护盾时抽2张牌；否则抽1张并获得1层锁定。", 1, 0, CardFamily.Utility),
                CardId.SealantRecycle => Card(id, "密封回收", "消耗最多6点护盾并降低等量热量；消耗满6点时抽1张牌。", 0, 0, CardFamily.Utility),
                CardId.BraceForImpact => Card(id, "抗冲击姿态", "获得8点护盾；手中仍有防御牌时再获得3点。", 1, 0, CardFamily.Defense),
                CardId.ParcelAegis => Card(id, "邮包神盾", "获得18点护盾；若有锁定，消耗1层并抽2张牌。", 3, 0, CardFamily.Defense),
                CardId.LastStandCourier => Card(id, "末班邮差", "追踪最低耐久敌人造成10点伤害；机体不高于一半时额外造成8点。", 2, 2, CardFamily.Weapon),

                CardId.FlashFreeze => Card(id, "闪速冻结", "降低2点热量；若热量降至0，获得5点护盾。", 0, 0, CardFamily.Utility),
                CardId.ThermalBattery => Card(id, "热能电池", "降低最多3点热量；实际降低至少2点时获得1点能量。", 0, 0, CardFamily.Utility),
                CardId.SuperheatedCoolant => Card(id, "过热冷媒", "抽2张牌；出牌前热量至少4点时再降低2点热量。", 0, 2, CardFamily.Utility),
                CardId.HeatSinkLance => Card(id, "热沉长枪", "对同航道造成6点伤害并降低2点热量；实际降温时额外造成4点。", 1, 1, CardFamily.Weapon),
                CardId.QuenchVolley => Card(id, "淬冷齐射", "对所有敌人造成3点伤害并降低2点热量。", 2, 1, CardFamily.Weapon),
                CardId.BoiloffArmor => Card(id, "沸腾装甲", "获得4点加当前热量的护盾，随后降低1点热量。", 1, 0, CardFamily.Defense),
                CardId.ColdStart => Card(id, "冷启动", "热量为0时获得1点能量并抽1张牌；否则降低2点热量。", 0, 0, CardFamily.Utility),
                CardId.IgnitionLoop => Card(id, "点火循环", "获得2点能量；结算后热量至少5点时抽1张牌。", 1, 2, CardFamily.Utility),
                CardId.ReactorPurge => Card(id, "炉心排空", "追踪最低耐久敌人造成4点加热量两倍的伤害，然后清空热量。", 2, 0, CardFamily.Weapon),
                CardId.WhiteoutProtocol => Card(id, "白障协议", "低热时获得8点护盾；否则获得4点护盾并降低2点热量。", 1, 0, CardFamily.Defense),
                CardId.FurnaceWake => Card(id, "炉心尾焰", "对同航道造成7点伤害；出牌前热量至少4点时额外造成6点。", 1, 2, CardFamily.Weapon),
                CardId.AbsoluteZero => Card(id, "绝对零度", "对所有敌人造成8点伤害；出牌前热量为0时改为14点。", 3, 0, CardFamily.Weapon),

                CardId.CrosswindCut => Card(id, "侧风切割", "切换至对侧外航道，再对新航道造成6点伤害。", 1, 1, CardFamily.Maneuver),
                CardId.MomentumGuard => Card(id, "动量护航", "获得4点加每层动量3点的护盾。", 1, 0, CardFamily.Defense),
                CardId.DriftFire => Card(id, "漂移射击", "对同航道造成5点加每层动量3点的伤害，不消耗动量。", 1, 1, CardFamily.Weapon),
                CardId.VectorLoop => Card(id, "矢量回环", "切换至对侧外航道，获得1层动量并抽1张牌。", 1, 0, CardFamily.Maneuver),
                CardId.TailwindCharge => Card(id, "顺风充能", "消耗1层动量，获得1点能量并抽1张牌。", 0, 1, CardFamily.Utility),
                CardId.SpiralBarrage => Card(id, "螺旋弹幕", "对所有敌人造成2点加每层动量2点的伤害，随后清空动量。", 2, 2, CardFamily.Weapon),
                CardId.SnapRoll => Card(id, "急滚规避", "切换至对侧外航道，降低1层航迹暴露并获得2点护盾。", 0, 0, CardFamily.Maneuver),
                CardId.WakeMine => Card(id, "尾流雷障", "对其他航道的敌人造成4点加当前动量的伤害，不消耗动量。", 1, 1, CardFamily.Weapon),
                CardId.PursuitVector => Card(id, "追击矢量", "进入最低耐久敌人所在航道，并获得1层动量。", 1, 0, CardFamily.Maneuver),
                CardId.GaleBreak => Card(id, "破风冲刺", "追踪最低耐久敌人造成8点加每层动量5点的伤害，随后清空动量。", 2, 2, CardFamily.Weapon),
                CardId.StormOrbit => Card(id, "风暴环航", "获得1层动量与1层航迹暴露并抽1张牌；本回合保留其余手牌。", 1, 1, CardFamily.Utility),
                CardId.TerminalDive => Card(id, "终端俯冲", "对所有敌人造成5点加每层动量5点的伤害，随后清空动量。", 3, 3, CardFamily.Weapon),

                CardId.TraceHarvest => Card(id, "航迹采收", "降低1层航迹暴露；若成功，获得1点能量。", 0, 0, CardFamily.Utility),
                CardId.ShadowLock => Card(id, "暗影标定", "获得1层航迹暴露与2层锁定。", 1, 1, CardFamily.Utility),
                CardId.DecoyPacket => Card(id, "诱饵数据", "获得1层航迹暴露并抽2张牌。", 0, 1, CardFamily.Utility),
                CardId.SilentBurst => Card(id, "静默爆破", "追踪最低耐久敌人造成8点伤害；没有暴露时额外造成6点。", 1, 1, CardFamily.Weapon),
                CardId.BroadcastMine => Card(id, "广播雷网", "对其他航道的敌人造成5点加每层暴露2点的伤害。", 1, 2, CardFamily.Weapon),
                CardId.GhostShield => Card(id, "幽灵护盾", "清除全部航迹暴露，获得4点加每层清除量4点的护盾。", 1, 0, CardFamily.Defense),
                CardId.SignalLeech => Card(id, "信号窃取", "消耗1层锁定，获得1点能量并抽1张牌。", 1, 0, CardFamily.Utility),
                CardId.BlindSpot => Card(id, "盲区穿行", "切换至对侧外航道，降低1层航迹暴露并获得4点护盾。", 1, 0, CardFamily.Maneuver),
                CardId.CounterSignal => Card(id, "反制信号", "追踪最低耐久敌人造成6点加每层暴露4点的伤害，结算后保留1层暴露。", 1, 1, CardFamily.Weapon),
                CardId.BlackoutVolley => Card(id, "黑障齐射", "对所有敌人造成2点加每层暴露3点的伤害，随后清除全部暴露。", 2, 2, CardFamily.Weapon),
                CardId.DeadDrop => Card(id, "死信投递", "获得1层锁定并抽1张牌。消耗。", 0, 0, CardFamily.Utility),
                CardId.ZeroSignature => Card(id, "零特征", "追踪最低耐久敌人；无暴露时造成18点，否则造成10点并清除暴露。", 3, 2, CardFamily.Weapon),

                CardId.OnePointPlan => Card(id, "一点计划", "剩余1点能量时抽2张牌；否则抽1张。", 0, 0, CardFamily.Utility),
                CardId.ReserveCapacitor => Card(id, "余量电容", "获得5点护盾；支付后剩1点能量时再获得1层锁定。", 1, 0, CardFamily.Defense),
                CardId.ScheduledShot => Card(id, "排程射击", "对同航道造成6点伤害；支付后剩1点能量时额外造成3点。", 1, 1, CardFamily.Weapon),
                CardId.DispatchLoop => Card(id, "派送循环", "抽1张牌；支付后剩1点能量时改为抽2张。", 1, 0, CardFamily.Utility),
                CardId.LockVoucher => Card(id, "锁定凭单", "消耗1层锁定并抽1张牌；支付后剩1点能量时获得1点能量并再抽1张。", 1, 0, CardFamily.Utility),
                CardId.DeferredVolley => Card(id, "延后齐射", "对所有敌人造成3点伤害；支付后剩1点能量时改为5点。", 2, 1, CardFamily.Weapon),
                CardId.BudgetThruster => Card(id, "预算推进", "切换至对侧外航道并获得3点护盾；支付后剩1点能量时返还1点。", 1, 0, CardFamily.Maneuver),
                CardId.SpareChannel => Card(id, "备用信道", "抽1张牌；支付后剩1点能量时，本回合保留其余手牌。", 1, 0, CardFamily.Utility),
                CardId.ExactChange => Card(id, "精确找零", "将超过1点的能量全部转化为护盾，每转化1点获得4点护盾。", 0, 0, CardFamily.Utility),
                CardId.QueueCollapse => Card(id, "队列坍缩", "追踪最低耐久敌人，手牌越多伤害越高；支付后剩1点能量时抽1张。", 2, 2, CardFamily.Weapon),
                CardId.FinalAllocation => Card(id, "最终分配", "获得6点加支付后剩余能量四倍的护盾。", 2, 0, CardFamily.Defense),
                CardId.PostalOverdrive => Card(id, "邮路超频", "追踪最低耐久敌人造成18点伤害；支付后剩1点能量时额外造成8点。", 3, 3, CardFamily.Weapon),
                _ => null
            };
            return spec != null;
        }

        public static bool Contains(CardId id) => id >= CardId.ThermalBarrier;

        public static bool IsDamaging(CardId id)
        {
            return id switch
            {
                CardId.KineticBroadside or CardId.TracerSwarm or
                CardId.MirrorPlating or CardId.BulkheadPulse or CardId.CompressionRam or CardId.LastStandCourier or
                CardId.HeatSinkLance or CardId.QuenchVolley or CardId.ReactorPurge or CardId.FurnaceWake or CardId.AbsoluteZero or
                CardId.CrosswindCut or CardId.DriftFire or CardId.SpiralBarrage or CardId.WakeMine or CardId.GaleBreak or CardId.TerminalDive or
                CardId.SilentBurst or CardId.BroadcastMine or CardId.CounterSignal or CardId.BlackoutVolley or CardId.ZeroSignature or
                CardId.ScheduledShot or CardId.DeferredVolley or CardId.QueueCollapse or CardId.PostalOverdrive => true,
                _ => false
            };
        }

        public static CardTargetRequirement TargetRequirement(CardId id)
        {
            return id switch
            {
                CardId.MirrorPlating or CardId.CompressionRam or CardId.HeatSinkLance or CardId.FurnaceWake or
                CardId.DriftFire or CardId.ScheduledShot => CardTargetRequirement.SameLane,
                CardId.WakeMine or CardId.BroadcastMine => CardTargetRequirement.OtherLane,
                CardId.LastStandCourier or CardId.ReactorPurge or CardId.PursuitVector or CardId.GaleBreak or
                CardId.SilentBurst or CardId.CounterSignal or CardId.ZeroSignature or CardId.QueueCollapse or
                CardId.PostalOverdrive => CardTargetRequirement.AnyEnemy,
                _ => CardTargetRequirement.None
            };
        }

        public static bool ExhaustsOnPlay(CardId id) =>
            id == CardId.EmergencySort || id == CardId.DeadDrop;

        public static bool CyclesRemainingHand(CardId id) => id == CardId.EmergencySort;

        public static bool IsVolley(CardId id)
        {
            return id == CardId.KineticBroadside || id == CardId.TracerSwarm ||
                id == CardId.BulkheadPulse || id == CardId.QuenchVolley || id == CardId.AbsoluteZero ||
                id == CardId.SpiralBarrage || id == CardId.WakeMine || id == CardId.TerminalDive ||
                id == CardId.BroadcastMine || id == CardId.BlackoutVolley ||
                id == CardId.DeferredVolley;
        }

        public static bool IsCooling(CardId id)
        {
            return id == CardId.ThermalBarrier || id == CardId.SealantRecycle ||
                id == CardId.FlashFreeze || id == CardId.ThermalBattery ||
                id == CardId.SuperheatedCoolant || id == CardId.HeatSinkLance ||
                id == CardId.QuenchVolley || id == CardId.BoiloffArmor ||
                id == CardId.ColdStart || id == CardId.ReactorPurge ||
                id == CardId.WhiteoutProtocol;
        }

        private static CardSpec Card(CardId id, string name, string rules, int cost, int heat, CardFamily family)
        {
            return new CardSpec(id, name, rules, cost, heat, family);
        }
    }

    public static class CardPoolCatalog
    {
        public const int TotalCardTypes = 108;

        private static readonly CardId[] Shared =
        {
            CardId.BurstFire, CardId.BankUp, CardId.BankDown, CardId.EmergencyCoolant,
            CardId.BroadsideVolley, CardId.WindGuard, CardId.OverloadAim, CardId.EngineOverclock,
            CardId.ThermalBarrier, CardId.CapacitorDump, CardId.KineticBroadside, CardId.TracerSwarm,
            CardId.QueueDirective, CardId.EmergencySort, CardId.HoldFormation, CardId.ArmorySearch
        };

        private static readonly Dictionary<CargoContract, CardId[]> ContractCards = new()
        {
            [CargoContract.FragileMedicine] = new[]
            {
                CardId.TargetLock, CardId.RailPiercer, CardId.ReactivePlating, CardId.AegisRam,
                CardId.LockCascade, CardId.PrismEcho, CardId.ReactiveSeal,
                CardId.AblativeFoam, CardId.PrecisionSeal, CardId.LockBastion, CardId.MirrorPlating,
                CardId.BulkheadPulse, CardId.CompressionRam, CardId.CargoScreen, CardId.ImpactLedger,
                CardId.SealantRecycle, CardId.BraceForImpact, CardId.ParcelAegis, CardId.LastStandCourier
            },
            [CargoContract.CryoSerum] = new[]
            {
                CardId.CryoPump, CardId.FrostLance, CardId.HeatCharge, CardId.MeltdownBurst,
                CardId.ZeroPointCalibration, CardId.RedlineIgnition, CardId.PhaseExchange,
                CardId.FlashFreeze, CardId.ThermalBattery, CardId.SuperheatedCoolant, CardId.HeatSinkLance,
                CardId.QuenchVolley, CardId.BoiloffArmor, CardId.ColdStart, CardId.IgnitionLoop,
                CardId.ReactorPurge, CardId.WhiteoutProtocol, CardId.FurnaceWake, CardId.AbsoluteZero
            },
            [CargoContract.StormCore] = new[]
            {
                CardId.VectorDash, CardId.PursuitShot, CardId.Scattershot, CardId.MissileSwarm,
                CardId.SlipstreamStrike, CardId.SwarmBeacon, CardId.EyeTransit,
                CardId.CrosswindCut, CardId.MomentumGuard, CardId.DriftFire, CardId.VectorLoop,
                CardId.TailwindCharge, CardId.SpiralBarrage, CardId.SnapRoll, CardId.WakeMine,
                CardId.PursuitVector, CardId.GaleBreak, CardId.StormOrbit, CardId.TerminalDive
            },
            [CargoContract.BlackBoxRelay] = new[]
            {
                CardId.SignalScrambler, CardId.CounterPursuit, CardId.AirBrake, CardId.InterceptMine,
                CardId.GhostProtocol, CardId.FalseTelemetry,
                CardId.TraceHarvest, CardId.ShadowLock, CardId.DecoyPacket, CardId.SilentBurst,
                CardId.BroadcastMine, CardId.GhostShield, CardId.SignalLeech, CardId.BlindSpot,
                CardId.CounterSignal, CardId.BlackoutVolley, CardId.DeadDrop, CardId.ZeroSignature
            },
            [CargoContract.SignalSeed] = new[]
            {
                CardId.ReserveShot, CardId.StandbyField, CardId.TightSchedule, CardId.RelayStep,
                CardId.ReserveRouting,
                CardId.OnePointPlan, CardId.ReserveCapacitor, CardId.ScheduledShot, CardId.DispatchLoop,
                CardId.LockVoucher, CardId.DeferredVolley, CardId.BudgetThruster, CardId.SpareChannel,
                CardId.ExactChange, CardId.QueueCollapse, CardId.FinalAllocation, CardId.PostalOverdrive
            }
        };

        public static IReadOnlyList<CardId> SharedCards => Shared;

        public static IReadOnlyList<CardId> CardsFor(CargoContract contract)
        {
            if (!ContractCards.TryGetValue(contract, out CardId[] cards))
                throw new ArgumentOutOfRangeException(nameof(contract), contract, null);
            return cards;
        }

        public static IReadOnlyList<CardId> RewardPool(CargoContract contract)
        {
            return Shared.Concat(CardsFor(contract)).Distinct().ToArray();
        }

        public static CardId[] CreateStarterDeck(CargoContract contract)
        {
            return new[]
            {
                CardId.BurstFire, CardId.BurstFire,
                CardId.BankUp, CardId.BankUp,
                CardId.BankDown, CardId.BankDown,
                CardId.WindGuard, CardId.WindGuard,
                CardId.EmergencyCoolant, CardId.BroadsideVolley,
                CardId.OverloadAim, CardId.EngineOverclock,
                ContractCatalog.StarterCard(contract)
            };
        }

        public static IReadOnlyList<CardId> AllCards =>
            Shared.Concat(ContractCards.Values.SelectMany(cards => cards)).Distinct().OrderBy(card => card).ToArray();

        public static bool IsShared(CardId card) => Array.IndexOf(Shared, card) >= 0;

        public static bool BelongsToContract(CardId card, CargoContract contract) =>
            ContractCards.TryGetValue(contract, out CardId[] cards) && Array.IndexOf(cards, card) >= 0;

        public static bool IsDamageCard(CardId card)
        {
            if (ExpandedCardCatalog.IsDamaging(card))
                return true;
            return card switch
            {
                CardId.BurstFire or CardId.BroadsideVolley or CardId.OverloadAim or
                CardId.RailPiercer or CardId.PursuitShot or CardId.AegisRam or
                CardId.FrostLance or CardId.MeltdownBurst or CardId.Scattershot or
                CardId.MissileSwarm or CardId.CounterPursuit or CardId.InterceptMine or
                CardId.SlipstreamStrike or CardId.PrismEcho or CardId.GhostProtocol or
                CardId.ReserveShot => true,
                _ => false
            };
        }
    }

    public static class CardOfferCatalog
    {
        public static CardId[] Select(CargoContract contract, AirspaceCondition airspace, int seed, int count,
            IEnumerable<CardId> ownedCards, CardId? forcedCard = null)
        {
            if (count <= 0)
                return Array.Empty<CardId>();

            var owned = new HashSet<CardId>(ownedCards ?? Array.Empty<CardId>());
            var selected = new List<CardId>(count);
            if (forcedCard.HasValue)
                selected.Add(forcedCard.Value);

            IEnumerable<CardId> pool = CardPoolCatalog.RewardPool(contract)
                .Where(card => !selected.Contains(card));
            if (!selected.Any(CardPoolCatalog.IsDamageCard))
                AddBest(selected, pool.Where(CardPoolCatalog.IsDamageCard), owned, airspace, seed);
            if (selected.Count < count && selected.All(CardPoolCatalog.IsDamageCard))
                AddBest(selected, pool.Where(card => !CardPoolCatalog.IsDamageCard(card)), owned, airspace, seed + 17);
            if (selected.Count < count && !selected.Any(card => !CardPoolCatalog.IsDamageCard(card)))
                AddBest(selected, pool.Where(card => !CardPoolCatalog.IsDamageCard(card)), owned, airspace, seed + 31);

            while (selected.Count < count)
            {
                CardId? next = Ordered(pool.Where(card => !selected.Contains(card)), owned, airspace,
                    seed + selected.Count * 53).Cast<CardId?>().FirstOrDefault();
                if (!next.HasValue)
                    break;
                selected.Add(next.Value);
            }
            return selected.ToArray();
        }

        public static int AirspaceAffinity(CardId card, AirspaceCondition airspace)
        {
            CardSpec spec = CardLibrary.Get(card);
            return airspace switch
            {
                AirspaceCondition.JetstreamCorridor =>
                    (spec.Family == CardFamily.Maneuver ? 5 : 0) + (spec.Cost == 0 ? 2 : 0) +
                    (spec.Heat == 0 ? 1 : 0),
                AirspaceCondition.StaticFront =>
                    (spec.Family == CardFamily.Utility ? 4 : 0) +
                    (spec.Family == CardFamily.Defense ? 2 : 0) + (spec.Cost == 1 ? 1 : 0),
                _ => (spec.Family == CardFamily.Weapon ? 4 : 0) + (spec.Cost >= 2 ? 2 : 0) +
                    (spec.Family == CardFamily.Defense ? 1 : 0)
            };
        }

        private static void AddBest(List<CardId> selected, IEnumerable<CardId> candidates, HashSet<CardId> owned,
            AirspaceCondition airspace, int seed)
        {
            CardId? best = Ordered(candidates.Where(card => !selected.Contains(card)), owned, airspace, seed)
                .Cast<CardId?>().FirstOrDefault();
            if (best.HasValue)
                selected.Add(best.Value);
        }

        private static IOrderedEnumerable<CardId> Ordered(IEnumerable<CardId> candidates, HashSet<CardId> owned,
            AirspaceCondition airspace, int seed)
        {
            return candidates
                .OrderBy(card => OfferRank(card, owned.Contains(card), airspace, seed));
        }

        private static double OfferRank(CardId card, bool owned, AirspaceCondition airspace, int seed)
        {
            double affinityWeight = 1d + AirspaceAffinity(card, airspace) * 0.22d;
            double discoveryPenalty = owned ? uint.MaxValue * 0.12d : 0d;
            return OfferHash(card, seed) / affinityWeight + discoveryPenalty;
        }

        private static uint OfferHash(CardId card, int seed)
        {
            uint value = unchecked((uint)seed) ^ (unchecked((uint)(int)card) + 1u) * 0x9E3779B9u;
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            return value ^ (value >> 16);
        }
    }
}
