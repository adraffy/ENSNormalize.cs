namespace ADRaffy.ENSNormalize
{
    public static class ENSNormalize
    {
        public static readonly NF NF = new(new(Blobs.NF));
        public static readonly ENSIP15 ENSIP15 = new(NF, new(Blobs.ENSIP15));
    }
}
