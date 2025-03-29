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
        MainPlugin.Logger.LogInfo("Removed name restriction: InstantiateViewNewNew_mobile.Awake");
        var nameInput = AccessTools.Field(typeof(InstantiateViewNewNew_mobile), "m_nameinput").GetValue(__instance) as TMP_InputField;
        var nextButton = AccessTools.Field(typeof(InstantiateViewNewNew_mobile), "m_NextBuBtn").GetValue(__instance) as Button;
        RemoveRestrictionOnInput(nameInput, nextButton);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(SweetPotato.InstantiateViewNewNew), "Awake")]
    public static void Postfix_InstantiateViewNewNew_Awake(InstantiateViewNewNew __instance)
    {
        MainPlugin.Logger.LogMessage("Removed name restriction: InstantiateViewNewNew.Awake");
        var nameInput = AccessTools.Field(typeof(InstantiateViewNewNew), "m_nameinput").GetValue(__instance) as TMP_InputField;
        var nextButton = AccessTools.Field(typeof(InstantiateViewNewNew), "m_NextBuBtn").GetValue(__instance) as Button;
        RemoveRestrictionOnInput(nameInput, nextButton);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(FaceSetView), "Awake")]
    public static void Postfix_FaceSetView_Awake(FaceSetView __instance)
    {
        MainPlugin.Logger.LogMessage("Removed name restriction: FaceSetView.Awake");
        var nameInput = AccessTools.Field(typeof(FaceSetView), "m_InputField").GetValue(__instance) as TMP_InputField;
        var nextButton = AccessTools.Field(typeof(FaceSetView), "btn_Input").GetValue(__instance) as Button;
        RemoveRestrictionOnInput(nameInput, nextButton);
    }

    public static void RemoveRestrictionOnInput(TMP_InputField nameInput, Button nextButton)
    {
        if (nameInput != null && nextButton != null)
        {
            nameInput.onValueChanged.RemoveAllListeners();
            nameInput.onValueChanged.AddListener((string newStr) => nextButton.interactable = Tools.IsStrAvailable(newStr));
            
            nameInput.onEndEdit.RemoveAllListeners();
            nameInput.onEndEdit.AddListener((string newStr) => nextButton.interactable = Tools.IsStrAvailable(newStr));
        }
    }
}
