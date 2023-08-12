using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ADRaffy.ENSNormalize
{
    public class NormDetails
    {
        public readonly string Name;
        public readonly string Desc;
        public readonly HashSet<Group> Groups;
        public readonly HashSet<EmojiSequence> Emojis;
        public readonly bool PossiblyConfusing;
        public string GroupDescription { get => string.Join("+", Groups.Select(g => g.Name).ToArray()); }
        public bool HasZWJEmoji { get => Emojis.Any(x => x.Normalized.Contains(0x200D)); }
        internal NormDetails(string norm, HashSet<Group> groups, HashSet<EmojiSequence> emojis, bool confusing) {
            Name = norm;
            Groups = groups;
            Emojis = emojis;
            PossiblyConfusing = confusing;
        }
    }
}
