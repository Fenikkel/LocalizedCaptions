using TMPro;
using UnityEngine;

/* 
 * Remember to copy the Rect Transform and TextMeshPro position values
 */

namespace LocalizedAudio
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class CaptionsHighlight : MonoBehaviour
    {
        [SerializeField] LocalizedCaptionsController _LocalizedCaptionsController;
        [Space]
        [SerializeField] Color _HighlightColor = Color.black; // Alpha to 0 disable it

        TextMeshProUGUI _HighlightText;

        void Start()
        {
            if (_LocalizedCaptionsController == null)
            {
                Debug.LogError($"Please assign {typeof(LocalizedCaptionsController)} to work.\n<i>Disabling the behaviour</i>");
                this.enabled = false;
                return;
            }

            _HighlightText = GetComponent<TextMeshProUGUI>();

            _LocalizedCaptionsController.OnTextChanged.AddListener(HighlightText);
        }


        private void HighlightText(string text)
        {

            _HighlightText.text = string.IsNullOrEmpty(text) ? null : $"<mark=#{ColorUtility.ToHtmlStringRGBA(_HighlightColor)}><alpha=#00>a{text}a<alpha=#FF></mark></font>";

            // Set some invisible letters at the start and end of the string to extend the highlight
            //captionString = $"<alpha=#00>a<alpha=#FF>{captionString}<alpha=#00>a<alpha=#FF>";

            /*
             * Put your asset and material preset in: Resources/Fonts & Materials/
             * Or in the custom path configured in: ProjectSettings -> TextMesh Pro -> Settings (http://digitalnativestudios.com/textmeshpro/docs/settings/#font)
             */

            // <font=\"{_CaptionText.font.name}\">
            // Example: <font="LiberationSans SDF">  
        }
    }
}
