using System.Linq;
using System.Collections.ObjectModel;

namespace ADRaffy.ENSNormalize
{
    public class EmojiSequence
    {
        public readonly string Form;
        public readonly ReadOnlyCollection<int> Beautified;
        public readonly ReadOnlyCollection<int> Normalized;
        public bool IsMangled { get => Beautified != Normalized; }
        internal EmojiSequence(int[] cps)
        {
            Beautified = new(cps);
            Form = cps.Implode();
            int[] norm = cps.Where(cp => cp != 0xFE0F).ToArray();
            Normalized = norm.Length < cps.Length ? new(norm) : Beautified;
        }
        public override string ToString() 
        {
            return $"Emoji[{Beautified.ToHexSequence()}]";
        }
    }
}
