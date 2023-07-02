using System.Collections.Generic;

namespace adraffy
{
    public class Label
    {
        // error: [Input, Tokens?, Error ]
        // valid: [Input, Tokens, Group?, Normalized ]

        public readonly IReadOnlyList<int> Input;
        public readonly IReadOnlyList<OutputToken> Tokens; // nullable
        public readonly NormException Error; // nullable
        public readonly IReadOnlyList<int> Normalized; // nullable
        public readonly Group Group; // nullable

        internal Label(int[] input, List<OutputToken> tokens, NormException e) {
            Input = input;
            Tokens = tokens;
            Error = e;
        }
        internal Label(int[] input, List<OutputToken> tokens, int[] cps, Group g) 
        {
            Input = input;
            Tokens = tokens;
            Normalized = cps;
            Group = g;
        }
    }
}
