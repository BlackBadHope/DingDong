using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
namespace DingDongBiomeHomes.Compatibility;
public sealed class ProgressionTooltip : GlobalItem
{
 internal static bool Russian => Language.ActiveCulture.Name.StartsWith("ru", StringComparison.OrdinalIgnoreCase);
 public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
 {
  if (!SignatureBalanceSystem.Available || !SignatureBalanceSystem.HasState(item)) return;
  bool ru = Russian;
  // The provider and compatibility layer described the same persisted level twice.
  // Accessories do not need weapon mastery, projectile or XP-rate instructions.
  int anchor = tooltips.FindIndex(t => t.Mod == "SignatureEquipment");
  tooltips.RemoveAll(t => t.Mod == "SignatureEquipment");
  if (item.accessory || (item.damage <= 0 && item.defense <= 0)) return;
  int raw = SignatureBalanceSystem.RawLevel(item), cap = SignatureBalanceSystem.Stage;
  int active = SignatureBalanceRules.CombatLevel(raw, cap);
  string text = ru ? $"Signature: ур. {raw} · {SignatureBalanceSystem.Experience(item)}/{SignatureBalanceSystem.ExperienceNeeded(item)} XP" :
   $"Signature: Lv. {raw} · {SignatureBalanceSystem.Experience(item)}/{SignatureBalanceSystem.ExperienceNeeded(item)} XP";
  if (raw > cap) text += ru ? $" · боевой ур. {active}" : $" · combat Lv. {active}";
  tooltips.Insert(Math.Clamp(anchor < 0 ? 2 : anchor, 0, tooltips.Count), new TooltipLine(Mod, "SignatureProgress", text) { OverrideColor = Color.Gold });
 }
 internal static IEnumerable<DamageGuideBlock> Details(Item item)
 {
  bool ru = Russian;
  if (SignatureBalanceSystem.Available && SignatureBalanceSystem.HasState(item) && !item.accessory && (item.damage > 0 || item.defense > 0)) {
   int raw = SignatureBalanceSystem.RawLevel(item), cap = SignatureBalanceSystem.Stage, active = SignatureBalanceRules.CombatLevel(raw, cap);
   string summary = ru ? $"Уровень предмета: {raw}. Опыт: {SignatureBalanceSystem.Experience(item)}/{SignatureBalanceSystem.ExperienceNeeded(item)}. Действующий уровень бонусов: {active}/{cap}." :
    $"Item level: {raw}. XP: {SignatureBalanceSystem.Experience(item)}/{SignatureBalanceSystem.ExperienceNeeded(item)}. Effective bonus level: {active}/{cap}.";
   summary += ru ? " Предел зависит от боссов: 20 / 40 / 60 / 80 / 100. Накопленный уровень не теряется." :
    " Boss progression sets the ceiling: 20 / 40 / 60 / 80 / 100. Earned levels are retained.";
   yield return new DamageGuideBlock("Signature", summary, DamageGuideTone.Information);
   if (item.damage > 0) {
    yield return new DamageGuideBlock(ru ? "Бонусы мастерства" : "Mastery bonuses", ru ?
     $"Урон +{active * .5f:0.#}%; крит +{active * .1f:0.#} п.п.; время атаки −{active * .13f:0.#}%. Эти числа относятся только к Signature." :
     $"Damage +{active * .5f:0.#}%; crit +{active * .1f:0.#} pp; attack time −{active * .13f:0.#}%. These bonuses are from Signature only.");
    double rate = SignatureExperienceRules.PerSecond(raw, cap, item.DamageType.CountsAsClass(DamageClass.Melee));
    yield return new DamageGuideBlock(ru ? "Получение опыта" : "Earning XP", ru ?
     $"До {rate:0.##} XP/с активного боя. Все снаряды одного предмета делят общий бюджет. Ближний бой и низкие уровни получают ускорение; прогресс мира и высокие уровни замедляют рост. Отдельного бонуса за убийство нет." :
     $"Up to {rate:0.##} XP/s of active combat, shared by every projectile of the same item. Melee and early levels grow faster; world progression and high levels slow growth. No separate kill XP.");
    yield return new DamageGuideBlock(ru ? "Количество и свойства снарядов" : "Projectile count and properties", ru ?
     $"Количество дополнительных снарядов продолжает расти. Пробивание и время жизни ограничены ×2 исходного. Цена миньона в слотах: ×{SignatureBalanceRules.MinionCost(1, raw):G4}; скидка продолжает расти." :
     $"Extra projectile count keeps growing. Penetration and lifetime are limited to ×2 original. Minion slot cost: ×{SignatureBalanceRules.MinionCost(1, raw):G4}; the discount keeps growing.");
   } else yield return new DamageGuideBlock(ru ? "Защита Signature" : "Signature defence", ru ?
    $"Прибавка к защите этого предмета: {SignatureBalanceRules.Defence(item.defense, raw, cap)}." :
    $"Additional defence from this item's mastery: {SignatureBalanceRules.Defence(item.defense, raw, cap)}.");
  }
  if (ModLoader.HasMod("WeaponEnchantments")) yield return new DamageGuideBlock(ru ? "Совместимость с зачарованиями" : "Enchantment compatibility", ru ?
   "Signature — мастерство предмета. Уровень Weapon Enchantments — ёмкость чар, инфузия WE — сила основы. Это разные системы с разным опытом: их уровни нельзя честно сложить в один. Урон в подсказке уже включает эффекты предмета и экипировки; предел Signature не ограничивает бонусы остальных модов." :
   "Signature tracks item mastery. Weapon Enchantments level grants enchantment capacity; WE infusion sets base power. Their XP pools and levels mean different things and cannot be added into one meaningful level. The displayed damage includes item and equipment effects; Signature's ceiling does not cap other mods.");
  if (item.ModItem?.FullName == "FargowiltasSouls/EternitySoul") yield return new DamageGuideBlock(ru ? "Душа вечности: собственные эффекты Fargo" : "Soul of Eternity: Fargo effects", ru ?
   "Финальный аксессуар: +250% общего урона, ×5 максимального здоровья, +20 миньонов и +10 турелей. Tin накапливает по +5% урона и +5 защиты до +4750% и +4750 защиты; получение урона сбрасывает накопление. При 100% TinCrit лечение равно 10% урона удара с перезарядкой 1 тик и проверкой запрета лечения. Это отдельные эффекты Fargo, а не бесконечный уровень Signature. Дополнительные эффекты можно отключать через меню Fargo." :
   "Final accessory: +250% generic damage, ×5 max life, +20 minions and +10 sentries. Tin builds +5% damage and +5 defence per proc, capped at +4750% and +4750 defence; taking damage resets it. At 100% TinCrit, healing restores 10% of hit damage with a 1-tick cooldown and anti-heal checks. These are Fargo effects, not infinite Signature mastery. Additional effects can be toggled in Fargo's menu.", DamageGuideTone.Warning);
 }
}
