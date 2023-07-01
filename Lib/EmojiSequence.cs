namespace ENS
{
    public class EmojiSequence
    {
        public readonly string Form;
        public readonly int[] Beautified;
        public readonly int[] Normalized;
        public readonly bool Mangled;
        public EmojiSequence(int[] cps)
        {
            Beautified = cps;
            Form = cps.Implode();
            int[] v = cps.Where(cp => cp != 0xFE0F).ToArray();
            Mangled = v.Length != cps.Length;
            Normalized = Mangled ? v : cps;
        }
    }
}
