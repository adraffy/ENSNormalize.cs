namespace ENS
{
    public class Whole
    {
        public readonly IReadOnlySet<int> Valid;
        public readonly IReadOnlySet<int> Confused;

        internal readonly Dictionary<int, int[]> Complement = new();
        internal Whole() { }
        internal Whole(IEnumerable<int> valid, IEnumerable<int> confused)
        {
            Valid = new HashSet<int>(valid);
            Confused = new HashSet<int>(confused);
        }
    }
}
