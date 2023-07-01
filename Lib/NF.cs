namespace adraffy
{
    public class NF
    {
        const int SHIFT = 24;
        const int MASK = (1 << SHIFT) - 1;
        const int NONE = -1;

        const int S0 = 0xAC00;
        const int L0 = 0x1100;
        const int V0 = 0x1161;
        const int T0 = 0x11A7;
        const int L_COUNT = 19;
        const int V_COUNT = 21;
        const int T_COUNT = 28;
        const int N_COUNT = V_COUNT * T_COUNT;
        const int S_COUNT = L_COUNT * N_COUNT;
        const int S1 = S0 + S_COUNT;
        const int L1 = L0 + L_COUNT;
        const int V1 = V0 + V_COUNT;
        const int T1 = T0 + T_COUNT;

        static bool IsHangul(int cp)
        {
            return cp >= S0 && cp < S1;
        }

        static int UnpackCC(int packed)
        {
            return packed >> SHIFT;
        }
        static int UnpackCP(int packed)
        {
            return packed & MASK;
        }

        public readonly string UnicodeVersion;

        private readonly HashSet<int> Exclusions;
        private readonly HashSet<int> QuickCheck;
        private readonly Dictionary<int, int> Rank = new();
        private readonly Dictionary<int, int[]> Decomp = new();
        private readonly Dictionary<int, Dictionary<int, int>> Recomp = new();

        public NF(Decoder dec)
        {
            UnicodeVersion = dec.ReadString();
            Exclusions = new(dec.ReadUnique());
            QuickCheck = new(dec.ReadUnique());
            int[] decomp1 = dec.ReadSortedUnique();
            int[] decomp1A = dec.ReadUnsortedDeltas(decomp1.Length);
            for (int i = 0; i < decomp1.Length; i++)
            {
                Decomp.Add(decomp1[i], new int[] { decomp1A[i] });
            };
            int[] decomp2 = dec.ReadSortedUnique();
            int[] decomp2A = dec.ReadUnsortedDeltas(decomp2.Length);
            int[] decomp2B = dec.ReadUnsortedDeltas(decomp2.Length);
            for (int i = 0; i < decomp2.Length; i++)
            {
                int cp = decomp2[i];
                int cpA = decomp2A[i];
                int cpB = decomp2B[i];
                Decomp.Add(cp, new int[] { cpB, cpA }); // reversed
                if (!Exclusions.Contains(cp))
                {
                    if (!Recomp.TryGetValue(cpA, out var recomp))
                    {
                        recomp = new();
                        Recomp.Add(cpA, recomp);
                    }
                    recomp.Add(cpB, cp);
                }
            }
            for (int rank = 0; ; )
            {
                rank += 1 << SHIFT;
                List<int> v = dec.ReadUnique();
                if (!v.Any()) break;
                foreach (int cp in v)
                {
                    Rank.Add(cp, rank);
                }
            }
        }
        int ComposePair(int a, int b)
        {
            if (a >= L0 && a < L1 && b >= V0 && b < V1)
            {
                return S0 + (a - L0) * N_COUNT + (b - V0) * T_COUNT;
            }
            else if (IsHangul(a) && b > T0 && b < T1 && (a - S0) % T_COUNT == 0)
            {
                return a + (b - T0);
            }
            else
            {
                if (Recomp.TryGetValue(a, out var recomp))
                {
                    if (recomp.TryGetValue(b, out var cp))
                    {
                        return cp;
                    }
                }
                return NONE;
            }
        }

        internal class Packer
        {
            readonly NF NF;
            bool CheckOrder = false;
            internal List<int> Packed = new();
            internal Packer(NF nf)
            {
                NF = nf;
            }
            internal void Add(int cp)
            {
                if (NF.Rank.TryGetValue(cp, out var rank))
                {
                    CheckOrder = true;
                    cp |= rank;
                }
                Packed.Add(cp);
            }
            internal void FixOrder()
            {
                // TODO: apply NFC Quick Check
                if (!CheckOrder || Packed.Count == 1) return;
                int prev = UnpackCC(Packed[0]);
                for (int i = 1; i < Packed.Count; i++)
                {
                    int cc = UnpackCC(Packed[i]);
                    if (cc == 0 || prev <= cc)
                    {
                        prev = cc;
                        continue;
                    }
                    int j = i - 1;
                    while (true)
                    {
                        (Packed[j], Packed[j + 1]) = (Packed[j + 1], Packed[j]);
                        if (j == 0) break;
                        prev = UnpackCC(Packed[--j]);
                        if (prev <= cc) break;
                    }
                    prev = UnpackCC(Packed[i]);
                }
            }
        }
        internal List<int> Decomposed(IEnumerable<int> cps)
        {
            Packer p = new(this);
            List<int> buf = new();
            foreach (int cp0 in cps)
            {
                int cp = cp0;
                while (true)
                {
                    if (cp < 0x80)
                    {
                        p.Packed.Add(cp);
                    }
                    else if (IsHangul(cp))
                    {
                        int s_index = cp - S0;
                        int l_index = s_index / N_COUNT | 0;
                        int v_index = (s_index % N_COUNT) / T_COUNT | 0;
                        int t_index = s_index % T_COUNT;
                        p.Add(L0 + l_index);
                        p.Add(V0 + v_index);
                        if (t_index > 0) p.Add(T0 + t_index);
                    }
                    else
                    {
                        if (Decomp.TryGetValue(cp, out var decomp))
                        {
                            buf.AddRange(decomp);
                        }
                        else
                        {
                            p.Add(cp);
                        }
                    }
                    int count = buf.Count;
                    if (count == 0) break;
                    cp = buf[--count];
                    buf.RemoveAt(count);
                }
            }
            p.FixOrder();
            return p.Packed;
        }

        internal List<int> ComposedFromPacked(IReadOnlyList<int> packed)
        {
            List<int> cps = new();
            List<int> stack = new();
            int prev_cp = NONE;
            int prev_cc = 0;
            foreach (int p in packed)
            {
                int cc = UnpackCC(p);
                int cp = UnpackCP(p);
                if (prev_cp == NONE)
                {
                    if (cc == 0)
                    {
                        prev_cp = cp;
                    }
                    else
                    {
                        cps.Add(cp);
                    }
                }
                else if (prev_cc > 0 && prev_cc >= cc)
                {
                    if (cc == 0)
                    {
                        cps.Add(prev_cp);
                        cps.AddRange(stack);
                        stack.Clear();
                        prev_cp = cp;
                    }
                    else
                    {
                        stack.Add(cp);
                    }
                    prev_cc = cc;
                }
                else
                {
                    int composed = ComposePair(prev_cp, cp);
                    if (composed != NONE)
                    {
                        prev_cp = composed;
                    }
                    else if (prev_cc == 0 && cc == 0)
                    {
                        cps.Add(prev_cp);
                        prev_cp = cp;
                    }
                    else
                    {
                        stack.Add(cp);
                        prev_cc = cc;
                    }
                }
            }
            if (prev_cp != NONE)
            {
                cps.Add(prev_cp);
                cps.AddRange(stack);
            }
            return cps;
        }

        // primary
        public List<int> NFD(IEnumerable<int> cps) 
        {
            return Decomposed(cps).ConvertAll(UnpackCP);
        }

        public List<int> NFC(IEnumerable<int> cps)
        {
            return ComposedFromPacked(Decomposed(cps));
        }

        // convenience
        public string NFC(string s)
        {
            return NFC(s.Explode()).Implode();
        }
        public string NFD(string s)
        {
            return NFD(s.Explode()).Implode();
        }

    }
}
