## Installation guide

Go to the Unity Asset Store and get CaptionsFileParser for free for your Unity account. Once it's in your library, you can go to your project and install it via *PackageManager -> MyAssets -> CaptionsFileParser*.

### Requirements

* 2019.4 and later (recommended)


&nbsp;

## Scripting

### Caption.cs

It is a *sctruct* that holds the minimun information of a caption. It holds the next information:

- FirstRow (string)
- SecondRow (string)
- EntryTime (TimeSpan)
- ExitTime (TimeSpan)

### CaptionDataParser.cs

It is in charge of parse a caption file to our Caption struct data. Use it this way:

```
string assetPath = "PathToYourCaptionsFile";

Queue<Caption> captionsQueue = new Queue<Caption>();

CaptionDataParser captionsParser = new CaptionDataParser();

captionsQueue = captionsParser.Parse(assetPath);
```