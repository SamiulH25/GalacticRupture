namespace GalacticRupture.Sim
{
    // FNV-1a over bit patterns: "same seed, same outcome" is checked by comparing these.
    public struct StateHash
    {
        ulong _h;

        public static StateHash Create() => new StateHash { _h = 14695981039346656037UL };

        public ulong Value => _h;

        public void Add(ulong v)
        {
            for (int i = 0; i < 8; i++)
            {
                _h ^= (v >> (i * 8)) & 0xFF;
                _h *= 1099511628211UL;
            }
        }

        public void Add(int v) => Add((ulong)(uint)v);

        public void Add(float v) => Add((ulong)System.BitConverter.SingleToInt32Bits(v));

        public void Add(System.Numerics.Vector3 v)
        {
            Add(v.X);
            Add(v.Y);
            Add(v.Z);
        }
    }
}
