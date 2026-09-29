namespace GalacticRupture.Sim
{
    // The simulation assembly has no UnityEngine reference (IDEAS.md §8.5): it must stay
    // deterministic and runnable headless.
    public static class SimInfo
    {
        public const int TicksPerSecond = 30;
    }
}
