namespace ADRaffy.ENSNormalize
{
    public class DisallowedCharacterException : NormException
    {
        public readonly int Codepoint;
        internal DisallowedCharacterException(string reason, int cp) : base("disallowed character", reason)
        {
            Codepoint = cp;
        }
    }
}
