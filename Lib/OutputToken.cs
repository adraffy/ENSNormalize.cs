namespace ENS
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
    }
}
