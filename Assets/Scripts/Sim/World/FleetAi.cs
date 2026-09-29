using System.Numerics;

namespace GalacticRupture.Sim
{
    // Tactical doctrine for a side nobody is commanding: idle ships pick the nearest enemy;
    // escorts make attack runs, line ships close and slug it out.
    public static class FleetAi
    {
        public static void Command(World world, int side)
        {
            foreach (var s in world.Ships)
            {
                if (!s.Alive || s.Side != side) continue;
                var current = world.ShipById(s.Order.OtherShip);
                bool busy = (s.Order.Kind == OrderKind.Attack || s.Order.Kind == OrderKind.AttackRun) && current != null && current.Alive;
                if (busy) continue;
                var target = Nearest(world, s);
                if (target == null) continue;
                if (s.Def.Class <= HullClass.Frigate)
                {
                    var o = ShipOrder.AttackRun(target.Id);
                    o.PassDistance = s.Def.Class == HullClass.Corvette ? world.Rng.Range(150f, 350f) : world.Rng.Range(400f, 700f);
                    o.RunOutDistance = world.Rng.Range(1400f, 2200f);
                    world.Issue(s.Id, o);
                }
                else
                {
                    world.Issue(s.Id, ShipOrder.Attack(target.Id));
                }
            }
        }

        public static Ship Nearest(World world, Ship from)
        {
            Ship best = null;
            float bestD = float.MaxValue;
            foreach (var s in world.Ships)
            {
                if (!s.Alive || s.Side == from.Side) continue;
                float d = Vector3.DistanceSquared(s.Position, from.Position);
                if (d < bestD)
                {
                    bestD = d;
                    best = s;
                }
            }
            return best;
        }
    }
}
