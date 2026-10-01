using System.Collections.Generic;
using System.Numerics;

namespace GalacticRupture.Sim
{
    // Where each ship of a group goes when the group moves to a point, facing a heading: the
    // line (destroyers and up) abreast in front, escorts in a rank behind it, troop ships at the
    // back. Ranks are spaced so a ship can swing to its broadside without fouling its
    // neighbour. Each ship keeps its own height.
    public static class Formation
    {
        const float Gap = 120f;

        static int Rank(Ship s) => s.Def.Class == HullClass.Transport ? 2 : s.Def.Class >= HullClass.Destroyer ? 0 : 1;

        public static Vector3[] Slots(IReadOnlyList<Ship> ships, Vector3 centre, float yaw)
        {
            var slots = new Vector3[ships.Count];
            Vector3 fwd = SimMath.HeadingToDir(yaw);
            Vector3 right = new Vector3(fwd.Z, 0f, -fwd.X);

            float back = 0f;
            var rank = new List<int>();
            for (int r = 0; r < 3; r++)
            {
                rank.Clear();
                for (int i = 0; i < ships.Count; i++)
                    if (Rank(ships[i]) == r) rank.Add(i);
                if (rank.Count == 0) continue;

                // Left to right as they already stand across the new heading, so no one crosses.
                rank.Sort((a, b) =>
                {
                    int c = Vector3.Dot(ships[a].Position, right).CompareTo(Vector3.Dot(ships[b].Position, right));
                    return c != 0 ? c : ships[a].Id.CompareTo(ships[b].Id);
                });

                float width = 0f, depth = 0f;
                for (int k = 0; k < rank.Count; k++)
                {
                    var d = ships[rank[k]].Def;
                    width += d.HalfLength * 2f + (k > 0 ? Gap : 0f);
                    depth = System.MathF.Max(depth, d.HalfLength);
                }
                // The first rank's centre is the click; later ranks fall in behind.
                if (back > 0f) back += depth;
                float x = -width * 0.5f;
                foreach (int i in rank)
                {
                    float half = ships[i].Def.HalfLength;
                    x += half;
                    Vector3 p = centre + right * x - fwd * back;
                    p.Y = ships[i].Position.Y;
                    slots[i] = p;
                    x += half + Gap;
                }
                back += depth + Gap * 2f;
            }
            return slots;
        }

        // The orders that send a group to `centre` in formation, all on one pace group.
        public static void Move(World world, IReadOnlyList<Ship> ships, Vector3 centre, float yaw)
        {
            var slots = Slots(ships, centre, yaw);
            int group = ships.Count > 1 ? world.NewGroup() : -1;
            for (int i = 0; i < ships.Count; i++)
            {
                var o = ShipOrder.MoveTo(slots[i]);
                o.Group = group;
                o.FacingYaw = yaw;
                world.Issue(ships[i].Id, o);
            }
        }
    }
}
