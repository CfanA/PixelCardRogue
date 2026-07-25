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
        StormManta,
        CalamityDrone
    }

    public enum EncounterId
    {
        Skirmish,
        Elite,
        Hunt,
        Boss
    }

    public enum CargoContract
    {
        FragileMedicine,
        CryoSerum,
        StormCore,
        BlackBoxRelay
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
        public int Armor;
        public int MaxArmor;
        public int ChargeTargetLane = -1;
        public int ChargeDamageTaken;
        public bool ChargeInterrupted;
        public int Phase = 1;
        public bool PhaseTransitionPending;

        public bool Alive => Health > 0;

        public EnemyState(EnemyKind kind, string name, int lane, int health, int damage)
        {
            Kind = kind;
            Name = name;
            Lane = lane;
            Health = health;
            MaxHealth = health;
            Damage = damage;
            Armor = kind == EnemyKind.StormManta ? 10 : kind == EnemyKind.MailEater ? 5 :
                kind == EnemyKind.CalamityDrone ? 3 : 0;
            MaxArmor = Armor;
        }
    }

    public sealed class BattleState
    {
        public const int MaxPlayerHealth = 36;
        public const int MaxHeat = 8;
        public const int CalamityBreakDamage = 7;
        public const int CalamityStrikeDamage = 8;
        public const int TrackingShotDamage = 5;
        public const int BossPhaseOneBreakDamage = 9;
        public const int BossPhaseTwoBreakDamage = 13;
        public const int BossPhaseOneStrikeDamage = 10;
        public const int BossPhaseTwoStrikeDamage = 14;
        public const int BossPhaseTwoSplashDamage = 5;

        private readonly Random random = new Random(70422);
        private readonly List<CardId> drawPile = new List<CardId>();
        private readonly List<CardId> discardPile = new List<CardId>();
        private readonly HashSet<CardId> upgradedCards = new HashSet<CardId>();
        private readonly Dictionary<CardId, UpgradeBranch> upgradeBranches = new Dictionary<CardId, UpgradeBranch>();
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
        public int EncounterVariant { get; private set; }
        public string FormationName { get; private set; }
        public int CardsPlayed { get; private set; }
        public int DamageTaken { get; private set; }
        public int OverheatCount { get; private set; }
        public int CalamityInterrupts { get; private set; }
        public int CalamityEvades { get; private set; }
        public int CalamityHits { get; private set; }
        public int LockOn { get; private set; }
        public int Momentum { get; private set; }
        public int EvasionExposure { get; private set; }
        public int TrackingHits { get; private set; }
        public string LastModuleProc { get; private set; }
        public bool LastAttackCritical { get; private set; }
        public string LastArmorBreak { get; private set; }
        public string LastStatusTrigger { get; private set; }
        public int LastShieldAbsorbed { get; private set; }
        public bool LastShieldBroken { get; private set; }
        private bool changedLaneThisTurn;
        private int stationaryTurns;
        private bool vectorThrusterUsedThisTurn;
        private bool executionChipUsedThisTurn;
        private bool trackingShotResolvedThisTurn;
        private bool criticalArmed;
        private bool swarmPrimed;
        private bool aegisCapacitorUsedThisTurn;
        private bool zeroPointReactorUsedThisTurn;
        private bool redlineReactorUsedThisTurn;
        private bool ghostDecoderUsedThisTurn;
        private bool currentCardCritical;
        private bool currentAttackIgnoresArmor;

        public int DrawCount => drawPile.Count;
        public int DiscardCount => discardPile.Count;
        public bool Victory => Enemies.Count > 0 && Enemies.All(enemy => !enemy.Alive);
        public bool Defeat => PlayerHealth <= 0;
        public int HeatLimit => MaxHeat + (HasModule(ModuleId.CryoHeart) ? 2 : 0);
        public bool IsUpgraded(CardId card) => upgradedCards.Contains(card);
        public UpgradeBranch UpgradeBranchFor(CardId card) => upgradeBranches.TryGetValue(card, out UpgradeBranch branch)
            ? branch : UpgradeBranch.Alpha;
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
            CargoContract cargo, ICollection<CardId> upgrades, ICollection<ModuleId> modules, int encounterVariant = -1,
            IReadOnlyDictionary<CardId, UpgradeBranch> branches = null)
        {
            upgradedCards.Clear();
            if (upgrades != null)
                upgradedCards.UnionWith(upgrades);
            upgradeBranches.Clear();
            if (upgrades != null)
            {
                foreach (CardId card in upgrades)
                    upgradeBranches[card] = branches != null && branches.TryGetValue(card, out UpgradeBranch branch)
                        ? branch : UpgradeBranch.Alpha;
            }
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
            LastShieldAbsorbed = 0;
            LastShieldBroken = false;
            LastStatusTrigger = string.Empty;
            changedLaneThisTurn = false;
            stationaryTurns = 0;
            vectorThrusterUsedThisTurn = false;
            executionChipUsedThisTurn = false;
            CardsPlayed = 0;
            DamageTaken = 0;
            OverheatCount = 0;
            CalamityInterrupts = 0;
            CalamityEvades = 0;
            CalamityHits = 0;
            LockOn = 0;
            Momentum = 0;
            EvasionExposure = 0;
            TrackingHits = 0;
            trackingShotResolvedThisTurn = false;
            criticalArmed = false;
            swarmPrimed = false;
            aegisCapacitorUsedThisTurn = false;
            zeroPointReactorUsedThisTurn = false;
            redlineReactorUsedThisTurn = false;
            ghostDecoderUsedThisTurn = false;
            currentCardCritical = false;
            currentAttackIgnoresArmor = false;
            LastModuleProc = string.Empty;
            LastAttackCritical = false;
            LastArmorBreak = string.Empty;
            LastStatusTrigger = string.Empty;
            LastShieldAbsorbed = 0;
            LastShieldBroken = false;
            Log = "配送航线遭到拦截。观察敌人意图，然后打出卡牌。";

            EncounterVariant = encounter == EncounterId.Boss
                ? 0
                : encounterVariant >= 0 ? encounterVariant % 2 : random.Next(2);
            ConfigureEnemies(encounter, EncounterVariant);

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
                case CardId.SlipstreamStrike:
                case CardId.PrismEcho:
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
            damagingCard = damagingCard || id == CardId.CounterPursuit || id == CardId.InterceptMine ||
                id == CardId.SlipstreamStrike || id == CardId.PrismEcho;
            damagingCard = damagingCard || id == CardId.GhostProtocol;
            bool executionBoost = damagingCard && HasModule(ModuleId.ExecutionChip) && !executionChipUsedThisTurn;
            int heatBefore = Heat;
            LastModuleProc = string.Empty;
            LastAttackCritical = false;
            LastArmorBreak = string.Empty;
            LastStatusTrigger = string.Empty;
            currentAttackIgnoresArmor = id == CardId.RailPiercer && upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta;
            currentCardCritical = damagingCard && (criticalArmed ||
                (id == CardId.OverloadAim && heatBefore >= 4) ||
                (id == CardId.FrostLance && heatBefore <= 2) ||
                (id == CardId.RailPiercer && LockOn >= 2));
            if (damagingCard && !currentCardCritical && HasModule(ModuleId.ZeroPointReactor) &&
                heatBefore <= 2 && !zeroPointReactorUsedThisTurn)
            {
                currentCardCritical = true;
                zeroPointReactorUsedThisTurn = true;
                LastModuleProc = "零点反应堆";
            }
            if (damagingCard && !currentCardCritical && HasModule(ModuleId.RedlineReactor) &&
                heatBefore >= 5 && !redlineReactorUsedThisTurn)
            {
                currentCardCritical = true;
                redlineReactorUsedThisTurn = true;
                LastModuleProc = "红线反应堆";
            }
            if (damagingCard && criticalArmed)
            {
                criticalArmed = false;
                LastStatusTrigger = "必定暴击已触发";
            }
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
                    GainArmor(upgraded && UpgradeBranchFor(id) == UpgradeBranch.Alpha ? 5 : 3);
                    if (upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta)
                    {
                        EvasionExposure = Math.Max(0, EvasionExposure - 1);
                        LastStatusTrigger = "静默变轨：航迹暴露 -1";
                    }
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
                    GainArmor(upgraded && UpgradeBranchFor(id) == UpgradeBranch.Alpha ? 5 : 3);
                    if (upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta)
                    {
                        EvasionExposure = Math.Max(0, EvasionExposure - 1);
                        LastStatusTrigger = "静默变轨：航迹暴露 -1";
                    }
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
                    if (upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta)
                    {
                        criticalArmed = true;
                        LastStatusTrigger = "冷凝瞄准：下一次攻击必定暴击";
                    }
                    Log = "冷却剂流过老旧的引擎，热量下降了。";
                    break;
                case CardId.BroadsideVolley:
                    foreach (EnemyState enemy in Enemies.Where(enemy => enemy.Alive))
                        DamageEnemy(enemy, 3 + (upgraded ? 2 : 0) + (executionBoost ? 2 : 0) +
                            (HasModule(ModuleId.SwarmUplink) ? 1 : 0) + (swarmPrimed ? 2 : 0));
                    ConsumeSwarmPrime();
                    Log = "看似不起眼的弹幕铺满了整片天空。";
                    break;
                case CardId.WindGuard:
                    GainArmor(upgraded && UpgradeBranchFor(id) == UpgradeBranch.Alpha ? 9 : 6);
                    if (upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta)
                    {
                        criticalArmed = true;
                        LastStatusTrigger = "反射阵列：下一次攻击必定暴击";
                    }
                    Log = "防风挡板锁定，货物绑带已经收紧。";
                    break;
                case CardId.OverloadAim:
                    DamageFirstInLane((heatBefore >= 4 ? 13 : 10) + (upgraded ? 3 : 0) + (executionBoost ? 4 : 0));
                    break;
                case CardId.EngineOverclock:
                    Energy += upgraded ? 2 : 1;
                    if (upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta)
                    {
                        Energy--;
                        criticalArmed = true;
                        LastStatusTrigger = "精准超频：下一次攻击必定暴击";
                    }
                    Log = "从引擎的未来借来了1点能量。";
                    break;
                case CardId.TargetLock:
                    LockOn = Math.Min(3, LockOn + (upgraded ? 2 : 1));
                    if (upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta)
                    {
                        LockOn = Math.Min(3, LockOn - 1);
                        criticalArmed = true;
                        LastStatusTrigger = "弱点标定：下一次攻击必定暴击";
                    }
                    Log = $"火控完成校准，当前锁定层数：{LockOn}。";
                    break;
                case CardId.RailPiercer:
                    int railDamage = (upgraded ? 11 : 8) + LockOn * (upgraded ? 6 : 5) + (executionBoost ? 4 : 0);
                    DamageFirstInLane(railDamage);
                    LockOn = HasModule(ModuleId.PrecisionMatrix) ? Math.Min(1, LockOn) : 0;
                    if (HasModule(ModuleId.PrecisionMatrix))
                        LastModuleProc = "精密矩阵";
                    break;
                case CardId.VectorDash:
                    int oldLane = PlayerLane;
                    PlayerLane = PlayerLane == 2 ? 1 : PlayerLane + 1;
                    changedLaneThisTurn = PlayerLane != oldLane;
                    Momentum = Math.Min(3, Momentum + (upgraded ? 2 : 1));
                    GainArmor(upgraded && UpgradeBranchFor(id) == UpgradeBranch.Alpha ? 4 : 2);
                    if (upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta)
                    {
                        Momentum = Math.Min(3, Momentum - 1);
                        EvasionExposure = Math.Max(0, EvasionExposure - 1);
                        LastStatusTrigger = "幽灵矢量：航迹暴露 -1";
                    }
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
                    Momentum = upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta ? Math.Min(1, Momentum) : 0;
                    break;
                case CardId.ReactivePlating:
                    int armorBeforePlating = Armor;
                    GainArmor(upgraded && UpgradeBranchFor(id) == UpgradeBranch.Alpha ? 11 : 7);
                    if (upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta && armorBeforePlating >= 5)
                    {
                        Energy++;
                        LastStatusTrigger = "电容回流：能量 +1";
                    }
                    Log = "反应装甲展开，护盾冲角已经充能。";
                    break;
                case CardId.AegisRam:
                    int armorBonus = Math.Min(Armor, upgraded ? 14 : 10);
                    DamageFirstInLane((upgraded ? 6 : 4) + armorBonus + (executionBoost ? 4 : 0));
                    Armor = upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta ? Armor / 2 : 0;
                    break;
                case CardId.CryoPump:
                    int cooled = Math.Min(Heat, upgraded ? 6 : 4);
                    Heat -= cooled;
                    if (cooled >= 3)
                        Energy += upgraded ? 2 : 1;
                    if (upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta && cooled >= 3)
                    {
                        Energy--;
                        criticalArmed = true;
                        LastStatusTrigger = "零点窗口：下一次攻击必定暴击";
                    }
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
                    if (upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta)
                    {
                        Energy--;
                        criticalArmed = true;
                        LastStatusTrigger = "红线窗口：下一次攻击必定暴击";
                    }
                    Log = "热能被强行压入行动回路。";
                    break;
                case CardId.MeltdownBurst:
                    int pulseDamage = (upgraded ? 3 : 2) + heatBefore + (executionBoost ? 2 : 0) +
                        (HasModule(ModuleId.SwarmUplink) ? 1 : 0) + (swarmPrimed ? 2 : 0);
                    DamageAll(pulseDamage);
                    Heat = upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta ? 2 : 0;
                    ConsumeSwarmPrime();
                    Log = $"熔毁脉冲释放储热，对全体造成{pulseDamage}点伤害。";
                    break;
                case CardId.Scattershot:
                    DamageAll((upgraded ? 3 : 2) + (executionBoost ? 2 : 0) +
                        (HasModule(ModuleId.SwarmUplink) ? 1 : 0) + (swarmPrimed ? 2 : 0));
                    ConsumeSwarmPrime();
                    Log = "散射弹幕覆盖全部航道。";
                    break;
                case CardId.MissileSwarm:
                    int missileCount = upgraded ? 6 : 4;
                    for (int i = 0; i < missileCount; i++)
                        DamageRandomAlive(2 + (executionBoost ? 1 : 0) + (HasModule(ModuleId.SwarmUplink) ? 1 : 0) +
                            (swarmPrimed ? 1 : 0));
                    ConsumeSwarmPrime();
                    Log = $"蜂群飞弹完成{missileCount}次随机追踪打击。";
                    break;
                case CardId.SignalScrambler:
                    int exposureCleared = EvasionExposure;
                    EvasionExposure = 0;
                    GainArmor(upgraded && UpgradeBranchFor(id) == UpgradeBranch.Alpha ? 7 : 5 +
                        (upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta ? exposureCleared * 3 : 0));
                    TriggerGhostDecoder(exposureCleared);
                    Log = "扰频脉冲抹除了航迹特征。";
                    break;
                case CardId.CounterPursuit:
                    int counterExposure = EvasionExposure;
                    int counterDamage = (upgraded ? 9 : 7) + EvasionExposure * (upgraded ? 8 : 6) +
                        (executionBoost ? 4 : 0);
                    DamageLowestAlive(counterDamage);
                    EvasionExposure = upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta ? Math.Min(1, EvasionExposure) : 0;
                    TriggerGhostDecoder(counterExposure - EvasionExposure);
                    break;
                case CardId.AirBrake:
                    int exposureBeforeBrake = EvasionExposure;
                    bool shedExposure = EvasionExposure > 0;
                    EvasionExposure = Math.Max(0, EvasionExposure - (upgraded ? 2 : 1));
                    GainArmor(upgraded && UpgradeBranchFor(id) == UpgradeBranch.Alpha ? 8 : 5);
                    if (upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta)
                        Momentum = Math.Min(3, Momentum + 1);
                    if (shedExposure)
                        Energy++;
                    TriggerGhostDecoder(exposureBeforeBrake - EvasionExposure);
                    Log = shedExposure ? "矢量刹车甩脱追踪并回收了能量。" : "矢量刹车展开防御姿态。";
                    break;
                case CardId.InterceptMine:
                    foreach (EnemyState enemy in Enemies.Where(enemy => enemy.Alive && enemy.Lane != PlayerLane))
                        DamageEnemy(enemy, (upgraded ? 9 : 6) + (HasModule(ModuleId.SwarmUplink) ? 1 : 0) +
                            (swarmPrimed ? 2 : 0));
                    ConsumeSwarmPrime();
                    Log = "航道雷网在侧翼航线上连锁引爆。";
                    break;
                case CardId.LockCascade:
                    bool hadLock = LockOn > 0;
                    LockOn = Math.Min(3, LockOn + (upgraded ? 2 : 1));
                    if (hadLock && Enemies.Any(enemy => enemy.Alive && enemy.Lane == PlayerLane))
                        DamageFirstInLane(upgraded ? 6 : 4);
                    LastStatusTrigger = hadLock ? "连锁标定：协同射击" : "连锁标定：锁定建立";
                    break;
                case CardId.SlipstreamStrike:
                    DamageFirstInLane((upgraded ? 7 : 5) + Momentum * (upgraded ? 3 : 2));
                    LastStatusTrigger = $"尾流保留：动量 {Momentum}";
                    break;
                case CardId.PrismEcho:
                    GainArmor(upgraded ? 7 : 5);
                    DamageFirstInLane(Math.Max(2, Armor / 2));
                    LastStatusTrigger = "棱镜回响：护盾转化射束";
                    break;
                case CardId.ZeroPointCalibration:
                    int zeroCooled = Math.Min(Heat, upgraded ? 5 : 3);
                    Heat -= zeroCooled;
                    if (zeroCooled >= 3)
                        criticalArmed = true;
                    LastStatusTrigger = zeroCooled >= 3 ? "零点校准：下一次攻击必定暴击" : "零点校准：热量下降";
                    break;
                case CardId.RedlineIgnition:
                    Energy += upgraded ? 3 : 2;
                    if (heatBefore >= 3)
                        criticalArmed = true;
                    LastStatusTrigger = heatBefore >= 3 ? "红线点火：下一次攻击必定暴击" : "红线点火：能量注入";
                    break;
                case CardId.SwarmBeacon:
                    swarmPrimed = true;
                    LastStatusTrigger = "蜂群信标：下一张齐射获得强化";
                    break;
                case CardId.GhostProtocol:
                    EvasionExposure = Math.Min(3, EvasionExposure + 1);
                    DamageLowestAlive((upgraded ? 8 : 6) + EvasionExposure * (upgraded ? 6 : 5));
                    LastStatusTrigger = $"幽灵协议：主动暴露 {EvasionExposure}";
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
            LastAttackCritical = currentCardCritical;
            currentCardCritical = false;
            currentAttackIgnoresArmor = false;
        }

        public void EndTurn()
        {
            if (Victory || Defeat)
                return;

            LastCargoDamageReason = string.Empty;

            foreach (CardId card in Hand)
                discardPile.Add(card);
            Hand.Clear();

            EvasionExposure = changedLaneThisTurn
                ? Math.Min(3, EvasionExposure + 1)
                : Math.Max(0, EvasionExposure - 1);
            trackingShotResolvedThisTurn = false;

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
            aegisCapacitorUsedThisTurn = false;
            zeroPointReactorUsedThisTurn = false;
            redlineReactorUsedThisTurn = false;
            ghostDecoderUsedThisTurn = false;
            if (!HasModule(ModuleId.MomentumFlywheel))
                Momentum = 0;
            else if (Momentum > 0)
            {
                LastModuleProc = "动量飞轮";
                LastStatusTrigger = $"动量跨回合保留：{Momentum}";
            }
            DrawToFive();
            Log = $"第{Turn}回合。敌方编队发出了新的行动信号。";
        }

        public string IntentFor(EnemyState enemy)
        {
            if (!enemy.Alive)
                return "已击落";

            if (enemy.Kind == EnemyKind.CalamityDrone)
            {
                if (enemy.ChargeTargetLane < 0)
                    return "重新校准 / 跳过行动";
                if (enemy.ChargeInterrupted)
                    return "系统失衡 / 跳过行动";
                return $"灾变 {CalamityStrikeDamage} / 航道 {enemy.ChargeTargetLane + 1} · 打断 {enemy.ChargeDamageTaken}/{CalamityBreakDamage}";
            }

            if (enemy.Kind == EnemyKind.StormManta)
            {
                if (enemy.PhaseTransitionPending)
                    return "阶段转换 / 磁暴甲壳重构";
                if (enemy.ChargeTargetLane < 0)
                    return $"阶段 {enemy.Phase} / 重新锁定航道";
                if (enemy.ChargeInterrupted)
                    return "核心过载 / 大招已打断";
                int strike = enemy.Phase == 1 ? BossPhaseOneStrikeDamage : BossPhaseTwoStrikeDamage;
                int threshold = enemy.Phase == 1 ? BossPhaseOneBreakDamage : BossPhaseTwoBreakDamage;
                return enemy.Phase == 1
                    ? $"磁暴俯冲 {strike} / 航道 {enemy.ChargeTargetLane + 1} · 打断 {enemy.ChargeDamageTaken}/{threshold}"
                    : $"吞界磁暴 {strike}+邻道{BossPhaseTwoSplashDamage} / 航道 {enemy.ChargeTargetLane + 1} · 打断 {enemy.ChargeDamageTaken}/{threshold}";
            }

            if (enemy.Kind == EnemyKind.StormBalloon)
                return $"风暴 {enemy.Damage} / 全航道";

            if (enemy.Kind == EnemyKind.RustKite &&
                changedLaneThisTurn && EvasionExposure >= 1)
                return $"追踪 {TrackingShotDamage} / 继续换道将暴露航迹";

            if (enemy.Kind == EnemyKind.MailEater)
                return enemy.Lane == PlayerLane
                    ? $"封锁 {enemy.Damage + 2} / 换道可规避"
                    : $"封锁航道 {enemy.Lane + 1} / 进入将受{enemy.Damage + 2}伤害";

            if (enemy.Lane == PlayerLane)
                return $"攻击 {enemy.Damage}";

            return enemy.Lane < PlayerLane ? "下降1条航道" : "上升1条航道";
        }

        private void ResolveEnemy(EnemyState enemy)
        {
            if (enemy.Kind == EnemyKind.CalamityDrone)
            {
                if (enemy.ChargeTargetLane < 0)
                {
                    BeginCalamityCharge(enemy);
                    return;
                }
                if (enemy.ChargeInterrupted)
                {
                    CalamityInterrupts++;
                }
                else if (PlayerLane == enemy.ChargeTargetLane)
                {
                    CalamityHits++;
                    TakeDamage(CalamityStrikeDamage, true);
                }
                else
                {
                    CalamityEvades++;
                }
                EnterCalamityCooldown(enemy);
                return;
            }

            if (enemy.Kind == EnemyKind.StormManta)
            {
                if (enemy.PhaseTransitionPending)
                {
                    enemy.PhaseTransitionPending = false;
                    BeginBossCharge(enemy);
                    LastStatusTrigger = "BOSS PHASE 2：磁暴甲壳重构";
                    return;
                }
                if (enemy.ChargeTargetLane < 0)
                {
                    BeginBossCharge(enemy);
                    return;
                }
                if (enemy.ChargeInterrupted)
                {
                    CalamityInterrupts++;
                    LastStatusTrigger = "BOSS大招已打断";
                }
                else
                {
                    int laneDistance = Math.Abs(PlayerLane - enemy.ChargeTargetLane);
                    if (laneDistance == 0)
                    {
                        TakeDamage(enemy.Phase == 1 ? BossPhaseOneStrikeDamage : BossPhaseTwoStrikeDamage, true);
                        CalamityHits++;
                    }
                    else if (enemy.Phase == 2 && laneDistance == 1)
                    {
                        TakeDamage(BossPhaseTwoSplashDamage, false);
                        CalamityHits++;
                    }
                    else
                    {
                        CalamityEvades++;
                    }
                }
                EnterCalamityCooldown(enemy);
                return;
            }

            if (enemy.Kind == EnemyKind.StormBalloon)
            {
                TakeDamage(enemy.Damage, false);
                return;
            }

            if (!trackingShotResolvedThisTurn && changedLaneThisTurn && EvasionExposure >= 2 &&
                enemy.Kind == EnemyKind.RustKite)
            {
                trackingShotResolvedThisTurn = true;
                TrackingHits++;
                TakeDamage(TrackingShotDamage, false);
                return;
            }

            if (enemy.Lane == PlayerLane)
            {
                TakeDamage(enemy.Kind == EnemyKind.MailEater ? enemy.Damage + 2 : enemy.Damage, true);
                return;
            }

            if (enemy.Kind == EnemyKind.MailEater)
                return;

            enemy.Lane += enemy.Lane < PlayerLane ? 1 : -1;
        }

        private void TakeDamage(int amount, bool threatensCargo)
        {
            int armorBefore = Armor;
            int absorbed = Math.Min(Armor, amount);
            Armor -= absorbed;
            int hullDamage = amount - absorbed;
            int healthBefore = PlayerHealth;
            PlayerHealth = Math.Max(0, PlayerHealth - hullDamage);
            DamageTaken += healthBefore - PlayerHealth;
            LastShieldAbsorbed += absorbed;
            if (armorBefore > 0 && Armor == 0 && absorbed > 0)
                LastShieldBroken = true;

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
                case CargoContract.BlackBoxRelay:
                    if (EvasionExposure >= 2)
                        DamageCargo("回合结束时航迹暴露达到2层");
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
            DamageEnemy(target, damage);
            Log = $"{target.Name}受到了{damage}点伤害。";
        }

        private void DamageAll(int damage)
        {
            foreach (EnemyState enemy in Enemies.Where(enemy => enemy.Alive))
                DamageEnemy(enemy, damage);
        }

        private void DamageLowestAlive(int damage)
        {
            EnemyState target = Enemies.Where(enemy => enemy.Alive).OrderBy(enemy => enemy.Health).First();
            DamageEnemy(target, damage);
            Log = $"逆向追猎锁定{target.Name}，造成{damage}点伤害。";
        }

        private void DamageRandomAlive(int damage)
        {
            EnemyState[] alive = Enemies.Where(enemy => enemy.Alive).ToArray();
            if (alive.Length == 0)
                return;
            EnemyState target = alive[random.Next(alive.Length)];
            DamageEnemy(target, damage);
        }

        private void DamageEnemy(EnemyState enemy, int damage)
        {
            if (!enemy.Alive)
                return;

            int resolvedDamage = currentCardCritical ? (int)Math.Ceiling(damage * 1.5f) : damage;
            if (currentCardCritical)
                LastAttackCritical = true;
            int armorBefore = enemy.Armor;
            int absorbed = currentAttackIgnoresArmor ? 0 : Math.Min(enemy.Armor, resolvedDamage);
            enemy.Armor -= absorbed;
            resolvedDamage -= absorbed;
            if (armorBefore > 0 && enemy.Armor == 0)
                LastArmorBreak = enemy.Name;

            int healthBefore = enemy.Health;
            enemy.Health = Math.Max(0, enemy.Health - resolvedDamage);

            if (enemy.Kind == EnemyKind.StormManta && enemy.Alive && enemy.Phase == 1 &&
                enemy.Health <= enemy.MaxHealth / 2)
            {
                enemy.Phase = 2;
                enemy.PhaseTransitionPending = true;
                enemy.Armor = Math.Max(enemy.Armor, 8);
                enemy.MaxArmor = Math.Max(enemy.MaxArmor, enemy.Armor);
                EnterCalamityCooldown(enemy);
                LastStatusTrigger = "BOSS PHASE 2：吞界磁暴上线";
            }

            if (enemy.PhaseTransitionPending)
                return;

            if ((enemy.Kind != EnemyKind.CalamityDrone && enemy.Kind != EnemyKind.StormManta) ||
                enemy.ChargeInterrupted || !enemy.Alive)
                return;

            enemy.ChargeDamageTaken += (armorBefore - enemy.Armor) + (healthBefore - enemy.Health);
            int breakDamage = enemy.Kind == EnemyKind.StormManta
                ? enemy.Phase == 1 ? BossPhaseOneBreakDamage : BossPhaseTwoBreakDamage
                : CalamityBreakDamage;
            if (enemy.ChargeDamageTaken >= breakDamage)
                enemy.ChargeInterrupted = true;
        }

        private void GainArmor(int amount)
        {
            Armor += Math.Max(0, amount);
            if (HasModule(ModuleId.AegisCapacitor) && !aegisCapacitorUsedThisTurn && Armor >= 10)
            {
                Energy++;
                aegisCapacitorUsedThisTurn = true;
                LastModuleProc = "神盾电容";
                LastStatusTrigger = "护盾达到10：能量 +1";
            }
        }

        private void ConsumeSwarmPrime()
        {
            if (!swarmPrimed)
                return;
            swarmPrimed = false;
            LastStatusTrigger = "蜂群信标已触发";
        }

        private void TriggerGhostDecoder(int cleared)
        {
            if (cleared <= 0 || !HasModule(ModuleId.GhostDecoder) || ghostDecoderUsedThisTurn)
                return;
            Energy++;
            ghostDecoderUsedThisTurn = true;
            LastModuleProc = "幽灵解码器";
            LastStatusTrigger = "清除航迹：能量 +1";
        }

        private void BeginCalamityCharge(EnemyState enemy)
        {
            enemy.ChargeTargetLane = PlayerLane;
            enemy.ChargeDamageTaken = 0;
            enemy.ChargeInterrupted = false;
        }

        private void BeginBossCharge(EnemyState enemy)
        {
            enemy.ChargeTargetLane = PlayerLane;
            enemy.ChargeDamageTaken = 0;
            enemy.ChargeInterrupted = false;
        }

        private static void EnterCalamityCooldown(EnemyState enemy)
        {
            enemy.ChargeTargetLane = -1;
            enemy.ChargeDamageTaken = 0;
            enemy.ChargeInterrupted = false;
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

        private void ConfigureEnemies(EncounterId encounter, int variant)
        {
            Enemies.Clear();
            EncounterDefinition definition = EncounterCatalog.Get(encounter, variant);
            FormationName = definition.FormationName;
            foreach (EnemySpec enemy in definition.Enemies)
                Enemies.Add(new EnemyState(enemy.Kind, enemy.Name, enemy.Lane, enemy.Health, enemy.Damage));

            foreach (EnemyState enemy in Enemies.Where(enemy => enemy.Kind == EnemyKind.CalamityDrone))
                BeginCalamityCharge(enemy);
            foreach (EnemyState enemy in Enemies.Where(enemy => enemy.Kind == EnemyKind.StormManta))
                BeginBossCharge(enemy);
        }
    }
}
