using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using Characters;
using Characters.Gear.Upgrades;
using HarmonyLib;
using UnityEngine;

namespace FanDarkAbilities
{
    [BepInPlugin("local.skul.fan-dark-abilities", "Fan Dark Abilities", "0.2.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal const string Id = "FanRegeneration";
        internal static ConfigEntry<float> Rate;
        Harmony harmony;
        ConfigEntry<bool> verification;
        void Awake()
        {
            Rate = Config.Bind("Regeneration", "ShieldPerSecond", 5f,
                new ConfigDescription("Original post specifies slow recovery without a rate.", new AcceptableValueRange<float>(0.1f, 100f)));
            verification = Config.Bind("Development", "VerificationHotkeys", false, "Development only: F6 purchase/upgrade/save (changes progress); F7 numerical checks; F8 open dark ability shop.");
            harmony = new Harmony("local.skul.fan-dark-abilities");
            try { harmony.PatchAll(); } catch (Exception error) { harmony.UnpatchSelf(); Logger.LogError("Patch registration failed: " + error); throw; }
            Logger.LogInfo("Fan abilities 0.2.0 development build loaded.");
        }
        void OnDestroy() { if (harmony != null) harmony.UnpatchSelf(); }
        void Update()
        {
            if (!verification.Value) return;
            try
            {
                if (Input.GetKeyDown(KeyCode.F8))
                {
                    var panel = Scenes.Scene<Scenes.GameBase>.instance.uiManager.upgradeShop;
                    Singletons.Singleton<UpgradeShop>.Instance.LoadCusredLineUp();
                    panel.Open();
                    var element = panel.GetComponentsInChildren<UI.Upgrades.UpgradeElement>().FirstOrDefault(e => e.reference.name == Id);
                    if (element != null) {
                        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(element.selectable.gameObject);
                        element.selectable.Select();
                    }
                }
                if (Input.GetKeyDown(KeyCode.F7)) Verify();
                if (Input.GetKeyDown(KeyCode.F9)) ExpansionVerification.Run();
                if (Input.GetKeyDown(KeyCode.F10)) StartCoroutine(IntegrationVerification.Run());
                if (Input.GetKeyDown(KeyCode.F11)) IntegrationVerification.NextMap();
                if (Input.GetKeyDown(KeyCode.F12)) IntegrationVerification.SaveProbe();
                if (Input.GetKeyDown(KeyCode.F6)) VerifySave();
            }
            catch (Exception e) { Logger.LogError("VERIFICATION ERROR: " + e); }
        }
        void Check(string name, double actual, double expected)
        {
            if (Math.Abs(actual - expected) > 0.001) throw new Exception(name + ": expected " + expected + ", got " + actual);
            Logger.LogInfo("PASS " + name + " = " + actual);
        }
        void VerifySave()
        {
            var manager = Singletons.Singleton<Services.Service>.Instance.levelManager;
            var reference = UpgradeResource.instance.upgradeReferences.Single(r => r.name == Id);
            var shop = Singletons.Singleton<UpgradeShop>.Instance;
            var currency = Data.GameData.Currency.heartQuartz;
            // Only enabled during verification, with a complete pre-test save backup.
            currency.balance = Math.Max(currency.balance, 4);
            if (!manager.player.playerComponents.inventory.upgrade.Has(reference))
            {
                if (!shop.TryUpgrade(reference)) throw new Exception("Purchase failed");
                Logger.LogInfo("PASS actual shop purchase");
            }
            if (reference.GetCurrentLevel() < 2)
            {
                if (!shop.TryLevelUp(reference)) throw new Exception("Level-up failed");
            }
            Check("shop level-up", reference.GetCurrentLevel(), 2);
            manager.Save();
            Data.GameData.Currency.SaveAll();
            Singletons.PersistentSingleton<Platforms.PlatformManager>.Instance.SaveDataToFile();
            Logger.LogInfo("PASS game save completed at Regeneration level 2; restart to verify restoration");
        }
        void Verify()
        {
            var player = Singletons.Singleton<Services.Service>.Instance.levelManager.player;
            if (player == null) throw new Exception("No player");
            if (player.playerComponents.inventory.upgrade.upgrades.Any(u => u != null && Own(u)))
                throw new Exception("Remove equipped Regeneration before running isolated numerical checks.");
            var reference = UpgradeResource.instance.upgradeReferences.Single(r => r.name == Id);
            Logger.LogInfo("PASS registered reference: " + reference.displayName);
            var shield = player.health.shield;
            var entries = (List<Shield.Instance>)AccessTools.Field(typeof(Shield), "_shields").GetValue(shield);
            var saved = entries.ToArray();
            var amounts = saved.Select(s => s.amount).ToArray();
            var originalRate = Rate.Value;
            var baseline = player.stat.GetPercentPoint(Stat.Kind.PhysicalAttackDamage);
            var baselineMagic = player.stat.GetPercentPoint(Stat.Kind.MagicAttackDamage);
            UpgradeObject obj = null;
            try
            {
                entries.Clear();
                obj = reference.Instantiate();
                obj.Attach(player);
                Check("max level", obj.maxLevel, 2);
                var effect = obj.GetComponent<Regeneration>();
                Rate.Value = 5;
                shield.Add(new object(), 100);
                effect.Tick(0); player.stat.Update();
                Check("L1 physical bonus", player.stat.GetPercentPoint(Stat.Kind.PhysicalAttackDamage) - baseline, 0.75);
                Check("L1 magic bonus", player.stat.GetPercentPoint(Stat.Kind.MagicAttackDamage) - baselineMagic, 0.75);
                shield.Consume(20);
                effect.Tick(2);
                Check("L1 shield 100 - 20 + 10", shield.amount, 90);
                obj.SetLevel(2);
                effect.Tick(0); player.stat.Update();
                Check("L2 physical bonus", player.stat.GetPercentPoint(Stat.Kind.PhysicalAttackDamage) - baseline, 1.5);
                Check("L2 magic bonus", player.stat.GetPercentPoint(Stat.Kind.MagicAttackDamage) - baselineMagic, 1.5);
                entries.Clear();
                shield.Add(new object(), 100);
                shield.Consume(20);
                effect.Tick(3);
                Check("L2 shield 100 - 20 + 15", shield.amount, 95);
                shield.Consume(1000);
                effect.Tick(10); player.stat.Update();
                Check("full depletion cancels recovery", shield.amount, 0);
                Check("no shield no physical bonus", player.stat.GetPercentPoint(Stat.Kind.PhysicalAttackDamage) - baseline, 0);
                obj.Detach(); player.stat.Update();
                Check("detach removes bonus", player.stat.GetPercentPoint(Stat.Kind.MagicAttackDamage) - baselineMagic, 0);
                Logger.LogInfo("VERIFICATION ALL NUMERICAL CHECKS PASSED");
            }
            finally
            {
                if (obj != null) { obj.Detach(); Destroy(obj.gameObject); }
                entries.Clear(); entries.AddRange(saved);
                for (int i = 0; i < saved.Length; i++) saved[i].amount = amounts[i];
                Rate.Value = originalRate;
                Logger.LogInfo("VERIFICATION player shields/stat restored; inventory and currency untouched");
            }
        }
        internal static bool Own(UpgradeObject obj) { return obj.reference != null && Catalog.Find(obj.reference.name) != null; }
        internal static Sprite Icon()
        {
            var texture = new Texture2D(32, 32);
            texture.filterMode = FilterMode.Point;
            var pixels = new Color[1024];
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                bool shield = y > 4 && y < 27 && Math.Abs(x - 15) < Math.Min(12, y / 2 + 3);
                if (shield) pixels[y * 32 + x] = (Math.Abs(x - 15) <= 2 || (y >= 14 && y <= 18))
                    ? new Color(0.85f, 0.95f, 1f) : new Color(0.5f, 0.1f, 0.9f);
            }
            texture.SetPixels(pixels); texture.Apply();
            DontDestroyOnLoad(texture);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
            DontDestroyOnLoad(sprite); return sprite;
        }
        internal static void Set(object obj, string field, object value) { AccessTools.Field(obj.GetType(), field).SetValue(obj, value); }
        internal static UpgradeObject Create(UpgradeResource.Reference reference)
        {
            var go = new GameObject(reference.name);
            var obj = go.AddComponent<UpgradeObject>();
            if (reference.name == Id) go.AddComponent<Regeneration>();
            else go.AddComponent<GiftAbility>();
            obj.reference = reference;
            Set(obj, "_prices", reference.prices);
            Set(obj, "_removable", reference.removable);
            Set(obj, "_type", reference.type);
            Set(obj, "_valuesByLevel", reference.valuesForDescription);
            return obj;
        }
    }

