using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SkyCourier
{
    public enum TutorialTopic
    {
        Intent,
        LaneAttack,
        Heat,
        Cargo,
        LaneShift,
        Tracking,
        Retrofit,
        Chronicle,
        Outpost,
        Boss,
        EnemyIntel
    }

    [Serializable]
    public sealed class TutorialProgressData
    {
        public int Version = TutorialProgressService.CurrentVersion;
        public List<int> SeenTopics = new List<int>();
    }

    public sealed class RuleGlossaryEntry
    {
        public TutorialTopic Topic { get; }
        public string Symbol { get; }
        public string CategoryKey { get; }
        public string CategoryFallback { get; }

        public RuleGlossaryEntry(TutorialTopic topic, string symbol, string categoryKey, string categoryFallback)
        {
            Topic = topic;
            Symbol = symbol;
            CategoryKey = categoryKey;
            CategoryFallback = categoryFallback;
        }

        public string Category => LocalizationService.Text(CategoryKey, CategoryFallback);
        public string Title => LocalizationService.Text($"tutorial.{Topic}.title", Topic.ToString());
        public string Trigger => LocalizationService.Text($"tutorial.{Topic}.trigger", string.Empty);
        public string Body => LocalizationService.Text($"tutorial.{Topic}.body", string.Empty);
        public string Glossary => LocalizationService.Text($"glossary.{Topic}", Body);
    }

    public static class RuleGlossaryCatalog
    {
        private static readonly RuleGlossaryEntry[] Entries =
        {
            new RuleGlossaryEntry(TutorialTopic.Intent, "!", "tutorial.category.read", "判读"),
            new RuleGlossaryEntry(TutorialTopic.LaneAttack, ">", "tutorial.category.attack", "攻击"),
            new RuleGlossaryEntry(TutorialTopic.Heat, "#", "tutorial.category.resource", "资源"),
            new RuleGlossaryEntry(TutorialTopic.Cargo, "[]", "tutorial.category.contract", "合同"),
            new RuleGlossaryEntry(TutorialTopic.LaneShift, "<>", "tutorial.category.maneuver", "机动"),
            new RuleGlossaryEntry(TutorialTopic.Tracking, "X", "tutorial.category.danger", "危险"),
            new RuleGlossaryEntry(TutorialTopic.Retrofit, "+", "tutorial.category.build", "构筑"),
            new RuleGlossaryEntry(TutorialTopic.Chronicle, "*", "tutorial.category.route", "路线"),
            new RuleGlossaryEntry(TutorialTopic.Outpost, "$", "tutorial.category.route", "路线"),
            new RuleGlossaryEntry(TutorialTopic.Boss, "!!", "tutorial.category.danger", "危险"),
            new RuleGlossaryEntry(TutorialTopic.EnemyIntel, "i", "tutorial.category.read", "判读")
        };

        public static IReadOnlyList<RuleGlossaryEntry> All => Entries;

        public static RuleGlossaryEntry Get(TutorialTopic topic) =>
            Entries.First(entry => entry.Topic == topic);

        public static bool IsComplete => Entries.Length == Enum.GetValues(typeof(TutorialTopic)).Length &&
            Entries.Select(entry => entry.Topic).Distinct().Count() == Entries.Length;

        public static IEnumerable<string> LocalizationKeys()
        {
            foreach (RuleGlossaryEntry entry in Entries)
            {
                yield return entry.CategoryKey;
                yield return $"tutorial.{entry.Topic}.title";
                yield return $"tutorial.{entry.Topic}.trigger";
                yield return $"tutorial.{entry.Topic}.body";
                yield return $"glossary.{entry.Topic}";
            }
        }
    }

    public static class TutorialProgressService
    {
        public const int CurrentVersion = 1;
        private const string ProgressKey = "SkyCourier.Tutorial.v1";

        public static TutorialProgressData Load(bool legacyGuideCompleted = false)
        {
            TutorialProgressData data = null;
            if (PlayerPrefs.HasKey(ProgressKey))
            {
                try
                {
                    data = JsonUtility.FromJson<TutorialProgressData>(PlayerPrefs.GetString(ProgressKey));
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"TUTORIAL_PROGRESS_LOAD_FAILED: {exception.Message}");
                }
            }

            data ??= new TutorialProgressData();
            Normalize(data);
            if (legacyGuideCompleted && data.SeenTopics.Count == 0)
            {
                foreach (TutorialTopic topic in new[]
                {
                    TutorialTopic.Intent, TutorialTopic.LaneAttack, TutorialTopic.Heat,
                    TutorialTopic.LaneShift, TutorialTopic.Tracking
                })
                    data.SeenTopics.Add((int)topic);
                Save(data);
            }
            return data;
        }

        public static bool HasSeen(TutorialProgressData data, TutorialTopic topic)
        {
            Normalize(data);
            return data.SeenTopics.Contains((int)topic);
        }

        public static void MarkSeen(TutorialProgressData data, TutorialTopic topic)
        {
            Normalize(data);
            if (!data.SeenTopics.Contains((int)topic))
                data.SeenTopics.Add((int)topic);
            Save(data);
        }

        public static void Normalize(TutorialProgressData data)
        {
            if (data == null)
                return;
            data.Version = CurrentVersion;
            data.SeenTopics ??= new List<int>();
            data.SeenTopics = data.SeenTopics
                .Where(value => Enum.IsDefined(typeof(TutorialTopic), value))
                .Distinct().OrderBy(value => value).ToList();
        }

        public static void Save(TutorialProgressData data)
        {
            if (data == null)
                return;
            Normalize(data);
            PlayerPrefs.SetString(ProgressKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public static void Reset()
        {
            PlayerPrefs.DeleteKey(ProgressKey);
            PlayerPrefs.Save();
        }
    }

    public static class FirstRunGuidanceRules
    {
        public static bool ChallengesAvailable(DeliveryArchiveData archive)
        {
            if (archive == null)
                return false;
            return archive.DeliveriesCompleted + archive.EncountersLost > 0 ||
                (archive.RecentRuns != null && archive.RecentRuns.Count > 0);
        }
    }
}
