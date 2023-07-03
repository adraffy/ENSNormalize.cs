# ENSNormalize.cs
0-dependancy [ENSIP-15](https://docs.ens.domains/ens-improvement-proposals/ensip-15-normalization-standard) in C#

* Reference Implementation: [@adraffy/ens-normalize.js](https://github.com/adraffy/ens-normalize.js)
	* Unicode: `15.0.0`
	* Spec: [`962316964553fce6188e25a5166a4c1e906333adf53bdf2964c71dedc0f8e2c8`](https://github.com/ensdomains/docs/blob/master/ens-improvement-proposals/ensip-15/spec.json)
* Passes **100%** [Validation Tests](https://github.com/ensdomains/docs/blob/master/ens-improvement-proposals/ensip-15/tests.json)
* Passes **100%** [Normalization Tests](https://unicode.org/Public/15.0.0/ucd/NormalizationTest.txt)
* Space Efficient: `~60KB` as [Inline Blobs](./Lib/Blobs.cs) via [make.js](./Compress/make.js)
* Legacy Support: `netstandard1.1`, `net451`, `netcoreapp3.1`

```c#
// globals
adraffy.ENSNormalize.ENSIP15 // Main Library
adraffy.ENSNormalize.NF      // NFC/NFD
```

### Primary API

```c#
// string -> string
// throws on invalid names
ENSIP15.Normalize("RaFFY🚴‍♂️.eTh"); // "raffy🚴‍♂.eth"

// works like Normalize()
ENSIP15.Beautify("1⃣2⃣.eth"); // "1️⃣2️⃣.eth"
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
//       OutputToken { Codepoints: [ 128169 ], IsEmoji: true }
//       OutputToken { Codepoints: [ 114, 97, 102, 102, 121 ] }
//     ],
//     Normalized: [ 128169, 114, 97, 102, 102, 121 ],
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
// note: this throws on "."
```

### Normalization Properties

* [Group](./Lib/Group.cs) — `ENSIP15.Groups: IReadOnlyList<Group>`
* [EmojiSequence](./Lib/EmojiSequence.cs) — `ENSIP15.Emojis: IReadOnlyList<Emoji>`
* [Whole](./Lib/Whole.cs) — `ENSIP15.Wholes: IReadOnlyList<Whole>`

### Error Handling

All errors are safe to print. Functions that accept names as input wrap their exceptions in [InvalidLabelException](./Lib/InvalidLabelException.cs) for additional context.

#### Errors with Additional Context
* (Base) [NormException](./Lib/NormException.cs) `{ Kind: string, Reason: string? }`
* [DisallowedCharacterException](./Lib/DisallowedCharacterException.cs) `{ Codepoint }`
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

### Unicode Normalization Forms: [NF](./Lib/NF.cs)

```c#
// string -> string 
// IEnumerable<int> -> int[]
NF.NFC("\x65\u0300"); // [E5]
NF.NFD("\xE5");  //  [65 300]
```

### Utilities

Normalize name fragments for substring search:
```c#
// string -> string
// only throws InvalidLabelException w/DisallowedCharacterException
ENSIP15.NormalizeFragment("AB--");
ENSIP15.NormalizeFragment("\u0300");
ENSIP15.NormalizeFragment("\u03BF\u043E");
// note: Normalize() throws on these
```

Construct safe strings:
```c#
// int -> string
ENSIP15.SafeCodepoint(0x303); // "◌̃"
// IReadOnlyList<int> -> string
ENSIP15.SafeImplode(new int[]{ 0x303, 0xFE0F }); // "◌̃{FE0F}"
```
Determine if a character shouldn't be printed directly:
```c#
// IReadOnlyCollection<int>
ENSIP15.ShouldEscape.Contains(0x202E); // RIGHT-TO-LEFT OVERRIDE => true
```
Determine if a character is a combining mark:
```c#
// IReadOnlyCollection<int>
ENSIP15.CombiningMarks.Contains(0x20E3); // COMBINING ENCLOSING KEYCAP => true
```
