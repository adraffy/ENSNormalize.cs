using System;
using System.Collections.Generic;
using System.Linq;

namespace ADRaffy.ENSNormalize
{
    public class Decoder
    {
        static int AsSigned(int i)
        {
            return (i & 1) != 0 ? ~i >> 1 : i >> 1;
        }

        private readonly uint[] Words;
        private readonly int[] Magic;
        private int Index, Bits;
        private uint Word;
        public Decoder(uint[] words) {
            Words = words;
            Index = 0;
            Word = 0;
            Bits = 0;
            Magic = ReadMagic();
        }
        public bool ReadBit()
        {
            if (Bits == 0)
            {
                Word = Words[Index++];
                Bits = 1;
            }
            bool bit = (Word & Bits) != 0;
            Bits <<= 1;
            return bit;
        }
        // read an ascending array 
        private int[] ReadMagic()
        {
            List<int> magic = new();
            int w = 0;
            while (true)
            {
                int dw = ReadUnary();
                if (dw == 0) break;
                magic.Add(w += dw);
            }
            return magic.ToArray();
        }
        // 1*0 = number of 1s
        // eg. 4 = 11110
        //     1 = 10
        //     0 = 0
        public int ReadUnary()
        {
            int x = 0;
            while (ReadBit()) x++;
            return x;
        }
        // read w-bits => interpret as w-bit int 
        // MSB first
        public int ReadBinary(int w)
        {
            int x = 0;
            for (int b = 1 << (w - 1); b > 0; b >>= 1)
            {
                if (ReadBit())
                {
                    x |= b;
                }
            }  
            return x;
        }
        // read magic-encoded int
        public int ReadUnsigned()
        {
            int a = 0;
            int w;
            int n;
            for (int i = 0; ; )
            {
                w = Magic[i];
                n = 1 << w;
                if (++i == Magic.Length || !ReadBit()) break;
                a += n;
            }
            return a + ReadBinary(w);
        }
        public int[] ReadSortedAscending(int n) => ReadArray(n, (prev, x) => prev + 1 + x);
        public int[] ReadUnsortedDeltas(int n) => ReadArray(n, (prev, x) => prev + AsSigned(x));
        public int[] ReadArray(int count, Func<int,int,int> fn)
        {
            int[] v = new int[count];
            if (count > 0)
            {
                int prev = -1;
                for (int i = 0; i < count; i++)
                {
                    v[i] = prev = fn(prev, ReadUnsigned());
                }
            }
            return v;
        }
        public List<int> ReadUnique()
        {
            List<int> ret = new(ReadSortedAscending(ReadUnsigned()));
            int n = ReadUnsigned();
            int[] vX = ReadSortedAscending(n).ToArray();
            int[] vS = ReadUnsortedDeltas(n).ToArray();
            for (int i = 0; i < n; i++)
            {                
                for (int x = vX[i], e = x + vS[i]; x < e; x++)
                {
                    ret.Add(x);
                }
            }
            return ret;
        }
        public List<int[]> ReadTree()
        {
            List<int[]> ret = new();
            ReadTree(ret, new());
            return ret;
        }
        private void ReadTree(List<int[]> ret, List<int> path)
        {
            int i = path.Count;
            path.Add(0);
            foreach (int x in ReadSortedAscending(ReadUnsigned()))
            {
                path[i] = x;
                ret.Add(path.ToArray());
            }
            foreach (int x in ReadSortedAscending(ReadUnsigned()))
            {
                path[i] = x;
                ReadTree(ret, path);
            }
            path.RemoveAt(i);
        }
        // convenience
        public string ReadString() => ReadUnsortedDeltas(ReadUnsigned()).Implode();
        public int[] ReadSortedUnique()
        {
            int[] v = ReadUnique().ToArray();
            Array.Sort(v);
            return v;
        }
    }

}
