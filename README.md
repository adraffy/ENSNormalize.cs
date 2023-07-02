# ENSNormalize.cs
0-dependancy [ENSIP-15](https://docs.ens.domains/ens-improvement-proposals/ensip-15-normalization-standard) in C#

* Reference Implementation: [@adraffy/ens-normalize.js](https://github.com/adraffy/ens-normalize.js)
* Passes **100%** Validation Tests
* Space Efficient: `.dll` under `60KB`
	* [Resources](./Lib/Resources/) via [make.js](./Compress/make.js)


```c#
// globals
adraffy.ENSNormalize.ENSIP15 // Main Library
adraffy.ENSNormalize.NF      // NFC/NFD
```

### Primary API

```c#
// string -> string
// throws on invalid names
Console.WriteLine(ENSIP15.Normalize("RaFFY🚴‍♂️.eTh"));
// "raffy🚴‍♂.eth"

// works like Normalize()
Console.WriteLine(ENSIP15.Beautify("1⃣2⃣.eth")); 
// "1️⃣2️⃣.eth"
```
### Output-based tokenization: [Label](./Lib/Label.cs)

```c#
// string -> Label[]
// never throws
Label[] labels = ENSNormalize.ENSIP15.Split("💩Raffy.eth_");
// [
//   Label {
//     Input: [ 128169, 82, 97, 102, 102, 121 ],  
//     Tokens: [
//       OutputToken { Codepoints: [ 128169, 65039 ], IsEmoji: true }
//       OutputToken { Codepoints: [ 114, 97, 102, 102, 121 ] }
//     ],
//     Normalized: [ 128169, 114, 97, 102, 102, 121 ],
//     Kind: "Latin"
//     Group: Group { Name: "Latin", ... }
//   },
//   Label {
//     Input: [ 101, 116, 104, 95 ],
//     Tokens: [ 
//       OutputToken { Codepoints: [ 101, 116, 104, 95 ] }
//     ],
//     Error: NormError { Kind: "underscore allowed only at start" }
//   }
// ]

// string -> Label
// never throws
Label label = ENSNormalize.ENSIP15.NormalizeLabel("ABC");
```

### Normalization Properties

* [Group](./Lib/Group.cs) — `ENSIP15.Groups: IReadOnlyList<Group>`
* [EmojiSequence](./Lib/EmojiSequence.cs) — `ENSIP15.Emojis: IReadOnlyList<Emoji>`
* [Whole](./Lib/Whole.cs) — `ENSIP15.Wholes: IReadOnlyList<Whole>`

### Error Handling

All errors are safe to print. Functions that accept names as input wrap their exceptions in [InvalidLabelException](./Lib/InvalidLabelException.cs) `{ Label: string, Error: NormError }` for additional context.

```c#
// int -> string
ENSIP15.SafeCodepoint(0x303); // "◌̃"

// IReadOnlyList<int> -> string
ENSIP15.SafeImplode(new int[]{ 0x303, 0xFE0F }); // "◌̃{FE0F}"

// IReadOnlySet<int>
ENSIP15.ShouldEscape.Contains(0x202E); // true
```

#### Label Error with Additional Context
* (Base) [NormException](./Lib/NormException.cs) `{ Kind: string }`
* [DisallowedCharacterException](./Lib/DisallowedCharacterException.cs) `{ CodePoint }`
* [ConfusableException](./Lib/ConfusableException.cs) `{ Group, OtherGroup }`
* [IllegalMixtureException](./Lib/IllegalMixtureException.cs) `{ Codepoint, Group, OtherGroup? }`

#### Error Kinds

* `"empty label"`
* `"duplicate non-spacing marks"`
* `"excessive non-spacing marks"`
* `"leading fenced"`
* `"adjacent fenced"`
* `"trailing fenced"`
* `"leading combining mark"`
* `"emoji + combining mark"`
* `"invalid label extension"`
* `"underscore allowed only at start"`
* `"illegal mixture"`
* `"whole-script confusable"`
* `"disallowed character"`