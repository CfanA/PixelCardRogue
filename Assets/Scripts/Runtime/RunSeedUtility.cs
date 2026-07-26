using System;

namespace SkyCourier
{
    public static class RunSeedUtility
    {
        public const int LegacySeed = 70422;

        public static int Create()
        {
            byte[] bytes = Guid.NewGuid().ToByteArray();
            int seed = BitConverter.ToInt32(bytes, 0) & int.MaxValue;
            return seed == 0 ? LegacySeed : seed;
        }

        public static int DeriveEncounterSeed(int runSeed, int routeNodeId, EncounterId encounter)
        {
            unchecked
            {
                uint hash = 2166136261u;
                hash = (hash ^ (uint)(runSeed == 0 ? LegacySeed : runSeed)) * 16777619u;
                hash = (hash ^ (uint)routeNodeId) * 16777619u;
                hash = (hash ^ (uint)encounter) * 16777619u;
                int seed = (int)(hash & int.MaxValue);
                return seed == 0 ? LegacySeed : seed;
            }
        }
    }
}
