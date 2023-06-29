using System.Text;
using System.Text.Json;

namespace ENS
{
    using EmojiDict = Dictionary<int, EmojiNode>;

    internal class EmojiNode
    {
        internal EmojiSequence Emoji;
        internal EmojiDict Dict;
        internal EmojiNode Then(int cp)
        {
            if (Dict == null)
            {
                Dict = new();
            }
            if (Dict.TryGetValue(cp, out EmojiNode node))
            {
                return node;
            }
            return Dict[cp] = new();
        }
    }

    internal class Whole
    {
        internal readonly int[] Valid;
        internal readonly int[] Confused;
        internal readonly int[] Union;
        internal readonly Dictionary<int, int[]> Complement = new();
        internal Whole() { }
        internal Whole(int[] valid, int[] confused)
        {
            Valid = valid;
            Confused = confused;
            HashSet<int> union = new(confused);
            union.UnionWith(valid);
            Union = union.ToArray();
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
        public readonly string UnicodeVersion;
        public readonly string CLDRVersion;
        public readonly int NonSpacingMarkMax;
        public readonly List<EmojiSequence> Emoji = new();
        public readonly int[] Ignored;
        public readonly int[] CombiningMarks;
        public readonly int[] NonSpacingMarks;
        public readonly int[] ShouldEscape;
        public readonly Dictionary<int, int[]> Mapped = new();
        public readonly Dictionary<int, string> Fenced = new();

        public readonly List<Group> Groups = new();
        public readonly int[] PossiblyValid;

        private readonly EmojiNode EmojiRoot = new();
        private readonly Dictionary<int, Whole> Wholes = new();
        private readonly Whole UNIQUE_PH = new();

        public ENSIP15(NF nf, Stream stream)
        {
            NF = nf;
            JsonDocument json = JsonDocument.Parse(stream);

            UnicodeVersion = json.RootElement.GetProperty("unicode").GetString()!.Split(' ', 2)[0];
            CLDRVersion = json.RootElement.GetProperty("cldr").GetString()!.Split(' ', 2)[0];

            ShouldEscape = json.RootElement.GetProperty("escape").ToIntList().ToArray();
            Ignored = json.RootElement.GetProperty("ignored").ToIntList().ToArray();
            CombiningMarks = json.RootElement.GetProperty("cm").ToIntList().ToArray();
            NonSpacingMarks = json.RootElement.GetProperty("nsm").ToIntList().ToArray();
            NonSpacingMarkMax = json.RootElement.GetProperty("nsm_max").GetInt32();

            foreach (JsonElement x in json.RootElement.GetProperty("mapped").EnumerateArray())
            {
                Mapped.Add(x[0].GetInt32(), x[1].ToIntList().ToArray());
            }
            foreach (JsonElement x in json.RootElement.GetProperty("emoji").EnumerateArray())
            {
                AddEmoji(x.ToIntList().ToArray());
            }
            foreach (JsonElement x in json.RootElement.GetProperty("fenced").EnumerateArray())
            {
                Fenced.Add(x[0].GetInt32(), x[1].GetString()!);
            }
            foreach (JsonElement x in json.RootElement.GetProperty("groups").EnumerateArray())
            {
                Groups.Add(new(Groups.Count, x));
            }
            foreach (JsonElement x in json.RootElement.GetProperty("wholes").EnumerateArray())
            {
                int[] valid = x.GetProperty("valid").ToIntList().ToArray();
                int[] confused = x.GetProperty("confused").ToIntList().ToArray();
                Whole w = new(valid, confused);
                foreach (int cp in confused)
                {
                    Wholes.Add(cp, w);
                }
                HashSet<Group> outer = new();
                List<Extent> extents = new();
                foreach (int cp in w.Union)
                {
                    Group[] gs = Groups.Where(g => g.Valid.AssumeSortedContains(cp)).ToArray();
                    Extent extent = extents.Find(e => gs.Any(g => e.Groups.Contains(g)));
                    if (extent == null)
                    {
                        extent = new();
                        extents.Add(extent);
                    }
                    extent.Chars.Add(cp);
                    extent.Groups.UnionWith(gs);
                    outer.UnionWith(gs);
                }
                foreach (Extent extent in extents)  {
                    int[] complement = outer.Except(extent.Groups).Select(g => g.Index).Order().ToArray();
                    foreach (int cp in extent.Chars)
                    {
                        w.Complement.Add(cp, complement);
                    }
                }
            }

            HashSet<int> union = new();
            HashSet<int> multi = new();
            foreach (Group g in Groups)
            {
                foreach (int cp in g.Valid)
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
            HashSet<int> unique = new(union);
            unique.ExceptWith(multi);
            unique.ExceptWith(Wholes.Keys);
            foreach (int cp in unique)
            {
                Wholes.Add(cp, UNIQUE_PH);
            }
            union.UnionWith(NF.NFD(union));
            PossiblyValid = union.Order().ToArray();
        }

        private void AddEmoji(int[] cps)
        {
            EmojiSequence emoji = new(cps);
            Emoji.Add(emoji);

            List<EmojiNode> nodes = new() { EmojiRoot };
            foreach (int cp in cps)
            {
                if (cp == 0xFE0F)
                {
                    nodes.AddRange(nodes.ConvertAll(x => x.Then(cp)));
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
        // reset LTR context
        static string ResetBidi(string s)
        {
            return $"{s}\u200E";
        }
        // format as {HEX}
        static string HexEscape(int cp)
        {
            return $"{{{cp.ToString("X")}}}";
        }

        public string SafeCodepoint(int cp)
        {
            if (ShouldEscape.AssumeSortedContains(cp))
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
            StringBuilder sb = new(cps.Count + 16);
            if (cps.Any() && CombiningMarks.AssumeSortedContains(cps[0]))
            {
                sb.AppendCodepoint(0x25CC);
            }
            foreach (int cp in cps)
            {
                if (ShouldEscape.AssumeSortedContains(cp))
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

        public string Normalize(string name)
        {
            StringBuilder sb = new(name.Length << 1);
            foreach (string label in name.Split(STOP_CH))
            {
                List<int> cps = label.Explode();
                try
                {
                    List<OutputToken> tokens = OutputTokenize(cps, NF.NFC, e => e.Normalized);
                    int[] norm = NormalizeLabel(tokens, out _, out _);
                    if (sb.Length > 0) sb.Append(STOP_CH);
                    sb.AppendCodepoints(norm);
                }
                catch (NormException e)
                {
                    throw new InvalidLabelException(label, $"Invalid label \"{SafeImplode(cps)}\": {e.Message}", e);
                }
            }
            return sb.ToString();
        }

        public string Beautify(string name)
        {
            StringBuilder sb = new(name.Length << 1);
            foreach (string label in name.Split(STOP_CH))
            {
                List<int> cps = label.Explode();
                try
                {
                    List<OutputToken> tokens = OutputTokenize(cps, NF.NFC, e => e.Beautified);
                    int[] norm = NormalizeLabel(tokens, out string kind, out _);
                    if (sb.Length > 0) sb.Append(STOP_CH);
                    if (kind != "Greek")
                    {
                        for (int i = 0, e = norm.Length; i < e; i++)
                        {
                            if (norm[i] == 0x3BE)
                            {
                                norm[i] = 0x39E;
                            }
                        }
                    }
                    sb.AppendCodepoints(norm);
                }
                catch (NormException err)
                {
                    throw new InvalidLabelException(label, $"Invalid label \"{SafeImplode(cps)}\": {err.Message}", err);
                }
            }
            return sb.ToString();
        }


        public int[] NormalizeLabel(IReadOnlyList<OutputToken> tokens, out string kind, out bool hasEmoji)
        {
            if (!tokens.Any())
            {
                throw new NormException("empty label");
            }
            hasEmoji = tokens.Count > 1 || tokens[0].IsEmoji;
            if (!hasEmoji)
            {
                int[] cps = tokens[0].cps;
                if (cps.All(cp => cp < 0x80))
                {
                    CheckLabelExtension(cps);
                    CheckLeadingUnderscore(cps);
                    kind = "ASCII";
                    return cps;
                }
            }
            int[] norm = tokens.SelectMany(t => t.cps).ToArray();
            CheckLeadingUnderscore(norm);
            int[] chars = tokens.Where(t => !t.IsEmoji).SelectMany(x => x.cps).ToArray();
            if (hasEmoji && !chars.Any())
            {
                kind = "Emoji";
                return norm;                
            }
            if (CombiningMarks.AssumeSortedContains(chars[0]))
            {
                throw new NormException("leading combining mark", SafeCodepoint(chars[0]));
            }
            for (int i = 1, e = tokens.Count; i < e; i++)
            {
                OutputToken t = tokens[i];
                if (!t.IsEmoji && CombiningMarks.AssumeSortedContains(t.cps[0]))
                {
                    throw new NormException("emoji + combining mark", $"{tokens[i-1].cps.Implode()} + {SafeCodepoint(t.cps[0])}");
                }
            }
            CheckFenced(norm);
            int[] unique = chars.Distinct().ToArray();
            Group g = DetermineGroup(unique)[0];
            CheckGroup(g, chars);
            CheckWhole(g, unique);
            kind = g.Kind;
            return norm;
        }

        IReadOnlyList<Group> DetermineGroup(IReadOnlyList<int> unique)
        {
            IReadOnlyList<Group> prev = Groups;
            foreach (int cp in unique) {
                Group[] next = prev.Where(g => g.Valid.AssumeSortedContains(cp)).ToArray();
                if (!next.Any())
                {   
                    if (prev == Groups)
                    {
                        throw new DisallowedCharacterException(SafeCodepoint(cp), cp);
                    }
                    else
                    {
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
                if (!g.Valid.AssumeSortedContains(cp))
                {
                    throw CreateMixtureException(g, cp);
                }
            }
            if (!g.CMWhitelisted)
            {
                List<int> decomposed = NF.NFD(cps);
                for (int i = 1, e = decomposed.Count; i < e; i++)
                {
                    if (NonSpacingMarks.AssumeSortedContains(decomposed[i]))
                    {
                        int j = i + 1;
                        for (int cp; j < e && NonSpacingMarks.AssumeSortedContains(cp = decomposed[j]); j++)
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
                        if (n > NonSpacingMarkMax) {
                            throw new NormException("excessive non-spacing marks", $"{ResetBidi(SafeImplode(decomposed.GetRange(i - 1, n)))} ({n}/${NonSpacingMarkMax})");
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
                if (!Wholes.TryGetValue(cp, out Whole w))
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
                            if (comp.AssumeSortedContains(maker[i]))
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
                    if (shared.All(group.Valid.AssumeSortedContains))
                    {
                        throw new ConfusableException("whole-script confusable", g, group);
                    }
                }
            }
        }

        public EmojiSequence FindEmoji(IReadOnlyList<int> cps, ref int index)
        {
            EmojiNode node = EmojiRoot;
            EmojiSequence last = null;
            int i = index;
            while (i < cps.Count)
            {
                int cp = cps[i++];
                if (node.Dict == null || !node.Dict.TryGetValue(cp, out node))
                {
                    break;
                }
                if (node.Emoji != null)
                {
                    index = i;
                    last = node.Emoji;
                }
            }
            return last;
        }

        public List<OutputToken> OutputTokenize(List<int> cps, Func<List<int>, List<int>> nf, Func<EmojiSequence, int[]> emojiStyler)
        {
            List<OutputToken> tokens = new();
            List<int> buf = new(cps.Count);
            for (int i = 0, e = cps.Count; i < e; )
            {
                EmojiSequence emoji = FindEmoji(cps, ref i);
                if (emoji != null)
                {
                    if (buf.Any())
                    {
                        tokens.Add(new OutputToken(false, nf(buf).ToArray()));
                        buf.Clear();
                    }
                    tokens.Add(new OutputToken(true, emojiStyler(emoji)));
                }
                else
                {
                    int cp = cps[i++];
                    if (PossiblyValid.AssumeSortedContains(cp))
                    {
                        buf.Add(cp);
                    }
                    else if (Mapped.TryGetValue(cp, out var mapped))
                    {
                        buf.AddRange(mapped);
                    }
                    else if (!Ignored.AssumeSortedContains(cp))
                    {
                        throw new DisallowedCharacterException(SafeCodepoint(cp), cp);
                    }
                }
            }
            if (buf.Any())
            {
                tokens.Add(new OutputToken(false, nf(buf).ToArray()));
            }
            return tokens;
        }


        void CheckFenced(IReadOnlyList<int> cps)
        {
            if (Fenced.TryGetValue(cps[0], out string name))
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
            Group other = Groups.Find(x => x.Primary.AssumeSortedContains(cp));
            if (other != null)
            {
                conflict = $"{other.Kind} {conflict}";
            }
            return new IllegalMixtureException($"{g.Kind} + {conflict}", g, cp, other);
        }

        
    }

}