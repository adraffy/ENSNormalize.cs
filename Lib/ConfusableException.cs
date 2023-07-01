namespace adraffy
{
    public class ConfusableException : NormException
    {
        public readonly Group Group;
        public readonly Group OtherGroup;
        internal ConfusableException(Group group, Group other) : base("whole-script confusable", $"{group}/{other}")
        {
            Group = group;
            OtherGroup = other;
        }   
    }
}
