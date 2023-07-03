namespace adraffy
{
    public class Group
    {
        public enum GroupKind : byte
        {
            Script,
            Restricted,
            ASCII,
            Emoji
        }

        static public readonly Group ASCII = new(GroupKind.ASCII, "ASCII");
        static public readonly Group EMOJI = new(GroupKind.Emoji, "Emoji");

        public readonly int Index;
        public readonly string Name;
        public readonly string Description;
        public readonly GroupKind Kind;
        public readonly bool CMWhitelisted;
        public readonly ReadOnlyIntSet Primary;
        public readonly ReadOnlyIntSet Secondary;
        public bool IsRestricted { get => Kind == GroupKind.Restricted; }
        internal Group(GroupKind kind, string name)
        {
            Index = -1;
            Name = Description = name;
            Kind = kind;
            Primary = ReadOnlyIntSet.EMPTY;
            Secondary = ReadOnlyIntSet.EMPTY;
        }
        internal Group(int index, string name, bool restricted, bool cm, ReadOnlyIntSet primary, ReadOnlyIntSet secondary)
        {
            Index = index;
            Name = name;
            Description = restricted ? $"Restricted[{Name}]" : Name;
            Kind = restricted ? GroupKind.Script : GroupKind.Restricted;
            CMWhitelisted = cm;
            Primary = primary;
            Secondary = secondary;
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
