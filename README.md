# ENSNormalize.cs
0-dependancy [ENSIP-15](https://docs.ens.domains/ens-improvement-proposals/ensip-15-normalization-standard) in C#

* Reference Implementation: [@adraffy/ens-normalize.js](https://github.com/adraffy/ens-normalize.js)
* Passes **100%** Validation Tests
* Space Efficient: `<60KB .dll`

```c#
// string -> string
// throws on invalid names
Console.WriteLine(adraffy.ENSNormalize.ENSIP15.Normalize("RaFFY🚴‍♂️.eTh"));
// "raffy🚴‍♂.eth"

// works like Normalize()
Console.WriteLine(adraffy.ENSNormalize.ENSIP15.Beautify("1⃣2⃣.eth")); 
// "1️⃣2️⃣.eth"
```