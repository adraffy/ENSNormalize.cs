using System.Text;

namespace ENS
{ 
    public static class Utils
    {
        static public string ToHexSequence(this string s)
        {
            return string.Join(' ', s.Explode().Select(x => x.ToString("X").PadLeft(2, '0')));
        }

        static public IEnumerable<int> Explode(this string s)
        {
            return s.EnumerateRunes().Select(x => x.Value);
        }

        static public string Implode(this IEnumerable<int> cps)
        {
            int[] v = cps.ToArray();
            StringBuilder sb = new(v.UTF16Length());
            sb.AppendCodepoints(v);
            return sb.ToString();
        }

        static public int UTF16Length(this IReadOnlyList<int> cps)
        {
            return cps.Sum(x => x < 0x10000 ? 1 : 2);
        }

        static public void AppendCodepoint(this StringBuilder sb, int cp)
        {

            if (cp < 0x10000)
            {
                sb.Append((char)cp);
            }
            else
            {
                cp -= 0x10000;
                sb.Append((char)(0xD800 | (cp >> 10)));
                sb.Append((char)(0xDC00 | (cp & 0x3FF)));
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
