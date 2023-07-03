using System.Collections.Generic;
using System.Linq;

namespace adraffy
{
    public class EmojiSequence
    {
        public readonly string Form;
        public readonly IReadOnlyList<int> Beautified;
        public readonly IReadOnlyList<int> Normalized;
        public bool IsMangled { get => Beautified != Normalized; }
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
