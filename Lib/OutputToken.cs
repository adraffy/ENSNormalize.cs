using System.Collections.Generic;

namespace adraffy
{
    public class OutputToken
    {
        public readonly bool IsEmoji;
        public readonly int[] Codepoints;
        public OutputToken(bool emoji, int[] cps)
        {
            IsEmoji = emoji;
            Codepoints = cps;
        }
        public override string ToString() 
        {
            string name = IsEmoji ? "Emoji" : "Text";
            return $"{name}[{Codepoints.ToHexSequence()}]";
        }
    }
}
