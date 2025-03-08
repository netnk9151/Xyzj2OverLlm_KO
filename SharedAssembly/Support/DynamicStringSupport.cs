using System.Text.RegularExpressions;
using System.Text;
using EnglishPatch.Contracts;
using System.Linq;

namespace Translate.Support;

public class DynamicStringSupport
{

    public static bool IsSafeContract(DynamicStringContract contract, bool skipCombos = false)
    {
        string[] skipTypes = [
            // Game Components
            "Automat",
            "CustomDataInfo",
            "SpawnPointPrototype",
            "SystemIntroduce",
            "DownloadView",
            "PlayerStateComponent",
            "DataManager",
            "FashionComponent",
            "BuildEntity",
            "GmManager",
            "AIDialogView",
            "EndingView",
            "UploadFile",
            "DataMgr",
            "EditWayPointData",
            "AppGame",
            "Demolish",
            "Excalibur",
            "JHPerfSight",
            "MapAreaEvent",
            "MouseSimulator",
            "NpcChatView",
            "NpcRoleNodeNew",
            "OpenFileName",
            "PatchManager",
            "ScreenShotFaceParam",
            "StuntManager",
            "TestManagedHeap",
            "VirtualEvent",
            "VirtualGrid",
            "WaveController",
            "SweetPotato.WordEntryGlobalManager",
            "SweetPotato.NpcPrototypeBaseRandom",
            "SweetPotato.FileView",
            "FloydManager",

            // Looks like modding ui
            "EditNpcData",
            "EditorDataPanel",
            "EditSpwData",
            "EditTeleData",
            "MapAreaEditor",
            "ModSpace",

            //Valid but needs to be debugged:
            "RelationShipManager",
        ];
        
        string[] skipMethods = [
            "CheckTapAnti",
            "CheckDBVersionAndDownloadFromServer",
            "IsSaveableNow",
            "LoadCSV",
            "LoadDB",
            "CreateFromCsvRow",
            "OnButtonClick", //Bad stuff happens here
         ];

        // TODO: Figure out why these dont patch
        string[] skipCombinations = [
            "AnqiResearchView.RefreshCailiaoAndBtn",
            "AnqiResearchView.RefreshCondition",
            "ArtistryView.ConsultComplete",
            "ArtistryView.PreachCallBack",
            "AttriPart.BtnLeaveTeamClick",
            "AttriPart.BtnTeamStateClick",
            "AttriPart.OnInit",
            "AttriPart.OnStateBtnClick",
            "AttriPart.SwitchTeammate",
            "AutomatManager.Script_CheckFeiGeFinish",
            "AutomatManager.Script_CheckInteractNpcJieYuanOrJieYi",
            "AutomatManager.Script_GetInteractNpcId",
            "AutomatManager.Script_LearnMiShuMiFa",
            "AutomatManager.Script_OpenBox",
            "AutomatManager.Script_OpenBoxSelf",
            "AutomatManager.Script_RemoveLearnedMiShuMiFa",
            "AutomatManager.Script_SetNpcPostionCondition",
            "AutomatManager.Script_SetNpcPostionWithCurNpc",
            "AutomatManager.Script_ShowKeyTip",
            "AutomatManager.Script_StartAnswer",
            "AutomatManager.Script_StartAnswerCondition",
            "AutomatManager.Script_WandingOnly",
            "BuYeJingManager.CreateLeiTaiNpc",
            "BuYeJingManager.DoLunCiPaiMaiItemSendFeiGe",
            "BuYeJingManager.SetJingYuZhuNpcPos",
            "BuYeJingManager/<>c__DisplayClass51_0.<CreateLeiTaiNpc>b__0",
            "CardSkillInfo.CreateFromCsvRow",
            "CharacterView.OnReset",
            "CommonTipView.OnShowMenuItems",
            "ControllerEquipPart.OnInit",
            "DLCView.Buy",
            "DownloadView/<Down>d__20.MoveNext",
            "EditEntrustEventPointData.SavePointToDB",
            "EditWayPointData.CheckSelection",
            "EditWayPointData.SavePointToDB",
            "EntrustManager.DoEventAcceptPreAction",
            "FabricateItemSlot.OnReset",
            "FabricationView.GetConditionLessStr",
            "FabricationView.GetMishuMifaEffects",
            "FaceModelView.GetEffectDes",
            "FaceSetItem.SetBaseValue",
            "HandbookView.RefreshProperty",
            "InteractOther.SetInteract",
            "InteractSlot.Handle_IT_SCRIPT",
            "InteractSlot.OnReset",
            "InteractTipSlot.OnReset",
            "ItemDetailFabrication.CheckTextLink",
            "ItemDetailView.EquipOrUnEquip",
            "ItemDetailView.ShowItem",
            "ItemDetailView.ShowItem",
            "ItemEquip.GetEntry",
            "ItemPrototype.GetItemTypeDes",
            "ItemPrototype.InitRandomItems",
            "JailView.KaoWen_JiaoDai",
            "JailView.KaoWen_RenZui",
            "JingJieGraphMgr.YongLingActive",
            "JingJiePartView.GuLingCondition",
            "JingJiePartView.YongLingCondition",
            "JingMaiActiveConditionItemSlot.OnReset",
            "LianZhaoGraphMgr.XiuLianNew",
            "LianZhaoPartView.RefreshSelected",
            "LivingSystem.OpenLivingView",
            "LivingSystem.OpenLivingView",
            "LivingView.OnMenuItemClicked",
            "LivingView/<MakeItems>d__101.MoveNext",
            "LootItem.Init",
            "LunDaoBattle/<OnCastSkill>d__77.MoveNext",
            "LunDaoBattle/<OnSkillTrigger>d__75.MoveNext",
            "LunDaoBattle/<OnTriggerSkill>d__76.MoveNext",
            "MailChatItemSlot.Refresh",
            "MainPanel.ShowEnermy",
            "MapShiLiItem.Init",
            "MaterialSelectView.<Awake>b__38_2",
            "MenPai.GetInfoById",
            "MenPaiAttriPrefab.Refresh",
            "MenPaiChuZhengPanel.RefreshPathInfo",
            "MenPaiEntrustPrefab.OnRefresh",
            "MenPaiFaZhanPanel.RefreshInfo",
            "MenPaiNaXianPanel.GetStr",
            "MenPaiWarTipPrefab.Refresh",
            "MiJiActiveConditionItemSlot.OnReset",
            "MiJiMgr.Add",
            "MiJiSpellPassiveConditionItemSlot.OnReset",
            "ModSpace.ConditionProtoData.FormString",
            "ModSpace.DataManager.GetCommandName",
            "ModSpace.ModelView.Close",
            "ModSpace.NpcAttriDynamic.GetInfo",
            "ModSpace.WorksShopManager.SubmitItemUpdateResult",
            "Mystique.GetMishuEffectResult",
            "MyTimeline.ReBindPlayerController",
            "NewChuanWenView/<OnShowChuanWen>d__18.MoveNext",
            "NpcInteractButtonSlot.OnThisBtnClick",
            "NpcInteractButtonSlot.set_Interact",
            "PaiMaiHangView.OnShuaXinClick",
            "PaiMaiHangView.RefreshSelectedItem",
            "PingTuView.OnBlockEndDrag",
            "PlayerRESlot.OnReset",
            "PlayerStateComponent_Base.EnterCheckQingGongState",
            "PolygonDrawer.RemoveVertex",
            "PopInfoTip.GetMilitaryDes",
            "PopInfoTip.ShowInformation",
            "PopInfoTip.ShowInformation",
            "PropItemUI.GetRateStr",
            "QingGongNodeItemSlot.OnReset",
            "QuestJieMi.CreateFromCsvRow",
            "QuestViewNew.CheckTextLink",
            "RandomNpcManager.Refresh",
            "RecipeSlot.OnReset",
            "RelationPanelNew.OnInit",
            "RoleInformation.GetMilitaryDes",
            "RoleInformation.ShowInformation",
            "RoleInformation.ShowInformation",
            "SectSystem.GetEntitySectName",
            "SectSystem.GetSectName",
            "SectSystem.ModifySectMember",
            "ShiLiItemInteractSlot.OnReset",
            "ShopStoreSystem.OnEnterShopMode",
            "ShopStoreViewNew.UpdateShopRefreshTime",
            "ShopViewNew.OnClickToggle",
            "ShopViewNew.UpdateShopRefreshTime",
            "SkillItemUI.CastSpellReal",
            "SpellItemSlotNew.OnReset",
            "SweetPotato.AttriManager.GetAttriResultDes",
            "SweetPotato.AutomatScriptManager.PrintExeFuctionScript",
            "SweetPotato.ChronicleYearItem.SetItem",
            "SweetPotato.ChuanWenDetail.CheckTextLink",
            "SweetPotato.ConditionPrototype.ParseCondition",
            "SweetPotato.FloydManager.GetPathPiontPos",
            "SweetPotato.GameSaving.Save",
            "SweetPotato.GrowInfo.OnEntrustFinish",
            "SweetPotato.InstantiateShengFenPanel.Refresh",
            "SweetPotato.InstantiateViewNewNew_mobile/<EnterCharacterLine>d__111.MoveNext",
            "SweetPotato.InstantiateViewNewNew_mobile/<ShowLoginMoveTMLNEW_NEW>d__112.MoveNext",
            "SweetPotato.Item.get_RequestDes_NOS",
            "SweetPotato.Item.GetItemMiscvaluePurpose",
            "SweetPotato.Item.GetParseName",
            "SweetPotato.Item.GetSubType456Des",
            "SweetPotato.ItemStorage.DoMatchResultsTip",
            "SweetPotato.ItemStorage.GetWeaponDes",
            "SweetPotato.ItemStorage.RepairEquip",
            "SweetPotato.ItemStorage.UseItem",
            "SweetPotato.JieMiQuestViewNew_TuiLiPanel.OnXSZCSelect",
            "SweetPotato.JingJie_node.CreateFromCsvRow",
            "SweetPotato.JingMai.JingMaiDetailConditionItem.InitItem",
            "SweetPotato.JingMai.JingMaiXinFaSelectPanel.GetNameStr",
            "SweetPotato.JingMai.JingMaiYunGongSelectItemSlot.OnReset",
            "SweetPotato.JingMai.JingMaiZhouTianEffectItem.GetJingMaiName",
            "SweetPotato.JingMai_node.CreateFromCsvRow",
            "SweetPotato.JingMai_node.GetJingMaiTypeName",
            "SweetPotato.JingMai_ZhouTianEffect.GetEffectIdList",
            "SweetPotato.JingMai_ZhouTianEffect.GetEffectStrByType",
            "SweetPotato.JingMaiEditManager.CreateOrWashWordEntry",
            "SweetPotato.LianZhao_node.CreateFromCsvRow",
            "SweetPotato.LoginViewNew/<AsyncSetLoadVirtualCamera>d__31.MoveNext",
            "SweetPotato.MiniGames.GeZi1.GeZiGame.InitGame",
            "SweetPotato.MenPaiEntity.NpcGetCurMonthGongZi",
            "SweetPotato.MenPaiManager.OnNpcDead",
            "SweetPotato.MiJiPage.Load",
            "SweetPotato.NpcAttriDynamicExtralTemplete.GetInfo",
            "SweetPotato.NpcEntity.get_GenrePostName_NOS",
            "SweetPotato.NpcEntity.get_SectNameAll_NOS",
            "SweetPotato.NpcEntity.Init",
            "SweetPotato.NpcEntity.InitNpcAtt",
            "SweetPotato.NpcEntity.InitNpcSpell",
            "SweetPotato.NpcEntity.LearnPeifang",
            "SweetPotato.NpcSpellDynamic.GetInfo",
            "SweetPotato.NpcSpellDynamic.Init",
            "SweetPotato.NpcSpellDynamic.InitNpcSpell",
            "SweetPotato.NpcSpellDynamicName.CreateFromCsvRow",
            "SweetPotato.PlayerQuest.Load",
            "SweetPotato.QingGong_node.CreateFromCsvRow",
            "SweetPotato.QuestLog.GetJieMiConditionType",
            "SweetPotato.QuestViewNew.QuestRewardItemTip.ShowItemTip",
            "SweetPotato.QuestViewNew.QuestRewardItemTipByXSY.ShowItemTip",
            "SweetPotato.RandomEventsStorage/CompleteState.ShowResult",
            "SweetPotato.RandomEventsStorage/WaitCompleteState.OnCheckRandomEventState",
            "SweetPotato.RandomEventsStorage/WaitCompleteState.OnEnter",
            "SweetPotato.RandomName.CreateFromCsvRow",
            "SweetPotato.SettingViewNew.OnToggleClick",
            "SweetPotato.SettingViewNew.SetAutoSave",
            "SweetPotato.SettingViewNew.SetAutoTalkTip",
            "SweetPotato.SettingViewNew.SetBgm",
            "SweetPotato.SettingViewNew.SetCache",
            "SweetPotato.SettingViewNew.SetCaoZuoTip",
            "SweetPotato.SettingViewNew.SetChaoZuoMoshi",
            "SweetPotato.SettingViewNew.SetHeadIconTip",
            "SweetPotato.SettingViewNew.SetShoubingView_LR",
            "SweetPotato.SettingViewNew.SetShoubingView_UD",
            "SweetPotato.ShangChengViewNew.OnChuanDaiBtnClick",
            "SweetPotato.ShangChengViewNew.RefreshJiNengTeXiaoSelectItem",
            "SweetPotato.ShangChengViewNew.RefreshWuqiTeXiaoSelectItem",
            "SweetPotato.SomeBoneManager.ChangeUnitSocialData",
            "SweetPotato.SpawnPointChild.CreateUnitEntity",
            "SweetPotato.Spell.CheckEffectCond",
            "SweetPotato.Spell.PlayCastAnim",
            "SweetPotato.SpellEditView.CheckCondition",
            "SweetPotato.SpellEditView.RefreshCanwu",
            "SweetPotato.SpellEditView.ShowChangeTaoLu",
            "SweetPotato.SpellManager.ChangeSpellTaoLu",
            "SweetPotato.SpellManager.LearnSpell",
            "SweetPotato.SpellManager.SetLianZhaoInfo",
            "SweetPotato.SpellManager.UpSpellAndBuff",
            "SweetPotato.TeleportTrans.GetInfo",
            "SweetPotato.Tools.GetAttriTypeByLiveSkillType",
            "SweetPotato.Tools.GetItemEquipDes",
            "SweetPotato.Tools.GetRealTimeDate2",
            "SweetPotato.Tools.IsVigorEngEnough",
            "SweetPotato.Tools.NumberToChinese",
            "SweetPotato.UnitController.OnHPChanged",
            "SweetPotato.UnitController.SetInSightCombat",
            "SweetPotato.UnitController/<MoveAlongPointByBiao>d__220.MoveNext",
            "SweetPotato.WashIntensify.WashIntensifyCostSlotUI.OnReset",
            "SweetPotato.WordEntryLooter.GetWordDes",
            "SweetPotato.WordEntryLooter.GetWordDesNew",
            "SweetPotato.WorldManager.LoadSceneState",
            "SweetPotato.WorldManager.set_IsShouBingKeyboard",
            "SweetPotato.YunBiao.BiaoBasicInfo.GetInfo",
            "SweetPotato.YunBiao.BiaoTeamManager.CreateTeam",
            "SynthesisItemSlot.OnReset",
            "SystemIntroduceView.OnReset",
            "TeammateSlot.SetSlotEnabled",
            "TianGongTuView/<>c__DisplayClass95_0.<DoFuXieAnim>b__0",
            "UnityHelper.GetNunWordFromNum",           
            "WashIntensifyView.CanShowPanel",
            "WineGameView.OnNpcAdd",
            "WineGameView.OnSet",
            "XiaYunLuManager.GetBangName",
            "XiaYunLuManager.OnDaTingFeiGeSend",
            "XiaYunLuManager.RefreshXiaYunBang",
            "XinfaItemSlot.OnReset",
            "XuanShangLingItem/<>c__DisplayClass17_0.<Awake>b__2",
            "XuanShangQuest.DoInitPostAction",
        ];

        var combo = $"{contract.Type}.{contract.Method}";

        if (skipTypes.Contains(contract.Type))
            return false;

        foreach (var type in skipTypes)
            if (contract.Type.Contains(type))
                return false;

        if (skipMethods.Contains(contract.Method))
            return false;

        // These are used purely for hiding stuff we dont know what it does yet
        if (!skipCombos && skipCombinations.Contains(combo))
            return false;

        return true;
    }

