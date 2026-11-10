using System;
using System.Collections.Generic;
using System.Linq;
using Characters;
using Characters.Gear.Upgrades;
using GameResources;
using HarmonyLib;
using UnityEngine;

namespace FanDarkAbilities
{
    [HarmonyPatch(typeof(MapReward),"get_isHardmodeItem")]
    static class OmenRewardTypeHook
    {
        static void Postfix(MapReward __instance,ref bool __result){var h=EffectHost.Player;if(h!=null&&h.Has(Gift.DeadChest)&&__instance.type==MapReward.Type.Item)__result=true;}
    }
    [HarmonyPatch(typeof(MapReward),"Activate")]
    static class MissingChestHook
    {
        static bool Prefix(MapReward __instance,ref bool __result){
            var h=EffectHost.Player;if(h==null||!h.Has(Gift.DeadChest)||__instance.type!=MapReward.Type.Item||__instance.activated)return true;
            if(UnityEngine.Random.value>=Math.Min(1,h.Scale(Gift.DeadChest,.66)))return true;
            __instance.activated=true;__result=false;return false; // Native caller opens the gates without waiting for loot.
        }
    }
    [HarmonyPatch(typeof(Level.Chest),"Load")]
    static class OmenChestScope
    {
        internal static int depth;
        static void Prefix(){depth++;}
        static Exception Finalizer(Exception __exception){depth--;return __exception;}
    }
    [HarmonyPatch(typeof(Services.GearManager),"GetItemToTake",new Type[]{typeof(System.Random),typeof(Rarity)})]
    static class OmenNormalChestHook
    {
        static bool Prefix(Services.GearManager __instance,System.Random random,ref ItemReference __result){
            var h=EffectHost.Player;if(h==null||!h.Has(Gift.DeadChest)||OmenChestScope.depth<=0)return true;
            __result=__instance.GetOmenItems(random);return false;
        }
    }
    [HarmonyPatch(typeof(Level.Chest),"Initialize")]
    static class OmenChestArtHook
    {
        static void Postfix(Level.Chest __instance){
            var h=EffectHost.Player;if(h==null||!h.Has(Gift.DeadChest))return;
            var prefab=CommonResource.instance.hardmodeChest.GetComponent<Level.HardmodeChest>();if(prefab==null)return;
            var rarity=(Rarity)AccessTools.Field(typeof(Level.Chest),"_rarity").GetValue(__instance);
            string field=rarity==Rarity.Legendary?"_legendaryOmenChest":rarity==Rarity.Unique?"_uniqueOmenChest":rarity==Rarity.Rare?"_rareOmenChest":"_commonOmenChest";
            var animator=(Animator)AccessTools.Field(typeof(Level.Chest),"_animator").GetValue(__instance);
            animator.runtimeAnimatorController=(RuntimeAnimatorController)AccessTools.Field(typeof(Level.HardmodeChest),field).GetValue(prefab);
        }
    }
    [HarmonyPatch(typeof(Level.Npc.FieldNpcs.FieldNpc),"OnCageDestroyed")]
    static class FieldSacrificeHook
    {
        static bool Prefix(Level.Npc.FieldNpcs.FieldNpc __instance){
            var h=EffectHost.Player;if(h==null||!h.Has(Gift.RedCrown))return true;
            h.sacrifices=Math.Min(6,h.sacrifices+1);h.Refresh();FanEffect.Flash(h,__instance.transform.position,Color.red,1);UnityEngine.Object.Destroy(__instance.gameObject);return false;
        }
    }
    [HarmonyPatch(typeof(Minion),"TryToExpire")]
    static class MonkeyLifetimeHook
    {
        static void Prefix(Minion __instance,ref float ____elapsedLifeTime){
            var h=EffectHost.Player;if(h==null||!h.Has(Gift.Destiny)||__instance.leader==null||__instance.leader.player!=h.owner||__instance.GetComponent<Characters.AI.StoneMonkeyBunsin>()==null)return;
            ____elapsedLifeTime-=Chronometer.global.deltaTime*(1-1/(1+.5f*(float)h.Power(Gift.Destiny)));
        }
    }
    [HarmonyPatch(typeof(Characters.Operations.GrabBorad.AddToGrabBoard),"Run")]
    static class AsceticGrabHook
    {
        static void Postfix(Character ____owner){
            var h=EffectHost.Player;if(h==null||!h.Has(Gift.Destiny)||____owner!=h.owner||SkullRules.State(h).family!="Ascetic")return;
            if(h.Ready("AsceticGrab",.25f)&&++SkullRules.State(h).grabs%3==0)h.Burst(h.owner.transform.position,4,0,"Ascetic",true);
        }
    }
}
