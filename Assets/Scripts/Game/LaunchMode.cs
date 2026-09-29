namespace GalacticRupture.Game
{
    public enum Launch
    {
        Campaign,   // default: the galaxy map, battles fought in the tactical view
        Skirmish,   // -grSkirmish: a single tactical engagement
        Toy,        // -grToy or -grCapture: the turret toy
        Bench,      // -grBench: unit-count benchmark
        RtsCapture, // -grCaptureRts: scripted skirmish screenshots
    }

    public static class LaunchMode
    {
        static Launch? _mode;

        public static Launch Current => _mode ??= Resolve();

        static Launch Resolve()
        {
            var args = System.Environment.GetCommandLineArgs();
            bool Has(string a) => System.Array.IndexOf(args, a) >= 0;
            if (Has("-grBench")) return Launch.Bench;
            if (Has("-grCaptureRts")) return Launch.RtsCapture;
            if (Has("-grCapture") || Has("-grToy")) return Launch.Toy;
            if (Has("-grSkirmish")) return Launch.Skirmish;
            return Launch.Campaign;
        }
    }
}
