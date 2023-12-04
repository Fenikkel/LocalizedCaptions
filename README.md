# Localized Captions

Multi-lingual captions system that use .srt or similar as input.

<p align="center">
  <img src="https://github.com/Fenikkel/LocalizedCaptions/assets/41298931/eb1c716f-aae5-458c-ad9a-bfde912ddafc" alt="Art image"/>
</p>



&nbsp;
## Usage
1. Make sure you have a LocalizationSettings.
2. Create an AssetTableCollection for your subtitles.
3. Create an entry to drag your subtitle files on it.
4. Drag and drop the LocalizedCaptions.prefab to any scene. Go to the LocalizedCaptionsController attached to the captions text and set your desired configuration.
5. To play the captions, you can make an static call from any script: 

```
    LocalizedTextAsset localizedTextAsset;
    LocalizedCaptionsController.Play(localizedTextAsset);
```
6. Preload your LocalizedTextAssed for skip loading deleays:
```
    LocalizedTextAsset localizedTextAsset;
    LocalizedCaptionsController.Preload(localizedTextAsset);
```

&nbsp;
## Installation
Add the custom package to your project via:
- [Unity Asset Store](https://u3d.as/3c32)

or

- Package Manager -> + -> Add package from git URL -> https://github.com/Fenikkel/LocalizedCaptions.git


<p align="center">
    <img src="https://github.com/Fenikkel/SimpleTween/assets/41298931/0f447b8c-85ca-4205-9915-ca7203dc4741" alt="Instructions" height="384">
</p>


&nbsp;
## Technical details

Supported captions file:
- SubRip Subtitle (.srt)

_More file formats coming soon_

&nbsp;
## Compatibility
- Unity Version: 2019.4 (LTS) or higher
- Any pipeline (Build-in, URP, HDRP, etc)

&nbsp;
## Dependencies
- [Localization](https://docs.unity3d.com/Packages/com.unity.localization@1.4/manual/index.html)
- [Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@2.0/manual/index.html)
- [TextMeshPro](https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/manual/index.html)
- [Captions File Parser](https://u3d.as/3bXj)

&nbsp;
## Support
⭐ Star if you like it  
❤️️ Follow me for more
