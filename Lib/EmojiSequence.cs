namespace adraffy
{
    public class EmojiSequence
    {
        public readonly string Form;
        public readonly bool Mangled;
        public IReadOnlyList<int> Codepoints { get => Beautified; }

        internal readonly int[] Beautified;
        internal readonly int[] Normalized;
        internal EmojiSequence(int[] cps)
        {
            Beautified = cps;
            Form = cps.Implode();
            int[] v = cps.Where(cp => cp != 0xFE0F).ToArray();
            Mangled = v.Length != cps.Length;
            Normalized = Mangled ? v : cps;
        }
        public override string ToString() 
        {
            return $"Emoji[{Beautified.ToHexSequence()}]";
        }
    }
}
