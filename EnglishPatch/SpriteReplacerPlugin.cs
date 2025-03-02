using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using XUnity.ResourceRedirector;

namespace EnglishPatch
{
    [BepInPlugin($"{MyPluginInfo.PLUGIN_GUID}.SpriteReplacer", "Replace Sprites in game", MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("gravydevsupreme.xunity.resourceredirector")]
    public class SpriteReplacerPlugin : BaseUnityPlugin
    {
        // Internal variables
        internal static new ManualLogSource Logger;
        private Dictionary<string, byte[]> cachedReplacements = new Dictionary<string, byte[]>();
        private string spritesPath;

        private ConfigEntry<string> _logWhenAssetContains;

        private void Awake()
        {
            Logger = base.Logger;
            spritesPath = Path.Combine(Paths.BepInExRootPath, "sprites");

            _logWhenAssetContains = Config.Bind("General",
                        "LogWhenAssetContains",
                        "loginviewnew",
                        "Log in the console when any part of the assetname or path includes the value provided");


            // Cache all textures from the replacement folder
            CacheReplacementTextures();

            // Register our resource redirector
            ResourceRedirection.EnableSyncOverAsyncAssetLoads();

            ResourceRedirection.RegisterAssetLoadedHook(
                behaviour: HookBehaviour.OneCallbackPerResourceLoaded,
                priority: 1000,
                action: OnAssetLoaded);

            Logger.LogWarning("Sprite Replacer plugin patching complete!");
            Logger.LogInfo($"Watching for replacement sprites in: {spritesPath}");
        }

        private void CacheReplacementTextures()
        {
            if (!Directory.Exists(spritesPath))
            {
                Logger.LogWarning($"Replacement directory does not exist: {spritesPath}");
                return;
            }

            // Get all PNG and JPG files in the replacement directory
            string[] imageFiles = Directory.GetFiles(spritesPath, "*.*", SearchOption.AllDirectories)
                .ToArray();

            Logger.LogInfo($"Found {imageFiles.Length} potential replacement sprites");

            foreach (string imagePath in imageFiles)
            {
                try
                {
                    // Get relative path for key
                    string relativePath = imagePath.Substring(spritesPath.Length + 1);

                    // Load texture
                    byte[] fileData = File.ReadAllBytes(imagePath);

                    // Remove file extension for easier matching
                    string keyName = FilePathToKey(relativePath);
                    cachedReplacements.Add(keyName, fileData);

                    if (keyName.Contains(_logWhenAssetContains.Value))
                        Logger.LogInfo($"Cached replacement sprite: {keyName}");
                    
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error caching texture {imagePath}: {ex.Message}");
                }
            }

            Logger.LogInfo($"Successfully cached {cachedReplacements.Count} replacement sprites");           
        }

        private string FilePathToKey(string filePath)
        {
            //We use the format: <LastDirectory>/<SpriteName>
            //This is because we don't know the full path of a gameobject and sprite names can be duped
            var directory = Path.GetFileName(Path.GetDirectoryName(filePath));
            var fileName = Path.GetFileNameWithoutExtension(filePath);

            string result = !string.IsNullOrEmpty(directory) ? Path.Combine(directory, fileName) : fileName;
            result = result.Replace("\\", "/")
                .ToLower();

            return result;
        }

        private string PrepareAssetPath(string path, string assetName = "")
        {
            if (assetName != "")
                path = $"{path}/{assetName}";

            path = path
                .Replace("\\", "/")
                .ToLower();

            // This is hang over from direct asset paths because we didnt include the Asset/ beginning
            if (path.StartsWith("assets/"))
                path = path[..^7];

            return path;
        }

        public void OnAssetLoaded(AssetLoadedContext context)
        {
            string assetName = context.Asset?.name;
            string assetPath = context.Parameters?.Name ?? string.Empty;
            string type = context.Parameters?.Type.ToString();
            string loadType = context.Parameters?.LoadType.ToString();
            string fullPath = PrepareAssetPath(assetPath);

            if (fullPath.Contains(_logWhenAssetContains.Value) || assetName.Contains(_logWhenAssetContains.Value))
                Logger.LogInfo($"Loaded Asset: {assetName}  Path: {assetPath} Type: {type}  LoadType: {loadType}");

            if (context.Asset is GameObject prefab)
            {
                var allChildren = prefab.GetComponentsInChildren<UnityEngine.UI.Image>();

                foreach (var child in allChildren)
                {
                    if (child.sprite != null)
                    {
                        string spritePath = PrepareAssetPath(assetName, child.sprite.name);

                        if (spritePath.Contains(_logWhenAssetContains.Value))
                            Logger.LogWarning($"Found Sprite Path: {spritePath}");                        

                        if (cachedReplacements.TryGetValue(spritePath, out var replacementTexture))
                        {                           
                            child.sprite.texture.LoadImage(replacementTexture, false);

                            if (spritePath.Contains(_logWhenAssetContains.Value))
                                Logger.LogWarning($"Replaced sprite: {spritePath}");
                        }
                    }
                }
            }
            // Texture2D might need to come back into play for non-prefab textures but its unlikely they are used in game 
            // Needs more testing
            //else if ((context.Asset is Texture2D))
            //    Logger.LogError($"{fullPath} is a Texture2D but we disabled replacement.");
        }

        //private void ProcessTexture(AssetLoadedContext context, string fullPath)
        //{
        //    if (cachedReplacements.TryGetValue(fullPath, out Texture2D replacementTexture))
        //    {
        //        Logger.LogWarning($"Replacing in place sprite: {fullPath}");

        //        // Create a new Sprite from our replacement texture
        //        if (context.Asset is Sprite originalSprite)
        //        {
        //            // For sprites, we need to create a new sprite with the same properties
        //            Sprite newSprite = Sprite.Create(
        //                replacementTexture,
        //                originalSprite.rect,
        //                originalSprite.pivot,
        //                originalSprite.pixelsPerUnit,
        //                0,
        //                SpriteMeshType.FullRect);

        //            context.Asset = newSprite;
        //        }
        //        else
        //        {
        //            // For textures, just replace with our cached texture
        //            context.Asset = replacementTexture;
        //        }
        //    }
        //}

        public void OnDestroy()
        {
            // Unregister our hook when the plugin is unloaded
            ResourceRedirection.UnregisterAssetLoadedHook(OnAssetLoaded);
        }
    }
}
