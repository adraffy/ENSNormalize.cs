using System.Collections.Generic;

namespace adraffy
{
    public class OutputToken
    {
        public readonly IReadOnlyList<int> Codepoints;
        public readonly EmojiSequence? Emoji;
        public bool IsEmoji { get => Emoji != null; }
        public OutputToken(IReadOnlyList<int> cps, EmojiSequence? emoji = null)
        {
            Emoji = emoji;
            Codepoints = cps;
        }
        public override string ToString() 
        {
            string name = IsEmoji ? "Emoji" : "Text";
            return $"{name}[{Codepoints.ToHexSequence()}]";
        }
    }
}
