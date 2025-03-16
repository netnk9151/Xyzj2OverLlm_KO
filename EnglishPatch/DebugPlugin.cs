using BepInEx;
using BepInEx.Logging;
using EnglishPatch.Support;
using HarmonyLib;
using SweetPotato;
using System.Collections.Generic;
using System.Reflection;
using LitJson;

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

/* 
[Error  :XUnity.Common] An error occurred while invoking AssetLoaded event.
System.NullReferenceException
 at (wrapper managed-to-native) UnityEngine.Object.GetName(UnityEngine.Object)
 at UnityEngine.Object.get_name () [0x00001] in <c6f9c541975c45798261d77d99bb6eb2>:0 
 at EnglishPatch.Sprites.SpriteReplacerPlugin.ReplaceSpriteInAsset (System.String parentAssetName, UnityEngine.UI.Image child) [0x00020] in <f89debbaca4946c798a611fd74046fa9>:0 
 at EnglishPatch.Sprites.SpriteReplacerPlugin.OnAssetLoaded (XUnity.ResourceRedirector.AssetLoadedContext context) [0x000f6] in <f89debbaca4946c798a611fd74046fa9>:0 
 at XUnity.ResourceRedirector.ResourceRedirection.FireAssetLoadedEvent (XUnity.ResourceRedirector.AssetLoadedParameters parameters, UnityEngine.AssetBundle assetBundle, UnityEngine.Object[]& assets) [0x001db] in <2aa225b7d50341e7b2dc1bfd1a8d4bf7>:0 

       [Error  : Unity Log] NullReferenceException: Object reference not set to an instance of an object
Stack trace:
SpellItemSlotNew.OnSelected () (at <ae2a872697a14070ab6bb7ed0aae4e78>:0)
SweetPotato.UIItemSlot.set_IsSelected (System.Boolean value) (at <ae2a872697a14070ab6bb7ed0aae4e78>:0)
SweetPotato.UIItemGrid.SelectItemSlot (SweetPotato.UIItemSlot selectItem, System.Boolean notifyMsg) (at <ae2a872697a14070ab6bb7ed0aae4e78>:0)
SweetPotato.UIItemGrid+<RefreshSlotsRoutine>d__62.MoveNext () (at <ae2a872697a14070ab6bb7ed0aae4e78>:0)
UnityEngine.SetupCoroutine.InvokeMoveNext (System.Collections.IEnumerator enumerator, System.IntPtr returnValueAddress) (at <c6f9c541975c45798261d77d99bb6eb2>:0)
UnityEngine.MonoBehaviour:StartCoroutine(IEnumerator)
SweetPotato.UIItemGrid:RefreshSlots(Int32)
SweetPotato.UIItemGrid:set_DataProvider(List`1)
NpcInformationView:DMD<NpcInformationView::ShowNpcSpell>(NpcInformationView)
NpcInformationView:ShuXing()
NpcInformationView:OnSelectComplete()
NpcInformationView:<Awake>b__57_1()
UnityEngine.EventSystems.EventSystem:Update()

*/

    private void Test()
    {


    }

    //// Opening Screen
    //[HarmonyPrefix, HarmonyPatch(typeof(SweetPotato.LoginViewNew), "OnButtonClick")]
    //public static void Postfix_OnButtonClick()
    //{
    //    Logger.LogWarning($"Hooked POSTFIX OnButtonClick!");
    //}

    ////[HarmonyPostfix, HarmonyPatch(typeof(SweetPotato.LoginViewNew), "OnButtonClick")]
    ////public static void Postfix_OnButtonClick(IEnumerable<CodeInstruction> __instructions)
    ////{
    ////    InstructionLogger.LogInstructions(__instructions);
    ////}

    //[HarmonyPostfix, HarmonyPatch(typeof(SweetPotato.LoginViewNew), "OnButtonNewGame")]
    //public static void Postfix_LoginViewNew_OnButtonNewGame()
    //{
    //    Logger.LogWarning("Hooked OnButtonNewGame!");
    //}
}

