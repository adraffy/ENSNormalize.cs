namespace ENS
{
    public class ConfusableException : NormException
    {
        public readonly Group Group;
        public readonly Group OtherGroup;
        internal ConfusableException(string reason, Group group, Group other) : base("whole-script-confusable", reason)
        {
            Group = group;
            OtherGroup = other;
        }   
    }
}
