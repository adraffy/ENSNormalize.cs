using System.Collections.Generic;

namespace ADRaffy.ENSNormalize
{
    public class Whole
    {
        public readonly ReadOnlyIntSet Valid;
        public readonly ReadOnlyIntSet Confused;

        internal readonly Dictionary<int, int[]> Complement = new();
        internal Whole(ReadOnlyIntSet valid, ReadOnlyIntSet confused)
        {
            Valid = valid;
            Confused = confused;
        }
        public bool Contains(int cp) => Valid.Contains(cp) || Confused.Contains(cp);
    }
}
