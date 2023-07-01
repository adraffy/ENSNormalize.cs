namespace adraffy
{
    public class Label
    {
        public readonly IReadOnlyList<int> Input;
        public readonly IReadOnlyList<OutputToken>? Tokens;
        public readonly IReadOnlyList<int> Normalized;
        public readonly string Kind;
        public readonly Group Group;
        public readonly NormException Error;

        internal Label(int[] input, List<OutputToken> tokens, NormException e) {
            Input = input;
            Tokens = tokens;
            Error = e;
        }
        internal Label(int[] input, List<OutputToken> tokens, int[] cps, string kind, Group g) 
        {
            Input = input;
            Tokens = tokens;
            Normalized = cps;
            Kind = kind;
            Group = g;
        }
    }
}
