using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
#if UNITY_EDITOR
using UnityEditor;
#endif


/* 
 * Suggestion: If you can, preload the tables you gonna use so the first time doen't have a load delay
 * 
 * TextConfig: On TMP UI, disable Wrapping to make the text correctly visible.
 */

[RequireComponent(typeof(TextMeshProUGUI), typeof(LocalizeTextAssetEvent))]
public class LocalizedCaptionsController : MonoBehaviour
{
    [Header("Captions")]
    [SerializeField] bool _FadeTransition = true;
    [Range(0.1f, 0.75f)]
    [SerializeField] float _FadeSpeed = 0.25f; // Seconds we spend to do a fade (in or out)

    [Space(30)]
    public UnityEvent OnStartCaptions;
    [Space]
    public UnityEvent OnEndCaptions;
    [Space]
    public UnityEvent<string> OnTextChanged;

    LocalizeTextAssetEvent _LocalizeTextAssetEvent;
    TextMeshProUGUI _CaptionText;
    Queue<Caption> _CaptionsQueue = new Queue<Caption>();
    CaptionDataParser _CaptionsParser = new CaptionDataParser();
    Coroutine _CaptionsCoroutine;
    Coroutine _PreloadCoroutine;
    Coroutine _FadeCoroutine;
    UnityAction<TextAsset> _OnLanguageChange;

    public bool FadeTransition { get { return _FadeTransition; } set { _FadeTransition = value; } }

    #region Singleton
    private static LocalizedCaptionsController _Instance = null;
    public static LocalizedCaptionsController Instance
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

