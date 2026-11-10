using System;
using System.Linq;
using System.Collections.Generic;
using Characters.Gear.Upgrades;
using UnityEngine;

namespace FanDarkAbilities
{
    public enum Gift
    {
        BigBang, Vampire, Intimidation, Harmony, MilkyWay, Defense, Panacea, Enchant,
        Scarecrow, ShadowDodge, Brawler, Judgment, Conductor, Disaster, Regeneration,
        Legion, PlateArmor, Storm, Penance, Cold, Swamp, Wind, Raid, Necromancy,
        Marksman, Destiny, Chaos,
        Doppelganger, Deadweight, Decay, Secular, Osteoporosis, Spider, DeadChest,
        Tortoise, Fission, InternalBleeding, Necrosis, Fever, Frostbite, Coma,
        Humility, Void, Ossification, Genetic, RedCrown, Fate, TimeExchange,
        Abyss, Guillotine, WyvernTail, WyvernHead, BlackHeart, BlackSun, Wyvern, TrueChaos, WyvernReserve
    }
    public sealed class Definition
    {
        public Gift gift; public string title, description; public int levels, price;
        public bool removable = true;
        public bool cursed { get { return gift >= Gift.Doppelganger; } }
        public string id { get { return gift == Gift.Regeneration ? Plugin.Id : "Fan" + gift; } }
        public UpgradeResource.Reference Reference()
        {
            var icon = Catalog.Icon((int)gift, cursed);
            return new UpgradeResource.Reference {
                name = id, guid = id, icon = icon, thumbnail = icon,
                prices = Enumerable.Repeat(price, levels).ToArray(), maxLevel = levels,
                type = cursed ? UpgradeObject.Type.Cursed : UpgradeObject.Type.Normal,
                needUnlock = gift == Gift.WyvernReserve, removable = removable, orderInShop = -100 + (int)gift,
                valuesForDescription = new UpgradeObject.ValueForDescription[0]
            };
        }
    }
    public static class Catalog
    {
        public static readonly List<Definition> All = new List<Definition>();
        public static readonly Dictionary<string, Definition> ById = new Dictionary<string, Definition>();
        static void Add(Gift g, string name, int levels, int price, string desc, bool removable)
        {
            var d = new Definition { gift=g, title=name, levels=levels, price=price, description=desc, removable=removable };
            All.Add(d); ById.Add(d.id,d);
        }
        static void N(Gift g, string n, int l, int p, string d) { Add(g,n,l,p,d,true); }
        static void C(Gift g, string n, string d) { Add(g,n,1,1,d,true); }
        static Catalog()
        {
            N(Gift.BigBang,"대폭발",3,1,"폭발 피해와 범위가 25/50/75% 증폭됩니다. 재사용 대기시간: 1초.");
            N(Gift.Vampire,"흡혈",1,3,"최대 체력의 5% 이하를 회복하면 체력을 5 추가 회복합니다.");
            N(Gift.Intimidation,"위압",1,3,"현재 스컬의 키에 비례해 가하는 피해가 최대 15% 증폭됩니다.");
            N(Gift.Harmony,"조화",3,1,"물리·마법 공격력 차이가 100%p 이하면 피해가 20/40/60% 증폭됩니다.");
            N(Gift.MilkyWay,"은하수",3,1,"정수 쿨다운 속도 +25/50/75%. 정수 타격 피해에 비례해 공격력이 최대 100/150/200% 증가합니다. 정수가 다시 준비되면 초기화됩니다.");
            N(Gift.Defense,"수비",2,2,"공격을 방어하면 5초간 물리·마법 공격력이 15/25% 증폭됩니다. 최대 3중첩.");
            N(Gift.Panacea,"만병통치약",1,3,"넉백, 함정 피해, 기절에 면역이 됩니다.");
            N(Gift.Enchant,"마법부여",2,2,"아이템 효과의 쿨다운이 25/50% 감소하고 지속시간이 25/50% 증가합니다.");
            N(Gift.Scarecrow,"허수아비",1,3,"미클리어 방에서 3초마다 허수아비를 소환합니다(최대 6). 타격 시 10초간 물리·마법 공격력 +20%, 최대 200%.");
            N(Gift.ShadowDodge,"그림자회피",3,1,"피격 시 25/30/35% 확률로 회피합니다. 발동 후 1/2/3초간 공격이 반드시 치명타로 적중합니다.");
            N(Gift.Brawler,"난동꾼",2,2,"내려찍기로 지상 적에게 기절 충격파를 발생시킵니다. 쿨다운 8/5초. 높은 공격력에 비례합니다.");
            N(Gift.Judgment,"심판",2,2,"이동속도 +40/80%. 2/1초마다 주변에 번개가 떨어집니다. 이동할수록 번개 수가 늘고 간격이 줄어듭니다.");
            N(Gift.Conductor,"지휘자",3,1,"집중속도 +20/40/60%. 집중 완료 시 5초간 적을 추적하는 구체를 소환합니다.");
            N(Gift.Disaster,"재앙",3,1,"화상 부여 시 화염구 3개를 소환합니다. 쿨다운 4초. 착탄 지점에 2/3/4초간 화염을 남깁니다.");
            N(Gift.Regeneration,"재생",2,2,"방어막 손실의 50/75%를 천천히 재생합니다. 완전히 소진되면 중단됩니다. 방어막 보유 시 물리·마법 공격력 +75/150%.");
            N(Gift.Legion,"군단",3,1,"소환물이 주는 피해가 15/30/45% 증폭됩니다.");
            N(Gift.PlateArmor,"판금갑옷",2,2,"물리·마법 공격력 +100/200%. 방마다 첫 2/3회 피격은 피해가 50% 감소하고 남은 공격력 보너스가 절반이 됩니다.");
            N(Gift.Storm,"폭풍",3,1,"교대 쿨다운 속도 +20/40/60%. 교대 시 지면을 돌아다니는 폭풍을 4/5/6초간 소환합니다.");
            N(Gift.Penance,"고행",1,3,"10초 내 3회 피격 시 기절 충격파. 맞은 적이 받는 피해가 영구적으로 15% 증가합니다(최대 3중첩).");
            N(Gift.Cold,"혹한",3,1,"빙결 시 적의 공격을 취소합니다. 해동 시 주변에 냉기 피해를 줍니다. 단계에 따라 피해가 증가합니다.");
            N(Gift.Swamp,"늪지대",3,1,"중독 부여 시 8초간 늪을 생성합니다(쿨다운 8초). 늪 위 적이 받는 중독 피해 +25/50/75%.");
            N(Gift.Wind,"칼바람",3,1,"기본공격 시 무적·방어막을 관통하는 바람을 발사합니다. 10/15/20% 확률로 상처를 부여합니다.");
            N(Gift.Raid,"습격",3,1,"대시 횟수 +1. 대시 시 30/40/50% 확률로 쿨다운을 초기화합니다. 대시 피해 +25/50/75%.");
            N(Gift.Necromancy,"강령술",2,2,"처치마다 물리·마법 공격력 +10/20%(최대 10중첩), 방마다 초기화. 처치 시 추적 유령을 2/3개 소환합니다.");
            N(Gift.Marksman,"백발백중",3,1,"투사체 피해가 15/30/45% 증폭됩니다.");
            N(Gift.Destiny,"운명",1,3,"현재 스컬의 고유한 운명이 발현됩니다. 스컬별 세부 효과는 설계 기록을 참고하세요.");
            N(Gift.Chaos,"혼돈",1,3,"방마다 무작위 일반 능력 6개를 최대 단계로 적용합니다. 5초마다 물리·마법 공격력이 50~150% 증가합니다.");
            Add(Gift.Doppelganger,"도플갱어",1,1,"대기 스컬을 현재 스컬의 복제로 바꿉니다. 최대 체력 -50%. 제거할 수 없습니다.",false);
            C(Gift.Deadweight,"사하중","빈 아이템 슬롯마다 가하는 피해 +50%.");
            C(Gift.Decay,"부패","현재 스컬이 레어이면 피해 +120%, 유니크 +60%, 레전더리 -50%.");
            C(Gift.Secular,"세속","골드·마석 획득 +100%. 뼈 획득과 체력 회복 -50%.");
            C(Gift.Osteoporosis,"골다공증","죽음을 피할 때마다 가하는 피해와 받는 피해 +15%. 영구 적용, 최대 75%.");
            C(Gift.Spider,"거미몰림","각성의 뼈 비용 +80%. 각성할 때마다 물리·마법 공격력 +80%.");
            C(Gift.DeadChest,"망자의함","상자가 모두 흉조 상자로 바뀌고 흉조 아이템이 중복 등장합니다. 아이템 방은 66% 확률로 클리어 상자가 없습니다.");
            C(Gift.Tortoise,"거북의갑주","집중속도 -70%. 집중 1초마다 피해 +20%(최대 100%), 집중 종료 후 4초간 유지.");
            Add(Gift.Fission,"핵분열",1,1,"가하는 피해 +200%. 남은 제한시간 안에 게임을 클리어해야 합니다(최초 10분). 제거할 수 없습니다.",false);
            C(Gift.InternalBleeding,"내출혈","출혈 피해 +120%. 출혈 부여 시 33% 확률로 체력 1 소모. 출혈 처치 시 체력 1 회복.");
            C(Gift.Necrosis,"괴사","중독이 최대 3회 중첩됩니다. 중독된 적에게서 받는 피해 +60%.");
            C(Gift.Fever,"고열","화상 피해 간격 -90%. 화상에 걸린 적 사망 시 폭발하여 주변에 화상을 부여합니다.");
            C(Gift.Frostbite,"동상","가하는 피해 -15%. 빙결된 적에게 주는 피해 +100%. 공격으로 빙결이 풀리지 않습니다.");
            C(Gift.Coma,"혼수","기절 지속시간 2배. 기절 시간에 비례해 주는 피해가 최대 70% 증가합니다. 기절 부여 시 50% 확률로 즉시 해제.");
            C(Gift.Humility,"겸손","아이템 효과 -30%. 각인 효과 +100%.");
            C(Gift.Void,"공허","아이템 분해 시 골드를 얻지 않습니다. 분해마다 공격력 +10%(최대 300%). 분해한 아이템은 다시 등장하지 않습니다.");
            C(Gift.Ossification,"골화증","가하는 피해 +70%. 아이템 공격이 피해를 주지 않습니다.");
            Add(Gift.Genetic,"유전자조작",1,1,"모든 스컬의 오른쪽 스킬을 왼쪽 스킬로 복제합니다. 스킬 쿨다운 속도 -30%. 제거할 수 없습니다.",false);
            C(Gift.RedCrown,"붉은왕관","구출한 NPC를 희생시키고 보상을 받지 않습니다. 희생마다 물리·마법 공격력 +66%, 최대 6회.");
            C(Gift.Fate,"숙명","현재 스컬에 따른 힘과 대가를 얻습니다. 스컬별 세부 효과는 설계 기록을 참고하세요.");
            C(Gift.TimeExchange,"시간교환","적의 시간이 40% 느려집니다. 교대·스킬 쿨다운 속도 -40%.");
            C(Gift.Abyss,"심연의부름","스킬 10회 사용마다 거대 촉수로 주변 적을 10초간 묶고 피해를 줍니다. 10초간 스킬을 사용하지 않으면 체력 10 소모.");
            C(Gift.Guillotine,"목을쳐라","교대 쿨다운 제거. 교대 시 체력 7 소모. 교대 후 2초간 피해 +50%.");
            C(Gift.WyvernTail,"와이번의꼬리","최대 체력 -25%. 와이번의머리와 함께 보유하면 와이번으로 합성됩니다.");
            C(Gift.WyvernHead,"와이번의머리","받는 피해 +25%. 와이번의꼬리와 함께 보유하면 와이번으로 합성됩니다.");
            C(Gift.BlackHeart,"검은심장","맵마다 최소 한 마리의 어둠 적이 등장합니다. 처치 시 체력 30 회복, 최대 체력 +10.");
            C(Gift.BlackSun,"검은태양","다른 저주 효과는 2배, 일반 능력의 효과는 절반이 됩니다.");
            C(Gift.Wyvern,"와이번","두 슬롯 사용. 치명상 시 최대 체력 10% 회복하고 10초간 와이번으로 변신합니다(쿨다운20초). 변신 중 무적·지면 화염·적 공격력150% 반격.");
            C(Gift.TrueChaos,"진정한혼돈","방마다 무작위 저주 6개를 적용합니다. 5초마다 가하는 피해가 -15~+50% 변합니다.");
            C(Gift.WyvernReserve,"와이번 · 두 번째 슬롯","와이번이 사용하는 두 번째 슬롯입니다. 와이번 제거 시 함께 비워집니다.");
        }
        public static Definition Find(string id) { Definition d; return id!=null && ById.TryGetValue(id,out d) ? d : null; }
        static readonly Dictionary<int,Sprite> Icons = new Dictionary<int,Sprite>();
        public static Sprite Icon(int index, bool cursed)
        {
            Sprite found; if(Icons.TryGetValue(index,out found)) return found;
            var tex = new Texture2D(32,32); tex.filterMode=FilterMode.Point;
            var pixels = PixelIcon.Draw(index,cursed);
            tex.SetPixels(pixels);tex.Apply(); UnityEngine.Object.DontDestroyOnLoad(tex);
            found=Sprite.Create(tex,new Rect(0,0,32,32),new Vector2(.5f,.5f),32);
            UnityEngine.Object.DontDestroyOnLoad(found);Icons[index]=found;return found;
        }
    }
}
