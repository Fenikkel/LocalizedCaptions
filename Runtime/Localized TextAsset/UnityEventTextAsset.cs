using System;
using UnityEngine;
using UnityEngine.Events;

namespace LocalizedCaptions
{
    [Serializable] // It's mandatory
    public class UnityEventTextAsset : UnityEvent<TextAsset> { }
}