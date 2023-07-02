using System.Collections.Generic;

namespace adraffy
{
    public class Whole
    {
        public readonly IReadOnlyCollection<int> Valid;
        public readonly IReadOnlyCollection<int> Confused;

        internal readonly Dictionary<int, int[]> Complement = new();
        internal Whole() { }
        internal Whole(List<int> valid, List<int> confused)
        {
            Valid = (IReadOnlyCollection<int>)new HashSet<int>(valid);
            Confused = (IReadOnlyCollection<int>)new HashSet<int>(confused);
        }
    }
}
