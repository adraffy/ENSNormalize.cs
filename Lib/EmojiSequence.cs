using System.Collections.Generic;
using System.Linq;

namespace adraffy
{
    public class EmojiSequence
    {
        public readonly string Form;
        public bool IsMangled { get => Beautified != Normalized; }
        public IReadOnlyList<int> Codepoints { get => Beautified; }
        public int NormalizedLength { get => Normalized.Length; }    

        internal readonly int[] Beautified;
        internal readonly int[] Normalized;
        internal EmojiSequence(int[] cps)
        {
            Beautified = cps;
            Form = cps.Implode();
            int[] norm = cps.Where(cp => cp != 0xFE0F).ToArray();
            Normalized = norm.Length < cps.Length ? norm : cps;
        }
        public override string ToString() 
        {
            return $"Emoji[{Beautified.ToHexSequence()}]";
        }
    }
}
