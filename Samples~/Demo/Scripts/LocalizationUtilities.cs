using UnityEditor.Localization;
using UnityEditor;

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;
using System.Collections.ObjectModel;
using UnityEngine.Localization.Tables;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;
using System;

/*
    USAGE:

        - CreateAssetTableCollection:

            List<Locale> locales = new List<Locale>() { };
            locales.Add(LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(SystemLanguage.Spanish)));
            LocalizationTablesBuilder.CreateAssetTableCollection("StringName", "StringGroupName", Path.Combine(Application.dataPath, "Folder"), locales);    

        - CreateAssetTableEntry<T>:
        
            LocalizationTablesBuilder.CreateAssetTableEntry<Texture2D>(new AssetTableCollection(), "Texture2dEntryName", new Dictionary<LocaleIdentifier, Texture2D>());
            LocalizationTablesBuilder.CreateAssetTableEntry<TextAsset>(new AssetTableCollection(), "TextAssetEntryName", new Dictionary<LocaleIdentifier, TextAsset>());

        - CreateLocalizationSettings:

            LocalizationUtilities.CreateLocalizationSettings("Assets/Settings/Localization", new SystemLanguage[] { SystemLanguage.Spanish, SystemLanguage.English, SystemLanguage.French});

   DISCLAIMER: 
        - This script only works on the Unity Editor. Don't try to make a build with some script referencing it.
 */


public static class LocalizationUtilities
{
    const string LOCALIZATION_FOLDER_PATH = "Assets/Settings/Localization";
    const string LOCALES_FOLDER_PATH = LOCALIZATION_FOLDER_PATH + "/Locales";
    const string TABLES_FOLDER_PATH = LOCALIZATION_FOLDER_PATH + "/Tables";

    public static bool IsLocalizationSettingsReady()
    {

        if (LocalizationEditorSettings.ActiveLocalizationSettings == null)
        {
            Debug.LogError("No active <b>localization settings</b> found in the project. Create one via:\n- Window -> Asset Management -> Localization Tables\n- Right click (in Project) -> Create -> Localization -> Localization Settings");
            return false;
        }

        if (!LocalizationSettings.InitializationOperation.IsDone)
        {
            Debug.LogWarning("Localization settings are not initialized yet.");
            return false;
        }

        return true;
    }

    public static AssetTableCollection CreateAssetTableCollection(string tableCollectionName, string tableCollectionGroup = "Asset Table", string saveFolderPath = TABLES_FOLDER_PATH, List<Locale> locales = null)
    {
        if (!IsLocalizationSettingsReady())
        {
            return null;
        }

        LocalizationSettings localizationSettings = LocalizationEditorSettings.ActiveLocalizationSettings;

        AssetTableCollection assetTableCollection;


        try
        {
            AssetDatabase.StartAssetEditing(); // Start batch editing

            /* Check if the AssetTableCollection already exist*/
            assetTableCollection = LocalizationEditorSettings.GetAssetTableCollection(tableCollectionName);

            if (assetTableCollection != null)
            {
                Debug.Log($"The AssetTableCollection <b>{assetTableCollection}</b> already exist");
                return assetTableCollection;
            }

            /* Check the Locales created */
            if (localizationSettings.GetAvailableLocales().Locales.Count < 1)
            {
                Debug.LogError("No Locales.asset setted in the LocalizationsSettings. Remember to create at least one in the Locale Generator."); // Also create a StringTable.asset, asign the corresponding Locale.asset and assign this StringTable.asset to the StringTableCollection.asset in the \"Tables\" field.
                return null;
            }

            /* Fill parametters with default values */
            if (locales == null)
            {
                locales = localizationSettings.GetAvailableLocales().Locales;
            }
            else
            {
                for (int index = locales.Count - 1; 0 <= index; index--)
                {
                    if (locales[index] == null)
                    {
                        Debug.LogWarning("The provided locales list has nulls");
                        locales.RemoveAt(index);
                    }
                }
            }

            if (!CheckFolderPath(saveFolderPath))
            {
                return null;
            }

            /* Create the table */
            assetTableCollection = LocalizationEditorSettings.CreateAssetTableCollection(tableCollectionName, Path.Combine(saveFolderPath, tableCollectionName), locales);

            assetTableCollection.Group = tableCollectionGroup;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.SetDirty(assetTableCollection); // Refresh the inspector
            assetTableCollection.RefreshAddressables();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Unexpected error during creation of the AssetTableCollection <b>{tableCollectionName}</b>:\n{ex.Message}");
            return null;
        }
        finally
        {
            // Ensure that asset editing is stopped even if an exception occurs
            AssetDatabase.StopAssetEditing();        
        }

        return assetTableCollection;
    }

