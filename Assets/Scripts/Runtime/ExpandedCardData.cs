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
                CardId.DeferredVolley => Card(id, "延后齐射", "预定下回合开始时对所有敌人造成5点伤害；支付后剩1点能量时改为7点。", 2, 1, CardFamily.Weapon),
                CardId.BudgetThruster => Card(id, "预算推进", "切换至对侧外航道并获得3点护盾；支付后剩1点能量时返还1点。", 1, 0, CardFamily.Maneuver),
                CardId.SpareChannel => Card(id, "备用信道", "抽1张牌；支付后剩1点能量时，本回合保留其余手牌。", 1, 0, CardFamily.Utility),
                CardId.ExactChange => Card(id, "精确找零", "将超过1点的能量全部托管，下回合返还；每托管1点立即获得2点护盾。", 0, 0, CardFamily.Utility),
                CardId.QueueCollapse => Card(id, "队列坍缩", "追踪最低耐久敌人，当前手牌与上回合保留手牌越多伤害越高；剩1点能量时抽1张。", 2, 2, CardFamily.Weapon),
                CardId.FinalAllocation => Card(id, "最终分配", "获得6点护盾；本回合每点已返还的托管能量再获得6点护盾。", 2, 0, CardFamily.Defense),
                CardId.PostalOverdrive => Card(id, "邮路超频", "追踪最低耐久敌人造成18点伤害；剩1点能量时额外造成8点，每点已返还托管能量再造成4点。", 3, 3, CardFamily.Weapon),

                CardId.EscortAnchor => Card(id, "护航锚点", "获得4点护盾，在当前航道部署锚点；下次进入或回合开始于此处时，获得6点护盾与1层锁定。", 1, 0, CardFamily.Defense),
                CardId.PrismReticle => Card(id, "棱镜准星", "获得1层锁定；已有至少6点护盾时抽1张牌，否则获得4点护盾。", 1, 0, CardFamily.Utility),
                CardId.AegisRicochet => Card(id, "神盾跳弹", "对同航道造成5点加当前护盾三分之一的伤害；有锁定时再获得4点护盾。", 1, 1, CardFamily.Weapon),

                CardId.CondenserBeacon => Card(id, "冷凝航标", "降低1点热量并在当前航道部署冷凝区；下次进入或回合开始于此处时降低3点热量并校准暴击。", 1, 0, CardFamily.Utility),
                CardId.ThermalPendulum => Card(id, "热摆回路", "热量不高于2时增加3点热量并获得1点能量；否则降低3点热量并抽1张牌。", 0, 0, CardFamily.Utility),
                CardId.QuenchDetonation => Card(id, "淬火爆破", "对同航道造成10点伤害；低热时增加3点热量，否则降低3点热量。", 1, 1, CardFamily.Weapon),

                CardId.SlipstreamGate => Card(id, "尾流门", "在当前航道部署尾流，穿越至对侧外航道并获得1层动量；再次进入尾流时获得能量与动量。", 1, 0, CardFamily.Maneuver),
                CardId.OrbitSalvo => Card(id, "环航齐射", "对所有敌人造成2点加当前动量的伤害；本回合换过航道时校准下一张齐射。", 2, 2, CardFamily.Weapon),
                CardId.VectorMinefield => Card(id, "矢量雷区", "在当前航道部署6点加动量两倍的雷区，再穿越至对侧外航道；雷区会在回合结束时攻击留在其中的敌人。", 1, 1, CardFamily.Maneuver),

                CardId.GhostCorridor => Card(id, "幽灵走廊", "获得1层航迹暴露并抽1张牌，在当前航道部署幽灵走廊；再次进入时清除2层航迹并获得1层锁定。", 1, 0, CardFamily.Utility),
                CardId.DecoyMinefield => Card(id, "诱饵雷区", "按5点加航迹两倍部署当前航道雷区，穿越至对侧外航道，然后获得1层航迹暴露。", 1, 1, CardFamily.Maneuver),
                CardId.SilentLedger => Card(id, "静默账本", "没有航迹时获得1层锁定并抽1张牌；否则清除1层航迹并获得6点护盾。", 0, 0, CardFamily.Utility),

                CardId.EscrowProtocol => Card(id, "托管协议", "将1点当前能量存入托管，下回合返还2点；仅在拥有至少2点能量时可用。", 0, 0, CardFamily.Utility),
                CardId.QueueCache => Card(id, "队列缓存", "保留其余手牌，并按最多3张手牌在当前航道部署缓存；下次进入时获得对应护盾并抽1张牌。", 1, 0, CardFamily.Utility),
                CardId.DeferredStrike => Card(id, "延迟投递", "预定下回合开始时追踪最低耐久敌人造成10点伤害；支付后剩1点能量时追加4点。", 1, 1, CardFamily.Weapon),
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
                CardId.AegisRicochet or CardId.QuenchDetonation or CardId.OrbitSalvo or CardId.DeferredStrike => true,
                _ => false
            };
        }

        public static CardTargetRequirement TargetRequirement(CardId id)
        {
            return id switch
            {
                CardId.MirrorPlating or CardId.CompressionRam or CardId.HeatSinkLance or CardId.FurnaceWake or
                CardId.DriftFire or CardId.ScheduledShot or CardId.AegisRicochet or
                CardId.QuenchDetonation => CardTargetRequirement.SameLane,
                CardId.WakeMine or CardId.BroadcastMine => CardTargetRequirement.OtherLane,
                CardId.LastStandCourier or CardId.ReactorPurge or CardId.PursuitVector or CardId.GaleBreak or
                CardId.SilentBurst or CardId.CounterSignal or CardId.ZeroSignature or CardId.QueueCollapse or
                CardId.PostalOverdrive or CardId.DeferredStrike => CardTargetRequirement.AnyEnemy,
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
                id == CardId.DeferredVolley || id == CardId.OrbitSalvo;
        }

        public static bool IsCooling(CardId id)
        {
            return id == CardId.ThermalBarrier || id == CardId.SealantRecycle ||
                id == CardId.FlashFreeze || id == CardId.ThermalBattery ||
                id == CardId.SuperheatedCoolant || id == CardId.HeatSinkLance ||
                id == CardId.QuenchVolley || id == CardId.BoiloffArmor ||
                id == CardId.ColdStart || id == CardId.ReactorPurge ||
                id == CardId.WhiteoutProtocol || id == CardId.CondenserBeacon ||
                id == CardId.ThermalPendulum || id == CardId.QuenchDetonation;
        }

        private static CardSpec Card(CardId id, string name, string rules, int cost, int heat, CardFamily family)
        {
            return new CardSpec(id, name, rules, cost, heat, family);
        }
    }

    [Flags]
    public enum CardSynergyTag
    {
        None = 0,
        Lock = 1 << 0,
        Shield = 1 << 1,
        Cooling = 1 << 2,
        Heat = 1 << 3,
        Momentum = 1 << 4,
        Volley = 1 << 5,
        Exposure = 1 << 6,
        Stealth = 1 << 7,
        Reserve = 1 << 8,
        Queue = 1 << 9,
        Deferred = 1 << 10,
        Lane = 1 << 11,
        Draw = 1 << 12
    }

    public enum ExpandedUpgradeTheme
    {
        Lock,
        Shield,
        Cooling,
        Heat,
        Momentum,
        Volley,
        Exposure,
        Stealth,
        Reserve,
        Queue,
        Deferred,
        Lane,
        Draw
    }

    public static class CardSynergyCatalog
    {
        public static CardSynergyTag Tags(CardId card)
        {
            CardSynergyTag tags = Produced(card) | Consumed(card);
            CardSpec spec = CardLibrary.Get(card);
            if (spec.Family == CardFamily.Maneuver)
                tags |= CardSynergyTag.Lane | CardSynergyTag.Momentum;
            if (ExpandedCardCatalog.IsVolley(card) || card == CardId.BroadsideVolley ||
                card == CardId.Scattershot || card == CardId.MissileSwarm)
                tags |= CardSynergyTag.Volley;
            return tags;
        }

        public static CardSynergyTag Produced(CardId card)
        {
            return card switch
            {
                CardId.ShadowLock => CardSynergyTag.Lock | CardSynergyTag.Exposure,
                CardId.GhostShield => CardSynergyTag.Shield | CardSynergyTag.Stealth,
                CardId.DecoyPacket => CardSynergyTag.Exposure | CardSynergyTag.Draw,
                CardId.DecoyMinefield => CardSynergyTag.Exposure | CardSynergyTag.Lane,
                CardId.TargetLock or CardId.LockCascade or CardId.PrecisionSeal or CardId.ImpactLedger or
                CardId.DeadDrop or CardId.PrismReticle => CardSynergyTag.Lock,
                CardId.WindGuard or CardId.ReactivePlating or CardId.PrismEcho or CardId.ReactiveSeal or
                CardId.AblativeFoam or CardId.LockBastion or CardId.MirrorPlating or CardId.CargoScreen or
                CardId.BraceForImpact or CardId.ParcelAegis or CardId.MomentumGuard or
                CardId.StandbyField or CardId.ReserveCapacitor or CardId.FinalAllocation or CardId.EscortAnchor =>
                    CardSynergyTag.Shield,
                CardId.EmergencyCoolant or CardId.CryoPump or CardId.ZeroPointCalibration or CardId.PhaseExchange or
                CardId.ThermalBarrier or CardId.SealantRecycle or CardId.FlashFreeze or CardId.ThermalBattery or
                CardId.HeatSinkLance or CardId.QuenchVolley or CardId.ColdStart or CardId.ReactorPurge or
                CardId.WhiteoutProtocol or CardId.CondenserBeacon => CardSynergyTag.Cooling,
                CardId.EngineOverclock or CardId.HeatCharge or CardId.RedlineIgnition or CardId.IgnitionLoop or
                CardId.ThermalPendulum or CardId.QuenchDetonation => CardSynergyTag.Heat,
                CardId.VectorDash or CardId.EyeTransit or CardId.VectorLoop or CardId.PursuitVector or
                CardId.StormOrbit or CardId.SlipstreamGate => CardSynergyTag.Momentum | CardSynergyTag.Lane,
                CardId.SignalScrambler or CardId.AirBrake or CardId.TraceHarvest or
                CardId.BlindSpot or CardId.SilentLedger => CardSynergyTag.Stealth,
                CardId.GhostProtocol or CardId.FalseTelemetry or CardId.GhostCorridor => CardSynergyTag.Exposure,
                CardId.TightSchedule or CardId.ReserveRouting or CardId.OnePointPlan or CardId.DispatchLoop or
                CardId.SpareChannel or CardId.QueueCache => CardSynergyTag.Queue | CardSynergyTag.Draw,
                CardId.ExactChange or CardId.EscrowProtocol => CardSynergyTag.Deferred,
                CardId.HoldFormation or CardId.EmergencySort or CardId.ArmorySearch or CardId.SuperheatedCoolant =>
                    CardSynergyTag.Draw,
                CardId.VectorMinefield => CardSynergyTag.Lane,
                _ => CardSynergyTag.None
            };
        }

        public static CardSynergyTag Consumed(CardId card)
        {
            return card switch
            {
                CardId.QueueCollapse => CardSynergyTag.Reserve | CardSynergyTag.Queue,
                CardId.QueueCache => CardSynergyTag.Reserve | CardSynergyTag.Queue | CardSynergyTag.Lane,
                CardId.DeferredVolley or CardId.PostalOverdrive or CardId.DeferredStrike =>
                    CardSynergyTag.Reserve | CardSynergyTag.Deferred,
                CardId.WakeMine or CardId.VectorMinefield => CardSynergyTag.Momentum | CardSynergyTag.Lane,
                CardId.BroadcastMine or CardId.DecoyMinefield => CardSynergyTag.Exposure | CardSynergyTag.Lane,
                CardId.RailPiercer or CardId.ReactiveSeal or CardId.LockBastion or CardId.TracerSwarm or
                CardId.QueueDirective or CardId.SignalLeech or CardId.LockVoucher or CardId.ParcelAegis or
                CardId.AegisRicochet => CardSynergyTag.Lock,
                CardId.AegisRam or CardId.PrismEcho or CardId.CapacitorDump or CardId.BulkheadPulse or
                CardId.CompressionRam or CardId.SealantRecycle => CardSynergyTag.Shield,
                CardId.FrostLance or CardId.AbsoluteZero => CardSynergyTag.Cooling,
                CardId.OverloadAim or CardId.MeltdownBurst or CardId.BoiloffArmor or CardId.ReactorPurge or
                CardId.FurnaceWake or CardId.ThermalPendulum or CardId.QuenchDetonation => CardSynergyTag.Heat,
                CardId.PursuitShot or CardId.SlipstreamStrike or CardId.KineticBroadside or CardId.MomentumGuard or
                CardId.DriftFire or CardId.TailwindCharge or CardId.SpiralBarrage or
                CardId.GaleBreak or CardId.TerminalDive or CardId.OrbitSalvo =>
                    CardSynergyTag.Momentum,
                CardId.CounterPursuit or CardId.InterceptMine or CardId.CounterSignal or
                CardId.BlackoutVolley or CardId.GhostProtocol => CardSynergyTag.Exposure,
                CardId.SilentBurst or CardId.ZeroSignature or CardId.SilentLedger => CardSynergyTag.Stealth,
                CardId.ReserveShot or CardId.StandbyField or CardId.TightSchedule or CardId.RelayStep or
                CardId.ReserveRouting or CardId.OnePointPlan or CardId.ReserveCapacitor or CardId.ScheduledShot or
                CardId.DispatchLoop or CardId.LockVoucher or CardId.BudgetThruster or CardId.SpareChannel =>
                    CardSynergyTag.Reserve,
                CardId.FinalAllocation => CardSynergyTag.Deferred,
                CardId.CrosswindCut or CardId.SlipstreamGate or CardId.GhostCorridor =>
                    CardSynergyTag.Lane,
                _ => CardSynergyTag.None
            };
        }

        public static ExpandedUpgradeTheme UpgradeTheme(CardId card)
        {
            if (card is CardId.EscortAnchor or CardId.CondenserBeacon or CardId.SlipstreamGate or
                CardId.VectorMinefield or CardId.GhostCorridor or CardId.DecoyMinefield or CardId.QueueCache)
                return ExpandedUpgradeTheme.Lane;
            if (card is CardId.DeferredVolley or CardId.ExactChange or CardId.FinalAllocation or
                CardId.PostalOverdrive or CardId.EscrowProtocol or CardId.DeferredStrike)
                return ExpandedUpgradeTheme.Deferred;
            if (card is CardId.QueueDirective or CardId.EmergencySort or CardId.HoldFormation or
                CardId.TightSchedule or CardId.ReserveRouting or CardId.OnePointPlan or CardId.DispatchLoop or
                CardId.LockVoucher or CardId.SpareChannel or CardId.QueueCollapse)
                return ExpandedUpgradeTheme.Queue;
            if (CardPoolCatalog.BelongsToContract(card, CargoContract.SignalSeed))
                return ExpandedUpgradeTheme.Reserve;
            if (card is CardId.KineticBroadside or CardId.TracerSwarm or CardId.Scattershot or CardId.MissileSwarm or
                CardId.SwarmBeacon or CardId.SpiralBarrage or CardId.TerminalDive or CardId.OrbitSalvo)
                return ExpandedUpgradeTheme.Volley;
            if (CardPoolCatalog.BelongsToContract(card, CargoContract.StormCore))
                return ExpandedUpgradeTheme.Momentum;
            if (card is CardId.SignalScrambler or CardId.AirBrake or CardId.TraceHarvest or CardId.SilentBurst or
                CardId.GhostShield or CardId.BlindSpot or CardId.ZeroSignature or CardId.SilentLedger)
                return ExpandedUpgradeTheme.Stealth;
            if (CardPoolCatalog.BelongsToContract(card, CargoContract.BlackBoxRelay))
                return ExpandedUpgradeTheme.Exposure;
            if (card is CardId.HeatCharge or CardId.RedlineIgnition or CardId.IgnitionLoop or CardId.ReactorPurge or
                CardId.FurnaceWake or CardId.ThermalPendulum or CardId.QuenchDetonation)
                return ExpandedUpgradeTheme.Heat;
            if (CardPoolCatalog.BelongsToContract(card, CargoContract.CryoSerum))
                return ExpandedUpgradeTheme.Cooling;
            if ((Tags(card) & CardSynergyTag.Lock) != 0)
                return ExpandedUpgradeTheme.Lock;
            if ((Tags(card) & CardSynergyTag.Shield) != 0)
                return ExpandedUpgradeTheme.Shield;
            return ExpandedUpgradeTheme.Draw;
        }

        public static int SynergyScore(CardId candidate, IEnumerable<CardId> ownedCards)
        {
            CardSynergyTag candidateProduces = Produced(candidate);
            CardSynergyTag candidateConsumes = Consumed(candidate);
            CardSynergyTag candidateTags = Tags(candidate);
            int score = 0;
            foreach (CardId owned in ownedCards ?? Array.Empty<CardId>())
            {
                if ((candidateConsumes & Produced(owned)) != 0)
                    score += 4;
                if ((candidateProduces & Consumed(owned)) != 0)
                    score += 3;
                if ((candidateTags & Tags(owned)) != 0)
                    score += 1;
            }
            return Math.Min(24, score);
        }

        public static string SynergyLabel(CardId candidate, IEnumerable<CardId> ownedCards)
        {
            CardSynergyTag producedByDeck = CardSynergyTag.None;
            CardSynergyTag consumedByDeck = CardSynergyTag.None;
            foreach (CardId owned in ownedCards ?? Array.Empty<CardId>())
            {
                producedByDeck |= Produced(owned);
                consumedByDeck |= Consumed(owned);
            }

            CardSynergyTag payoff = Consumed(candidate) & producedByDeck;
            if (payoff != CardSynergyTag.None)
                return $"构筑兑现 · {TagLabel(FirstTag(payoff))}";
            CardSynergyTag enabler = Produced(candidate) & consumedByDeck;
            if (enabler != CardSynergyTag.None)
                return $"构筑补强 · {TagLabel(FirstTag(enabler))}";
            CardSynergyTag shared = Tags(candidate) & (producedByDeck | consumedByDeck);
            return shared != CardSynergyTag.None ? $"同轴联动 · {TagLabel(FirstTag(shared))}" : string.Empty;
        }

        private static CardSynergyTag FirstTag(CardSynergyTag tags)
        {
            foreach (CardSynergyTag tag in Enum.GetValues(typeof(CardSynergyTag)))
            {
                if (tag != CardSynergyTag.None && (tags & tag) != 0)
                    return tag;
            }
            return CardSynergyTag.None;
        }

        private static string TagLabel(CardSynergyTag tag)
        {
            return tag switch
            {
                CardSynergyTag.Lock => "锁定",
                CardSynergyTag.Shield => "护盾",
                CardSynergyTag.Cooling => "降温",
                CardSynergyTag.Heat => "热量",
                CardSynergyTag.Momentum => "动量",
                CardSynergyTag.Volley => "齐射",
                CardSynergyTag.Exposure => "航迹",
                CardSynergyTag.Stealth => "隐匿",
                CardSynergyTag.Reserve => "余量",
                CardSynergyTag.Queue => "队列",
                CardSynergyTag.Deferred => "托管",
                CardSynergyTag.Lane => "航道协议",
                CardSynergyTag.Draw => "手牌循环",
                _ => "独立战术"
            };
        }
    }

    public static class ExpandedUpgradeCatalog
    {
        public static int AlphaDamageBonus(CardId card)
        {
            if (CardLibrary.Get(card).Family != CardFamily.Weapon)
                return 0;
            return ExpandedCardCatalog.IsVolley(card) ? 1 : 3;
        }

        public static string Rules(CardId card, UpgradeBranch branch)
        {
            string baseRules = CardLibrary.Get(card).Rules;
            bool english = LocalizationService.IsEnglish;
            if (branch == UpgradeBranch.Alpha)
            {
                string alpha = CardLibrary.Get(card).Family switch
                {
                    CardFamily.Weapon => ExpandedCardCatalog.IsVolley(card)
                        ? english ? "EACH DAMAGE INSTANCE +1." : "每段伤害+1。"
                        : english ? "DAMAGE +3." : "伤害+3。",
                    CardFamily.Defense => english
                        ? "AFTER GAINING SHIELD, GAIN 3 MORE." : "结算后若获得护盾，再获得3点。",
                    CardFamily.Maneuver => english
                        ? "AFTER MOVING, GAIN 2 SHIELD AND REMOVE 1 HEAT."
                        : "成功换道后获得2点护盾并降低1点热量。",
                    _ => english
                        ? "REMOVE 1 HEAT AFTER RESOLUTION; IF NONE, GAIN 2 SHIELD."
                        : "结算后降低1点热量；无热可降则获得2点护盾。"
                };
                return $"{baseRules} A // {alpha}";
            }

            string beta = CardSynergyCatalog.UpgradeTheme(card) switch
            {
                ExpandedUpgradeTheme.Lock => english ? "GAIN 1 LOCK AFTER RESOLUTION." : "结算后获得1层锁定。",
                ExpandedUpgradeTheme.Shield => english
                    ? "AFTER GAINING SHIELD, REMOVE 1 HEAT; OTHERWISE GAIN 3 SHIELD."
                    : "本牌获得护盾后降低1点热量；未获得则补充3点护盾。",
                ExpandedUpgradeTheme.Cooling => english
                    ? "REMOVING HEAT ARMS A CRITICAL HIT." : "本牌成功降温后校准下一次攻击暴击。",
                ExpandedUpgradeTheme.Heat => english
                    ? "AT 4+ HEAT AFTER RESOLUTION, DRAW 1 AND GAIN 2 SHIELD."
                    : "结算后达到4点热量时抽1张牌并获得2点护盾。",
                ExpandedUpgradeTheme.Momentum => english
                    ? "KEEP 1 MOMENTUM AFTER SPENDING IT; GAIN 1 IF MOVEMENT GENERATED NONE."
                    : "消耗动量后至少保留1层；换道且未获得动量时补1层。",
                ExpandedUpgradeTheme.Volley => english
                    ? "PRIME THE NEXT VOLLEY OR MISSILE CARD." : "结算后校准下一张齐射或飞弹牌。",
                ExpandedUpgradeTheme.Exposure => english
                    ? "WHEN TRACE CHANGES, GAIN 1 LOCK." : "航迹层数发生变化时获得1层锁定。",
                ExpandedUpgradeTheme.Stealth => english
                    ? "AT ZERO TRACE GAIN 4 SHIELD; OTHERWISE CLEAR 1 MORE TRACE."
                    : "零航迹时获得4点护盾，否则额外清除1层航迹。",
                ExpandedUpgradeTheme.Reserve => english
                    ? "AT 1 ENERGY AFTER PAYMENT, GAIN 4 SHIELD AND REMOVE 1 HEAT."
                    : "支付后剩1点能量时获得4点护盾并降低1点热量。",
                ExpandedUpgradeTheme.Queue => english ? "RETAIN YOUR OTHER CARDS THIS TURN." : "本回合保留其余手牌。",
                ExpandedUpgradeTheme.Deferred => english
                    ? "ESCROW 1 EXTRA ENERGY FOR NEXT TURN." : "额外托管1点能量到下回合。",
                ExpandedUpgradeTheme.Lane => english
                    ? "THE DEPLOYED LANE PROTOCOL GAINS 1 STRENGTH."
                    : "本牌部署的航道协议强度+1。",
                _ => english ? "DRAW 1 EXTRA CARD." : "额外抽1张牌。"
            };
            return $"{baseRules} B // {beta}";
        }
    }

    public static class CardPoolCatalog
    {
        public const int TotalCardTypes = 123;

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
                CardId.SealantRecycle, CardId.BraceForImpact, CardId.ParcelAegis, CardId.LastStandCourier,
                CardId.EscortAnchor, CardId.PrismReticle, CardId.AegisRicochet
            },
            [CargoContract.CryoSerum] = new[]
            {
                CardId.CryoPump, CardId.FrostLance, CardId.HeatCharge, CardId.MeltdownBurst,
                CardId.ZeroPointCalibration, CardId.RedlineIgnition, CardId.PhaseExchange,
                CardId.FlashFreeze, CardId.ThermalBattery, CardId.SuperheatedCoolant, CardId.HeatSinkLance,
                CardId.QuenchVolley, CardId.BoiloffArmor, CardId.ColdStart, CardId.IgnitionLoop,
                CardId.ReactorPurge, CardId.WhiteoutProtocol, CardId.FurnaceWake, CardId.AbsoluteZero,
                CardId.CondenserBeacon, CardId.ThermalPendulum, CardId.QuenchDetonation
            },
            [CargoContract.StormCore] = new[]
            {
                CardId.VectorDash, CardId.PursuitShot, CardId.Scattershot, CardId.MissileSwarm,
                CardId.SlipstreamStrike, CardId.SwarmBeacon, CardId.EyeTransit,
                CardId.CrosswindCut, CardId.MomentumGuard, CardId.DriftFire, CardId.VectorLoop,
                CardId.TailwindCharge, CardId.SpiralBarrage, CardId.SnapRoll, CardId.WakeMine,
                CardId.PursuitVector, CardId.GaleBreak, CardId.StormOrbit, CardId.TerminalDive,
                CardId.SlipstreamGate, CardId.OrbitSalvo, CardId.VectorMinefield
            },
            [CargoContract.BlackBoxRelay] = new[]
            {
                CardId.SignalScrambler, CardId.CounterPursuit, CardId.AirBrake, CardId.InterceptMine,
                CardId.GhostProtocol, CardId.FalseTelemetry,
                CardId.TraceHarvest, CardId.ShadowLock, CardId.DecoyPacket, CardId.SilentBurst,
                CardId.BroadcastMine, CardId.GhostShield, CardId.SignalLeech, CardId.BlindSpot,
                CardId.CounterSignal, CardId.BlackoutVolley, CardId.DeadDrop, CardId.ZeroSignature,
                CardId.GhostCorridor, CardId.DecoyMinefield, CardId.SilentLedger
            },
            [CargoContract.SignalSeed] = new[]
            {
                CardId.ReserveShot, CardId.StandbyField, CardId.TightSchedule, CardId.RelayStep,
                CardId.ReserveRouting,
                CardId.OnePointPlan, CardId.ReserveCapacitor, CardId.ScheduledShot, CardId.DispatchLoop,
                CardId.LockVoucher, CardId.DeferredVolley, CardId.BudgetThruster, CardId.SpareChannel,
                CardId.ExactChange, CardId.QueueCollapse, CardId.FinalAllocation, CardId.PostalOverdrive,
                CardId.EscrowProtocol, CardId.QueueCache, CardId.DeferredStrike
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

            CardId[] ownedList = (ownedCards ?? Array.Empty<CardId>()).ToArray();
            var owned = new HashSet<CardId>(ownedList);
            var selected = new List<CardId>(count);
            if (forcedCard.HasValue)
                selected.Add(forcedCard.Value);

            IEnumerable<CardId> pool = CardPoolCatalog.RewardPool(contract)
                .Where(card => !selected.Contains(card));
            if (selected.Count < count && ownedList.Length > 0)
                AddBest(selected, pool, owned, ownedList, airspace, seed + 7);
            if (!selected.Any(CardPoolCatalog.IsDamageCard))
                AddBest(selected, pool.Where(CardPoolCatalog.IsDamageCard), owned, ownedList, airspace, seed);
            if (selected.Count < count && selected.All(CardPoolCatalog.IsDamageCard))
                AddBest(selected, pool.Where(card => !CardPoolCatalog.IsDamageCard(card)), owned, ownedList,
                    airspace, seed + 17);
            if (selected.Count < count && !selected.Any(card => !CardPoolCatalog.IsDamageCard(card)))
                AddBest(selected, pool.Where(card => !CardPoolCatalog.IsDamageCard(card)), owned, ownedList,
                    airspace, seed + 31);

            while (selected.Count < count)
            {
                CardId? next = Ordered(pool.Where(card => !selected.Contains(card)), owned, ownedList, airspace,
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
            IReadOnlyCollection<CardId> ownedList, AirspaceCondition airspace, int seed)
        {
            CardId? best = Ordered(candidates.Where(card => !selected.Contains(card)), owned, ownedList,
                    airspace, seed)
                .Cast<CardId?>().FirstOrDefault();
            if (best.HasValue)
                selected.Add(best.Value);
        }

        private static IOrderedEnumerable<CardId> Ordered(IEnumerable<CardId> candidates, HashSet<CardId> owned,
            IReadOnlyCollection<CardId> ownedList, AirspaceCondition airspace, int seed)
        {
            return candidates
                .OrderBy(card => OfferRank(card, owned.Contains(card), ownedList, airspace, seed));
        }

        private static double OfferRank(CardId card, bool owned, IReadOnlyCollection<CardId> ownedList,
            AirspaceCondition airspace, int seed)
        {
            double affinityWeight = 1d + AirspaceAffinity(card, airspace) * 0.22d;
            double synergyWeight = 1d + CardSynergyCatalog.SynergyScore(card, ownedList) * 0.16d;
            double discoveryPenalty = owned ? uint.MaxValue * 0.05d : 0d;
            return OfferHash(card, seed) / (affinityWeight * synergyWeight) + discoveryPenalty;
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
