using System;
using System.Linq;
using ADRaffy.ENSNormalize;

foreach (Group g in ENSNormalize.ENSIP15.Groups)
{
    Console.WriteLine($"{g.Index + 1}. {g} Primary({g.Primary.Count}) Secondary({g.Secondary.Count}) CM({g.CMWhitelisted})");
}

Console.WriteLine(ENSNormalize.NF.NFC("\x65\u0300").ToHexSequence());
Console.WriteLine(ENSNormalize.NF.NFD("\xE8").ToHexSequence());

Console.WriteLine(ENSNormalize.ENSIP15.SafeCodepoint(0x450));
Console.WriteLine(ENSNormalize.ENSIP15.Normalize("\u0435\u0300"));

DumpSplit("RAFFY.eTh");
DumpSplit("xn--💩.eth");
DumpSplit("бургер");
DumpSplit("☝\uFE0F🏻");
DumpSplit("💩Raffy.eth_");
DumpSplit("");
DumpSplit("x\u0303");

DumpLabel(ENSNormalize.ENSIP15.NormalizeLabel("."));
DumpLabel(ENSNormalize.ENSIP15.NormalizeLabel("raffy"));

Console.WriteLine(ENSNormalize.ENSIP15.InvalidCompositions.ToHexSequence());

Console.WriteLine($"Emojis: {ENSNormalize.ENSIP15.Emojis.Count}");
Console.WriteLine($"Groups: {ENSNormalize.ENSIP15.Groups.Count}");

Console.WriteLine(ENSNormalize.ENSIP15.Normalize("RaFFY🚴‍♂️.eTh").ToHexSequence());
Console.WriteLine(ENSNormalize.ENSIP15.Beautify("1⃣2⃣.eth").ToHexSequence());

Console.WriteLine(ENSNormalize.ENSIP15.SafeCodepoint(0x303));
Console.WriteLine(ENSNormalize.ENSIP15.SafeImplode(new int[] { 0x303, 0xFE0F }).ToHexSequence());
Console.WriteLine(ENSNormalize.ENSIP15.ShouldEscape.Contains(0x202E));

Console.WriteLine(ENSNormalize.ENSIP15.NormalizeFragment("AB--").ToHexSequence());
Console.WriteLine(ENSNormalize.ENSIP15.NormalizeFragment("\u0300").ToHexSequence());
Console.WriteLine(ENSNormalize.ENSIP15.NormalizeFragment("\u03BF\u043E").ToHexSequence());

void DumpSplit(string name)
{
    Label[] labels = ENSNormalize.ENSIP15.Split(name);
    Console.Write('[');
    if (labels.Length > 0) Console.WriteLine();
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
        Console.Write($" <{string.Join("+", label.Tokens.Select(t => t.ToString()).ToArray())}>");
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