namespace SkyCourier
{
    public enum BossStoryAlignment
    {
        Neutral,
        Allied,
        Hostile
    }

    public enum RouteStoryState
    {
        None,
        BeaconPromise,
        SalvageDebt,
        PromiseStrengthened,
        DebtDeepened,
        PromiseFulfilled,
        PromiseBetrayed,
        DebtRepaid,
        DebtDefied,
        SignalSevered,
        SilenceMaintained,
        SilentRouteSecured
    }

    public static class RouteStoryRules
    {
        public static bool IsPromise(RouteStoryState state)
        {
            return state == RouteStoryState.BeaconPromise ||
                state == RouteStoryState.PromiseStrengthened;
        }

        public static bool IsDebt(RouteStoryState state)
        {
            return state == RouteStoryState.SalvageDebt ||
                state == RouteStoryState.DebtDeepened;
        }

        public static bool IsSilent(RouteStoryState state)
        {
            return state == RouteStoryState.SignalSevered ||
                state == RouteStoryState.SilenceMaintained;
        }

        public static bool IsPending(RouteStoryState state)
        {
            return IsPromise(state) || IsDebt(state) || IsSilent(state);
        }

        public static BossStoryAlignment BossAlignment(RouteStoryState state)
        {
            if (IsPromise(state) || state == RouteStoryState.PromiseFulfilled ||
                state == RouteStoryState.DebtRepaid)
                return BossStoryAlignment.Allied;
            if (IsDebt(state) || state == RouteStoryState.PromiseBetrayed ||
                state == RouteStoryState.DebtDefied)
                return BossStoryAlignment.Hostile;
            return BossStoryAlignment.Neutral;
        }

        public static RouteStoryState Begin(bool cooperativeChoice)
        {
            return cooperativeChoice ? RouteStoryState.BeaconPromise : RouteStoryState.SalvageDebt;
        }

        public static RouteStoryState ContinueAtWreckage(RouteStoryState current, bool cooperativeChoice)
        {
            if (IsPromise(current))
                return cooperativeChoice ? RouteStoryState.PromiseStrengthened : RouteStoryState.SalvageDebt;
            if (IsDebt(current))
                return cooperativeChoice ? RouteStoryState.BeaconPromise : RouteStoryState.DebtDeepened;
            if (IsSilent(current))
                return cooperativeChoice ? RouteStoryState.BeaconPromise : RouteStoryState.SalvageDebt;
            return Begin(cooperativeChoice);
        }

        public static RouteStoryState ResolveAtObservatory(RouteStoryState current, bool cooperativeChoice)
        {
            if (IsPromise(current))
                return cooperativeChoice ? RouteStoryState.PromiseFulfilled : RouteStoryState.PromiseBetrayed;
            if (IsDebt(current))
                return cooperativeChoice ? RouteStoryState.DebtRepaid : RouteStoryState.DebtDefied;
            if (IsSilent(current))
                return cooperativeChoice ? RouteStoryState.PromiseFulfilled : RouteStoryState.DebtDefied;
            return current;
        }

        public static RouteStoryState ChooseIndependent(RouteStoryState current, int routeNodeId)
        {
            if (routeNodeId >= 12)
                return RouteStoryState.SilentRouteSecured;
            if (routeNodeId >= 7)
                return RouteStoryState.SilenceMaintained;
            return RouteStoryState.SignalSevered;
        }
    }
}
