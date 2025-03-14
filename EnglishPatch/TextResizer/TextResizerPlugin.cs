using BepInEx;
using BepInEx.Logging;
using EnglishPatch.Support;
using HarmonyLib;
using SharedAssembly.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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

    private KeyCode _addResizerAtCursorHotKey = KeyCode.KeypadMinus;
    private KeyCode _addResizerAtCursorHotKey2 = KeyCode.F1;
    //private KeyCode _addResizerAtCursorHotKey2 = KeyCode.Tilde;

    private KeyCode _reloadHotkey = KeyCode.KeypadPlus;
    private KeyCode _reloadHotkey2 = KeyCode.F2;

    private KeyCode _addResizerHotKey = KeyCode.KeypadMultiply;
    private KeyCode _addResizerHotKey2 = KeyCode.F3;

    private string _resizerFolder;

    // Required Static for patches to see it
    public static bool ResizersLoaded = false;
    public static Dictionary<string, TextResizerContract> Resizers = [];

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
        if (UnityInput.Current.GetKeyDown(_reloadHotkey)
            || UnityInput.Current.GetKeyDown(_reloadHotkey2))
        {
            LoadResizers();
            ApplyAllResizers();
            Logger.LogWarning("Resizers Reloaded");
        }

        if (UnityInput.Current.GetKeyDown(_addResizerHotKey)
            || UnityInput.Current.GetKeyDown(_addResizerHotKey2))
        {
            Logger.LogWarning("Adding Resizers for Scene");
            AddTextElementsToResizers(FindAllTextElements());
        }

        if (UnityInput.Current.GetKeyDown(_addResizerAtCursorHotKey) 
            || UnityInput.Current.GetKeyDown(_addResizerAtCursorHotKey2))
        {
            Logger.LogWarning("Adding Resizers at Cursor");
            AddTextElementsToResizers(FindTextElementsUnderCursor(), addUnderCursor: true);
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

                //Logger.LogInfo($"Loading resizer file: {file}");

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
                // Create a new resizer contract for this text element
                var newResizer = new TextResizerContract()
                {
                    Path = path,
                    SampleText = textElement.text,
                    IdealFontSize = textElement.fontSize,
                    //AllowAutoSizing = textElement.enableAutoSizing,
                    AllowWordWrap = textElement.enableWordWrapping,
                    Alignment = textElement.alignment.ToString(),
                    //OverflowMode = textElement.overflowMode.ToString(),
                    //Add More if we want more
                    AllowLeftTrimText = false, //Want to serialise
                };

                foundResizers.Add(newResizer);
            }
        }

        if (foundResizers.Count > 0)
        {
            var serializer = Yaml.CreateSerializer();

            var addedResizersFile = $"{_resizerFolder}/zzAddedResizers.yaml";
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

        // Check if TextMetadata is already attached
        var metadata = textComponent.GetComponent<TextMetadata>();

        // If metadata is not attached, add it and store the original values against it
        if (metadata == null)
        {
            metadata = textComponent.gameObject.AddComponent<TextMetadata>();
            metadata.OriginalX = textComponent.rectTransform.anchoredPosition.x;
            metadata.OriginalY = textComponent.rectTransform.anchoredPosition.y;
            metadata.OriginalWidth = textComponent.rectTransform.sizeDelta.x;
            metadata.OriginalHeight = textComponent.rectTransform.sizeDelta.y;
            metadata.OriginalAlignment = textComponent.alignment;
            metadata.OriginalOverflowMode = textComponent.overflowMode;
            metadata.OriginalAllowWordWrap = textComponent.enableWordWrapping;
            metadata.OriginalAllowAutoSizing = textComponent.enableAutoSizing;
        }

        // Apply position change if needed
        if (resizer.AdjustX != metadata.AdjustX || resizer.AdjustY != metadata.AdjustY)
        {
            //Logger.LogInfo($"Changing Padding: {path}");
            metadata.AdjustX = resizer.AdjustX;
            metadata.AdjustY = resizer.AdjustY;
            var vector = new Vector2(metadata.OriginalX + resizer.AdjustX, metadata.OriginalY + resizer.AdjustY);
            textComponent.rectTransform.anchoredPosition = vector;
        }

        // Apply size change if needed
        if (resizer.AdjustWidth != metadata.AdjustWidth || resizer.AdjustHeight != metadata.AdjustHeight)
        {
            //Logger.LogInfo($"Changing Size: {path}");
            metadata.AdjustWidth = resizer.AdjustWidth;
            metadata.AdjustHeight = resizer.AdjustHeight;

            var size = textComponent.rectTransform.sizeDelta;
            size.x = metadata.OriginalWidth + metadata.AdjustWidth;
            size.y = metadata.OriginalHeight + metadata.AdjustHeight;
            textComponent.rectTransform.sizeDelta = size;
        }


        // Apply the resizing
        if (textComponent.fontSize != resizer.IdealFontSize && resizer.IdealFontSize != null)
        {
            //Logger.LogInfo($"Changed Font Size: {path} from {textComponent.fontSize} to {resizer.IdealFontSize}");
            textComponent.fontSize = resizer.IdealFontSize.Value;
        }

        // Text Alignment
        var alignmentLegit = Enum.TryParse<TextAlignmentOptions>(resizer.Alignment, true, out var alignment);
        if (alignmentLegit && textComponent.alignment != alignment)
        {
            //Logger.LogInfo($"Changed Text Alignment: {path} from {textComponent.alignment} to {resizer.Alignment}");
            textComponent.alignment = alignment;
        }
        else if (textComponent.alignment != metadata.OriginalAlignment)
        {
            textComponent.alignment = metadata.OriginalAlignment;
        }

        var overflowLegit = Enum.TryParse<TextOverflowModes>(resizer.OverflowMode, true, out var overflowMode);
        if (overflowLegit && textComponent.overflowMode != overflowMode)
        {
            //Logger.LogInfo($"Changed Text Overflow: {path} from {textComponent.overflowMode} to {resizer.OverflowMode}");
            textComponent.overflowMode = overflowMode;
        }
        else if (textComponent.overflowMode != metadata.OriginalOverflowMode)
        {
            textComponent.overflowMode = metadata.OriginalOverflowMode;
        }

        // Toggles
        if (resizer.AllowWordWrap.HasValue)
        {
            if (textComponent.enableWordWrapping != resizer.AllowWordWrap.Value)
            {
                //Logger.LogInfo($"Changed Word Wrapping: {path} from {textComponent.enableWordWrapping} to {resizer.AllowWordWrap}");
                textComponent.enableWordWrapping = resizer.AllowWordWrap.Value;
            }
        }
        else if (textComponent.enableWordWrapping != metadata.OriginalAllowWordWrap)
            textComponent.enableWordWrapping = metadata.OriginalAllowWordWrap;

        if (resizer.AllowAutoSizing.HasValue)
        {
            if (textComponent.enableAutoSizing != resizer.AllowAutoSizing.Value)
            {
                //Logger.LogInfo($"Changed AutoSizing: {path} from {textComponent.enableAutoSizing} to {resizer.AllowAutoSizing}");
                textComponent.enableAutoSizing = resizer.AllowAutoSizing.Value;
            }
        }
        else if (textComponent.enableWordWrapping != metadata.OriginalAllowWordWrap)
            textComponent.enableWordWrapping = metadata.OriginalAllowWordWrap;

        // Auto Sizing configuration
        if (textComponent.enableAutoSizing)
        {
            if (resizer.MinFontSize.HasValue && resizer.MinFontSize != textComponent.fontSizeMin)
            {
                //Logger.LogInfo($"Changed MinFont: {path} from {textComponent.fontSizeMin} to {resizer.MinFontSize}");
                textComponent.fontSizeMin = resizer.MinFontSize.Value;
            }

            if (resizer.MaxFontSize.HasValue && resizer.MaxFontSize != textComponent.fontSizeMax)
            {
                //Logger.LogInfo($"Changed MaxFont: {path} from {textComponent.fontSizeMax} to {resizer.MaxFontSize}");
                textComponent.fontSizeMax = resizer.MaxFontSize.Value;
            }
        }

        if (resizer.AllowLeftTrimText)
        {
            //Trim it first so when it initialises it at least trims
            var trimmed = textComponent.text.TrimStart();
            if (textComponent.text != trimmed)
                textComponent.text = trimmed;

            // Take out he behaviour for now to save perfrormance
            //// Only add the behaviour component if it hasn't been added already
            //if (!textComponent.gameObject.TryGetComponent<TextChangedBehaviour>(out var existingBehaviour))
            //    existingBehaviour = textComponent.gameObject.AddComponent<TextChangedBehaviour>();

            //// Set the parameter after adding the component
            //existingBehaviour.SetOptions(resizer);
        }
        //else if (textComponent.gameObject.TryGetComponent<TextChangedBehaviour>(out var textChangeBehavior))
        //{
        //    Destroy(textChangeBehavior);
        //}
    }

    public static TextResizerContract FindAppropriateResizer(string path)
    {
        if (Resizers.TryGetValue(path, out var tryResizer))
            return tryResizer;

        foreach (var resizerPair in Resizers)
        {
            var resizer = resizerPair.Value;

            if (resizer.PathIsRegex)
            {
                if (Regex.IsMatch(path, resizer.Path))
                    return resizer;
                else
                    continue;
            }

            //if (path.StartsWith(resizer.Path))
            //    return resizer;

            if (resizer.Path.Contains("*"))
            {
                // Convert to Regex
                var pattern = resizer.Path
                    .Replace("/", @"\/")
                    .Replace("(", @"\(")
                    .Replace(")", @"\)")
                    .Replace("*", ".*");

                if (Regex.IsMatch(path, pattern))
                    return resizer;
            }
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
}