    public class Regeneration : UpgradeAbility
    {
        internal static readonly Dictionary<Shield, Regeneration> Owners = new Dictionary<Shield, Regeneration>();
        Character owner;
        Stat.Values bonus;
        double pending;
        readonly object shieldKey = new object();
        public override void Attach(Character target)
        {
            Detach();
            owner = target;
            Owners[target.health.shield] = this;
            EffectHost.Ensure(target);
        }
        public override void Detach()
        {
            if (owner != null)
            {
                Regeneration current; if(Owners.TryGetValue(owner.health.shield,out current)&&current==this)Owners.Remove(owner.health.shield);
                if (bonus != null) owner.stat.DetachValues(bonus);
            }
            bonus = null; owner = null; pending = 0;
        }
        internal void Lost(double before, double after)
        {
            if (after <= 0) { pending = 0; return; }
            var level = EffectLevel();
            pending += Math.Max(0, before - after) * (level >= 2 ? 0.75 : 0.5) * Strength();
        }
        int EffectLevel() { var h=EffectHost.Player; return Math.Max(GetComponent<UpgradeObject>().level,h==null?0:h.Level(Gift.Regeneration)); }
        double Strength() { var h=EffectHost.Player; return h!=null&&h.Has(Gift.Regeneration)?h.Power(Gift.Regeneration):1; }
        internal void RefreshBonus()
        {
            if (owner != null && bonus != null) owner.stat.DetachValues(bonus);
            bonus = null;
        }
        void Update() { Tick(Time.deltaTime); }
        internal void Tick(float deltaTime)
        {
            if (owner == null) return;
            var shield = owner.health.shield;
            if (shield.amount <= 0)
            {
                pending = 0;
                if (bonus != null) { owner.stat.DetachValues(bonus); bonus = null; }
                return;
            }
            if (bonus == null)
            {
                double amount = (EffectLevel() >= 2 ? 1.5 : 0.75) * Strength();
                bonus = new Stat.Values(
                    new Stat.Value(Stat.Category.PercentPoint, Stat.Kind.PhysicalAttackDamage, amount),
                    new Stat.Value(Stat.Category.PercentPoint, Stat.Kind.MagicAttackDamage, amount));
                owner.stat.AttachValues(bonus);
            }
            double recovery = Math.Min(pending, Plugin.Rate.Value * deltaTime);
            if (recovery > 0)
            {
                // Keep the recovered shield separate from the original source's lifetime.
                var fields = AccessTools.Field(typeof(Shield), "_shields");
                var entries = (List<Shield.Instance>)fields.GetValue(shield);
                var existing = entries.FirstOrDefault(s => s.key == shieldKey);
                if (existing == null) shield.Add(shieldKey, (float)recovery);
                else existing.amount += recovery;
                pending -= recovery;
            }
        }
        void OnDestroy() { Detach(); }
    }

