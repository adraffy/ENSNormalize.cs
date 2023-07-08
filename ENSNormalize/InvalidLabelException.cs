﻿using System;

namespace ADRaffy.ENSNormalize
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