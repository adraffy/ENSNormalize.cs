namespace adraffy
{
    public class Group
    {
        public readonly int Index;
        public readonly string Name;
        public readonly string Description;
        public readonly bool Restricted;
        public readonly IReadOnlySet<int> Primary;
        public readonly IReadOnlySet<int> Secondary;
        public readonly bool CMWhitelisted;
        internal Group(int index,  string name, bool restricted, bool cm, IEnumerable<int> primary, IEnumerable<int> secondary)
        {
            Index = index;
            Name = name;
            Restricted = restricted;
            CMWhitelisted = cm;
            Primary = new HashSet<int>(primary); 
            Secondary = new HashSet<int>(secondary);
            Description = Restricted ? $"Restricted[{Name}]" : Name;
        }
        public bool Contains(int cp)
        {
            return Primary.Contains(cp) || Secondary.Contains(cp);
        }
        public override string ToString()
        {
            return Description;
        }
    }
}
