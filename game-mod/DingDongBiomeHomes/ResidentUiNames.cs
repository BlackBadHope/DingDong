using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Localization;
namespace DingDongBiomeHomes;
internal static class ResidentUiNames
{
    private static readonly Dictionary<string,string> Russian=new(StringComparer.Ordinal){
        ["CalamityMod/Bandit"]="Бандит",
        ["ThoriumMod/DesertAcolyte"]="Пустынный послушник",
        ["CalValEX/Jharim"]="Тиран джунглей",
        ["ThoriumMod/Druid"]="Друид",
        ["AlchemistNPCLite/Alchemist"]="Алхимик",
        ["AlchemistNPCLite/Architect"]="Архитектор",
        ["AlchemistNPCLite/Brewer"]="Зельевар",
        ["AlchemistNPCLite/Jeweler"]="Ювелир",
        ["AlchemistNPCLite/Musician"]="Музыкант",
        ["AlchemistNPCLite/Operator"]="Оператор",
        ["AlchemistNPCLite/Tinkerer"]="Мастер",
        ["AlchemistNPCLite/YoungBrewer"]="Юный зельевар",
        ["Fargowiltas/LumberJack"]="Дровосек",
        ["Fargowiltas/Mutant"]="Мутант",
        ["Fargowiltas/Squirrel"]="Белка",
        ["ThoriumMod/Cobbler"]="Сапожник",
        ["ThoriumMod/ConfusedZombie"]="Растерянный зомби",
        ["ThoriumMod/Cook"]="Повар",
        ["ThoriumMod/Tracker"]="Следопыт",
        ["ThoriumMod/WeaponMaster"]="Оружейный мастер",
        ["CalamityMod/Archmage"]="Архимаг",
        ["CalValEX/OracleNPC"]="Оракул",
        ["Fargowiltas/Deviantt"]="Девиант",
        ["ThoriumMod/Spiritualist"]="Спиритуалист",
        ["WeaponEnchantments/Witch"]="Ведьма",
        ["CalamityMod/BrimstoneWitch"]="Серная ведьма",
        ["Fargowiltas/Abominationn"]="Мерзость",
        ["CalamityMod/SeaKing"]="Морской король",
        ["CalValEX/JellyPriestNPC"]="Жрица медуз",
        ["FishermanNPC/Fisherman"]="Старый рыбак",
        ["ThoriumMod/Diverman"]="Водолаз",
        ["UnuBattleRods/Fishylady"]="Рыбачка",
        ["MagicStorage/Golem"]="Голем хранилища",
        ["miningcracks_take_on_luiafk/Deeps"]="Дипс",
        ["ThoriumMod/Blacksmith"]="Кузнец"
    };
    internal static string Resolve(string key,int type)
    {
        string native=Lang.GetNPCNameValue(type);
        if(Language.ActiveCulture.CultureInfo.TwoLetterISOLanguageName!="ru"||native.Any(c=>c>='А'&&c<='я'||c is 'Ё' or 'ё'))return native;
        return Russian.TryGetValue(key,out string value)?value:native;
    }
}
