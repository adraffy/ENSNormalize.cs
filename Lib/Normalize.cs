namespace ENS
{
    public class Normalize
    {
        private Normalize() {}

        public static readonly ENSIP15 LATEST = new(new NF(new Decoder(Properties.Resources.nf)), new Decoder(Properties.Resources.spec));
    }
}
