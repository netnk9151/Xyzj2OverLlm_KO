using BepInEx;
using BepInEx.Logging;
using EnglishPatch.Support;
using HarmonyLib;
using SharedAssembly.Contracts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

namespace EnglishPatch;

/// <summary>
/// Put dicey stuff in here that might crash the plugin - so it doesnt crash the existing plugins
/// </summary>
[BepInPlugin($"{MyPluginInfo.PLUGIN_GUID}.TextResizer", "TextResizer", MyPluginInfo.PLUGIN_VERSION)]
internal class TextResizerPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private KeyCode _addResizerHotKey = KeyCode.KeypadMultiply;
    private KeyCode _addResizerAtCursorHotKey = KeyCode.KeypadMinus;
    private KeyCode _copyResizerToCursorHotKey = KeyCode.KeypadPeriod; //Not working needs debugging
    private KeyCode _reloadHotkey = KeyCode.KeypadPlus;
    private string _resizerFolder;

    // Required Static for patches to see it
    public static bool ResizersLoaded = false;
    public static Dictionary<string, TextResizerContract> Resizers = [];

    public static TextResizerContract LastResizerContract = null;

    private void Awake()
    {
        Logger = base.Logger;

        Harmony.CreateAndPatchAll(typeof(TextResizerPlugin));
        Logger.LogWarning($"TextResizer Plugin should be patched!");

        _resizerFolder = Path.Combine(Paths.BepInExRootPath, "resizers");
        if (!Directory.Exists(_resizerFolder))
            Directory.CreateDirectory(_resizerFolder);

        LoadResizers();
        Logger.LogWarning($"TextResizer Plugin Loaded!");
    }

    internal void Update()
    {
        if (UnityInput.Current.GetKeyDown(_reloadHotkey))
        {
            LoadResizers();
            ApplyAllResizers();
            Logger.LogWarning("Resizers Reloaded");
        }

        if (UnityInput.Current.GetKeyDown(_addResizerHotKey))
        {
            Logger.LogWarning("Adding Resizers for Scene");
            AddTextElementsToResizers(FindAllTextElements());
        }

        if (UnityInput.Current.GetKeyDown(_addResizerAtCursorHotKey))
        {
            Logger.LogWarning("Adding Resizers at Cursor");
            AddTextElementsToResizers(FindTextElementsUnderCursor(), addUnderCursor: true);
        }

        if (UnityInput.Current.GetKeyDown(_copyResizerToCursorHotKey))
        {
            Logger.LogWarning("Copying Last Cursor Resizer to Cursor");
            AddTextElementsToResizers(FindTextElementsUnderCursor(), copyUnderCursor: true);
        }
    }

    public void LoadResizers()
    {
        ResizersLoaded = false;

        var deserializer = Yaml.CreateDeserializer();
        Resizers.Clear();

        foreach (var file in Directory.EnumerateFiles(_resizerFolder))
        {
            try
            {
                if (!file.EndsWith("yaml"))
                    continue;

                Logger.LogInfo($"Loading resizer file: {file}");

                var content = File.ReadAllText(file);
                if (string.IsNullOrWhiteSpace(content))
                    continue;

                var newResizers = deserializer.Deserialize<List<TextResizerContract>>(content);

                AddFoundResizers(newResizers);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error Loading resizer '{file}': {ex}");
            }
        }

        ResizersLoaded = true;
    }

    private void AddFoundResizers(List<TextResizerContract> newResizers)
    {
        foreach (var newResizer in newResizers)
            if (!Resizers.ContainsKey(newResizer.Path))
                Resizers.Add(newResizer.Path, newResizer);
    }

    public TextMeshProUGUI[] FindTextElementsUnderCursor()
    {
        // Get the current mouse position
        var mousePosition = UnityInput.Current.mousePosition;

        // Create a 10x10 pixel area around the cursor (20 pixel buffer on each side)
        var cursorArea = new Rect(mousePosition.x - 10, mousePosition.y - 10, 20, 20);

        // Find all TextMeshProUGUI components in the scene
        var textElements = FindObjectsOfType<TextMeshProUGUI>();

        var responseElements = new List<TextMeshProUGUI>();

        foreach (TextMeshProUGUI textElement in textElements)
        {
            // Get the RectTransform to check if it contains the cursor position
            var rectTransform = textElement.rectTransform;
            if (rectTransform == null) continue;

            // Check if the text element's screen rect overlaps with our cursor area
            Canvas canvas = textElement.canvas;
            if (canvas == null) continue;

            // Get the screen rect of the text element
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Rect screenRect = RectTransformUtility.PixelAdjustRect(rectTransform, canvas);

            // Convert the rect to screen coordinates if not in overlay mode
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && camera != null)
            {
                Vector3[] corners = new Vector3[4];
                rectTransform.GetWorldCorners(corners);

                // Convert world corners to screen points
                Vector2 min = camera.WorldToScreenPoint(corners[0]);
                Vector2 max = camera.WorldToScreenPoint(corners[2]);
                screenRect = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
            }

            // Check if the cursor area overlaps with the text element's screen rect
            if (screenRect.Overlaps(cursorArea))
                responseElements.Add(textElement);
        }

        return responseElements.ToArray();
    }

    public static TextMeshProUGUI[] FindAllTextElements()
    {
        // Find all TextMeshProUGUI components in the scene
        return FindObjectsOfType<TextMeshProUGUI>();
    }

    public void AddTextElementsToResizers(TextMeshProUGUI[] textElements, bool addUnderCursor = false, bool copyUnderCursor = false)
    {
        var foundResizers = new List<TextResizerContract>();

        foreach (TextMeshProUGUI textElement in textElements)
        {
            // Log information about the text element
            var path = ObjectHelper.GetGameObjectPath(textElement.gameObject);
            //Logger.LogInfo($"Found text element: {path}");

            if (!Resizers.ContainsKey(path))
            {
                TextResizerContract newResizer;

                if (copyUnderCursor && LastResizerContract != null)
                {
                    newResizer = LastResizerContract.ShallowClone();
                    newResizer.Path = path;
                }
                else
                {
                    // Create a new resizer contract for this text element
                    newResizer = new TextResizerContract()
                    {
                        Path = path,
                        SampleText = textElement.text,
                        IdealFontSize = textElement.fontSize,
                        AllowAutoSizing = textElement.enableAutoSizing,
                        AllowWordWrap = textElement.enableWordWrapping,
                        Alignment = textElement.alignment.ToString(),
                        //Add More if we want more
                    };

                    if (addUnderCursor && !copyUnderCursor)
                        LastResizerContract = newResizer;
                }

                foundResizers.Add(newResizer);
            }
            else if (copyUnderCursor && LastResizerContract != null)
            {
                var newResizer = LastResizerContract.ShallowClone();
                newResizer.Path = path;

                Resizers[path] = newResizer;
                ApplyAllResizers(); // Reload them
            }
        }

        if (foundResizers.Count > 0)
        {
            var serializer = Yaml.CreateSerializer();

            var addedResizersFile = $"{_resizerFolder}/AddedResizers.yaml";
            var newText = serializer.Serialize(foundResizers);

            Logger.LogWarning($"Writing to {addedResizersFile}");

            if (!File.Exists(addedResizersFile))
                File.WriteAllText(addedResizersFile, newText);
            else
                File.AppendAllText(addedResizersFile, newText);

            AddFoundResizers(foundResizers);
        }
        else
        {
            Logger.LogInfo("No new text elements found in scene");
        }
    }

    public static void ApplyResizing(TextMeshProUGUI textComponent)
    {
        if (textComponent == null) return;

        string path = ObjectHelper.GetGameObjectPath(textComponent.gameObject);

        var resizer = FindAppropriateResizer(path);
        if (resizer == null)
            return;

        // Apply the resizing
        if (textComponent.fontSize != resizer.IdealFontSize && resizer.IdealFontSize > 0)
        {
            Logger.LogInfo($"Changed Font Size: {path} from {textComponent.fontSize} to {resizer.IdealFontSize}");
            textComponent.fontSize = resizer.IdealFontSize;
        }

        if (resizer.AdjustX != 0 || resizer.AdjustY != 0 || resizer.adjustWidth != 0 || resizer.adjustHeight != 0)
        {
            // Check if TextMetadata is already attached
            var metadata = textComponent.GetComponent<TextMetadata>();
            RectTransform container = textComponent.rectTransform.parent.GetComponent<RectTransform>();

            // If metadata is not attached, add it and store the original X position
            if (metadata == null)
            {
                metadata = textComponent.gameObject.AddComponent<TextMetadata>();
                metadata.OriginalX = textComponent.rectTransform.anchoredPosition.x;
                metadata.OriginalY = textComponent.rectTransform.anchoredPosition.y;

                metadata.OriginalMarginLeft = textComponent.margin.x;
                //metadata.OriginalMarginRight = textComponent.margin.y;
                metadata.OriginalMarginTop = textComponent.margin.z;
                //metadata.OriginalMarginBottom = textComponent.margin.w;
            }

            if (resizer.AdjustX != metadata.AdjustX || resizer.AdjustY != metadata.AdjustY)
            {
                Logger.LogInfo($"Changing Padding: {path}");
                metadata.AdjustX = resizer.AdjustX;
                metadata.AdjustY = resizer.AdjustY;
                var vector = new Vector2(metadata.OriginalX + resizer.AdjustX, metadata.OriginalY + resizer.AdjustY);
                textComponent.rectTransform.anchoredPosition = vector;
            }

            if (resizer.adjustWidth != metadata.MarginLeft || resizer.adjustHeight != metadata.MarginBottom)
            {
                Logger.LogInfo($"Changing Size: {path}");
                metadata.MarginLeft = resizer.adjustWidth;
                metadata.MarginBottom = resizer.adjustHeight;

                //var size = container.sizeDelta;
                //size.x = size.x + resizer.ModifyWidth;
                //size.y = size.y + resizer.ModifyHeight;
                ////var size = new Vector2(metadata.OriginalWidth + metadata.ModifyWidth, metadata.OriginalHeight + metadata.ModifyHeight);
                //container.sizeDelta = size;

                textComponent.margin = new Vector4(
                    metadata.OriginalMarginLeft + metadata.MarginLeft,
                    metadata.OriginalMarginRight + metadata.MarginLeft,
                    metadata.OriginalMarginTop + metadata.MarginTop,
                    metadata.OriginalMarginBottom + metadata.MarginTop);
            }
        }

        // Text Alignment
        var alignmentLegit = Enum.TryParse<TextAlignmentOptions>(resizer.Alignment, true, out var alignment);
        if (alignmentLegit && textComponent.alignment != alignment)
        {
            Logger.LogInfo($"Changed Text Alignment: {path} from {textComponent.alignment} to {resizer.Alignment}");
            textComponent.alignment = alignment;
        }

        // Toggles
        if (textComponent.enableWordWrapping != resizer.AllowWordWrap)
        {
            Logger.LogInfo($"Changed Word Wrapping: {path} from {textComponent.enableWordWrapping} to {resizer.AllowWordWrap}");
            textComponent.enableWordWrapping = resizer.AllowWordWrap;
        }

        if (textComponent.enableAutoSizing != resizer.AllowAutoSizing)
        {
            Logger.LogInfo($"Changed AutoSizing: {path} from {textComponent.enableAutoSizing} to {resizer.AllowAutoSizing}");
            textComponent.enableAutoSizing = resizer.AllowAutoSizing;
        }

        // Auto Sizing configuration
        if (resizer.AllowAutoSizing)
        {
            if (resizer.MinFontSize > 0 && resizer.MinFontSize != textComponent.fontSizeMin)
            {
                Logger.LogInfo($"Changed MinFont: {path} from {textComponent.fontSizeMin} to {resizer.MinFontSize}");
                textComponent.fontSizeMin = resizer.MinFontSize;
            }

            if (resizer.MaxFontSize > 0 && resizer.MaxFontSize != textComponent.fontSizeMax)
            {
                Logger.LogInfo($"Changed MaxFont: {path} from {textComponent.fontSizeMax} to {resizer.MaxFontSize}");
                textComponent.fontSizeMax = resizer.MaxFontSize;
            }
        }

        if (resizer.AllowLeftTrimText)
        {
            //Trim it first so when it initialises it at least trims
            var trimmed = textComponent.text.TrimStart();
            if (textComponent.text != trimmed)
                textComponent.text = trimmed;

            Logger.LogInfo($"Adding Behaviour");

            // Only add the behaviour component if it hasn't been added already
            if (!textComponent.gameObject.TryGetComponent<TextChangedBehaviour>(out var existingBehaviour))
                existingBehaviour = textComponent.gameObject.AddComponent<TextChangedBehaviour>();

            // Set the parameter after adding the component
            existingBehaviour.SetOptions(resizer);
        }
        else if (textComponent.gameObject.TryGetComponent<TextChangedBehaviour>(out var textChangeBehavior))
        {
            Destroy(textChangeBehavior);
        }
    }

    public static TextResizerContract FindAppropriateResizer(string path)
    {
        if (Resizers.TryGetValue(path, out var tryResizer))
            return tryResizer;

        foreach (var resizerPair in Resizers)
        {
            var resizer = resizerPair.Value;

            if (resizer.AllowPartialPath && path.Contains(resizer.Path))
                return resizer;
            else if (path.StartsWith(resizer.Path))
                return resizer;
        }

        return null;
    }

    public static void ApplyAllResizers()
    {
        foreach (var textElement in FindAllTextElements())
            ApplyResizing(textElement);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(GameObject), nameof(GameObject.SetActive), [typeof(bool)])]
    public static void Postfix_GameObject_SetActive(GameObject __instance)
    {
        if (!ResizersLoaded)
            return;

        //TODO: This should be most efficient but we could use Object.Instantiate
        //to get it at as the objects created. But maybe there is post processing occuring after.
        var items = __instance.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (var item in items)
            ApplyResizing(item);
    }


    //// Patch the Awake method
    //[HarmonyPostfix, HarmonyPatch(typeof(TextMeshProUGUI), "Awake")]
    //public static void Awake_Postfix(TextMeshProUGUI __instance)
    //{
    //    if (!ResizersLoaded)
    //        return;

    //    try
    //    {
    //        Logger.LogInfo("TextMeshProUGUI.Awake");
    //        ApplyResizing(__instance);
    //    }
    //    catch (Exception ex)
    //    {
    //        Logger.LogError($"Error in Awake_Postfix: {ex}");
    //    }
    //}

    //// Patch OnEnable for components that might be reactivated
    //[HarmonyPostfix, HarmonyPatch(typeof(TextMeshProUGUI), "OnEnable")]
    //public static void OnEnable_Postfix(TextMeshProUGUI __instance)
    //{
    //    if (!ResizersLoaded)
    //        return;

    //    try
    //    {
    //        ApplyResizing(__instance);
    //    }
    //    catch (Exception ex)
    //    {
    //        Logger.LogError($"Error in OnEnable_Postfix: {ex}");
    //    }
    //}

    //// Patch SetText to catch dynamic text changes
    //[HarmonyPostfix, HarmonyPatch(typeof(TMP_Text), "SetText", new Type[] { typeof(string) })]
    //public static void SetText_Postfix(TMP_Text __instance)
    //{
    //    try
    //    {
    //        if (__instance is TextMeshProUGUI textComponent)
    //        {
    //            ApplyResizing(textComponent);
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Logger.LogError($"Error in SetText_Postfix: {ex}");
    //    }
    //}
}

