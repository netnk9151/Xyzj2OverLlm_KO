using HarmonyLib;
using SweetPotato;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace EnglishPatch.Patches;

public static class RandomNamePatch
{
    public static int Replacement_RandomNameStr(NpcProBaseType gender, ref string randomName)
    {
        int firstNameId = -1;
        int secondNameId = -1;

        // Check world already has me
        int randomNameId = RandomNameNew.GetRandomNameID(gender);
        
        if (randomNameId != -1)
        {
            RandomNameNew infoById = RandomNameNew.GetInfoById(randomNameId, gender);
            randomName = infoById.name;
            return infoById.firstnameid;
        }

        while (!RandomName.IsRandomNameAvaiable(firstNameId, secondNameId))
        {
            firstNameId = RandomName.RandomFirstNameInfoId();
            secondNameId = RandomName.RandomSecondNameInfoId(gender);
        }

        if (firstNameId != -1 && secondNameId != -1)
        {
            randomName = $"{RandomName.mTemplateList[firstNameId].name} {RandomName.mTemplateList[secondNameId].name}";
        }

        return firstNameId;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(RandomName), nameof(RandomName.RandomNameStr))]
    public static bool Prefix_RandomNameStr(NpcProBaseType gender, ref string randomName, ref int __result)
    {
        __result = Replacement_RandomNameStr(gender, ref randomName);
        return false;
    }
}