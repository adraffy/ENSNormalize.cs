using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ADRaffy.ENSNormalize
{
    public class Label
    {
        // error: [Input, Tokens?, Error ]
        // valid: [Input, Tokens, Group, Normalized ]

        public readonly IList<int> Input;
        public readonly IList<OutputToken>? Tokens;
        public readonly NormException? Error;
        public readonly int[]? Normalized;
        public readonly Group? Group;

        internal Label(IList<int> input, IList<OutputToken>? tokens, NormException e) {
            Input = input;
            Tokens = tokens;
            Error = e;
        }
        internal Label(IList<int> input, IList<OutputToken> tokens, int[] cps, Group g) 
        {
            Input = input;
            Tokens = tokens;
            Normalized = cps;
            Group = g;
        }
    }
}
