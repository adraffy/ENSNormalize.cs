using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ENS
{
    public class OutputToken
    {
        public readonly bool IsEmoji;
        public int[] cps;
        public OutputToken(bool _IsEmoji, int[] _cps)
        {
            IsEmoji = _IsEmoji;
            cps = _cps;
        }
    }
}
