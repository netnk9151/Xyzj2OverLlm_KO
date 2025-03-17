using BepInEx;
using BepInEx.Logging;
using EnglishPatch.Patches;
using HarmonyLib;
using SweetPotato;
using UnityEngine;

namespace EnglishPatch;

[BepInPlugin($"{MyPluginInfo.PLUGIN_GUID}.NameChangerPlugin", "NameChangerPlugin", MyPluginInfo.PLUGIN_VERSION)]
public class NameChangerPlugin : BaseUnityPlugin
{
    private bool showUI = false;
    private string userInput = "";
    private bool initialized = false;  // Flag to check if input was initialized

    internal static new ManualLogSource Logger;

    private void Awake()
    {
        Logger = base.Logger;

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

        GUI.Box(new Rect(10, 10, 300, 120), "Change your name");

        GUI.Label(new Rect(20, 40, 80, 20), "Enter Name:");
        userInput = GUI.TextField(new Rect(100, 40, 180, 20), userInput);

        if (GUI.Button(new Rect(100, 70, 100, 30), "OK"))
        {
            WorldManager.Instance.m_PlayerEntity.m_name = userInput;
        }
    }
}
