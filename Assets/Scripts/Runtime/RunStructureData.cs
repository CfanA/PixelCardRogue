using System.Collections.Generic;
using System.Linq;

namespace SkyCourier
{
    public enum RunAct
    {
        Departure,
        Pivot,
        FinalApproach
    }

    public enum DepartureDirective
    {
        Unselected,
        LegacyManifest,
        StandardManifest,
        AdvancePayment,
        HotLaunch
    }

    public enum FinalApproachPlan
    {
        Unselected,
        HoldCourse,
        FieldPatch,
        DeadweightTrim,
        CargoOverclock
    }

    public static class RunStructureCatalog
    {
        public const int RetrofitColumn = 4;
        public const int FinalApproachColumn = 9;

        private static readonly ModuleId[] FragileMedicineModules =
        {
            ModuleId.PrismBulkhead,
            ModuleId.AegisCapacitor,
            ModuleId.PrecisionMatrix
        };

        private static readonly ModuleId[] CryoSerumModules =
        {
            ModuleId.CryoHeart,
            ModuleId.ZeroPointReactor,
            ModuleId.RedlineReactor
        };

        private static readonly ModuleId[] StormCoreModules =
        {
            ModuleId.MomentumFlywheel,
            ModuleId.VectorThruster,
            ModuleId.SwarmUplink
        };

        private static readonly ModuleId[] BlackBoxModules =
        {
            ModuleId.GhostDecoder,
            ModuleId.VectorThruster,
            ModuleId.ExecutionChip
        };

        private static readonly ModuleId[] SignalSeedModules =
        {
            ModuleId.PrecisionMatrix,
            ModuleId.AegisCapacitor,
            ModuleId.ExecutionChip
        };

        public static RunAct ActForColumn(int column)
        {
            if (column < 3)
                return RunAct.Departure;
            return column < FinalApproachColumn ? RunAct.Pivot : RunAct.FinalApproach;
        }

        public static int ActNumber(RunAct act) => (int)act + 1;

        public static int FloorForColumn(int column)
        {
            if (column < 2) return 1;
            if (column < 4) return 2;
            if (column < 7) return 3;
            if (column < 9) return 4;
            return 5;
        }

        public static string FloorRole(int floor) => floor switch
        {
            1 => "启动期",
            2 => "体检期",
            3 => "战力分水岭",
            4 => "高压期",
            _ => "终局考核"
        };

        public static IReadOnlyList<ModuleId> FinalModulePriority(CargoContract contract)
        {
            return contract switch
            {
                CargoContract.CryoSerum => CryoSerumModules,
                CargoContract.StormCore => StormCoreModules,
                CargoContract.BlackBoxRelay => BlackBoxModules,
                CargoContract.SignalSeed => SignalSeedModules,
                _ => FragileMedicineModules
            };
        }

        public static ModuleId? SuggestedFinalModule(CargoContract contract, IEnumerable<ModuleId> installed)
        {
            HashSet<ModuleId> installedSet = installed == null
                ? new HashSet<ModuleId>()
                : new HashSet<ModuleId>(installed);
            return FinalModulePriority(contract).Cast<ModuleId?>()
                .FirstOrDefault(module => module.HasValue && !installedSet.Contains(module.Value));
        }
    }
}
