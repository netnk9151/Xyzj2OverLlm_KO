using HarmonyLib;
using SweetPotato;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine.UI;

namespace EnglishPatch.Patches;

public static class ItemsPatch
{
    //Remove Name Restrictions
    public static string Calculate_RequestDes_NOS(Item item, ItemPrototype m_pProto)
    {
        ItemPrototype._CLASS type = (ItemPrototype._CLASS)item.Proto_NOS().type;
        ItemPrototype._SUB_CLASS subType = (ItemPrototype._SUB_CLASS)item.Proto_NOS().subType;
        string requestDesNos1 = string.Empty;
        string useCondition = item.Proto_NOS().useCondition;
        if (!string.IsNullOrEmpty(useCondition))
        {
            string requestDesNos2 = requestDesNos1 + "要求：";
            foreach (string str1 in useCondition.Split("+", StringSplitOptions.None))
            {
                string[] strArray1 = str1.Split("&", StringSplitOptions.None);
                if (strArray1.Length >= 2)
                {
                    string[] strArray2 = strArray1[1].Split("|", StringSplitOptions.None);
                    AnswerViewNew.ExtraConditonType extraConditonType = (AnswerViewNew.ExtraConditonType)int.Parse(strArray2[0]);
                    if (type == ItemPrototype._CLASS.C_USEABLE && subType == ItemPrototype._SUB_CLASS.SC_TGT_CGL && int.Parse(strArray2[0]) == 28)
                    {
                        string str2 = item.NameDes_NOS.Contains("秘法") ? "秘法" : "秘术";
                        string str3;
                        if (int.Parse(strArray1[0]) == 1)
                        {
                            if (!AnswerViewNew.IsMatchMultCondition(useCondition, m_pProto.ModId))
                                str3 = "<color=#F75A55>学习任意" + Tools.NumberToChinese(item.m_Level) + "阶对应身份" + str2 + "</color>";
                            else
                                str3 = "学习任意" + Tools.NumberToChinese(item.m_Level) + "阶对应身份" + str2;
                        }
                        else
                        {
                            int result;
                            int.TryParse(strArray2[1], out result);
                            string str4 = Stringlang.GetStr((long)result);
                            str3 = AnswerViewNew.IsMatchMultCondition(useCondition, m_pProto.ModId) ? "学习过" + str4 : "<color=#F75A55>学习过" + str4 + "</color>";
                        }
                        string str5;
                        return str5 = "要求：" + str3;
                    }
                    switch (extraConditonType)
                    {
                        case AnswerViewNew.ExtraConditonType.ECT_NPCATTRI:
                            string[] strArray3 = item.Proto_NOS().useCondition.Split("&", StringSplitOptions.None)[1].Split("|", StringSplitOptions.None);
                            string str6 = "达到";
                            switch (strArray3[4])
                            {
                                case ">":
                                    str6 = "大于";
                                    break;
                                case "=":
                                    str6 = "达到";
                                    break;
                                case "<":
                                    str6 = "小于";
                                    break;
                            }
                            string str7;
                            if (!AnswerViewNew.IsMatchMultCondition(useCondition, m_pProto.ModId))
                                str7 = "<color=#F75A55>" + Tools.m_AttrName[int.Parse(strArray3[2])] + str6 + strArray3[3] + "</color>";
                            else
                                str7 = Tools.m_AttrName[int.Parse(strArray3[2])] + str6 + strArray3[3];
                            requestDesNos2 += str7;
                            break;
                        case AnswerViewNew.ExtraConditonType.ECT_UsedItemCount:
                            string str8 = AnswerViewNew.IsMatchMultCondition(useCondition, m_pProto.ModId) ? "限使用次数" + strArray2[3] + "次" : "<color=#F75A55>限使用次数" + strArray2[3] + "次</color>";
                            requestDesNos2 += str8;
                            break;
                        case AnswerViewNew.ExtraConditonType.ECT_IsInBottleneck:
                            int index = int.Parse(strArray2[1]);
                            if (str1.EndsWith("!"))
                            {
                                string str9 = AnswerViewNew.IsMatchMultCondition(useCondition, m_pProto.ModId) ? Tools.m_AttrName[index] + "没有到达瓶颈" : "<color=#F75A55>" + Tools.m_AttrName[index] + "没有到达瓶颈</color>";
                                requestDesNos2 += str9;
                                break;
                            }
                            string str10 = AnswerViewNew.IsMatchMultCondition(useCondition, m_pProto.ModId) ? Tools.m_AttrName[index] + "到达瓶颈" : "<color=#F75A55>" + Tools.m_AttrName[index] + "到达瓶颈</color>";
                            requestDesNos2 += str10;
                            break;
                        case AnswerViewNew.ExtraConditonType.ECT_PlayerWeaponType:
                            int key1 = int.Parse(item.Proto_NOS().useCondition.Split("&", StringSplitOptions.None)[1].Split("|", StringSplitOptions.None)[1]);
                            string str11 = AnswerViewNew.IsMatchMultCondition(useCondition, m_pProto.ModId) ? "当前装备武器类型为" + Tools.taoluDes[(TAOLU_TYPE)key1] : "<color=#F75A55>当前装备武器类型为" + Tools.taoluDes[(TAOLU_TYPE)key1] + "</color>";
                            requestDesNos2 += str11;
                            break;
                        case AnswerViewNew.ExtraConditonType.ECT_IsHaveStunt:
                            int key2 = int.Parse(item.Proto_NOS().useCondition.Split("&", StringSplitOptions.None)[1].Split("|", StringSplitOptions.None)[1]);
                            string str12 = AnswerViewNew.IsMatchMultCondition(useCondition, m_pProto.ModId) ? "学会特技[" + StuntPrototype.mTemplateList[(long)key2].name + "]" : "<color=#F75A55>学会特技[" + StuntPrototype.mTemplateList[(long)key2].name + "]</color>";
                            requestDesNos2 += str12;
                            break;
                        case AnswerViewNew.ExtraConditonType.ECT_IsUnLockStunt:
                            int key3 = int.Parse(item.Proto_NOS().useCondition.Split("&", StringSplitOptions.None)[1].Split("|", StringSplitOptions.None)[1]);
                            string str13 = AnswerViewNew.IsMatchMultCondition(useCondition, m_pProto.ModId) ? "解锁身份[" + StuntPrototype.mTemplateList[(long)key3].name + "]" : "<color=#F75A55>解锁身份[" + StuntPrototype.mTemplateList[(long)key3].name + "]</color>";
                            requestDesNos2 += str13;
                            break;
                    }
                    requestDesNos2 = requestDesNos2 ?? "";
                }
            }
            return requestDesNos2;
        }
        if (type == ItemPrototype._CLASS.C_USEABLE && subType == ItemPrototype._SUB_CLASS.SC_PENDANT)
        {
            string miscvalue1 = item.Proto_NOS().miscvalue1;
            if (!Tools.IsStrAvailable(miscvalue1))
                return requestDesNos1;
            string[] strArray = miscvalue1.Substring(2, miscvalue1.Length - 2).Split('|', StringSplitOptions.None);
            AttriType index = (AttriType)Enum.Parse(typeof(AttriType), strArray[2]);
            string str14 = Tools.m_AttrName[(int)index];
            string str15 = int.Parse(strArray[3]).ToString();
            string str16 = "以上";
            switch (strArray[4])
            {
                case "==":
                    str16 = string.Empty;
                    break;
                case ">":
                    str16 = "以上";
                    break;
                case "<":
                    str16 = "以下";
                    break;
                case ">=":
                    str16 = "及以上";
                    break;
                case "<=":
                    str16 = "及以下";
                    break;
            }
            requestDesNos1 = AnswerViewNew.IsMatchMultCondition(useCondition, m_pProto.ModId) ? string.Format("{0}要求：{1}{2}", str14, str15, str16) : string.Format("{0}要求：<color=#F75A55>{1}{2}</color>", str14, str15, str16);
        }
        return requestDesNos1;
    }

