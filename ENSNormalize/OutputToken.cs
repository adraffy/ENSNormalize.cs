using System.Collections.Generic;

namespace ADRaffy.ENSNormalize
{
    public class OutputToken
    {
        public readonly IList<int> Codepoints;
        public readonly EmojiSequence? Emoji;
        public bool IsEmoji { get => Emoji != null; }
        public OutputToken(IList<int> cps, EmojiSequence? emoji = null)
        {
            Codepoints = cps;
            Emoji = emoji;
        }
        public override string ToString() 
        {
            string name = IsEmoji ? "Emoji" : "Text";
            return $"{name}[{Codepoints.ToHexSequence()}]";
        }
    }
}
