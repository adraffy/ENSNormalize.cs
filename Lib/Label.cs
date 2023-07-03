using System.Collections.Generic;

namespace adraffy
{
    public class Label
    {
        // error: [Input, Tokens?, Error ]
        // valid: [Input, Tokens, Group, Normalized ]

        public readonly IReadOnlyList<int> Input;
        public readonly IReadOnlyList<OutputToken>? Tokens;
        public readonly NormException? Error;
        public readonly IReadOnlyList<int>? Normalized;
        public readonly Group? Group;

        internal Label(int[] input, List<OutputToken>? tokens, NormException e) {
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
