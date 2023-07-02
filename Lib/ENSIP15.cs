using System.Text;
using System.Collections.Generic;
using System.Linq;
using System;

namespace adraffy
{
    internal class EmojiNode
    {
        internal EmojiSequence Emoji;
        internal Dictionary<int, EmojiNode> Dict;
        internal EmojiNode Then(int cp)
        {
            Dict ??= new();
            if (Dict.TryGetValue(cp, out var node)) return node;
            return Dict[cp] = new();
        }
    }

    internal class Extent
    {
        internal readonly HashSet<Group> Groups = new();
        internal readonly List<int> Chars = new();
    }

    public class ENSIP15
    {
        const char STOP_CH = '.';

        public readonly NF NF;
        public readonly int MaxNonSpacingMarks;
        public readonly IReadOnlyList<EmojiSequence> Emojis;

        public readonly IReadOnlyCollection<int> Ignored;
        public readonly IReadOnlyCollection<int> CombiningMarks;
        public readonly IReadOnlyCollection<int> NonSpacingMarks;
        public readonly IReadOnlyCollection<int> ShouldEscape;
        public readonly IReadOnlyCollection<int> NFCCheck;
        public readonly IReadOnlyCollection<int> PossiblyValid;
        public readonly IReadOnlyDictionary<int, IReadOnlyList<int>> Mapped;
        public readonly IReadOnlyDictionary<int, string> Fenced;
        public readonly IReadOnlyList<Group> Groups;
        public readonly IReadOnlyList<Whole> Wholes;

        private readonly EmojiNode EmojiRoot = new();
        private readonly Dictionary<int, Whole> Confusables = new();
        private readonly Whole UNIQUE_PH = new();
        private readonly Group GREEK;

        static Dictionary<int, IReadOnlyList<int>> DecodeMapped(Decoder dec)
        {
            Dictionary<int, IReadOnlyList<int>> ret = new();
            while (true)
            {
                int w = dec.ReadUnsigned();
                if (w == 0) break;
                int[] keys = dec.ReadSortedUnique();
                int n = keys.Length;
                List<List<int>> m = new();
                for (int i = 0; i < n; i++) m.Add(new());
                for (int j = 0; j < w; j++)
                {
                    int[] v = dec.ReadUnsortedDeltas(n);
                    for (int i = 0; i < n; i++) m[i].Add(v[i]);
                }
                for (int i = 0; i < n; i++) ret.Add(keys[i], m[i].ToArray());
            }
            return ret;
        }

        static Dictionary<int, string> DecodeNamedCodepoints(Decoder dec)
        {
            Dictionary<int, string> ret = new();
            int n = dec.ReadUnsigned();
            foreach (int cp in dec.ReadSortedAscending(n))
            {
                ret.Add(cp, dec.ReadString());
            }
            return ret;
        }

        static Group[] DecodeGroups(Decoder dec)
        {
            List<Group> ret = new();
            while (true)
            {
                string name = dec.ReadString();
                if (name.Length == 0) break;
                int bits = dec.ReadUnsigned();
                bool restricted = (bits & 1) > 0;
                bool cm = (bits & 2) > 0;
                ret.Add(new(ret.Count, name, restricted, cm, dec.ReadUnique(), dec.ReadUnique()));
            }
            return ret.ToArray();
        }

