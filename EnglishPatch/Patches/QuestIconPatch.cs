using HarmonyLib;
using SweetPotato;
using UnityEngine;

namespace EnglishPatch.Patches;

/// <summary>
/// Fixes the Quest Icon being too far to the right because the multiplier for character size
/// </summary>
public static class QuestIconPatch
{
    //private static double questMultiplier = 0.019999999552965164; //Game default
    private static double questMultiplier = 0.01; //My happy default

    [HarmonyPostfix, HarmonyPatch(typeof(UnitHead), "RefreshQuestIcon")]
    public static void RefreshQuestIcon(UnitHead __instance)
    {
        var m_UnitController = AccessTools.Field(typeof(UnitHead), "m_UnitController").GetValue(__instance) as UnitController;
        var m_QuestIcon = AccessTools.Field(typeof(UnitHead), "m_QuestIcon").GetValue(__instance) as MeshRenderer;
        var m_QuestIconQuan = AccessTools.Field(typeof(UnitHead), "m_QuestIconQuan").GetValue(__instance) as MeshRenderer;
        var m_QuestTargetIcon = AccessTools.Field(typeof(UnitHead), "m_QuestTargetIcon").GetValue(__instance) as MeshRenderer;
        var m_hideHpBar = AccessTools.Field(typeof(UnitHead), "m_hideHpBar").GetValue(__instance) as bool?;

        double num = 0.0;
        if (!m_hideHpBar.Value)
            num = 0.05;

        int length = m_UnitController.m_Entity.GetNpcName().Length;
        double nameAdjustment = questMultiplier * (length - 1);

        m_QuestIcon.material?.SetFloat("_PosOffsetX", (float)(0.035000000149011612 + num + nameAdjustment));
        m_QuestIconQuan.material?.SetFloat("_PosOffsetX", (float)(0.035000000149011612 + num + nameAdjustment));
        m_QuestTargetIcon.material?.SetFloat("_PosOffsetX", (float)-(0.035000000149011612 + num + nameAdjustment));
    }
}