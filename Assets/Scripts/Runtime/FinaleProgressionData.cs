namespace SkyCourier
{
    public enum RouteIntel
    {
        None,
        CurtainCipher,
        FluxCompass,
        DualChannelDecoder
    }

    public enum FinaleEnding
    {
        None,
        WyrmClearSky,
        WyrmSignalCovenant,
        WyrmBlackout,
        MantaCalmSea,
        MantaPostalShield,
        MantaScavengerCrown
    }

    public static class FinaleProgressionRules
    {
        public static RouteIntel IntelForPreludeNode(int nodeId)
        {
            return nodeId switch
            {
                15 => RouteIntel.CurtainCipher,
                16 => RouteIntel.DualChannelDecoder,
                17 => RouteIntel.FluxCompass,
                _ => RouteIntel.None
            };
        }

        public static bool IntelApplies(RouteIntel intel, EnemyKind boss)
        {
            return intel == RouteIntel.DualChannelDecoder ||
                intel == RouteIntel.CurtainCipher && boss == EnemyKind.CloudWyrm ||
                intel == RouteIntel.FluxCompass && boss == EnemyKind.StormManta;
        }

        public static FinaleEnding EndingFor(EnemyKind boss, BossStoryAlignment alignment)
        {
            if (boss == EnemyKind.CloudWyrm)
            {
                return alignment switch
                {
                    BossStoryAlignment.Allied => FinaleEnding.WyrmSignalCovenant,
                    BossStoryAlignment.Hostile => FinaleEnding.WyrmBlackout,
                    _ => FinaleEnding.WyrmClearSky
                };
            }

            return alignment switch
            {
                BossStoryAlignment.Allied => FinaleEnding.MantaPostalShield,
                BossStoryAlignment.Hostile => FinaleEnding.MantaScavengerCrown,
                _ => FinaleEnding.MantaCalmSea
            };
        }
    }
}
