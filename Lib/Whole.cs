using System.Collections.Generic;

namespace adraffy
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
    }
}
