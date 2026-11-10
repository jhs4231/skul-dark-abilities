using System;
using System.Collections.Generic;
using System.Linq;
using Characters;
using Characters.Gear.Upgrades;
using Characters.Gear.Weapons;
using UnityEngine;
using HarmonyLib;
using Act = Characters.Actions.Action;

namespace FanDarkAbilities
{
    public sealed class GiftAbility : UpgradeAbility
    {
        internal EffectHost host;
        public override void Attach(Character target) {
            Detach(); host=EffectHost.Ensure(target); host.Refresh();
        }
        public override void Detach() { var old=host;host=null;if(old!=null)old.Refresh(); }
        internal void Refresh() { if(host!=null)host.Refresh(); }
        void OnDestroy() { Detach(); }
    }
    public sealed class EnemyMark : MonoBehaviour
    {
        public int penance, poisonStacks; public bool dark;
        public float stunStarted,hookedUntil;
    }
    public sealed class EffectHost : MonoBehaviour
    {
        public static EffectHost Player;
        public Character owner;
        internal readonly Dictionary<Gift,int> effective = new Dictionary<Gift,int>();
        readonly List<Gift> chaos = new List<Gift>(), trueChaos = new List<Gift>();
        readonly Dictionary<string,float> cooldowns = new Dictionary<string,float>();
        readonly List<float> defense = new List<float>(), scarecrowHits = new List<float>(), hits = new List<float>();
        readonly Dictionary<Act,int> dashOriginal = new Dictionary<Act,int>();
        readonly HashSet<Character> thawTargets = new HashSet<Character>();
        Stat.Values stats;
        int mapId, kills, armorHits;
        internal int revivals, awakenings, sacrifices, destroyed, darkKills;
        internal float fissionTime = 600;
        public float time;
        float refreshAt, lightningAt, scarecrowAt, randomAt, abyssAt;
        float criticalUntil, turtleCharge, turtleUntil, swapUntil, chargeStarted=-1;
        internal float wyvernUntil, wyvernReady;
        internal double milk, chaosAttack, chaosDamage;
        int skillCount;
        bool essenceWasCooling, initialized, refreshing;
        UpgradeObject virtualRegen;
        Vector3 lastPosition;
        float distance;
        public static EffectHost Ensure(Character c) {
            var h=c.GetComponent<EffectHost>();if(h==null){h=c.gameObject.AddComponent<EffectHost>();h.Initialize(c);}return h;
        }
        void Initialize(Character c) {
            owner=c;Player=this;gameObject.AddComponent<RunHud>();lastPosition=c.transform.position;
            c.onStartAction+=OnAction;
            c.onStartCharging+=ChargeStart;c.onStopCharging+=ChargeStop;c.onCancelCharging+=ChargeCancel;
            c.playerComponents.inventory.weapon.onSwap+=OnSwap;
            initialized=true; RunState.Load(this); Refresh();
        }
        public int Level(Gift g) { int n;return effective.TryGetValue(g,out n)?n:0; }
        public bool Has(Gift g) { return Level(g)>0; }
        bool Equipped(Gift g) {
            return owner!=null&&owner.playerComponents.inventory.upgrade.upgrades.Any(u=>u!=null&&u.reference!=null&&u.reference.name==Catalog.All[(int)g].id);
        }
        public double Power(Gift g) {
            if(!Has(g))return 0;
            if(g==Gift.BlackSun)return 1;
            return Has(Gift.BlackSun)?(g>=Gift.Doppelganger?2:.5):1;
        }
        public double Scale(Gift g,double amount) { return amount*Power(g); }
        public bool Ready(string key,float cd) {
            float end;if(cooldowns.TryGetValue(key,out end)&&end>time)return false;
            cooldowns[key]=time+cd;return true;
        }
        public void Refresh() {
            if(!initialized||refreshing||owner==null)return;
            bool hadArmor=Has(Gift.PlateArmor);
            refreshing=true;
            try {
                effective.Clear();
                foreach(var u in owner.playerComponents.inventory.upgrade.upgrades) {
                    if(u==null||u.reference==null)continue;
                    var d=Catalog.Find(u.reference.name);if(d==null)continue;
                    var effect=u.GetComponent<GiftAbility>();
                    if(d.gift!=Gift.Regeneration&&(effect==null||effect.host!=this))continue;
                    effective[d.gift]=Mathf.Clamp(u.level,1,d.levels);
                }
                if(Has(Gift.Chaos)&&chaos.Count==0)Roll(chaos,false);
                if(!Has(Gift.Chaos))chaos.Clear();
                if(Has(Gift.TrueChaos)&&trueChaos.Count==0)Roll(trueChaos,true);
                if(!Has(Gift.TrueChaos))trueChaos.Clear();
                foreach(var g in chaos)effective[g]=Catalog.All[(int)g].levels;
                foreach(var g in trueChaos)effective[g]=1;
                if(Has(Gift.WyvernHead)&&Has(Gift.WyvernTail)&&(trueChaos.Contains(Gift.WyvernHead)||trueChaos.Contains(Gift.WyvernTail)))effective[Gift.Wyvern]=1;
                if(!Has(Gift.Wyvern))wyvernUntil=0;
                if(Has(Gift.PlateArmor)&&!hadArmor)armorHits=0;
                if(!Has(Gift.Defense))defense.Clear();
                if(!Has(Gift.Scarecrow))scarecrowHits.Clear();
                SkullRules.Apply(this);
                RebuildStats();
                ApplyDashStacks();
                bool realRegen=Equipped(Gift.Regeneration);
                if(Has(Gift.Regeneration)&&!realRegen&&virtualRegen==null){
                    virtualRegen=Plugin.Create(UpgradeResource.instance.upgradeReferences.First(r=>r.name==Plugin.Id));virtualRegen.transform.SetParent(owner.transform);virtualRegen.Attach(owner);virtualRegen.SetLevel(Level(Gift.Regeneration));
                }
                if(virtualRegen!=null&&(!Has(Gift.Regeneration)||realRegen)){virtualRegen.Detach();Destroy(virtualRegen.gameObject);virtualRegen=null;}
                SkullRules.Apply(this);
            } finally { refreshing=false; }
        }
        void Roll(List<Gift> list,bool cursed) {
            list.Clear();
            var pool=Catalog.All.Where(d=>d.cursed==cursed&&d.gift!=Gift.Chaos&&d.gift!=Gift.TrueChaos&&d.gift!=Gift.Wyvern&&d.gift!=Gift.WyvernReserve).Select(d=>d.gift).ToList();
            // Six distinct effects; an already equipped effect is not counted twice.
            var chapter=Singletons.Singleton<Services.Service>.Instance.levelManager.currentChapter;
            int seed=Data.GameData.Save.instance.randomSeed ^ ((int)chapter.type<<24) ^ (chapter.stageIndex<<16) ^ (chapter.currentStage.pathIndex<<8) ^ (cursed?0x1f342:0x3f971);
            var random=new System.Random(seed);
            for(int i=0;i<6&&pool.Count>0;i++) {int n=random.Next(pool.Count);list.Add(pool[n]);pool.RemoveAt(n);}
        }
        void RebuildStats() {
            if(stats!=null)owner.stat.DetachValues(stats);
            var v=new List<Stat.Value>();
            double attack=0;
            if(Has(Gift.PlateArmor))attack+=Scale(Gift.PlateArmor,Level(Gift.PlateArmor)*Math.Pow(.5,armorHits));
            if(Has(Gift.Necromancy))attack+=Scale(Gift.Necromancy,Math.Min(10,kills)*(.1*Level(Gift.Necromancy)));
            attack+=Scale(Gift.MilkyWay,milk)+Scale(Gift.Scarecrow,Math.Min(10,scarecrowHits.Count)*.2);
            attack+=Scale(Gift.Chaos,chaosAttack)+Scale(Gift.Spider,awakenings*.8)+Scale(Gift.RedCrown,Math.Min(6,sacrifices)*.66)+Scale(Gift.Void,Math.Min(30,destroyed)*.1);
            v.Add(new Stat.Value(Stat.Category.PercentPoint,Stat.Kind.PhysicalAttackDamage,attack));
            v.Add(new Stat.Value(Stat.Category.PercentPoint,Stat.Kind.MagicAttackDamage,attack));
            if(Has(Gift.Defense)){
                double n=1+Scale(Gift.Defense,defense.Count*(Level(Gift.Defense)==1?.15:.25));
                v.Add(new Stat.Value(Stat.Category.Percent,Stat.Kind.PhysicalAttackDamage,n));
                v.Add(new Stat.Value(Stat.Category.Percent,Stat.Kind.MagicAttackDamage,n));
            }
            Add(v,Gift.MilkyWay,Stat.Kind.EssenceCooldownSpeed,.25*Level(Gift.MilkyWay));
            Add(v,Gift.Judgment,Stat.Kind.MovementSpeed,.4*Level(Gift.Judgment));
            Add(v,Gift.Conductor,Stat.Kind.ChargingSpeed,.2*Level(Gift.Conductor));
            Add(v,Gift.Storm,Stat.Kind.SwapCooldownSpeed,.2*Level(Gift.Storm));
            Add(v,Gift.Tortoise,Stat.Kind.ChargingSpeed,-.7);
            Add(v,Gift.Genetic,Stat.Kind.SkillCooldownSpeed,-.3);
            Add(v,Gift.TimeExchange,Stat.Kind.SkillCooldownSpeed,-.4);
            Add(v,Gift.TimeExchange,Stat.Kind.SwapCooldownSpeed,-.4);
            Add(v,Gift.Secular,Stat.Kind.TakingHealAmount,-.5);
            if(Has(Gift.Panacea)){
                v.Add(new Stat.Value(Stat.Category.Percent,Stat.Kind.KnockbackResistance,0));
                v.Add(new Stat.Value(Stat.Category.Percent,Stat.Kind.StunResistance,0));
            }
            double hp=1;
            if(Has(Gift.Doppelganger))hp*=Math.Max(.05,1-Scale(Gift.Doppelganger,.5));
            if(Has(Gift.WyvernTail)&&!Has(Gift.Wyvern))hp*=Math.Max(.05,1-Scale(Gift.WyvernTail,.25));
            v.Add(new Stat.Value(Stat.Category.Percent,Stat.Kind.Health,hp));
            v.Add(new Stat.Value(Stat.Category.Constant,Stat.Kind.Health,Scale(Gift.BlackHeart,darkKills*10)));
            SkullRules.Stats(this,v);
            stats=new Stat.Values(v.ToArray());owner.stat.AttachValues(stats);
        }
        void Add(List<Stat.Value> v, Gift g,Stat.Kind kind,double n) {
            if(Has(g))v.Add(new Stat.Value(Stat.Category.PercentPoint,kind,Scale(g,n)));
        }
        void ApplyDashStacks() {
            foreach(var a in owner.actions.Where(a=>a!=null&&a.type==Act.Type.Dash&&a.cooldown!=null&&a.cooldown.time!=null)){
                int old;if(!dashOriginal.TryGetValue(a,out old)){old=a.cooldown.time.maxStack;dashOriginal[a]=old;}
                int target=old+(Has(Gift.Raid)?1:0);
                AccessTools.Property(typeof(Characters.Cooldowns.Time),"maxStack").SetValue(a.cooldown.time,target,null);
                if(a.cooldown.stacks>target)a.cooldown.stacks=target;
            }
        }
        void NewRoom() {
            kills=0;armorHits=0;distance=0;defense.Clear();scarecrowHits.Clear();hits.Clear();thawTargets.Clear();
            chaos.Clear();trueChaos.Clear();cooldowns.Clear();milk=0;chargeStarted=-1;turtleCharge=0;
            lightningAt=time+1;scarecrowAt=time+3;abyssAt=time+10;
            foreach(var effect in FindObjectsOfType<FanEffect>())if(effect.host==this)Destroy(effect.gameObject);
            Refresh();WorldRules.NewRoom(this);
            if(Has(Gift.Fate)&&FateDesign.For(this)["roomShield"]>0)owner.health.shield.Add(this,(float)(FateDesign.For(this)["roomShield"]*Power(Gift.Fate)));
        }
        void Update() {
            if(!initialized||owner==null||owner.health.dead)return;
            float dt=owner.chronometer.master.deltaTime;
            if(dt<=0)return;
            time+=dt;
            int id=global::Level.Map.Instance==null?0:global::Level.Map.Instance.GetInstanceID();
            if(id!=mapId){mapId=id;NewRoom();}
            float travel=Vector3.Distance(owner.transform.position,lastPosition);lastPosition=owner.transform.position;
            if(travel<5)distance+=travel;
            if(time>=randomAt){randomAt=time+5;chaosAttack=UnityEngine.Random.Range(.5f,1.5f);chaosDamage=UnityEngine.Random.Range(-.15f,.5f);}
            if(time>=refreshAt){refreshAt=time+.2f;defense.RemoveAll(t=>t<time);scarecrowHits.RemoveAll(t=>t<time);Refresh();}
            if(Has(Gift.Tortoise)&&chargeStarted>=0)turtleCharge=Mathf.Min(5,time-chargeStarted)*.2f;
            if(Has(Gift.Judgment)&&time>=lightningAt){
                lightningAt=time+Mathf.Max(.25f,(Level(Gift.Judgment)==1?2:1)/(1+distance*.03f));
                var enemies=Enemies().OrderBy(e=>(e.transform.position-owner.transform.position).sqrMagnitude).Take(1+Math.Min(5,(int)(distance/10))).ToArray();
                foreach(var e in enemies){FanEffect.Flash(this,e.transform.position,Color.yellow,1.5f);Strike(e,12*Power(Gift.Judgment),"Judgment");}
            }
            if(Has(Gift.Scarecrow)&&time>=scarecrowAt){scarecrowAt=time+3;if(Enemies().Any()&&FindObjectsOfType<FanDummy>().Count(d=>d.host==this)<6)FanDummy.Create(this);}
            if(Has(Gift.MilkyWay)){
                var q=owner.playerComponents.inventory.quintessence;
                bool cooling=q!=null && WorldRules.QuintessenceCooling(q);
                if(essenceWasCooling&&!cooling){milk=0;RebuildStats();}essenceWasCooling=cooling;
            }
            if(Has(Gift.Fission)){fissionTime-=dt*(float)Power(Gift.Fission);if(fissionTime<=0){fissionTime=0;owner.health.TryKill();}}
            if(Has(Gift.Abyss)&&time>=abyssAt){abyssAt=time+10;owner.health.TakeHealth(10*Power(Gift.Abyss));}
            owner.invulnerable.Detach(this);
            if(wyvernUntil>time)owner.invulnerable.Attach(this);
            foreach(var target in thawTargets.ToArray()){
                if(target==null||target.health.dead){thawTargets.Remove(target);continue;}
                if(target.status==null||!target.status.freezed){thawTargets.Remove(target);Burst(target.transform.position,2.5f,8*Level(Gift.Cold)*Power(Gift.Cold),"Cold",false);}
            }
            SkullRules.Tick(this,dt);WorldRules.Tick(this,dt);
        }
        public Character[] Enemies() {
            return FindObjectsOfType<Character>().Where(c=>c!=null&&c!=owner&&c.gameObject.activeInHierarchy&&c.health!=null&&!c.health.dead&&c.type!=Character.Type.Player&&c.type!=Character.Type.PlayerMinion&&c.type!=Character.Type.Dummy&&c.type!=Character.Type.Trap&&c.collider!=null).ToArray();
        }
        public void Strike(Character target,double damage,string key,bool bypass=false,Damage.AttackType attack=Damage.AttackType.Additional) {
            if(target==null||target.health.dead||damage<=0)return;
            Damage.Attribute attribute=owner.stat.GetAdaptiveForceAttribute();
            var d=new Damage(owner,damage,(Vector2)target.transform.position,attribute,attack,Damage.MotionType.DarkAbility,"Fan"+key,
                owner.stat.GetFinal(attribute==Damage.Attribute.Physical?Stat.Kind.PhysicalAttackDamage:Stat.Kind.MagicAttackDamage)*owner.stat.GetFinal(Stat.Kind.AttackDamage),0,
                owner.stat.GetFinal(Stat.Kind.CriticalChance)-1,owner.stat.GetFinal(Stat.Kind.CriticalDamage),1,true,false,0,0,(short)(bypass?1:0));
            if(bypass){
                // Damage the health directly so shields and cinematic invulnerability do not absorb the wind.
                Outgoing(target,ref d);d.Evaluate(target.health.immuneToCritical);target.health.TakeHealth(d.amount);
                if(target.health.dead)Killed(target,d.key);
            } else owner.Attack(target,ref d);
        }
        public void Burst(Vector3 pos,float radius,double damage,string key,bool stun) {
            FanEffect.Flash(this,pos,stun?Color.white:new Color(.4f,.7f,1),radius);
            foreach(var e in Enemies().Where(e=>Vector3.Distance(e.transform.position,pos)<=radius)){
                Strike(e,damage,key);if(stun&&!e.health.dead)Status(e,CharacterStatus.Kind.Stun);
            }
        }
        public void Status(Character target,CharacterStatus.Kind kind) { if(target!=null&&target.status!=null&&!target.health.dead)owner.GiveStatus(target,new CharacterStatus.ApplyInfo(kind)); }
        void ChargeStart(Act a){chargeStarted=time;}
        void ChargeCancel(Act a){chargeStarted=-1;turtleUntil=time+4;}
        void ChargeStop(Act a){
            if(Has(Gift.Conductor))FanEffect.Orb(this,owner.transform.position,5,6*Level(Gift.Conductor)*Power(Gift.Conductor),"Conductor",Color.cyan);
            chargeStarted=-1;turtleUntil=time+4;
        }
        void OnAction(Act a) {
            if(a==null)return;
            if((a.type==Act.Type.BasicAttack||a.type==Act.Type.JumpAttack)&&Has(Gift.Wind)&&Ready("Wind",.12f)) {
                var direction=owner.lookingDirection==Character.LookingDirection.Right?Vector2.right:Vector2.left;
                FanEffect.Projectile(this,owner.collider.bounds.center,direction,10,10*Power(Gift.Wind),"Wind",Color.white);
            }
            if(a.type==Act.Type.Skill){
                if(Has(Gift.Abyss)){abyssAt=time+10;if(++skillCount%10==0){var e=Enemies().OrderBy(c=>(c.transform.position-owner.transform.position).sqrMagnitude).FirstOrDefault();
                    if(e!=null){FanEffect.Zone(this,e.transform.position,3,10,8*Power(Gift.Abyss),"Abyss",new Color(.6f,.1f,.7f));foreach(var c in Enemies().Where(c=>Vector3.Distance(c.transform.position,e.transform.position)<3)){Status(c,CharacterStatus.Kind.Unmoving);if(c.status!=null)c.status.unmoving.remainTime=10;}}}}
            }
            if(a.type==Act.Type.Dash&&Has(Gift.Raid)&&UnityEngine.Random.value<Scale(Gift.Raid,.2+.1*Level(Gift.Raid)))a.cooldown.stacks=a.cooldown.maxStack;
            if(a.type==Act.Type.JumpAttack&&owner.playerComponents.inventory.weapon.polymorphOrCurrent.category==Weapon.Category.Power&&Has(Gift.Brawler)&&Ready("Brawler",Level(Gift.Brawler)==1?8:5))StartCoroutine(LandingShock());
            SkullRules.Action(this,a);
        }
        System.Collections.IEnumerator LandingShock(){
            float until=time+3;while(owner!=null&&!owner.movement.isGrounded&&time<until)yield return null;
            if(owner==null||!owner.movement.isGrounded)yield break;
            foreach(var e in Enemies().Where(e=>e.movement!=null&&e.movement.isGrounded)){
                Strike(e,18*Power(Gift.Brawler),"Brawler");Status(e,CharacterStatus.Kind.Stun);
            }
            FanEffect.Flash(this,owner.transform.position,Color.yellow,4);
        }
        void OnSwap(){
            if(Has(Gift.Storm))FanEffect.Storm(this,owner.transform.position,3+Level(Gift.Storm),5*Power(Gift.Storm));
            if(Has(Gift.Guillotine)){owner.playerComponents.inventory.weapon.ResetSwapCooldown();owner.health.TakeHealth(7*Power(Gift.Guillotine));swapUntil=time+2;}
            Refresh();SkullRules.Swap(this);
        }
        public void Outgoing(Character target,ref Damage d) {
            if(d.@null)return;
            double m=1;
            if(Has(Gift.Harmony)&&Math.Abs(owner.stat.GetFinal(Stat.Kind.PhysicalAttackDamage)-owner.stat.GetFinal(Stat.Kind.MagicAttackDamage))<=1.00001)m*=1+Scale(Gift.Harmony,.2*Level(Gift.Harmony));
            if(Has(Gift.Intimidation)){
                var w=owner.playerComponents.inventory.weapon.polymorphOrCurrent;
                double height=w!=null&&w.hitbox!=null?w.hitbox.size.y:owner.collider.size.y;
                m*=1+Scale(Gift.Intimidation,Math.Min(.15,Math.Max(0,height)*.075));
            }
            if(Has(Gift.Raid)&&d.motionType==Damage.MotionType.Dash)m*=1+Scale(Gift.Raid,.25*Level(Gift.Raid));
            if(Has(Gift.Marksman)&&(d.attackType==Damage.AttackType.Projectile||d.attacker.projectile!=null))m*=1+Scale(Gift.Marksman,.15*Level(Gift.Marksman));
            if(Has(Gift.Legion)&&d.attacker.character!=owner&&d.attacker.character!=null&&d.attacker.character.type==Character.Type.PlayerMinion)m*=1+Scale(Gift.Legion,.15*Level(Gift.Legion));
            if(Has(Gift.Deadweight))m*=1+Scale(Gift.Deadweight,owner.playerComponents.inventory.item.items.Count(i=>i==null)*.5);
            if(Has(Gift.Decay)){
                var w=owner.playerComponents.inventory.weapon.polymorphOrCurrent;
                if(w!=null){var rarity=w.rarity.ToString();m*=Math.Max(0,1+Scale(Gift.Decay,rarity=="Rare"?1.2:rarity=="Unique"?.6:rarity=="Legendary"?-.5:0));}
            }
            m*=1+Scale(Gift.Osteoporosis,Math.Min(5,revivals)*.15);
            m*=1+Scale(Gift.Fission,2);
            m*=1+Scale(Gift.Ossification,.7);
            m*=Math.Max(0,1-Scale(Gift.Frostbite,.15));
            m*=Math.Max(0,1+Scale(Gift.TrueChaos,chaosDamage));
            if(Has(Gift.Tortoise)&&(chargeStarted>=0||time<turtleUntil))m*=1+Scale(Gift.Tortoise,Math.Min(1,turtleCharge));
            if(time<swapUntil)m*=1+Scale(Gift.Guillotine,.5);
            if(d.motionType==Damage.MotionType.Item){if(Has(Gift.Ossification))m=0;else if(Has(Gift.Humility))m*=Math.Max(0,1-Scale(Gift.Humility,.3));}
            if(d.key==CharacterStatus.AttackKeyBleed)m*=1+Scale(Gift.InternalBleeding,1.2);
            if(target!=null){
                var mark=target.GetComponent<EnemyMark>();if(mark!=null)m*=1+mark.penance*.15;
                if(target.status!=null){
                    if(target.status.freezed)m*=1+Scale(Gift.Frostbite,1);
                    if(target.status.stuned&&Has(Gift.Coma))m*=1+Scale(Gift.Coma,Math.Min(.7,(mark==null?0:time-mark.stunStarted)*.14));
                    if(d.key==CharacterStatus.AttackKeyPoison){if(Has(Gift.Necrosis))m*=Math.Max(1,mark==null?1:mark.poisonStacks);
                        if(Has(Gift.Swamp)&&FanEffect.InZone(this,target.transform.position,"Swamp"))m*=1+Scale(Gift.Swamp,.25*Level(Gift.Swamp));}
                }
            }
            if(time<criticalUntil){d.canCritical=true;d.SetGuaranteedCritical(int.MinValue,true);d.critical=true;}
            SkullRules.Damage(this,target,ref d,ref m);
            if(ExplosionRules.depth>0||ExplosionRules.Named(d.key))m*=ExplosionRules.Multiplier(this);
            if(m<=0){d.@null=true;d.@base=0;}else if(d.attribute==Damage.Attribute.Fixed)d.@base*=m;else d.multiplier*=m;
        }
        public void Gave(Character target,Damage d,double dealt) {
            if(target==null||dealt<=0)return;
            if(d.motionType==Damage.MotionType.Quintessence&&Has(Gift.MilkyWay))milk=Math.Min(.5+.5*Level(Gift.MilkyWay),milk+dealt*.001);
            SkullRules.Gave(this,target,d,dealt);
        }
        public void Blocked(){if(Has(Gift.Defense)){if(defense.Count==3)defense.RemoveAt(0);defense.Add(time+5);RebuildStats();}SkullRules.Block(this);}
        public bool Incoming(ref Damage d) {
            if(d.@null)return false;
            if(wyvernUntil>time){
                var attacker=d.attacker.character;if(attacker!=null&&attacker!=owner)Strike(attacker,d.amount*1.5,"WyvernCounter");d.@null=true;return true;
            }
            if(Has(Gift.Panacea)&&d.attacker.trap!=null){d.@null=true;return true;}
            if(Has(Gift.ShadowDodge)&&UnityEngine.Random.value<Scale(Gift.ShadowDodge,.2+.05*Level(Gift.ShadowDodge))){criticalUntil=time+Level(Gift.ShadowDodge);d.@null=true;return true;}
            if(Has(Gift.PlateArmor)&&armorHits<Level(Gift.PlateArmor)+1){armorHits++;d.multiplier*=Math.Max(0,1-Scale(Gift.PlateArmor,.5));RebuildStats();}
            d.multiplier*=1+Scale(Gift.Osteoporosis,Math.Min(5,revivals)*.15);
            if(Has(Gift.WyvernHead)&&!Has(Gift.Wyvern))d.multiplier*=1+Scale(Gift.WyvernHead,.25);
            if(Has(Gift.Necrosis)&&d.attacker.character!=null&&d.attacker.character.status!=null&&d.attacker.character.status.poisoned)d.multiplier*=1+Scale(Gift.Necrosis,.6);
            SkullRules.Incoming(this,ref d);
            return false;
        }
        public void Took(double damage) {
            if(damage<=0)return;
            if(Has(Gift.Fate)&&FateDesign.For(this)["hitGold"]>0)Data.GameData.Currency.gold.Consume(Math.Min(Data.GameData.Currency.gold.balance,(int)(FateDesign.For(this)["hitGold"]*Power(Gift.Fate))));
            if(Has(Gift.Penance)){
                hits.RemoveAll(t=>time-t>10);hits.Add(time);
                if(hits.Count>=3){hits.Clear();var enemies=Enemies().Where(c=>Vector3.Distance(c.transform.position,owner.transform.position)<5).ToArray();
                    Burst(owner.transform.position,5,25*Power(Gift.Penance),"Penance",true);
                    foreach(var e in enemies)if(e!=null&&!e.health.dead){var mark=Mark(e);mark.penance=Math.Min(3,mark.penance+1);}}
            }
        }
        public EnemyMark Mark(Character e){var m=e.GetComponent<EnemyMark>();return m!=null?m:e.gameObject.AddComponent<EnemyMark>();}
        public void Applied(Character target,CharacterStatus.ApplyInfo info){
            if(target==null||target.status==null)return;
            var status=target.status;
            if(info.kind==CharacterStatus.Kind.Burn&&Has(Gift.Disaster)&&Ready("Disaster",4)){
                for(int i=0;i<3;i++)FanEffect.Fireball(this,owner.collider.bounds.center,target.transform.position+Vector3.right*(i-1),1+Level(Gift.Disaster),8*Power(Gift.Disaster));
            }
            if(info.kind==CharacterStatus.Kind.Freeze&&Has(Gift.Cold)){target.CancelAction();thawTargets.Add(target);}
            if(info.kind==CharacterStatus.Kind.Poison){var m=Mark(target);m.poisonStacks=Math.Min(3,m.poisonStacks+1);
                if(Has(Gift.Swamp)&&Ready("Swamp",8))FanEffect.Zone(this,target.transform.position,2.5f,8,0,"Swamp",new Color(.1f,.7f,.2f));}
            if(info.kind==CharacterStatus.Kind.Stun){Mark(target).stunStarted=time;if(Has(Gift.Coma)){status.stun.remainTime*=1+(float)Power(Gift.Coma);if(UnityEngine.Random.value<Scale(Gift.Coma,.5))status.RemoveStun();}}
            SkullRules.Applied(this,target,info);
        }
        public void Bleed(Character target){if(Has(Gift.InternalBleeding)&&UnityEngine.Random.value<Scale(Gift.InternalBleeding,.33))owner.health.TakeHealth(1);}
        public void Killed(Character target,string key){
            if(target==null)return;
            var dummy=target.GetComponent<FanDummy>();if(dummy!=null)return;
            if(Has(Gift.Necromancy)){kills++;for(int i=0;i<Level(Gift.Necromancy)+1;i++)FanEffect.Orb(this,target.transform.position+Vector3.up*.3f*i,4,12*Power(Gift.Necromancy),"Ghost",Color.magenta);}
            if(Has(Gift.InternalBleeding)&&key==CharacterStatus.AttackKeyBleed)owner.health.Heal(Power(Gift.InternalBleeding));
            if(Has(Gift.Fever)&&target.status!=null&&target.status.burning){
                FanEffect.Flash(this,target.transform.position,new Color(1,.3f,0),3);
                foreach(var e in Enemies().Where(e=>e!=target&&Vector3.Distance(e.transform.position,target.transform.position)<3)){Strike(e,12*Power(Gift.Fever),"Fever");Status(e,CharacterStatus.Kind.Burn);}
            }
            var mark=target.GetComponent<EnemyMark>();if(Has(Gift.BlackHeart)&&mark!=null&&mark.dark){darkKills++;RebuildStats();owner.stat.Update();owner.health.Heal(30*Power(Gift.BlackHeart));}
            SkullRules.Killed(this,target);
        }
        public void ScarecrowHit(){if(scarecrowHits.Count==10)scarecrowHits.RemoveAt(0);scarecrowHits.Add(time+10);RebuildStats();}
        public void Revived(){if(Has(Gift.Osteoporosis)){revivals=Math.Min(5,revivals+1);Refresh();}}
        public bool Lethal(ref double amount){
            if(amount<owner.health.currentHealth||owner.health.currentHealth<=0)return false;
            if(Has(Gift.Wyvern)&&time>=wyvernReady){
                amount=0;wyvernUntil=time+10;wyvernReady=time+20;
                owner.health.SetCurrentHealth(Math.Max(1,owner.health.maximumHealth*.1*Power(Gift.Wyvern)));
                Revived();WyvernForm.Begin(this);FanEffect.Zone(this,owner.transform.position,3,10,8*Power(Gift.Wyvern),"Wyvern",Color.red);return true;
            }
            return SkullRules.Lethal(this,ref amount);
        }
        public string ActiveSummary(){
            string text="";
            if(Has(Gift.Destiny))text+="운명: "+SkullRules.State(this).family+"\n";
            if(Has(Gift.Fate)){var f=FateDesign.For(this);text+="숙명: "+f.title+"\n"+f.description+"\n";}
            if(Has(Gift.Chaos))text+="혼돈: "+string.Join(" · ",chaos.Select(g=>Catalog.All[(int)g].title).ToArray())+"\n";
            if(Has(Gift.TrueChaos))text+="진정한혼돈: "+string.Join(" · ",trueChaos.Select(g=>Catalog.All[(int)g].title).ToArray())+"\n";return text.TrimEnd();
        }
        void OnDestroy(){
            if(Player==this)Player=null;if(owner==null)return;
            owner.onStartAction-=OnAction;owner.onStartCharging-=ChargeStart;owner.onStopCharging-=ChargeStop;owner.onCancelCharging-=ChargeCancel;
            owner.playerComponents.inventory.weapon.onSwap-=OnSwap;owner.invulnerable.Detach(this);
            if(stats!=null)owner.stat.DetachValues(stats);
            foreach(var pair in dashOriginal)if(pair.Key!=null&&pair.Key.cooldown.time!=null)AccessTools.Property(typeof(Characters.Cooldowns.Time),"maxStack").SetValue(pair.Key.cooldown.time,pair.Value,null);
            WorldRules.Cleanup(this);SkullRules.Cleanup(this);
        }
    }
}
