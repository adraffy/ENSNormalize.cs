
using ENS;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

ENSIP15 impl = ENS.Normalize.LATEST;

if (!args.Any())
{
    TestNF(impl.NF);
    //Console.WriteLine(impl.Normalize(new int[] { 0x78, 0x6E, 0x2D, 0x2D, 0x1F4A9 }.Implode()).ToHexSequence());
    //Console.WriteLine(impl.Normalize("бургер").ToHexSequence());
    
    TestENSIP15(impl);
} 
else if (args[0] == "nf")
{
    TestNF(impl.NF);
} 
else if (args[0] == "ensip15")
{
    TestENSIP15(impl);
}
else if (args[0] == "play")
{
    Console.WriteLine(impl.NF.NFC("e\u0300"));
    Console.WriteLine(impl.Normalize("\u03C2"));
    Console.WriteLine(impl.Normalize("RAFFY.eTh"));
}
else
{
    throw new Exception("unknown mode");
}

int TestENSIP15(ENSIP15 impl)
{
    int errors = 0;
    Console.WriteLine($"Unicode Version: {impl.UnicodeVersion}");
    Console.WriteLine($"   CLDR Version: {impl.CLDRVersion}");
    JsonDocument json = JsonDocument.Parse(File.ReadAllBytes("data/tests.json"));
    foreach (JsonElement test in json.RootElement.EnumerateArray())
    {
        string name = test.GetProperty("name").ToString();
        string norm0 = test.TryGetProperty("norm", out JsonElement _norm) ? _norm.ToString() : name;
        bool shouldError = test.TryGetProperty("error", out JsonElement _error) && _error.GetBoolean();
        try
        {
            string norm = impl.Normalize(name);
            if (shouldError)
            {
                errors++;
                Console.WriteLine($"Expected Error: Name({name.ToHexSequence()}) Got({norm.ToHexSequence()})");
            }
            else if (norm != norm0)
            {
                errors++;
                Console.WriteLine($"Wrong Norm: Name({name.ToHexSequence()}) Expect({norm0.ToHexSequence()}) Got({norm.ToHexSequence()})");
            }
        }
        catch (InvalidLabelException err)
        {
            if (!shouldError)
            {
                errors++;
                Console.WriteLine($"Unexpected Error: Name({name.ToHexSequence()}) Expect({norm0.ToHexSequence()}) Got({err.Message})");
            }
        }
    }
    Console.WriteLine($"{PassOrFail(errors == 0)} Errors: {errors}");
    return errors;
}


int TestNF(NF impl)
{
    int errors = 0;
    Console.WriteLine($"Unicode Version: {impl.UnicodeVersion}");
    JsonDocument json = JsonDocument.Parse(File.ReadAllBytes("data/nf-tests.json"));
    int pad = json.RootElement.EnumerateObject().ToList().ConvertAll(x => x.Value.GetArrayLength()).Max().ToString().Length;
    foreach (JsonProperty section in json.RootElement.EnumerateObject())
    {        
        int sectionErrors = 0;
        foreach (JsonElement test in section.Value.EnumerateArray())
        {
            string input = test[0].GetString();
            string nfd0 = test[1].GetString();
            string nfc0 = test[2].GetString();

            string nfd = impl.NFD(input);
            string nfc = impl.NFC(input);

            if (nfd != nfd0)
            {
                Console.WriteLine($"Wrong NFD: Expect({nfd0.ToHexSequence()}) Got({nfd.ToHexSequence()})");
                sectionErrors++;
            }

            if (nfc != nfc0)
            {
                Console.WriteLine($"Wrong NFC: Expect({nfc0.ToHexSequence()}) Got({nfc.ToHexSequence()})");
                sectionErrors++;
            }
        }
        string state = PassOrFail(sectionErrors == 0);
        string error = sectionErrors.ToString().PadLeft(pad);
        string count = section.Value.GetArrayLength().ToString().PadRight(pad);
        Console.WriteLine($"{state} {error}/{count} {section.Name}");
        errors += sectionErrors;
    }
    Console.WriteLine($"{PassOrFail(errors == 0)} Errors: {errors}");
    return errors;
}

string PassOrFail(bool pass)
{
    return pass ? "PASS" : "FAIL";
}