        public ENSIP15(NF nf, Decoder dec)
        {
            NF = nf;
            ShouldEscape = (IReadOnlyCollection<int>)dec.ReadSet();
            Ignored = (IReadOnlyCollection<int>)dec.ReadSet();
            CombiningMarks = (IReadOnlyCollection<int>)dec.ReadSet();
            MaxNonSpacingMarks = dec.ReadUnsigned();
            NonSpacingMarks = (IReadOnlyCollection<int>)dec.ReadSet();
            NFCCheck = (IReadOnlyCollection<int>)dec.ReadSet();
            Fenced = DecodeNamedCodepoints(dec);
            Mapped = DecodeMapped(dec);
            Groups = DecodeGroups(dec);
            Emojis = dec.ReadTree().Select(cps => new EmojiSequence(cps)).ToList();

            // precompute: greek
            GREEK = Groups.FirstOrDefault(g => g.Name == "Greek");

            // precompute: confusable extent complements
            List<Whole> wholes = new();
            while (true)
            {
                List<int> confused = dec.ReadUnique();
                if (!confused.Any()) break;
                List<int> valid = dec.ReadUnique();
                Whole w = new(valid, confused);
                wholes.Add(w);
                foreach (int cp in confused)
                {
                    Confusables.Add(cp, w);
                }
                HashSet<Group> groups = new();
                List<Extent> extents = new();
                foreach (int cp in confused.Concat(valid))
                {
                    Group[] gs = Groups.Where(g => g.Contains(cp)).ToArray();
                    Extent extent = extents.Find(e => gs.Any(g => e.Groups.Contains(g)));
                    if (extent == null)
                    {
                        extent = new();
                        extents.Add(extent);
                    }
                    extent.Chars.Add(cp);
                    extent.Groups.UnionWith(gs);
                    groups.UnionWith(gs);
                }
                foreach (Extent extent in extents)
                {
                    int[] complement = groups.Except(extent.Groups).Select(g => g.Index).ToArray();
                    Array.Sort(complement);
                    foreach (int cp in extent.Chars)
                    {
                        w.Complement.Add(cp, complement);
                    }
                }
            }
            Wholes = wholes.ToArray();

            // precompute: emoji trie
            foreach (EmojiSequence emoji in Emojis)
            {
                List<EmojiNode> nodes = new() { EmojiRoot };
                foreach (int cp in emoji.Beautified)
                {
                    if (cp == 0xFE0F)
                    {
                        for (int i = 0, e = nodes.Count; i < e; i++)
                        {
                            nodes.Add(nodes[i].Then(cp));
                        }
                    }
                    else
                    {
                        for (int i = 0, e = nodes.Count; i < e; i++)
                        {
                            nodes[i] = nodes[i].Then(cp);
                        }
                    }
                }
                foreach (EmojiNode x in nodes)
                {
                    x.Emoji = emoji;
                }
            }

            // precompute: possibly valid
            HashSet<int> union = new();
            HashSet<int> multi = new();
            foreach (Group g in Groups)
            {
                foreach (int cp in g.Primary.Concat(g.Secondary))
                {
                    if (union.Contains(cp))
                    {
                        multi.Add(cp);
                    }
                    else
                    {
                        union.Add(cp);
                    }
                }
            }
            union.UnionWith(NF.NFD(union));
            PossiblyValid = (IReadOnlyCollection<int>)union;

            // precompute: unique non-confusables
            HashSet<int> unique = new(union);
            unique.ExceptWith(multi);
            unique.ExceptWith(Confusables.Keys);
            foreach (int cp in unique)
            {
                Confusables.Add(cp, UNIQUE_PH);
            }
        }
    
        // reset LTR context
        static string ResetBidi(string s)
        {
            return $"{s}\u200E";
        }
        // format as {HEX}
        static string HexEscape(int cp)
        {
            return $"{{{cp:X}}}";
        }

        // format as "X {HEX}" if possible
        public string SafeCodepoint(int cp)
        {
            if (ShouldEscape.Contains(cp))
            {
                return HexEscape(cp);
            }
            else
            {
                return $"{ResetBidi(SafeImplode(new int[] { cp }))} {HexEscape(cp)}";
            }
        }

        // assume: cps.length > 0
        public string SafeImplode(IReadOnlyList<int> cps)
        {
            StringBuilder sb = new(cps.Count + 16); // guess
            if (cps.Any() && CombiningMarks.Contains(cps[0]))
            {
                sb.AppendCodepoint(0x25CC);
            }
            foreach (int cp in cps)
            {
                if (ShouldEscape.Contains(cp))
                {
                    sb.Append(HexEscape(cp));
                }
                else
                {
                    sb.AppendCodepoint(cp);
                }
            }
            return sb.ToString();
        }

        // throws
        public string Normalize(string name)
        {
            return Transform(name, cps => NormalizedLabelFromTokens(OutputTokenize(cps, NF.NFC, e => e.Normalized), out _));
        }
        // throws
        public string Beautify(string name)
        {
            return Transform(name, cps => {
                int[] norm = NormalizedLabelFromTokens(OutputTokenize(cps, NF.NFC, e => e.Beautified), out var g);
                if (g != GREEK)
                {
                    for (int i = 0, e = norm.Length; i < e; i++)
                    {
                        // ξ => Ξ if not greek
                        if (norm[i] == 0x3BE) norm[i] = 0x39E;
                    }
                }
                return norm;
            });
        }
        // only throws InvalidLabelException w/DisallowedCharacterException
        public string NormalizeFragment(string name, bool decompose = false)
        {
            return Transform(name, cps => OutputTokenize(cps, decompose ? NF.NFD : NF.NFC, e => e.Normalized).SelectMany(t => t.Codepoints));
        }

        string Transform(string name, Func<int[], IEnumerable<int>> fn)
        {
            StringBuilder sb = new(name.Length + 16);
            foreach (string label in name.Split(STOP_CH))
            {
                int[] cps = label.Explode().ToArray();
                try
                {
                    if (sb.Length > 0) sb.Append(STOP_CH);
                    sb.AppendCodepoints(fn(cps));
                }
                catch (NormException e)
                {
                    throw new InvalidLabelException(label, $"Invalid label \"{SafeImplode(cps)}\": {e.Message}", e);
                }
            }
            return sb.ToString();
        }


