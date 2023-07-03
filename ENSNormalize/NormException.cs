using System;

namespace ADRaffy.ENSNormalize
{
    public class NormException : Exception
    {
        public readonly string Kind;
        public readonly string? Reason;
        internal NormException(string kind, string? reason = null) : base(reason != null ? $"{kind}: {reason}" : kind)
        {
            Kind = kind;
            Reason = reason;
        }
    }
}
