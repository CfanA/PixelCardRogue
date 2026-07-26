using System;
using UnityEngine;

namespace SkyCourier
{
    public enum CardId
    {
        BurstFire,
        BankUp,
        BankDown,
        EmergencyCoolant,
        BroadsideVolley,
        WindGuard,
        OverloadAim,
        EngineOverclock,
        TargetLock,
        RailPiercer,
        VectorDash,
        PursuitShot,
        ReactivePlating,
        AegisRam,
        CryoPump,
        FrostLance,
        HeatCharge,
        MeltdownBurst,
        Scattershot,
        MissileSwarm,
        SignalScrambler,
        CounterPursuit,
        AirBrake,
        InterceptMine,
        LockCascade,
        SlipstreamStrike,
        PrismEcho,
        ZeroPointCalibration,
        RedlineIgnition,
        SwarmBeacon,
        GhostProtocol,
        ReactiveSeal,
        PhaseExchange,
        EyeTransit,
        FalseTelemetry
    }

    public enum CardFamily
    {
        Weapon,
        Maneuver,
        Defense,
        Utility
    }

    public enum ModuleId
    {
        VectorThruster,
        PrismBulkhead,
        CryoHeart,
        ExecutionChip,
        PrecisionMatrix,
        MomentumFlywheel,
        AegisCapacitor,
        ZeroPointReactor,
        RedlineReactor,
        SwarmUplink,
        GhostDecoder
    }

    public enum UpgradeBranch
    {
        Alpha,
        Beta
    }

    [Serializable]
    public sealed class CardSpec
    {
        public CardId Id;
        public string Name;
        public string Rules;
        public int Cost;
        public int Heat;
        public CardFamily Family;

        public CardSpec(CardId id, string name, string rules, int cost, int heat, CardFamily family)
        {
            Id = id;
            Name = LocalizationService.Text($"card.{id}.name", name);
            Rules = rules;
            Cost = cost;
            Heat = heat;
            Family = family;
        }
    }

