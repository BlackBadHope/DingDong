using System.Linq;
using Terraria.UI;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
internal sealed partial class AccessoryEnchantingUiState
{
    private void GuideRow(float width, string heading, string content, float height = 92, AccessoryEnchantmentId id = AccessoryEnchantmentId.None)
    {
        var row = new UIElement(); row.Width.Set(width, 0); row.Height.Set(height + 8, 0);
        var card = Card(row, 0, 0, width, height);
        float inset = id == AccessoryEnchantmentId.None ? 0 : 44;
        if (id != AccessoryEnchantmentId.None) { var icon = new EnchantmentGlyphElement(); icon.SetEnchantment(id); icon.Width.Set(30, 0); icon.Height.Set(30, 0); icon.IgnoresMouseInteraction = true; card.Append(icon); }
        Label(card, inset, 0, width - 22 - inset, 26, Warning, 0.84f).SetText(heading);
        Label(card, inset, 29, width - 22 - inset, height - 42, Ink).SetText(content);
        body.Add(row);
    }
    private void BuildGuide(float width)
    {
        GuideRow(width, Word("How it works", "Как это работает"), Word("Upgrade the TARGET to open slots. Reforge a DONOR in the Goblin's ordinary reforge slot: each reforge rolls one of 14 imprints at random (1/14 each). Transfer that exact imprint into one free target slot. Fusion is guaranteed; it is not another random roll.", "Улучши ЦЕЛЬ для открытия слотов. Перекуй ДОНОРА в обычном слоте Гоблина: каждая перековка случайно выбирает один из 14 отпечатков (шанс каждого 1/14). Слияние гарантированно переносит именно этот отпечаток в один свободный слот, без нового случайного выбора."), width < 600 ? 182 : 136);
        string prices = string.Join(" · ", Enumerable.Range(0, 5).Select(i => $"+{i + 1}: {Coins(AccessoryEnchantingService.UpgradeCostForLevel(i))}"));
        GuideRow(width, Word("Upgrade prices · per step", "Цены улучшений · за каждый шаг"), prices + "\n" + Word("+1…+5 means 1…5 slots, with no direct stat gain. Upgrades never consume a donor.", "+1…+5 означает 1…5 слотов, без прямого роста статов. Улучшение не расходует донора."), width < 600 ? 130 : 108);
        GuideRow(width, Word("What fusion costs and consumes", "Цена и расход при слиянии"), Word("Fusion fee uses the items' internal values: target / 10 + donor / 5, rounded down, minimum 10 gold, maximum 5 platinum. Exact price is shown before confirmation. The entire donor is destroyed; only its imprint transfers. Duplicate effects on the same target are blocked. Base properties, prefix, levels and donor's installed effects do not transfer.", "Плата считается по внутренней стоимости предметов: цель / 10 + донор / 5, с округлением вниз, минимум 10 золотых, максимум 5 платины. Точная цена видна до подтверждения. Донор уничтожается; переносится только отпечаток. Повтор одного эффекта на цели запрещён. Свойства, префикс, уровни и установленные эффекты донора не переносятся."), width < 600 ? 226 : 170);
        GuideRow(width, Word("When effects work", "Когда работают эффекты"), Word("Installed effects work while the target accessory is equipped. A transferable imprint alone grants no equipped bonus. Effects on different equipped accessories can stack; the caps listed below still apply. You can scroll this guide without moving your items.", "Установленные эффекты работают при ношении целевого аксессуара. Сам переносимый отпечаток бонуса не даёт. Эффекты на разных надетых аксессуарах могут складываться с указанными ниже ограничениями. Просмотр справочника не перемещает предметы."), width < 600 ? 166 : 126);
        foreach (var id in AccessoryEnchantmentCatalog.All) {
            var effect = AccessoryEnchantmentCatalog.Get(id);
            string content = effect.Effect;
            if (id == AccessoryEnchantmentId.SecondWind) content = Word("After taking damage: +6 defence and +6 lifeRegen units (3 HP/s) for 4 seconds with one copy. Each extra copy adds 1 second and 2 lifeRegen units (1 HP/s), capped at 10 seconds. Bastion adds another +6 defence during the effect.", "После получения урона: +6 защиты и +6 единиц регенерации (3 HP/с) на 4 секунды при одной копии. Каждая дополнительная копия добавляет 1 секунду и 2 единицы регенерации (1 HP/с), максимум 10 секунд. Бастион добавляет ещё +6 защиты на время эффекта.");
            GuideRow(width, effect.Name, content, id == AccessoryEnchantmentId.SecondWind ? (width < 600 ? 190 : 138) : id == AccessoryEnchantmentId.Echo ? 116 : 88, id);
        }
        GuideRow(width, Word("Combinations · across equipped accessories", "Сочетания · на всех надетых аксессуарах"), Word("Bastion + Vitality: extra +4 defence and +20 max HP. Precision + Rhythm: extra +2% crit and +3% attack speed. Velocity + Fortune: extra +0.08 luck while velocity squared ≥36. Vanguard + Ballistics + Spellweave + Command: extra +5% all damage. Each combination is granted once, not once per matching pair.", "Бастион + Живучесть: дополнительно +4 защиты и +20 максимального HP. Точность + Ритм: дополнительно +2% крита и +3% скорости атаки. Скорость + Удача: ещё +0,08 удачи при квадрате скорости ≥36. Авангард + Баллистика + Чароплетение + Командование: ещё +5% всего урона. Каждое сочетание срабатывает один раз, а не за каждую пару."), width < 600 ? 230 : 174);
        statusText.SetText(Word("Guide only. Scroll to see all 14 effects and combinations; your items stay in their slots.", "Справочник. Прокрути до всех 14 эффектов и сочетаний; предметы остаются в ячейках."));
    }
}
