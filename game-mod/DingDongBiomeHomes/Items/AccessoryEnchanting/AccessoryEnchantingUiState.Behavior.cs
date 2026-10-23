using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;

namespace DingDongBiomeHomes.Items.AccessoryEnchanting;
internal sealed partial class AccessoryEnchantingUiState
{
    private uint statusAt, statusUntil;
    internal void NotifyResult(bool success, string failure)
    {
        armedFusion = null;
        if (success) { SetStatus(Word("Done. The result is in the TARGET slot; click it to retrieve your accessory.", "Готово. Результат в ячейке СОХРАНЯЕМ; кликни, чтобы забрать аксессуар."), true); SoundEngine.PlaySound(SoundID.Item37); Recipe.FindRecipes(); }
        else { SetStatus(Failure(failure), false); SoundEngine.PlaySound(SoundID.MenuTick); }
        lastRefresh = 0;
    }
    private void SetStatus(string text, bool success)
    {
        statusAt = (uint)Main.GameUpdateCount; statusUntil = 600;
        statusText.TextColor = success ? Good : Warning; statusText.SetText(text);
    }
    private static bool Russian => Language.ActiveCulture?.Name?.StartsWith("ru", StringComparison.OrdinalIgnoreCase) == true;
    private static string Coins(long copper) => AccessoryWorkshopFormatting.Coins(copper, Russian);
    private static string Word(string english, string russian) => Russian ? russian : english;
    private static string Failure(string failure) => failure switch {
        "accessory-only" => Word("Place an accessory in each required slot.", "Положи аксессуар в каждую нужную ячейку."),
        "single-accessory" => Word("Separate ONE accessory from the stack first.", "Сначала отдели ОДИН аксессуар от стопки."),
        "maximum-level" => Word("All five slots are already unlocked.", "Все пять слотов уже открыты."),
        "no-unlocked-slot" => Word("No free slot. Upgrade the target first.", "Нет свободного слота. Сначала улучши цель."),
        "donor-has-no-imprint" => Word("Reforge the donor first to roll an imprint.", "Сначала перекуй донора, чтобы получить отпечаток."),
        "duplicate-enchantment" => Word("This effect is already installed on the target.", "Этот эффект уже установлен на цели."),
        "insufficient-coins" => Word("Not enough coins in accessible storage.", "Недостаточно монет в доступных хранилищах."),
        "goblin-session-required" => Word("Stay near the Goblin and keep this dialogue open.", "Оставайся рядом с Гоблином и держи диалог открытым."),
        "coin-transaction-failed" => Word("Payment failed; no upgrade was committed.", "Оплата не прошла; улучшение не выполнено."),
        _ => Word("Transaction rejected. Items were not consumed.", "Операция отклонена. Предметы не израсходованы.") };
}
