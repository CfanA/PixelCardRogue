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
        public const int VariantCount = 6;
        public const int BossVariantCount = 2;

        public static EncounterDefinition Get(EncounterId encounter, int variant)
        {
            int normalizedVariant = encounter == EncounterId.Boss
                ? Math.Abs(variant) % BossVariantCount
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
                case EncounterId.Hunt:
                    return new EncounterDefinition(encounter, 3, "信号追猎编队",
                        new EnemySpec(EnemyKind.HandJammer, "噪声织网", 0, 20, 5),
                        new EnemySpec(EnemyKind.SignalHijacker, "协议劫持机·追迹型", 2, 22, 4));
                case EncounterId.Boss when normalizedVariant == 0:
                    return new EncounterDefinition(encounter, 0, "磁暴鳐核心",
                        new EnemySpec(EnemyKind.StormManta, "磁暴鳐", 1, 50, 8));
                case EncounterId.Boss:
                    return new EncounterDefinition(encounter, 1, "雷幕云龙天穹",
                        new EnemySpec(EnemyKind.CloudWyrm, "雷幕云龙", 1, 48, 9));
                default:
                    throw new ArgumentOutOfRangeException(nameof(encounter), encounter, null);
            }
        }
    }
}
