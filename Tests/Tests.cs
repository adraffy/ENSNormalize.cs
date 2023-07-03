using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json.Linq;

namespace ADRaffy.ENSNormalize.Tests
{
    public class Tests
    {
        private readonly ITestOutputHelper Output;

        public Tests(ITestOutputHelper output)
        {
            Output = output;
        }

        [Fact]
        public void NFTests()
        {
            int errors = 0;
            foreach (var section in JObject.Parse(File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "data/nf-tests.json"))))
            {
                foreach (JToken test in (JArray)section.Value!)
                {
                    string input = (string)test[0]!;
                    string nfd0 = (string)test[1]!;
                    string nfc0 = (string)test[2]!;

                    string nfd = ENSNormalize.NF.NFD(input);
                    string nfc = ENSNormalize.NF.NFC(input);

                    if (nfd != nfd0)
                    {
                        errors++;
                        Output.WriteLine($"Wrong NFD: Expect[{nfd0.ToHexSequence()}] Got[{nfd.ToHexSequence()}]");
                    }

                    if (nfc != nfc0)
                    {
                        errors++;
                        Output.WriteLine($"Wrong NFC: Expect[{nfc0.ToHexSequence()}] Got[{nfc.ToHexSequence()}]");
                    }
                }
            }
            Assert.Equal(0, errors);
        }

        [Fact]
        public void ValidationTests()
        {
            int errors = 0;
            foreach (JToken test in JArray.Parse(File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "data/tests.json"))))
            {
                string name = (string)test["name"]!;
                string norm0 = (string?)test["norm"] ?? name;
                bool shouldError = (bool?)test["error"] ?? false;
                Console.WriteLine(name.ToHexSequence());
                try
                {
                    string norm = ENSNormalize.ENSIP15.Normalize(name);
                    if (shouldError)
                    {
                        errors++;
                        Output.WriteLine($"Expected Error: [{name.ToHexSequence()}] Got[{norm.ToHexSequence()}]");
                    }
                    else if (norm != norm0)
                    {
                        errors++;
                        Output.WriteLine($"Wrong Norm: [{name.ToHexSequence()}] Expect[{norm0.ToHexSequence()}] Got[{norm.ToHexSequence()}]");
                    }
                }
                catch (InvalidLabelException err)
                {
                    if (!shouldError)
                    {
                        errors++;
                        Output.WriteLine($"Unexpected Error: [{name.ToHexSequence()}] Expect[{norm0.ToHexSequence()}] {err.Message}");
                    }
                }
            }
            Assert.Equal(0, errors);
        }

        [Fact]
        public void CodepointCoding()
        {
            StringBuilder sb = new();
            for (int cp = 0; cp < 0x110000; cp++)
            {
                sb.Clear();
                sb.AppendCodepoint(cp);
                List<int> cps = sb.ToString().Explode();
                if (cps.Count != 1 || cps[0] != cp)
                {
                    throw new Exception($"Expect[{cp.ToHex()}] vs Got[{cps.ToHexSequence()}]");
                }
            }
        }

        [Fact]
        public void NormalizeFragments()
        {
            ENSNormalize.ENSIP15.NormalizeFragment("AB--");
            ENSNormalize.ENSIP15.NormalizeFragment("..\u0300");
            ENSNormalize.ENSIP15.NormalizeFragment("\u03BF\u043E");
        }

    }
}