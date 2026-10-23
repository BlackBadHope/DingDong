using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Compatibility;

/// <summary>
/// Calamity 2.2.2 syncs its initialization flag in ai[2], but its cached Owner
/// field is local. A remotely created minion therefore skips initializing Owner.
/// Restore that local reference before AI without changing its synchronized AI.
/// </summary>
public sealed class CalamitySquirrelOwnerCompatibility : GlobalProjectile
{
	private static Type squirrelType;
	private static FieldInfo ownerField;

	public override void Load()
	{
		if (!ModLoader.TryGetMod("CalamityMod", out Mod calamity) || calamity.Version != new Version(2, 2, 2))
			return;
		squirrelType = calamity.Code.GetType("CalamityMod.Projectiles.Summon.SquirrelSquireMinion", throwOnError: true);
		ownerField = squirrelType.GetField("Owner", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (ownerField == null || ownerField.FieldType != typeof(Player))
			throw new InvalidOperationException("Calamity 2.2.2 squirrel owner contract changed.");
	}

	public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) =>
		squirrelType != null && entity.ModProjectile?.GetType() == squirrelType;

	public override bool PreAI(Projectile projectile)
	{
		if (ownerField == null || projectile.ModProjectile?.GetType() != squirrelType)
			return true;
		if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers || Main.player[projectile.owner] == null)
			return false;
		Player owner = Main.player[projectile.owner];
		if (!ReferenceEquals(ownerField.GetValue(projectile.ModProjectile), owner))
			ownerField.SetValue(projectile.ModProjectile, owner);
		return true;
	}

	public override void Unload()
	{
		ownerField = null;
		squirrelType = null;
	}
}
