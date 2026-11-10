using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Characters;
using Characters.Abilities;
using Characters.Gear;
using Characters.Gear.Weapons;
using HarmonyLib;
using UnityEngine;

namespace FanDarkAbilities
{
    [HarmonyPatch(typeof(RarityPrices),"get_Item",new Type[]{typeof(Rarity)})]
    static class SpiderPriceHook
    {
        static void Postfix(RarityPrices __instance,ref int __result){var h=EffectHost.Player;if(h!=null&&h.Has(Gift.Spider)&&ReferenceEquals(__instance,Settings.instance.bonesToUpgrade))__result=(int)Math.Ceiling(__result*(1+h.Scale(Gift.Spider,.8)));}
    }
    internal static class ExplosionRules
    {
        internal static int depth;
        static float last=-100;
        public static bool Named(string name){string n=(name??"").ToLowerInvariant();return n.Contains("explos")||n.Contains("explode")||n.Contains("bomb")||n.Contains("blast")||n.Contains("폭발");}
        public static double Multiplier(EffectHost h){
            if(!h.Has(Gift.BigBang))return 1;
            if(h.time<last||h.time-last>=1)last=h.time;
            return h.time-last<.025f?1+h.Scale(Gift.BigBang,.25*h.Level(Gift.BigBang)):1;
        }
    }
    // Local range changes are restored even if an attack throws.
    [HarmonyPatch]
    static class ExplosionAttackHook
    {
        sealed class Snapshot{public Transform range;public Vector3 scale;public bool active;}
        static IEnumerable<MethodBase> TargetMethods(){
            foreach(var t in new[]{typeof(Characters.Operations.Attack.InstantAttack),typeof(Characters.Operations.Attack.SweepAttack),typeof(Characters.Operations.Attack.RingAttack),typeof(Characters.Operations.Attack.CastAttack)})yield return AccessTools.Method(t,"Run",new[]{typeof(Character)});
        }
        static void Prefix(Component __instance,Character owner,out Snapshot __state){
            __state=new Snapshot();var h=EffectHost.Player;if(h==null||owner!=h.owner||!h.Has(Gift.BigBang))return;
            Transform p=__instance.transform;bool match=false;
            for(int i=0;p!=null&&i<4;i++,p=p.parent)if(ExplosionRules.Named(p.name)){match=true;break;}
            if(!match)return;
            __state.active=true;ExplosionRules.depth++;
            double m=ExplosionRules.Multiplier(h);
            var prop=AccessTools.Property(__instance.GetType(),"range");var range=prop==null?null:prop.GetValue(__instance,null) as Collider2D;
            if(range!=null){__state.range=range.transform;__state.scale=range.transform.localScale;range.transform.localScale=__state.scale*(float)m;}
        }
        static Exception Finalizer(Exception __exception,Snapshot __state){if(__state!=null&&__state.active){if(__state.range!=null)__state.range.localScale=__state.scale;ExplosionRules.depth--;}return __exception;}
    }
    [HarmonyPatch(typeof(Characters.Operations.Summon.SummonMinion),"Run")]
    static class MonkeySummonHook
    {
        internal static bool summoning;
        static void Prefix(Character owner){var h=EffectHost.Player;summoning=h!=null&&h.Has(Gift.Destiny)&&owner==h.owner&&SkullRules.State(h).family=="Monkey";}
        static Exception Finalizer(Exception __exception){summoning=false;return __exception;}
    }
    [HarmonyPatch(typeof(Characters.Gear.Weapons.Gauges.ValueGauge),"Consume")]
    static class MonkeyCostHook
    {
        static void Prefix(Characters.Gear.Weapons.Gauges.ValueGauge __instance,ref float amount){var h=EffectHost.Player;if(h!=null&&h.Has(Gift.Destiny)&&__instance.transform.IsChildOf(h.owner.playerComponents.inventory.weapon.polymorphOrCurrent.transform)&&SkullRules.State(h).family=="Monkey"&&UnityEngine.Random.value<.25*h.Power(Gift.Destiny))amount=0;}
    }
    [HarmonyPatch(typeof(Characters.Gear.Weapons.GhoulHook),"Pull")]
    static class GhoulHookRule
    {
        static void Prefix(BoxCollider2D ____pullCollider){var h=EffectHost.Player;if(h==null||!h.Has(Gift.Destiny)||SkullRules.State(h).family!="Ghoul"||____pullCollider==null)return;
            foreach(var e in h.Enemies())if(____pullCollider.bounds.Intersects(e.collider.bounds))h.Mark(e).hookedUntil=h.time+3;
        }
    }
    [HarmonyPatch(typeof(Characters.Abilities.Customs.GhoulPassive),"OnOwnerKilled")]
    static class GhoulMeatHook
    {
        static void Prefix(object __instance,ITarget target,ref int ____possibility,out int __state){__state=____possibility;var h=EffectHost.Player;if(h!=null&&h.Has(Gift.Destiny)&&target.character!=null&&target.character.GetComponent<EnemyMark>()!=null&&target.character.GetComponent<EnemyMark>().hookedUntil>h.time)____possibility=100;}
        static void Postfix(ref int ____possibility,int __state){____possibility=__state;}
    }
    [HarmonyPatch(typeof(Characters.Abilities.Weapons.Ghoul.GhoulPassive2),"OnOwnerGaveDamage")]
    static class GhoulMeat2Hook
    {
        static bool Prepare(){return AccessTools.Method(typeof(Characters.Abilities.Weapons.Ghoul.GhoulPassive2),"OnOwnerGaveDamage")!=null;}
        static void Prefix(ITarget target,ref int ____dropPossibility,out int __state){__state=____dropPossibility;var h=EffectHost.Player;if(h!=null&&h.Has(Gift.Destiny)&&target.character!=null&&target.character.GetComponent<EnemyMark>()!=null&&target.character.GetComponent<EnemyMark>().hookedUntil>h.time)____dropPossibility=100;}
        static void Postfix(ref int ____dropPossibility,int __state){____dropPossibility=__state;}
    }
    // A swap scope lets native expiry/removal keep working outside the swap event.
    [HarmonyPatch(typeof(Characters.Player.WeaponInventory),"NextWeapon")]
    static class DestinySwapScope
    {
        internal static bool active;
        static void Prefix(){active=true;}
        static Exception Finalizer(Exception __exception){active=false;return __exception;}
    }
    [HarmonyPatch(typeof(CharacterAbilityManager),"Remove",new Type[]{typeof(IAbility)})]
    static class PreserveDestinyBuffHook
    {
        static bool Prefix(CharacterAbilityManager __instance,IAbility ability,ref bool __result){
            var h=EffectHost.Player;if(h==null||!h.Has(Gift.Destiny)||!DestinySwapScope.active||__instance!=h.owner.ability)return true;
            bool preserve=ability.GetType().Name=="DarkPaladinPassive";
            foreach(var prisoner in SkullRules.Native(h,"PrisonerPassive"))foreach(string field in new[]{"_brutalityStat","_tacticsStat","_survivalStat"}){
                var f=AccessTools.Field(prisoner.GetType(),field);if(f==null)continue;var component=f.GetValue(prisoner);if(component==null)continue;
                var prop=AccessTools.Property(component.GetType(),"ability");if(prop!=null&&ReferenceEquals(prop.GetValue(component,null),ability))preserve=true;
            }
            if(!preserve)return true;__result=false;return false;
        }
    }
}