    public static class CardLibrary
    {
        public static CardSpec Get(CardId id)
        {
            switch (id)
            {
                case CardId.BurstFire:
                    return new CardSpec(id, "点射", "对同航道首个敌人造成6点伤害。", 1, 2, CardFamily.Weapon);
                case CardId.BankUp:
                    return new CardSpec(id, "拉升", "向上移动1条航道，获得3点护盾。", 1, 0, CardFamily.Maneuver);
                case CardId.BankDown:
                    return new CardSpec(id, "俯冲", "向下移动1条航道，获得3点护盾。", 1, 0, CardFamily.Maneuver);
                case CardId.EmergencyCoolant:
                    return new CardSpec(id, "应急冷却", "降低3点热量。", 0, 0, CardFamily.Utility);
                case CardId.BroadsideVolley:
                    return new CardSpec(id, "弹幕齐射", "对所有敌人造成3点伤害。", 2, 3, CardFamily.Weapon);
                case CardId.WindGuard:
                    return new CardSpec(id, "防风挡板", "获得6点护盾。", 1, 0, CardFamily.Defense);
                case CardId.OverloadAim:
                    return new CardSpec(id, "过载瞄准", "对同航道造成10点伤害；已有4点热量时额外造成3点。", 2, 4, CardFamily.Weapon);
                case CardId.EngineOverclock:
                    return new CardSpec(id, "引擎超频", "获得1点能量。", 0, 2, CardFamily.Utility);
                case CardId.TargetLock:
                    return new CardSpec(id, "目标锁定", "获得1层锁定；穿甲轨炮会消耗锁定并增伤。", 0, 1, CardFamily.Utility);
                case CardId.RailPiercer:
                    return new CardSpec(id, "穿甲轨炮", "造成8点伤害；每层锁定额外造成5点伤害并消耗全部锁定。", 1, 2, CardFamily.Weapon);
                case CardId.VectorDash:
                    return new CardSpec(id, "矢量突进", "向下移动1条航道；已在最下方时改为向上。获得2点护盾与1层动量。", 1, 0, CardFamily.Maneuver);
                case CardId.PursuitShot:
                    return new CardSpec(id, "追猎射击", "造成5点伤害；每层动量额外造成4点伤害并消耗全部动量。", 1, 1, CardFamily.Weapon);
                case CardId.ReactivePlating:
                    return new CardSpec(id, "反应装甲", "获得7点护盾，为护盾冲角储备伤害。", 1, 0, CardFamily.Defense);
                case CardId.AegisRam:
                    return new CardSpec(id, "护盾冲角", "造成4点伤害，并追加最多10点当前护盾值，然后清空护盾。", 1, 1, CardFamily.Defense);
                case CardId.CryoPump:
                    return new CardSpec(id, "低温泵", "降低4点热量；若实际降低至少3点，获得1点能量。", 0, 0, CardFamily.Utility);
                case CardId.FrostLance:
                    return new CardSpec(id, "霜脉长枪", "造成7点伤害；出牌前热量不高于2时额外造成6点。", 1, 1, CardFamily.Weapon);
                case CardId.HeatCharge:
                    return new CardSpec(id, "热能充注", "获得2点能量，同时增加3点热量。", 0, 3, CardFamily.Utility);
                case CardId.MeltdownBurst:
                    return new CardSpec(id, "熔毁脉冲", "对所有敌人造成2点加当前热量的伤害，然后清空热量。", 2, 0, CardFamily.Weapon);
                case CardId.Scattershot:
                    return new CardSpec(id, "散射弹幕", "对所有敌人造成2点伤害。", 1, 1, CardFamily.Weapon);
                case CardId.MissileSwarm:
                    return new CardSpec(id, "蜂群飞弹", "发射4枚飞弹，每枚对随机敌人造成2点伤害。", 2, 2, CardFamily.Weapon);
                case CardId.SignalScrambler:
                    return new CardSpec(id, "信号扰频", "清除全部航迹暴露，获得5点护盾。", 0, 1, CardFamily.Utility);
                case CardId.CounterPursuit:
                    return new CardSpec(id, "逆向追猎", "追踪最低耐久敌人造成7点伤害；每层航迹暴露额外造成6点，随后清除暴露。", 1, 1, CardFamily.Weapon);
                case CardId.AirBrake:
                    return new CardSpec(id, "矢量刹车", "降低1层航迹暴露并获得5点护盾；若成功降低，获得1点能量。", 1, 0, CardFamily.Maneuver);
                case CardId.InterceptMine:
                    return new CardSpec(id, "航道雷网", "对所有不同航道的敌人造成6点伤害。", 1, 2, CardFamily.Weapon);
                case CardId.LockCascade:
                    return new CardSpec(id, "连锁标定", "获得1层锁定；已有锁定时额外对同航道造成4点伤害。", 1, 1, CardFamily.Utility);
                case CardId.SlipstreamStrike:
                    return new CardSpec(id, "尾流收割", "造成5点加每层动量2点的伤害，且不消耗动量。", 1, 1, CardFamily.Weapon);
                case CardId.PrismEcho:
                    return new CardSpec(id, "棱镜回响", "获得5点护盾，并对同航道造成当前护盾一半的伤害。", 1, 0, CardFamily.Defense);
                case CardId.ZeroPointCalibration:
                    return new CardSpec(id, "零点校准", "降低3点热量；若实际降低3点，使下一次攻击必定暴击。", 0, 0, CardFamily.Utility);
                case CardId.RedlineIgnition:
                    return new CardSpec(id, "红线点火", "获得2点能量并增加3点热量；高热时使下一次攻击必定暴击。", 0, 3, CardFamily.Utility);
                case CardId.SwarmBeacon:
                    return new CardSpec(id, "蜂群信标", "校准蜂群链路，使下一张齐射或飞弹牌获得额外伤害。", 1, 1, CardFamily.Utility);
                case CardId.GhostProtocol:
                    return new CardSpec(id, "幽灵协议", "主动获得1层航迹暴露，追踪最低耐久敌人造成6点加每层暴露5点的伤害。", 1, 1, CardFamily.Weapon);
                case CardId.ReactiveSeal:
                    return new CardSpec(id, "再生密封", "获得6点护盾；若有锁定，消耗1层锁定并再获得6点护盾。", 1, 0, CardFamily.Defense);
                case CardId.PhaseExchange:
                    return new CardSpec(id, "相变置换", "清空热量；每实际降低3点热量抽1张牌，最多抽2张。", 0, 0, CardFamily.Utility);
                case CardId.EyeTransit:
                    return new CardSpec(id, "风眼穿越", "直接穿越至另一侧外航道；每跨越1条航道获得1层动量。", 1, 1, CardFamily.Maneuver);
                case CardId.FalseTelemetry:
                    return new CardSpec(id, "伪造遥测", "主动获得2层航迹暴露，然后抽2张牌。", 0, 1, CardFamily.Utility);
                default:
                    throw new ArgumentOutOfRangeException(nameof(id), id, null);
            }
        }

        public static Color FamilyColor(CardFamily family)
        {
            switch (family)
            {
                case CardFamily.Weapon: return new Color32(219, 76, 72, 255);
                case CardFamily.Maneuver: return new Color32(49, 165, 183, 255);
                case CardFamily.Defense: return new Color32(241, 174, 67, 255);
                default: return new Color32(124, 102, 172, 255);
            }
        }
    }

    public static class ContractCardCatalog
    {
        public static CardId SignatureCard(CargoContract contract)
        {
            return contract switch
            {
                CargoContract.FragileMedicine => CardId.ReactiveSeal,
                CargoContract.CryoSerum => CardId.PhaseExchange,
                CargoContract.StormCore => CardId.EyeTransit,
                CargoContract.BlackBoxRelay => CardId.FalseTelemetry,
                _ => throw new ArgumentOutOfRangeException(nameof(contract), contract, null)
            };
        }

        public static bool BelongsTo(CardId card, CargoContract contract)
        {
            return card == SignatureCard(contract);
        }
    }
}
