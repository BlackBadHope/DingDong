using System;
using System.Collections.Generic;
using System.Linq;
using DingDongBiomeHomes;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.AiArsenal;
internal static partial class AiArsenalAudit
{
	private static string ExpectedDelivery(Item item, AiArsenalCategory category) => category switch
	{
		AiArsenalCategory.Ranged when item.useAmmo == AmmoID.Arrow => DamageTags.Arrow,
		AiArsenalCategory.Ranged when item.useAmmo == AmmoID.Bullet => DamageTags.Bullet,
		AiArsenalCategory.Magic => DamageTags.MagicProjectile,
		AiArsenalCategory.Summon => DamageTags.MinionProjectile,
		_ => DamageTags.KineticContact
	};
}
