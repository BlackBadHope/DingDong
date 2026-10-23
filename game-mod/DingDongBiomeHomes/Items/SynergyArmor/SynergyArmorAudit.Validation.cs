using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DingDongBiomeHomes.Items.MobRelics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Items.SynergyArmor;
internal static partial class SynergyArmorAudit
{
	private static void RunRuntimeProbe(IReadOnlyDictionary<SynergyArmorSetId, SynergyArmorSet> sets, ref int setHooksPassed, ref int runtimePassed, ref int relicLinksPassed, List<string> failures)
	{
		int previousMyPlayer = Main.myPlayer;
		Player previousPlayer = Main.player[0];
		NPC previousTarget = Main.npc[0];
		List<Projectile> spawned = new();
		try
		{
			Player player = new()
			{
				whoAmI = 0,
				active = true,
				dead = false
			};
			player.position = new Microsoft.Xna.Framework.Vector2(1600f, 1600f);
			player.statLife = 100;
			player.statLifeMax2 = 100;
			player.statMana = 20;
			player.statManaMax2 = 20;
			Item heldItem = new();
			heldItem.SetDefaults(ItemID.WoodenSword);
			player.selectedItem = 0;
			player.inventory[player.selectedItem] = heldItem;
			EnsureProbePlayers(player, out SynergyArmorPlayer armorPlayer, out MobRelicPlayer relicPlayer);
			Main.player[0] = player;
			Main.myPlayer = 0;
			NPC target = new();
			target.SetDefaults(NPCID.BlueSlime);
			target.whoAmI = 0;
			target.active = true;
			target.friendly = false;
			target.Center = player.Center + Microsoft.Xna.Framework.Vector2.UnitX * 120f;
			Main.npc[0] = target;
			SynergyArmorGlobalItem global = new();
			HashSet<string> signatures = new(StringComparer.Ordinal);
			MobRelicSet[] providerRelics = MobSpecificRelicCatalog.RegisteredSets.Values.GroupBy(set => set.Source.Provider, StringComparer.Ordinal).Select(group => group.First()).Take(3).ToArray();
			foreach ((SynergyArmorSetId setId, SynergyArmorSet set)in sets.OrderBy(pair => pair.Key))
			{
				armorPlayer.ResetEffects();
				relicPlayer.ResetEffects();
				player.setBonus = string.Empty;
				Item head = NewItem(set.Head.Type);
				Item body = NewItem(set.Body.Type);
				Item legs = NewItem(set.Legs.Type);
				string key = global.IsArmorSet(head, body, legs);
				global.UpdateArmorSet(player, key);
				if (string.Equals(key, set.Definition.Key, StringComparison.Ordinal) && armorPlayer.ActiveSetId == setId && player.setBonus.Contains(set.Definition.LocalizedSetBonus(), StringComparison.Ordinal))
				{
					setHooksPassed++;
				}
				else
				{
					Failure(failures, $"set-hook:{setId}");
				}

				foreach (MobRelicSet relicSet in providerRelics)
				{
					foreach (GeneratedMobRelicItem relic in relicSet.Items)
						relic.UpdateAccessory(player, hideVisual: false);
				}

				relicPlayer.UpdateEquips();
				armorPlayer.PostUpdateEquips();
				if (armorPlayer.ProviderCount >= Math.Min(3, providerRelics.Length) && armorPlayer.RelicSourceCount >= providerRelics.Length && armorPlayer.ActiveAspectCount > 0 && armorPlayer.ActiveTierCount == Enum.GetValues<MobRelicTier>().Length)
				{
					relicLinksPassed++;
				}
				else
				{
					Failure(failures, $"relic-link:{setId}");
				}

				int projectileIndex = armorPlayer.ForceMechanicProbe(setId, target, out string signature);
				Projectile projectile = projectileIndex >= 0 && projectileIndex < Main.maxProjectiles ? Main.projectile[projectileIndex] : null;
				if (projectile?.ModProjectile is SynergyArmorPulseProjectile pulse && projectile.damage > 0 && (int)projectile.ai[0] == (int)setId && signatures.Add(signature))
				{
					NPC.HitModifiers modifiers = target.GetIncomingStrikeModifiers(projectile.DamageType, 1, false);
					DamageAuthoritySystem.ApplyProjectileHit(target, projectile, ref modifiers);
					float finalDamage = modifiers.FinalDamage.ApplyTo(projectile.damage);
					pulse.OnHitNPC(target, default, Math.Max(1, (int)finalDamage));
					if (finalDamage > 0f && target.buffType.Any(type => type > 0))
						runtimePassed++;
					else
						Failure(failures, $"runtime-damage:{setId}");
					spawned.Add(projectile);
				}
				else
				{
					Failure(failures, $"runtime-spawn:{setId}");
				}

				Array.Clear(target.buffType);
				Array.Clear(target.buffTime);
			}
		}
		catch (Exception exception)
		{
			Failure(failures, $"runtime:{exception.GetType().Name}:{exception.Message}");
		}
		finally
		{
			foreach (Projectile projectile in spawned)
				projectile.active = false;
			Main.npc[0] = previousTarget;
			Main.player[0] = previousPlayer;
			Main.myPlayer = previousMyPlayer;
		}
	}
}