    public static bool CreateAssetTableEntry<T>(AssetTableCollection assetTableCollection, string entryName, Dictionary<LocaleIdentifier, T> entryValues) where T : UnityEngine.Object
    {
        if (assetTableCollection == null)
        {
            Debug.LogError("Null AssetTableCollection");
            return false;
        }

        ReadOnlyCollection<AssetTable> assetTables = assetTableCollection.AssetTables; // Warning: The LocalizationTables windows may have the tables sorted by the LocalizationSettings.asset in the field "Availiable Locales". The index of the ReadOnlyCollection<AssetTable> is equal to the order of the AssetTable.asset assigned in the field "Tables" in the AssetTableCollection.asset

        if (assetTables.Count < 1)
        {
            Debug.LogError("Add at least one AssetTable to the AssetTableCollection.\nABORTING: Table entry not added.");
            return false;
        }


        try
        {
            AssetDatabase.StartAssetEditing(); // Start batch editing

            LocalizationTable localizationTable;
            AssetTable assetTable;
            AssetTableEntry assetTableEntry;


            string assetPath;
            string assetGuid;

            foreach (KeyValuePair<LocaleIdentifier, T> languageValue in entryValues)
            {
                if (languageValue.Value == null)
                {
                    Debug.LogWarning($"Null detected on value for language <b>{languageValue.Key}</b>");
                    continue;
                }

                /* Get the assetTableEntry of the language */
                localizationTable = assetTableCollection.GetTable(languageValue.Key);

                if (localizationTable == null)
                {
                    Debug.LogWarning($"The AssetTableCollection <b>{assetTableCollection.name}</b> doesn't have a LocalizationTable for the language <b>{languageValue.Key}</b>\n<i>The asset <b>\"{languageValue.Value.name}\"</b> was not assigned</i>");
                    continue;
                }

                assetTable = (AssetTable)localizationTable;
                assetTableEntry = assetTable.GetEntry(entryName);

                /* Get the GUID (Globally Unique Identifier) */
                assetPath = AssetDatabase.GetAssetPath(languageValue.Value);
                assetGuid = AssetDatabase.AssetPathToGUID(assetPath);


                if (assetTableEntry == null)
                {
                    assetTable.AddEntry(entryName, assetGuid);
                }
                else if (assetTableEntry.IsEmpty)
                {
                    Debug.LogWarning($"Missing reference in the key <b>{assetTableEntry.Key}</b> and the table <b>{assetTable.name}</b> in the collection <b>{assetTableCollection.name}</b>\n<i>Problem solved (?)</i>");
                    assetTableEntry.Guid = assetGuid;
                }
                else
                {
                    if (assetTableEntry.Guid != assetGuid)
                    {
                        Debug.LogWarning($"There is already a value in the key <b>{assetTableEntry.Key}</b> and the language <b>{assetTable.LocaleIdentifier}</b> in the collection <b>{assetTableCollection.name}</b>\n<i>Overriding.</i>");
                    }

                    assetTableEntry.Guid = assetGuid;
                }

                assetTableCollection.SetEntryAssetType((TableEntryReference)entryName, languageValue.Value.GetType());

                /* Make the asset Addressable in order to work with LocalizationTables */
                if (!IsAddressable(languageValue.Value))
                {
                    MakeAddressable(languageValue.Value);
                }

            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Unexpected error during creation of the <i>TableEntry</i> for the <i>AssetTableCollection</i> <b>{assetTableCollection.name}</b>:\n{ex.Message}");
            return false;
        }
        finally
        {
            // Ensure that asset editing is stopped even if an exception occurs
            AssetDatabase.StopAssetEditing();

            /* Save the changes */
            EditorUtility.SetDirty(assetTableCollection);
            //EditorUtility.SetDirty(assetTable);
            //EditorUtility.SetDirty(assetTable.SharedData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            assetTableCollection.RefreshAddressables();
        }

        return true;
    }


    public static Locale CreateLocale(SystemLanguage systemLanguage, string saveFolderPath = LOCALES_FOLDER_PATH)
    {
        try
        {
            AssetDatabase.StartAssetEditing();

            if (!CheckFolderPath(saveFolderPath))
            {
                return null;
            }


            string filePath = Path.Combine(saveFolderPath, $"{systemLanguage}.asset");

            if (File.Exists(filePath))
            {
                // Debug.LogWarning($"A Locale with the same name already exist in the path: {filePath}\n<i>Aborting the creation of the locale.</i>");
                return null;
            }


            Locale locale = Locale.CreateLocale(systemLanguage);

            AssetDatabase.CreateAsset(locale, filePath);
            AssetDatabase.SaveAssets();

            MakeAddressable(locale); // Mandatory   

            AssetDatabase.Refresh();

            return locale;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Unexpected error during the Locale creation:\n<i>{ex.Message}</i>");
            return null;
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
    }

    public static LocalizationSettings CreateLocalizationSettings(string saveFolderPath = LOCALIZATION_FOLDER_PATH, SystemLanguage[] desiredLanguages = null)
    {
        try
        {
            AssetDatabase.StartAssetEditing();

            if (!CheckFolderPath(saveFolderPath))
            {
                return null;
            }

            /* Create the LocalizationSettings asset */
            LocalizationSettings localizationSettings = ScriptableObject.CreateInstance<LocalizationSettings>();

            string filePath = Path.Combine(saveFolderPath, $"LocalizationSettings.asset");

            if (File.Exists(filePath))
            {
                Debug.LogWarning($"A LocalizationSettings.asset already exist in the path: {saveFolderPath}\n<i>Overriding.</i>");
            }

            AssetDatabase.CreateAsset(localizationSettings, filePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = localizationSettings;

            MakeAddressable(localizationSettings); // Mandatory


            /* Assign as active settings */
            if (LocalizationEditorSettings.ActiveLocalizationSettings != null)
            {
                Debug.LogWarning("<i>Changing the active LocalizationSettings.</i>");
            }

            LocalizationEditorSettings.ActiveLocalizationSettings = localizationSettings;
 

            /* Add the LocalesProvider */
            if (desiredLanguages != null)
            {
                ILocalesProvider localesProvider = new LocalesProvider();

                Locale currentLocale;

                foreach (SystemLanguage systemLanguage in desiredLanguages)
                {
                    // Debug.Log($"Adding {systemLanguage} to the LocalizationSettings");

                    currentLocale = CreateLocale(systemLanguage);

                    localesProvider.AddLocale(currentLocale);

                }

                localizationSettings.SetAvailableLocales(localesProvider);

                //localizationSettings.SetSelectedLocale(localesProvider.Locales[0]); // We need to build the Addessables I think
            }

            return localizationSettings;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Unexpected error during creation of the <i>LocalizationSettings</i>:\n{ex.Message}");
            return null;
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
    }

    #region Private methods
    private static bool CheckFolderPath(string folderPath)
    {
        if (Directory.Exists(folderPath))
        {
            return true;
        }

        DirectoryInfo directoryInfo = Directory.CreateDirectory(folderPath);

        if (directoryInfo.Exists)
        {
            Debug.Log($"Folder created at the path: <i>{folderPath}</i>");
            return true;
        }

        Debug.LogError($"The path it's not a folder or has an incorrect format: <b>{folderPath}</b>");

        return false;
    }

    private static bool MakeAddressable<T>(this T unityObject, AddressableAssetGroup addressablesGroup = null) where T : UnityEngine.Object
    {

        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

        if (settings == null)
        {
            Debug.LogError("Addressable Asset Settings not found!");
            return false;
        }

        if (IsAddressable(unityObject))
        {
            Debug.LogWarning($"Overriding <b>{unityObject.name}</b> Addressable");
        }

        string assetPath = AssetDatabase.GetAssetPath(unityObject);
        string guid = AssetDatabase.AssetPathToGUID(assetPath);

        if (string.IsNullOrEmpty(guid))
        {
            Debug.LogError("Asset not found: " + assetPath);
            return false;
        }

        if (addressablesGroup == null)
        {
            addressablesGroup = settings.DefaultGroup;
        }


        try
        {
            AssetDatabase.StartAssetEditing();

            /* Add the asset to the addressables */
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, addressablesGroup);
            entry.address = unityObject.name; // Addressable Name (It can be a duplicate)

            /* Save the settings */
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Unexpected error making the <b>{unityObject.name}</b> an <i>Addressable</i>:\n{ex.Message}");
            return false;
        }
        finally
        {
            // Ensure that asset editing is stopped even if an exception occurs
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
        }
    }

    private static bool IsAddressable<T>(this T unityObject) where T : UnityEngine.Object
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

        if (settings == null)
        {
            Debug.LogError("Addressable Asset Settings not found!");
            return false;
        }

        string assetPath = AssetDatabase.GetAssetPath(unityObject);
        string guid = AssetDatabase.AssetPathToGUID(assetPath);

        /* Check if the GUID exists in any group */
        foreach (AddressableAssetGroup group in settings.groups)
        {
            if (group.GetAssetEntry(guid) != null)
            {
                // Debug.Log($"<b>{unityObject.name}</b> with GUID <b>{guid}</b> is in Addressables");
                return true;
            }
        }

        return false;
    }
    #endregion
}
