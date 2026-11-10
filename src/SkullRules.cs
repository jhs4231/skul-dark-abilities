using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Characters;
using Characters.Abilities;
using Characters.Gear.Weapons;
using HarmonyLib;
using UnityEngine;
using Act=Characters.Actions.Action;

namespace FanDarkAbilities
{
    internal static class SkullRules
    {
        internal sealed class SkullState {
            public float skillUntil,air,invincibleUntil,blockUntil,nextTick,nextNative;
            public int blocks,basics,dashes,grabs,mummyLives;
            public float comboAt;
            public string family="Unknown";
            public readonly Dictionary<CircleCollider2D,float> radii=new Dictionary<CircleCollider2D,float>();
            public readonly FieldEdits edits=new FieldEdits();
        }
        static readonly Dictionary<EffectHost,SkullState> states=new Dictionary<EffectHost,SkullState>();
        internal static SkullState State(EffectHost h){SkullState s;if(!states.TryGetValue(h,out s)){s=new SkullState();states[h]=s;}return s;}
        static readonly string[][] families={
            new[]{"Sword","skeleton_sword"},new[]{"Ent","ent"},new[]{"Monkey","stonemonkey","stone_monkey"},
            new[]{"Mage","wizard","mage"},new[]{"Slave","slave","spartacus","spartan"},new[]{"Mummy","mummy"},
            new[]{"Gargoyle","gargoyle"},new[]{"Frost","frost","ice","glacialskull"},new[]{"Minotaur","minotaur"},
            new[]{"Hunter","hunter","skeleton_bow"},new[]{"Genie","genie"},new[]{"Ghoul","ghoul"},
            new[]{"Rockstar","rockstar"},new[]{"Ninja","ninja"},new[]{"DarkPaladin","darkpaladin","dark_paladin"},
            new[]{"LivingArmor","livingarmor","living_armor"},new[]{"Prisoner","prisoner"},new[]{"Ascetic","ascetic","hercules"},
            new[]{"Reaper","grimreaper","grim_reaper"},new[]{"Champion","fighter","champion"},
            new[]{"Yaksha","yaksha"},new[]{"Archlich","archlich"},new[]{"DavyJones","davyjones","davy_jones"},
            new[]{"Skul","skul","littlebone"},new[]{"Warrior","warrior"},new[]{"Shield","skeleton_shield"},
            new[]{"Spear","skeleton_pike","skeleton_spear"},new[]{"Thief","thief"},new[]{"Werewolf","werewolf"},
            new[]{"Alchemist","alchemist"},new[]{"Recruit","recruit","carleon"},new[]{"Bomber","bomber","skeleton_bomb","bombskul"},
            new[]{"Clown","clown"},new[]{"Water","water","aquaskull"},new[]{"Samurai","samurai"},new[]{"Berserker","berserker"},
            new[]{"Rider","rider","ghostrider"},new[]{"Predator","predator"},new[]{"King","kingarthur","king_arthur"},
            new[]{"Gambler","gambler"},new[]{"Aladdin","aladdin"},new[]{"Viking","viking","thor"},
            new[]{"Warlock","highwarlock"},new[]{"GraveDigger","gravedigger"},new[]{"Dominator","dominator"},new[]{"GuanYu","guanyu"},
            new[]{"ChiefGuard","chiefguard"},new[]{"Dracula","dracula"},new[]{"PlagueDoctor","plaguedoctor"},new[]{"Harpy","harpy"},new[]{"Slime","slime_head"},new[]{"Balrog","balrog_head"},new[]{"HeroSkul","heroskul"},
            new[]{"Unknown"}
        };
        public static string Family(Weapon w){
            if(w==null)return "Unknown";return FamilyName(w.name);
        }
        public static string FamilyName(string name){
            string n=name.ToLowerInvariant();
            foreach(var f in families)for(int i=1;i<f.Length;i++)if(n.StartsWith(f[i],StringComparison.Ordinal))return f[0];return "Unknown";
        }
        public static void Apply(EffectHost h){State(h).family=Family(h.owner.playerComponents.inventory.weapon.polymorphOrCurrent);}
        public static IEnumerable<object> Native(EffectHost h,string type){
            var result=new HashSet<object>();
            foreach(var w in h.owner.playerComponents.inventory.weapon.weapons.Where(w=>w!=null)){
                foreach(var c in w.GetComponentsInChildren<MonoBehaviour>(true)){
                    if(c==null)continue;var prop=AccessTools.Property(c.GetType(),"baseAbility");
                    if(prop==null)continue;var a=prop.GetValue(c,null);if(a!=null&&a.GetType().Name==type)result.Add(a);
                }
            }
            return result;
        }
        static object Get(object o,string name){var f=AccessTools.Field(o.GetType(),name);if(f!=null)return f.GetValue(o);var p=AccessTools.Property(o.GetType(),name);return p==null?null:p.GetValue(o,null);}
        static void MultiplyField(SkullState s,object obj,string field,double multiplier){
            var f=AccessTools.Field(obj.GetType(),field);if(f==null)return;var v=f.GetValue(obj);
            if(v is float)s.edits.Set(obj,f,(float)((float)v*multiplier));else if(v is double)s.edits.Set(obj,f,(double)v*multiplier);
        }
        public static void Stats(EffectHost h,List<Stat.Value> values){
            var s=State(h);string f=s.family;
            if(h.Has(Gift.Destiny)){
                if(f=="Mage")values.Add(new Stat.Value(Stat.Category.PercentPoint,Stat.Kind.BasicAttackSpeed,h.Scale(Gift.Destiny,.5)));
                if(f=="Gargoyle")values.Add(new Stat.Value(Stat.Category.PercentPoint,Stat.Kind.CriticalChance,h.Scale(Gift.Destiny,Math.Min(.5,s.air*.05))));
                if(f=="Berserker")values.Add(new Stat.Value(Stat.Category.PercentPoint,Stat.Kind.PhysicalAttackDamage,h.Scale(Gift.Destiny,(1-h.owner.health.percent))));
                if(f=="Rider")values.Add(new Stat.Value(Stat.Category.PercentPoint,Stat.Kind.MovementSpeed,h.Scale(Gift.Destiny,.3)));
                if(f=="Warrior")values.Add(new Stat.Value(Stat.Category.PercentPoint,Stat.Kind.ChargingSpeed,h.Scale(Gift.Destiny,.35)));
                if(f=="Shield")values.Add(new Stat.Value(Stat.Category.PercentPoint,Stat.Kind.PhysicalAttackDamage,h.Scale(Gift.Destiny,Math.Min(1,h.owner.health.shield.amount/100))));
                if(f=="Warlock")values.Add(new Stat.Value(Stat.Category.PercentPoint,Stat.Kind.ChargingSpeed,h.Scale(Gift.Destiny,.4)));
                if(f=="Harpy"&&!h.owner.movement.isGrounded)values.Add(new Stat.Value(Stat.Category.PercentPoint,Stat.Kind.SkillCooldownSpeed,h.Scale(Gift.Destiny,.4)));
                if(f=="Slime")values.Add(new Stat.Value(Stat.Category.PercentPoint,Stat.Kind.TakingHealAmount,h.Scale(Gift.Destiny,.3)));
                if(f=="Unknown")values.Add(new Stat.Value(Stat.Category.PercentPoint,Stat.Kind.CriticalChance,h.Scale(Gift.Destiny,.1)));
            }
            if(h.Has(Gift.Destiny)&&f!="DarkPaladin"&&h.owner.health.shield.amount>0)foreach(var a in Native(h,"DarkPaladinPassive")){
                if(!(a is IAbility)||!h.owner.ability.Contains((IAbility)a))continue;var native=Get(a,"_stat") as Stat.Values;if(native==null)continue;
                foreach(var v in native.values){double amount=v.value;
                    if(v.categoryIndex==Stat.Category.Percent.index)amount=Math.Abs(amount)<.0001?1:2-1/amount;
                    values.Add(new Stat.Value(v.categoryIndex,v.kindIndex,amount));
                }
            }
            FateDesign.Stats(h,values);
        }
        public static void Tick(EffectHost h,float dt){
            var s=State(h);s.air=h.owner.movement.isGrounded?0:s.air+dt;
            h.owner.invulnerable.Detach(s);
            bool champion=h.Has(Gift.Destiny)&&s.family=="Champion"&&h.owner.runningMotion!=null&&h.owner.runningMotion.action!=null&&h.owner.runningMotion.action.type==Act.Type.Skill;
            if(champion||h.time<s.invincibleUntil)h.owner.invulnerable.Attach(s);
            if(h.Has(Gift.Destiny)&&s.family=="LivingArmor")foreach(var a in Native(h,"LivingArmorPassive")){var gauge=Get(a,"_gauge") as Characters.Gear.Weapons.Gauges.ValueGauge;if(gauge!=null)gauge.Add(5*dt*(float)h.Power(Gift.Destiny));}
            if(h.Has(Gift.Destiny)&&h.time>=s.nextTick){
                s.nextTick=h.time+1;
                if(s.family=="Frost")foreach(var e in h.Enemies().Where(e=>e.status!=null&&e.status.freezed))h.Strike(e,4*h.Power(Gift.Destiny),"FrozenDestiny");
                if(s.family=="DavyJones"&&h.Ready("DavyDestiny",8))h.Burst(h.owner.transform.position,4,28*h.Power(Gift.Destiny),"DavyTentacle",false);
            }
            if(h.time>=s.nextNative){s.nextNative=h.time+.5f;TuneNative(h,s);}
        }
        static void TuneNative(EffectHost h,SkullState s){
            s.edits.Restore();foreach(var pair in s.radii)if(pair.Key!=null)pair.Key.radius=pair.Value;if(!h.Has(Gift.Destiny))return;
            if(s.family=="Mage")foreach(var a in Native(h,"WizardPassive"))MultiplyField(s,a,"_baseManaChargingSpeed",1+4*h.Power(Gift.Destiny));
            if(s.family=="Rockstar")foreach(var a in Native(h,"RockstarPassive"))MultiplyField(s,a,"_summonCooldown",Math.Max(.1,h.owner.stat.GetFinal(Stat.Kind.AttackSpeed)));
            if(s.family=="Ent"){
                var w=h.owner.playerComponents.inventory.weapon.polymorphOrCurrent;
                double factor=1/Math.Max(.1,1+(h.owner.stat.GetSkillCooldownSpeed()-1)*.5*h.Power(Gift.Destiny));
                foreach(var trigger in w.GetComponentsInChildren<Characters.Abilities.Triggers.TriggerComponent>(true))s.edits.Tune(trigger,factor,1,1,false);
            }
            if(s.family=="Yaksha")foreach(var home in UnityEngine.Object.FindObjectsOfType<Characters.Abilities.Weapons.Yaksha.YakshaHome>()){
                foreach(string name in new[]{"_enterBoundary","_exitBoundary"}){var collider=Get(home,name) as CircleCollider2D;if(collider!=null){if(!s.radii.ContainsKey(collider))s.radii[collider]=collider.radius;collider.radius=s.radii[collider]*(1+.25f*(float)h.Power(Gift.Destiny));}}
            }
        }
        public static void Action(EffectHost h,Act a){
            var s=State(h);
            if(h.Has(Gift.Fate)){var f=FateDesign.For(h);if(a.type==Act.Type.Skill&&f["skillCost"]>0)h.owner.health.TakeHealth(f["skillCost"]*h.Power(Gift.Fate));if(a.type==Act.Type.Dash&&f["dashCost"]>0)h.owner.health.TakeHealth(f["dashCost"]*h.Power(Gift.Fate));}
            if(!h.Has(Gift.Destiny))return;
            if(a.type==Act.Type.Skill)s.skillUntil=h.time+2;
            if(a.type==Act.Type.BasicAttack){
                s.basics++;
                if(s.family=="Genie"&&s.basics%3==0)s.blockUntil=h.time+.75f;

            }
            if(a.type==Act.Type.Dash){
                if(h.time-s.comboAt>1)s.dashes=0;s.comboAt=h.time;s.dashes++;
                if(s.family=="Ninja"&&s.dashes%3==0)for(int i=0;i<12;i++){
                    float angle=i*Mathf.PI/6;FanEffect.Projectile(h,h.owner.collider.bounds.center,new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)),9,8*h.Power(Gift.Destiny),"Shuriken",Color.gray);
                }
                if(s.family=="Werewolf")h.owner.stat.AttachTimedValues(new Stat.Values(new Stat.Value(Stat.Category.PercentPoint,Stat.Kind.BasicAttackSpeed,.3*h.Power(Gift.Destiny))),2);
            }
            if(a.type==Act.Type.Skill){
                if(s.family=="Skul")foreach(var dash in h.owner.actions.Where(d=>d.type==Act.Type.Dash))if(dash.cooldown!=null)dash.cooldown.stacks=dash.cooldown.maxStack;
                if(s.family=="Water")FanEffect.Zone(h,h.owner.transform.position,2,3,4*h.Power(Gift.Destiny),"WaterDestiny",Color.blue);
                if(s.family=="Recruit")FanEffect.Orb(h,h.owner.transform.position,4,10*h.Power(Gift.Destiny),"RecruitDestiny",Color.yellow);
                if(s.family=="King"&&UnityEngine.Random.value<.25*h.Power(Gift.Destiny))a.cooldown.stacks=Math.Min(a.cooldown.maxStack,a.cooldown.stacks+1);

            }
        }
        public static void Damage(EffectHost h,Character target,ref Damage d,ref double m){
            FateDesign.Damage(h,target,ref d,ref m);
            if(!h.Has(Gift.Destiny))return;var s=State(h);
            if(s.family=="Hunter"&&h.Enemies().Length==1)d.criticalDamageMultiplier*=1+h.Scale(Gift.Destiny,.35);
            if(s.family=="Spear"&&d.motionType==Characters.Damage.MotionType.Dash)m*=1+h.Scale(Gift.Destiny,.4);
            if(s.family=="Samurai"&&target!=null&&target.status!=null&&target.status.wounded){d.SetGuaranteedCritical(int.MinValue,true);}
            if(s.family=="Bomber"&&d.key.ToLowerInvariant().Contains("explos"))m*=1+h.Scale(Gift.Destiny,.3);
            if(s.family=="HeroSkul"&&h.Enemies().Any(e=>e.type==Character.Type.Boss))m*=1+h.Scale(Gift.Destiny,.4);
            if(s.family=="PlagueDoctor"&&target!=null&&target.status!=null&&target.status.poisoned)m*=1+h.Scale(Gift.Destiny,.3);
            if(s.family=="Dominator"&&h.time<s.skillUntil&&d.motionType==Characters.Damage.MotionType.Basic)m*=1+h.Scale(Gift.Destiny,.3);
            if(s.family=="Gambler"&&UnityEngine.Random.value<.2*h.Power(Gift.Destiny))m*=3;
        }
        public static void Gave(EffectHost h,Character target,Damage d,double dealt){
            if(h.Has(Gift.Fate)&&d.motionType!=Characters.Damage.MotionType.DarkAbility&&!target.health.dead&&UnityEngine.Random.value<FateDesign.For(h)["stun"]*h.Power(Gift.Fate))h.Status(target,CharacterStatus.Kind.Stun);
            if(!h.Has(Gift.Destiny)||d.motionType==Characters.Damage.MotionType.DarkAbility||target.health.dead)return;var s=State(h);
            if(s.family=="Sword"&&h.time<s.skillUntil&&d.motionType==Characters.Damage.MotionType.Basic&&UnityEngine.Random.value<h.Scale(Gift.Destiny,.5))h.Status(target,CharacterStatus.Kind.Wound);
            if(s.family=="Minotaur"&&UnityEngine.Random.value<h.Scale(Gift.Destiny,.08))h.Status(target,CharacterStatus.Kind.Stun);
            if(s.family=="Gargoyle"&&!h.owner.movement.isGrounded&&d.critical)h.Strike(target,6*h.Power(Gift.Destiny),"Gargoyle");
            if(s.family=="Alchemist"&&d.motionType==Characters.Damage.MotionType.Skill&&UnityEngine.Random.value<h.Scale(Gift.Destiny,.2))h.Status(target,CharacterStatus.Kind.Poison);
            if(s.family=="Clown"&&d.critical&&h.Ready("Clown",.3f))h.Strike(target,5*h.Power(Gift.Destiny),"ClownKnife",false,Characters.Damage.AttackType.Projectile);
            if(s.family=="Champion")foreach(var a in Native(h,"FighterPassive")){var gauge=Get(a,"_gauge") as Characters.Gear.Weapons.Gauges.ValueGauge;if(gauge!=null)gauge.Add(15*(float)h.Power(Gift.Destiny));}
            if(s.family=="GuanYu"&&d.motionType==Characters.Damage.MotionType.Skill&&h.Ready("GuanYu",2))h.Strike(target,20*h.Power(Gift.Destiny),"GuanYuBlade");
            if(s.family=="Dracula"&&d.critical&&h.Ready("Dracula",.5f))h.owner.health.Heal(h.Power(Gift.Destiny));
            if((s.family=="Slime"||s.family=="PlagueDoctor")&&UnityEngine.Random.value<.15*h.Power(Gift.Destiny))h.Status(target,CharacterStatus.Kind.Poison);
            if(s.family=="Balrog"&&UnityEngine.Random.value<.3*h.Power(Gift.Destiny))h.Status(target,CharacterStatus.Kind.Burn);
            if(s.family=="Aladdin"&&h.Ready("AladdinGold",1))Data.GameData.Currency.gold.Earn(3*h.Power(Gift.Destiny));
            if(s.family=="Viking"&&d.motionType==Characters.Damage.MotionType.Skill)h.Status(target,CharacterStatus.Kind.Freeze);
        }
        public static void Killed(EffectHost h,Character target){
            if(h.Has(Gift.Fate)){var f=FateDesign.For(h);if(f["killHeal"]>0)h.owner.health.Heal(f["killHeal"]*h.Power(Gift.Fate));if(f["killCooldown"]>0)foreach(var a in h.owner.actions.Where(a=>a.type==Act.Type.Skill&&a.cooldown.time!=null))a.cooldown.time.ReduceCooldownPercent((float)(f["killCooldown"]*h.Power(Gift.Fate)));}
            if(!h.Has(Gift.Destiny))return;var s=State(h);
            if(s.family=="Reaper"&&target.type==Character.Type.Boss)foreach(var a in Native(h,"GrimReaperPassive")){var stack=AccessTools.Property(a.GetType(),"stack");if(stack!=null)stack.SetValue(a,(int)stack.GetValue(a,null)+(int)(44*h.Power(Gift.Destiny)),null);}
            if(s.family=="GraveDigger")FanEffect.Orb(h,target.transform.position,4,10*h.Power(Gift.Destiny),"Ghost",Color.magenta);
            if(s.family=="Thief")Data.GameData.Currency.gold.Earn(5*h.Power(Gift.Destiny));
            if(s.family=="Predator")foreach(var a in h.owner.actions.Where(a=>a.type==Act.Type.Skill&&a.cooldown.time!=null))a.cooldown.time.ReduceCooldownPercent(.03f*(float)h.Power(Gift.Destiny));
        }
        public static void Block(EffectHost h){
            var s=State(h);if(!h.Has(Gift.Destiny))return;
            if(s.family=="ChiefGuard"&&h.Ready("ChiefGuard",3))h.owner.health.shield.Add(s,10*(float)h.Power(Gift.Destiny));
            if(s.family=="Slave"&&++s.blocks%3==0){s.invincibleUntil=h.time+3;h.owner.stat.AttachTimedValues(new Stat.Values(new Stat.Value(Stat.Category.Percent,Stat.Kind.PhysicalAttackDamage,1+.25*h.Power(Gift.Destiny))),3);}
        }
        public static void Incoming(EffectHost h,ref Damage d){if(h.Has(Gift.Fate)){var f=FateDesign.For(h);d.multiplier*=Math.Max(0,1+h.Power(Gift.Fate)*(f["incoming"]+(h.owner.health.shield.amount<=0?f["unshieldedIncoming"]:0)));}var s=State(h);if(h.Has(Gift.Destiny)&&s.family=="Genie"&&h.time<s.blockUntil){d.@null=true;h.Blocked();}}
        public static void Applied(EffectHost h,Character target,CharacterStatus.ApplyInfo info){}
        public static void Swap(EffectHost h){
            var s=State(h);if(h.Has(Gift.Fate)&&FateDesign.For(h)["swapCost"]>0)h.owner.health.TakeHealth(FateDesign.For(h)["swapCost"]*h.Power(Gift.Fate));if(!h.Has(Gift.Destiny))return;
            s.skillUntil=h.time+5;
            if(s.family=="Archlich")foreach(var a in h.owner.playerComponents.inventory.weapon.polymorphOrCurrent.GetComponentsInChildren<Act>(true).Where(a=>a.name.ToLowerInvariant().Contains("seal")))if(a.cooldown.time!=null)a.cooldown.time.ReduceCooldown(10*(float)h.Power(Gift.Destiny));
        }
        public static bool Lethal(EffectHost h,ref double amount){
            var s=State(h);
            if(h.Has(Gift.Fate)&&FateDesign.For(h)["revive"]>0&&h.Ready("FateRevive",(float)FateDesign.For(h)["revive"])){h.Revived();amount=0;h.owner.health.SetCurrentHealth(Math.Max(1,h.owner.health.maximumHealth*.2*h.Power(Gift.Fate)));s.invincibleUntil=h.time+1;return true;}
            if(!h.Has(Gift.Destiny)||s.family!="Mummy"||s.mummyLives>=3)return false;
            s.mummyLives++;h.Revived();amount=0;h.owner.health.SetCurrentHealth(Math.Max(1,h.owner.health.maximumHealth*.1));s.invincibleUntil=h.time+1;return true;
        }
        public static string Save(EffectHost h){return State(h).mummyLives.ToString();}
        public static void Load(EffectHost h,string data){int n;if(int.TryParse(data,out n))State(h).mummyLives=n;}
        public static void Cleanup(EffectHost h){SkullState s;if(states.TryGetValue(h,out s)){s.edits.Restore();h.owner.invulnerable.Detach(s);states.Remove(h);}}
    }
}
