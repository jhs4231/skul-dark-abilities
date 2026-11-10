using System;
using System.Linq;
using Characters;
using Characters.Gear.Upgrades;
using UnityEngine;

namespace FanDarkAbilities
{
    internal static class ExpansionVerification
    {
        static void Assert(bool b,string name){if(!b)throw new Exception("EXPANSION FAIL: "+name);Debug.Log("EXPANSION PASS: "+name);}
        static void Near(double actual,double expected,string name){Assert(Math.Abs(actual-expected)<.001,name+" actual="+actual+" expected="+expected);}
        static UpgradeObject Equip(EffectHost host,Gift gift,int level){
            var inv=host.owner.playerComponents.inventory.upgrade;
            for(int i=0;i<inv.upgrades.Count;i++)if(inv.upgrades[i]!=null){inv.upgrades[i].Detach();inv.Remove(i);}
            var reference=UpgradeResource.instance.upgradeReferences.Single(r=>r.name==Catalog.All[(int)gift].id);
            inv.EquipAt(reference,0);var obj=inv.upgrades[0];obj.SetLevel(level);host.Refresh();host.owner.stat.Update();return obj;
        }
        static Damage Attack(EffectHost h,Damage.MotionType motion,Damage.AttackType type){return new Damage(h.owner,100,Vector2.zero,Damage.Attribute.Physical,type,motion);}
        internal static void Run(){
            var player=Singletons.Singleton<Services.Service>.Instance.levelManager.player;
            if(player==null)throw new Exception("Player required");var host=EffectHost.Ensure(player);
            var inv=player.playerComponents.inventory.upgrade;
            var saved=inv.upgrades.Select(u=>u==null?null:u.reference).ToArray();var levels=inv.upgrades.Select(u=>u==null?0:u.level).ToArray();
            double health=player.health.currentHealth;
            var random=UnityEngine.Random.state;
            try{
                Assert(Catalog.All.Count==57,"56 definitions and one reserved slot");
                Assert(Catalog.All.Select(d=>d.id).Distinct().Count()==57,"unique save identifiers");
                foreach(var d in Catalog.All){
                    Assert(Catalog.All[(int)d.gift]==d,"stable enum/catalog mapping "+d.title);
                    var obj=Equip(host,d.gift,d.levels);
                    Assert(obj.maxLevel==d.levels&&obj.level==d.levels,"instantiate/attach/level "+d.title);
                    Assert(obj.GetCurrentAbility()!=null,"attached ability "+d.title);
                }
                Equip(host,Gift.Harmony,3);var damage=Attack(host,Damage.MotionType.Basic,Damage.AttackType.Melee);host.Outgoing(null,ref damage);
                double diff=Math.Abs(player.stat.GetFinal(Stat.Kind.PhysicalAttackDamage)-player.stat.GetFinal(Stat.Kind.MagicAttackDamage));
                Near(damage.multiplier,diff<=1?1.6:1,"Harmony threshold");
                Equip(host,Gift.Marksman,3);damage=Attack(host,Damage.MotionType.Basic,Damage.AttackType.Projectile);host.Outgoing(null,ref damage);Near(damage.multiplier,1.45,"Marksman projectile");
                damage=Attack(host,Damage.MotionType.Basic,Damage.AttackType.Melee);host.Outgoing(null,ref damage);Near(damage.multiplier,1,"Marksman melee excluded");
                Equip(host,Gift.Raid,3);damage=Attack(host,Damage.MotionType.Dash,Damage.AttackType.Melee);host.Outgoing(null,ref damage);Near(damage.multiplier,1.75,"Raid dash");
                Equip(host,Gift.Ossification,1);damage=Attack(host,Damage.MotionType.Item,Damage.AttackType.Projectile);host.Outgoing(null,ref damage);Near(damage.multiplier,0,"Ossification item suppression");
                damage=Attack(host,Damage.MotionType.Skill,Damage.AttackType.Melee);host.Outgoing(null,ref damage);Near(damage.multiplier,1.7,"Ossification skill amplification");
                Equip(host,Gift.PlateArmor,2);damage=Attack(host,Damage.MotionType.Skill,Damage.AttackType.Melee);host.Incoming(ref damage);Near(damage.multiplier,.5,"PlateArmor first hit");
                Equip(host,Gift.Deadweight,1);damage=Attack(host,Damage.MotionType.Basic,Damage.AttackType.Melee);host.Outgoing(null,ref damage);Near(damage.multiplier,1+player.playerComponents.inventory.item.items.Count(i=>i==null)*.5,"Deadweight empty slots");
                Equip(host,Gift.Fission,1);damage=Attack(host,Damage.MotionType.Basic,Damage.AttackType.Melee);host.Outgoing(null,ref damage);Near(damage.multiplier,3,"Fission outgoing damage");
                Equip(host,Gift.Frostbite,1);damage=Attack(host,Damage.MotionType.Basic,Damage.AttackType.Melee);host.Outgoing(null,ref damage);Near(damage.multiplier,.85,"Frostbite penalty");
                Equip(host,Gift.WyvernHead,1);damage=Attack(host,Damage.MotionType.Basic,Damage.AttackType.Melee);host.Incoming(ref damage);Near(damage.multiplier,1.25,"Wyvern head incoming");
                Equip(host,Gift.Raid,3);
                inv.EquipAt(UpgradeResource.instance.upgradeReferences.Single(r=>r.name=="FanBlackSun"),1);host.Refresh();damage=Attack(host,Damage.MotionType.Dash,Damage.AttackType.Melee);host.Outgoing(null,ref damage);Near(damage.multiplier,1.375,"BlackSun halves normal bonus");
                Equip(host,Gift.Ossification,1);inv.EquipAt(UpgradeResource.instance.upgradeReferences.Single(r=>r.name=="FanBlackSun"),1);host.Refresh();damage=Attack(host,Damage.MotionType.Skill,Damage.AttackType.Melee);host.Outgoing(null,ref damage);Near(damage.multiplier,2.4,"BlackSun doubles curse bonus");
                Debug.Log("EXPANSION VERIFICATION COMPLETE");
            }finally{
                for(int i=0;i<inv.upgrades.Count;i++)if(inv.upgrades[i]!=null){inv.upgrades[i].Detach();inv.Remove(i);}
                for(int i=0;i<saved.Length;i++)if(saved[i]!=null){inv.EquipAt(saved[i],i);inv.upgrades[i].SetLevel(levels[i]);}
                host.Refresh();player.stat.Update();player.health.SetCurrentHealth(health);UnityEngine.Random.state=random;
                Debug.Log("EXPANSION VERIFICATION INVENTORY RESTORED (no save invoked)");
            }
        }
    }
}
