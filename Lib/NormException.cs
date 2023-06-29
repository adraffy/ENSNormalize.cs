using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ENS
{
    public class NormException : Exception
    {
        public readonly string Kind;
        internal NormException(string kind) : base(kind)
        {
            Kind = kind;
        }
        internal NormException(string kind, string reason) : base($"{kind}: {reason}")
        {
            Kind = kind;
        }
    }
}
