
namespace ENS
{
    public class Label
    {
        public string Input;
        public int Offset;
        public bool HasEmoji;
        public string Type;
        public string Output;
        public Exception Error { get; internal set; }
        public Label(string input, int offset)
        {
            Input = input;
            Offset = offset;
        }
    }
}
