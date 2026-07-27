using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SkyCourier
{
    [Serializable]
    public sealed class ArchivedRunRecord
    {
        public string RecordedAtUtc;
        public string AttemptId;
        public int RunSeed;
        public int Contract;
        public string Outcome;
        public int RouteNodeId;
        public int Encounter;
        public int CargoIntegrity;
        public int Hull;
        public int Credits;
        public int Turns;
        public int CardsPlayed;
        public int DeckCount;
        public int ModuleCount;
        public int RouteIntel;
        public int FinaleEnding;
        public int Challenge;
        public int BossKind = -1;
        public int DefeatSource = -1;
        public string DefeatDealer;
        public int DefeatDamage;
        public int DefeatRawDamage;
        public int DefeatShieldAbsorbed;
        public int DefeatHullBefore;
        public int DefeatTurn;
        public int DamageTaken;
        public int Overheats;
        public int CalamityInterrupts;
        public int CalamityEvades;
        public int CalamityHits;
        public int TrackingHits;
        public int ContractProcs;
        public int ContractBonusCredits;
        public int AirframeModification;
        public int RouteStoryState;
        public int DepartureDirective;
        public int FinalApproachPlan;
        public string BuildProfile;
        public string RouteProfile;
        public List<RunBuildSnapshot> BuildSnapshots = new List<RunBuildSnapshot>();
    }

    [Serializable]
    public sealed class RunWinRateRecord
    {
        public string Dimension;
        public string Key;
        public int Attempts;
        public int Wins;
    }

    [Serializable]
    public sealed class DeliveryArchiveData
    {
        public int Version = DeliveryArchiveService.CurrentVersion;
        public string UpdatedAtUtc;
        public int RunsStarted;
        public int DeliveriesCompleted;
        public int EncountersLost;
        public int BattlesWon;
        public int BestCargoIntegrity = -1;
        public int BestCredits;
        public int TotalTurns;
        public int TotalCardsPlayed;
        public List<int> DiscoveredContracts = new List<int>();
        public List<int> DiscoveredCards = new List<int>();
        public List<int> DiscoveredModules = new List<int>();
        public List<int> DiscoveredEnemies = new List<int>();
        public List<int> DiscoveredEndings = new List<int>();
        public List<ChallengeProgressRecord> ChallengeProgress = new List<ChallengeProgressRecord>();
        public List<ContractMasteryRecord> ContractMastery = new List<ContractMasteryRecord>();
        public List<BossDossierRecord> BossDossiers = new List<BossDossierRecord>();
        public List<ArchivedRunRecord> RecentRuns = new List<ArchivedRunRecord>();
        public List<RunWinRateRecord> PerformanceStats = new List<RunWinRateRecord>();
        public List<string> ResolvedAttemptIds = new List<string>();
    }

    public static class DeliveryArchiveService
    {
        public const int CurrentVersion = 6;
        public const int MaximumRecentRuns = 24;
        public const string ContractDimension = "contract";
        public const string BuildDimension = "build";
        public const string RouteDimension = "route";
        public const string BossDimension = "boss";
        private const string ArchiveFileName = "archive.json";
        private const string BackupFileName = "archive_backup.json";
        private const string TempFileName = "archive.tmp";

        public static string ArchivePath => Path.Combine(Application.persistentDataPath, ArchiveFileName);

        public static DeliveryArchiveData Load(out bool restoredBackup, out string error)
        {
            return LoadFromDirectory(Application.persistentDataPath, out restoredBackup, out error);
        }

        public static DeliveryArchiveData LoadFromDirectory(string directory, out bool restoredBackup, out string error)
        {
            restoredBackup = false;
            string archivePath = Path.Combine(directory, ArchiveFileName);
            string backupPath = Path.Combine(directory, BackupFileName);
            if (!File.Exists(archivePath) && !File.Exists(backupPath))
            {
                error = null;
                return new DeliveryArchiveData();
            }

            if (TryRead(archivePath, out DeliveryArchiveData data, out error))
                return data;
            string primaryError = error;
            if (TryRead(backupPath, out data, out error))
            {
                restoredBackup = true;
                return data;
            }

            error = string.IsNullOrEmpty(primaryError) ? error : $"{primaryError} / {error}";
            return new DeliveryArchiveData();
        }

        public static void Save(DeliveryArchiveData data)
        {
            SaveToDirectory(data, Application.persistentDataPath);
        }

        public static void SaveToDirectory(DeliveryArchiveData data, string directory)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            Normalize(data);
            data.Version = CurrentVersion;
            data.UpdatedAtUtc = DateTime.UtcNow.ToString("O");
            Directory.CreateDirectory(directory);

            string archivePath = Path.Combine(directory, ArchiveFileName);
            string backupPath = Path.Combine(directory, BackupFileName);
            string tempPath = Path.Combine(directory, TempFileName);
            File.WriteAllText(tempPath, JsonUtility.ToJson(data, true));
            if (File.Exists(archivePath))
            {
                try
                {
                    File.Replace(tempPath, archivePath, backupPath);
                    return;
                }
                catch (PlatformNotSupportedException)
                {
                }
                catch (IOException)
                {
                }

                File.Copy(archivePath, backupPath, true);
                File.Delete(archivePath);
            }
            File.Move(tempPath, archivePath);
        }

        public static void RegisterRunStarted(DeliveryArchiveData data, int contract, IEnumerable<int> deck,
            int challenge = (int)ChallengeId.Standard)
        {
            data.RunsStarted++;
            AddUnique(data.DiscoveredContracts, contract);
            AddUnique(data.DiscoveredCards, deck);
            ContractMasteryRecord mastery = FindOrCreateContractMastery(data, contract);
            mastery.Runs++;
            if (Enum.IsDefined(typeof(ChallengeId), challenge) && challenge != (int)ChallengeId.Standard)
                FindOrCreateChallengeProgress(data, challenge).Attempts++;
            Normalize(data);
        }

        public static void RegisterBattleStarted(DeliveryArchiveData data, IEnumerable<int> enemies,
            IEnumerable<int> deck, IEnumerable<int> modules)
        {
            AddUnique(data.DiscoveredEnemies, enemies);
            AddUnique(data.DiscoveredCards, deck);
            AddUnique(data.DiscoveredModules, modules);
            Normalize(data);
        }

        public static void RegisterRewardDiscoveries(DeliveryArchiveData data, IEnumerable<int> deck,
            IEnumerable<int> modules)
        {
            AddUnique(data.DiscoveredCards, deck);
            AddUnique(data.DiscoveredModules, modules);
            Normalize(data);
        }

        public static void RegisterBattleWon(DeliveryArchiveData data)
        {
            data.BattlesWon++;
            Normalize(data);
        }

        public static void RegisterRunResult(DeliveryArchiveData data, ArchivedRunRecord record, bool completed)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            data.ResolvedAttemptIds ??= new List<string>();
            string attemptId = record.AttemptId?.Trim() ?? string.Empty;
            if (attemptId.Length > 0 && data.ResolvedAttemptIds.Contains(attemptId))
                return;
            if (attemptId.Length > 0)
                data.ResolvedAttemptIds.Insert(0, attemptId);

            record.RecordedAtUtc = DateTime.UtcNow.ToString("O");
            record.Outcome = completed ? "DELIVERED" : "LOST";
            if (completed)
            {
                data.DeliveriesCompleted++;
                data.BestCargoIntegrity = Math.Max(data.BestCargoIntegrity, record.CargoIntegrity);
                data.BestCredits = Math.Max(data.BestCredits, record.Credits);
                data.TotalTurns += Math.Max(0, record.Turns);
                data.TotalCardsPlayed += Math.Max(0, record.CardsPlayed);
                if (Enum.IsDefined(typeof(FinaleEnding), record.FinaleEnding) &&
                    record.FinaleEnding != (int)FinaleEnding.None)
                    AddUnique(data.DiscoveredEndings, record.FinaleEnding);
            }
            else
            {
                data.EncountersLost++;
            }

            ContractMasteryRecord mastery = FindOrCreateContractMastery(data, record.Contract);
            if (completed)
            {
                mastery.Deliveries++;
                mastery.BossVictories++;
                if (record.CargoIntegrity >= 3)
                    mastery.PristineDeliveries++;
                if (Enum.IsDefined(typeof(ChallengeId), record.Challenge) &&
                    record.Challenge != (int)ChallengeId.Standard)
                {
                    mastery.ChallengeDeliveries++;
                    ChallengeProgressRecord challenge = FindOrCreateChallengeProgress(data, record.Challenge);
                    challenge.Completions++;
                    challenge.BestHull = Math.Max(challenge.BestHull, record.Hull);
                    challenge.BestCargo = Math.Max(challenge.BestCargo, record.CargoIntegrity);
                    if (record.Turns > 0 && (challenge.BestTurns <= 0 || record.Turns < challenge.BestTurns))
                        challenge.BestTurns = record.Turns;
                }
            }

            if (Enum.IsDefined(typeof(EnemyKind), record.BossKind) &&
                BattleState.IsBossKind((EnemyKind)record.BossKind))
            {
                BossDossierRecord dossier = FindOrCreateBossDossier(data, record.BossKind);
                dossier.Encounters++;
                if (completed)
                {
                    dossier.Victories++;
                    if (Enum.IsDefined(typeof(FinaleEnding), record.FinaleEnding) &&
                        record.FinaleEnding != (int)FinaleEnding.None)
                        AddUnique(dossier.Endings, record.FinaleEnding);
                }
            }

            AccumulateWinRate(data, ContractDimension, record.Contract.ToString(), completed);
            if (!string.IsNullOrWhiteSpace(record.BuildProfile))
                AccumulateWinRate(data, BuildDimension, record.BuildProfile, completed);
            if (!string.IsNullOrWhiteSpace(record.RouteProfile))
                AccumulateWinRate(data, RouteDimension, record.RouteProfile, completed);
            if (record.BossKind >= 0)
                AccumulateWinRate(data, BossDimension, record.BossKind.ToString(), completed);

            data.RecentRuns.Insert(0, record);
            Normalize(data);
        }

        public static string CourierRank(DeliveryArchiveData data)
        {
            int completed = data?.DeliveriesCompleted ?? 0;
            if (completed >= 15) return LocalizationService.Text("rank.chief", "群岛首席邮差");
            if (completed >= 8) return LocalizationService.Text("rank.navigator", "云海航路员");
            if (completed >= 3) return LocalizationService.Text("rank.storm", "风暴信使");
            if (completed >= 1) return LocalizationService.Text("rank.courier", "正式邮差");
            return LocalizationService.Text("rank.trainee", "见习分拣员");
        }

        public static void Normalize(DeliveryArchiveData data)
        {
            if (data == null)
                return;
            data.RunsStarted = Math.Max(0, data.RunsStarted);
            data.DeliveriesCompleted = Math.Max(0, data.DeliveriesCompleted);
            data.EncountersLost = Math.Max(0, data.EncountersLost);
            data.BattlesWon = Math.Max(0, data.BattlesWon);
            data.BestCargoIntegrity = Math.Max(-1, Math.Min(3, data.BestCargoIntegrity));
            data.BestCredits = Math.Max(0, data.BestCredits);
            data.TotalTurns = Math.Max(0, data.TotalTurns);
            data.TotalCardsPlayed = Math.Max(0, data.TotalCardsPlayed);
            data.DiscoveredContracts = NormalizeIds(data.DiscoveredContracts, typeof(CargoContract));
            data.DiscoveredCards = NormalizeIds(data.DiscoveredCards, typeof(CardId));
            data.DiscoveredModules = NormalizeIds(data.DiscoveredModules, typeof(ModuleId));
            data.DiscoveredEnemies = NormalizeIds(data.DiscoveredEnemies, typeof(EnemyKind));
            data.DiscoveredEndings = NormalizeIds(data.DiscoveredEndings, typeof(FinaleEnding))
                .Where(value => value != (int)FinaleEnding.None).ToList();
            data.ChallengeProgress ??= new List<ChallengeProgressRecord>();
            data.ChallengeProgress = data.ChallengeProgress
                .Where(record => record != null && Enum.IsDefined(typeof(ChallengeId), record.Challenge) &&
                    record.Challenge != (int)ChallengeId.Standard)
                .GroupBy(record => record.Challenge).Select(group =>
                {
                    ChallengeProgressRecord first = group.First();
                    first.Attempts = Math.Max(0, group.Sum(record => record.Attempts));
                    first.Completions = Math.Max(0, group.Sum(record => record.Completions));
                    first.BestHull = Math.Max(0, group.Max(record => record.BestHull));
                    first.BestCargo = Math.Max(-1, Math.Min(3, group.Max(record => record.BestCargo)));
                    int[] turns = group.Select(record => record.BestTurns).Where(value => value > 0).ToArray();
                    first.BestTurns = turns.Length == 0 ? 0 : turns.Min();
                    return first;
                }).OrderBy(record => record.Challenge).ToList();
            data.ContractMastery ??= new List<ContractMasteryRecord>();
            data.ContractMastery = data.ContractMastery
                .Where(record => record != null && Enum.IsDefined(typeof(CargoContract), record.Contract))
                .GroupBy(record => record.Contract).Select(group =>
                {
                    ContractMasteryRecord first = group.First();
                    first.Runs = Math.Max(0, group.Sum(record => record.Runs));
                    first.Deliveries = Math.Max(0, group.Sum(record => record.Deliveries));
                    first.PristineDeliveries = Math.Max(0, group.Sum(record => record.PristineDeliveries));
                    first.ChallengeDeliveries = Math.Max(0, group.Sum(record => record.ChallengeDeliveries));
                    first.BossVictories = Math.Max(0, group.Sum(record => record.BossVictories));
                    return first;
                }).OrderBy(record => record.Contract).ToList();
            data.BossDossiers ??= new List<BossDossierRecord>();
            data.BossDossiers = data.BossDossiers
                .Where(record => record != null && Enum.IsDefined(typeof(EnemyKind), record.Boss) &&
                    BattleState.IsBossKind((EnemyKind)record.Boss))
                .GroupBy(record => record.Boss).Select(group =>
                {
                    BossDossierRecord first = group.First();
                    first.Encounters = Math.Max(0, group.Sum(record => record.Encounters));
                    first.Victories = Math.Max(0, group.Sum(record => record.Victories));
                    first.Endings = group.SelectMany(record => record.Endings ?? new List<int>())
                        .Where(value => Enum.IsDefined(typeof(FinaleEnding), value) &&
                            value != (int)FinaleEnding.None)
                        .Distinct().OrderBy(value => value).ToList();
                    return first;
                }).OrderBy(record => record.Boss).ToList();
            data.RecentRuns ??= new List<ArchivedRunRecord>();
            data.RecentRuns = data.RecentRuns.Where(record => record != null).Take(MaximumRecentRuns).ToList();
            foreach (ArchivedRunRecord record in data.RecentRuns)
            {
                if (record.Outcome != "LOST" ||
                    !Enum.IsDefined(typeof(PlayerDamageSource), record.DefeatSource))
                    record.DefeatSource = -1;
                record.AttemptId ??= string.Empty;
                record.DefeatDealer ??= string.Empty;
                record.DefeatDamage = Math.Max(0, record.DefeatDamage);
                record.DefeatRawDamage = Math.Max(0, record.DefeatRawDamage);
                record.DefeatShieldAbsorbed = Math.Max(0, record.DefeatShieldAbsorbed);
                record.DefeatHullBefore = Math.Max(0, record.DefeatHullBefore);
                record.DefeatTurn = Math.Max(0, record.DefeatTurn);
                record.DamageTaken = Math.Max(0, record.DamageTaken);
                record.Overheats = Math.Max(0, record.Overheats);
                record.CalamityInterrupts = Math.Max(0, record.CalamityInterrupts);
                record.CalamityEvades = Math.Max(0, record.CalamityEvades);
                record.CalamityHits = Math.Max(0, record.CalamityHits);
                record.TrackingHits = Math.Max(0, record.TrackingHits);
                record.ContractProcs = Math.Max(0, record.ContractProcs);
                record.ContractBonusCredits = Math.Max(0, record.ContractBonusCredits);
                record.BuildProfile ??= string.Empty;
                record.RouteProfile ??= string.Empty;
                if (!Enum.IsDefined(typeof(RouteIntel), record.RouteIntel))
                    record.RouteIntel = (int)RouteIntel.None;
                if (!Enum.IsDefined(typeof(FinaleEnding), record.FinaleEnding))
                    record.FinaleEnding = (int)FinaleEnding.None;
                if (!Enum.IsDefined(typeof(ChallengeId), record.Challenge))
                    record.Challenge = (int)ChallengeId.Standard;
                if (!Enum.IsDefined(typeof(EnemyKind), record.BossKind) ||
                    !BattleState.IsBossKind((EnemyKind)record.BossKind))
                    record.BossKind = -1;
                record.BuildSnapshots ??= new List<RunBuildSnapshot>();
                record.BuildSnapshots = record.BuildSnapshots
                    .Where(snapshot => snapshot != null)
                    .TakeLast(RunBuildSnapshotRules.MaximumSnapshots)
                    .ToList();
                foreach (RunBuildSnapshot snapshot in record.BuildSnapshots)
                    RunBuildSnapshotRules.Normalize(snapshot);
            }
            data.PerformanceStats ??= new List<RunWinRateRecord>();
            data.PerformanceStats = data.PerformanceStats
                .Where(record => record != null && !string.IsNullOrWhiteSpace(record.Dimension) &&
                    !string.IsNullOrWhiteSpace(record.Key))
                .GroupBy(record => new { Dimension = record.Dimension.Trim(), Key = record.Key.Trim() })
                .Select(group =>
                {
                    int attempts = Math.Max(0, group.Sum(record => Math.Max(0, record.Attempts)));
                    return new RunWinRateRecord
                    {
                        Dimension = group.Key.Dimension,
                        Key = group.Key.Key,
                        Attempts = attempts,
                        Wins = Math.Min(attempts, Math.Max(0, group.Sum(record => Math.Max(0, record.Wins))))
                    };
                }).OrderBy(record => record.Dimension).ThenBy(record => record.Key).ToList();
            data.ResolvedAttemptIds ??= new List<string>();
            data.ResolvedAttemptIds = data.ResolvedAttemptIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct()
                .Take(128)
                .ToList();
        }

        private static bool TryRead(string path, out DeliveryArchiveData data, out string error)
        {
            data = null;
            if (!File.Exists(path))
            {
                error = $"{Path.GetFileName(path)} 不存在";
                return false;
            }

            try
            {
                data = JsonUtility.FromJson<DeliveryArchiveData>(File.ReadAllText(path));
                if (data == null)
                    throw new InvalidDataException("档案内容为空");
                Migrate(data);
                if (data.Version != CurrentVersion)
                    throw new InvalidDataException($"不支持的档案版本 {data.Version}");
                Normalize(data);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                data = null;
                error = $"{Path.GetFileName(path)}：{exception.Message}";
                return false;
            }
        }

        private static void Migrate(DeliveryArchiveData data)
        {
            if (data.Version == 1)
            {
                data.RecentRuns ??= new List<ArchivedRunRecord>();
                foreach (ArchivedRunRecord record in data.RecentRuns)
                {
                    if (record != null)
                    {
                        record.DefeatSource = -1;
                        record.DefeatDealer ??= string.Empty;
                    }
                }
                data.Version = 2;
            }

            if (data.Version == 2)
            {
                data.DiscoveredEndings ??= new List<int>();
                data.Version = 3;
            }

            if (data.Version == 3)
            {
                data.ChallengeProgress ??= new List<ChallengeProgressRecord>();
                data.ContractMastery ??= new List<ContractMasteryRecord>();
                data.BossDossiers ??= new List<BossDossierRecord>();
                data.RecentRuns ??= new List<ArchivedRunRecord>();
                foreach (ArchivedRunRecord record in data.RecentRuns.Where(record => record != null))
                {
                    record.Challenge = (int)ChallengeId.Standard;
                    record.BossKind = -1;
                }
                data.Version = 4;
            }

            if (data.Version == 4)
            {
                data.RecentRuns ??= new List<ArchivedRunRecord>();
                foreach (ArchivedRunRecord record in data.RecentRuns.Where(record => record != null))
                    record.BuildSnapshots ??= new List<RunBuildSnapshot>();
                data.Version = 5;
            }

            if (data.Version == 5)
            {
                data.PerformanceStats ??= new List<RunWinRateRecord>();
                data.ResolvedAttemptIds ??= new List<string>();
                data.RecentRuns ??= new List<ArchivedRunRecord>();
                foreach (ArchivedRunRecord record in data.RecentRuns.Where(record => record != null))
                {
                    bool completed = record.Outcome == "DELIVERED";
                    AccumulateWinRate(data, ContractDimension, record.Contract.ToString(), completed);
                    if (record.BossKind >= 0)
                        AccumulateWinRate(data, BossDimension, record.BossKind.ToString(), completed);
                }
                data.Version = CurrentVersion;
            }
        }

        private static void AccumulateWinRate(DeliveryArchiveData data, string dimension, string key, bool won)
        {
            if (data == null || string.IsNullOrWhiteSpace(dimension) || string.IsNullOrWhiteSpace(key))
                return;
            data.PerformanceStats ??= new List<RunWinRateRecord>();
            RunWinRateRecord stat = data.PerformanceStats.FirstOrDefault(record => record != null &&
                record.Dimension == dimension && record.Key == key);
            if (stat == null)
            {
                stat = new RunWinRateRecord { Dimension = dimension, Key = key };
                data.PerformanceStats.Add(stat);
            }
            stat.Attempts++;
            if (won)
                stat.Wins++;
        }

        private static ChallengeProgressRecord FindOrCreateChallengeProgress(DeliveryArchiveData data, int challenge)
        {
            data.ChallengeProgress ??= new List<ChallengeProgressRecord>();
            ChallengeProgressRecord record =
                data.ChallengeProgress.FirstOrDefault(item => item.Challenge == challenge);
            if (record != null)
                return record;
            record = new ChallengeProgressRecord { Challenge = challenge };
            data.ChallengeProgress.Add(record);
            return record;
        }

        private static ContractMasteryRecord FindOrCreateContractMastery(DeliveryArchiveData data, int contract)
        {
            data.ContractMastery ??= new List<ContractMasteryRecord>();
            ContractMasteryRecord record = data.ContractMastery.FirstOrDefault(item => item.Contract == contract);
            if (record != null)
                return record;
            record = new ContractMasteryRecord { Contract = contract };
            data.ContractMastery.Add(record);
            return record;
        }

        private static BossDossierRecord FindOrCreateBossDossier(DeliveryArchiveData data, int boss)
        {
            data.BossDossiers ??= new List<BossDossierRecord>();
            BossDossierRecord record = data.BossDossiers.FirstOrDefault(item => item.Boss == boss);
            if (record != null)
                return record;
            record = new BossDossierRecord { Boss = boss };
            data.BossDossiers.Add(record);
            return record;
        }

        private static void AddUnique(List<int> target, int value)
        {
            target ??= new List<int>();
            if (!target.Contains(value))
                target.Add(value);
        }

        private static void AddUnique(List<int> target, IEnumerable<int> values)
        {
            if (values == null)
                return;
            foreach (int value in values)
                AddUnique(target, value);
        }

        private static List<int> NormalizeIds(List<int> values, Type enumType)
        {
            return (values ?? new List<int>())
                .Where(value => Enum.IsDefined(enumType, value))
                .Distinct()
                .OrderBy(value => value)
                .ToList();
        }
    }
}
