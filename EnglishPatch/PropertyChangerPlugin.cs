using BepInEx;
using BepInEx.Logging;
using EnglishPatch.Patches;
using HarmonyLib;
using SweetPotato;
using UnityEngine;

namespace EnglishPatch;

[BepInPlugin($"{MyPluginInfo.PLUGIN_GUID}.PropertyChangerPlugin", "PropertyChangerPlugin", MyPluginInfo.PLUGIN_VERSION)]
public class PropertyChangerPlugin : BaseUnityPlugin
{
    private bool showUI = false;
    private string userInput = "";
    private static float sightRangeFront = 1.0f;
    private static float sightRangeBack = 1.0f;
    private static float warnRangeFront = 1.0f;
    private static float combatRange = 1.0f;
    private static float defenceRange = 1.0f;
    private static float trackRange = 1.0f;
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
            WorldManager.Instance.m_PlayerEntity.m_name = userInput;
        }

        GUI.Label(new Rect(20, 80, 150, 20), "NPC Range Multipliers: (Only applies when NPC in range)");

        GUI.Label(new Rect(20, 110, 150, 20), "SightRangeFront:");
        sightRangeFront = float.Parse(GUI.TextField(new Rect(180, 110, 50, 20), sightRangeFront.ToString()));

        GUI.Label(new Rect(20, 140, 150, 20), "SightRangeBack:");
        sightRangeBack = float.Parse(GUI.TextField(new Rect(180, 140, 50, 20), sightRangeBack.ToString()));

        GUI.Label(new Rect(20, 170, 150, 20), "WarnRangeFront:");
        warnRangeFront = float.Parse(GUI.TextField(new Rect(180, 170, 50, 20), warnRangeFront.ToString()));

        GUI.Label(new Rect(20, 200, 150, 20), "CombatRange:");
        combatRange = float.Parse(GUI.TextField(new Rect(180, 200, 50, 20), combatRange.ToString()));

        GUI.Label(new Rect(20, 230, 150, 20), "DefenceRange:");
        defenceRange = float.Parse(GUI.TextField(new Rect(180, 230, 50, 20), defenceRange.ToString()));

        GUI.Label(new Rect(20, 260, 150, 20), "TrackRange:");
        trackRange = float.Parse(GUI.TextField(new Rect(180, 260, 50, 20), trackRange.ToString()));
    }

    [HarmonyPrefix, HarmonyPatch(typeof(NpcFight), "GetNpcFight")]
    public static bool GetNpcFight(long id, ref NpcFight __result)
    {
        NpcFight.mTemplateList.TryGetValue(id, out var npcFight);

        if (npcFight != null)
        {
            npcFight.SightRangeFront *= sightRangeFront;
            npcFight.SightRangeBack *= sightRangeBack;
            npcFight.WarnRangeFront *= warnRangeFront;
            npcFight.CombatRange *= combatRange;
            npcFight.DefenceRange *= defenceRange;
            npcFight.TrackRange *= trackRange;
        }

        __result = npcFight;

        return false;
    }

}