    [HarmonyPatch(typeof(UpgradeResource), "Initialize")]
    static class Register
    {
        static void Postfix(UpgradeResource __instance)
        {
            var refs = __instance.upgradeReferences;
            if (refs == null) return;
            foreach (var definition in Catalog.All)
                if (!refs.Any(r => r.name == definition.id)) refs.Add(definition.Reference());
        }
    }
    [HarmonyPatch(typeof(UpgradeResource.Reference), "Instantiate")]
    static class Instantiate
    {
        static bool Prefix(UpgradeResource.Reference __instance, ref UpgradeObject __result)
        {
            if (Catalog.Find(__instance.name) == null) return true;
            __result = Plugin.Create(__instance); return false;
        }
    }
    [HarmonyPatch(typeof(UpgradeManager), "FindByName")]
    static class SavedReference
    {
        static void Postfix(string name, ref UpgradeResource.Reference __result)
        {
            if (Catalog.Find(name) != null && __result == null)
                __result = UpgradeResource.instance.upgradeReferences.FirstOrDefault(r => r.name == name);
        }
    }
    [HarmonyPatch(typeof(UpgradeResource.Reference), "get_displayName")]
    static class Name { static bool Prefix(UpgradeResource.Reference __instance, ref string __result) {
        if (Catalog.Find(__instance.name) == null) return true; __result = Catalog.Find(__instance.name).title; return false; } }
    [HarmonyPatch(typeof(UpgradeResource.Reference), "get_description")]
    static class Description { static bool Prefix(UpgradeResource.Reference __instance, ref string __result) {
        if (Catalog.Find(__instance.name) == null) return true;
        __result = Catalog.Find(__instance.name).description;
        var h=EffectHost.Player;if(h!=null&&__instance.name=="FanFate"){var f=FateDesign.For(h);__result+="\n\n"+f.title+"\n"+f.description;} return false; } }
    [HarmonyPatch(typeof(UpgradeResource.Reference), "get_flavor")]
    static class Flavor { static bool Prefix(UpgradeResource.Reference __instance, ref string __result) {
        if (Catalog.Find(__instance.name) == null) return true; __result = "LDS_의 검은능력 구상 · 팬 모드"; return false; } }
    [HarmonyPatch(typeof(UpgradeObject), "get_maxLevel")]
    static class MaxLevel { static bool Prefix(UpgradeObject __instance, ref int __result) {
        if (!Plugin.Own(__instance)) return true; __result = Catalog.Find(__instance.reference.name).levels; return false; } }
    [HarmonyPatch(typeof(UpgradeObject), "Attach")]
    static class Attach { static bool Prefix(UpgradeObject __instance, Character target) {
        if (!Plugin.Own(__instance)) return true;
        Plugin.Set(__instance, "_level", 1);
        var regen = __instance.GetComponent<Regeneration>();
        if (regen != null) regen.Attach(target); else __instance.GetComponent<GiftAbility>().Attach(target);
        return false; } }
    [HarmonyPatch(typeof(UpgradeObject), "Detach")]
    static class Detach { static bool Prefix(UpgradeObject __instance) {
        if (!Plugin.Own(__instance)) return true;
        var effect = __instance.GetComponent<Regeneration>(); if (effect != null) effect.Detach();
        var gift = __instance.GetComponent<GiftAbility>(); if(gift != null) gift.Detach(); return false; } }
    [HarmonyPatch(typeof(UpgradeObject), "SetLevel")]
    static class SetLevel { static bool Prefix(UpgradeObject __instance, int level) {
        if (!Plugin.Own(__instance)) return true;
        Plugin.Set(__instance, "_level", Mathf.Clamp(level, 1, Catalog.Find(__instance.reference.name).levels));
        var regen = __instance.GetComponent<Regeneration>(); if(regen != null) regen.RefreshBonus();
        var gift = __instance.GetComponent<GiftAbility>(); if(gift != null) gift.Refresh(); return false; } }
    [HarmonyPatch(typeof(UpgradeObject), "LevelUp")]
    static class LevelUp { static bool Prefix(UpgradeObject __instance) {
        if (!Plugin.Own(__instance)) return true;
        __instance.SetLevel(__instance.level + 1); return false; } }
    [HarmonyPatch(typeof(UpgradeObject), "GetCurrentAbility")]
    static class CurrentAbility { static bool Prefix(UpgradeObject __instance, ref UpgradeAbility __result) {
        if (!Plugin.Own(__instance)) return true;
        __result = __instance.GetComponent<UpgradeAbility>(); return false; } }
    [HarmonyPatch(typeof(Shield), "Consume")]
    static class ShieldDamage
    {
        static void Prefix(Shield __instance, out double __state) { __state = __instance.amount; }
        static void Postfix(Shield __instance, double __state) {
            Regeneration effect;
            if (Regeneration.Owners.TryGetValue(__instance, out effect)) effect.Lost(__state, __instance.amount);
        }
    }
}
