using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SkyCourier
{
    [Serializable]
    public sealed class RunSaveData
    {
        public int Version = RunSaveService.CurrentVersion;
        public string SavedAtUtc;
        public int RunSeed;
        public int EncounterSeed;
        public string Screen;
        public int Encounter;
        public int Contract;
        public int AirframeModification;
        public int RouteStoryState;
        public int RouteIntel;
        public List<int> Deck = new List<int>();
        public List<int> Upgrades = new List<int>();
        public List<int> UpgradeBranchCards = new List<int>();
        public List<int> UpgradeBranches = new List<int>();
        public List<int> Modules = new List<int>();
        public List<int> CompletedRouteNodes = new List<int>();
        public int RouteIndex;
        public int SelectedRouteNodeId;
        public int LastCompletedRouteNodeId = -1;
        public float RouteScroll;
        public bool EventResolved;
        public string EventResult;
        public bool RestResolved;
        public string RestResult;
        public int Credits;
        public int Hull;
        public int CargoIntegrity;
        public int ContractBonus;
        public bool RepairBought;
        public bool[] ShopBought = new bool[3];
        public int Turns;
        public int CardsPlayed;
        public int DamageTaken;
        public int Overheats;
        public int CalamityInterrupts;
        public int CalamityEvades;
        public int CalamityHits;
        public int TrackingHits;
        public int LastRewardCredits;
        public int LastFieldRepair;
    }

    public static class RunSaveService
    {
        public const int CurrentVersion = 5;
        private const string SaveFileName = "run.json";
        private const string BackupFileName = "run_backup.json";
        private const string TempFileName = "run.tmp";

        public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);
        public static string BackupPath => Path.Combine(Application.persistentDataPath, BackupFileName);

        public static bool HasSave => File.Exists(SavePath) || File.Exists(BackupPath);

        public static bool TryLoad(out RunSaveData data, out bool restoredBackup, out string error)
        {
            return TryLoadFromDirectory(Application.persistentDataPath, out data, out restoredBackup, out error);
        }

        public static bool TryLoadFromDirectory(string directory, out RunSaveData data, out bool restoredBackup,
            out string error)
        {
            restoredBackup = false;
            string savePath = Path.Combine(directory, SaveFileName);
            string backupPath = Path.Combine(directory, BackupFileName);
            if (TryRead(savePath, out data, out error))
                return true;

            string primaryError = error;
            if (TryRead(backupPath, out data, out error))
            {
                restoredBackup = true;
                return true;
            }

            error = string.IsNullOrEmpty(primaryError) ? error : $"{primaryError} / {error}";
            return false;
        }

        public static void Save(RunSaveData data)
        {
            SaveToDirectory(data, Application.persistentDataPath);
        }

        public static void SaveToDirectory(RunSaveData data, string directory)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            data.Version = CurrentVersion;
            data.SavedAtUtc = DateTime.UtcNow.ToString("O");
            Directory.CreateDirectory(directory);

            string json = JsonUtility.ToJson(data, true);
            string savePath = Path.Combine(directory, SaveFileName);
            string backupPath = Path.Combine(directory, BackupFileName);
            string tempPath = Path.Combine(directory, TempFileName);
            File.WriteAllText(tempPath, json);

            if (File.Exists(savePath))
            {
                try
                {
                    File.Replace(tempPath, savePath, backupPath);
                    return;
                }
                catch (PlatformNotSupportedException)
                {
                }
                catch (IOException)
                {
                }

                File.Copy(savePath, backupPath, true);
                File.Delete(savePath);
            }

            File.Move(tempPath, savePath);
        }

        public static void Delete()
        {
            DeleteFromDirectory(Application.persistentDataPath);
        }

        public static void DeleteFromDirectory(string directory)
        {
            DeleteIfPresent(Path.Combine(directory, SaveFileName));
            DeleteIfPresent(Path.Combine(directory, BackupFileName));
            DeleteIfPresent(Path.Combine(directory, TempFileName));
        }

        private static bool TryRead(string path, out RunSaveData data, out string error)
        {
            data = null;
            if (!File.Exists(path))
            {
                error = $"{Path.GetFileName(path)} 不存在";
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                data = JsonUtility.FromJson<RunSaveData>(json);
                if (data == null)
                    throw new InvalidDataException("存档内容为空");
                Migrate(data);
                if (data.Version != CurrentVersion)
                    throw new InvalidDataException($"不支持的存档版本 {data.Version}");
                if (data.Deck == null || data.Deck.Count == 0)
                    throw new InvalidDataException("牌组数据缺失");
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

        private static void Migrate(RunSaveData data)
        {
            if (data.Version == 1)
            {
                data.RunSeed = RunSeedUtility.LegacySeed;
                data.EncounterSeed = RunSeedUtility.LegacySeed;
                data.Version = 2;
            }

            if (data.Version == 2)
            {
                data.AirframeModification = (int)SkyCourier.AirframeModification.None;
                data.Version = 3;
            }

            if (data.Version == 3)
            {
                data.RouteStoryState = (int)SkyCourier.RouteStoryState.None;
                data.Version = 4;
            }

            if (data.Version == 4)
            {
                data.RouteIntel = (int)SkyCourier.RouteIntel.None;
                data.Version = CurrentVersion;
            }

            if (data.Version == CurrentVersion)
            {
                if (data.RunSeed == 0)
                    data.RunSeed = RunSeedUtility.LegacySeed;
                if (data.EncounterSeed == 0)
                    data.EncounterSeed = RunSeedUtility.LegacySeed;
            }
        }

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
