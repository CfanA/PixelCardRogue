using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyCourier
{
    public enum ChallengeId
    {
        Standard,
        RedlineRelay,
        NoSafeHarbor,
        LeanManifest
    }

    public enum AchievementId
    {
        FirstChallenge,
        FiveContracts,
        TwinBossArchive,
        SixEndings,
        ContractMaster
    }

    public sealed class ChallengeDefinition
    {
        public ChallengeId Id { get; }
        public int FixedSeed { get; }
        public int StartingHull { get; }
        public int StartingHeat { get; }
        public bool FieldRepairsEnabled { get; }

        public ChallengeDefinition(ChallengeId id, int fixedSeed, int startingHull, int startingHeat,
            bool fieldRepairsEnabled)
        {
            Id = id;
            FixedSeed = fixedSeed;
            StartingHull = startingHull;
            StartingHeat = startingHeat;
            FieldRepairsEnabled = fieldRepairsEnabled;
        }
    }

    public static class ChallengeCatalog
    {
        private static readonly ChallengeDefinition[] Definitions =
        {
            new ChallengeDefinition(ChallengeId.Standard, 0, BattleState.MaxPlayerHealth, 0, true),
            new ChallengeDefinition(ChallengeId.RedlineRelay, 0x045001, BattleState.MaxPlayerHealth, 3, true),
            new ChallengeDefinition(ChallengeId.NoSafeHarbor, 0x045002, BattleState.MaxPlayerHealth, 0, false),
            new ChallengeDefinition(ChallengeId.LeanManifest, 0x045003, 28, 0, true)
        };

        public static IReadOnlyList<ChallengeDefinition> All => Definitions;

        public static ChallengeDefinition Get(ChallengeId id)
        {
            return Definitions.FirstOrDefault(definition => definition.Id == id) ?? Definitions[0];
        }
    }

    public static class ContractCatalog
    {
        private static readonly CargoContract[] Contracts =
            Enum.GetValues(typeof(CargoContract)).Cast<CargoContract>().ToArray();

        public static IReadOnlyList<CargoContract> All => Contracts;

        public static BossContractProtocol BossProtocol(CargoContract contract)
        {
            return contract switch
            {
                CargoContract.FragileMedicine => BossContractProtocol.SealMirror,
                CargoContract.CryoSerum => BossContractProtocol.CryoInversion,
                CargoContract.StormCore => BossContractProtocol.VectorIntercept,
                CargoContract.BlackBoxRelay => BossContractProtocol.GhostTrace,
                CargoContract.SignalSeed => BossContractProtocol.ReserveSiphon,
                _ => throw new ArgumentOutOfRangeException(nameof(contract), contract, null)
            };
        }

        public static CardId StarterCard(CargoContract contract)
        {
            return contract switch
            {
                CargoContract.FragileMedicine => CardId.ReactivePlating,
                CargoContract.CryoSerum => CardId.CryoPump,
                CargoContract.StormCore => CardId.VectorDash,
                CargoContract.BlackBoxRelay => CardId.SignalScrambler,
                CargoContract.SignalSeed => CardId.ReserveShot,
                _ => throw new ArgumentOutOfRangeException(nameof(contract), contract, null)
            };
        }
    }

    [Serializable]
    public sealed class ChallengeProgressRecord
    {
        public int Challenge;
        public int Attempts;
        public int Completions;
        public int BestHull;
        public int BestCargo = -1;
        public int BestTurns;
    }

    [Serializable]
    public sealed class ContractMasteryRecord
    {
        public int Contract;
        public int Runs;
        public int Deliveries;
        public int PristineDeliveries;
        public int ChallengeDeliveries;
        public int BossVictories;
    }

    [Serializable]
    public sealed class BossDossierRecord
    {
        public int Boss;
        public int Encounters;
        public int Victories;
        public List<int> Endings = new List<int>();
    }

    public sealed class ProgressGoal
    {
        public string Id;
        public int Current;
        public int Target;
    }

    public static class LongTermProgressionRules
    {
        public static int MasteryPoints(ContractMasteryRecord record)
        {
            if (record == null)
                return 0;
            return Math.Max(0, record.Runs) +
                Math.Max(0, record.Deliveries) * 3 +
                Math.Max(0, record.PristineDeliveries) * 2 +
                Math.Max(0, record.ChallengeDeliveries) * 2 +
                Math.Max(0, record.BossVictories);
        }

        public static int MasteryLevel(ContractMasteryRecord record)
        {
            int points = MasteryPoints(record);
            if (points >= 24) return 4;
            if (points >= 14) return 3;
            if (points >= 7) return 2;
            if (points >= 2) return 1;
            return 0;
        }

        public static int MasteryLevelThreshold(int level)
        {
            return level switch
            {
                <= 0 => 0,
                1 => 2,
                2 => 7,
                3 => 14,
                _ => 24
            };
        }

        public static bool AchievementUnlocked(DeliveryArchiveData data, AchievementId achievement)
        {
            if (data == null)
                return false;
            return achievement switch
            {
                AchievementId.FirstChallenge => data.ChallengeProgress.Any(record => record.Completions > 0),
                AchievementId.FiveContracts => ContractCatalog.All.All(contract =>
                    data.ContractMastery.Any(record => record.Contract == (int)contract && record.Deliveries > 0)),
                AchievementId.TwinBossArchive => new[] { EnemyKind.StormManta, EnemyKind.CloudWyrm,
                    EnemyKind.CourierZero, EnemyKind.InvertedSkyWhale }.All(boss =>
                    data.BossDossiers.Any(record => record.Boss == (int)boss && record.Victories > 0)),
                AchievementId.SixEndings => data.DiscoveredEndings.Count >=
                    Enum.GetValues(typeof(FinaleEnding)).Length - 1,
                AchievementId.ContractMaster => ContractCatalog.All.All(contract =>
                    MasteryLevel(data.ContractMastery.FirstOrDefault(record =>
                        record.Contract == (int)contract)) >= 2),
                _ => false
            };
        }

        public static List<ProgressGoal> NextGoals(DeliveryArchiveData data, int maximum = 3)
        {
            data ??= new DeliveryArchiveData();
            var goals = new List<ProgressGoal>();
            int challengeTotal = ChallengeCatalog.All.Count - 1;
            int challengeComplete = data.ChallengeProgress.Count(record =>
                record.Challenge != (int)ChallengeId.Standard && record.Completions > 0);
            if (challengeComplete < challengeTotal)
                goals.Add(new ProgressGoal { Id = "challenges", Current = challengeComplete, Target = challengeTotal });

            int deliveredContracts = ContractCatalog.All.Count(contract => data.ContractMastery.Any(record =>
                record.Contract == (int)contract && record.Deliveries > 0));
            if (deliveredContracts < ContractCatalog.All.Count)
                goals.Add(new ProgressGoal
                {
                    Id = "contracts",
                    Current = deliveredContracts,
                    Target = ContractCatalog.All.Count
                });

            int bossVictories = new[] { EnemyKind.StormManta, EnemyKind.CloudWyrm,
                EnemyKind.CourierZero, EnemyKind.InvertedSkyWhale }.Count(boss =>
                data.BossDossiers.Any(record => record.Boss == (int)boss && record.Victories > 0));
            if (bossVictories < 4)
                goals.Add(new ProgressGoal { Id = "bosses", Current = bossVictories, Target = 4 });

            int endingTotal = Enum.GetValues(typeof(FinaleEnding)).Length - 1;
            if (data.DiscoveredEndings.Count < endingTotal)
                goals.Add(new ProgressGoal
                {
                    Id = "endings",
                    Current = data.DiscoveredEndings.Count,
                    Target = endingTotal
                });

            int masteredContracts = ContractCatalog.All.Count(contract =>
                MasteryLevel(data.ContractMastery.FirstOrDefault(record =>
                    record.Contract == (int)contract)) >= 2);
            if (masteredContracts < ContractCatalog.All.Count)
                goals.Add(new ProgressGoal
                {
                    Id = "mastery",
                    Current = masteredContracts,
                    Target = ContractCatalog.All.Count
                });

            return goals.Take(Math.Max(0, maximum)).ToList();
        }
    }
}
