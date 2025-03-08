using HarmonyLib;
using SweetPotato;
using TMPro;
using UnityEngine.UI;

namespace EnglishPatch.Patches;

public static class NameRestrictionPatch
{
    //Remove Name Restrictions
    [HarmonyPostfix, HarmonyPatch(typeof(SweetPotato.InstantiateViewNewNew_mobile), "Awake")]
    public static void Postfix_InstantiateViewNewNew_mobile_Awake(InstantiateViewNewNew_mobile __instance)
    {
        MainPlugin.Logger.LogWarning("Postfix_InstantiateViewNewNew_mobile_Awake");

        var nameInput = AccessTools.Field(typeof(InstantiateViewNewNew_mobile), "m_nameinput").GetValue(__instance) as TMP_InputField;
        var nextButton = AccessTools.Field(typeof(InstantiateViewNewNew_mobile), "m_NextBuBtn").GetValue(__instance) as Button;

        MainPlugin.Logger.LogWarning($"Hooked InstantiateViewNewNew Awake! {nameInput} {nextButton}");

        if (nameInput != null && nextButton != null)
        {
            nameInput.onValueChanged.RemoveAllListeners();
            nameInput.onValueChanged.AddListener((string newStr) =>
            {
                // Always allow the name, removing keyword restrictions
                nextButton.interactable = Tools.IsStrAvailable(newStr);
            });

            nameInput.onEndEdit.RemoveAllListeners();
            nameInput.onEndEdit.AddListener((string newStr) =>
            {
                // Always allow the name, removing keyword restrictions
                nextButton.interactable = Tools.IsStrAvailable(newStr);
            });
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(SweetPotato.InstantiateViewNewNew), "Awake")]
    public static void Postfix_InstantiateViewNewNew_Awake(InstantiateViewNewNew __instance)
    {
        MainPlugin.Logger.LogWarning("Postfix_InstantiateViewNewNew_mobile_Awake");

        var nameInput = AccessTools.Field(typeof(InstantiateViewNewNew), "m_nameinput").GetValue(__instance) as TMP_InputField;
        var nextButton = AccessTools.Field(typeof(InstantiateViewNewNew), "m_NextBuBtn").GetValue(__instance) as Button;

        MainPlugin.Logger.LogWarning($"Hooked InstantiateViewNewNew Awake! {nameInput} {nextButton}");

        if (nameInput != null && nextButton != null)
        {
            nameInput.onValueChanged.RemoveAllListeners();
            nameInput.onValueChanged.AddListener((string newStr) =>
            {
                // Always allow the name, removing keyword restrictions
                nextButton.interactable = Tools.IsStrAvailable(newStr);
            });

            nameInput.onEndEdit.RemoveAllListeners();
            nameInput.onEndEdit.AddListener((string newStr) =>
            {
                // Always allow the name, removing keyword restrictions
                nextButton.interactable = Tools.IsStrAvailable(newStr);
            });
        }
    }

}
