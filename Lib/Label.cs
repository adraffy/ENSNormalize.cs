namespace adraffy
{
    public class Label
    {
        // error: [Input, Tokens?, Error ]
        // valid: [Input, Tokens, Kind, Group?, Normalized ]

        public readonly IReadOnlyList<int> Input;
        public readonly IReadOnlyList<OutputToken> Tokens; // nullable
        public readonly NormException Error; // nullable
        public readonly IReadOnlyList<int> Normalized; // nullable
        public readonly string Kind; // nullable
        public readonly Group Group; // nullable

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
