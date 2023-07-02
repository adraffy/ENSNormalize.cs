using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace adraffy
{ 
    public static class Utils
    {
        const int UTF16_BMP = 0x10000;

        // format strings/codepoints as "HEX1 HEX2 ..."
        static public string ToHexSequence(this IEnumerable<int> v)
        {
            return string.Join(" ", v.Select(x => x.ToString("X").PadLeft(2, '0')));
        }
        static public string ToHexSequence(this string s)
        {
            return s.Explode().ToHexSequence();
        }
        
        // convert strings <=> codepoints
        static public IEnumerable<int> Explode(this string s)
        {         
            int n = s.Length;
            if (n == 0) return Array.Empty<int>();
            List<int> v = new(n);
            for (int i = 0; i < n; )
            {
                int cp = char.ConvertToUtf32(s, i);
                v.Add(cp);
                i += cp < UTF16_BMP ? 1 : 2;
            }
            return v;
        }
        static public string Implode(this IEnumerable<int> cps)
        {
            int[] v = cps.ToArray();
            StringBuilder sb = new(v.UTF16Length());
            sb.AppendCodepoints(v);
            return sb.ToString();
        }

        // efficiently build strings from codepoints
        static public int UTF16Length(this IReadOnlyList<int> cps)
        {
            return cps.Sum(x => x < UTF16_BMP ? 1 : 2);
        }
        static public void AppendCodepoint(this StringBuilder sb, int cp)
        {
            if (cp < UTF16_BMP)
            {
                sb.Append((char)cp);
            }
            else
            {
                cp -= UTF16_BMP;
                sb.Append((char)(0xD800 | (cp >> 10)));   // upper 10 bits
                sb.Append((char)(0xDC00 | (cp & 0x3FF))); // lower 10 bits
            }
        }
        static public void AppendCodepoints(this StringBuilder sb, IEnumerable<int> v)
        {
            foreach (int cp in v)
            {
                sb.AppendCodepoint(cp);
            }
        }
    }

}
