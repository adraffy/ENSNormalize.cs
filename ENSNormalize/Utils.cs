using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace ADRaffy.ENSNormalize
{
    public static class Utils
    {
        const int UTF16_BMP = 0x10000;
        const int UTF16_BITS = 10;
        const int UTF16_HEAD = ~0 << UTF16_BITS;      // upper 6 bits
        const int UTF16_DATA = (1 << UTF16_BITS) - 1; // lower 10 bits
        const int UTF16_HI = 0xD800; // 110110*
        const int UTF16_LO = 0xDC00; // 110111*

        // format strings/codepoints
        static public string ToHex(this int cp) => cp.ToString("X").PadLeft(2, '0');
        static public string ToHexSequence(this IEnumerable<int> v) => string.Join(" ", v.Select(x => x.ToHex()).ToArray());
        static public string ToHexSequence(this string s) => s.Explode().ToHexSequence();
        
        // convert strings <=> codepoints
        // note: we do not care if the string is invalid UTF-16
        static public List<int> Explode(this string s)
        {         
            int n = s.Length;
            List<int> v = new(n);
            for (int i = 0; i < n; )
            {
                char ch0 = s[i++];
                char ch1;
                int head = ch0 & UTF16_HEAD;
                if (head == UTF16_HI && i < n && ((ch1 = s[i]) & UTF16_HEAD) == UTF16_LO) // valid pair
                {
                    v.Add(UTF16_BMP + (((ch0 & UTF16_DATA) << UTF16_BITS) | (ch1 & UTF16_DATA)));
                    i++;
                }
                else // bmp OR illegal surrogates
                {
                    v.Add(ch0);
                }
                // reference implementation
                /*
                int cp = char.ConvertToUtf32(s, i); // errors on invalid
                v.Add(cp);
                i += char.IsSurrogatePair(s, i) ? 2 : 1;
                */
            }
            return v;
        }
        static public string Implode(this IEnumerable<int> cps)
        {
            StringBuilder sb = new(cps.UTF16Length());
            sb.AppendCodepoints(cps);
            return sb.ToString();
        }

        // efficiently build strings from codepoints
        static public int UTF16Length(this IEnumerable<int> cps) => cps.Sum(x => x < UTF16_BMP ? 1 : 2);
        static public void AppendCodepoint(this StringBuilder sb, int cp)
        {
            if (cp < UTF16_BMP)
            {
                sb.Append((char)cp);
            }
            else
            {
                cp -= UTF16_BMP;
                sb.Append((char)(UTF16_HI | ((cp >> UTF16_BITS) & UTF16_DATA)));
                sb.Append((char)(UTF16_LO | (cp & UTF16_DATA)));
            }
            // reference implementation
            //sb.Append(char.ConvertFromUtf32(cp)); // allocates a string
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
