using System.Diagnostics;
using System.Text;
using System.Text.Json;
using adraffy;

TestCodepoints();
TestNF(ENSNormalize.NF);
TestENSIP15(ENSNormalize.ENSIP15);

ENSNormalize.ENSIP15.Normalize("a");

foreach (Group g in ENSNormalize.ENSIP15.Groups)
{
    Console.WriteLine($"{g.Index + 1}. {g.Description} Primary({g.Primary.Count}) Secondary({g.Secondary.Count}) CM({g.CMWhitelisted})");
}

Console.WriteLine(ENSNormalize.NF.NFC("\x65\u0300").ToHexSequence());
Console.WriteLine(ENSNormalize.NF.NFD("\xE8").ToHexSequence());

DumpSplit("RAFFY.eTh");
DumpSplit("xn--💩.eth");
DumpSplit("бургер");
DumpSplit("☝\uFE0F🏻");
DumpSplit("💩Raffy.eth_");
DumpSplit("");

DumpLabel(ENSNormalize.ENSIP15.NormalizeLabel("."));
DumpLabel(ENSNormalize.ENSIP15.NormalizeLabel("raffy"));

Console.WriteLine($"Emojis: {ENSNormalize.ENSIP15.Emojis.Count}");
Console.WriteLine($"Groups: {ENSNormalize.ENSIP15.Groups.Count}");

// readme
Console.WriteLine(ENSNormalize.ENSIP15.Normalize("RaFFY🚴‍♂️.eTh").ToHexSequence());
Console.WriteLine(ENSNormalize.ENSIP15.Beautify("1⃣2⃣.eth").ToHexSequence());
Console.WriteLine(ENSNormalize.ENSIP15.SafeCodepoint(0x303));
Console.WriteLine(ENSNormalize.ENSIP15.SafeImplode(new int[] { 0x303, 0xFE0F }).ToHexSequence());
Console.WriteLine(ENSNormalize.ENSIP15.ShouldEscape.Contains(0x202E));

void DumpSplit(string name)
{
    Label[] labels = ENSNormalize.ENSIP15.Split(name);
    Console.Write('[');
    if (labels.Any()) Console.WriteLine();
    foreach (Label x in labels) 
    {
        Console.Write("  ");
        DumpLabel(x);
    }
    Console.WriteLine(']');
}
void DumpLabel(Label label)
{
    Console.Write($"[{label.Input.ToHexSequence()}]");
    if (label.Tokens != null)
    {
        Console.Write($" <{string.Join("+", label.Tokens.Select(t => t.ToString()))}>");
    }
    if (label.Error == null)
    {
        Console.Write($" {label.Group} [{label.Normalized.ToHexSequence()}]");
    }
    else
    {
        Console.Write($" \"{label.Error.Message}\"");
    }
    Console.WriteLine();
}
void TestCodepoints()
{
    StringBuilder sb = new();
    for (int cp = 0; cp < 0x110000; cp++)
    {
        //if (cp >= 0xD800 || cp <= 0xDFFF) continue;
        sb.Clear();
        sb.AppendCodepoint(cp);
        int[] cps = sb.ToString().Explode().ToArray();
        if (cps.Length != 1 || cps[0] != cp) throw new Exception($"wrong {cp} vs {cps[0]}");
    }
}
int TestENSIP15(ENSIP15 impl)
{
    Stopwatch watch = new();
    watch.Start();
    Console.WriteLine("[TestENSIP15]");
    int errors = 0;
    //Console.WriteLine($"Unicode Version: {impl.UnicodeVersion}");
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
    watch.Stop();
    Console.WriteLine($"{PassOrFail(errors == 0)} Errors({errors}) Time({watch.ElapsedMilliseconds})");
    return errors;
}
int TestNF(NF impl)
{
    Stopwatch watch = new();
    Console.WriteLine("[TestNF]");
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
    watch.Stop();
    Console.WriteLine($"{PassOrFail(errors == 0)} Errors({errors}) Time({watch.ElapsedMilliseconds})");
    return errors;
}
string PassOrFail(bool pass)
{
    return pass ? "PASS" : "FAIL";
}
