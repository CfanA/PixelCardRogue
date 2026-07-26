using System;
using System.Collections.Generic;

namespace SkyCourier
{
    public enum RunBuildSnapshotMoment
    {
        Departure,
        Retrofit,
        RouteEvent,
        Reward,
        ServiceDock,
        ShopService,
        FinalApproach,
        BossApproach,
        RunResult
    }

    [Serializable]
    public sealed class RunBuildSnapshot
    {
        public string Key;
        public string CapturedAtUtc;
        public int Moment;
        public int RouteColumn;
        public int RouteNodeId;
        public int Act;
        public int Hull;
        public int CargoIntegrity;
        public int Credits;
        public int AirframeModification;
        public int RouteStoryState;
        public List<int> Deck = new List<int>();
        public List<int> Upgrades = new List<int>();
        public List<int> UpgradeBranchCards = new List<int>();
        public List<int> UpgradeBranches = new List<int>();
        public List<int> Modules = new List<int>();

        public RunBuildSnapshot Clone()
        {
            return new RunBuildSnapshot
            {
                Key = Key,
                CapturedAtUtc = CapturedAtUtc,
                Moment = Moment,
                RouteColumn = RouteColumn,
                RouteNodeId = RouteNodeId,
                Act = Act,
                Hull = Hull,
                CargoIntegrity = CargoIntegrity,
                Credits = Credits,
                AirframeModification = AirframeModification,
                RouteStoryState = RouteStoryState,
                Deck = new List<int>(Deck ?? new List<int>()),
                Upgrades = new List<int>(Upgrades ?? new List<int>()),
                UpgradeBranchCards = new List<int>(UpgradeBranchCards ?? new List<int>()),
                UpgradeBranches = new List<int>(UpgradeBranches ?? new List<int>()),
                Modules = new List<int>(Modules ?? new List<int>())
            };
        }
    }

    public static class RunBuildSnapshotRules
    {
        public const int MaximumSnapshots = 32;

        public static void Normalize(RunBuildSnapshot snapshot)
        {
            if (snapshot == null)
                return;
            snapshot.Key ??= string.Empty;
            snapshot.CapturedAtUtc ??= string.Empty;
            snapshot.Deck ??= new List<int>();
            snapshot.Upgrades ??= new List<int>();
            snapshot.UpgradeBranchCards ??= new List<int>();
            snapshot.UpgradeBranches ??= new List<int>();
            snapshot.Modules ??= new List<int>();
        }

        public static List<RunBuildSnapshot> Clone(IEnumerable<RunBuildSnapshot> snapshots)
        {
            var result = new List<RunBuildSnapshot>();
            if (snapshots == null)
                return result;
            foreach (RunBuildSnapshot snapshot in snapshots)
            {
                if (snapshot != null)
                    result.Add(snapshot.Clone());
            }
            return result;
        }
    }

    public static class RouteDecisionCatalog
    {
        public static int ShopPurgeCost(int nodeId) => nodeId == 11 ? 28 : 18;

        public static int ShopCalibrationCost(int nodeId) => nodeId == 11 ? 38 : 28;

        public static int IndependentEventCredits(int nodeId) => nodeId == 2 ? 18 : nodeId == 7 ? 12 : 25;
    }
}
