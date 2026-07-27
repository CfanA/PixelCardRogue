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
        CalamityDrone,
        ShieldLeech,
        HandJammer,
        HeatSeeker,
        SignalHijacker,
        CloudWyrm,
        CurtainHerald,
        FluxSkimmer,
        TimeLagJelly,
        SalvageCorvid,
        LaneTailor,
        NullBeacon,
        PrismStowaway,
        DebtCollector,
        ThunderChoir,
        MobiusHangar,
        CrateHive,
        WeatherClock,
        CourierZero,
        InvertedSkyWhale
    }

    public enum EncounterId
    {
        Skirmish,
        Elite,
        Hunt,
        MidBoss,
        Boss
    }

    public enum CargoContract
    {
        FragileMedicine,
        CryoSerum,
        StormCore,
        BlackBoxRelay,
        SignalSeed
    }

    public enum AirframeModification
    {
        None,
        SealedBulkhead,
        OpenAvionics,
        RedlineTurbine
    }

    public enum PlayerDamageSource
    {
        DirectAttack,
        LaneBlock,
        StormField,
        TrackingShot,
        CalamityStrike,
        BossStrike,
        BossSplash,
        Overheat,
        HandJam,
        HeatSeek,
        BossWidebandJam,
        BossThermalLock,
        BossCurtain,
        PreludeCurtain,
        PreludeMagnet,
        TimePulse,
        RefractionShot,
        DebtCollection,
        ChoirResonance,
        WeatherHazard,
        MidBossStrike,
        CourierAudit,
        SkyWhaleTide
    }

    public enum BossContractProtocol
    {
        SealMirror,
        CryoInversion,
        VectorIntercept,
        GhostTrace,
        ReserveSiphon
    }

    public enum BossAirframeProtocol
    {
        None,
        ShieldCrack,
        WidebandJam,
        ThermalLock
    }

    public enum CardPlayBlockReason
    {
        None,
        InvalidCard,
        BattleEnded,
        InsufficientEnergy,
        NoSameLaneTarget,
        NoOtherLaneTarget,
        NoTarget,
        RequiresArmor,
        RequiresLockOn,
        RequiresMomentum,
        RequiresExposure,
        RequiresSurplusEnergy,
        AtTopLane,
        AtBottomLane
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
        public int ChargeCycle;
        public int Phase = 1;
        public bool PhaseTransitionPending;
        public int MechanicValue;
        public int MechanicTarget = -1;
        public bool Escaped;

        public bool Alive => Health > 0;

        public EnemyState(EnemyKind kind, string name, int lane, int health, int damage)
        {
            Kind = kind;
            Name = name;
            Lane = lane;
            Health = health;
            MaxHealth = health;
            Damage = damage;
            Armor = kind == EnemyKind.StormManta ? 10 : kind == EnemyKind.CloudWyrm ? 8 :
                kind == EnemyKind.CourierZero ? 9 : kind == EnemyKind.InvertedSkyWhale ? 12 :
                kind == EnemyKind.CrateHive ? 8 : kind == EnemyKind.WeatherClock ? 7 :
                kind == EnemyKind.MobiusHangar ? 6 : kind == EnemyKind.DebtCollector ? 4 :
                kind == EnemyKind.MailEater ? 5 :
                kind == EnemyKind.CalamityDrone ? 3 : 0;
            MaxArmor = Armor;
        }
    }

    public sealed partial class BattleState
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
        public const int BossAdaptiveArmor = 3;
        public const int BossAdaptationDamage = 4;
        public const int BossStoryArmorShift = 4;
        public const int CloudWyrmPhaseOneBreakDamage = 8;
        public const int CloudWyrmPhaseTwoBreakDamage = 12;
        public const int CloudWyrmPhaseOneStrikeDamage = 9;
        public const int CloudWyrmPhaseTwoStrikeDamage = 12;
        public const int PreludeBreakDamage = 6;

        private Random random = new Random(RunSeedUtility.LegacySeed);
        private readonly List<CardId> drawPile = new List<CardId>();
        private readonly List<CardId> discardPile = new List<CardId>();
        private readonly List<CardId> exhaustPile = new List<CardId>();
        private readonly HashSet<CardId> upgradedCards = new HashSet<CardId>();
        private readonly Dictionary<CardId, UpgradeBranch> upgradeBranches = new Dictionary<CardId, UpgradeBranch>();
        private readonly HashSet<ModuleId> installedModules = new HashSet<ModuleId>();
        private readonly LaneFieldKind[] laneFields = new LaneFieldKind[3];
        private readonly int[] laneFieldStrengths = new int[3];

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
        public AirframeModification Modification { get; private set; }
        public RouteStoryState StoryState { get; private set; }
        public RouteIntel Intel { get; private set; }
        public string LastCargoDamageReason { get; private set; }
        public string Log { get; private set; }
        public EncounterId Encounter { get; private set; }
        public int EncounterVariant { get; private set; }
        public int Seed { get; private set; } = RunSeedUtility.LegacySeed;
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
        public bool ChangedLaneThisTurn => changedLaneThisTurn;
        public PlayerDamageSource LastDamageSource { get; private set; }
        public string LastDamageDealer { get; private set; }
        public int LastHullDamage { get; private set; }
        public PlayerDamageSource DefeatSource { get; private set; }
        public string DefeatDealer { get; private set; }
        public int DefeatDamage { get; private set; }
        public int DefeatRawDamage { get; private set; }
        public int DefeatShieldAbsorbed { get; private set; }
        public int DefeatHullBefore { get; private set; }
        public int DefeatTurn { get; private set; }
        public bool HasDefeatCause { get; private set; }
        public string LastModuleProc { get; private set; }
        public bool LastAttackCritical { get; private set; }
        public string LastArmorBreak { get; private set; }
        public string LastStatusTrigger { get; private set; }
        public int LastShieldAbsorbed { get; private set; }
        public bool LastShieldBroken { get; private set; }
        public bool ContractPassiveTriggered { get; private set; }
        public int ContractPassiveProcs { get; private set; }
        public int CardsHeldAtEndTurn { get; private set; }
        public int DeferredEnergy { get; private set; }
        public int DeliveredEnergyThisTurn { get; private set; }
        public int DeferredSingleDamage { get; private set; }
        public int DeferredVolleyDamage { get; private set; }
        private bool changedLaneThisTurn;
        private int armorAtEnemyPhase;
        private int heatAtEnemyPhase;
        private int energyAtEnemyPhase;
        private int lockOnAtEnemyPhase;
        private int momentumAtEnemyPhase;
        private int exposureAtEnemyPhase;
        private int stationaryTurns;
        private bool contractPassiveUsedThisTurn;
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
        private bool retainHandThisTurn;
        private bool currentAttackIgnoresArmor;
        private int currentExpandedDamageBonus;

        public int DrawCount => drawPile.Count;
        public int DiscardCount => discardPile.Count;
        public int ExhaustCount => exhaustPile.Count;
        public int HandTarget => Modification == AirframeModification.OpenAvionics ? 6 :
            Modification == AirframeModification.SealedBulkhead ? 4 : 5;
        public int TurnEnergy => Modification == AirframeModification.RedlineTurbine ? 4 : 3;
        public BossContractProtocol ActiveBossContractProtocol => ContractCatalog.BossProtocol(Cargo);
        public BossAirframeProtocol ActiveBossAirframeProtocol => Modification switch
        {
            AirframeModification.SealedBulkhead => BossAirframeProtocol.ShieldCrack,
            AirframeModification.OpenAvionics => BossAirframeProtocol.WidebandJam,
            AirframeModification.RedlineTurbine => BossAirframeProtocol.ThermalLock,
            _ => BossAirframeProtocol.None
        };
        public BossStoryAlignment ActiveBossStoryAlignment => RouteStoryRules.BossAlignment(StoryState);
        public bool Victory => Enemies.Count > 0 && Enemies.All(enemy => !enemy.Alive);
        public bool Defeat => PlayerHealth <= 0;
        public int HeatLimit => MaxHeat + (HasModule(ModuleId.CryoHeart) ? 2 : 0);
        public bool IsUpgraded(CardId card) => upgradedCards.Contains(card);
        public UpgradeBranch UpgradeBranchFor(CardId card) => upgradeBranches.TryGetValue(card, out UpgradeBranch branch)
            ? branch : UpgradeBranch.Alpha;
        public bool HasModule(ModuleId module) => installedModules.Contains(module);
        public LaneFieldKind LaneFieldAt(int lane) => lane >= 0 && lane < laneFields.Length
            ? laneFields[lane] : LaneFieldKind.None;
        public int LaneFieldStrengthAt(int lane) => lane >= 0 && lane < laneFieldStrengths.Length
            ? laneFieldStrengths[lane] : 0;

        public bool BossContractProtocolWillTrigger()
        {
            if (Modification == AirframeModification.None)
                return false;
            return ActiveBossContractProtocol switch
            {
                BossContractProtocol.SealMirror => LockOn > 0,
                BossContractProtocol.CryoInversion => Heat <= 1,
                BossContractProtocol.VectorIntercept => Momentum > 0,
                BossContractProtocol.GhostTrace => EvasionExposure > 0,
                BossContractProtocol.ReserveSiphon => Energy == 1,
                _ => false
            };
        }

        public bool BossAirframeProtocolWillTrigger()
        {
            return ActiveBossAirframeProtocol switch
            {
                BossAirframeProtocol.ShieldCrack => Armor >= 5,
                BossAirframeProtocol.WidebandJam => Hand.Count >= 5,
                BossAirframeProtocol.ThermalLock => Heat >= 4,
                _ => false
            };
        }

        public void Reset()
        {
            var starterDeck = CardPoolCatalog.CreateStarterDeck(CargoContract.FragileMedicine);
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
            IReadOnlyDictionary<CardId, UpgradeBranch> branches = null, int? seed = null,
            AirframeModification modification = AirframeModification.None,
            RouteStoryState storyState = RouteStoryState.None,
            RouteIntel intel = RouteIntel.None,
            int startingHeat = 0)
        {
            if (seed.HasValue)
            {
                Seed = seed.Value == 0 ? RunSeedUtility.LegacySeed : seed.Value;
                random = new Random(Seed);
            }
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
            Modification = modification;
            StoryState = storyState;
            Intel = intel;
            PlayerHealth = Math.Max(1, Math.Min(MaxPlayerHealth, startingHealth));
            Armor = StartingArmor();
            Energy = TurnEnergy;
            Heat = Math.Max(0, Math.Min(HeatLimit - 1, startingHeat));
            PlayerLane = 1;
            Turn = 1;
            CargoIntegrity = Math.Max(0, Math.Min(3, cargoIntegrity));
            LastCargoDamageReason = string.Empty;
            LastShieldAbsorbed = 0;
            LastShieldBroken = false;
            ContractPassiveTriggered = false;
            ContractPassiveProcs = 0;
            CardsHeldAtEndTurn = 0;
            DeferredEnergy = 0;
            DeliveredEnergyThisTurn = 0;
            DeferredSingleDamage = 0;
            DeferredVolleyDamage = 0;
            currentExpandedDamageBonus = 0;
            Array.Clear(laneFields, 0, laneFields.Length);
            Array.Clear(laneFieldStrengths, 0, laneFieldStrengths.Length);
            LastStatusTrigger = string.Empty;
            changedLaneThisTurn = false;
            stationaryTurns = 0;
            contractPassiveUsedThisTurn = false;
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
            EvasionExposure = Modification == AirframeModification.OpenAvionics ? 1 : 0;
            TrackingHits = 0;
            LastDamageSource = PlayerDamageSource.DirectAttack;
            LastDamageDealer = string.Empty;
            LastHullDamage = 0;
            DefeatSource = PlayerDamageSource.DirectAttack;
            DefeatDealer = string.Empty;
            DefeatDamage = 0;
            DefeatRawDamage = 0;
            DefeatShieldAbsorbed = 0;
            DefeatHullBefore = 0;
            DefeatTurn = 0;
            HasDefeatCause = false;
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

            ResetExpandedEnemyState();
            EncounterVariant = encounter == EncounterId.Boss
                ? encounterVariant >= 0 ? encounterVariant % EncounterCatalog.BossVariantCount : 0
                : encounter == EncounterId.MidBoss
                    ? encounterVariant >= 0 ? encounterVariant % EncounterCatalog.MidBossVariantCount : 0
                    : encounterVariant >= 0 ? encounterVariant % EncounterCatalog.VariantCount : random.Next(2);
            ConfigureEnemies(encounter, EncounterVariant);

            drawPile.Clear();
            discardPile.Clear();
            exhaustPile.Clear();
            Hand.Clear();
            retainHandThisTurn = false;
            drawPile.AddRange(deck);
            Shuffle(drawPile);
            DrawToTarget();
            EnsureOpeningDamageCard();
        }

        public bool CanPlay(int handIndex)
        {
            return GetCardPlayBlockReason(handIndex) == CardPlayBlockReason.None;
        }

        public CardPlayBlockReason GetCardPlayBlockReason(int handIndex)
        {
            if (handIndex < 0 || handIndex >= Hand.Count)
                return CardPlayBlockReason.InvalidCard;
            if (Victory || Defeat)
                return CardPlayBlockReason.BattleEnded;

            CardSpec card = CardLibrary.Get(Hand[handIndex]);
            if (Energy < card.Cost)
                return CardPlayBlockReason.InsufficientEnergy;

            if (ExpandedCardCatalog.Contains(card.Id))
            {
                CardTargetRequirement requirement = ExpandedCardCatalog.TargetRequirement(card.Id);
                if (requirement == CardTargetRequirement.SameLane &&
                    !Enemies.Any(enemy => enemy.Alive && enemy.Lane == PlayerLane))
                    return CardPlayBlockReason.NoSameLaneTarget;
                if (requirement == CardTargetRequirement.AnyEnemy && !Enemies.Any(enemy => enemy.Alive))
                    return CardPlayBlockReason.NoTarget;
                if (requirement == CardTargetRequirement.OtherLane &&
                    !Enemies.Any(enemy => enemy.Alive && enemy.Lane != PlayerLane))
                    return CardPlayBlockReason.NoOtherLaneTarget;
                return ExpandedCardBlockReason(card.Id);
            }

            switch (card.Id)
            {
                case CardId.BankUp:
                    return PlayerLane > 0 ? CardPlayBlockReason.None : CardPlayBlockReason.AtTopLane;
                case CardId.BankDown:
                    return PlayerLane < 2 ? CardPlayBlockReason.None : CardPlayBlockReason.AtBottomLane;
                case CardId.BurstFire:
                case CardId.OverloadAim:
                case CardId.TargetLock:
                case CardId.RailPiercer:
                case CardId.PursuitShot:
                case CardId.AegisRam:
                case CardId.FrostLance:
                case CardId.SlipstreamStrike:
                case CardId.PrismEcho:
                case CardId.ReserveShot:
                    return Enemies.Any(enemy => enemy.Alive && enemy.Lane == PlayerLane)
                        ? CardPlayBlockReason.None
                        : CardPlayBlockReason.NoSameLaneTarget;
                default:
                    return CardPlayBlockReason.None;
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
            damagingCard = damagingCard || id == CardId.ReserveShot;
            damagingCard = damagingCard || ExpandedCardCatalog.IsDamaging(id);
            bool executionBoost = damagingCard && HasModule(ModuleId.ExecutionChip) && !executionChipUsedThisTurn;
            int heatBefore = Heat;
            LastModuleProc = string.Empty;
            LastAttackCritical = false;
            LastArmorBreak = string.Empty;
            LastStatusTrigger = string.Empty;
            ContractPassiveTriggered = false;
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
            cardsPlayedThisTurn++;
            Energy -= card.Cost;
            TriggerSignalSeedPassive();

            if (ExpandedCardCatalog.Contains(id))
            {
                ExpandedUpgradeSnapshot upgradeSnapshot = BeginExpandedUpgrade(id);
                ResolveExpandedCard(id, executionBoost, heatBefore, handIndex);
                FinishExpandedUpgrade(id, upgradeSnapshot);
            }
            else switch (id)
            {
                case CardId.BurstFire:
                    DamageFirstInLane(6 + (upgraded ? 3 : 0) + (executionBoost ? 4 : 0));
                    break;
                case CardId.BankUp:
                    MovePlayerToLane(PlayerLane - 1);
                    Momentum = Math.Min(3, Momentum + 1);
                    GainArmor(upgraded && UpgradeBranchFor(id) == UpgradeBranch.Alpha ? 5 : 3);
                    if (upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta)
                    {
                        ReduceExposure(1);
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
                    MovePlayerToLane(PlayerLane + 1);
                    Momentum = Math.Min(3, Momentum + 1);
                    GainArmor(upgraded && UpgradeBranchFor(id) == UpgradeBranch.Alpha ? 5 : 3);
                    if (upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta)
                    {
                        ReduceExposure(1);
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
                    ApplyCooling(upgraded ? 5 : 3);
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
                    MovePlayerToLane(PlayerLane == 2 ? 1 : PlayerLane + 1);
                    Momentum = Math.Min(3, Momentum + (upgraded ? 2 : 1));
                    GainArmor(upgraded && UpgradeBranchFor(id) == UpgradeBranch.Alpha ? 4 : 2);
                    if (upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta)
                    {
                        Momentum = Math.Min(3, Momentum - 1);
                        ReduceExposure(1);
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
                    int cooled = ApplyCooling(upgraded ? 6 : 4);
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
                    int frostDamage = (upgraded ? 9 : 7) + (heatBefore <= 2 ? (upgraded ? 7 : 5) : 0) + (executionBoost ? 4 : 0);
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
                    int exposureCleared = ReduceExposure(EvasionExposure);
                    GainArmor(upgraded && UpgradeBranchFor(id) == UpgradeBranch.Alpha ? 7 : 5 +
                        (upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta ? exposureCleared * 3 : 0));
                    TriggerGhostDecoder(exposureCleared);
                    Log = "扰频脉冲抹除了航迹特征。";
                    break;
                case CardId.CounterPursuit:
                    int counterDamage = (upgraded ? 9 : 7) + EvasionExposure * (upgraded ? 8 : 6) +
                        (executionBoost ? 4 : 0);
                    DamageLowestAlive(counterDamage);
                    int retainedExposure = upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta
                        ? Math.Min(1, EvasionExposure) : 0;
                    int counterCleared = ReduceExposure(EvasionExposure - retainedExposure);
                    TriggerGhostDecoder(counterCleared);
                    break;
                case CardId.AirBrake:
                    bool shedExposure = EvasionExposure > 0;
                    int brakeCleared = ReduceExposure(upgraded ? 2 : 1);
                    GainArmor(upgraded && UpgradeBranchFor(id) == UpgradeBranch.Alpha ? 8 : 5);
                    if (upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta)
                        Momentum = Math.Min(3, Momentum + 1);
                    if (shedExposure)
                        Energy++;
                    TriggerGhostDecoder(brakeCleared);
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
                    int zeroCooled = ApplyCooling(upgraded ? 5 : 3);
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
                case CardId.ReactiveSeal:
                    GainArmor(6);
                    if (LockOn > 0)
                    {
                        LockOn--;
                        GainArmor(6);
                        LastStatusTrigger = "再生密封：消耗锁定，密封强度翻倍";
                    }
                    else
                    {
                        LastStatusTrigger = "再生密封：基础密封展开";
                    }
                    break;
                case CardId.PhaseExchange:
                    int exchangedHeat = ApplyCooling(Heat);
                    int exchangeDraw = Math.Min(2, exchangedHeat / 3);
                    DrawCards(exchangeDraw);
                    LastStatusTrigger = $"相变置换：降低{exchangedHeat}热量，抽取{exchangeDraw}张牌";
                    break;
                case CardId.EyeTransit:
                    int lanesCrossed = Math.Abs((PlayerLane == 0 ? 2 : 0) - PlayerLane);
                    MovePlayerToLane(PlayerLane == 0 ? 2 : 0);
                    Momentum = Math.Min(3, Momentum + lanesCrossed);
                    LastStatusTrigger = $"风眼穿越：跨越{lanesCrossed}条航道，动量 {Momentum}";
                    break;
                case CardId.FalseTelemetry:
                    EvasionExposure = Math.Min(3, EvasionExposure + 2);
                    DrawCards(2);
                    LastStatusTrigger = $"伪造遥测：航迹暴露 {EvasionExposure}，抽取2张牌";
                    break;
                case CardId.ReserveShot:
                    DamageFirstInLane((upgraded ? 11 : 8) + (Energy == 1 ? 4 : 0) +
                        (executionBoost ? 4 : 0));
                    if (upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta && Energy == 1)
                    {
                        LockOn = Math.Min(3, LockOn + 1);
                        LastStatusTrigger = "余量校准：锁定 +1";
                    }
                    break;
                case CardId.StandbyField:
                    GainArmor((upgraded && UpgradeBranchFor(id) == UpgradeBranch.Alpha ? 9 : 6) +
                        (Energy == 1 ? 4 : 0));
                    if (upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta && Energy == 1)
                        ApplyCooling(1);
                    Log = Energy == 1 ? "待机力场接入保留回路，护盾增幅。" : "待机力场展开。";
                    break;
                case CardId.TightSchedule:
                    DrawCards(upgraded && UpgradeBranchFor(id) == UpgradeBranch.Alpha ? 3 : 2);
                    if (Energy == 1)
                    {
                        LockOn = Math.Min(3, LockOn + 1);
                        LastStatusTrigger = "紧凑班次：锁定 +1";
                    }
                    if (upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta && Energy == 1)
                        ApplyCooling(2);
                    Log = "班次压缩完成，新的指令已经入列。";
                    break;
                case CardId.RelayStep:
                    MovePlayerToLane(PlayerLane == 0 ? 2 : 0);
                    Momentum = Math.Min(3, Momentum + (upgraded ? 2 : 1));
                    GainArmor(upgraded && UpgradeBranchFor(id) == UpgradeBranch.Alpha ? 4 : 2);
                    if (Energy == 1)
                        ReduceExposure(upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta ? 2 : 1);
                    if (HasModule(ModuleId.VectorThruster) && !vectorThrusterUsedThisTurn)
                    {
                        Energy++;
                        vectorThrusterUsedThisTurn = true;
                        LastModuleProc = "矢量回流器";
                    }
                    Log = $"中继变轨完成，当前航道：{PlayerLane + 1}。";
                    break;
                case CardId.ReserveRouting:
                    GainArmor(upgraded && UpgradeBranchFor(id) == UpgradeBranch.Alpha ? 7 : 4);
                    if (Energy == 1)
                    {
                        DrawCards(upgraded && UpgradeBranchFor(id) == UpgradeBranch.Beta ? 3 : 2);
                        LastStatusTrigger = "余量调度：指令已补充";
                    }
                    Log = "保留回路重新分配了本回合余量。";
                    break;
            }

            if (executionBoost)
            {
                executionChipUsedThisTurn = true;
                LastModuleProc = "处决芯片";
            }

            Heat += card.Heat;
            Hand.RemoveAt(handIndex);
            if (ExpandedCardCatalog.ExhaustsOnPlay(id))
                exhaustPile.Add(id);
            else
                discardPile.Add(id);
            if (ExpandedCardCatalog.CyclesRemainingHand(id))
                CycleRemainingHand();
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
            LastShieldAbsorbed = 0;
            LastShieldBroken = false;
            ContractPassiveTriggered = false;
            LastStatusTrigger = string.Empty;
            CardsHeldAtEndTurn = Hand.Count;
            armorAtEnemyPhase = Armor;
            heatAtEnemyPhase = Heat;
            energyAtEnemyPhase = Energy;
            lockOnAtEnemyPhase = LockOn;
            momentumAtEnemyPhase = Momentum;
            exposureAtEnemyPhase = EvasionExposure;

            if (!retainHandThisTurn)
            {
                foreach (CardId card in Hand)
                    discardPile.Add(card);
                Hand.Clear();
            }
            else
            {
                LastStatusTrigger = $"保持编队：保留{Hand.Count}张手牌";
            }
            retainHandThisTurn = false;

            EvasionExposure = changedLaneThisTurn
                ? Math.Min(3, EvasionExposure + 1)
                : Math.Max(0, EvasionExposure - 1);
            trackingShotResolvedThisTurn = false;

            ResolveMinefields();

            foreach (EnemyState enemy in Enemies.Where(enemy => enemy.Alive))
                ResolveEnemy(enemy);

            CompleteExpandedEnemyPhase();

            if (Defeat)
            {
                Log = "邮运机坠毁了，这条航线仍未完成。";
                return;
            }

            ResolveCargoContract();

            Turn++;
            Armor = StartingArmor();
            Energy = TurnEnergy;
            DeliveredEnergyThisTurn = DeferredEnergy;
            Energy += DeferredEnergy;
            DeferredEnergy = 0;
            int naturalCooling = Modification == AirframeModification.RedlineTurbine
                ? 0
                : HasModule(ModuleId.CryoHeart) ? 2 : 1;
            Heat = Math.Max(0, Heat - naturalCooling);
            changedLaneThisTurn = false;
            vectorThrusterUsedThisTurn = false;
            executionChipUsedThisTurn = false;
            aegisCapacitorUsedThisTurn = false;
            zeroPointReactorUsedThisTurn = false;
            redlineReactorUsedThisTurn = false;
            ghostDecoderUsedThisTurn = false;
            contractPassiveUsedThisTurn = false;
            if (!HasModule(ModuleId.MomentumFlywheel))
                Momentum = 0;
            else if (Momentum > 0)
            {
                LastModuleProc = "动量飞轮";
                LastStatusTrigger = $"动量跨回合保留：{Momentum}";
            }
            if (Modification == AirframeModification.OpenAvionics)
                EvasionExposure = Math.Min(3, EvasionExposure + 1);
            ResolveDeferredAttacks();
            ResolveLaneFieldArrival(PlayerLane);
            DrawToTarget();
            Log = $"第{Turn}回合。敌方编队发出了新的行动信号。";
        }

        public string IntentFor(EnemyState enemy)
        {
            if (!enemy.Alive)
                return LocalizationService.Text("intent.destroyed", "已击落");

            if (TryExpandedEnemyIntent(enemy, out string expandedIntent))
                return expandedIntent;

            if (enemy.Kind == EnemyKind.CalamityDrone)
            {
                if (enemy.ChargeTargetLane < 0)
                    return LocalizationService.Text("intent.calamity.recalibrate", "重新校准 / 跳过行动");
                if (enemy.ChargeInterrupted)
                    return LocalizationService.Text("intent.calamity.staggered", "系统失衡 / 跳过行动");
                return LocalizationService.Text("intent.calamity.charge",
                    "灾变 {0} / 航道 {1} · 打断 {2}/{3}", CalamityStrikeDamage,
                    enemy.ChargeTargetLane + 1, enemy.ChargeDamageTaken, CalamityBreakDamage);
            }

            if (enemy.Kind == EnemyKind.StormManta)
            {
                if (enemy.PhaseTransitionPending)
                    return LocalizationService.Text("intent.manta.transition", "阶段转换 / 磁暴甲壳重构");
                if (enemy.ChargeTargetLane < 0)
                    return LocalizationService.Text("intent.manta.retarget", "阶段 {0} / 重新锁定航道", enemy.Phase);
                if (enemy.ChargeInterrupted)
                    return LocalizationService.Text("intent.manta.interrupted", "核心过载 / 大招已打断");
                int strike = enemy.Phase == 1 ? BossPhaseOneStrikeDamage : BossPhaseTwoStrikeDamage;
                int threshold = enemy.Phase == 1 ? BossPhaseOneBreakDamage : BossPhaseTwoBreakDamage;
                return enemy.Phase == 1
                    ? LocalizationService.Text("intent.manta.dive",
                        "磁暴俯冲 {0} / 航道 {1} · 打断 {2}/{3}", strike,
                        enemy.ChargeTargetLane + 1, enemy.ChargeDamageTaken, threshold)
                    : LocalizationService.Text("intent.manta.devour",
                        "吞界磁暴 {0}+邻道{1} / 航道 {2} · 打断 {3}/{4}", strike,
                        BossPhaseTwoSplashDamage, enemy.ChargeTargetLane + 1, enemy.ChargeDamageTaken, threshold);
            }

            if (enemy.Kind == EnemyKind.CloudWyrm)
            {
                if (enemy.PhaseTransitionPending)
                    return LocalizationService.Text("intent.wyrm.transition", "阶段转换 / 雷幕天穹展开");
                if (enemy.ChargeTargetLane < 0)
                    return LocalizationService.Text("intent.wyrm.retarget", "阶段 {0} / 重绘安全航道", enemy.Phase);
                if (enemy.ChargeInterrupted)
                    return LocalizationService.Text("intent.wyrm.interrupted", "雷幕短路 / 大招已打断");
                int strike = enemy.Phase == 1 ? CloudWyrmPhaseOneStrikeDamage : CloudWyrmPhaseTwoStrikeDamage;
                int threshold = enemy.Phase == 1 ? CloudWyrmPhaseOneBreakDamage : CloudWyrmPhaseTwoBreakDamage;
                return enemy.Phase == 1
                    ? LocalizationService.Text("intent.wyrm.curtain",
                        "双翼雷幕 {0} / 仅航道 {1} 安全 · 打断 {2}/{3}", strike,
                        enemy.ChargeTargetLane + 1, enemy.ChargeDamageTaken, threshold)
                    : LocalizationService.Text("intent.wyrm.overwrite",
                        "天穹覆写 {0} / 仅航道 {1} 安全 · 打断 {2}/{3}", strike,
                        enemy.ChargeTargetLane + 1, enemy.ChargeDamageTaken, threshold);
            }

            if (enemy.Kind == EnemyKind.CurtainHerald)
            {
                if (enemy.ChargeTargetLane < 0)
                    return LocalizationService.Text("intent.herald.recalibrate", "重绘雷幕 / 跳过行动");
                if (enemy.ChargeInterrupted)
                    return LocalizationService.Text("intent.herald.interrupted", "雷幕短路 / 跳过行动");
                return LocalizationService.Text("intent.herald.curtain",
                    "先导雷幕 {0} / 仅航道 {1} 安全 · 打断 {2}/{3}", enemy.Damage,
                    enemy.ChargeTargetLane + 1, enemy.ChargeDamageTaken, PreludeBreakDamage);
            }

            if (enemy.Kind == EnemyKind.FluxSkimmer)
            {
                if (enemy.ChargeTargetLane < 0)
                    return LocalizationService.Text("intent.skimmer.recalibrate", "磁针校准 / 跳过行动");
                if (enemy.ChargeInterrupted)
                    return LocalizationService.Text("intent.skimmer.interrupted", "磁针失衡 / 跳过行动");
                return LocalizationService.Text("intent.skimmer.sweep",
                    "磁针扫掠 {0} / 航道 {1}+邻道危险 · 打断 {2}/{3}", enemy.Damage,
                    enemy.ChargeTargetLane + 1, enemy.ChargeDamageTaken, PreludeBreakDamage);
            }

            if (enemy.Kind == EnemyKind.StormBalloon)
                return LocalizationService.Text("intent.storm", "风暴 {0} / 全航道", enemy.Damage);

            if (enemy.Kind == EnemyKind.ShieldLeech && Armor >= 5)
                return LocalizationService.Text("intent.shield_leech", "盾蚀 / 清空当前{0}点护盾", Armor);

            if (enemy.Kind == EnemyKind.HandJammer)
                return Hand.Count >= 5
                    ? LocalizationService.Text("intent.hand_jam", "手牌干扰 {0} / 保留5+张触发", enemy.Damage)
                    : LocalizationService.Text("intent.hand_safe", "监听手牌 / 少于5张安全");

            if (enemy.Kind == EnemyKind.HeatSeeker && Heat >= 4)
                return LocalizationService.Text("intent.heat_seek", "热寻 {0} / 当前热量4+", enemy.Damage);

            if (enemy.Kind == EnemyKind.SignalHijacker)
            {
                if (LockOn > 0)
                    return LocalizationService.Text("intent.hijack.lock", "劫持锁定 / 锁定-1，敌装甲+3");
                if (Momentum > 0)
                    return LocalizationService.Text("intent.hijack.momentum", "劫持动量 / 动量-1，敌装甲+3");
                if (EvasionExposure > 0)
                    return LocalizationService.Text("intent.hijack.trace", "污染航迹 / 暴露+1，敌装甲+3");
                return LocalizationService.Text("intent.hijack.scan", "协议扫描 / 无资源可劫持");
            }

            if (enemy.Kind == EnemyKind.RustKite &&
                changedLaneThisTurn && EvasionExposure >= 1)
                return LocalizationService.Text("intent.tracking", "追踪 {0} / 继续换道将暴露航迹", TrackingShotDamage);

            if (enemy.Kind == EnemyKind.MailEater)
                return enemy.Lane == PlayerLane
                    ? LocalizationService.Text("intent.block.hit", "封锁 {0} / 换道可规避", enemy.Damage + 2)
                    : LocalizationService.Text("intent.block.lane", "封锁航道 {0} / 进入将受{1}伤害",
                        enemy.Lane + 1, enemy.Damage + 2);

            if (enemy.Lane == PlayerLane)
                return LocalizationService.Text("intent.attack", "攻击 {0}", enemy.Damage);

            return enemy.Lane < PlayerLane
                ? LocalizationService.Text("intent.move.down", "下降1条航道")
                : LocalizationService.Text("intent.move.up", "上升1条航道");
        }

        private void ResolveEnemy(EnemyState enemy)
        {
            if (TryResolveExpandedEnemy(enemy))
                return;

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
                    TakeDamage(CalamityStrikeDamage, true, PlayerDamageSource.CalamityStrike, enemy.Name);
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
                if (enemy.Phase == 2)
                {
                    ResolveBossContractProtocol(enemy);
                    ResolveBossAirframeProtocol(enemy);
                    if (Defeat)
                        return;
                }
                if (enemy.ChargeInterrupted)
                {
                    CalamityInterrupts++;
                    AppendStatusTrigger("BOSS大招已打断");
                }
                else
                {
                    int laneDistance = Math.Abs(PlayerLane - enemy.ChargeTargetLane);
                    if (laneDistance == 0)
                    {
                        TakeDamage(enemy.Phase == 1 ? BossPhaseOneStrikeDamage : BossPhaseTwoStrikeDamage, true,
                            PlayerDamageSource.BossStrike, enemy.Name);
                        CalamityHits++;
                    }
                    else if (enemy.Phase == 2 && laneDistance == 1)
                    {
                        TakeDamage(BossPhaseTwoSplashDamage, false, PlayerDamageSource.BossSplash, enemy.Name);
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

            if (enemy.Kind == EnemyKind.CloudWyrm)
            {
                if (enemy.PhaseTransitionPending)
                {
                    enemy.PhaseTransitionPending = false;
                    BeginCloudWyrmCharge(enemy);
                    LastStatusTrigger = "BOSS PHASE 2：雷幕天穹展开";
                    return;
                }
                if (enemy.ChargeTargetLane < 0)
                {
                    BeginCloudWyrmCharge(enemy);
                    return;
                }
                if (enemy.Phase == 2)
                {
                    ResolveBossContractProtocol(enemy);
                    ResolveBossAirframeProtocol(enemy);
                    if (Defeat)
                        return;
                }
                if (enemy.ChargeInterrupted)
                {
                    CalamityInterrupts++;
                    AppendStatusTrigger("BOSS雷幕已打断");
                }
                else if (PlayerLane != enemy.ChargeTargetLane)
                {
                    TakeDamage(enemy.Phase == 1 ? CloudWyrmPhaseOneStrikeDamage : CloudWyrmPhaseTwoStrikeDamage,
                        true, PlayerDamageSource.BossCurtain, enemy.Name);
                    CalamityHits++;
                }
                else
                {
                    CalamityEvades++;
                }
                EnterCalamityCooldown(enemy);
                return;
            }

            if (enemy.Kind == EnemyKind.CurtainHerald)
            {
                if (enemy.ChargeTargetLane < 0)
                {
                    BeginCurtainHeraldCharge(enemy);
                    return;
                }
                if (enemy.ChargeInterrupted)
                    CalamityInterrupts++;
                else if (PlayerLane != enemy.ChargeTargetLane)
                {
                    TakeDamage(enemy.Damage, true, PlayerDamageSource.PreludeCurtain, enemy.Name);
                    CalamityHits++;
                }
                else
                    CalamityEvades++;
                EnterCalamityCooldown(enemy);
                return;
            }

            if (enemy.Kind == EnemyKind.FluxSkimmer)
            {
                if (enemy.ChargeTargetLane < 0)
                {
                    BeginFluxSkimmerCharge(enemy);
                    return;
                }
                if (enemy.ChargeInterrupted)
                    CalamityInterrupts++;
                else if (Math.Abs(PlayerLane - enemy.ChargeTargetLane) <= 1)
                {
                    TakeDamage(enemy.Damage, true, PlayerDamageSource.PreludeMagnet, enemy.Name);
                    CalamityHits++;
                }
                else
                    CalamityEvades++;
                EnterCalamityCooldown(enemy);
                return;
            }

            if (enemy.Kind == EnemyKind.StormBalloon)
            {
                TakeDamage(enemy.Damage, false, PlayerDamageSource.StormField, enemy.Name);
                return;
            }

            if (enemy.Kind == EnemyKind.ShieldLeech && armorAtEnemyPhase >= 5)
            {
                int eroded = Armor;
                Armor = 0;
                LastShieldBroken = eroded > 0;
                LastStatusTrigger = $"盾蚀脉冲：{eroded}点护盾被清除";
                return;
            }

            if (enemy.Kind == EnemyKind.HandJammer)
            {
                if (CardsHeldAtEndTurn >= 5)
                    TakeDamage(enemy.Damage, false, PlayerDamageSource.HandJam, enemy.Name);
                return;
            }

            if (enemy.Kind == EnemyKind.HeatSeeker && heatAtEnemyPhase >= 4)
            {
                TakeDamage(enemy.Damage, false, PlayerDamageSource.HeatSeek, enemy.Name);
                return;
            }

            if (enemy.Kind == EnemyKind.SignalHijacker)
            {
                string stolen = string.Empty;
                if (lockOnAtEnemyPhase > 0 && LockOn > 0)
                {
                    LockOn--;
                    stolen = "锁定";
                }
                else if (momentumAtEnemyPhase > 0 && Momentum > 0)
                {
                    Momentum--;
                    stolen = "动量";
                }
                else if (exposureAtEnemyPhase > 0)
                {
                    EvasionExposure = Math.Min(3, EvasionExposure + 1);
                    stolen = "航迹";
                }
                if (!string.IsNullOrEmpty(stolen))
                {
                    enemy.Armor += 3;
                    enemy.MaxArmor = Math.Max(enemy.MaxArmor, enemy.Armor);
                    LastStatusTrigger = $"协议劫持：{stolen}被篡改，敌装甲 +3";
                }
                return;
            }

            if (!trackingShotResolvedThisTurn && changedLaneThisTurn && EvasionExposure >= 2 &&
                enemy.Kind == EnemyKind.RustKite)
            {
                trackingShotResolvedThisTurn = true;
                TrackingHits++;
                TakeDamage(TrackingShotDamage, false, PlayerDamageSource.TrackingShot, enemy.Name);
                return;
            }

            if (enemy.Lane == PlayerLane)
            {
                TakeDamage(enemy.Kind == EnemyKind.MailEater ? enemy.Damage + 2 : enemy.Damage, true,
                    enemy.Kind == EnemyKind.MailEater ? PlayerDamageSource.LaneBlock : PlayerDamageSource.DirectAttack,
                    enemy.Name);
                return;
            }

            if (enemy.Kind == EnemyKind.MailEater)
                return;

            enemy.Lane += enemy.Lane < PlayerLane ? 1 : -1;
        }

        private void ResolveBossContractProtocol(EnemyState enemy)
        {
            if (Modification == AirframeModification.None)
                return;
            switch (ActiveBossContractProtocol)
            {
                case BossContractProtocol.SealMirror:
                    if (lockOnAtEnemyPhase <= 0 || LockOn <= 0)
                        return;
                    LockOn--;
                    AddBossAdaptiveArmor(enemy);
                    AppendStatusTrigger("密封镜像：锁定-1，首领装甲+3");
                    break;
                case BossContractProtocol.CryoInversion:
                    if (heatAtEnemyPhase > 1)
                        return;
                    AddBossAdaptiveArmor(enemy);
                    AppendStatusTrigger("低温逆转：首领装甲+3");
                    break;
                case BossContractProtocol.VectorIntercept:
                    if (momentumAtEnemyPhase <= 0 || Momentum <= 0)
                        return;
                    Momentum--;
                    AddBossAdaptiveArmor(enemy);
                    AppendStatusTrigger("矢量截获：动量-1，首领装甲+3");
                    break;
                case BossContractProtocol.GhostTrace:
                    if (exposureAtEnemyPhase <= 0)
                        return;
                    EvasionExposure = Math.Min(3, EvasionExposure + 1);
                    AddBossAdaptiveArmor(enemy);
                    AppendStatusTrigger("幽灵追迹：航迹暴露+1，首领装甲+3");
                    break;
                case BossContractProtocol.ReserveSiphon:
                    if (energyAtEnemyPhase != 1)
                        return;
                    AddBossAdaptiveArmor(enemy);
                    AppendStatusTrigger("余量虹吸：保留1点能量，首领装甲+3");
                    break;
            }
        }

        private void ResolveBossAirframeProtocol(EnemyState enemy)
        {
            switch (ActiveBossAirframeProtocol)
            {
                case BossAirframeProtocol.ShieldCrack:
                    if (armorAtEnemyPhase < 5)
                        return;
                    int eroded = Armor;
                    Armor = 0;
                    LastShieldBroken = eroded > 0;
                    AppendStatusTrigger($"裂盾回波：清除{eroded}点护盾");
                    break;
                case BossAirframeProtocol.WidebandJam:
                    if (CardsHeldAtEndTurn < 5)
                        return;
                    AppendStatusTrigger("宽频干扰：5+张手牌触发");
                    TakeDamage(BossAdaptationDamage, false, PlayerDamageSource.BossWidebandJam, enemy.Name);
                    break;
                case BossAirframeProtocol.ThermalLock:
                    if (heatAtEnemyPhase < 4)
                        return;
                    AppendStatusTrigger("热源锁定：4+热量触发");
                    TakeDamage(BossAdaptationDamage, false, PlayerDamageSource.BossThermalLock, enemy.Name);
                    break;
            }
        }

        private static void AddBossAdaptiveArmor(EnemyState enemy)
        {
            enemy.Armor += BossAdaptiveArmor;
            enemy.MaxArmor = Math.Max(enemy.MaxArmor, enemy.Armor);
        }

        private void AppendStatusTrigger(string status)
        {
            LastStatusTrigger = string.IsNullOrEmpty(LastStatusTrigger)
                ? status
                : $"{LastStatusTrigger}　//　{status}";
        }

        private void TakeDamage(int amount, bool threatensCargo, PlayerDamageSource source, string dealer)
        {
            int armorBefore = Armor;
            int absorbed = Math.Min(Armor, amount);
            Armor -= absorbed;
            int hullDamage = amount - absorbed;
            int healthBefore = PlayerHealth;
            PlayerHealth = Math.Max(0, PlayerHealth - hullDamage);
            DamageTaken += healthBefore - PlayerHealth;
            if (hullDamage > 0)
            {
                LastDamageSource = source;
                LastDamageDealer = dealer ?? string.Empty;
                LastHullDamage = healthBefore - PlayerHealth;
            }
            if (healthBefore > 0 && PlayerHealth <= 0)
            {
                DefeatSource = source;
                DefeatDealer = dealer ?? string.Empty;
                DefeatDamage = healthBefore - PlayerHealth;
                DefeatRawDamage = Math.Max(0, amount);
                DefeatShieldAbsorbed = absorbed;
                DefeatHullBefore = healthBefore;
                DefeatTurn = Turn;
                HasDefeatCause = true;
            }
            LastShieldAbsorbed += absorbed;
            if (armorBefore > 0 && Armor == 0 && absorbed > 0)
                LastShieldBroken = true;
            if (Cargo == CargoContract.FragileMedicine && source != PlayerDamageSource.Overheat &&
                absorbed > 0 && hullDamage == 0)
            {
                TriggerContractPassiveLock("密封缓冲：完全格挡，锁定 +1");
            }

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
                case CargoContract.SignalSeed:
                    if (Energy <= 0)
                        DamageCargo("回合结束时没有保留能量");
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

            int upgradedDamage = Math.Max(0, damage + currentExpandedDamageBonus);
            int resolvedDamage = currentCardCritical ? (int)Math.Ceiling(upgradedDamage * 1.5f) : upgradedDamage;
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
            RecordExpandedEnemyDamage(enemy, armorBefore - enemy.Armor + healthBefore - enemy.Health);

            if (IsBossKind(enemy.Kind) && enemy.Alive && enemy.Phase == 1 &&
                enemy.Health <= enemy.MaxHealth / 2)
            {
                enemy.Phase = 2;
                enemy.PhaseTransitionPending = true;
                enemy.Armor = Math.Max(enemy.Armor, 8);
                BossStoryAlignment alignment = ActiveBossStoryAlignment;
                if (alignment == BossStoryAlignment.Allied)
                    enemy.Armor = Math.Max(0, enemy.Armor - BossStoryArmorShift);
                else if (alignment == BossStoryAlignment.Hostile)
                    enemy.Armor += BossStoryArmorShift;
                enemy.MaxArmor = Math.Max(enemy.MaxArmor, enemy.Armor);
                EnterCalamityCooldown(enemy);
                LastStatusTrigger = alignment switch
                {
                    BossStoryAlignment.Allied => "盟友反向脉冲：二阶段首领装甲-4",
                    BossStoryAlignment.Hostile => "敌对信标上传航路数据：二阶段首领装甲+4",
                    _ => enemy.Kind switch
                    {
                        EnemyKind.CloudWyrm => "BOSS PHASE 2：雷幕天穹上线",
                        EnemyKind.CourierZero => "BOSS PHASE 2：航线复制上线",
                        EnemyKind.InvertedSkyWhale => "BOSS PHASE 2：重力翻转上线",
                        _ => "BOSS PHASE 2：吞界磁暴上线"
                    }
                };
            }

            if (enemy.PhaseTransitionPending)
                return;

            if ((!IsChargedKind(enemy.Kind)) ||
                enemy.ChargeInterrupted || !enemy.Alive)
                return;

            enemy.ChargeDamageTaken += (armorBefore - enemy.Armor) + (healthBefore - enemy.Health);
            int breakDamage = enemy.Kind switch
            {
                EnemyKind.StormManta => enemy.Phase == 1 ? BossPhaseOneBreakDamage : BossPhaseTwoBreakDamage,
                EnemyKind.CloudWyrm => enemy.Phase == 1 ? CloudWyrmPhaseOneBreakDamage : CloudWyrmPhaseTwoBreakDamage,
                EnemyKind.CourierZero => enemy.Phase == 1
                    ? CourierZeroPhaseOneBreakDamage : CourierZeroPhaseTwoBreakDamage,
                EnemyKind.InvertedSkyWhale => enemy.Phase == 1
                    ? SkyWhalePhaseOneBreakDamage : SkyWhalePhaseTwoBreakDamage,
                EnemyKind.CurtainHerald => PreludeBreakDamage,
                EnemyKind.FluxSkimmer => PreludeBreakDamage,
                _ => CalamityBreakDamage
            };
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

        private int ApplyCooling(int amount)
        {
            int cooled = Math.Min(Heat, Math.Max(0, amount));
            Heat -= cooled;
            if (Cargo == CargoContract.CryoSerum && cooled >= 3 && !contractPassiveUsedThisTurn)
            {
                Energy++;
                TriggerContractPassive("低温回收：能量 +1");
            }
            return cooled;
        }

        private int ReduceExposure(int amount)
        {
            int before = EvasionExposure;
            EvasionExposure = Math.Max(0, EvasionExposure - Math.Max(0, amount));
            int cleared = before - EvasionExposure;
            if (Cargo == CargoContract.BlackBoxRelay && cleared > 0)
                TriggerContractPassiveLock("幽灵译码：清除航迹，锁定 +1");
            return cleared;
        }

        private void TriggerStormCorePassive()
        {
            if (Cargo != CargoContract.StormCore || Momentum >= 3 || contractPassiveUsedThisTurn)
                return;
            Momentum++;
            TriggerContractPassive("矢量电荷：动量 +1");
        }

        private void TriggerSignalSeedPassive()
        {
            if (Cargo != CargoContract.SignalSeed || Energy != 1 || contractPassiveUsedThisTurn)
                return;
            DrawCards(1);
            TriggerContractPassive("余量回授：保留1点能量，抽1张牌");
        }

        private void TriggerContractPassiveLock(string status)
        {
            if (LockOn >= 3 || contractPassiveUsedThisTurn)
                return;
            LockOn++;
            TriggerContractPassive(status);
        }

        private void TriggerContractPassive(string status)
        {
            contractPassiveUsedThisTurn = true;
            ContractPassiveTriggered = true;
            ContractPassiveProcs++;
            LastStatusTrigger = status;
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

        private void BeginCloudWyrmCharge(EnemyState enemy)
        {
            int offset = enemy.ChargeCycle % 2 == 0 ? 1 : 2;
            enemy.ChargeTargetLane = (PlayerLane + offset) % 3;
            enemy.ChargeCycle++;
            enemy.ChargeDamageTaken = 0;
            enemy.ChargeInterrupted = false;
        }

        private void BeginCurtainHeraldCharge(EnemyState enemy)
        {
            int offset = enemy.ChargeCycle % 2 == 0 ? 1 : 2;
            enemy.ChargeTargetLane = (PlayerLane + offset) % 3;
            enemy.ChargeCycle++;
            enemy.ChargeDamageTaken = 0;
            enemy.ChargeInterrupted = false;
        }

        private void BeginFluxSkimmerCharge(EnemyState enemy)
        {
            int offset = enemy.ChargeCycle % 2 == 0 ? 1 : 2;
            enemy.ChargeTargetLane = (PlayerLane + offset) % 3;
            enemy.ChargeCycle++;
            enemy.ChargeDamageTaken = 0;
            enemy.ChargeInterrupted = false;
        }

        public static bool IsBossKind(EnemyKind kind)
        {
            return kind == EnemyKind.StormManta || kind == EnemyKind.CloudWyrm ||
                kind == EnemyKind.CourierZero || kind == EnemyKind.InvertedSkyWhale;
        }

        private static bool IsChargedKind(EnemyKind kind)
        {
            return kind == EnemyKind.CalamityDrone || IsBossKind(kind) ||
                kind == EnemyKind.CurtainHerald || kind == EnemyKind.FluxSkimmer;
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
            TakeDamage(5, false, PlayerDamageSource.Overheat, "引擎过热");
            OverheatCount++;
            Log = "引擎过热！机体受到5点伤害。";
        }

        private int StartingArmor()
        {
            int armor = HasModule(ModuleId.PrismBulkhead) ? 3 : 0;
            if (Modification == AirframeModification.SealedBulkhead)
                armor = Math.Max(armor, 5);
            return armor;
        }

        private void DrawToTarget()
        {
            while (Hand.Count < HandTarget)
            {
                if (!DrawOne())
                    return;
            }
        }

        private void DrawCards(int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                if (!DrawOne())
                    return;
            }
        }

        private bool DrawOne()
        {
            if (drawPile.Count == 0)
            {
                if (discardPile.Count == 0)
                    return false;
                drawPile.AddRange(discardPile);
                discardPile.Clear();
                Shuffle(drawPile);
            }

            int last = drawPile.Count - 1;
            Hand.Add(drawPile[last]);
            drawPile.RemoveAt(last);
            return true;
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
            foreach (EnemyState enemy in Enemies.Where(enemy => enemy.Kind == EnemyKind.CloudWyrm))
                BeginCloudWyrmCharge(enemy);
            foreach (EnemyState enemy in Enemies.Where(enemy => enemy.Kind == EnemyKind.CurtainHerald))
                BeginCurtainHeraldCharge(enemy);
            foreach (EnemyState enemy in Enemies.Where(enemy => enemy.Kind == EnemyKind.FluxSkimmer))
                BeginFluxSkimmerCharge(enemy);
            InitializeExpandedEnemies();

            EnemyState boss = Enemies.FirstOrDefault(enemy => IsBossKind(enemy.Kind));
            if (boss != null && FinaleProgressionRules.IntelApplies(Intel, boss.Kind))
            {
                boss.ChargeTargetLane = boss.Kind == EnemyKind.CloudWyrm ||
                    boss.Kind == EnemyKind.InvertedSkyWhale
                    ? PlayerLane
                    : (PlayerLane + 2) % 3;
                boss.ChargeDamageTaken = 0;
                boss.ChargeInterrupted = false;
            }
        }
    }
}
