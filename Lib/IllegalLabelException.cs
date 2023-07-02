using System;

namespace adraffy
{
    public class InvalidLabelException : Exception
    {
        public readonly string Label;
        public InvalidLabelException(string label, string message, NormException inner) : base(message, inner)
        {
            Label = label;
        }
    }
}
