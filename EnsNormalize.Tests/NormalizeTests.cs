using adraffy;

namespace EnsNormalize.Tests
{
    public class NormalizeTests
    {
        [Theory]
        [NormJsonFileData("data/tests.json")]
        public void ENSIP15Test(string name, bool error, string norm, string comment)
        {
            if (error)
            {
                Assert.Throws<InvalidLabelException>(() => ENSNormalize.ENSIP15.Normalize(name));
            }
            else
            {
                var result = ENSNormalize.ENSIP15.Normalize(name);
                var expected = string.IsNullOrEmpty(norm) ? name : norm;
                Assert.Equal(expected, result);
            }
        }

        
        [Theory]
        [JsonFileData("data/nf-tests.json", "Specific cases")]
        public void NFTest(string input, string expectednfd, string expectednfc)
        {
            string nfd = ENSNormalize.NF.NFD(input);
            string nfc = ENSNormalize.NF.NFC(input);

            Assert.Equal(expectednfd, nfd);
            
            Assert.Equal(expectednfc, nfc);
        }
    }
}