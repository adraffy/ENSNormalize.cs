namespace adraffy
{
    public class Group
    {
        public readonly int Index;
        public readonly string Name;
        public readonly GroupKind Kind;
        public readonly bool CMWhitelisted;
        public readonly ReadOnlyIntSet Primary;
        public readonly ReadOnlyIntSet Secondary;
        public bool IsRestricted { get => Kind == GroupKind.Restricted; }
        internal Group(int index, GroupKind kind, string name, bool cm, ReadOnlyIntSet primary, ReadOnlyIntSet secondary)
        {
            Index = index;
            Kind = kind;
            Name = name;
            CMWhitelisted = cm;
            Primary = primary;
            Secondary = secondary;
        }
        public bool Contains(int cp) => Primary.Contains(cp) || Secondary.Contains(cp);
        public override string ToString()
        {
            return IsRestricted ? $"Restricted[{Name}]" : Name;
        }
    }
}