        // never throws
        public Label[] Split(string name)
        {
            return name.Split(STOP_CH).Select(NormalizeLabel).ToArray();
        }
        public Label NormalizeLabel(string label)
        {
            int[] input = label.Explode().ToArray();
            List<OutputToken> tokens = null;
            try
            {
                tokens = OutputTokenize(input, NF.NFC, e => e.Normalized.ToArray()); // force a copy since we are exposing
                int[] norm = NormalizedLabelFromTokens(tokens, out var g);
                return new Label(input, tokens, norm, g);
            }
            catch (NormException e)
            {
                return new Label(input, tokens, e);
            }
        }

        int[] NormalizedLabelFromTokens(IReadOnlyList<OutputToken> tokens, out Group group)
        {
            if (!tokens.Any())
            {
                throw new NormException("empty label");
            }
            int[] norm = tokens.SelectMany(t => t.Codepoints).ToArray();
            CheckLeadingUnderscore(norm);
            bool emoji = tokens.Count > 1 || tokens[0].IsEmoji;
            if (!emoji && norm.All(cp => cp < 0x80))
            {
                CheckLabelExtension(norm);
                group = Group.ASCII;
                return norm;
            }
            int[] chars = tokens.Where(t => !t.IsEmoji).SelectMany(x => x.Codepoints).ToArray();
            if (emoji && !chars.Any())
            {
                group = Group.EMOJI;
                return norm;
            }
            CheckCombiningMarks(tokens);
            CheckFenced(norm);
            int[] unique = chars.Distinct().ToArray();
            group = DetermineGroup(unique)[0];
            CheckGroup(group, chars);
            CheckWhole(group, unique);
            return norm;
        }

        // assume: Groups.length > 1
        IReadOnlyList<Group> DetermineGroup(IReadOnlyList<int> unique)
        {
            IReadOnlyList<Group> prev = Groups;
            foreach (int cp in unique) {
                Group[] next = prev.Where(g => g.Contains(cp)).ToArray();
                if (!next.Any())
                {   
                    if (prev == Groups)
                    {
                        // the character was composed of valid parts
                        // but it's NFC form is invalid
                        throw new DisallowedCharacterException(SafeCodepoint(cp), cp);
                    }
                    else
                    {
                        // there is no group that contains all these characters
                        // throw using the highest priority group that matched
                        // https://www.unicode.org/reports/tr39/#mixed_script_confusables
                        throw CreateMixtureException(prev[0], cp);
                    }
                }                
                prev = next;
                if (prev.Count == 1) break;
            }
            return prev;
        }

        // assume: cps.length > 0
        // assume: cps[0] isn't CM
        void CheckGroup(Group g, IReadOnlyList<int> cps)
        {
            foreach (int cp in cps)
            {
                if (!g.Contains(cp))
                {
                    throw CreateMixtureException(g, cp);
                }
            }
            if (!g.CMWhitelisted)
            {
                List<int> decomposed = NF.NFD(cps);
                for (int i = 1, e = decomposed.Count; i < e; i++)
                {
                    if (NonSpacingMarks.Contains(decomposed[i]))
                    {
                        int j = i + 1;
                        for (int cp; j < e && NonSpacingMarks.Contains(cp = decomposed[j]); j++)
                        {
                            for (int k = i; k < j; k++)
                            {
                                if (decomposed[k] == cp)
                                {
                                    throw new NormException("duplicate non-spacing marks", SafeCodepoint(cp));
                                }
                            }
                        }
                        int n = j - i;
                        if (n > MaxNonSpacingMarks) {
                            throw new NormException("excessive non-spacing marks", $"{ResetBidi(SafeImplode(decomposed.GetRange(i - 1, n)))} ({n}/${MaxNonSpacingMarks})");
				        }
				        i = j;
                    }
                }
            }
        }

        void CheckWhole(Group g, IReadOnlyList<int> unique)
        {
            int bound = 0;
            int[] maker = null;
            List<int> shared = new();
            foreach (int cp in unique)
            {
                if (!Confusables.TryGetValue(cp, out Whole w))
                {
                    shared.Add(cp);
                } 
                else if (w == UNIQUE_PH)
                {
                    return; // unique, non-confusable
                }
                else 
                {
                    int[] comp = w.Complement[cp];
                    if (bound == 0)
                    {
                        maker = comp.ToArray();
                        bound = comp.Length;
                    }
                    else
                    {
                        int b = 0;
                        for (int i = 0; i < bound; i++)
                        {
                            if (comp.Contains(maker[i]))
                            {
                                if (i > b)
                                {
                                    maker[b] = maker[i];
                                }
                                ++b;
                            }
                        }
                        bound = b;
                    }
                    if (bound == 0)
                    {
                        return; // confusable intersection is empty
                    }
                }
            }
            if (bound > 0)
            {
                for (int i = 0; i < bound; i++)
                {
                    Group group = Groups[maker[i]];
                    if (shared.All(group.Contains))
                    {
                        throw new ConfusableException(g, group);
                    }
                }
            }
        }

