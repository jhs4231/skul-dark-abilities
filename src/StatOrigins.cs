using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Characters;
using Characters.Abilities;
using HarmonyLib;

namespace FanDarkAbilities
{
    // Scale the values only while the native stat accumulator reads them. Source objects,
    // live stack counters, and detach keys retain their original values and identity.
    [HarmonyPatch(typeof(Stat),"Update")]
    static class StatOriginHook
    {
        internal sealed class Restore { public readonly Dictionary<Stat.Value,double> values=new Dictionary<Stat.Value,double>(); }
        static void Prefix(Stat __instance,out Restore __state){
            __state=null;var h=EffectHost.Player;if(h==null||!h.Has(Gift.Humility)||__instance!=h.owner.stat)return;
            __state=new Restore();var sources=new Dictionary<Stat.Values,double>();var abilities=new Dictionary<IAbility,double>();
            double itemFactor=Math.Max(0,1-h.Scale(Gift.Humility,.3));double inscriptionFactor=1+h.Power(Gift.Humility);
            foreach(var item in h.owner.playerComponents.inventory.item.items.Where(i=>i!=null))foreach(var c in item.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true))Collect(c,itemFactor,sources,abilities,new HashSet<object>(),0);
            foreach(var c in h.owner.playerComponents.inventory.synergy.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true))
                if(c!=null&&c.GetType().Namespace!=null&&c.GetType().Namespace.StartsWith("Characters.Gear.Synergy.Inscriptions"))Collect(c,inscriptionFactor,sources,abilities,new HashSet<object>(),0);
            for(int i=0;i<h.owner.ability.Count;i++){
                var instance=h.owner.ability[i];double factor;if(abilities.TryGetValue(instance.ability,out factor))Collect(instance,factor,sources,abilities,new HashSet<object>(),0);
            }
            foreach(var pair in sources)foreach(var value in pair.Key.values){
                if(__state.values.ContainsKey(value))continue;__state.values[value]=value.value;
                double baseline=value.categoryIndex==Stat.Category.Percent.index?1:0;
                value.value=baseline+(value.value-baseline)*pair.Value;
            }
        }
        static void Collect(object obj,double factor,Dictionary<Stat.Values,double> sources,Dictionary<IAbility,double> abilities,HashSet<object> visited,int depth){
            if(obj==null||depth>5||!visited.Add(obj))return;
            var values=obj as Stat.Values;if(values!=null){sources[values]=factor;return;}
            var ability=obj as IAbility;if(ability!=null)abilities[ability]=factor;
            var enumerable=obj as IEnumerable;if(enumerable!=null&&!(obj is string)){foreach(var e in enumerable)Collect(e,factor,sources,abilities,visited,depth+1);return;}
            var t=obj.GetType();if(t.Assembly!=typeof(Character).Assembly)return;
            foreach(var field in AccessTools.GetDeclaredFields(t).Concat(t.BaseType==null?new List<FieldInfo>():AccessTools.GetDeclaredFields(t.BaseType))){
                if(field.IsStatic)continue;var v=field.GetValue(obj);if(v==null||v is Character)continue;
                if(v is Stat.Values||v is IAbility||v is IAbilityInstance||v is Array||field.Name=="_statBonus"||field.Name=="_ability")Collect(v,factor,sources,abilities,visited,depth+1);
            }
        }
        static Exception Finalizer(Exception __exception,Restore __state){if(__state!=null)foreach(var p in __state.values)p.Key.value=p.Value;return __exception;}
    }
    [HarmonyPatch(typeof(Stat),"GetDamage")]
    static class InscriptionDamageOriginHook
    {
        static void Postfix(Stat __instance,ref Damage __result){
            var h=EffectHost.Player;if(h==null||!h.Has(Gift.Humility)||__instance!=h.owner.stat)return;
            foreach(var frame in new StackTrace().GetFrames()){
                var t=frame.GetMethod().DeclaringType;if(t==null||t.Namespace==null)continue;
                if(!t.Namespace.StartsWith("Characters.Gear.Synergy.Inscriptions"))continue;
                double factor=1+h.Power(Gift.Humility);
                if(__result.motionType==Damage.MotionType.Item)factor/=Math.Max(.001,1-h.Scale(Gift.Humility,.3));
                __result.multiplier*=factor;break;
            }
        }
    }
}
