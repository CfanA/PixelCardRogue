using System;

namespace SkyCourier
{
    public sealed class EnemySpec
    {
        public EnemyKind Kind { get; }
        public string Name { get; }
        public int Lane { get; }
        public int Health { get; }
        public int Damage { get; }

        public EnemySpec(EnemyKind kind, string name, int lane, int health, int damage)
        {
            Kind = kind;
            Name = name;
            Lane = lane;
            Health = health;
            Damage = damage;
        }
    }

    public sealed class EncounterDefinition
    {
        public EncounterId Id { get; }
        public int Variant { get; }
        public string FormationName { get; }
        public EnemySpec[] Enemies { get; }

        public EncounterDefinition(EncounterId id, int variant, string formationName, params EnemySpec[] enemies)
        {
            Id = id;
            Variant = variant;
            FormationName = formationName;
            Enemies = enemies;
        }
    }

    public static class EncounterCatalog
    {
        public const int VariantCount = 12;
        public const int MidBossVariantCount = 3;
        public const int BossVariantCount = 4;

        public static EncounterDefinition Get(EncounterId encounter, int variant)
        {
            int normalizedVariant = encounter == EncounterId.Boss
                ? Math.Abs(variant) % BossVariantCount
                : encounter == EncounterId.MidBoss
                    ? Math.Abs(variant) % MidBossVariantCount
                    : Math.Abs(variant) % VariantCount;
            switch (encounter)
            {
                case EncounterId.Skirmish when normalizedVariant == 0:
                    return new EncounterDefinition(encounter, 0, "标准拦截编队",
                        new EnemySpec(EnemyKind.RustKite, "锈翼鸢", 0, 14, 5),
                        new EnemySpec(EnemyKind.MailEater, "噬邮兽", 1, 18, 7));
                case EncounterId.Skirmish when normalizedVariant == 1:
                    return new EncounterDefinition(encounter, 1, "灾变猎杀编队",
                        new EnemySpec(EnemyKind.RustKite, "锈翼鸢", 0, 14, 5),
                        new EnemySpec(EnemyKind.CalamityDrone, "灾变无人机", 1, 16, BattleState.CalamityStrikeDamage));
                case EncounterId.Skirmish when normalizedVariant == 2:
                    return new EncounterDefinition(encounter, 2, "机体猎杀编队",
                        new EnemySpec(EnemyKind.ShieldLeech, "盾蚀水蛭", 0, 16, 5),
                        new EnemySpec(EnemyKind.HeatSeeker, "热寻隼", 2, 18, 5));
                case EncounterId.Skirmish when normalizedVariant == 5:
                    return new EncounterDefinition(encounter, 5, "磁针鳐卫前哨",
                        new EnemySpec(EnemyKind.FluxSkimmer, "磁针鳐卫", 1, 24, 7),
                        new EnemySpec(EnemyKind.ShieldLeech, "磁壳水蛭", 2, 18, 5));
                case EncounterId.Skirmish when normalizedVariant == 6:
                    return new EncounterDefinition(encounter, 6, "时差观测群",
                        new EnemySpec(EnemyKind.TimeLagJelly, "时差水母", 0, 15, 5),
                        new EnemySpec(EnemyKind.RustKite, "节拍锈翼鸢", 2, 14, 5));
                case EncounterId.Skirmish when normalizedVariant == 7:
                    return new EncounterDefinition(encounter, 7, "航路拾荒群",
                        new EnemySpec(EnemyKind.SalvageCorvid, "拾荒鸦艇", 0, 13, 0),
                        new EnemySpec(EnemyKind.MailEater, "护赃噬邮兽", 1, 19, 6));
                case EncounterId.Skirmish when normalizedVariant == 8:
                    return new EncounterDefinition(encounter, 8, "缝合航道编队",
                        new EnemySpec(EnemyKind.LaneTailor, "航道裁缝", 1, 17, 4),
                        new EnemySpec(EnemyKind.HeatSeeker, "热寻隼", 2, 17, 5));
                case EncounterId.Skirmish when normalizedVariant == 9:
                    return new EncounterDefinition(encounter, 9, "空白协议编队",
                        new EnemySpec(EnemyKind.NullBeacon, "空白信标", 0, 14, 3),
                        new EnemySpec(EnemyKind.ShieldLeech, "盾蚀水蛭", 2, 18, 5));
                case EncounterId.Skirmish when normalizedVariant == 10:
                    return new EncounterDefinition(encounter, 10, "折光寄舱群",
                        new EnemySpec(EnemyKind.PrismStowaway, "折光寄舱蟹", 0, 18, 6),
                        new EnemySpec(EnemyKind.SignalHijacker, "折光协议机", 2, 17, 4));
                case EncounterId.Skirmish when normalizedVariant == 11:
                    return new EncounterDefinition(encounter, 11, "延迟投递编队",
                        new EnemySpec(EnemyKind.TimeLagJelly, "时差水母", 0, 16, 5),
                        new EnemySpec(EnemyKind.PrismStowaway, "折光寄舱蟹", 2, 18, 6));
                case EncounterId.Skirmish:
                    return new EncounterDefinition(encounter, 3, "协议封锁编队",
                        new EnemySpec(EnemyKind.HandJammer, "噪声织网", 0, 16, 5),
                        new EnemySpec(EnemyKind.SignalHijacker, "协议劫持机", 2, 18, 4));
                case EncounterId.Elite when normalizedVariant == 0:
                    return new EncounterDefinition(encounter, 0, "雷暴封锁编队",
                        new EnemySpec(EnemyKind.RustKite, "锈翼鸢·改", 0, 18, 6),
                        new EnemySpec(EnemyKind.MailEater, "重装噬邮兽", 1, 22, 7),
                        new EnemySpec(EnemyKind.StormBalloon, "风暴气囊", 2, 12, 3));
                case EncounterId.Elite when normalizedVariant == 1:
                    return new EncounterDefinition(encounter, 1, "灾变封锁编队",
                        new EnemySpec(EnemyKind.MailEater, "噬邮兽·护航型", 0, 20, 6),
                        new EnemySpec(EnemyKind.CalamityDrone, "灾变无人机·改", 1, 20, BattleState.CalamityStrikeDamage),
                        new EnemySpec(EnemyKind.StormBalloon, "风暴气囊", 2, 12, 2));
                case EncounterId.Elite when normalizedVariant == 2:
                    return new EncounterDefinition(encounter, 2, "机体拆解编队",
                        new EnemySpec(EnemyKind.ShieldLeech, "盾蚀水蛭·重型", 0, 20, 6),
                        new EnemySpec(EnemyKind.HeatSeeker, "热寻隼·改", 1, 22, 6),
                        new EnemySpec(EnemyKind.StormBalloon, "风暴气囊", 2, 12, 2));
                case EncounterId.Elite when normalizedVariant == 4:
                    return new EncounterDefinition(encounter, 4, "双频天穹先遣队",
                        new EnemySpec(EnemyKind.CurtainHerald, "雷幕先导", 0, 22, 7),
                        new EnemySpec(EnemyKind.FluxSkimmer, "磁针鳐卫", 2, 24, 7));
                case EncounterId.Elite when normalizedVariant == 6:
                    return new EncounterDefinition(encounter, 6, "债务清算编队",
                        new EnemySpec(EnemyKind.DebtCollector, "债务清算官", 1, 30, 6),
                        new EnemySpec(EnemyKind.SalvageCorvid, "抵押鸦艇", 2, 14, 0));
                case EncounterId.Elite when normalizedVariant == 7:
                    return new EncounterDefinition(encounter, 7, "雷鸣唱诗班",
                        new EnemySpec(EnemyKind.ThunderChoir, "雷鸣唱诗班", 1, 32, 7),
                        new EnemySpec(EnemyKind.TimeLagJelly, "低音时差水母", 0, 14, 4),
                        new EnemySpec(EnemyKind.RustKite, "高音锈翼鸢", 2, 16, 5));
                case EncounterId.Elite when normalizedVariant == 8:
                    return new EncounterDefinition(encounter, 8, "莫比乌斯机库护航",
                        new EnemySpec(EnemyKind.MobiusHangar, "莫比乌斯机库", 1, 34, 6),
                        new EnemySpec(EnemyKind.PrismStowaway, "机库寄舱蟹", 0, 17, 5));
                case EncounterId.Elite when normalizedVariant == 9:
                    return new EncounterDefinition(encounter, 9, "空白雷网",
                        new EnemySpec(EnemyKind.NullBeacon, "空白信标·重型", 0, 20, 4),
                        new EnemySpec(EnemyKind.ThunderChoir, "残响唱诗班", 2, 28, 7));
                case EncounterId.Elite when normalizedVariant == 10:
                    return new EncounterDefinition(encounter, 10, "折叠清算所",
                        new EnemySpec(EnemyKind.DebtCollector, "债务清算官", 0, 28, 6),
                        new EnemySpec(EnemyKind.MobiusHangar, "莫比乌斯机库", 2, 30, 5));
                case EncounterId.Elite when normalizedVariant == 11:
                    return new EncounterDefinition(encounter, 11, "三频寄舱封锁",
                        new EnemySpec(EnemyKind.LaneTailor, "航道裁缝·重型", 0, 22, 5),
                        new EnemySpec(EnemyKind.PrismStowaway, "折光寄舱蟹", 1, 20, 6),
                        new EnemySpec(EnemyKind.NullBeacon, "空白信标", 2, 16, 3));
                case EncounterId.Elite:
                    return new EncounterDefinition(encounter, 3, "协议压制编队",
                        new EnemySpec(EnemyKind.HandJammer, "噪声织网·重型", 0, 20, 6),
                        new EnemySpec(EnemyKind.SignalHijacker, "协议劫持机·改", 1, 22, 4),
                        new EnemySpec(EnemyKind.RustKite, "追迹锈翼鸢", 2, 18, 5));
                case EncounterId.Hunt when normalizedVariant == 0:
                    return new EncounterDefinition(encounter, 0, "逆风追猎编队",
                        new EnemySpec(EnemyKind.RustKite, "追迹锈翼鸢", 0, 18, 6),
                        new EnemySpec(EnemyKind.MailEater, "截航噬邮兽", 2, 22, 7));
                case EncounterId.Hunt when normalizedVariant == 1:
                    return new EncounterDefinition(encounter, 1, "灾变追猎编队",
                        new EnemySpec(EnemyKind.CalamityDrone, "灾变无人机·追迹型", 0, 20, BattleState.CalamityStrikeDamage),
                        new EnemySpec(EnemyKind.RustKite, "追迹锈翼鸢", 2, 18, 5));
                case EncounterId.Hunt when normalizedVariant == 2:
                    return new EncounterDefinition(encounter, 2, "热源追猎编队",
                        new EnemySpec(EnemyKind.ShieldLeech, "盾蚀水蛭", 0, 20, 5),
                        new EnemySpec(EnemyKind.HeatSeeker, "热寻隼·追迹型", 2, 22, 6));
                case EncounterId.Hunt when normalizedVariant == 4:
                    return new EncounterDefinition(encounter, 4, "雷幕先导追猎群",
                        new EnemySpec(EnemyKind.CurtainHerald, "雷幕先导", 1, 22, 7),
                        new EnemySpec(EnemyKind.RustKite, "天穹锈翼鸢", 2, 18, 5));
                case EncounterId.Hunt when normalizedVariant == 6:
                    return new EncounterDefinition(encounter, 6, "拾荒鸦艇追猎群",
                        new EnemySpec(EnemyKind.SalvageCorvid, "拾荒鸦艇·头领", 0, 18, 0),
                        new EnemySpec(EnemyKind.RustKite, "护赃锈翼鸢", 2, 20, 6));
                case EncounterId.Hunt when normalizedVariant == 7:
                    return new EncounterDefinition(encounter, 7, "航道缝合追猎",
                        new EnemySpec(EnemyKind.LaneTailor, "航道裁缝·追迹型", 1, 22, 5),
                        new EnemySpec(EnemyKind.HeatSeeker, "热寻隼·改", 2, 21, 6));
                case EncounterId.Hunt when normalizedVariant == 8:
                    return new EncounterDefinition(encounter, 8, "折光延迟追猎",
                        new EnemySpec(EnemyKind.PrismStowaway, "折光寄舱蟹·追迹型", 0, 22, 7),
                        new EnemySpec(EnemyKind.TimeLagJelly, "时差水母·改", 2, 20, 6));
                case EncounterId.Hunt when normalizedVariant == 9:
                    return new EncounterDefinition(encounter, 9, "债务追索编队",
                        new EnemySpec(EnemyKind.DebtCollector, "债务清算官·追索型", 1, 32, 7),
                        new EnemySpec(EnemyKind.SignalHijacker, "资产协议机", 2, 20, 4));
                case EncounterId.Hunt when normalizedVariant == 10:
                    return new EncounterDefinition(encounter, 10, "莫比乌斯追猎环",
                        new EnemySpec(EnemyKind.MobiusHangar, "莫比乌斯机库", 1, 34, 6),
                        new EnemySpec(EnemyKind.RustKite, "折叠锈翼鸢", 0, 19, 6));
                case EncounterId.Hunt when normalizedVariant == 11:
                    return new EncounterDefinition(encounter, 11, "雷鸣追猎仪仗",
                        new EnemySpec(EnemyKind.ThunderChoir, "雷鸣唱诗班", 1, 34, 8),
                        new EnemySpec(EnemyKind.NullBeacon, "唱诗信标", 2, 18, 3));
                case EncounterId.Hunt:
                    return new EncounterDefinition(encounter, 3, "信号追猎编队",
                        new EnemySpec(EnemyKind.HandJammer, "噪声织网", 0, 20, 5),
                        new EnemySpec(EnemyKind.SignalHijacker, "协议劫持机·追迹型", 2, 22, 4));
                case EncounterId.MidBoss when normalizedVariant == 0:
                    return new EncounterDefinition(encounter, 0, "万箱母巢",
                        new EnemySpec(EnemyKind.NullBeacon, "退件加工舱", 0, 20, 4),
                        new EnemySpec(EnemyKind.CrateHive, "万箱母巢核心", 1, 42, 8),
                        new EnemySpec(EnemyKind.HandJammer, "无人机孵化舱", 2, 20, 5));
                case EncounterId.MidBoss when normalizedVariant == 1:
                    return new EncounterDefinition(encounter, 1, "三相气象钟",
                        new EnemySpec(EnemyKind.WeatherClock, "三相气象钟", 1, 52, 8));
                case EncounterId.MidBoss:
                    return new EncounterDefinition(encounter, 2, "莫比乌斯机库核心",
                        new EnemySpec(EnemyKind.MobiusHangar, "莫比乌斯机库·核心", 1, 46, 8),
                        new EnemySpec(EnemyKind.PrismStowaway, "外环寄舱蟹", 0, 18, 6),
                        new EnemySpec(EnemyKind.LaneTailor, "内环裁缝", 2, 18, 5));
                case EncounterId.Boss when normalizedVariant == 0:
                    return new EncounterDefinition(encounter, 0, "磁暴鳐核心",
                        new EnemySpec(EnemyKind.StormManta, "磁暴鳐", 1, 50, 8));
                case EncounterId.Boss when normalizedVariant == 1:
                    return new EncounterDefinition(encounter, 1, "雷幕云龙天穹",
                        new EnemySpec(EnemyKind.CloudWyrm, "雷幕云龙", 1, 48, 9));
                case EncounterId.Boss when normalizedVariant == 2:
                    return new EncounterDefinition(encounter, 2, "零号邮局",
                        new EnemySpec(EnemyKind.CourierZero, "零号邮差", 1, 54, 9));
                case EncounterId.Boss:
                    return new EncounterDefinition(encounter, 3, "倒悬天穹鲸",
                        new EnemySpec(EnemyKind.InvertedSkyWhale, "倒悬天穹鲸", 1, 58, 10));
                default:
                    throw new ArgumentOutOfRangeException(nameof(encounter), encounter, null);
            }
        }
    }
}
