using System.Reflection;

namespace ENS
{
    public class Normalize
    {

        static Stream Embed(String name)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream(name)!;
        }

        private Normalize() {}

        public static readonly ENSIP15 LATEST = new ENSIP15(new NF(Embed("ENS.data.nf.json")), Embed("ENS.data.spec.json"));
    }

}