// This component will monitor the text changes
public class TextChangedBehaviour : MonoBehaviour
{
    private string _lastText;
    private TextMeshProUGUI _textComponent;
    private TextResizerContract _contract;
    private Coroutine _monitoringCoroutine;

    public void SetOptions(TextResizerContract contract)
    {
        _contract = contract;
    }

    private void Awake()
    {
        _textComponent = GetComponent<TextMeshProUGUI>();
        if (_textComponent != null)
        {
            _lastText = _textComponent.text;
            StartMonitoring(_textComponent);
        }
    }

    public void StartMonitoring(TextMeshProUGUI textComponent)
    {
        if (_monitoringCoroutine != null)
        {
            // If monitoring is already started, do not start it again
            return;
        }

        _textComponent = textComponent;
        _lastText = textComponent.text;

        _monitoringCoroutine = StartCoroutine(MonitorTextChanges());
    }

    private IEnumerator MonitorTextChanges()
    {
        while (_textComponent != null)
        {
            if (_textComponent.text != _lastText)
            {
                _lastText = _textComponent.text;


                TextResizerPlugin.Logger.LogInfo($"Trimming");
                if (_contract.AllowLeftTrimText)
                    _textComponent.text = _textComponent.text.TrimStart(); // Trim leading spaces
            }

            yield return null; // Check every frame
        }
    }
}

public class TextMetadata : MonoBehaviour
{
    public float OriginalX;
    public float OriginalY;
    public float AdjustX;
    public float AdjustY;

    public float OriginalMarginLeft;
    public float OriginalMarginRight;
    public float OriginalMarginTop;
    public float OriginalMarginBottom;
    public float MarginLeft;
    public float MarginRight;
    public float MarginTop;
    public float MarginBottom;
}