# Captions File Parser

Easy and simple way to convert subtitle files into C# data structures.

<p align="center">
  <img src="https://github.com/Fenikkel/CaptionsFileParser/assets/41298931/148d7f8a-6555-4a75-984b-a635f5a3f186" alt="Art image"/>
</p>


&nbsp;
## Usage
Create a CaptionDataParser and use Parse() with the desired caption file to obtain a Queue<Caption> with all the data:

```
    Queue<Caption> captionsQueue = new Queue<Caption>();

    CaptionDataParser captionsParser = new CaptionDataParser();

    captionsQueue = captionsParser.Parse(textAsset);
```

&nbsp;
## Installation
Add the custom package to your project via:
- [Unity Asset Store](https://u3d.as/3bXj)

or

- Package Manager -> + -> Add package from git URL -> https://github.com/Fenikkel/CaptionsFileParser.git


<p align="center">
    <img src="https://github.com/Fenikkel/SimpleTween/assets/41298931/0f447b8c-85ca-4205-9915-ca7203dc4741" alt="Instructions" height="384">
</p>


&nbsp;
## Technical details

Supported captions file:

    SubRip Subtitle (.srt)


_More file formats coming soon_

&nbsp;
## Compatibility
- Any Unity version
- Any pipeline (Build-in, URP, HDRP, etc)

&nbsp;
## Support
⭐ Star if you like it  
❤️️ Follow me for more