    public static string[] Calculate_BaseAttriDes_NOS(Item item)
    {
        List<string> stringList = new List<string>();
        ItemPrototype itemPrototype = item.Proto_NOS();
        ItemPrototype._CLASS type = (ItemPrototype._CLASS)itemPrototype.type;
        ItemPrototype._SUB_CLASS subType = (ItemPrototype._SUB_CLASS)itemPrototype.subType;
        switch (type)
        {
            case ItemPrototype._CLASS.C_EQUIP:
                using (Dictionary<int, int>.Enumerator enumerator = item.m_BaseAttri.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        KeyValuePair<int, int> current = enumerator.Current;
                        stringList.Add(string.Format("{0}：+{1}", Tools.m_AttrName[current.Key], Tools.IntensifyValue(current.Value, item.m_Intensify)));
                    }
                    break;
                }
            case ItemPrototype._CLASS.C_MATERIAL:
                switch (subType)
                {
                    case ItemPrototype._SUB_CLASS.SC_BELT:
                        stringList.Add(PropItemUI.GetRateStr(ArtistryType.Fish, int.Parse(itemPrototype.miscvalue2)));
                        break;
                    case ItemPrototype._SUB_CLASS.SC_RING:
                        stringList.Add(PropItemUI.GetRateStr(ArtistryType.Plant, int.Parse(itemPrototype.miscvalue2)));
                        break;
                    case ItemPrototype._SUB_CLASS.SC_NECKLACE:
                        stringList.Add(string.Format("成功率：+{0}", (float)((double)float.Parse(itemPrototype.miscvalue2) / 100.0)));
                        break;
                    case ItemPrototype._SUB_CLASS.SC_PENDANT:
                        using (Dictionary<int, int>.Enumerator enumerator = Tools.GetItemMisvalueDic(itemPrototype.miscvalue2).GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                KeyValuePair<int, int> current = enumerator.Current;
                                stringList.Add(string.Format("增加词条概率：{0} +{1}", WordEntryLooter.Instance.GetWordName((long)current.Key), (float)((double)current.Value / 100.0)));
                            }
                            break;
                        }
                    case ItemPrototype._SUB_CLASS.SC_TRESURE:
                        stringList.Add(string.Format("增加出现多个合成物概率：+{0}", (float)((double)float.Parse(itemPrototype.miscvalue2) / 100.0)));
                        break;
                    case ItemPrototype._SUB_CLASS.SC_MA:
                        stringList.Add(string.Format("增加合成不消耗材料的概率：+{0}", (float)((double)float.Parse(itemPrototype.miscvalue2) / 100.0)));
                        break;
                    case ItemPrototype._SUB_CLASS.SC_CHENGHAO:
                        using (Dictionary<int, int>.Enumerator enumerator = Tools.GetItemMisvalueDic(itemPrototype.miscvalue2).GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                KeyValuePair<int, int> current = enumerator.Current;
                                stringList.Add(string.Format("增加合成物对应质量的概率：{0} +{1}", Tools.QualityDes[current.Key], (float)((double)current.Value / 100.0)));
                            }
                            break;
                        }
                    case ItemPrototype._SUB_CLASS.SC_MIJINODEWordEntry_DAN_YAO:
                        stringList.Add(string.Format("能力值：{0}", item.GetTianGongTuNengLiZhi()));
                        break;
                }
                break;
            case ItemPrototype._CLASS.C_USEABLE:
                switch (subType)
                {
                    case ItemPrototype._SUB_CLASS.SC_BELT:
                    case ItemPrototype._SUB_CLASS.SC_RING:
                    case ItemPrototype._SUB_CLASS.SC_NECKLACE:
                        using (Dictionary<int, int>.Enumerator enumerator = Tools.GetItemMisvalueDic(itemPrototype.miscvalue2).GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                KeyValuePair<int, int> current = enumerator.Current;
                                stringList.Add(string.Format("{0}: +{1}", Tools.m_AttrName[current.Key], current.Value));
                            }
                            break;
                        }
                    case ItemPrototype._SUB_CLASS.SC_MIJINODEWordEntry_DAN_YAO:
                        stringList.Add(string.Format("能力值：{0}", item.GetTianGongTuNengLiZhi()));
                        break;
                }
                break;
        }
        return stringList.ToArray();
    }

    public static string GetItemMiscvaluePurpose(Item item)
    {
        ItemPrototype itemPrototype = item.Proto_NOS();
        ItemPrototype._CLASS type = (ItemPrototype._CLASS)itemPrototype.type;
        ItemPrototype._SUB_CLASS subType = (ItemPrototype._SUB_CLASS)itemPrototype.subType;
        string miscvalue1 = itemPrototype.miscvalue1;
        string miscvalue2 = itemPrototype.miscvalue2;
        string miscvalue3 = itemPrototype.miscvalue3;
        var sb = new StringBuilder(); //TODO: It was originally capped at 128 capacity

        switch (type)
        {
            case ItemPrototype._CLASS.C_QUEST:
                if (subType != ItemPrototype._SUB_CLASS.SC_NECKLACE)
                {
                    if (subType == ItemPrototype._SUB_CLASS.SC_PENDANT)
                        break;
                    break;
                }
                sb.Append("出示给某人后可以得到一些信息");
                break;
            case ItemPrototype._CLASS.C_MATERIAL:
                switch (subType)
                {
                    case ItemPrototype._SUB_CLASS.SC_WEAPON:
                        if (Tools.IsStrAvailable(miscvalue3))
                        {
                            sb.Append(item.GetLivingDes(miscvalue3));
                            break;
                        }
                        sb.Append("用于生活系统");
                        break;
                    case ItemPrototype._SUB_CLASS.SC_ARMOR:
                        if (Tools.IsStrAvailable(miscvalue3))
                        {
                            sb.Append(item.GetLivingDes(miscvalue3));
                            break;
                        }
                        sb.Append("用于生活系统");
                        break;
                    case ItemPrototype._SUB_CLASS.SC_SHOE:
                        if (Tools.IsStrAvailable(miscvalue3))
                        {
                            sb.Append(item.GetLivingDes(miscvalue3));
                            break;
                        }
                        sb.Append("用于生活系统");
                        break;
                    case ItemPrototype._SUB_CLASS.SC_BELT:
                        if (Tools.IsStrAvailable(miscvalue2))
                        {
                            sb.AppendFormat("钓鱼完成率：{0}%", int.Parse(miscvalue2));
                            break;
                        }
                        sb.Append("用于生活系统");
                        break;
                    case ItemPrototype._SUB_CLASS.SC_NECKLACE:
                        if (Tools.IsStrAvailable(miscvalue2))
                        {
                            sb.AppendFormat("用于经脉，可作为经脉秘卷材料辅助使用");
                            break;
                        }
                        sb.Append("用于生活系统");
                        break;
                    case ItemPrototype._SUB_CLASS.SC_MA:
                        if (Tools.IsStrAvailable(miscvalue2))
                        {
                            sb.AppendFormat("用于生活系统, {0}%概率不消耗材料", (float)((double)int.Parse(miscvalue2) / 100.0));
                            break;
                        }
                        sb.Append("用于生活系统");
                        break;
                    case ItemPrototype._SUB_CLASS.SC_CHENGHAO:
                        if (Tools.IsStrAvailable(miscvalue2))
                        {
                            sb.Append("用于生活系统, ");
                            string[] strArray1 = miscvalue2.Split('|', StringSplitOptions.None);
                            for (int index = 0; index < strArray1.Length; ++index)
                            {
                                string[] strArray2 = strArray1[index].Split('&', StringSplitOptions.None);
                                if (index == strArray1.Length - 1)
                                    sb.AppendFormat("{0}概率提高{1}%", Tools.QualityDes[int.Parse(strArray2[0])], (float)((double)int.Parse(strArray2[1]) / 100.0));
                                else
                                    sb.AppendFormat("{0}概率提高{1}%\n", Tools.QualityDes[int.Parse(strArray2[0])], (float)((double)int.Parse(strArray2[1]) / 100.0));
                            }
                            break;
                        }
                        sb.Append("用于生活系统");
                        break;
                    case ItemPrototype._SUB_CLASS.SC_SHIZHUANG:
                        if (Tools.IsStrAvailable(miscvalue2))
                        {
                            sb.AppendFormat("用于经脉，可作为经脉秘卷材料辅助使用");
                            break;
                        }
                        sb.Append("用于生活系统");
                        break;
                    case ItemPrototype._SUB_CLASS.SC_EQUIP_END:
                        if (Tools.IsStrAvailable(miscvalue3))
                            sb.Append(item.GetSubType12And13Des(miscvalue3));
                        else
                            sb.Append("用于经脉和五脏");
                        if (Tools.IsStrAvailable(miscvalue2))
                        {
                            sb.AppendFormat(", 并附加词条", (float)((double)int.Parse(miscvalue2) / 100.0));
                            break;
                        }
                        break;
                    case ItemPrototype._SUB_CLASS.SC_SPECIAL_ADDWORD:
                        if (Tools.IsStrAvailable(miscvalue3))
                            sb.Append(item.GetSubType12And13Des(miscvalue3));
                        else
                            sb.Append("用于装备回锻，将附带的词条替换到装备上");
                        if (Tools.IsStrAvailable(miscvalue2))
                        {
                            sb.AppendFormat(", 并附加词条", (float)((double)int.Parse(miscvalue2) / 100.0));
                            break;
                        }
                        break;
                    case ItemPrototype._SUB_CLASS.SC_QINGGONG_UPGRADE:
                        sb.Append("用于轻功激活和升级");
                        break;
                    case ItemPrototype._SUB_CLASS.SC_MIJI_UPGRADE:
                        sb.Append("用于秘籍领悟");
                        break;
                    case ItemPrototype._SUB_CLASS.SC_TGT_CGL:
                        sb.Append(string.Format("用于天工图，覆写成功率提升{0}%", (int.Parse(miscvalue2) / 100)));
                        break;
                    case ItemPrototype._SUB_CLASS.SC_MIJINODEWordEntry_DAO_JU:
                        sb.Append("用于武学，冲灵所窍时使用有几率获得新的词条");
                        break;
                    case ItemPrototype._SUB_CLASS.SC_MIJINODEWordEntry_DAN_YAO:
                        sb.Append("用于武学，冲灵所窍时使用有几率获得新的词条");
                        break;
                    case ItemPrototype._SUB_CLASS.SC_MIJINODEWordEntry_MI_JUAN:
                        sb.Append("用于武学，冲灵所窍时使用有几率获得新的词条");
                        break;
                    case ItemPrototype._SUB_CLASS.SC_MATERIAL_MIFA:
                        sb.Append("用于生活系统，使用秘法时必要的材料");
                        break;
                    case ItemPrototype._SUB_CLASS.SC_CHUILIAN:
                        sb.Append("用于锤炼，回锻");
                        break;
                }
                break;
            case ItemPrototype._CLASS.C_USEABLE:
                switch (subType)
                {
                    case ItemPrototype._SUB_CLASS.SC_SHOE:
                        sb.Append("可赠予他人");
                        break;
                    case ItemPrototype._SUB_CLASS.SC_BELT:
                        if (Tools.IsStrAvailable(miscvalue2))
                        {
                            sb.Append(item.GetSubType456Des(miscvalue2, miscvalue1, miscvalue3));
                            break;
                        }
                        break;
                    case ItemPrototype._SUB_CLASS.SC_RING:
                        if (Tools.IsStrAvailable(miscvalue2))
                        {
                            sb.Append(item.GetSubType456Des(miscvalue2, miscvalue1, miscvalue3));
                            break;
                        }
                        break;
                    case ItemPrototype._SUB_CLASS.SC_NECKLACE:
                        if (Tools.IsStrAvailable(miscvalue2))
                        {
                            sb.Append(item.GetSubType456Des(miscvalue2, miscvalue1, miscvalue3));
                            break;
                        }
                        break;
                    case ItemPrototype._SUB_CLASS.SC_PENDANT:
                        sb.AppendFormat("使用后学习{0}", DataManager.GetStringLangById(itemPrototype.nameId));
                        break;
                    case ItemPrototype._SUB_CLASS.SC_TRESURE:
                    case ItemPrototype._SUB_CLASS.SC_MIJI_UPGRADE:
                        if (Tools.IsStrAvailable(miscvalue2))
                        {
                            sb.Append("使用后");
                            foreach (string str1 in miscvalue2.Split('|', StringSplitOptions.None))
                            {
                                string[] strArray = str1.Split('&', StringSplitOptions.None);
                                string str2 = "增加";
                                string str3 = "";
                                if (strArray[2] == "1")
                                    str3 = "%";
                                if (Tools.bagTypeName.ContainsKey(strArray[0]))
                                {
                                    sb.AppendFormat("{0}{1}{2}{3}\n", Tools.bagTypeName[strArray[0]], str2, strArray[1], str3);
                                }
                                else
                                {
                                    int num = int.Parse(strArray[1]);
                                    PlayerController.Instance?.GetAttriGainAdditionRand(int.Parse(strArray[0]), ref num);
                                    sb.AppendFormat("{0}{1}{2}{3}\n", Tools.m_AttrName[int.Parse(strArray[0])], str2, num, str3);
                                }
                            }
                        }
                        if (Tools.IsStrAvailable(miscvalue3))
                        {
                            string[] strArray3 = miscvalue3.Split('|', StringSplitOptions.None);
                            sb.Append(strArray3[0] == "0" ? "第一次使用" : "使用后");
                            string[] strArray4 = strArray3[1].Split('|', StringSplitOptions.None);
                            for (int index = 0; index < strArray4.Length; ++index)
                            {
                                string[] strArray5 = strArray4[index].Split('&', StringSplitOptions.None);
                                string str4 = "增加";
                                string str5 = "";
                                if (strArray4[2] == "1")
                                    str5 = "%";
                                sb.AppendFormat("{0}{1}{2}{3}\n", Tools.m_AttrName[int.Parse(strArray5[0])], str4, strArray5[1], str5);
                            }
                            break;
                        }
                        break;
                    case ItemPrototype._SUB_CLASS.SC_MA:
                        sb.Append("可赠予他人");
                        break;
                    case ItemPrototype._SUB_CLASS.SC_CHENGHAO:
                        sb.Append(string.Format("用于垂钓，最多可同时使用{0}个鱼饵", int.Parse(miscvalue2)));
                        break;
                    case ItemPrototype._SUB_CLASS.SC_QINGGONG_UPGRADE:
                        sb.Append("用于武学，使用后学习" + item.Name_NOS);
                        break;
                    case ItemPrototype._SUB_CLASS.SC_TGT_CGL:
                        int result1;
                        if (Tools.IsStrAvailable(miscvalue2) && int.TryParse(miscvalue2, out result1))
                        {
                            Mystique info1 = Mystique.GetInfo((long)result1);
                            if (info1 != null)
                            {
                                if (info1.type == 1)
                                {
                                    sb.AppendFormat("使用后学习{0}，用于{1}", info1.name, Tools.livingTypeDes[info1.subtype.ToString()]);
                                    Mystique info2 = Mystique.GetInfo((long)int.Parse(info1.useEffect));
                                    Mystique.ParseMishuEffect(info2);
                                    sb.Append("\n赋予：" + Mystique.GetMishuEffectResultString(info2));
                                    break;
                                }
                                if (info1.type == 2)
                                {
                                    sb.AppendFormat("使用后学习{0}，用于{1}", info1.name, Tools.livingTypeDes[info1.subtype.ToString()]);
                                    int result2;
                                    if (!string.IsNullOrEmpty(info1.useEffect2) && int.TryParse(info1.useEffect2, out result2))
                                    {
                                        sb.Append("\n赋予：" + Mystique.GetMishuEffectResultString(Mystique.GetInfo((long)result2)));
                                        break;
                                    }
                                    break;
                                }
                                if (info1.type == 3)
                                {
                                    sb.AppendFormat("使用后学习{0}，用于天工图", Mystique.GetInfo((long)int.Parse(miscvalue2)).name);
                                    break;
                                }
                                break;
                            }
                            break;
                        }
                        sb.Append("用于生活系统");
                        break;
                    case ItemPrototype._SUB_CLASS.SC_MIJINODEWordEntry_DAN_YAO:
                        if (!item.m_IsCuiQu)
                        {
                            sb.Append(string.Format("以销毁原装备为前提，萃取{0}能力值的词条并覆写在其他装备上", item.GetTianGongTuNengLiZhi()));
                            break;
                        }
                        sb.Append(string.Format("可将当前{0}能力值的词条覆写到装备上", item.GetTianGongTuNengLiZhi()));
                        break;
                    case ItemPrototype._SUB_CLASS.SC_MIJINODEWordEntry_MI_JUAN:
                        string[] strArray6 = new string[5]
                        {
                "剑法",
                "刀法",
                "枪棍",
                "搏击",
                "通用"
                        };
                        int result3;
                        int.TryParse(item.Proto_NOS().miscvalue2, out result3);
                        sb.Append("用于" + (result3 > strArray6.Length - 1 ? strArray6[result3] : "") + "武学，使用后学习" + item.Name_NOS);
                        break;
                    case ItemPrototype._SUB_CLASS.SC_MATERIAL_MIFA:
                        int result4;
                        int.TryParse(item.Proto_NOS().miscvalue2.Split("|", StringSplitOptions.None)[1], out result4);
                        int result5;
                        int.TryParse(item.Proto_NOS().miscvalue2.Split("|", StringSplitOptions.None)[0], out result5);
                        int result6;
                        int.TryParse(item.Proto_NOS().miscvalue1, out result6);
                        sb.Append("用于武学，使用后激活<color=#5DBCF3>" + DataManager.GetStringLangById(ItemPrototype.GetItemPrototype((long)result6).nameId) + "</color>" + SpellPrototype.GetNameStr((long)result5) + "<color=#FFDA5A>" + MiJi_node.GetNameStr((long)result4) + "</color>");
                        break;
                    case ItemPrototype._SUB_CLASS.SC_CHONGWOSKILL:
                        sb.Append("使用后获得" + Stringlang.GetStr(ItemPrototype.GetItemPrototype((long)int.Parse(miscvalue2)).nameId));
                        break;
                    case ItemPrototype._SUB_CLASS.SC_MATERIAL_END:
                        sb.Append("境界提升时使用");
                        break;
                }
                break;
        }
        return sb.ToString();
    }

    // This is most items description - warning if the underlying code changes this breaks badly
    public static string Replacement_GetSubType456Des(string itemModifiersRaw, string itemRestrictionsRaw, string itemBonusesRaw)
    {
        StringBuilder builder = new StringBuilder();

        var itemModifiers = itemModifiersRaw.Split('|', StringSplitOptions.None);
        List<int> recoveryItemIds = new List<int>() { 2, 3, 33, 83 };

        for (int index = 0; index < itemModifiers.Length; ++index)
        {
            var itemProperties = itemModifiers[index].Split('&', StringSplitOptions.None);
            
            var itemIdString = itemProperties[0];
            int.TryParse(itemIdString, out var itemId);

            var prefix = index == 0 ? "After use " : "， ";
            var modifiedDesc = !recoveryItemIds.Contains(itemId) ? "Increase" : "Recover";
            var statModified = Tools.bagTypeName.ContainsKey(itemIdString) ? Tools.bagTypeName[itemIdString] : Tools.m_AttrName[itemId];
            var amount = itemProperties[1];
            var amountSuffix = itemProperties[2] == "1" ? "%" : "";

            builder.Append($"{prefix} {modifiedDesc} {statModified} by {amount}{amountSuffix}");
        }

        if (Tools.IsStrAvailable(itemRestrictionsRaw))
        {
            var itemRestrictions = itemRestrictionsRaw.Split("&", StringSplitOptions.None)[1].Split("|", StringSplitOptions.None);

            if (itemRestrictions[2] == "<")
                builder.AppendFormat("，this item can only be used {0} times.", itemRestrictions[3]); //This item can only be used {0} times
        }

        if (Tools.IsStrAvailable(itemBonusesRaw))
        {
            string[] itemBonuses = itemBonusesRaw.Split("|", StringSplitOptions.None);
            if (itemBonuses[0] == "0")
            {
                var itemProperties = itemBonuses[1].Split("&", StringSplitOptions.None);

                var itemIdString = itemProperties[0];
                int.TryParse(itemIdString, out var itemId);

                var prefix = "，On first use get an extra: ";
                var modifiedDesc = !recoveryItemIds.Contains(itemId) ? "Increase" : "Recover";
                var statModified = Tools.bagTypeName.ContainsKey(itemIdString) ? Tools.bagTypeName[itemIdString] : Tools.m_AttrName[itemId];
                var amount = itemProperties[1];
                string amountSuffix = itemProperties[2] == "0" ? "" : "%";

                builder.Append($"{prefix} {modifiedDesc} {statModified} by {amount}{amountSuffix}");
            }
        }

        return builder.ToString();
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Item), nameof(Item.GetSubType456Des))]
    public static bool Prefix_GetSubType456Des(Item __instance, ref string __result, string misc2, string misc1, string misc3)
    {
        __result = Replacement_GetSubType456Des(misc2, misc1, misc3);
        return false;
    }
}
