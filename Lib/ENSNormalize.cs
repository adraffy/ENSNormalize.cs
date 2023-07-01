namespace adraffy
{
    public class ENSNormalize
    {
        private ENSNormalize() {}

        public static readonly NF NF = new(new(Properties.Resources.nf));
        public static readonly ENSIP15 ENSIP15 = new(NF, new(Properties.Resources.spec));
    }
}
