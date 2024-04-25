using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using LocalizedCaptions;

namespace Fenikkel.LocalizedCaptions.Example
{
    public class LocalizedCaptionsExample : MonoBehaviour
    {
        const string TABLE_COLLECTION_NAME = "CaptionsDemoTable";
        const string TABLE_ENTRY_NAME = "DemoEntry";

        [SerializeField] LocalizedTextAsset _LocalizedTextAsset;

        [SerializeField] TextAsset _EnglishCaptions;
        [SerializeField] TextAsset _FrenchCaptions;
        [SerializeField] TextAsset _SpanishCaptions;

        Coroutine _Coroutine;

        private void Awake()
        {
            
            InitAndSetupLocalization();
        }

        public void PreloadLocalizedTextAsset()
        {
            if (_LocalizedTextAsset.IsEmpty)
            {
                Debug.LogWarning("Please, create a Asset Table and asign an entry of a TextAsset in _LocalizedTextAsset");
            }

            LocalizedCaptionsController.Preload(_LocalizedTextAsset); // Or -> LocalizedCaptionsController.Instance.PreloadCaptions(_LocalizedTextAsset);
        }

        public void PlayLocalizedTextAsset()
        {
            if (_LocalizedTextAsset.IsEmpty)
            {
                Debug.LogWarning("Please, create a Asset Table and asign an entry of a TextAsset in _LocalizedTextAsset");
            }

            LocalizedCaptionsController.Play(_LocalizedTextAsset); // Or -> LocalizedCaptionsController.Instance.PlayCaptions(_LocalizedTextAsset);
        }

        public void StopLocalizedCaptions()
        {
            LocalizedCaptionsController.Stop(); // Or -> LocalizedCaptionsController.Instance.Stop();
        }

        private void InitAndSetupLocalization() 
        {
            // Localization settings
            LocalizationSettings localizationSettings = LocalizationUtilities.CreateLocalizationSettings("Assets/Settings/Localization", new SystemLanguage[] { SystemLanguage.Spanish, SystemLanguage.English, SystemLanguage.French });

            if (localizationSettings != null)
            {
                if (_Coroutine != null)
                {
                    StopCoroutine(_Coroutine);
                }

                _Coroutine = StartCoroutine(CreateTables(localizationSettings));
            }                    
        }

        IEnumerator CreateTables(LocalizationSettings localizationSettings) 
        {
            yield return new WaitUntil(() => localizationSettings.GetInitializationOperation().IsDone);

            List<Locale> locales = localizationSettings.GetAvailableLocales().Locales;

            AssetTableCollection assetTableCollection = LocalizationUtilities.CreateAssetTableCollection(TABLE_COLLECTION_NAME, "CaptionsDemo", "Assets/Settings/Localization/Tables", locales);

            Dictionary<LocaleIdentifier, TextAsset> dictionary = new Dictionary<LocaleIdentifier, TextAsset>();
            dictionary.Add(new LocaleIdentifier(SystemLanguage.Spanish), _SpanishCaptions);
            dictionary.Add(new LocaleIdentifier(SystemLanguage.French), _FrenchCaptions);
            dictionary.Add(new LocaleIdentifier(SystemLanguage.English), _EnglishCaptions);

            bool success = LocalizationUtilities.CreateAssetTableEntry<TextAsset>(assetTableCollection, TABLE_ENTRY_NAME, dictionary);

            if (success)
            {
                _LocalizedTextAsset.SetReference(assetTableCollection.TableCollectionNameReference, TABLE_ENTRY_NAME);
                EditorUtility.SetDirty(this);
            }

            _Coroutine = null;
        }
    }
}
