namespace adraffy
{
    public class IllegalMixtureException : NormException
    {
        public readonly Group Group;
        public readonly int Codepoint;
        public readonly Group OtherGroup;
        internal IllegalMixtureException(string reason, Group group, int cp, Group other) : base("illegal mixture", reason)
        {
            Group = group;
            Codepoint = cp;
            OtherGroup = other;
        }
    }
}
