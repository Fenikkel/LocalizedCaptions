using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/*
    Setup initial language (startup language): 
        - Go to Localization Settings -> Locale Selectors (https://docs.unity3d.com/Packages/com.unity.localization@1.4/manual/LocaleSelector.html)
        - There you can add, remove or remove the order of trying to obtain the startup language. At the app start, if Unity can't obtain a selector because the method doesn't return a locale(null), then it queries the next, until it finds a valid Locale or it reaches the end of the list.
        - The most logical order for this config is: 
            + Command Line Locale Selector (-language=): Useful to debug the app
            + Player Prefs Locale Selector (selected-locale): Saves and load the last sesion language (null if it's first time).
            + System Locale Selector: Get the locale from the running device language
            + Specific Locale Selector: Manually, set up the default locale

    Change locale via unity event:
        - In the inspector, add a new event and drag the LocalizationSettings.asset
        - Select either 'SetSelectedLocale (Locale)' or 'Locale SelectedLocale'. Both works the same.
        - Drag the locale to load.
        - Any locale can be loaded, but if it's not in Localization Settings -> Available Locales, it won't be saved for the next session and the defauld locale will be loaded.
        - If you dont assign a locale it will return an error message (non critical)
 */

namespace Fenikkel.LocalizedCaptions.Example
{

    public class LocaleSelector : MonoBehaviour
    {
        Coroutine _Coroutine;

        #region Singleton
        private static LocaleSelector _Instance = null;
        public static LocaleSelector Instance
        {
            get
            {
                return _Instance;
            }
        }
        #endregion

        private void Awake()
        {
            CheckSingleton();
        }

        public void ChangeLocale(Locale locale)
        {
            if (_Coroutine != null)
            {
                Debug.LogWarning("Already changing the locale");
                return;
            }

            if (LocalizationSettings.AvailableLocales.GetLocale(locale.Identifier) != null)
            {
                _Coroutine = StartCoroutine(SetLocaleCoroutine(locale));
            }
            else
            {
                Debug.LogWarning($"{locale} is not within the: Localization Settings -> Available Locales\nPlease add it to the Available locales to work.");
            }
        }

        public void ChangeLocale(string localeFormatter) // Formatter -> es, ca, en
        {

            if (_Coroutine != null)
            {
                Debug.LogWarning("Already changing the locale");
                return;
            }

            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(localeFormatter);

            if (locale != null)
            {
                _Coroutine = StartCoroutine(SetLocaleCoroutine(locale));
            }
            else
            {
                Debug.LogWarning($"The locale with the formatter </b>{localeFormatter}</b> is not within the: Localization Settings -> Available Locales\nPlease add it to the Available locales to work.");
            }


            _Coroutine = StartCoroutine(SetLocaleCoroutine(locale));
        }

        public void ChangeLocale(int availableLocaleIndex) // LocalizationSettings.AvailableLocales.Locales[localeIndex]
        {
            if (_Coroutine != null)
            {
                Debug.LogWarning("Already changing the locale");
                return;
            }

            if (LocalizationSettings.AvailableLocales.Locales.Count <= availableLocaleIndex)
            {
                Debug.LogWarning($"There are less available locales than the provided index: </b>{availableLocaleIndex}</b>.\nPlease check Localization Settings -> Available Locales");
            }

            Locale locale = LocalizationSettings.AvailableLocales.Locales[availableLocaleIndex];

            _Coroutine = StartCoroutine(SetLocaleCoroutine(locale));
        }

        IEnumerator SetLocaleCoroutine(Locale locale)
        {
            // Wait for localization loaded and ready to use
            yield return LocalizationSettings.InitializationOperation;

            LocalizationSettings.SelectedLocale = locale;

            _Coroutine = null;
        }

        private void CheckSingleton()
        {
            // Check if this instance is a duplicated
            if (_Instance != null && _Instance != this)
            {
                Debug.LogWarning($"Multiple instances of <b>{GetType().Name}</b>\nDestroying the component in <b>{name}</b>.");
                Destroy(this);
                return;
            }

            // Set this instance as the selected
            _Instance = this;
        }
    }
}




