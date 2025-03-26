using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SweetPotato;
using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

namespace EnglishPatch;

[BepInPlugin($"{MyPluginInfo.PLUGIN_GUID}.PropertyChangerPlugin", "PropertyChangerPlugin", MyPluginInfo.PLUGIN_VERSION)]
public class PropertyChangerPlugin : BaseUnityPlugin
{
    private bool showUI = false;
    private string userInput = "";

    private bool initialized = false;  // Flag to check if input was initialized

    internal static new ManualLogSource Logger;

    private void Awake()
    {
        Logger = base.Logger;

        Harmony.CreateAndPatchAll(typeof(PropertyChangerPlugin));

        // Plugin startup logic
        Logger.LogWarning($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void Update()
    {
        // Toggle UI with F2 key
        if (Input.GetKeyDown(KeyCode.KeypadPeriod))
        {
            showUI = !showUI;

            if (showUI && !initialized)
            {
                if (WorldManager.Instance != null && WorldManager.Instance.m_PlayerEntity != null)
                {                    
                    userInput = WorldManager.Instance.m_PlayerEntity.m_name;
                    initialized = true; // Mark as initialized
                }                
            }
        }
    }

    private void OnGUI()
    {
        if (!showUI) return;

        GUI.Box(new Rect(10, 10, 400, 300), "Property Changer");

        GUI.Label(new Rect(20, 40, 80, 20), "Enter Name:");
        userInput = GUI.TextField(new Rect(100, 40, 180, 20), userInput);

        if (GUI.Button(new Rect(290, 40, 100, 20), "Change Name"))
        {
            //Replace spaces with non breaking space
            userInput = Regex.Replace(userInput, @"\s", "\u00A0");
            WorldManager.Instance.m_PlayerEntity.m_name = userInput;
        }           
    }
}
