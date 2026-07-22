using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyCourier
{
    public enum EnemyKind
    {
        RustKite,
        MailEater,
        StormBalloon,
        StormManta
    }

    public enum EncounterId
    {
        Skirmish,
        Elite,
        Boss
    }

    public enum CargoContract
    {
        FragileMedicine,
        CryoSerum,
        StormCore
    }

    [Serializable]
    public sealed class EnemyState
    {
        public EnemyKind Kind;
        public string Name;
        public int Lane;
        public int Health;
        public int MaxHealth;
        public int Damage;

        public bool Alive => Health > 0;

        public EnemyState(EnemyKind kind, string name, int lane, int health, int damage)
        {
            Kind = kind;
            Name = name;
            Lane = lane;
            Health = health;
            MaxHealth = health;
            Damage = damage;
        }
    }

    public sealed class BattleState
    {
        public const int MaxPlayerHealth = 36;
        public const int MaxHeat = 8;

        private readonly Random random = new Random(70422);
        private readonly List<CardId> drawPile = new List<CardId>();
        private readonly List<CardId> discardPile = new List<CardId>();
        private readonly HashSet<CardId> upgradedCards = new HashSet<CardId>();
        private readonly HashSet<ModuleId> installedModules = new HashSet<ModuleId>();

        public readonly List<CardId> Hand = new List<CardId>();
        public readonly List<EnemyState> Enemies = new List<EnemyState>();

        public int PlayerHealth { get; private set; }
        public int Armor { get; private set; }
        public int Energy { get; private set; }
        public int Heat { get; private set; }
        public int PlayerLane { get; private set; }
        public int Turn { get; private set; }
        public int CargoIntegrity { get; private set; }
        public CargoContract Cargo { get; private set; }
        public string LastCargoDamageReason { get; private set; }
        public string Log { get; private set; }
        public EncounterId Encounter { get; private set; }
        public int CardsPlayed { get; private set; }
        public int DamageTaken { get; private set; }
        public int OverheatCount { get; private set; }
        public int LockOn { get; private set; }
        public int Momentum { get; private set; }
        public string LastModuleProc { get; private set; }
        private bool changedLaneThisTurn;
        private int stationaryTurns;
        private bool vectorThrusterUsedThisTurn;
        private bool executionChipUsedThisTurn;

        public int DrawCount => drawPile.Count;
        public int DiscardCount => discardPile.Count;
        public bool Victory => Enemies.Count > 0 && Enemies.All(enemy => !enemy.Alive);
        public bool Defeat => PlayerHealth <= 0;
        public int HeatLimit => MaxHeat + (HasModule(ModuleId.CryoHeart) ? 2 : 0);
        public bool IsUpgraded(CardId card) => upgradedCards.Contains(card);
        public bool HasModule(ModuleId module) => installedModules.Contains(module);

        public void Reset()
        {
            var starterDeck = new List<CardId>
            {
                CardId.BurstFire, CardId.BurstFire,
                CardId.BankUp, CardId.BankUp,
                CardId.BankDown, CardId.BankDown,
                CardId.WindGuard, CardId.WindGuard,
                CardId.EmergencyCoolant, CardId.BroadsideVolley,
                CardId.OverloadAim, CardId.EngineOverclock
            };
            StartEncounter(EncounterId.Skirmish, starterDeck, MaxPlayerHealth, 3);
        }

        public void StartEncounter(EncounterId encounter, IList<CardId> deck, int startingHealth, int cargoIntegrity)
        {
            StartEncounter(encounter, deck, startingHealth, cargoIntegrity, CargoContract.FragileMedicine);
        }

        public void StartEncounter(EncounterId encounter, IList<CardId> deck, int startingHealth, int cargoIntegrity,
            CargoContract cargo)
        {
            StartEncounter(encounter, deck, startingHealth, cargoIntegrity, cargo, null, null);
        }

        public void StartEncounter(EncounterId encounter, IList<CardId> deck, int startingHealth, int cargoIntegrity,
            CargoContract cargo, ICollection<CardId> upgrades, ICollection<ModuleId> modules)
        {
            upgradedCards.Clear();
            if (upgrades != null)
                upgradedCards.UnionWith(upgrades);
            installedModules.Clear();
            if (modules != null)
                installedModules.UnionWith(modules);
            Encounter = encounter;
            Cargo = cargo;
            PlayerHealth = Math.Max(1, Math.Min(MaxPlayerHealth, startingHealth));
            Armor = HasModule(ModuleId.PrismBulkhead) ? 3 : 0;
            Energy = 3;
            Heat = 0;
            PlayerLane = 1;
            Turn = 1;
            CargoIntegrity = Math.Max(0, Math.Min(3, cargoIntegrity));
            LastCargoDamageReason = string.Empty;
            changedLaneThisTurn = false;
            stationaryTurns = 0;
            vectorThrusterUsedThisTurn = false;
            executionChipUsedThisTurn = false;
            CardsPlayed = 0;
            DamageTaken = 0;
            OverheatCount = 0;
            LockOn = 0;
            Momentum = 0;
            LastModuleProc = string.Empty;
            Log = "配送航线遭到拦截。观察敌人意图，然后打出卡牌。";

            ConfigureEnemies(encounter);

            drawPile.Clear();
            discardPile.Clear();
            Hand.Clear();
            drawPile.AddRange(deck);
            Shuffle(drawPile);
            DrawToFive();
        }

        public bool CanPlay(int handIndex)
        {
            if (handIndex < 0 || handIndex >= Hand.Count || Victory || Defeat)
                return false;

            CardSpec card = CardLibrary.Get(Hand[handIndex]);
            if (Energy < card.Cost)
                return false;

            switch (card.Id)
            {
                case CardId.BankUp: return PlayerLane > 0;
                case CardId.BankDown: return PlayerLane < 2;
                case CardId.BurstFire:
                case CardId.OverloadAim:
                case CardId.TargetLock:
                case CardId.RailPiercer:
                case CardId.PursuitShot:
                case CardId.AegisRam:
                case CardId.FrostLance:
                    return Enemies.Any(enemy => enemy.Alive && enemy.Lane == PlayerLane);
                default: return true;
            }
        }

        public void PlayCard(int handIndex)
        {
            if (!CanPlay(handIndex))
                return;

            CardId id = Hand[handIndex];
            CardSpec card = CardLibrary.Get(id);
            bool upgraded = IsUpgraded(id);
            bool damagingCard = id == CardId.BurstFire || id == CardId.BroadsideVolley || id == CardId.OverloadAim ||
                id == CardId.RailPiercer || id == CardId.PursuitShot || id == CardId.AegisRam ||
                id == CardId.FrostLance || id == CardId.MeltdownBurst || id == CardId.Scattershot || id == CardId.MissileSwarm;
            bool executionBoost = damagingCard && HasModule(ModuleId.ExecutionChip) && !executionChipUsedThisTurn;
            int heatBefore = Heat;
            LastModuleProc = string.Empty;
            CardsPlayed++;
            Energy -= card.Cost;

            switch (id)
            {
                case CardId.BurstFire:
                    DamageFirstInLane(6 + (upgraded ? 3 : 0) + (executionBoost ? 4 : 0));
                    break;
                case CardId.BankUp:
                    PlayerLane--;
                    changedLaneThisTurn = true;
                    Momentum = Math.Min(3, Momentum + 1);
                    Armor += upgraded ? 5 : 3;
                    if (HasModule(ModuleId.VectorThruster) && !vectorThrusterUsedThisTurn)
                    {
                        Energy++;
                        vectorThrusterUsedThisTurn = true;
                        LastModuleProc = "矢量回流器";
                    }
                    Log = upgraded ? "强化矢量喷口完成拉升，获得5点护盾。" : "飞机向上拉升，借助气流获得了3点护盾。";
                    break;
                case CardId.BankDown:
                    PlayerLane++;
                    changedLaneThisTurn = true;
                    Momentum = Math.Min(3, Momentum + 1);
                    Armor += upgraded ? 5 : 3;
                    if (HasModule(ModuleId.VectorThruster) && !vectorThrusterUsedThisTurn)
                    {
                        Energy++;
                        vectorThrusterUsedThisTurn = true;
                        LastModuleProc = "矢量回流器";
                    }
                    Log = upgraded ? "强化矢量喷口完成俯冲，获得5点护盾。" : "飞机俯冲一条航道，获得了3点护盾。";
                    break;
                case CardId.EmergencyCoolant:
                    Heat = Math.Max(0, Heat - (upgraded ? 5 : 3));
                    Log = "冷却剂流过老旧的引擎，热量下降了。";
                    break;
                case CardId.BroadsideVolley:
                    foreach (EnemyState enemy in Enemies.Where(enemy => enemy.Alive))
                        enemy.Health = Math.Max(0, enemy.Health - (3 + (upgraded ? 2 : 0) + (executionBoost ? 2 : 0)));
                    Log = "看似不起眼的弹幕铺满了整片天空。";
                    break;
                case CardId.WindGuard:
                    Armor += upgraded ? 9 : 6;
                    Log = "防风挡板锁定，货物绑带已经收紧。";
                    break;
                case CardId.OverloadAim:
                    DamageFirstInLane((heatBefore >= 4 ? 13 : 10) + (upgraded ? 3 : 0) + (executionBoost ? 4 : 0));
                    break;
                case CardId.EngineOverclock:
                    Energy += upgraded ? 2 : 1;
                    Log = "从引擎的未来借来了1点能量。";
                    break;
                case CardId.TargetLock:
                    LockOn = Math.Min(3, LockOn + (upgraded ? 2 : 1));
                    Log = $"火控完成校准，当前锁定层数：{LockOn}。";
                    break;
                case CardId.RailPiercer:
                    int railDamage = (upgraded ? 11 : 8) + LockOn * (upgraded ? 6 : 5) + (executionBoost ? 4 : 0);
                    DamageFirstInLane(railDamage);
                    LockOn = 0;
                    break;
                case CardId.VectorDash:
                    int oldLane = PlayerLane;
                    PlayerLane = PlayerLane == 2 ? 1 : PlayerLane + 1;
                    changedLaneThisTurn = PlayerLane != oldLane;
                    Momentum = Math.Min(3, Momentum + (upgraded ? 2 : 1));
                    Armor += upgraded ? 4 : 2;
                    if (HasModule(ModuleId.VectorThruster) && !vectorThrusterUsedThisTurn)
                    {
                        Energy++;
                        vectorThrusterUsedThisTurn = true;
                        LastModuleProc = "矢量回流器";
                    }
                    Log = $"矢量突进完成，动量提升至{Momentum}层。";
                    break;
                case CardId.PursuitShot:
                    int pursuitDamage = (upgraded ? 7 : 5) + Momentum * (upgraded ? 5 : 4) + (executionBoost ? 4 : 0);
                    DamageFirstInLane(pursuitDamage);
                    Momentum = 0;
                    break;
                case CardId.ReactivePlating:
                    Armor += upgraded ? 11 : 7;
                    Log = "反应装甲展开，护盾冲角已经充能。";
                    break;
                case CardId.AegisRam:
                    int armorBonus = Math.Min(Armor, upgraded ? 14 : 10);
                    DamageFirstInLane((upgraded ? 6 : 4) + armorBonus + (executionBoost ? 4 : 0));
                    Armor = 0;
                    break;
                case CardId.CryoPump:
                    int cooled = Math.Min(Heat, upgraded ? 6 : 4);
                    Heat -= cooled;
                    if (cooled >= 3)
                        Energy += upgraded ? 2 : 1;
                    Log = cooled >= 3 ? "低温泵回收废热并返还能量。" : "低温泵排出了剩余热量。";
                    break;
                case CardId.FrostLance:
                    int frostDamage = (upgraded ? 9 : 7) + (heatBefore <= 2 ? (upgraded ? 8 : 6) : 0) + (executionBoost ? 4 : 0);
                    DamageFirstInLane(frostDamage);
                    break;
                case CardId.HeatCharge:
                    Energy += upgraded ? 3 : 2;
                    if (upgraded)
                        Heat++;
                    Log = "热能被强行压入行动回路。";
                    break;
                case CardId.MeltdownBurst:
                    int pulseDamage = (upgraded ? 3 : 2) + heatBefore + (executionBoost ? 2 : 0);
                    DamageAll(pulseDamage);
                    Heat = 0;
                    Log = $"熔毁脉冲释放储热，对全体造成{pulseDamage}点伤害。";
                    break;
                case CardId.Scattershot:
                    DamageAll((upgraded ? 3 : 2) + (executionBoost ? 2 : 0));
                    Log = "散射弹幕覆盖全部航道。";
                    break;
                case CardId.MissileSwarm:
                    int missileCount = upgraded ? 6 : 4;
                    for (int i = 0; i < missileCount; i++)
                        DamageRandomAlive(2 + (executionBoost ? 1 : 0));
                    Log = $"蜂群飞弹完成{missileCount}次随机追踪打击。";
                    break;
            }

            if (executionBoost)
            {
                executionChipUsedThisTurn = true;
                LastModuleProc = "处决芯片";
            }

            Heat += card.Heat;
            discardPile.Add(id);
            Hand.RemoveAt(handIndex);
            ResolveOverheat();
        }

        public void EndTurn()
        {
            if (Victory || Defeat)
                return;

            LastCargoDamageReason = string.Empty;

            foreach (CardId card in Hand)
                discardPile.Add(card);
            Hand.Clear();

            foreach (EnemyState enemy in Enemies.Where(enemy => enemy.Alive))
                ResolveEnemy(enemy);

            if (Defeat)
            {
                Log = "邮运机坠毁了，这条航线仍未完成。";
                return;
            }

            ResolveCargoContract();

            Turn++;
            Armor = HasModule(ModuleId.PrismBulkhead) ? 3 : 0;
            Energy = 3;
            Heat = Math.Max(0, Heat - (HasModule(ModuleId.CryoHeart) ? 2 : 1));
            changedLaneThisTurn = false;
            vectorThrusterUsedThisTurn = false;
            executionChipUsedThisTurn = false;
            Momentum = 0;
            DrawToFive();
            Log = $"第{Turn}回合。敌方编队发出了新的行动信号。";
        }

        public string IntentFor(EnemyState enemy)
        {
            if (!enemy.Alive)
                return "已击落";

            if (enemy.Kind == EnemyKind.StormBalloon)
                return $"风暴 {enemy.Damage} / 全航道";

            if (enemy.Kind == EnemyKind.StormManta && Turn % 3 == 0)
                return "磁暴 5 / 全航道";

            if (enemy.Lane == PlayerLane)
                return $"攻击 {enemy.Damage}";

            return enemy.Lane < PlayerLane ? "下降1条航道" : "上升1条航道";
        }

        private void ResolveEnemy(EnemyState enemy)
        {
            if (enemy.Kind == EnemyKind.StormBalloon)
            {
                TakeDamage(enemy.Damage, false);
                return;
            }

            if (enemy.Kind == EnemyKind.StormManta && Turn % 3 == 0)
            {
                TakeDamage(5, true);
                return;
            }

            if (enemy.Lane == PlayerLane)
            {
                TakeDamage(enemy.Damage, true);
                return;
            }

            enemy.Lane += enemy.Lane < PlayerLane ? 1 : -1;
        }

        private void TakeDamage(int amount, bool threatensCargo)
        {
            int absorbed = Math.Min(Armor, amount);
            Armor -= absorbed;
            int hullDamage = amount - absorbed;
            int healthBefore = PlayerHealth;
            PlayerHealth = Math.Max(0, PlayerHealth - hullDamage);
            DamageTaken += healthBefore - PlayerHealth;

            if (Cargo == CargoContract.FragileMedicine && threatensCargo && hullDamage >= 6)
                DamageCargo("单次受到6点以上未抵消伤害");
        }

        private void ResolveCargoContract()
        {
            switch (Cargo)
            {
                case CargoContract.CryoSerum:
                    if (Heat >= 6)
                        DamageCargo("回合结束时热量达到6点");
                    break;
                case CargoContract.StormCore:
                    stationaryTurns = changedLaneThisTurn ? 0 : stationaryTurns + 1;
                    if (stationaryTurns >= 2)
                    {
                        DamageCargo("连续两回合没有切换航道");
                        stationaryTurns = 0;
                    }
                    break;
            }
        }

        private void DamageCargo(string reason)
        {
            if (CargoIntegrity <= 0)
                return;
            CargoIntegrity--;
            LastCargoDamageReason = reason;
        }

        private void DamageFirstInLane(int damage)
        {
            EnemyState target = Enemies.First(enemy => enemy.Alive && enemy.Lane == PlayerLane);
            target.Health = Math.Max(0, target.Health - damage);
            Log = $"{target.Name}受到了{damage}点伤害。";
        }

        private void DamageAll(int damage)
        {
            foreach (EnemyState enemy in Enemies.Where(enemy => enemy.Alive))
                enemy.Health = Math.Max(0, enemy.Health - damage);
        }

        private void DamageRandomAlive(int damage)
        {
            EnemyState[] alive = Enemies.Where(enemy => enemy.Alive).ToArray();
            if (alive.Length == 0)
                return;
            EnemyState target = alive[random.Next(alive.Length)];
            target.Health = Math.Max(0, target.Health - damage);
        }

        private void ResolveOverheat()
        {
            if (Heat < HeatLimit)
                return;

            Heat = 4;
            int healthBefore = PlayerHealth;
            PlayerHealth = Math.Max(0, PlayerHealth - 5);
            DamageTaken += healthBefore - PlayerHealth;
            OverheatCount++;
            Log = "引擎过热！机体受到5点伤害。";
        }

        private void DrawToFive()
        {
            while (Hand.Count < 5)
            {
                if (drawPile.Count == 0)
                {
                    if (discardPile.Count == 0)
                        return;
                    drawPile.AddRange(discardPile);
                    discardPile.Clear();
                    Shuffle(drawPile);
                }

                int last = drawPile.Count - 1;
                Hand.Add(drawPile[last]);
                drawPile.RemoveAt(last);
            }
        }

        private void Shuffle(List<CardId> pile)
        {
            for (int i = pile.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                CardId temp = pile[i];
                pile[i] = pile[j];
                pile[j] = temp;
            }
        }

        private void ConfigureEnemies(EncounterId encounter)
        {
            Enemies.Clear();
            switch (encounter)
            {
                case EncounterId.Skirmish:
                    Enemies.Add(new EnemyState(EnemyKind.RustKite, "锈翼鸢", 0, 14, 5));
                    Enemies.Add(new EnemyState(EnemyKind.MailEater, "噬邮兽", 1, 18, 7));
                    break;
                case EncounterId.Elite:
                    Enemies.Add(new EnemyState(EnemyKind.RustKite, "锈翼鸢·改", 0, 18, 6));
                    Enemies.Add(new EnemyState(EnemyKind.MailEater, "重装噬邮兽", 1, 22, 7));
                    Enemies.Add(new EnemyState(EnemyKind.StormBalloon, "风暴气囊", 2, 12, 3));
                    break;
                case EncounterId.Boss:
                    Enemies.Add(new EnemyState(EnemyKind.StormManta, "磁暴鳐", 1, 50, 8));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(encounter), encounter, null);
            }
        }
    }
}
