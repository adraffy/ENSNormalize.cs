using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ENS
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
