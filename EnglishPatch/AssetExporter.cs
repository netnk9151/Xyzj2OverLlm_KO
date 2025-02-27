using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Translate;

public class AssetBundleExporter
{
    public static void ExportMonobehavioursWithText(string filePath, string exportPath)
    {
        var exportedStrings = new List<string>();
        if (!File.Exists(filePath))
        {
            Console.WriteLine("AssetBundle not found: " + filePath);
            return;
        }

        AssetBundle bundle = AssetBundle.LoadFromFile(filePath);
        if (bundle == null)
        {
            Console.WriteLine("Failed to load AssetBundle.");
            return;
        }

        foreach (var assetName in bundle.GetAllAssetNames())
        {
            UnityEngine.Object asset = bundle.LoadAsset(assetName);
            if (asset is GameObject gameObject)
            {
                foreach (var component in gameObject.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    Type type = component.GetType();
                    var textField = type.GetField("m_Text", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (textField != null && textField.FieldType == typeof(string))
                    {
                        var textValue = textField.GetValue(component) as string;
                        if (!string.IsNullOrEmpty(textValue))
                        {
                            //var name = GetContainerPath(gameObject);

                            exportedStrings.Add($"{assetName}  =  {textValue}");

                            //File.AppendAllText(exportFile, textValue + "\n");
                            //Console.WriteLine($"Exported text from {type.Name} to {exportFile}");
                        }
                    }
                }
            }
        }

        bundle.Unload(false);

        File.WriteAllLines($"{exportPath}/1.txt", exportedStrings);
    }

    private static string GetContainerPath(GameObject obj)
    {
        string path = obj.name;
        Transform parentTransform = obj.transform.parent;

        while (parentTransform != null)
        {
            path = parentTransform.name + "/" + path;
            parentTransform = parentTransform.parent;
        }

        return "/" + path; // Starting with '/' for the root
    }
}

