using System;

namespace adraffy
{
    public class NormException : Exception
    {
        public readonly string Kind;
        public readonly string Reason; // nullable
        internal NormException(string kind) : base(kind)
        {
            Kind = kind;
        }
        internal NormException(string kind, string reason) : base($"{kind}: {reason}")
        {
            Kind = kind;
            Reason = reason;
        }
    }
}
