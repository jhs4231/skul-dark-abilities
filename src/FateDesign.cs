using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Characters;
using UnityEngine;

namespace FanDarkAbilities
{
    internal sealed class FateDesign
    {
        public string title,description;public readonly Dictionary<string,double> values=new Dictionary<string,double>();
        public double this[string key]{get{double n;return values.TryGetValue(key,out n)?n:0;}}
        static readonly Dictionary<string,FateDesign> definitions=new Dictionary<string,FateDesign>();
        static void Add(string skull,string title,string description,string data){
            var d=new FateDesign{title=title,description=description};
            foreach(var part in data.Split(';')){var p=part.Split('=');d.values[p[0]]=double.Parse(p[1],CultureInfo.InvariantCulture);}definitions[skull]=d;
        }
        static FateDesign(){
            Add("Sword","피의 서약","출혈 피해 +180%, 최대 체력 -25%.","bleed=1.8;Health=-.25");
            Add("Ent","뿌리내린 고목","최대 체력 +70%, 이동속도 -35%.","Health=.7;MovementSpeed=-.35");
            Add("Monkey","천군만마","소환물 피해 +100%, 직접 가하는 피해 -25%.","minion=1;direct=-.25");
            Add("Mage","마력 소진","스킬 피해 +120%. 스킬 사용 시 체력 2 소모.","skill=1.2;skillCost=2");
            Add("Slave","쇠사슬의 맹세","방 입장 시 방어막 60. 받는 피해 +30%.","roomShield=60;incoming=.3");
            Add("Mummy","저주받은 불사","40초마다 치명상을 체력 20%로 회피. 최대 체력 -50%.","revive=40;Health=-.5");
            Add("Gargoyle","석상의 날개","공중에서 주는 피해 +100%, 지상에서 -40%.","air=1;ground=-.4");
            Add("Frost","영구동토","빙결 지속시간 +3초. 빙결되지 않은 적에게 주는 피해 -30%.","FreezeDuration=3;unfrozen=-.3");
            Add("Minotaur","광우의 돌진","기절 지속시간 +2초, 스킬 쿨다운 속도 -40%.","StunDuration=2;SkillCooldownSpeed=-.4");
            Add("Hunter","완벽한 사냥","투사체 피해 +100%, 근접 피해 -60%.","projectile=1;melee=-.6");
            Add("Genie","모래의 대가","교대 공격 피해 +160%. 교대 시 체력 5 소모.","swap=1.6;swapCost=5");
            Add("Ghoul","끝없는 폭식","최대 체력 +100%, 받는 회복량 -50%.","Health=1;TakingHealAmount=-.5");
            Add("Rockstar","끝없는 앵콜","아이템 공격 피해 +100%, 스킬 쿨다운 속도 -40%.","item=1;SkillCooldownSpeed=-.4");
            Add("Ninja","금지된 보법","대시 공격 피해 +100%. 대시할 때 체력 1 소모.","dash=1;dashCost=1");
            Add("DarkPaladin","타락한 성역","방어막이 있으면 피해 +100%, 없으면 받는 피해 +50%.","shieldDamage=1;unshieldedIncoming=.5");
            Add("LivingArmor","꺼지지 않는 불씨","스킬 피해 +100%, 받는 회복량 -35%.","skill=1;TakingHealAmount=-.35");
            Add("Prisoner","저주받은 세포","물리·마법 공격력 +120%. 피격 시 골드 10 소모.","PhysicalAttackDamage=1.2;MagicAttackDamage=1.2;hitGold=10");
            Add("Ascetic","폭군의 수련","타격 시 20% 확률로 기절. 받는 피해 +40%.","stun=.2;incoming=.4");
            Add("Reaper","영혼의 거래","적 처치 시 체력 2 회복. 최대 체력 -40%.","killHeal=2;Health=-.4");
            Add("Champion","끝없는 광란","공격속도 +80%, 받는 피해 +50%.","AttackSpeed=.8;incoming=.5");
            Add("Yaksha","나찰의 영역","3미터 안의 적에게 피해 +120%, 멀리 있는 적에게 -40%.","near=1.2;far=-.4");
            Add("Archlich","생명을 태운 파멸","마법 공격력 +150%, 최대 체력 -35%.","MagicAttackDamage=1.5;Health=-.35");
            Add("DavyJones","심해의 포격","스킬 피해 +120%, 이동속도 -35%.","skill=1.2;MovementSpeed=-.35");
            Add("Skul","작은 영웅의 짐","교대 공격 피해 +150%, 최대 체력 -25%.","swap=1.5;Health=-.25");
            Add("Warrior","일격의 집념","집중속도 +200%, 기본공격 속도 -50%.","ChargingSpeed=2;BasicAttackSpeed=-.5");
            Add("Shield","움직이는 성벽","받는 피해 -50%, 가하는 피해 -25%.","incoming=-.5;damage=-.25");
            Add("Spear","멈추지 않는 돌격","대시 공격 피해 +150%, 교대 쿨다운 속도 -50%.","dash=1.5;SwapCooldownSpeed=-.5");
            Add("Thief","목숨값","골드 획득 +200%, 받는 피해 +50%.","gold=2;incoming=.5");
            Add("Werewolf","달빛의 갈증","기본공격 속도 +70%, 최대 체력 -30%.","BasicAttackSpeed=.7;Health=-.3");
            Add("Alchemist","금지된 실험","중독 피해 +150%, 화상 피해 -50%.","poison=1.5;burn=-.5");
            Add("Recruit","고독한 지휘관","소환물 피해 +120%, 기본공격 피해 -40%.","minion=1.2;basic=-.4");
            Add("Bomber","파괴 충동","폭발 피해 +180%, 받는 피해 +50%.","explosion=1.8;incoming=.5");
            Add("Clown","마지막 웃음","치명타 확률 +40%, 치명타 피해 -35%.","CriticalChance=.4;CriticalDamage=-.35");
            Add("Water","마르지 않는 파도","교대 쿨다운 속도 +150%, 스킬 피해 -35%.","SwapCooldownSpeed=1.5;skill=-.35");
            Add("Samurai","발도의 대가","스킬 피해 +180%, 기본공격 피해 -60%.","skill=1.8;basic=-.6");
            Add("Berserker","붉은 분노","잃은 체력 비율당 피해 최대 +180%, 받는 회복량 -50%.","missingHealth=1.8;TakingHealAmount=-.5");
            Add("Rider","유령의 질주","이동속도 +100%, 최대 체력 -40%.","MovementSpeed=1;Health=-.4");
            Add("Predator","사냥의 굴레","적 처치 시 모든 스킬 쿨다운 10% 감소. 받는 피해 +40%.","killCooldown=.1;incoming=.4");
            Add("King","왕관의 무게","스킬 쿨다운 속도 +120%, 이동속도 -30%.","SkillCooldownSpeed=1.2;MovementSpeed=-.3");
            Add("Gambler","목숨을 건 판돈","타격마다 피해 -50~+200% 무작위, 받는 피해 +25%.","gamble=1;incoming=.25");
            Add("Aladdin","욕망의 등불","골드 획득 +150%, 최대 체력 -30%.","gold=1.5;Health=-.3");
            Add("Viking","얼어붙은 맹세","빙결된 적에게 피해 +150%, 빙결되지 않은 적에게 -40%.","frozen=1.5;unfrozen=-.4");
            Add("Warlock","심연의 집중","집중속도 +200%. 스킬 사용 시 체력 3 소모.","ChargingSpeed=2;skillCost=3");
            Add("GraveDigger","무덤지기의 빚","소환물 피해 +140%, 받는 피해 +35%.","minion=1.4;incoming=.35");
            Add("Dominator","지배의 굴레","기본공격 피해 +150%, 스킬 쿨다운 속도 -50%.","basic=1.5;SkillCooldownSpeed=-.5");
            Add("GuanYu","일기당천","스킬 피해 +100%, 치명타 확률 -25%p.","skill=1;CriticalChance=-.25");
            Add("ChiefGuard","최후의 경호","방어막 보유 시 피해 +140%, 없으면 받는 피해 +50%.","shieldDamage=1.4;unshieldedIncoming=.5");
            Add("Dracula","핏빛 만찬","처치 시 체력 5 회복, 최대 체력 -50%.","killHeal=5;Health=-.5");
            Add("PlagueDoctor","죽음의 역병","중독 피해 +200%, 화상 피해 -70%.","poison=2;burn=-.7");
            Add("Harpy","추락의 맹세","공중에서 피해 +150%, 지상에서 -50%.","air=1.5;ground=-.5");
            Add("Slime","끝없는 재생","회복량 +150%, 받는 피해 +60%.","TakingHealAmount=1.5;incoming=.6");
            Add("Balrog","심연의 불꽃","화상 피해 +200%, 회복량 -50%.","burn=2;TakingHealAmount=-.5");
            Add("HeroSkul","부서지는 왕관","가하는 피해 +150%, 최대 체력 -60%.","damage=1.5;Health=-.6");
            Add("Unknown","이름 없는 숙명","물리·마법 공격력 +80%, 받는 피해 +30%.","PhysicalAttackDamage=.8;MagicAttackDamage=.8;incoming=.3");
        }
        public static FateDesign For(EffectHost h){FateDesign d;return definitions.TryGetValue(SkullRules.State(h).family,out d)?d:definitions["Unknown"];}
        public static void Stats(EffectHost h,List<Stat.Value> result){
            if(!h.Has(Gift.Fate))return;var d=For(h);double power=h.Power(Gift.Fate);
            foreach(var p in d.values){var f=typeof(Stat.Kind).GetField(p.Key);if(f==null)continue;var kind=(Stat.Kind)f.GetValue(null);
                bool multiplier=kind==Stat.Kind.Health;bool constant=kind==Stat.Kind.FreezeDuration||kind==Stat.Kind.StunDuration;
                result.Add(new Stat.Value(multiplier?Stat.Category.Percent:constant?Stat.Category.Constant:Stat.Category.PercentPoint,kind,multiplier?Math.Max(.05,1+p.Value*power):p.Value*power));
            }
        }
        public static void Damage(EffectHost h,Character target,ref Damage damage,ref double multiplier){
            if(!h.Has(Gift.Fate))return;var d=For(h);double bonus=d["damage"];
            if(damage.attacker.character!=null&&damage.attacker.character.type==Character.Type.PlayerMinion)bonus+=d["minion"];else bonus+=d["direct"];
            if(damage.motionType==Characters.Damage.MotionType.Basic)bonus+=d["basic"];
            if(damage.motionType==Characters.Damage.MotionType.Skill)bonus+=d["skill"];
            if(damage.motionType==Characters.Damage.MotionType.Dash)bonus+=d["dash"];
            if(damage.motionType==Characters.Damage.MotionType.Item)bonus+=d["item"];
            if(damage.motionType==Characters.Damage.MotionType.Swap)bonus+=d["swap"];
            if(damage.attackType==Characters.Damage.AttackType.Projectile)bonus+=d["projectile"];
            if(damage.attackType==Characters.Damage.AttackType.Melee)bonus+=d["melee"];
            if(damage.key==CharacterStatus.AttackKeyBleed)bonus+=d["bleed"];
            if(damage.key==CharacterStatus.AttackKeyPoison)bonus+=d["poison"];
            if(damage.key==CharacterStatus.AttackKeyBurn||damage.key==CharacterStatus.AttackKeyEmber)bonus+=d["burn"];
            if(ExplosionRules.Named(damage.key)||ExplosionRules.depth>0)bonus+=d["explosion"];
            bonus+=h.owner.movement.isGrounded?d["ground"]:d["air"];
            bonus+=(1-h.owner.health.percent)*d["missingHealth"];
            if(h.owner.health.shield.amount>0)bonus+=d["shieldDamage"];
            if(target!=null){bonus+=target.status!=null&&target.status.freezed?d["frozen"]:d["unfrozen"];bonus+=Vector3.Distance(target.transform.position,h.owner.transform.position)<3?d["near"]:d["far"];}
            if(d["gamble"]>0)bonus+=UnityEngine.Random.Range(-.5f,2f);
            multiplier*=Math.Max(0,1+bonus*h.Power(Gift.Fate));
        }
    }
}
