using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.MobRelics;

internal sealed partial class MobRelicGameplayProbe
{
	private int previousMyPlayer;
	private Player previousProbePlayer;
	private int targetIndex = -1;
	private NPC previousTarget;
	private Projectile spawnedEcho;
	private Player player;
	private MobRelicPlayer relicPlayer;
	private NPC target;
	private HashSet<int> activeProjectiles;
	private string stage = "setup";
	private string failure = "unknown";

	internal static bool Run(out string failure)
	{
		MobRelicGameplayProbe probe = new();
		bool passed = probe.Execute();
		failure = probe.failure;
		return passed;
	}

	private bool Execute()
	{
		previousMyPlayer = Main.myPlayer;
		previousProbePlayer = Main.player[0];
		try
		{
			PreparePlayer();
			if (!EquipConvergedSet() || !ExerciseWard() || !PrepareTarget() || !SpawnEcho() || !ExerciseEchoDamage())
				return false;
			failure = "none";
			return true;
		}
		catch (Exception exception)
		{
			failure = $"{stage}:{exception.GetType().Name}:{exception.Message}";
			return false;
		}
		finally
		{
			if (spawnedEcho != null)
				spawnedEcho.active = false;
			if (targetIndex >= 0)
				Main.npc[targetIndex] = previousTarget;
			Main.player[0] = previousProbePlayer;
			Main.myPlayer = previousMyPlayer;
		}
	}

	private void PreparePlayer()
	{
		player = new Player { whoAmI = 0, active = true, dead = false, position = new Vector2(1600f, 1600f), moveSpeed = 1f, manaCost = 1f };
		relicPlayer = MobRelicAudit.EnsureProbeRelicPlayer(player);
		player.GetDamage(DamageClass.Generic) = StatModifier.Default;
		player.GetCritChance(DamageClass.Generic) = 0f;
		player.GetAttackSpeed(DamageClass.Generic) = 1f;
		player.statDefense = Player.DefenseStat.Default;
		player.endurance = 0f;
		player.lifeRegen = 0;
		player.statLifeMax2 = 100;
		player.statManaMax2 = 20;
		Item heldItem = new();
		heldItem.SetDefaults(ItemID.WoodenSword);
		player.selectedItem = 0;
		player.inventory[player.selectedItem] = heldItem;
		Main.player[player.whoAmI] = player;
	}
}
