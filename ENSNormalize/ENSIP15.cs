﻿using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ADRaffy.ENSNormalize
{
    internal class EmojiNode
    {
        internal EmojiSequence? Emoji;
        internal Dictionary<int, EmojiNode>? Dict;
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
        public readonly ReadOnlyIntSet ShouldEscape;
        public readonly ReadOnlyIntSet Ignored;
        public readonly ReadOnlyIntSet CombiningMarks;
        public readonly ReadOnlyIntSet NonSpacingMarks;
        public readonly ReadOnlyIntSet NFCCheck;
        public readonly ReadOnlyIntSet PossiblyValid;
        public readonly IDictionary<int, string> Fenced;
        public readonly IDictionary<int, ReadOnlyCollection<int>> Mapped;
        public readonly ReadOnlyCollection<Group> Groups;
        public readonly ReadOnlyCollection<EmojiSequence> Emojis;
        public readonly ReadOnlyCollection<Whole> Wholes;

        private readonly EmojiNode EmojiRoot = new();
        private readonly Dictionary<int, Whole> Confusables = new();
        private readonly Whole UNIQUE_PH = new(ReadOnlyIntSet.EMPTY, ReadOnlyIntSet.EMPTY);
        private readonly Group LATIN, GREEK, ASCII, EMOJI;

        // experimental
        private readonly string[] POSSIBLY_CONFUSING = new string[] { "ą", "ç", "ę", "ş", "ì", "í", "î", "ï", "ǐ", "ł" };

        static Dictionary<int, ReadOnlyCollection<int>> DecodeMapped(Decoder dec)
        {
            Dictionary<int, ReadOnlyCollection<int>> ret = new();
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
                for (int i = 0; i < n; i++) ret.Add(keys[i], new(m[i]));
            }
            return ret;
        }

        static Dictionary<int, string> DecodeNamedCodepoints(Decoder dec)
        {
            Dictionary<int, string> ret = new();
            foreach (int cp in dec.ReadSortedAscending(dec.ReadUnsigned()))
            {
                ret.Add(cp, dec.ReadString());
            }
            return ret;
        }

        static IDictionary<K, V> AsReadOnlyDict<K, V>(Dictionary<K, V> dict) where K: notnull 
        {
#if NETSTANDARD1_1 || NET35
            return dict; // pls no bully
#else
            return new ReadOnlyDictionary<K,V>(dict);
#endif
        }

        static List<Group> DecodeGroups(Decoder dec)
        {
            List<Group> ret = new();
            while (true)
            {
                string name = dec.ReadString();
                if (name.Length == 0) break;
                int bits = dec.ReadUnsigned();
                GroupKind kind = (bits & 1) != 0 ? GroupKind.Restricted : GroupKind.Script;
                bool cm = (bits & 2) != 0;
                ret.Add(new(ret.Count, kind, name, cm, new(dec.ReadUnique()), new(dec.ReadUnique())));
            }
            return ret;
        }

        public ENSIP15(NF nf, Decoder dec)
        {
            NF = nf;
            ShouldEscape = new(dec.ReadUnique());
            Ignored = new(dec.ReadUnique());
            CombiningMarks = new(dec.ReadUnique());
            MaxNonSpacingMarks = dec.ReadUnsigned();
            NonSpacingMarks = new(dec.ReadUnique());
            NFCCheck = new(dec.ReadUnique());
            Fenced = AsReadOnlyDict(DecodeNamedCodepoints(dec));
            Mapped = AsReadOnlyDict(DecodeMapped(dec));
            Groups = new(DecodeGroups(dec));
            Emojis = new(dec.ReadTree().Select(cps => new EmojiSequence(cps)).ToArray());

            // precompute: confusable extent complements
            List<Whole> wholes = new();
            while (true)
            {
                ReadOnlyIntSet confused = new(dec.ReadUnique());
                if (confused.Count == 0) break;
                ReadOnlyIntSet valid = new(dec.ReadUnique());
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
                    Extent? extent = extents.FirstOrDefault(e => gs.Any(g => e.Groups.Contains(g)));
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
            Wholes = new(wholes);

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
            PossiblyValid = new(union.Union(NF.NFD(union)));

            // precompute: unique non-confusables
            HashSet<int> unique = new(union);
            unique.ExceptWith(multi);
            unique.ExceptWith(Confusables.Keys);
            foreach (int cp in unique)
            {
                Confusables.Add(cp, UNIQUE_PH);
            }

            // precompute: special groups
            LATIN = Groups.First(g => g.Name == "Latin");
            GREEK = Groups.First(g => g.Name == "Greek");
            ASCII = new(-1, GroupKind.ASCII, "ASCII", false, new(PossiblyValid.Where(cp => cp < 0x80)), ReadOnlyIntSet.EMPTY);
            EMOJI = new(-1, GroupKind.Emoji, "Emoji", false, ReadOnlyIntSet.EMPTY, ReadOnlyIntSet.EMPTY);
        }
        
        // format as {HEX}
        static string HexEscape(int cp)
        {
            return $"{{{cp.ToHex()}}}";
        }

        // format as "X {HEX}" if possible
        public string SafeCodepoint(int cp)
        {
            return ShouldEscape.Contains(cp) ? HexEscape(cp) : $"\"{SafeImplode(new int[] { cp })}\" {HexEscape(cp)}";
        }
        public string SafeImplode(IList<int> cps)
        {
            int n = cps.Count;
            if (n == 0) return "";
            StringBuilder sb = new(n + 16); // guess
            if (CombiningMarks.Contains(cps[0]))
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
            // some messages can be mixed-directional and result in spillover
            // use 200E after a input string to reset the bidi direction
            // https://www.w3.org/International/questions/qa-bidi-unicode-controls#exceptions
            sb.AppendCodepoint(0x200E);
            return sb.ToString();
        }

        // throws
        public string Normalize(string name)
        {
            return Transform(name, cps => OutputTokenize(cps, NF.NFC, e => e.Normalized), tokens => {
                int[] norm = tokens.SelectMany(t => t.Codepoints).ToArray();
                CheckValid(norm, tokens);
                return norm;
            });
        }
        // throws
        public string Beautify(string name)
        {
            return Transform(name, cps => OutputTokenize(cps, NF.NFC, e => e.Beautified), tokens => {
                int[] norm = tokens.SelectMany(t => t.Codepoints).ToArray();
                Group group = CheckValid(norm, tokens);
                if (group != GREEK)
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
            return Transform(name, cps => OutputTokenize(cps, decompose ? NF.NFD : NF.NFC, e => e.Normalized), tokens => {
                return tokens.SelectMany(t => t.Codepoints);
            });
        }

        string Transform(string name, Func<List<int>, IList<OutputToken>> tokenizer, Func<IList<OutputToken>, IEnumerable<int>> fn)
        {
            if (name.Length == 0) return ""; // empty name allowance
            StringBuilder sb = new(name.Length + 16); // guess
            string[] labels = name.Split(STOP_CH);
            foreach (string label in labels)
            {
                List<int> cps = label.Explode();
                try
                {
                    IList<OutputToken> tokens = tokenizer(cps);
                    if (sb.Length > 0) sb.Append(STOP_CH);
                    sb.AppendCodepoints(fn(tokens));
                }
                catch (NormException e)
                {
                    throw new InvalidLabelException(label, $"Invalid label \"{SafeImplode(cps)}\": {e.Message}", e);
                }
            }
            return sb.ToString();
        }

        // never throws
        public IList<Label> Split(string name)
        {
            string[] labels = name.Split(STOP_CH);
            List<Label> ret = new(labels.Length);
            if (name.Length == 0) return ret; // empty name allowance
            foreach (string label in labels)
            {
                List<int> cps = label.Explode();
                IList<OutputToken>? tokens = null;
                try
                {
                    tokens = OutputTokenize(cps, NF.NFC, e => e.Normalized.ToList()); // make copy
                    int[] norm = tokens.SelectMany(t => t.Codepoints).ToArray();
                    Group group = CheckValid(norm, tokens);
                    ret.Add(new(cps, tokens, norm, group));
                }
                catch (NormException e)
                {
                    ret.Add(new(cps, tokens, e));
                }
            }
            return ret;
        }
        // experimental
        // throws
        public NormDetails NormalizeDetails(string name)
        {
            HashSet<Group> groups = new();
            HashSet<EmojiSequence> emojis = new();
            string norm = Transform(name, cps => OutputTokenize(cps, NF.NFC, e => e.Normalized), tokens => {
                int[] norm = tokens.SelectMany(t => t.Codepoints).ToArray();
                Group group = CheckValid(norm, tokens);
                emojis.UnionWith(tokens.Where(t => t.IsEmoji).Select(t => t.Emoji!));
                if (group == LATIN && tokens.All(t => t.IsEmoji || t.Codepoints.All(cp => cp < 0x80)))
                {
                    group = ASCII;
                }
                groups.Add(group);
                return norm;
            });
            if (groups.Contains(LATIN))
            {
                groups.Remove(ASCII);
            }
            if (emojis.Count > 0)
            {
                groups.Add(EMOJI);
            }
            bool confusing = POSSIBLY_CONFUSING.Any(norm.Contains);
            return new(norm, groups, emojis, confusing);
        }

        Group CheckValid(int[] norm, IList<OutputToken> tokens)
        {
            if (norm.Length == 0)  
            {
                throw new NormException("empty label");
            }
            CheckLeadingUnderscore(norm);
            bool emoji = tokens.Count > 1 || tokens[0].IsEmoji;
            if (!emoji && norm.All(cp => cp < 0x80))
            {
                CheckLabelExtension(norm);
                return ASCII;
            }
            int[] chars = tokens.Where(t => !t.IsEmoji).SelectMany(x => x.Codepoints).ToArray();
            if (emoji && chars.Length == 0)
            {
                return EMOJI;
            }
            CheckCombiningMarks(tokens);
            CheckFenced(norm);
            int[] unique = chars.Distinct().ToArray();
            Group group = DetermineGroup(unique);
            CheckGroup(group, chars); // need text in order
            CheckWhole(group, unique); // only need unique text
            return group;
        }

        // assume: Groups.length > 1
        Group DetermineGroup(int[] unique)
        {
            Group[] gs = Groups.ToArray();
            int prev = gs.Length;
            foreach (int cp in unique) {
                int next = 0;
                for (int i = 0; i < prev; i++)
                {
                    if (gs[i].Contains(cp))
                    {
                        gs[next++] = gs[i];
                    }
                }
                if (next == 0)
                {   
                    if (!Groups.Any(g => g.Contains(cp)))
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
                        throw CreateMixtureException(gs[0], cp);
                    }
                }
                prev = next;
                if (prev == 1) break; // there is only one group left
            }
            return gs[0];
        }

        // assume: cps.length > 0
        // assume: cps[0] isn't CM
        void CheckGroup(Group g, int[] cps)
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
                    // https://www.unicode.org/reports/tr39/#Optional_Detection
                    if (NonSpacingMarks.Contains(decomposed[i]))
                    {
                        int j = i + 1;
                        for (int cp; j < e && NonSpacingMarks.Contains(cp = decomposed[j]); j++)
                        {
                            for (int k = i; k < j; k++)
                            {
                                // a. Forbid sequences of the same nonspacing mark.
                                if (decomposed[k] == cp)
                                {
                                    throw new NormException("duplicate non-spacing marks", SafeCodepoint(cp));
                                }
                            }
                        }
                        // b. Forbid sequences of more than 4 nonspacing marks (gc=Mn or gc=Me).
                        int n = j - i;
                        if (n > MaxNonSpacingMarks) {
                            throw new NormException("excessive non-spacing marks", $"{SafeImplode(decomposed.GetRange(i - 1, n))} ({n}/${MaxNonSpacingMarks})");
				        }
				        i = j;
                    }
                }
            }
        }

        void CheckWhole(Group g, int[] unique)
        {
            int bound = 0;
            int[]? maker = null;
            List<int> shared = new();
            foreach (int cp in unique)
            {
                if (!Confusables.TryGetValue(cp, out var w))
                {
                    shared.Add(cp);
                } 
                else if (w == UNIQUE_PH)
                {
                    return; // unique, non-confusable
                }
                else 
                {
                    int[] comp = w.Complement[cp]; // exists by construction
                    if (bound == 0)
                    {
                        maker = comp.ToArray(); // non-empty
                        bound = comp.Length; 
                    }
                    else // intersect(comp, maker)
                    {
                        int b = 0;
                        for (int i = 0; i < bound; i++)
                        {
                            if (comp.Contains(maker![i]))
                            {
                                if (i > b) maker[b] = maker[i];
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
                    Group group = Groups[maker![i]];
                    if (shared.All(group.Contains))
                    {
                        throw new ConfusableException(g, group);
                    }
                }
            }
        }

        // find the longest emoji that matches at index
        // if found, returns and updates the index
        EmojiSequence? FindEmoji(List<int> cps, ref int index)
        {
            EmojiNode? node = EmojiRoot;
            EmojiSequence? last = null;
            for (int i = index, e = cps.Count; i < e; )
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

        IList<OutputToken> OutputTokenize(List<int> cps, Func<List<int>, List<int>> nf, Func<EmojiSequence, IList<int>> emojiStyler)
        {
            List<OutputToken> tokens = new();
            int n = cps.Count;
            List<int> buf = new(n);
            for (int i = 0; i < n; )
            {
                EmojiSequence? emoji = FindEmoji(cps, ref i);
                if (emoji != null) // found an emoji
                {
                    if (buf.Count > 0) // consume buffered
                    {
                        tokens.Add(new(nf(buf)));
                        buf.Clear();
                    }
                    tokens.Add(new(emojiStyler(emoji), emoji)); // add emoji
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
            if (buf.Count > 0) // flush buffered
            {
                tokens.Add(new(nf(buf)));
            }
            return tokens;
        }
        // assume: cps.length > 0
        void CheckFenced(int[] cps)
        {
            if (Fenced.TryGetValue(cps[0], out var name))
            {
                throw new NormException("leading fenced", name);
            }
            int n = cps.Length;
            int last = -1;
            string prev = "";
            for (int i = 1; i < n; i++)
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
            if (last == n)
            {
                throw new NormException("trailing fenced", prev);
            }
        }
        void CheckCombiningMarks(IList<OutputToken> tokens)
        {
            for (int i = 0, e = tokens.Count; i < e; i++)
            {
                OutputToken t = tokens[i];
                if (t.IsEmoji) continue;
                int cp = t.Codepoints[0];
                if (CombiningMarks.Contains(cp))
                {
                    if (i == 0)
                    {
                        throw new NormException("leading combining mark", SafeCodepoint(cp));
                    }
                    else 
                    {
                        // note: the previous token must an EmojiSequence
                        throw new NormException("emoji + combining mark", $"{tokens[i - 1].Emoji!.Form} + {SafeCodepoint(cp)}");
                    }
                }
            }
        }
        // assume: ascii
        static void CheckLabelExtension(int[] cps)
        {
            const int HYPHEN = 0x2D;
            if (cps.Length >= 4 && cps[2] == HYPHEN && cps[3] == HYPHEN)
            {
                throw new NormException("invalid label extension", cps.Take(4).Implode());
            }
        }
        static void CheckLeadingUnderscore(int[] cps)
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
            Group? other = Groups.FirstOrDefault(x => x.Primary.Contains(cp));
            if (other != null)
            {
                conflict = $"{other} {conflict}";
            }
            return new IllegalMixtureException($"{g} + {conflict}", cp, g, other);
        }
    }

}