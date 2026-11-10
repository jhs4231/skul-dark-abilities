using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Characters;
using Characters.Player;
using Data;
using HarmonyLib;
using UnityEngine;

namespace FanDarkAbilities
{
    [HarmonyPatch(typeof(Character),"TryAttackCharacter")]
    static class CombatHooks
    {
        internal struct Snapshot {public double hp,shield; public bool alive,owned;}
        static void Prefix(Character __instance,ITarget target,ref Damage damage,out Snapshot __state){
            __state=new Snapshot();var h=EffectHost.Player;if(h==null||target.character==null)return;
            var c=target.character;__state.hp=c.health.currentHealth;__state.shield=c.health.shield.amount;__state.alive=!c.health.dead;
            __state.owned=__instance==h.owner||__instance.type==Character.Type.PlayerMinion;
            if(__state.owned)h.Outgoing(c,ref damage);
        }
        static void Postfix(Character __instance,ITarget target,ref Damage damage,bool __result,Snapshot __state){
            var h=EffectHost.Player;if(h==null||!__state.owned||!__result||target.character==null)return;
            var c=target.character;double dealt=Math.Max(0,__state.hp-c.health.currentHealth+__state.shield-c.health.shield.amount);
            if(__instance==h.owner)h.Gave(c,damage,dealt);
            if(__state.alive&&c.health.dead)h.Killed(c,damage.key);
        }
    }
    [HarmonyPatch(typeof(Health),"TakeDamage",new Type[]{typeof(Damage),typeof(double)},new ArgumentType[]{ArgumentType.Ref,ArgumentType.Out})]
    static class IncomingHooks
    {
        static void Prefix(Health __instance,ref Damage damage,out double[] __state){
            __state=new[]{__instance.currentHealth+__instance.shield.amount,damage.amount};
            var h=EffectHost.Player;if(h==null||__instance!=h.owner.health)return;
            // Do not spend armor charges on attacks already blocked by native immunity.
            if(h.wyvernUntil>h.time||(!h.owner.invulnerable.value&&!h.owner.evasion.value&&!h.owner.cinematic.value))h.Incoming(ref damage);
        }
        static void Postfix(Health __instance,ref Damage damage,double[] __state,bool __result){
            var h=EffectHost.Player;if(h==null||__instance!=h.owner.health)return;
            double lost=Math.Max(0,__state[0]-__instance.currentHealth-__instance.shield.amount);
            if(lost>0)h.Took(lost);
            else if(__state[1]>0&&__result&&!damage.@null&&!h.owner.invulnerable.value&&!h.owner.evasion.value&&!h.owner.cinematic.value)h.Blocked();
        }
    }
    [HarmonyPatch(typeof(Health),"TakeHealth")]
    static class LethalHook
    {
        static void Prefix(Health __instance,ref double amount,out bool __state){
            __state=false;var h=EffectHost.Player;if(h==null||__instance!=h.owner.health)return;
            bool lethal=amount>=__instance.currentHealth&&__instance.currentHealth>0;
            bool prevented=h.Lethal(ref amount);__state=lethal&&!prevented;
        }
        static void Postfix(Health __instance,bool __state){var h=EffectHost.Player;if(__state&&h!=null&&__instance==h.owner.health&&!__instance.dead&&__instance.currentHealth>0)h.Revived();}
    }
    [HarmonyPatch(typeof(Health),"Heal",new Type[]{typeof(double),typeof(bool)},new ArgumentType[]{ArgumentType.Ref,ArgumentType.Normal})]
    static class VampireHook
    {
        static void Prefix(Health __instance,ref double amount){
            var h=EffectHost.Player;if(h!=null&&__instance==h.owner.health&&h.Has(Gift.Vampire)&&amount>0){
                double effective=amount*h.owner.stat.GetFinal(Stat.Kind.TakingHealAmount);
                if(effective<=__instance.maximumHealth*.05)amount+=5*h.Power(Gift.Vampire)/Math.Max(.0001,h.owner.stat.GetFinal(Stat.Kind.TakingHealAmount));
            }
        }
    }
    [HarmonyPatch(typeof(CharacterStatus),"Apply")]
    static class StatusHook
    {
        static bool Prefix(CharacterStatus __instance,Character attacker,CharacterStatus.ApplyInfo applyInfo,ref bool __result){
            var h=EffectHost.Player;if(h!=null&&__instance==h.owner.status&&h.Has(Gift.Panacea)&&applyInfo.kind==CharacterStatus.Kind.Stun){__result=false;return false;}return true;
        }
        static void Postfix(CharacterStatus __instance,Character attacker,CharacterStatus.ApplyInfo applyInfo,bool __result){
            var h=EffectHost.Player;if(h==null||attacker!=h.owner||!__result)return;
            var c=__instance.GetComponent<Character>();if(c!=null)h.Applied(c,applyInfo);
        }
    }
    [HarmonyPatch(typeof(Characters.Abilities.Statuses.Wound),"GiveDamage")]
    static class BleedingHook
    {
        static void Postfix(Characters.Abilities.Statuses.Wound __instance){var h=EffectHost.Player;if(h!=null&&__instance.attacker==h.owner)h.Bleed(__instance.owner);}
    }
    [HarmonyPatch(typeof(Characters.Abilities.Statuses.Freeze),"OnTookDamage")]
    static class FreezeHook
    {
        static bool Prefix(Characters.Abilities.Statuses.Freeze __instance){var h=EffectHost.Player;return h==null||!h.Has(Gift.Frostbite)||__instance.attacker!=h.owner;}
    }
    [HarmonyPatch(typeof(Characters.Abilities.Statuses.Burn),"UpdateTime")]
    static class FeverHook
    {
        static void Prefix(Characters.Abilities.Statuses.Burn __instance,float deltaTime){
            var h=EffectHost.Player;if(h==null||!h.Has(Gift.Fever)||__instance.attacker!=h.owner)return;
            var f=AccessTools.Field(__instance.GetType(),"_remainTimeToNextTick");
            // Advance the tick timer alone; the burn duration and effect animation keep normal time.
            f.SetValue(__instance,(float)f.GetValue(__instance)-deltaTime*9*(float)h.Power(Gift.Fever));
        }
    }
    [HarmonyPatch(typeof(GameData.Currency),"Earn",new Type[]{typeof(double)})]
    static class CurrencyHook
    {
        internal static bool suppressGold;
        static void Prefix(GameData.Currency __instance,ref double amount){
            var h=EffectHost.Player;if(h==null)return;
            if(suppressGold&&__instance==GameData.Currency.gold){amount=0;return;}
            if(h.Has(Gift.Fate)&&__instance==GameData.Currency.gold)amount*=1+FateDesign.For(h)["gold"]*h.Power(Gift.Fate);
            if(!h.Has(Gift.Secular))return;
            if(__instance==GameData.Currency.gold||__instance==GameData.Currency.darkQuartz)amount*=1+h.Power(Gift.Secular);
            if(__instance==GameData.Currency.bone)amount*=Math.Max(0,1-.5*h.Power(Gift.Secular));
        }
    }
    [HarmonyPatch(typeof(ItemInventory),"Discard",new Type[]{typeof(int)})]
    static class VoidDiscardHook
    {
        static void Prefix(ItemInventory __instance,int index,out string __state){
            __state=null;var h=EffectHost.Player;if(h==null||!h.Has(Gift.Void)||__instance!=h.owner.playerComponents.inventory.item)return;
            if(index>=0&&index<__instance.items.Count&&__instance.items[index]!=null){__state=__instance.items[index].name;CurrencyHook.suppressGold=true;}
        }
        static Exception Finalizer(Exception __exception){CurrencyHook.suppressGold=false;return __exception;}
        static void Postfix(bool __result,string __state){
            CurrencyHook.suppressGold=false;var h=EffectHost.Player;if(h!=null&&__result&&__state!=null){h.destroyed++;WorldRules.Banned.Add(__state.Replace("(Clone)","").Trim());h.Refresh();}
        }
    }
    [HarmonyPatch(typeof(WeaponInventory),"NextWeapon")]
    static class GuillotineHook
    {
        static void Prefix(WeaponInventory __instance){var h=EffectHost.Player;if(h!=null&&h.Has(Gift.Guillotine)&&__instance==h.owner.playerComponents.inventory.weapon)__instance.ResetSwapCooldown();}
        static void Postfix(WeaponInventory __instance){var h=EffectHost.Player;if(h!=null&&h.Has(Gift.Guillotine)&&__instance==h.owner.playerComponents.inventory.weapon)__instance.ResetSwapCooldown();}
    }
    [HarmonyPatch(typeof(WeaponInventory),"UpgradeCurrentWeapon")]
    static class AwakeningHook
    {
        static void Postfix(){var h=EffectHost.Player;if(h!=null&&h.Has(Gift.Spider)){h.awakenings++;h.Refresh();}}
    }
    // Run-scoped state is stored in the same game save, using the game's own data provider.
    internal static class RunState
    {
        static readonly StringData state=new StringData("FanDarkAbilities/run","" );
        static readonly IntData seed=new IntData("FanDarkAbilities/seed",int.MinValue);
        internal static void Load(EffectHost h){
            if(seed.value!=GameData.Save.instance.randomSeed)return;
            string[] s=(state.value??"").Split('|');
            if(s.Length<8)return;
            int n;float f;
            if(int.TryParse(s[0],out n))h.revivals=n;
            if(int.TryParse(s[1],out n))h.awakenings=n;
            if(int.TryParse(s[2],out n))h.sacrifices=n;
            if(int.TryParse(s[3],out n))h.destroyed=n;
            if(int.TryParse(s[4],out n))h.darkKills=n;
            if(float.TryParse(s[5],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out f))h.fissionTime=f;
            foreach(var item in s[6].Split(','))if(item.Length>0)WorldRules.Banned.Add(item);
            SkullRules.Load(h,s[7]);
            if(s.Length>8)WorldRules.LoadTransient(h,s[8]);
        }
        internal static void Save(){
            var h=EffectHost.Player;if(h==null)return;
            seed.value=GameData.Save.instance.randomSeed;
            state.value=string.Join("|",new[]{h.revivals.ToString(),h.awakenings.ToString(),h.sacrifices.ToString(),h.destroyed.ToString(),h.darkKills.ToString(),h.fissionTime.ToString(System.Globalization.CultureInfo.InvariantCulture),string.Join(",",WorldRules.Banned.ToArray()),SkullRules.Save(h),WorldRules.ExportTransient(h)});
            seed.Save();state.Save();
        }
        internal static void Reset(){seed.value=int.MinValue;state.value="";seed.Save();state.Save();WorldRules.Banned.Clear();}
    }
    [HarmonyPatch(typeof(Level.LevelManager),"Save")]
    static class SaveHook { static void Postfix(){RunState.Save();} }
    [HarmonyPatch(typeof(GameData.Save),"ResetAll")]
    static class ResetHook { static void Postfix(){RunState.Reset();} }
}
