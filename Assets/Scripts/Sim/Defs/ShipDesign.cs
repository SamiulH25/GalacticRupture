using System.Collections.Generic;

namespace GalacticRupture.Sim
{
    // A hull with a chosen gun in every hardpoint (IDEAS.md §1.1, §5.2): configuration, not
    // access, is a faction's identity.
    public sealed class ShipDesign
    {
        public string Name = "";
        public HullDef Hull;                 // the stock hull the design is built on
        public readonly List<MountDef> Mounts = new List<MountDef>();
        HullDef _built;

        public ShipDesign(string name, HullDef hull)
        {
            Name = name;
            Hull = hull;
            foreach (var hp in hull.Hardpoints) Mounts.Add(hp.Mount);
        }

        public static bool Fits(MountDef m, HardpointDef hp) => m != null && m.Size <= hp.Size;

        public bool Set(int hardpoint, MountDef m)
        {
            if (hardpoint < 0 || hardpoint >= Mounts.Count || !Fits(m, Hull.Hardpoints[hardpoint])) return false;
            Mounts[hardpoint] = m;
            _built = null;
            return true;
        }

        // What the yards build: the hull with these mounts, its cost and build time adjusted.
        public HullDef Built => _built ??= Hull.WithMounts(Name, Mounts);

        public ShipDesign Copy(string name)
        {
            var d = new ShipDesign(name, Hull);
            for (int i = 0; i < Mounts.Count; i++) d.Mounts[i] = Mounts[i];
            return d;
        }
    }
}
