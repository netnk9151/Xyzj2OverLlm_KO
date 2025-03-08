using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SweetPotato;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

namespace EnglishPatch;

/// <summary>
/// Put dicey stuff in here that might crash the plugin - so it doesnt crash the existing plugins
/// </summary>
[BepInPlugin($"{MyPluginInfo.PLUGIN_GUID}.Debug", "DebugGame", MyPluginInfo.PLUGIN_VERSION)]
internal class DebugPlugin: BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private void Awake()
    {
        Logger = base.Logger;

        Harmony.CreateAndPatchAll(typeof(DebugPlugin));
        Logger.LogWarning($"Debug Game Plugin should be patched!");
    }

    [HarmonyPrefix, HarmonyPatch(typeof(AttriPart), "OnInit")]
    public static bool Prefix_AttriPart_OnInit(AttriPart __instance, PlayerAttriAndBagView parent)
    {
        Logger.LogWarning($"Hooked Prefix_AttriPart_OnInit!");
        return true;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(AttriPart), "OnInit")]
    public static void Postfix_AttriPart_OnInit(AttriPart __instance)
    {
        Logger.LogWarning($"Hooked Postfix_AttriPart_OnInit!");
    }

    // Original Code
    //public void OnInit(PlayerAttriAndBagView parent)
    //{
    //    this.parent = parent;
    //    SetTeamButtonState();
    //    shotcut.SetActivate(!LearnViewNew.Instance.isShowNpc);
    //    showEquip = false;
    //    attriTogGroup.anchoredPosition = new Vector2(0f, attriTogGroup.anchoredPosition.y);
    //    OnStateBtnClick();
    //    OnReset();
    //    cg_AttriEquip.SetActivate(active: true);
    //    attriTogGroup.SetActivate(active: true);
    //    for (int i = 0; i < attriToggle.Length; i++)
    //    {
    //        if (!attriToggle[i].isOn)
    //        {
    //            attriToggle[i].isOn = true;
    //            break;
    //        }
    //    }

    //    attriToggle[0].isOn = true;
    //    attriToggle[0].onValueChanged.Invoke(arg0: true);
    //    if (LearnViewNew.Instance.isShowNpc)
    //    {
    //        textTeamState.text = (TeamManager.Instance.IsInDeployTeam(LearnViewNew.Instance.npcEntity.guid) ? "下阵" : "上阵");
    //        equipPart.OnInit(LearnViewNew.Instance.npcEntity.guid);
    //    }
    //    else
    //    {
    //        equipPart.OnInit(PlayerController.Instance.guid);
    //    }

    //    CloseAllTog();
    //}

    // Opening Screen
    //[HarmonyPrefix, HarmonyPatch(typeof(SweetPotato.LoginViewNew), "OnButtonClick")]
    //public static bool Prefix_OnButtonClick(SweetPotato.LoginViewNew __instance, int index, RectTransform rect)
    //{
    //    var text = rect.FindChildCustom<TextMeshProUGUI>("btnname").text.Trim();

    //    Logger.LogWarning($"Hooked OnButtonClick! [{text}]");

    //    Logger.LogInfo($"Actual Text Hex: {string.Join(" ", text.Select(c => ((int)c).ToString("X2")))}");
    //    Logger.LogInfo($"Switch Case Hex: {string.Join(" ", "A new Jianghu.".Select(c => ((int)c).ToString("X2")))}");

    //    switch (rect.FindChildCustom<TextMeshProUGUI>("btnname").text.Trim())
    //    {
    //        case "新的江湖":
    //            Logger.LogWarning($"Old Text");
    //            break;
    //        case "A new Jianghu.":
    //            Logger.LogWarning($"New Text");
    //            break;
    //    }

    //    return true;
    //}
}
