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
        public int DefeatSource = -1;
        public string DefeatDealer;
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
        public List<ArchivedRunRecord> RecentRuns = new List<ArchivedRunRecord>();
    }

    public static class DeliveryArchiveService
    {
        public const int CurrentVersion = 3;
        public const int MaximumRecentRuns = 8;
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

        public static void RegisterRunStarted(DeliveryArchiveData data, int contract, IEnumerable<int> deck)
        {
            data.RunsStarted++;
            AddUnique(data.DiscoveredContracts, contract);
            AddUnique(data.DiscoveredCards, deck);
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
            data.RecentRuns ??= new List<ArchivedRunRecord>();
            data.RecentRuns = data.RecentRuns.Where(record => record != null).Take(MaximumRecentRuns).ToList();
            foreach (ArchivedRunRecord record in data.RecentRuns)
            {
                if (record.Outcome != "LOST" ||
                    !Enum.IsDefined(typeof(PlayerDamageSource), record.DefeatSource))
                    record.DefeatSource = -1;
                record.DefeatDealer ??= string.Empty;
                if (!Enum.IsDefined(typeof(RouteIntel), record.RouteIntel))
                    record.RouteIntel = (int)RouteIntel.None;
                if (!Enum.IsDefined(typeof(FinaleEnding), record.FinaleEnding))
                    record.FinaleEnding = (int)FinaleEnding.None;
            }
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
                data.Version = CurrentVersion;
            }
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
