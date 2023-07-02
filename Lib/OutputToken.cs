namespace adraffy
{
    public class OutputToken
    {
        public readonly int[] Codepoints;
        public readonly EmojiSequence Emoji; // nullable
        public bool IsEmoji { get => Emoji != null; }
        public OutputToken(int[] cps, EmojiSequence emoji)
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
