using System;
using UnityEngine;
using UnityEngine.Localization.Components;

/*
    This script it's linked with LocalizedTextAsset and UnityEventTextAsset
    Everyone has to be [Serializable] to work. Also every script has to be an independent script (.cs)
 */

namespace LocalizedAudio
{
    [AddComponentMenu("Localization/Asset/Localize TextAsset Event")]
    [Serializable] // It's mandatory
    public class LocalizeTextAssetEvent : LocalizedAssetEvent<TextAsset, LocalizedTextAsset, UnityEventTextAsset> { }

}