        Init();
    }

    #region Controls
    public static Coroutine Preload(LocalizedTextAsset localizedTextAsset)
    {
        if (Instance == null)
        {
            Debug.LogWarning($"No {typeof(LocalizedCaptionsController)} on the scene ");
            return null;
        }

        return Instance.PreloadCaptions(localizedTextAsset);

    }

    public Coroutine PreloadCaptions(LocalizedTextAsset localizedTextAsset)
    {
        if (localizedTextAsset == null || localizedTextAsset.IsEmpty)
        {
            Debug.LogWarning($"Empty <b>LocalizedTextAsset</b> variable.");
            return null;
        }

        if (_PreloadCoroutine != null)
        {
            StopCoroutine(_PreloadCoroutine);
            _PreloadCoroutine = null;
            Debug.LogWarning("Another preload was on course");
        }

        _PreloadCoroutine = StartCoroutine(PreloadCaptionsCoroutine(localizedTextAsset));

        return _PreloadCoroutine;
    }

    private IEnumerator PreloadCaptionsCoroutine(LocalizedTextAsset localizedTextAsset)
    {
        _LocalizeTextAssetEvent.AssetReference = localizedTextAsset;

        // CurrentLoadingOperationHandle.Status --> None
        yield return new WaitUntil(() => localizedTextAsset.CurrentLoadingOperationHandle.IsDone); // Throws an error if the file format it's not the correct
        // CurrentLoadingOperationHandle.Status --> Success

        // TODO: Make it async to improve the performance
        AssetTable assetTable = LocalizationSettings.AssetDatabase.GetTable(localizedTextAsset.TableReference);
        SharedTableData.SharedTableEntry sharedTableEntry = assetTable.SharedData.GetEntryFromReference(localizedTextAsset.TableEntryReference);
        string entryName = sharedTableEntry != null ? sharedTableEntry.Key : null;
        string tableCollectionName = localizedTextAsset.TableReference.TableCollectionName;

        try
        {
            if (localizedTextAsset.CurrentLoadingOperationHandle.Status == AsyncOperationStatus.Succeeded)
            {
                if (sharedTableEntry == null)
                {
                    Debug.LogWarning($"Missing reference to the <b>TableEntry</b> in the table \"<b>{tableCollectionName}</b>\".\n<b><i>You deleted the TableEntry</i></b>");
                }
                // DefaultAsset result = localizedDefaultAsset.CurrentLoadingOperationHandle.Result;
            }
            else
            {
                Debug.LogWarning($"Error to load the captions in the table \"<b>{tableCollectionName}</b>\" in the entry \"<b>{entryName}</b>\" in the language \"<b>{LocalizationSettings.SelectedLocale}</b>\".");
            }
        }
        catch (Exception ex)
        {
            // Debug.LogError($"Exception message: {ex.Message}");
            Debug.LogWarning($"Seems that entry \"<b>{entryName}</b>\" in your table \"<b>{tableCollectionName}</b>\" have some missing/broken references or the asset it's not Addressable.\n<b><i>You may need to make the asset Addressable or redo the table.</i></b>");

            _LocalizeTextAssetEvent.AssetReference = null;
            yield break;

        }

        // Debug.Log("Preloaded");

#if UNITY_EDITOR
        EditorUtility.SetDirty(_LocalizeTextAssetEvent); // Refresh the inspector
#endif

        _PreloadCoroutine = null;
    }

    public static void Play(LocalizedTextAsset localizedTextAsset)
    {
        if (Instance == null)
        {
            Debug.LogWarning($"No {typeof(LocalizedCaptionsController)} on the scene ");
            return;
        }

        Instance.PlayCaptions(localizedTextAsset);

    }

    public void PlayCaptions(LocalizedTextAsset localizedTextAsset)
    {
        StopCaptions();

        _CaptionsCoroutine = StartCoroutine(PlayCaptionsCoroutine(localizedTextAsset));
    }

    private IEnumerator PlayCaptionsCoroutine(LocalizedTextAsset localizedTextAsset)
    {
        if (localizedTextAsset.IsEmpty)
        {
            Debug.LogWarning($"Empty <b>LocalizedTextAsset</b> variable.");
            yield break;
        }

        yield return PreloadCaptions(localizedTextAsset);

        if (_LocalizeTextAssetEvent.AssetReference == null || _LocalizeTextAssetEvent.AssetReference.IsEmpty)
        {
            Debug.LogWarning($"Something went wrong during the load of <b>{localizedTextAsset}</b>.");
            yield break;
        }

        OnStartCaptions.Invoke();

        // Init setup
        _LocalizeTextAssetEvent.OnUpdateAsset.AddListener(_OnLanguageChange);
        ChangeTextAsset(_LocalizeTextAssetEvent.AssetReference.LoadAsset()); // localizedTextAsset.LoadAsset()

        float currentTime = 0f;
        float entryTime;
        float exitTime;
        float onScreenTime;
        float fullAlphaTime;
        float fadeTime;
        Caption currentCaption;
        float fadeDeltaTime;
        string captionText;

        while (0 < _CaptionsQueue.Count)
        {
            currentCaption = _CaptionsQueue.Dequeue();

            entryTime = (float)currentCaption.EntryTime.TotalSeconds;
            exitTime = (float)currentCaption.ExitTime.TotalSeconds;
            onScreenTime = exitTime - entryTime;

            if (exitTime < currentTime)
            {
                continue; // Go for next
            }

            while (currentTime < entryTime)
            {
                currentTime += Time.deltaTime;
                yield return null;
            };

            captionText = string.IsNullOrEmpty(currentCaption.SecondRow) ? currentCaption.FirstRow : $"{currentCaption.FirstRow}\n{currentCaption.SecondRow}";

            ChangeCaptionsText(captionText);
            OnTextChanged.Invoke(captionText);

            onScreenTime -= (currentTime - entryTime); // Take in account the residual time lost

            fadeDeltaTime = Time.time;

            if (_FadeTransition)
            {
                fadeTime = 0f < (onScreenTime - (_FadeSpeed * 2f)) ? _FadeSpeed : (onScreenTime / 2f);
                fullAlphaTime = Mathf.Max(0f, onScreenTime - (fadeTime * 2f));

                if (onScreenTime - (_FadeSpeed * 2f) <= 0f)
                {
                    Debug.LogWarning("Caption time lapse too tight for fade");

                }

                yield return Fade(_CaptionText, 1f, fadeTime);

                yield return new WaitForSeconds(fullAlphaTime);

                yield return Fade(_CaptionText, 0f, fadeTime);
            }
            else
            {
                _CaptionText.alpha = 1.0f;
                yield return new WaitForSeconds(onScreenTime);
                _CaptionText.alpha = 0.0f;
            }

            fadeDeltaTime = Time.time - fadeDeltaTime;

            currentTime += fadeDeltaTime;
            OnTextChanged.Invoke(string.Empty);
        }

        OnStartCaptions.Invoke();

        ResetVariables();
        _CaptionsCoroutine = null;
    }

    public static void Stop()
    {
        if (Instance == null)
        {
            Debug.LogWarning($"No {typeof(LocalizedCaptionsController)} on the scene ");
            return;
        }

        Instance.StopCaptions();
    }

    public void StopCaptions()
    {
        if (_CaptionsCoroutine != null)
        {
            Debug.LogWarning("Another captions were playing.");

            StopCoroutine(_CaptionsCoroutine);
            _CaptionsCoroutine = null;
        }

        ResetVariables();
    }

    private void ResetVariables()
    {
        _LocalizeTextAssetEvent.OnUpdateAsset.RemoveListener(_OnLanguageChange);

        if (_FadeCoroutine != null)
        {
            StopCoroutine(_FadeCoroutine);
            _FadeCoroutine = null;
        }

        _CaptionText.text = string.Empty;
        _CaptionText.alpha = 0f;
        _CaptionsQueue.Clear();
    }
    #endregion

    #region Update events

    private void ChangeTextAsset(TextAsset textAsset)
    {
        if (textAsset == null)
        {
            Debug.LogWarning($"The language <b>{LocalizationSettings.SelectedLocale}</b> doesn't have a <b>TextAsset</b> (captions file) asigned");
            _CaptionsQueue.Clear();
            return;
        }

        _CaptionsQueue = _CaptionsParser.Parse(AssetDatabase.GetAssetPath(textAsset));

        if (_CaptionsQueue == null)
        {
            _CaptionsQueue = new Queue<Caption>();
            Debug.LogWarning($"Failed to get the captions of <b>{textAsset.name}</b>.\n<i>Continuing without captions.</i>");
            return;
        }
    }

    private void ChangeCaptionsText(string captionString)
    {
        if (string.IsNullOrEmpty(captionString))
        {
            //Debug.LogWarning("Empty string");
            //_CaptionText.text = "<b>EMPTY</b>";
            return;
        }

        _CaptionText.text = captionString;

        //Debug.Log($"Caption changed to: {_CaptionText.text}");
    }
    #endregion

    #region Fade
    private Coroutine Fade(TextMeshProUGUI text, float targetAlpha = 1f, float fadeTime = 1f)
    {
        if (_FadeCoroutine != null)
        {
            StopCoroutine(_FadeCoroutine);
            _FadeCoroutine = null;
        }
        _FadeCoroutine = StartCoroutine(FadeCoroutine(text, targetAlpha, fadeTime));
        return _FadeCoroutine;
    }

    private IEnumerator FadeCoroutine(TextMeshProUGUI text, float targetAlpha = 1f, float fadeTime = 1f)
    {
        /* Check values */
        if (text == null)
        {
            Debug.LogWarning("Text <b>not initialized</b>. It is null");
            yield break;
        }

        targetAlpha = Mathf.Clamp01(targetAlpha);


        if (text.color.a == targetAlpha)
        {
            Debug.LogWarning("Trying to fade with the <b>same alpha</b>");
            yield break;
        }

        fadeTime = Mathf.Max(0f, fadeTime); // Force positive values

        /* Start tween */
        Color originalColor = text.color;
        Color targetColor = new Color(originalColor.r, originalColor.g, originalColor.b, targetAlpha);
        float progress = 0f;
        float step;

        do
        {
            // Add the new tiny extra amount
            progress += Time.deltaTime / fadeTime;
            progress = Mathf.Clamp01(progress);

            // Apply changes

            step = -(Mathf.Cos(Mathf.PI * progress) - 1.0f) / 2.0f; // Convert to ease-in ease-out interpolation
            text.color = Color.Lerp(originalColor, targetColor, step);

            // Wait for the next frame
            yield return null;
        }
        while (progress < 1f); // Keep moving while we don't reach any goal

        _FadeCoroutine = null;

    }

    #endregion

    #region Init
    private void Init()
    {
        _LocalizeTextAssetEvent = GetComponent<LocalizeTextAssetEvent>();

        _CaptionText = GetComponent<TextMeshProUGUI>();

        /* Text config recommendations */
        if (_CaptionText.enableWordWrapping)
        {
            Debug.Log("It is recommended to disable <b>Wrapping</b> on the text captions to fully visualize the captions.\n<i>Configure other options like Canvas Scaler to solve text size problems.</i>");
        }

        if (_CaptionText.verticalAlignment != VerticalAlignmentOptions.Bottom)
        {
            Debug.Log("It is recommended to have the text vertically aligned to the bottom.\n<i>Gives more visual space in case there is only one line of captions.</i>");
        }

        if (_CaptionText.horizontalAlignment != HorizontalAlignmentOptions.Center)
        {
            Debug.Log("It is recommended to have the text horizontally aligned to the center.");
        }

        /* Automatization */
        if (_LocalizeTextAssetEvent.OnUpdateAsset.GetPersistentEventCount() != 0)
        {
            Debug.LogWarning("Be careful, you added a event to OnUpdateAsset, we ALREADY update the TextAsset via script. Count: " + _LocalizeTextAssetEvent.OnUpdateAsset.GetPersistentEventCount());
        }

        /* Update events */
        _OnLanguageChange = ChangeTextAsset;

        /* Hide Text */
        _CaptionText.alpha = 0f;
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
    #endregion

}
