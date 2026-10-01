namespace GalacticRupture.Sim
{
    // How fast a battle plays at ×1, applied on top of every hull and gun. Ships move slower
    // than their rated speed, guns reload slower and hit softer, so a fight is a matter of
    // minutes and the player has time to read it and give orders. The HUD's speed buttons are
    // the way to hurry it.
    public static class Pace
    {
        public const float Movement = 0.55f;   // speed and acceleration
        public const float Turn = 0.8f;
        public const float Reload = 1.4f;
        public const float Damage = 0.7f;
        // Shields regenerate in step with the lower damage, so their share of a fight holds.
        public const float ShieldRegen = Damage / Reload;
    }
}
