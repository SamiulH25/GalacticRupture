using System.Numerics;

namespace GalacticRupture.Sim
{
    public enum SimEventKind
    {
        Fired,          // Ship, Mount, Barrel, Position = muzzle, Direction
        ShieldHit,      // Ship, Position, Value = damage absorbed
        HullHit,        // Ship, Position, Value = damage to hull
        MountDestroyed, // Ship, Mount, Position
        ShipDestroyed,  // Ship, Position
        ShipSpawned,    // Ship
        ShieldsDown,    // Ship, Position = where the last hit broke through
        AbilityUsed,    // Ship, Mount = the Ability
        RockHit,        // Position = where a round struck an asteroid, Value = damage
    }

    // What happened this tick, for the presentation layer. Nothing in the sim reads these back.
    public struct SimEvent
    {
        public SimEventKind Kind;
        public int Ship;
        public int Mount;
        public int Barrel;
        public Vector3 Position;
        public Vector3 Direction;
        public float Value;
    }
}
