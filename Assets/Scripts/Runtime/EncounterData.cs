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
        public const int VariantCount = 2;

        public static EncounterDefinition Get(EncounterId encounter, int variant)
        {
            int normalizedVariant = encounter == EncounterId.Boss ? 0 : Math.Abs(variant) % VariantCount;
            switch (encounter)
            {
                case EncounterId.Skirmish when normalizedVariant == 0:
                    return new EncounterDefinition(encounter, 0, "标准拦截编队",
                        new EnemySpec(EnemyKind.RustKite, "锈翼鸢", 0, 14, 5),
                        new EnemySpec(EnemyKind.MailEater, "噬邮兽", 1, 18, 7));
                case EncounterId.Skirmish:
                    return new EncounterDefinition(encounter, 1, "灾变猎杀编队",
                        new EnemySpec(EnemyKind.RustKite, "锈翼鸢", 0, 14, 5),
                        new EnemySpec(EnemyKind.CalamityDrone, "灾变无人机", 1, 16, BattleState.CalamityStrikeDamage));
                case EncounterId.Elite when normalizedVariant == 0:
                    return new EncounterDefinition(encounter, 0, "雷暴封锁编队",
                        new EnemySpec(EnemyKind.RustKite, "锈翼鸢·改", 0, 18, 6),
                        new EnemySpec(EnemyKind.MailEater, "重装噬邮兽", 1, 22, 7),
                        new EnemySpec(EnemyKind.StormBalloon, "风暴气囊", 2, 12, 3));
                case EncounterId.Elite:
                    return new EncounterDefinition(encounter, 1, "灾变封锁编队",
                        new EnemySpec(EnemyKind.MailEater, "噬邮兽·护航型", 0, 20, 6),
                        new EnemySpec(EnemyKind.CalamityDrone, "灾变无人机·改", 1, 20, BattleState.CalamityStrikeDamage),
                        new EnemySpec(EnemyKind.StormBalloon, "风暴气囊", 2, 12, 2));
                case EncounterId.Hunt when normalizedVariant == 0:
                    return new EncounterDefinition(encounter, 0, "逆风追猎编队",
                        new EnemySpec(EnemyKind.RustKite, "追迹锈翼鸢", 0, 18, 6),
                        new EnemySpec(EnemyKind.MailEater, "截航噬邮兽", 2, 22, 7));
                case EncounterId.Hunt:
                    return new EncounterDefinition(encounter, 1, "灾变追猎编队",
                        new EnemySpec(EnemyKind.CalamityDrone, "灾变无人机·追迹型", 0, 20, BattleState.CalamityStrikeDamage),
                        new EnemySpec(EnemyKind.RustKite, "追迹锈翼鸢", 2, 18, 5));
                case EncounterId.Boss:
                    return new EncounterDefinition(encounter, 0, "磁暴鳐核心",
                        new EnemySpec(EnemyKind.StormManta, "磁暴鳐", 1, 50, 8));
                default:
                    throw new ArgumentOutOfRangeException(nameof(encounter), encounter, null);
            }
        }
    }
}