        // find the longest emoji that matches at index
        // if found, returns and updates the index
        EmojiSequence FindEmoji(IReadOnlyList<int> cps, ref int index)
        {
            EmojiNode node = EmojiRoot;
            EmojiSequence last = null;
            for (int i = index; i < cps.Count; )
            {
                if (node.Dict == null || !node.Dict.TryGetValue(cps[i++], out node)) break;
                if (node.Emoji != null) // the emoji is valid
                {
                    index = i; // eat the emoji
                    last = node.Emoji; // save it
                }
            }
            return last; // last emoji found
        }

        List<OutputToken> OutputTokenize(IReadOnlyList<int> cps, Func<List<int>, List<int>> nf, Func<EmojiSequence, int[]> emojiStyler)
        {
            List<OutputToken> tokens = new();
            List<int> buf = new(cps.Count);
            for (int i = 0, e = cps.Count; i < e; )
            {
                EmojiSequence emoji = FindEmoji(cps, ref i);
                if (emoji != null) // found an emoji
                {
                    if (buf.Any()) // consume buffered
                    {
                        tokens.Add(new OutputToken(nf(buf).ToArray(), null));
                        buf.Clear();
                    }
                    tokens.Add(new OutputToken(emojiStyler(emoji), emoji)); // add emoji
                }
                else
                {
                    int cp = cps[i++];
                    if (PossiblyValid.Contains(cp))
                    {
                        buf.Add(cp);
                    }
                    else if (Mapped.TryGetValue(cp, out var mapped))
                    {
                        buf.AddRange(mapped);
                    }
                    else if (!Ignored.Contains(cp))
                    {
                        throw new DisallowedCharacterException(SafeCodepoint(cp), cp);
                    }
                }
            }
            if (buf.Any()) // flush buffered
            {
                tokens.Add(new OutputToken(nf(buf).ToArray(), null));
            }
            return tokens;
        }
        // assume: cps.length > 0
        void CheckFenced(IReadOnlyList<int> cps)
        {
            if (Fenced.TryGetValue(cps[0], out var name))
            {
                throw new NormException("leading fenced", name);
            }
            int count = cps.Count;
            int last = -1;
            string prev = "";
            for (int i = 1; i < count; i++)
            {
                if (Fenced.TryGetValue(cps[i], out name))
                {
                    if (last == i)
                    {
                        throw new NormException("adjacent fenced", $"{prev} + {name}");
                    }
                    last = i + 1;
                    prev = name;
                }
            }
            if (last == count)
            {
                throw new NormException($"trailing fenced", prev);
            }
        }
        void CheckCombiningMarks(IReadOnlyList<OutputToken> tokens)
        {
            for (int i = 0, e = tokens.Count; i < e; i++)
            {
                OutputToken t = tokens[i];
                if (!t.IsEmoji && CombiningMarks.Contains(t.Codepoints[0]))
                {
                    if (i == 0)
                    {
                        throw new NormException("leading combining mark", SafeCodepoint(t.Codepoints[0]));
                    }
                    else
                    {
                        throw new NormException("emoji + combining mark", $"{tokens[i - 1].Codepoints.Implode()} + {SafeCodepoint(t.Codepoints[0])}");
                    }
                }
            }
        }
        // assume: ascii
        static void CheckLabelExtension(IReadOnlyList<int> cps)
        {
            const int HYPHEN = 0x2D;
            if (cps.Count >= 4 && cps[2] == HYPHEN && cps[3] == HYPHEN)
            {
                throw new NormException("invalid label extension", cps.Take(4).Implode());
            }
        }
        static void CheckLeadingUnderscore(IReadOnlyList<int> cps)
        {
            const int UNDERSCORE = 0x5F;
            bool allowed = true;
            foreach (int cp in cps)
            {
                if (allowed)
                {
                    if (cp != UNDERSCORE)
                    {
                        allowed = false;
                    }
                } 
                else
                {
                    if (cp == UNDERSCORE)
                    {
                        throw new NormException("underscore allowed only at start");
                    }
                }
            }
        }
        private IllegalMixtureException CreateMixtureException(Group g, int cp)
        {
            string conflict = SafeCodepoint(cp);
            Group other = Groups.FirstOrDefault(x => x.Primary.Contains(cp));
            if (other != null)
            {
                conflict = $"{other.Description} {conflict}";
            }
            return new IllegalMixtureException($"{g.Description} + {conflict}", cp, g, other);
        }
    }

}