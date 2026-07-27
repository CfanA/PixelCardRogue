using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SkyCourier
{
    [Serializable]
    public sealed class SessionRecoveryData
    {
        public int Version = SessionRecoveryService.CurrentVersion;
        public string SessionId;
        public string StartedAtUtc;
        public string EndedAtUtc;
        public string LastHeartbeatUtc;
        public string LastScreen;
        public string AttemptId;
        public bool CleanExit;
        public int Heartbeats;
    }

    public readonly struct SessionRecoveryInfo
    {
        public readonly bool PreviousSessionInterrupted;
        public readonly string LastScreen;
        public readonly string LastHeartbeatUtc;
        public readonly string AttemptId;

        public SessionRecoveryInfo(bool interrupted, string lastScreen, string lastHeartbeatUtc, string attemptId)
        {
            PreviousSessionInterrupted = interrupted;
            LastScreen = lastScreen;
            LastHeartbeatUtc = lastHeartbeatUtc;
            AttemptId = attemptId;
        }
    }

    public static class SessionRecoveryService
    {
        public const int CurrentVersion = 1;
        private const string FileName = "session.json";
        private const string TempFileName = "session.tmp";
        private static SessionRecoveryData current;

        public static string SessionPath => Path.Combine(Application.persistentDataPath, FileName);

        public static SessionRecoveryInfo BeginSession()
        {
            SessionRecoveryInfo previous = InspectDirectory(Application.persistentDataPath);
            current = new SessionRecoveryData
            {
                SessionId = Guid.NewGuid().ToString("N"),
                StartedAtUtc = DateTime.UtcNow.ToString("O"),
                LastHeartbeatUtc = DateTime.UtcNow.ToString("O"),
                LastScreen = "Title",
                CleanExit = false,
                Heartbeats = 1
            };
            WriteAtomic(Application.persistentDataPath, current);
            return previous;
        }

        public static SessionRecoveryInfo InspectDirectory(string directory)
        {
            string path = Path.Combine(directory, FileName);
            if (!File.Exists(path))
                return new SessionRecoveryInfo(false, null, null, null);

            try
            {
                SessionRecoveryData data = JsonUtility.FromJson<SessionRecoveryData>(File.ReadAllText(path));
                bool interrupted = data != null && data.Version == CurrentVersion && !data.CleanExit;
                return new SessionRecoveryInfo(interrupted, data?.LastScreen, data?.LastHeartbeatUtc,
                    data?.AttemptId);
            }
            catch
            {
                // A torn marker is itself evidence that the previous process did not finish cleanly.
                return new SessionRecoveryInfo(true, "Unknown", null, null);
            }
        }

        public static void Heartbeat(string screen, string attemptId)
        {
            if (current == null)
                return;
            current.LastHeartbeatUtc = DateTime.UtcNow.ToString("O");
            current.LastScreen = string.IsNullOrWhiteSpace(screen) ? "Unknown" : screen;
            current.AttemptId = attemptId;
            current.Heartbeats++;
            WriteAtomic(Application.persistentDataPath, current);
        }

        public static void MarkCleanExit()
        {
            if (current == null)
                return;
            current.CleanExit = true;
            current.EndedAtUtc = DateTime.UtcNow.ToString("O");
            current.LastHeartbeatUtc = current.EndedAtUtc;
            WriteAtomic(Application.persistentDataPath, current);
        }

        public static void WriteAtomic(string directory, SessionRecoveryData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            Directory.CreateDirectory(directory);
            string target = Path.Combine(directory, FileName);
            string temp = Path.Combine(directory, TempFileName);
            File.WriteAllText(temp, JsonUtility.ToJson(data, true));
            if (File.Exists(target))
                File.Delete(target);
            File.Move(temp, target);
        }
    }

    public static class ReleaseQualityRules
    {
        public const float ReferenceWidth = 1600f;
        public const float ReferenceHeight = 900f;
        public const int TargetFps = 60;
        public const int MaximumAmbientPrimitives = 180;
        public const int MaximumCombatPrimitives = 420;
        public const int MinimumAuxiliaryFontSize = 10;
        public const int MinimumBodyFontSize = 12;
        public const int MinimumDecisionFontSize = 14;

        public static Rect ContentViewport(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Display dimensions must be positive.");
            float scale = Mathf.Min(width / ReferenceWidth, height / ReferenceHeight);
            float viewportWidth = ReferenceWidth * scale;
            float viewportHeight = ReferenceHeight * scale;
            return new Rect((width - viewportWidth) * 0.5f, (height - viewportHeight) * 0.5f,
                viewportWidth, viewportHeight);
        }

        public static IReadOnlyList<Vector2Int> ReleaseResolutionMatrix { get; } = new[]
        {
            new Vector2Int(1024, 768),
            new Vector2Int(1280, 720),
            new Vector2Int(1366, 768),
            new Vector2Int(1600, 900),
            new Vector2Int(1920, 1080),
            new Vector2Int(1920, 1200),
            new Vector2Int(2560, 1080),
            new Vector2Int(2560, 1440),
            new Vector2Int(3440, 1440),
            new Vector2Int(3840, 2160)
        };
    }
}
