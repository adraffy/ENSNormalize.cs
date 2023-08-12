using System.Collections.Generic;
using System.Linq;

namespace ADRaffy.ENSNormalize
{
    public class NormDetails
    {
        public readonly string Name;
        public readonly string Desc;
        public readonly HashSet<EmojiSequence> Emojis;
        public readonly bool PossiblyConfusing;

        internal NormDetails(string norm, string desc, HashSet<EmojiSequence> emojis, bool confusing) {
            Name = norm;
            Desc = desc;
            Emojis = emojis;
            PossiblyConfusing = confusing;
        }
    }
}
