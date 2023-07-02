using System.Collections.Generic;
using System.Linq;

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
        public readonly IReadOnlyCollection<int> Primary;
        public readonly IReadOnlyCollection<int> Secondary;
        public bool IsRestricted { get => Kind == GroupKind.Restricted; }
        internal Group(GroupKind kind, string name)
        {
            Index = -1;
            Name = Description = name;
            Kind = kind;
        }
        internal Group(int index, string name, bool restricted, bool cm, IEnumerable<int> primary, IEnumerable<int> secondary)
        {
            Index = index;
            Name = name;
            Description = restricted ? $"Restricted[{Name}]" : Name;
            Kind = restricted ? GroupKind.Script : GroupKind.Restricted;
            CMWhitelisted = cm;
            Primary = (IReadOnlyCollection<int>)new HashSet<int>(primary); 
            Secondary = (IReadOnlyCollection<int>)new HashSet<int>(secondary);
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
