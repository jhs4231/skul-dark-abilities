using System;
using System.Linq;
using System.Collections.Generic;
using Characters.Gear.Upgrades;
using Characters.Player;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace FanDarkAbilities
{
    [HarmonyPatch(typeof(UI.Upgrades.UpgradableContainer),"CreateElements")]
    static class ShopPagingHook
    {
        static void Postfix(UI.Upgrades.UpgradableContainer __instance){
            var pages=__instance.GetComponent<ShopPages>();if(pages==null)pages=__instance.gameObject.AddComponent<ShopPages>();pages.Initialize(__instance);
        }
    }
    [HarmonyPatch(typeof(UI.Upgrades.UpgradableContainer),"GetDefaultFocusTarget")]
    static class PageFocusHook
    {
        static bool Prefix(UI.Upgrades.UpgradableContainer __instance,ref UI.Upgrades.UpgradeElement __result){
            var pages=__instance.GetComponent<ShopPages>();if(pages==null)return true;
            __result=pages.First();return false;
        }
    }
    public sealed class ShopPages : MonoBehaviour
    {
        UI.Upgrades.UpgradableContainer container;
        UI.Upgrades.UpgradeElement[] elements;
        int page; const int size=27;Font font;
        public void Initialize(UI.Upgrades.UpgradableContainer c){
            container=c;elements=c.GetComponentsInChildren<UI.Upgrades.UpgradeElement>(true).Where(e=>e.reference.type==UpgradeObject.Type.Normal).ToArray();
            if(elements.Length>0){var grid=elements[0].transform.parent.GetComponent<GridLayoutGroup>();if(grid!=null){grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=9;}}
            Show(Math.Min(page,Math.Max(0,(elements.Length-1)/size)),false);
        }
        public UI.Upgrades.UpgradeElement First(){return elements==null?null:elements.FirstOrDefault(e=>e!=null&&e.gameObject.activeSelf&&e.selectable.interactable);}
        void Show(int p,bool focus){
            page=p;for(int i=0;i<elements.Length;i++)elements[i].gameObject.SetActive(i/size==page);
            if(focus){var e=First();if(e!=null){var panel=(UI.Upgrades.Panel)AccessTools.Field(typeof(UI.Upgrades.UpgradableContainer),"_panel").GetValue(container);panel.Focus(e.selectable);}}
        }
        void Update(){if(elements==null||elements.Length<=size)return;int count=(elements.Length+size-1)/size;
            if(Input.GetKeyDown(KeyCode.PageDown))Show((page+1)%count,true);
            if(Input.GetKeyDown(KeyCode.PageUp))Show((page+count-1)%count,true);
        }
        void OnGUI(){
            if(elements==null||elements.Length<=size)return;int count=(elements.Length+size-1)/size;
            if(font==null)font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",18);
            var style=new GUIStyle(GUI.skin.button);style.font=font;style.fontSize=Mathf.Max(14,Screen.height/60);
            float width=Screen.width*.17f,x=Screen.width*.105f,y=Screen.height*.81f,height=Screen.height*.04f;
            if(GUI.Button(new Rect(x,y,width*.24f,height),"◀",style))Show((page+count-1)%count,true);
            GUI.Label(new Rect(x+width*.27f,y,width*.55f,height),string.Format("{0} / {1}  [PgUp / PgDn]",page+1,count),style);
            if(GUI.Button(new Rect(x+width*.76f,y,width*.24f,height),"▶",style))Show((page+1)%count,true);
        }
    }
    [HarmonyPatch(typeof(UpgradeShop),"CheckPurchasable")]
    static class WyvernPurchaseHook
    {
        static void Postfix(UpgradeResource.Reference reference,ref bool __result){
            if(reference.name!="FanWyvern"||!__result)return;
            var p=Singletons.Singleton<Services.Service>.Instance.levelManager.player;
            if(p.playerComponents.inventory.upgrade.upgrades.Count(u=>u==null)<2)__result=false;
        }
    }
    [HarmonyPatch(typeof(UpgradeInventory),"EquipAt",new Type[]{typeof(UpgradeResource.Reference),typeof(int)})]
    static class WyvernSlotHook
    {
        static void Postfix(UpgradeInventory __instance,UpgradeResource.Reference upgrade,int index){
            if(upgrade.name!="FanWyvern"||__instance.upgrades.Any(u=>u!=null&&u.reference.name=="FanWyvernReserve"))return;
            var reserve=UpgradeResource.instance.upgradeReferences.First(r=>r.name=="FanWyvernReserve");__instance.TryEquip(reserve);
        }
    }
    [HarmonyPatch(typeof(UpgradeShop),"TryRemoveUpgradeObjet")]
    static class WyvernRemoveHook
    {
        static bool Prefix(ref UpgradeResource.Reference reference,ref bool __result){
            var d=Catalog.Find(reference.name);if(d!=null&&!d.removable){__result=false;return false;}
            if(reference.name=="FanWyvernReserve")reference=UpgradeResource.instance.upgradeReferences.First(r=>r.name=="FanWyvern");return true;
        }
        static void Postfix(UpgradeResource.Reference reference,bool __result){
            if(!__result||reference.name!="FanWyvern")return;
            var inv=Singletons.Singleton<Services.Service>.Instance.levelManager.player.playerComponents.inventory.upgrade;
            int i=inv.upgrades.FindIndex(u=>u!=null&&u.reference.name=="FanWyvernReserve");if(i>=0)inv.Remove(i);inv.Trim();
        }
    }
}
