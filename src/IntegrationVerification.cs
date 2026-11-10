using System;
using System.Collections;
using System.Linq;
using Characters;
using Characters.Gear.Upgrades;
using Data;
using GameResources;
using UnityEngine;

namespace FanDarkAbilities
{
    internal static class IntegrationVerification
    {
        internal static bool running;
        static void Check(bool condition,string message){if(!condition)throw new Exception("INTEGRATION FAIL: "+message);Debug.Log("INTEGRATION PASS: "+message);}
        static void Clear(EffectHost h){var inv=h.owner.playerComponents.inventory.upgrade;for(int i=0;i<inv.upgrades.Count;i++)if(inv.upgrades[i]!=null){inv.upgrades[i].Detach();inv.Remove(i);}h.Refresh();}
        static void Equip(EffectHost h,Gift g,int slot){var inv=h.owner.playerComponents.inventory.upgrade;var d=Catalog.All[(int)g];inv.EquipAt(UpgradeResource.instance.upgradeReferences.Single(r=>r.name==d.id),slot);inv.upgrades[slot].SetLevel(d.levels);h.Refresh();h.owner.stat.Update();}
        static IEnumerator Wait(EffectHost h,float seconds){float end=h.time+seconds;while(h!=null&&h.time<end)yield return null;}
        internal static IEnumerator Run(){
            if(running)yield break;running=true;
            var p=Singletons.Singleton<Services.Service>.Instance.levelManager.player;var h=EffectHost.Ensure(p);var inv=p.playerComponents.inventory.upgrade;
            var refs=inv.upgrades.Select(u=>u==null?null:u.reference).ToArray();var levels=inv.upgrades.Select(u=>u==null?0:u.level).ToArray();
            double hp=p.health.currentHealth;var weapons=p.playerComponents.inventory.weapon;var names=weapons.weapons.Select(w=>w==null?null:w.name).ToArray();int current=weapons.currentIndex;
            try{
                Clear(h);
                foreach(var w in GearResource.instance.weapons)Debug.Log("SKULL REFERENCE: "+w.name+" family="+SkullRules.FamilyName(w.name)+" obtainable="+w.obtainable);
                double physical=p.stat.GetFinal(Stat.Kind.PhysicalAttackDamage);
                Equip(h,Gift.Humility,0);yield return Wait(h,.6f);double reduced=p.stat.GetFinal(Stat.Kind.PhysicalAttackDamage);
                Debug.Log("INTEGRATION OBSERVE: Humility physical baseline="+physical+" active="+reduced);
                Check(reduced<=physical+.001,"Humility does not increase item attack bonuses");
                Clear(h);yield return Wait(h,.6f);
                Equip(h,Gift.Vampire,0);p.health.SetCurrentHealth(Math.Max(1,p.health.maximumHealth-30));double before=p.health.currentHealth;
                p.health.Heal(1,false);Check(Math.Abs(p.health.currentHealth-before-6)<.001,"Vampire actual Heal gains 1+5");
                Clear(h);yield return Wait(h,.1f);
                Equip(h,Gift.PlateArmor,0);before=p.health.currentHealth;
                var attack=new Damage(p,20,Vector2.zero,Damage.Attribute.Physical,Damage.AttackType.Melee,Damage.MotionType.Basic);
                double expected=Math.Ceiling(10*p.stat.GetFinal(Stat.Kind.TakingDamage)*p.stat.GetFinal(Stat.Kind.TakingPhysicalDamage));
                p.health.TakeDamage(ref attack);Debug.Log("PLATE ACTUAL: loss="+(before-p.health.currentHealth)+" expected="+expected+" modified="+attack.amount+" shield="+p.health.shield.amount);Check(Math.Abs(before-p.health.currentHealth-expected)<.001,"PlateArmor halves damage before native defense");
                Clear(h);yield return Wait(h,.6f);
                Equip(h,Gift.Genetic,0);yield return Wait(h,.8f);
                foreach(var w in weapons.weapons.Where(w=>w!=null&&w.currentSkills!=null&&w.currentSkills.Count>=2)){
                    Check(w.currentSkills[0].key==w.currentSkills[1].key,"Genetic duplicates left skill "+w.name);
                    Check(w.currentSkills[0].action!=w.currentSkills[1].action,"Genetic uses distinct actions and cooldowns "+w.name);
                }
                Clear(h);yield return Wait(h,.8f);
                Check(weapons.weapons.All(w=>w==null||!w.GetComponentsInChildren<Characters.Gear.Weapons.SkillInfo>(true).Any(s=>s.name=="FanGeneticSkill")),"Genetic temporary clone removed");
                Equip(h,Gift.Doppelganger,0);yield return Wait(h,1.5f);
                Check(weapons.weapons.All(w=>w!=null)&&weapons.weapons[0].name==weapons.weapons[1].name,"Doppelganger loads a real duplicate skull");
                Clear(h);yield return Wait(h,1);
                Check(weapons.weapons.Select(w=>w==null?null:w.name).SequenceEqual(names),"Doppelganger temporary standby restored");
                if(weapons.currentIndex!=current)weapons.NextWeapon(true);
                Equip(h,Gift.Wyvern,0);Check(inv.upgrades.Count(u=>u!=null)==2,"Wyvern reserves two upgrade slots");
                p.health.TakeHealth(p.health.currentHealth+1000);
                yield return Wait(h,.15f);
                Check(!p.health.dead&&h.wyvernUntil>h.time,"Wyvern intercepts actual lethal damage");
                Check(Math.Abs(p.health.currentHealth-p.health.maximumHealth*.1)<1.001,"Wyvern restores ten percent health");
                Check(p.GetComponentInChildren<WyvernForm>()!=null,"Wyvern form renderer appears");
                Clear(h);yield return Wait(h,.2f);Check(p.GetComponentInChildren<WyvernForm>()==null&&p.spriteEffectStack.mainRenderer.enabled,"Wyvern form detaches and restores player renderer");
                Debug.Log("INTEGRATION VERIFICATION COMPLETE");
            }finally{
                Clear(h);foreach(var r in refs.Select((value,index)=>new{value,index}))if(r.value!=null){inv.EquipAt(r.value,r.index);inv.upgrades[r.index].SetLevel(levels[r.index]);}
                h.Refresh();p.stat.Update();p.health.SetCurrentHealth(Math.Min(hp,p.health.maximumHealth));running=false;
                Debug.Log("INTEGRATION VERIFICATION RESTORED UPGRADE INVENTORY; ORIGINAL SAVE BACKUP STILL REQUIRED");
            }
        }
        internal static void SaveProbe(){
            var manager=Singletons.Singleton<Services.Service>.Instance.levelManager;var h=EffectHost.Ensure(manager.player);
            if(h.Has(Gift.Ossification)&&h.Has(Gift.BlackSun)&&h.Has(Gift.Fate)){
                Check(h.revivals==3&&h.awakenings==2&&h.destroyed==7,"saved counters restored after process restart");
                Check(h.Level(Gift.Ossification)==1&&h.Level(Gift.BlackSun)==1&&h.Level(Gift.Fate)==1,"three custom references restored after process restart");
                Debug.Log("EXPANSION RESTART RESTORATION COMPLETE");return;
            }
            Clear(h);Equip(h,Gift.Ossification,0);Equip(h,Gift.BlackSun,1);Equip(h,Gift.Fate,2);
            h.revivals=3;h.awakenings=2;h.destroyed=7;h.fissionTime=512.5f;
            manager.Save();GameData.Currency.SaveAll();Singletons.PersistentSingleton<Platforms.PlatformManager>.Instance.SaveDataToFile();
            Debug.Log("EXPANSION SAVE PROBE WRITTEN; RESTART THEN PRESS F12; RESTORE ORIGINAL BACKUP AFTER TEST");
        }
        internal static void NextMap(){
            var p=Singletons.Singleton<Services.Service>.Instance.levelManager.player;
            var gate=Level.Map.Instance.GetComponentsInChildren<Level.Gate>().FirstOrDefault(g=>g.activated);
            if(gate==null)throw new Exception("No active gate");gate.InteractWith(p);
        }
    }
}
