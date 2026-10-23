using System;
using System.Reflection;
using DingDongBiomeHomes.Compatibility;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Acceptance;

/// <summary>Opt-in checks on disposable dedicated profiles only.</summary>
public sealed class HotfixRuntimeProbeSystem : ModSystem
{
	public override void PostAddRecipes()
	{
		if (!Main.dedServ || Environment.GetEnvironmentVariable("DINGDONG_HOTFIX_E2E_AUTO") != "1")
			return;
		try
		{
			int checks = CheckStacks();
			checks += CheckSquirrel();
			Emit($"DINGDONG_HOTFIX_RUNTIME|verdict=PASS|checks={checks}|world=DISPOSABLE");
		}
		catch (Exception error)
		{
			Mod.Logger.Error(error);
			Emit($"DINGDONG_HOTFIX_RUNTIME|verdict=FAIL|reason={error.GetType().Name}:{error.Message.Replace('|', '/')}");
		}
	}

	private static int CheckStacks()
	{
		int checks = 0;
		foreach (int type in new[] { ItemID.WoodenArrow, ItemID.CopperCoin, ItemID.Wood, ItemID.Barrel })
		{
			Item destination = NewItem(type, 10), source = NewItem(type, 20);
			Origin(destination, DamageTags.Crafted);
			Origin(source, DamageTags.CreatureDrop);
			Require(ItemLoader.CanStack(destination, source), $"all-mod stack veto for {type}");
			Require(ItemLoader.TryStackItems(destination, source, out int transferred) && transferred == 20 && destination.stack == 30 && source.stack == 0,
				$"all-mod transfer/count mismatch for {type}");
			checks += 2;
		}
		Item left = NewItem(ItemID.WoodenArrow, 10), right = NewItem(ItemID.WoodenArrow, 20);
		typeof(DamageAffinityGlobalItem).GetProperty(nameof(DamageAffinityGlobalItem.ElementTag)).SetValue(left.GetGlobalItem<DamageAffinityGlobalItem>(), DamageTags.Fire);
		typeof(DamageAffinityGlobalItem).GetProperty(nameof(DamageAffinityGlobalItem.ElementTag)).SetValue(right.GetGlobalItem<DamageAffinityGlobalItem>(), DamageTags.Frost);
		Require(!ItemLoader.CanStack(left, right), "distinct elements merged");
		return checks + 1;
	}

	private static int CheckSquirrel()
	{
		ModProjectile prototype = ModContent.Find<ModProjectile>("CalamityMod/SquirrelSquireMinion");
		Projectile projectile = new();
		projectile.SetDefaults(prototype.Type);
		Require(projectile.TryGetGlobalProjectile(out CalamitySquirrelOwnerCompatibility compatibility), "compatibility global not attached");
		int slot = Main.maxPlayers - 1;
		Player saved = Main.player[slot];
		FieldInfo ownerField = projectile.ModProjectile.GetType().GetField("Owner", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		try
		{
			Player owner = new() { whoAmI = slot, active = true };
			owner.Center = new Microsoft.Xna.Framework.Vector2(Main.spawnTileX * 16f, Main.spawnTileY * 16f - 32f);
			Main.player[slot] = owner;
			projectile.owner = slot;
			projectile.Center = owner.Center;
			projectile.ai[2] = 1;
			ownerField.SetValue(projectile.ModProjectile, null);
			Require(compatibility.PreAI(projectile), "valid owner blocked");
			projectile.ModProjectile.AI();
			Require(ReferenceEquals(ownerField.GetValue(projectile.ModProjectile), owner) && projectile.ai[2] == 1,
				"actual Calamity AI lost local owner or changed initialization flag");
			projectile.owner = -1;
			Require(!compatibility.PreAI(projectile), "invalid owner accepted");
			return 4;
		}
		finally { Main.player[slot] = saved; }
	}

	private static Item NewItem(int type, int stack)
	{
		Item item = new();
		item.SetDefaults(type);
		item.stack = stack;
		return item;
	}
	private static void Origin(Item item, string origin) =>
		typeof(DamageAffinityGlobalItem).GetProperty(nameof(DamageAffinityGlobalItem.AcquisitionTag)).SetValue(item.GetGlobalItem<DamageAffinityGlobalItem>(), origin);
	private static void Require(bool condition, string message)
	{
		if (!condition) throw new InvalidOperationException(message);
	}
	private void Emit(string marker) { Mod.Logger.Info(marker); Console.WriteLine(marker); }
}