    public static string[] PrepareMethodParameters(string split)
    {
        var rawParameters = split[1..^1] //Remove square brackets
            .Replace("，", ","); // Replace Wide quotes back

        string splitPattern = @",(?![^\[\]{}<>]*[\]\}>])";
        string replacement = "，";  // Change to whatever you want

        // Replace for generics because split regex is flakey
        rawParameters = ReplaceCommasInBrackets(rawParameters, replacement);

        var parameters = Regex.Split(rawParameters, splitPattern) ?? [];

        // Replace back for generics
        for (int i = 0; i < parameters.Length; i++)
            parameters[i] = parameters[i].Replace(replacement, ",");

        //parameters = parameters.Where(p => !string.IsNullOrEmpty(p)).ToArray(); -- Serialises wierd
        return parameters;
    }

    static string ReplaceCommasInBrackets(string input, string replacement)
    {
        var output = new StringBuilder();
        int depth = 0;

        foreach (char c in input)
        {
            if (c == '<' || c == '[' || c == '{')
                depth++;  // Entering a bracketed section

            if (c == '>' || c == ']' || c == '}')
                depth--;  // Exiting a bracketed section

            if (c == ',' && depth > 0)
                output.Append(replacement);  // Replace ',' only inside brackets
            else
                output.Append(c);
        }

        return output.ToString();
    }
}
