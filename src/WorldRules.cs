using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Characters;
using Characters.Player;
using Characters.Gear.Weapons;
using Characters.Gear.Upgrades;
using Characters.Gear;
using Characters.Gear.Items;
using GameResources;
using Data;
using HarmonyLib;
using UnityEngine;

namespace FanDarkAbilities
{
    internal static class WorldRules
    {
        internal static readonly HashSet<string> Banned=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        static readonly Dictionary<Character,EffectHost> slowed=new Dictionary<Character,EffectHost>();
        static readonly Dictionary<EffectHost,WorldState> states=new Dictionary<EffectHost,WorldState>();
        internal sealed class WorldState {
            public bool darkProvided,duplicate;public int cloneIndex=-1;public Weapon backup,clone;
            public bool loadingBackup;public string pendingBackup,pendingSkills;
            public float nextScan;
            public readonly Dictionary<Weapon,SkillInfo[]> skills=new Dictionary<Weapon,SkillInfo[]>();
            public readonly FieldEdits edits=new FieldEdits();
            public readonly Dictionary<Rarity,int> bonePrices=new Dictionary<Rarity,int>();
        }
        static WorldState State(EffectHost h){WorldState s;if(!states.TryGetValue(h,out s)){s=new WorldState();states[h]=s;}return s;}
        public static bool QuintessenceCooling(QuintessenceInventory q){return q.items.Any(i=>i!=null&&i.cooldown!=null&&!i.cooldown.canUse);}
        public static void NewRoom(EffectHost h){State(h).darkProvided=false;}
        public static void Tick(EffectHost h,float dt){
            var s=State(h);if(h.time<s.nextScan)return;s.nextScan=h.time+.5f;
            var enemies=h.Enemies();
            foreach(var e in enemies){
                if(h.Has(Gift.TimeExchange)){e.chronometer.master.AttachTimeScale(h,Mathf.Max(.05f,1-.4f*(float)h.Power(Gift.TimeExchange)));slowed[e]=h;}
                else if(slowed.ContainsKey(e)){e.chronometer.master.DetachTimeScale(h);slowed.Remove(e);}
                var mark=e.GetComponent<EnemyMark>();if(mark!=null&&(e.status==null||!e.status.poisoned))mark.poisonStacks=0;
                if(e.GetComponent<Characters.Abilities.Darks.DarkEnemy>()!=null&&e.GetComponent<Characters.Abilities.Darks.DarkEnemy>().enabled)h.Mark(e).dark=true;
            }
            if(h.Has(Gift.BlackHeart)&&!s.darkProvided&&enemies.Length>0){
                var existing=enemies.FirstOrDefault(e=>{var d=e.GetComponent<Characters.Abilities.Darks.DarkEnemy>();return d!=null&&d.enabled;});
                if(existing!=null){h.Mark(existing).dark=true;s.darkProvided=true;}
                else {
                    var target=enemies.FirstOrDefault(e=>e.type!=Character.Type.Boss&&e.GetComponent<Characters.Abilities.Darks.DarkEnemy>()!=null);
                    if(target!=null){
                        var constructors=(Characters.Abilities.Darks.DarkAbilityConstructor[])AccessTools.Field(typeof(Level.DarkEnemySelector),"_constructors").GetValue(Level.DarkEnemySelector.instance);
                        if(constructors!=null&&constructors.Length>0&&constructors[0].Provide(target)){
                            var dark=target.GetComponent<Characters.Abilities.Darks.DarkEnemy>();dark.RunIntro();h.Mark(target).dark=true;s.darkProvided=true;
                        }
                    }
                }
            }
            ApplyStructural(h,s);TuneEquipment(h,s);
        }
        static void ApplyStructural(EffectHost h,WorldState s){
            var inv=h.owner.playerComponents.inventory.weapon;
            if(s.pendingBackup!=null&&!s.loadingBackup){h.StartCoroutine(LoadBackup(h,s));return;}
            if(s.loadingBackup)return;
            if(s.pendingSkills!=null){
                var saved=s.pendingSkills.Split(';');s.pendingSkills=null;
                for(int i=0;i<saved.Length;i++){
                    var keys=saved[i].Split('~');var w=i==0?inv.current:inv.next;
                    if(w==null||keys.Length<2)continue;
                    var originals=keys.Select(key=>w.skills.FirstOrDefault(skill=>skill.key==key)).ToArray();
                    if(originals.All(skill=>skill!=null))s.skills[w]=originals;
                }
            }
            if(h.Has(Gift.Doppelganger)&&!s.duplicate&&inv.current!=null){
                int index=1-inv.currentIndex;s.cloneIndex=index;s.backup=inv.weapons[index];
                if(s.backup!=null){inv.weapons[index]=null;s.backup.gameObject.SetActive(false);}
                var reference=Singletons.Singleton<Services.Service>.Instance.gearManager.GetWeaponByName(inv.current.name);
                if(reference!=null){
                    // Instantiate through the game's resource loader, not by cloning live passive objects.
                    h.StartCoroutine(Duplicate(h,s,reference,index));s.duplicate=true;
                }
            }
            if(!h.Has(Gift.Doppelganger)&&s.duplicate)RestoreDuplicate(h,s);
            foreach(var w in inv.weapons.Where(w=>w!=null)){
                if(h.Has(Gift.Genetic)&&w.currentSkills!=null&&w.currentSkills.Count>=2){
                    if(!s.skills.ContainsKey(w))s.skills[w]=w.currentSkills.ToArray();
                    var original=s.skills[w];
                    if(w.currentSkills[1].name!="FanGeneticSkill"){
                        // A real cloned SkillInfo/action gives the right slot its own button/cooldown.
                        var clone=UnityEngine.Object.Instantiate(original[0].gameObject,w.transform).GetComponent<SkillInfo>();
                        clone.name="FanGeneticSkill";clone.Initialize();clone.action.Initialize(h.owner);
                        w.currentSkills[1]=clone;w.SetSkillButtons();h.owner.InitializeActions();
                    }
                }
            }
            if(!h.Has(Gift.Genetic)){
                foreach(var p in s.skills.ToArray())if(p.Key!=null){
                    for(int i=0;i<p.Value.Length&&i<p.Key.currentSkills.Count;i++){
                        var c=p.Key.currentSkills[i];if(c!=p.Value[i]&&c!=null&&c.name=="FanGeneticSkill")UnityEngine.Object.Destroy(c.gameObject);
                        p.Key.currentSkills[i]=p.Value[i];
                    }p.Key.SetSkillButtons();
                }s.skills.Clear();
            }
            var upgrades=h.owner.playerComponents.inventory.upgrade;
            if(h.Has(Gift.WyvernTail)&&h.Has(Gift.WyvernHead)&&!h.Has(Gift.Wyvern)){
                int tail=upgrades.upgrades.FindIndex(u=>u!=null&&u.reference.name==Catalog.All[(int)Gift.WyvernTail].id);
                int head=upgrades.upgrades.FindIndex(u=>u!=null&&u.reference.name==Catalog.All[(int)Gift.WyvernHead].id);
                if(tail>=0&&head>=0){
                    upgrades.Remove(head);upgrades.EquipAt(UpgradeResource.instance.upgradeReferences.First(r=>r.name==Catalog.All[(int)Gift.Wyvern].id),tail);
                    upgrades.EquipAt(UpgradeResource.instance.upgradeReferences.First(r=>r.name=="FanWyvernReserve"),head);
                }
            }
            foreach(Rarity rarity in Enum.GetValues(typeof(Rarity))){
                if(!s.bonePrices.ContainsKey(rarity))s.bonePrices[rarity]=Settings.instance.bonesToUpgrade[rarity];
                // Price adjustment is handled by the RarityPrices getter hook below.
            }
        }
        static IEnumerator Duplicate(EffectHost h,WorldState s,WeaponReference reference,int index){
            var request=reference.LoadAsync();while(!request.isDone)yield return null;
            if(h==null){request.Release();yield break;}
            var weapon=request.Instantiate(h.owner.transform.position,Quaternion.identity);request.Release();
            var inv=h.owner.playerComponents.inventory.weapon;int current=inv.currentIndex;
            inv.ForceEquipAt(weapon,index);s.clone=weapon;
            if(current!=index)inv.NextWeapon(true);
        }
        static IEnumerator LoadBackup(EffectHost h,WorldState s){
            s.loadingBackup=true;var data=s.pendingBackup.Split('~');s.pendingBackup=null;
            var inv=h.owner.playerComponents.inventory.weapon;
            if(data.Length<3){s.loadingBackup=false;yield break;}
            s.cloneIndex=data[0]=="1"?inv.currentIndex:1-inv.currentIndex;s.clone=inv.weapons[s.cloneIndex];s.duplicate=true;
            var reference=Singletons.Singleton<Services.Service>.Instance.gearManager.GetWeaponByName(data[1]);
            if(reference!=null){
                var request=reference.LoadAsync();while(!request.isDone)yield return null;
                if(h==null){request.Release();yield break;}
                s.backup=request.Instantiate(h.owner.transform.position,Quaternion.identity);request.Release();
                s.backup.SetOwner(h.owner);s.backup.transform.SetParent(h.owner.@base,false);
                if(data.Length>3)s.backup.SetSkills(data[3].Split(','),false);
                float stack;var stackable=s.backup.GetComponentInChildren<IStackable>(true);
                if(stackable!=null&&float.TryParse(data[2],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out stack))stackable.stack=stack;
                s.backup.gameObject.SetActive(false);
            }
            s.loadingBackup=false;if(!h.Has(Gift.Doppelganger))RestoreDuplicate(h,s);
        }
        internal static string ExportTransient(EffectHost h){
            var s=State(h);var inv=h.owner.playerComponents.inventory.weapon;string backup="";
            bool permanent=h.owner.playerComponents.inventory.upgrade.upgrades.Any(u=>u!=null&&u.reference.name=="FanDoppelganger");
            if(s.duplicate&&s.backup!=null&&!permanent){
                var stackable=s.backup.GetComponentInChildren<IStackable>(true);
                backup=string.Join("~",new[]{inv.currentIndex==s.cloneIndex?"1":"0",s.backup.name,(stackable==null?0:stackable.stack).ToString(System.Globalization.CultureInfo.InvariantCulture),string.Join(",",s.backup.currentSkills.Select(skill=>skill.key).ToArray())});
            }
            string[] skills=new string[2];for(int i=0;i<2;i++){
                var w=i==0?inv.current:inv.next;SkillInfo[] original;
                skills[i]=w!=null&&s.skills.TryGetValue(w,out original)?string.Join("~",original.Select(skill=>skill.key).ToArray()):"";
            }
            return backup+"#"+string.Join(";",skills);
        }
        internal static void LoadTransient(EffectHost h,string data){
            if(string.IsNullOrEmpty(data))return;var parts=data.Split('#');var s=State(h);
            if(parts[0].Length>0)s.pendingBackup=parts[0];if(parts.Length>1&&parts[1].Length>1)s.pendingSkills=parts[1];
        }
        static void RestoreDuplicate(EffectHost h,WorldState s){
            var inv=h.owner.playerComponents.inventory.weapon;
            if(s.cloneIndex>=0&&inv.weapons[s.cloneIndex]==s.clone){
                if(s.backup!=null)inv.ForceEquipAt(s.backup,s.cloneIndex);
                else {if(inv.current==s.clone)inv.NextWeapon(true);inv.weapons[s.cloneIndex]=null;if(s.clone!=null)UnityEngine.Object.Destroy(s.clone.gameObject);}
            }else if(s.backup!=null)UnityEngine.Object.Destroy(s.backup.gameObject);
            s.backup=null;s.clone=null;s.duplicate=false;
        }
        static void TuneEquipment(EffectHost h,WorldState s){
            // Reapply from recorded original values, never multiply an already modified value.
            s.edits.Restore();
            double duration=1+h.Scale(Gift.Enchant,.25*h.Level(Gift.Enchant));
            double cooldown=Math.Max(.05,1-h.Scale(Gift.Enchant,.25*h.Level(Gift.Enchant)));
            double itemPower=1; // StatOriginHook scales live bonuses; combat hook scales item damage.
            foreach(var item in h.owner.playerComponents.inventory.item.items.Where(i=>i!=null)){
                foreach(var component in item.GetComponentsInChildren<MonoBehaviour>(true))
                    s.edits.Tune(component,cooldown,duration*itemPower,itemPower,false);
            }
            h.owner.stat.SetNeedUpdate();
        }
        public static void Cleanup(EffectHost h){
            foreach(var e in slowed.Where(p=>p.Value==h).Select(p=>p.Key).ToArray()){if(e!=null)e.chronometer.master.DetachTimeScale(h);slowed.Remove(e);}
            WorldState s;if(states.TryGetValue(h,out s)){
                s.edits.Restore();
                if(s.backup!=null)UnityEngine.Object.Destroy(s.backup.gameObject);states.Remove(h);
            }
        }
    }
    internal sealed class FieldEdits
    {
        sealed class Entry{public object obj,original;public FieldInfo field;}
        readonly List<Entry> changes=new List<Entry>();readonly HashSet<object> visited=new HashSet<object>();
        public void Restore(){foreach(var e in changes){var u=e.obj as UnityEngine.Object;if(!ReferenceEquals(u,null)&&u==null)continue;e.field.SetValue(e.obj,e.original);}changes.Clear();visited.Clear();}
        public void Set(object obj,FieldInfo field,object value){if(field.IsInitOnly||field.IsStatic)return;changes.Add(new Entry{obj=obj,field=field,original=field.GetValue(obj)});field.SetValue(obj,value);}
        public void Tune(object obj,double cooldown,double duration,double power,bool inscription){Walk(obj,cooldown,duration,power,0);}
        void Walk(object obj,double cooldown,double duration,double power,int depth){
            if(obj==null||depth>5||!visited.Add(obj))return;
            var values=obj as Stat.Values;if(values!=null){foreach(var item in values.values)Walk(item,cooldown,duration,power,depth+1);return;}
            var value=obj as Stat.Value;
            if(value!=null){
                double baseline=value.categoryIndex==Stat.Category.Percent.index?1:0;
                Set(value,AccessTools.Field(typeof(Stat.Value),"value"),baseline+(value.value-baseline)*power);return;
            }
            var enumerable=obj as IEnumerable;if(enumerable!=null&&!(obj is string)){foreach(var item in enumerable)Walk(item,cooldown,duration,power,depth+1);return;}
            var type=obj.GetType();if(type.Assembly!=typeof(Character).Assembly||type.IsEnum)return;
            foreach(var field in AccessTools.GetDeclaredFields(type).Concat(type.BaseType==null?new List<FieldInfo>():AccessTools.GetDeclaredFields(type.BaseType))){
                if(field.IsStatic||field.IsInitOnly)continue;
                bool serialized=field.IsPublic||Attribute.IsDefined(field,typeof(SerializeField));if(!serialized)continue;
                string name=field.Name.ToLowerInvariant();object current=field.GetValue(obj);
                if(current is float||current is double){
                    double n=Convert.ToDouble(current),factor=1;
                    if(name.Contains("cooldown")||name.Contains("interval"))factor=cooldown;
                    else if(name.Contains("duration"))factor=duration;
                    else if(name.Contains("damage")&&(name.Contains("multiplier")||name.Contains("amount")||name.Contains("ratio")))factor=power;
                    if(factor!=1&&n>0)Set(obj,field,field.FieldType==typeof(float)?(object)(float)(n*factor):(object)(n*factor));
                }else if(current is Stat.Values||current is Stat.Value||current is Array||current is Characters.Abilities.IAbility||name=="_ability"||name=="_trigger")Walk(current,cooldown,duration,power,depth+1);
            }
        }
    }
    [HarmonyPatch(typeof(Level.HardmodeChest),"TryToChangeOmenChest")]
    static class DeadChestHook
    {
        static void Postfix(Level.HardmodeChest __instance,ref bool __result){
            var h=EffectHost.Player;if(h==null||!h.Has(Gift.DeadChest))return;
            Plugin.Set(__instance,"_isOmenChest",true);__result=true;
        }
    }
    [HarmonyPatch(typeof(Services.GearManager),"GetOmenItems")]
    static class DuplicateOmenHook
    {
        static bool Prefix(System.Random random,ref ItemReference __result){
            var h=EffectHost.Player;if(h==null||!h.Has(Gift.DeadChest))return true;
            var pool=GearResource.instance.items.Where(i=>i.available&&(i.gearTag&Gear.Tag.Omen)!=0&&(i.gearTag&Gear.Tag.UpgradedOmen)==0&&!WorldRules.Banned.Contains(i.name)).ToArray();
            if(pool.Length==0)return true;__result=pool[random.Next(pool.Length)];return false;
        }
    }
    [HarmonyPatch(typeof(GearReference),"get_available")]
    static class BannedItemHook {static void Postfix(GearReference __instance,ref bool __result){var h=EffectHost.Player;if(h!=null&&h.Has(Gift.Void)&&__instance is ItemReference&&WorldRules.Banned.Contains(__instance.name))__result=false;}}
    [HarmonyPatch(typeof(Level.Npc.RescuableNpc),"CRescueConversation")]
    static class SacrificeHook
    {
        static bool Prefix(Level.Npc.RescuableNpc __instance,ref IEnumerator __result){
            var h=EffectHost.Player;if(h==null||!h.Has(Gift.RedCrown))return true;
            h.sacrifices=Math.Min(6,h.sacrifices+1);h.Refresh();FanEffect.Flash(h,__instance.transform.position,Color.red,1);UnityEngine.Object.Destroy(__instance.gameObject);__result=Empty();return false;
        }
        static IEnumerator Empty(){yield break;}
    }
}
