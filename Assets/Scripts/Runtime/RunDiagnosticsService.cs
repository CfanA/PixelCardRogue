using System;
using System.IO;
using UnityEngine;

namespace SkyCourier
{
    [Serializable]
    public sealed class RunDiagnosticRecord
    {
        public string TimestampUtc;
        public string Event;
        public string GameVersion;
        public int RunSeed;
        public string Screen;
        public int RouteNodeId = -1;
        public int Encounter = -1;
        public int Contract = -1;
        public int Hull;
        public int CargoIntegrity;
        public int Turn;
        public string Message;
    }

    public static class RunDiagnosticsService
    {
        private const string DirectoryName = "Diagnostics";
        private const string LatestFileName = "latest-run.jsonl";
        private const string PreviousFileName = "previous-run.jsonl";
        private const string CrashFileName = "last-error.json";
        private static string diagnosticsDirectory;
        private static bool initialized;
        private static bool writingLogCallback;
        private static RunDiagnosticRecord lastContext;

        public static string DiagnosticsDirectory =>
            diagnosticsDirectory ?? Path.Combine(Application.persistentDataPath, DirectoryName);

        public static string LatestPath => Path.Combine(DiagnosticsDirectory, LatestFileName);

        public static void Initialize()
        {
            if (initialized)
                return;

            diagnosticsDirectory = Path.Combine(Application.persistentDataPath, DirectoryName);
            try
            {
                Directory.CreateDirectory(diagnosticsDirectory);
                string latest = Path.Combine(diagnosticsDirectory, LatestFileName);
                string previous = Path.Combine(diagnosticsDirectory, PreviousFileName);
                if (File.Exists(latest))
                    File.Copy(latest, previous, true);
                File.WriteAllText(latest, string.Empty);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"RUN_DIAGNOSTICS_INIT_FAILED: {exception.Message}");
            }

            Application.logMessageReceived += OnLogMessageReceived;
            initialized = true;
            Record(new RunDiagnosticRecord
            {
                Event = "session_started",
                Message = $"{Application.platform} | {Screen.width}x{Screen.height}"
            });
        }

        public static void Shutdown()
        {
            if (!initialized)
                return;
            Application.logMessageReceived -= OnLogMessageReceived;
            initialized = false;
        }

        public static void Record(RunDiagnosticRecord record)
        {
            if (record == null)
                return;
            record.TimestampUtc = DateTime.UtcNow.ToString("O");
            record.GameVersion = Application.version;
            lastContext = record;
            try
            {
                WriteRecordToDirectory(DiagnosticsDirectory, record, LatestFileName, true);
            }
            catch (Exception exception)
            {
                if (!writingLogCallback)
                    Debug.LogWarning($"RUN_DIAGNOSTICS_WRITE_FAILED: {exception.Message}");
            }
        }

        public static void WriteRecordToDirectory(string directory, RunDiagnosticRecord record, string fileName,
            bool append)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("诊断文件名不能为空", nameof(fileName));

            Directory.CreateDirectory(directory);
            string line = JsonUtility.ToJson(record) + Environment.NewLine;
            string path = Path.Combine(directory, fileName);
            if (append)
                File.AppendAllText(path, line);
            else
                File.WriteAllText(path, line);
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert || writingLogCallback)
                return;

            writingLogCallback = true;
            try
            {
                var record = new RunDiagnosticRecord
                {
                    TimestampUtc = DateTime.UtcNow.ToString("O"),
                    Event = "unity_error",
                    GameVersion = Application.version,
                    RunSeed = lastContext?.RunSeed ?? 0,
                    Screen = lastContext?.Screen,
                    RouteNodeId = lastContext?.RouteNodeId ?? -1,
                    Encounter = lastContext?.Encounter ?? -1,
                    Contract = lastContext?.Contract ?? -1,
                    Hull = lastContext?.Hull ?? 0,
                    CargoIntegrity = lastContext?.CargoIntegrity ?? 0,
                    Turn = lastContext?.Turn ?? 0,
                    Message = string.IsNullOrEmpty(stackTrace) ? condition : $"{condition}\n{stackTrace}"
                };
                WriteRecordToDirectory(DiagnosticsDirectory, record, CrashFileName, false);
            }
            catch
            {
                // Diagnostics must never interfere with gameplay or recursively log their own failure.
            }
            finally
            {
                writingLogCallback = false;
            }
        }
    }
}
