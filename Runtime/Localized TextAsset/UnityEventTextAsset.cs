using System;
using UnityEngine;
using UnityEngine.Events;

namespace LocalizedAudio
{
    [Serializable] // It's mandatory
    public class UnityEventTextAsset : UnityEvent<TextAsset> { }
}