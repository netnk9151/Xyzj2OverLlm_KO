using HarmonyLib;
using SweetPotato;
using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EnglishPatch.Patches;

public static class ToolsPatch
{
    public static readonly float mYear = 3.1104E+07f;
    public static readonly float mMonth = 2592000f;
    public static readonly float mDay = 86400f;
    public static readonly float mHour = 3600f;
    public static readonly float mMin = 120f;

    public static double ConvertToSeconds(double realTime)
    {
        return realTime * mMin + 12f * mHour + 3f * mDay + 5f * mMonth + 40f * mYear;
    }

    public static void CalculateTimeIncrements(double totalSeconds, out int year, out int month, out int day, out int hour)
    {
        year = Mathf.FloorToInt((float)(totalSeconds / mYear));
        month = Mathf.CeilToInt((float)(totalSeconds % mYear) / mMonth);
        day = Mathf.CeilToInt((float)(totalSeconds % mMonth) / mDay);
        hour = Mathf.FloorToInt((float)(totalSeconds % mDay) / mHour);
    }

    public static string GetRealTimeDate2(double mtime)
    {
        var days = (int)(mtime / 1440.0);
        var hours = (int)((mtime % 1440.0) / 120.0);
        var minutes = (int)((mtime % 120.0) / 15.0);

        if (mtime > 1440.0)
            return $"{days} days {hours} hours {minutes} mins";

        if (mtime > 120.0)
            return $"{hours} hours {minutes} mins";

        if (mtime > 15.0)
            return $"{minutes} mins";

        return "Less than a minute";
    }
    
    public static string GetGameTimeDate(double realTime)
    {
        double totalSeconds = ConvertToSeconds(realTime);
        CalculateTimeIncrements(totalSeconds, out var year, out var month, out var day, out var hour);

        if (hour == 24)
            hour = 0;

        var dateTime = new DateTime(year, month, day);
        var shortMonth = dateTime.ToString("MMM");

        return $"{day}-{shortMonth}-JY{year} {Tools.SHI_CHENG_STR[hour / 2]}";
    }

    public static string GetXiaoxiGameTimeDate(double realTime)
    {
        double totalSeconds = ConvertToSeconds(realTime);
        CalculateTimeIncrements(totalSeconds, out var year, out var month, out var day, out var _);

        var dateTime = new DateTime(year, month, day);
        var shortMonth = dateTime.ToString("MMM");

        return $"{day}-{shortMonth}-JY{year}";
    }

    public static string GetTaskGameTimeDate(double realTime, bool certainHour, bool addOriginalTime)
    {
        double totalSeconds = ConvertToSeconds(realTime);
        if (!addOriginalTime)
            totalSeconds = realTime;

        CalculateTimeIncrements(totalSeconds, out var year, out var month, out var day, out var hour);

        var dateTime = new DateTime(year, month, day);
        var shortMonth = dateTime.ToString("MMM");

        if (certainHour)
            return $"{day}-{shortMonth}-JY{year} {Tools.SHI_CHENG_STR[hour / 2]}";

        return $"{day}-{shortMonth}-JY{year}";
    }

    public static string GetRemainTimeDate(float remainTime, bool certainHour)
    {
        var newRemainTime = remainTime * mMin;
        int num = Mathf.FloorToInt(newRemainTime / mYear);
        int num2 = Mathf.FloorToInt((newRemainTime - (float)num * mYear) / mMonth);
        int num3 = Mathf.FloorToInt((newRemainTime - (float)num * mYear - (float)num2 * mMonth) / mDay);
        int num4 = Mathf.FloorToInt((newRemainTime - (float)num * mYear - (float)num2 * mMonth - (float)num3 * mDay) / mHour);

        var builder = new StringBuilder();

        if (num > 0)
            builder.Append($"{num} years ");

        if (num2 > 0)
            builder.Append($"{num2} months ");

        if (num3 > 0)
            builder.Append($"{num3} days ");

        if (certainHour && num4 > 0)
            builder.Append($"{num4} hour ");

        return builder.ToString();
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Tools), nameof(GetGameTimeDate))]
    public static bool Prefix_Tools_GetGameTimeDate(ref string __result, double realTime)
    {
        __result = GetGameTimeDate(realTime);
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Tools), nameof(GetXiaoxiGameTimeDate))]
    public static bool Prefix_Tools_GetXiaoxiGameTimeDate(ref string __result, double realTime)
    {
        __result = GetXiaoxiGameTimeDate(realTime);
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Tools), nameof(GetTaskGameTimeDate))]
    public static bool Prefix_Tools_GetTaskGameTimeDate(ref string __result, double realTime, bool contanHour, bool addOriginTIme)
    {
        __result = GetTaskGameTimeDate(realTime, contanHour, addOriginTIme);
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Tools), nameof(GetRemainTimeDate))]
    public static bool Prefix_Tools_GetRemainTimeDate(ref string __result, float remainTime, bool contanHour)
    {
        __result = GetRemainTimeDate(remainTime, contanHour);
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(UnityHelper), "GetNunWordFromNum")]
    public static bool Prefix_UnityHelper_GetNunWordFromNum(ref string __result, int num)
    {
        __result = $"{num}";
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Tools), "NumberToChinese")]
    public static bool Prefix_Tools_NumberToChinese(ref string __result, int number, bool first)
    {
        __result = $"{number}";
        return false;
    }
}
